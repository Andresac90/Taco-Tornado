// PlayerController.cs — WASD / gamepad joystick movement clamped inside the truck
// Attach to the Player root GameObject (parent of Camera).
// The camera child handles look; this script handles position.
//
// HOW TO SET UP:
//   1. Attach to the empty "Player" GO (NOT the camera).
//   2. Set truckCenter to the center of the playable area in world space.
//      (Or just leave it and adjust TRUCK_MIN/MAX_X/Z in GameConstants.)
//   3. The player slides left/right/forward/back — no jumping, no gravity issues.

using UnityEngine;

namespace TacoTornado.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed     = GameConstants.PLAYER_MOVE_SPEED;
        [SerializeField] private float smoothTime    = 0.08f; // acceleration feel

        [Header("Truck Bounds (local space)")]
        [Tooltip("Min/Max X and Z the player can reach inside the truck. Tweak to fit your model.")]
        [SerializeField] private float boundsMinX = GameConstants.TRUCK_MIN_X;
        [SerializeField] private float boundsMaxX = GameConstants.TRUCK_MAX_X;
        [SerializeField] private float boundsMinZ = GameConstants.TRUCK_MIN_Z;
        [SerializeField] private float boundsMaxZ = GameConstants.TRUCK_MAX_Z;

        [Header("Reference")]
        [Tooltip("Drag the Camera child here so movement is relative to where the player is looking horizontally.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController cc;
        private Vector3 currentVelocity;
        private Vector3 smoothVelocity; // for SmoothDamp

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isShiftActive) return;

            Vector2 input = GetMovementInput();
            MovePlayer(input);
            ClampToBounds();
        }

        // ──────────────────────────────────────────────
        //  INPUT
        // ──────────────────────────────────────────────

        private Vector2 GetMovementInput()
        {
            // Keyboard WASD
            float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
            float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down arrows

            // Gamepad left stick (Unity's default Input: "Horizontal"/"Vertical" also covers gamepad)
            // If you're using the new Input System, replace with InputSystem actions.

            return new Vector2(h, v).normalized;
        }

        // ──────────────────────────────────────────────
        //  MOVEMENT
        // ──────────────────────────────────────────────

        private void MovePlayer(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
            {
                // Decelerate to stop
                currentVelocity = Vector3.SmoothDamp(currentVelocity, Vector3.zero, ref smoothVelocity, smoothTime);
            }
            else
            {
                // Move relative to camera's horizontal facing direction
                Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;

                Vector3 forward = cam != null ? cam.forward : transform.forward;
                Vector3 right   = cam != null ? cam.right   : transform.right;

                // Flatten to horizontal plane
                forward.y = 0f; forward.Normalize();
                right.y   = 0f; right.Normalize();

                Vector3 desiredDir = (right * input.x + forward * input.y).normalized;
                Vector3 target     = desiredDir * moveSpeed;

                currentVelocity = Vector3.SmoothDamp(currentVelocity, target, ref smoothVelocity, smoothTime);
            }

            cc.Move(currentVelocity * Time.deltaTime);
        }

        // ──────────────────────────────────────────────
        //  BOUNDS CLAMP
        // ──────────────────────────────────────────────

        private void ClampToBounds()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsMinX, boundsMaxX);
            pos.z = Mathf.Clamp(pos.z, boundsMinZ, boundsMaxZ);
            // Y is managed by CharacterController gravity — don't clamp it
            transform.position = pos;
        }

        // ──────────────────────────────────────────────
        //  DEBUG GIZMO
        // ──────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((boundsMinX + boundsMaxX) / 2f, transform.position.y, (boundsMinZ + boundsMaxZ) / 2f);
            Vector3 size   = new Vector3(boundsMaxX - boundsMinX, 0.1f, boundsMaxZ - boundsMinZ);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
