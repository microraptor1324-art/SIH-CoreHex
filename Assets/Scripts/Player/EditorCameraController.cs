using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace ARMiningSimulator.Player
{
    /// <summary>
    /// Free-look camera controller for testing in Unity Editor Play Mode on PC/laptops.
    /// Controls:
    /// - Right-Click + Drag: Look around (yaw & pitch).
    /// - W/A/S/D or Arrow Keys: Walk around the virtual mine floor.
    /// - Left Shift: Sprint / faster walk.
    /// 
    /// On Android / iOS, this component automatically destroys itself so native ARCore
    /// 6-DoF phone motion tracking is 100% in control.
    /// </summary>
    public class EditorCameraController : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 1.8f;
        [SerializeField] private float _sprintSpeed = 3.2f;
        [SerializeField] private float _lookSensitivity = 0.15f;

        private float _yaw = 0f;
        private float _pitch = 0f;
        private bool _isLooking = false;

        private void Start()
        {
            Vector3 currentAngles = transform.eulerAngles;
            _yaw = currentAngles.y;
            _pitch = currentAngles.x;
            if (_pitch > 180f) _pitch -= 360f;
        }

        private void Update()
        {
            HandleMouseLook();
            HandleKeyboardMovement();
        }

        private void HandleMouseLook()
        {
            if (Mouse.current == null) return;

            // Hold right mouse button to look around
            if (Mouse.current.rightButton.isPressed)
            {
                if (!_isLooking)
                {
                    _isLooking = true;
                    Cursor.lockState = CursorLockMode.Confined;
                }

                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                _yaw += mouseDelta.x * _lookSensitivity;
                _pitch -= mouseDelta.y * _lookSensitivity;
                _pitch = Mathf.Clamp(_pitch, -75f, 75f);

                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            else
            {
                if (_isLooking)
                {
                    _isLooking = false;
                    Cursor.lockState = CursorLockMode.None;
                }
            }
        }

        private void HandleKeyboardMovement()
        {
            if (Keyboard.current == null) return;

            Vector3 moveDirection = Vector3.zero;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                moveDirection += transform.forward;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                moveDirection -= transform.forward;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                moveDirection -= transform.right;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                moveDirection += transform.right;

            // Flatten movement to stay parallel to the floor plane
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                moveDirection.Normalize();
                float currentSpeed = Keyboard.current.leftShiftKey.isPressed ? _sprintSpeed : _walkSpeed;
                transform.position += moveDirection * (currentSpeed * Time.deltaTime);
            }
        }
#else
        private void Awake()
        {
            // Self-destruct on mobile platforms so ARCore handles camera position
            Destroy(this);
        }
#endif
    }
}
