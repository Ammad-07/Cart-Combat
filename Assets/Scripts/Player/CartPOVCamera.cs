using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CartCombat.Player
{
    public enum CameraViewMode
    {
        FirstPersonPOV,
        ThirdPersonChase
    }

    /// <summary>
    /// Point Of View camera system for Cart Combat.
    /// In First-Person POV, the camera is positioned at the character's eye level looking over the cart handlebar.
    /// Toggleable with 'V' to Third-Person over-the-shoulder chase view.
    /// </summary>
    public class CartPOVCamera : MonoBehaviour
    {
        [Header("Target & View Settings")]
        [SerializeField] private Transform characterBody;
        [SerializeField] private CameraViewMode currentView = CameraViewMode.FirstPersonPOV;

        [Header("POV Offsets (Local to Character)")]
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 1.65f, -0.35f);
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0f, 2.2f, -3.2f);
        [SerializeField] private float transitionSpeed = 8f;

        [Header("Look Controls")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [SerializeField] private float minPitch = -65f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private bool lockCursor = true;

        [Header("Head Bobbing & Motion")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobFrequency = 8f;
        [SerializeField] private float bobAmount = 0.04f;

        private float pitch = 0f;
        private float yaw = 0f;
        private Vector3 targetLocalPosition;
        private float bobTimer = 0f;

        public CameraViewMode CurrentView => currentView;

        private void Start()
        {
            if (characterBody == null && transform.parent != null)
            {
                characterBody = transform.parent;
            }

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            yaw = (characterBody != null) ? characterBody.eulerAngles.y : transform.eulerAngles.y;
            targetLocalPosition = (currentView == CameraViewMode.FirstPersonPOV) ? firstPersonOffset : thirdPersonOffset;
            transform.localPosition = targetLocalPosition;
        }

        private void Update()
        {
            HandleInput();
            ApplyRotation();
            UpdateViewPosition();
        }

        private void HandleInput()
        {
            float mouseX = 0f;
            float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                mouseX = delta.x * mouseSensitivity * 0.1f;
                mouseY = delta.y * mouseSensitivity * 0.1f;
            }

            // Toggle view mode with V key
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                ToggleViewMode();
            }

            // Escape toggles cursor lock
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }
#else
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleViewMode();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
            }
#endif

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void ApplyRotation()
        {
            if (characterBody != null)
            {
                // Rotate character body horizontally (Yaw)
                characterBody.rotation = Quaternion.Euler(0f, yaw, 0f);

                // Rotate camera vertically (Pitch)
                transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }

        private void UpdateViewPosition()
        {
            Vector3 baseOffset = (currentView == CameraViewMode.FirstPersonPOV) ? firstPersonOffset : thirdPersonOffset;

            // Optional head bobbing when moving
            Vector3 bobOffset = Vector3.zero;
            if (enableBobbing && characterBody != null)
            {
                Rigidbody rb = characterBody.GetComponent<Rigidbody>();
                CharacterController cc = characterBody.GetComponent<CharacterController>();
                float speed = 0f;

                if (rb != null) speed = rb.linearVelocity.magnitude;
                else if (cc != null) speed = cc.velocity.magnitude;

                if (speed > 0.5f)
                {
                    bobTimer += Time.deltaTime * bobFrequency;
                    bobOffset.y = Mathf.Sin(bobTimer) * bobAmount;
                    bobOffset.x = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f;
                }
                else
                {
                    bobTimer = 0f;
                }
            }

            targetLocalPosition = baseOffset + bobOffset;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * transitionSpeed);
        }

        public void ToggleViewMode()
        {
            currentView = (currentView == CameraViewMode.FirstPersonPOV) ? CameraViewMode.ThirdPersonChase : CameraViewMode.FirstPersonPOV;
        }

        public void SetTarget(Transform character)
        {
            characterBody = character;
        }
    }
}
