using UnityEngine;
using UnityEngine.InputSystem;

namespace Margins
{
    public sealed class FirstPersonController : MonoBehaviour
    {
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
            pitch = Mathf.Clamp(pitch - lookDelta.y, -85f, 85f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleModeToggle()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetGameplayMode(!IsGameplayMode);
            }
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
