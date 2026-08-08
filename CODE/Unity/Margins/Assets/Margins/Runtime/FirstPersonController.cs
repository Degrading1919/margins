using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Margins
{
    [System.Serializable]
    public struct FirstStorePlayerTransformSnapshot
    {
        public Vector3 worldPosition;
        public float bodyYawDegrees;
        public float cameraPitchDegrees;

        public FirstStorePlayerTransformSnapshot(
            Vector3 worldPosition,
            float bodyYawDegrees,
            float cameraPitchDegrees)
        {
            this.worldPosition = worldPosition;
            this.bodyYawDegrees = bodyYawDegrees;
            this.cameraPitchDegrees = cameraPitchDegrees;
        }
    }

    public sealed class FirstPersonController : MonoBehaviour
    {
        private const float MinimumPitchDegrees = -85f;
        private const float MaximumPitchDegrees = 85f;

        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string inputActionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string jumpActionName = "Jump";
        [SerializeField, Min(0f)] private float moveSpeed = 0.9f;
        [FormerlySerializedAs("briskWalkSpeed")]
        [SerializeField, Min(0f)] private float sprintSpeed = 4.5f;
        [SerializeField, Min(0.01f)] private float acceleration = 17f;
        [SerializeField, Min(0.01f)] private float deceleration = 23f;
        [FormerlySerializedAs("mouseSensitivity")]
        [SerializeField, Min(0f)] private float horizontalLookSensitivity = 0.1f;
        [SerializeField, Min(0f)] private float verticalLookSensitivity = 0.1f;
        [SerializeField] private bool invertY;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.15f;
        [SerializeField, Range(0f, 0.08f)] private float cameraBobAmplitude = 0.026f;
        [SerializeField, Range(0f, 3f)] private float cameraBobFrequency = 1.75f;

        private float pitch;
        private float verticalVelocity;
        private Vector3 planarVelocity;
        private Vector3 cameraBaseLocalPosition;
        private Camera playerCamera;
        private float baseFieldOfView = 70f;
        private float bobPhase;
        private float distanceSinceFootstep;
        private bool cameraStateCaptured;
        private bool wasGrounded;
        private int discardLookThroughFrame;
        private bool hasAppliedInitialMode;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private bool ownsFallbackActions;

        public event Action<bool> Footstep;
        public event Action Landed;
        public event Action Jumped;

        public bool IsGameplayMode { get; private set; }
        public CursorLockMode RequestedCursorLockState { get; private set; }
        public bool IsGameplayInputActive =>
            IsGameplayMode &&
            (Application.isBatchMode ||
             Cursor.lockState == CursorLockMode.Locked);
        public bool CameraMotionEnabled { get; private set; } = true;
        public float MouseSensitivity => horizontalLookSensitivity;
        public float HorizontalLookSensitivity => horizontalLookSensitivity;
        public float VerticalLookSensitivity => verticalLookSensitivity;
        public bool InvertY => invertY;
        public bool IsSprinting { get; private set; }
        public bool IsBriskWalking => IsSprinting;
        public bool IsGrounded { get; private set; }
        public float CurrentPlanarSpeed => planarVelocity.magnitude;
        public float VerticalVelocity => verticalVelocity;
        public float WalkSpeed => moveSpeed;
        public float SprintSpeed => sprintSpeed;
        public float JumpHeight => jumpHeight;

        public FirstStorePlayerTransformSnapshot CaptureTransformSnapshot()
        {
            return new FirstStorePlayerTransformSnapshot(
                transform.position,
                transform.eulerAngles.y,
                pitch);
        }

        public bool TryPreflightApplyTransformSnapshot(
            FirstStorePlayerTransformSnapshot snapshot,
            out string error)
        {
            if (!IsFinite(snapshot.worldPosition) ||
                !IsFinite(snapshot.bodyYawDegrees) ||
                !IsFinite(snapshot.cameraPitchDegrees))
            {
                error = "Player transform contains a non-finite value.";
                return false;
            }

            if (snapshot.cameraPitchDegrees < MinimumPitchDegrees ||
                snapshot.cameraPitchDegrees > MaximumPitchDegrees)
            {
                error = $"Player camera pitch must be between {MinimumPitchDegrees:0} and {MaximumPitchDegrees:0} degrees.";
                return false;
            }

            if (cameraPivot == null)
            {
                error = "Player camera pivot is not configured.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool TryApplyTransformSnapshot(
            FirstStorePlayerTransformSnapshot snapshot,
            out string error)
        {
            if (!TryPreflightApplyTransformSnapshot(snapshot, out error))
            {
                return false;
            }

            bool characterControllerWasEnabled =
                characterController != null && characterController.enabled;
            if (characterControllerWasEnabled)
            {
                characterController.enabled = false;
            }

            try
            {
                transform.SetPositionAndRotation(
                    snapshot.worldPosition,
                    Quaternion.Euler(0f, snapshot.bodyYawDegrees, 0f));
                pitch = snapshot.cameraPitchDegrees;
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                verticalVelocity = 0f;
                planarVelocity = Vector3.zero;
                bobPhase = 0f;
                distanceSinceFootstep = 0f;
                EnsureCameraState();
                cameraPivot.localPosition = cameraBaseLocalPosition;
            }
            finally
            {
                if (characterControllerWasEnabled)
                {
                    characterController.enabled = true;
                }
            }

            error = string.Empty;
            return true;
        }

        private void OnEnable()
        {
            EnsureCameraState();
            if (characterController != null)
            {
                characterController.minMoveDistance = 0f;
            }
            if (!TryResolveMovementActions(out string error))
            {
                Debug.LogError(error, this);
            }
            IsGrounded = characterController != null &&
                         characterController.isGrounded;
            wasGrounded = IsGrounded;
            SetGameplayMode(true);
        }

        private void OnDisable()
        {
            IsGameplayMode = false;
            RequestedCursorLockState = CursorLockMode.None;
            planarVelocity = Vector3.zero;
            IsSprinting = false;
            SetMovementActionsEnabled(false);
            if (ownsFallbackActions)
            {
                moveAction?.Dispose();
                lookAction?.Dispose();
                sprintAction?.Dispose();
                jumpAction?.Dispose();
                moveAction = null;
                lookAction = null;
                sprintAction = null;
                jumpAction = null;
                ownsFallbackActions = false;
            }
            ResetCameraMotion(true);
            UnlockCursor();
        }

        private void Update()
        {
            HandleModeToggle();
            if (!IsGameplayMode)
            {
                IsSprinting = false;
                ResetCameraMotion(false);
                return;
            }

            HandleLook();
            HandleMovement();
        }

        public void SetGameplayMode(bool isGameplayMode)
        {
            bool shouldDiscardRelockDelta =
                isGameplayMode &&
                hasAppliedInitialMode &&
                !IsGameplayMode;
            IsGameplayMode = isGameplayMode;
            RequestedCursorLockState = isGameplayMode
                ? CursorLockMode.Locked
                : CursorLockMode.None;
            if (isGameplayMode)
            {
                discardLookThroughFrame = shouldDiscardRelockDelta
                    ? Time.frameCount + 1
                    : Time.frameCount - 1;
                LockCursor();
            }
            else
            {
                UnlockCursor();
            }
            hasAppliedInitialMode = true;
        }

        private void HandleMovement()
        {
            if (characterController == null || moveAction == null ||
                sprintAction == null || jumpAction == null)
            {
                return;
            }

            Vector2 input = Vector2.ClampMagnitude(
                moveAction.ReadValue<Vector2>(),
                1f);

            IsSprinting = input.sqrMagnitude > 0.01f &&
                          sprintAction.IsPressed();
            float targetSpeed = IsSprinting ? sprintSpeed : moveSpeed;
            Vector3 desiredVelocity =
                (transform.right * input.x + transform.forward * input.y) * targetSpeed;
            float rate = input.sqrMagnitude > 0.01f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                desiredVelocity,
                rate * Time.deltaTime);

            bool groundedBeforeMove = characterController.isGrounded;
            if (groundedBeforeMove && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (groundedBeforeMove && jumpAction.WasPressedThisFrame())
            {
                verticalVelocity = Mathf.Sqrt(
                    jumpHeight * -2f * gravity);
                groundedBeforeMove = false;
                Jumped?.Invoke();
            }
            else if (!groundedBeforeMove)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 frameMotion = planarVelocity + Vector3.up * verticalVelocity;
            CollisionFlags collisionFlags = characterController.Move(
                frameMotion * Time.deltaTime);
            if ((collisionFlags & CollisionFlags.Above) != 0 &&
                verticalVelocity > 0f)
            {
                verticalVelocity = 0f;
            }
            bool grounded = characterController.isGrounded ||
                            (collisionFlags & CollisionFlags.Below) != 0;
            if (grounded && !wasGrounded && verticalVelocity < -4f)
            {
                Landed?.Invoke();
            }
            wasGrounded = grounded;
            IsGrounded = grounded;

            UpdateCameraMotion(grounded, targetSpeed);
            UpdateFootsteps(grounded);
        }

        private void HandleLook()
        {
            if (cameraPivot == null ||
                lookAction == null ||
                !IsGameplayInputActive)
            {
                return;
            }

            if (Time.frameCount <= discardLookThroughFrame)
            {
                lookAction.ReadValue<Vector2>();
                return;
            }

            Vector2 rawDelta = lookAction.ReadValue<Vector2>();
            float horizontalLook = rawDelta.x * horizontalLookSensitivity;
            float verticalLook = rawDelta.y * verticalLookSensitivity;
            transform.Rotate(0f, horizontalLook, 0f);
            verticalLook = invertY ? -verticalLook : verticalLook;
            pitch = Mathf.Clamp(pitch - verticalLook, MinimumPitchDegrees, MaximumPitchDegrees);
            ApplyCameraRotation();
        }

        private void HandleModeToggle()
        {
            if (GamePauseMenuController.IsAnyMenuOpen)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetGameplayMode(!IsGameplayMode);
            }

        }

        public void ApplyPlayerSettings(
            float sensitivity,
            bool shouldInvertY,
            bool cameraMotionEnabled)
        {
            ApplyPlayerSettings(
                sensitivity,
                sensitivity,
                shouldInvertY,
                cameraMotionEnabled);
        }

        public void ApplyPlayerSettings(
            float horizontalSensitivity,
            float verticalSensitivity,
            bool shouldInvertY,
            bool cameraMotionEnabled)
        {
            horizontalLookSensitivity =
                Mathf.Clamp(horizontalSensitivity, 0.01f, 0.5f);
            verticalLookSensitivity =
                Mathf.Clamp(verticalSensitivity, 0.01f, 0.5f);
            invertY = shouldInvertY;
            CameraMotionEnabled = cameraMotionEnabled;
            if (!CameraMotionEnabled)
            {
                ResetCameraMotion(true);
            }
        }

        private void UpdateCameraMotion(bool grounded, float targetSpeed)
        {
            EnsureCameraState();
            if (cameraPivot == null)
            {
                return;
            }

            float speed = planarVelocity.magnitude;
            bool moving = CameraMotionEnabled && grounded && speed > 0.15f;
            Vector3 targetPosition = cameraBaseLocalPosition;
            float roll = 0f;
            if (moving)
            {
                float speedRatio = Mathf.Clamp01(speed / Mathf.Max(0.01f, targetSpeed));
                bobPhase += Time.deltaTime * cameraBobFrequency *
                            Mathf.Lerp(5.2f, 7.4f, speedRatio);
                targetPosition += new Vector3(
                    Mathf.Cos(bobPhase * 0.5f) * cameraBobAmplitude * 0.45f,
                    Mathf.Sin(bobPhase) * cameraBobAmplitude,
                    0f);
                roll = Mathf.Cos(bobPhase * 0.5f) * 0.35f * speedRatio;
            }

            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition,
                targetPosition,
                1f - Mathf.Exp(-14f * Time.deltaTime));
            ApplyCameraRotation(roll);

            if (playerCamera != null)
            {
                float targetFov = baseFieldOfView + (IsSprinting ? 2.5f : 0f);
                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView,
                    targetFov,
                    1f - Mathf.Exp(-7f * Time.deltaTime));
            }
        }

        private void UpdateFootsteps(bool grounded)
        {
            if (!grounded || planarVelocity.sqrMagnitude < 0.12f)
            {
                distanceSinceFootstep = 0f;
                return;
            }

            distanceSinceFootstep += planarVelocity.magnitude * Time.deltaTime;
            float stepDistance = IsSprinting ? 1.35f : 1.55f;
            if (distanceSinceFootstep < stepDistance)
            {
                return;
            }

            distanceSinceFootstep %= stepDistance;
            Footstep?.Invoke(IsSprinting);
        }

        public bool TryValidateInputConfiguration(out string error)
        {
            return TryResolveMovementActions(out error);
        }

        private bool TryResolveMovementActions(out string error)
        {
            if (moveAction != null && lookAction != null &&
                sprintAction != null && jumpAction != null)
            {
                SetMovementActionsEnabled(true);
                error = null;
                return true;
            }

            if (inputActions != null)
            {
                InputActionMap actionMap = inputActions.FindActionMap(
                    inputActionMapName,
                    false);
                moveAction = actionMap?.FindAction(moveActionName, false);
                lookAction = actionMap?.FindAction(lookActionName, false);
                sprintAction = actionMap?.FindAction(sprintActionName, false);
                jumpAction = actionMap?.FindAction(jumpActionName, false);
                ownsFallbackActions = false;
            }
            else
            {
                moveAction = new InputAction(
                    moveActionName,
                    InputActionType.Value,
                    expectedControlType: "Vector2");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                lookAction = new InputAction(
                    lookActionName,
                    InputActionType.Value,
                    "<Mouse>/delta",
                    expectedControlType: "Vector2");
                sprintAction = new InputAction(
                    sprintActionName,
                    InputActionType.Button,
                    "<Keyboard>/leftShift",
                    expectedControlType: "Button");
                jumpAction = new InputAction(
                    jumpActionName,
                    InputActionType.Button,
                    "<Keyboard>/space",
                    expectedControlType: "Button");
                ownsFallbackActions = true;
            }

            if (moveAction == null || lookAction == null ||
                sprintAction == null || jumpAction == null)
            {
                error =
                    $"Input action map '{inputActionMapName}' must define '{moveActionName}', " +
                    $"'{lookActionName}', " +
                    $"'{sprintActionName}', and '{jumpActionName}'.";
                return false;
            }

            if (moveAction.expectedControlType != "Vector2" ||
                lookAction.expectedControlType != "Vector2" ||
                sprintAction.expectedControlType != "Button" ||
                jumpAction.expectedControlType != "Button")
            {
                error =
                    "Player input actions must use Vector2 Move/Look and Button Sprint/Jump controls.";
                return false;
            }

            SetMovementActionsEnabled(true);
            error = null;
            return true;
        }

        private void SetMovementActionsEnabled(bool enabled)
        {
            InputAction[] actions =
            {
                moveAction,
                lookAction,
                sprintAction,
                jumpAction
            };
            foreach (InputAction action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                if (enabled)
                {
                    action.Enable();
                }
                else
                {
                    action.Disable();
                }
            }
        }

        private void EnsureCameraState()
        {
            if (cameraPivot == null)
            {
                return;
            }

            if (!cameraStateCaptured)
            {
                cameraBaseLocalPosition = cameraPivot.localPosition;
                cameraStateCaptured = true;
            }

            if (playerCamera == null)
            {
                playerCamera = cameraPivot.GetComponentInChildren<Camera>();
                if (playerCamera != null)
                {
                    baseFieldOfView = playerCamera.fieldOfView;
                }
            }
        }

        private void ResetCameraMotion(bool immediate)
        {
            EnsureCameraState();
            if (cameraPivot == null)
            {
                return;
            }

            cameraPivot.localPosition = immediate
                ? cameraBaseLocalPosition
                : Vector3.Lerp(
                    cameraPivot.localPosition,
                    cameraBaseLocalPosition,
                    1f - Mathf.Exp(-14f * Time.deltaTime));
            ApplyCameraRotation();
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = immediate
                    ? baseFieldOfView
                    : Mathf.Lerp(
                        playerCamera.fieldOfView,
                        baseFieldOfView,
                        1f - Mathf.Exp(-7f * Time.deltaTime));
            }
        }

        private void ApplyCameraRotation(float roll = 0f)
        {
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, roll);
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
