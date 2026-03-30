// FirstPersonLook.cs — Mouse look for the stationary player inside the food truck
// Attach to the Camera (child of player empty GO at the truck counter position)

using UnityEngine;

namespace TacoTornado.Player
{
    public class FirstPersonLook : MonoBehaviour
    {
        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = GameConstants.PLAYER_LOOK_SPEED;

        [Header("Clamp")]
        [SerializeField] private float clampUp = GameConstants.PLAYER_LOOK_CLAMP_UP;
        [SerializeField] private float clampDown = GameConstants.PLAYER_LOOK_CLAMP_DOWN;

        private float xRotation = 0f;
        private float yRotation = 0f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Initialize rotation from current transform
            Vector3 euler = transform.eulerAngles;
            yRotation = euler.y;
            xRotation = euler.x;
            if (xRotation > 180f) xRotation -= 360f;
        }

        private void Update()
        {
            if (!GameManager.Instance.isShiftActive) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -clampUp, clampDown);

            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        /// <summary>
        /// Temporarily unlock the cursor (for menus, end of shift, etc.)
        /// </summary>
        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
