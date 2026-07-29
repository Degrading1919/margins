using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
        [SerializeField] private float gravity = -20f;

        private float pitch;
        private float verticalVelocity;

        public bool IsGameplayMode { get; private set; }
        public CursorLockMode RequestedCursorLockState { get; private set; }
        public bool IsGameplayInputActive =>
            IsGameplayMode &&
            (Application.isBatchMode ||
             Cursor.lockState == CursorLockMode.Locked);

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
            SetGameplayMode(true);
        }

        private void OnDisable()
        {
            IsGameplayMode = false;
            RequestedCursorLockState = CursorLockMode.None;
            UnlockCursor();
        }

        private void Update()
        {
            HandleModeToggle();
            if (!IsGameplayMode)
            {
                return;
            }

            HandleLook();
            HandleMovement();
        }

        public void SetGameplayMode(bool isGameplayMode)
        {
            IsGameplayMode = isGameplayMode;
            RequestedCursorLockState = isGameplayMode
                ? CursorLockMode.Locked
                : CursorLockMode.None;
            if (isGameplayMode)
            {
                LockCursor();
            }
            else
            {
                UnlockCursor();
            }
        }

        private void HandleMovement()
        {
            if (characterController == null || Keyboard.current == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            input.y += Keyboard.current.wKey.isPressed ? 1f : 0f;
            input.y -= Keyboard.current.sKey.isPressed ? 1f : 0f;
            input.x += Keyboard.current.dKey.isPressed ? 1f : 0f;
            input.x -= Keyboard.current.aKey.isPressed ? 1f : 0f;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 horizontalMovement = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
            characterController.Move(horizontalMovement * Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void HandleLook()
        {
            if (cameraPivot == null ||
                Mouse.current == null ||
                !IsGameplayInputActive)
            {
                return;
            }

            Vector2 lookDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, lookDelta.x, 0f);
            pitch = Mathf.Clamp(pitch - lookDelta.y, MinimumPitchDegrees, MaximumPitchDegrees);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleModeToggle()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetGameplayMode(!IsGameplayMode);
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
