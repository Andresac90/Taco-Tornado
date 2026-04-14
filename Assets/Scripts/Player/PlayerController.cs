using UnityEngine;

namespace TacoTornado.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed  = GameConstants.PLAYER_MOVE_SPEED;
        [SerializeField] private float smoothTime = 0.08f;

        [Header("Reference")]
        [Tooltip("Drag the Camera child here so movement is relative to look direction.")]
        [SerializeField] private Transform cameraTransform;

        private CharacterController cc;
        private Vector3 currentVelocity;
        private Vector3 smoothVelocity;

        private float verticalVelocity = 0f;
        private const float gravity    = -9.81f;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isShiftActive) return;

            MovePlayer(GetMovementInput());
        }

        private Vector2 GetMovementInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            return new Vector2(h, v).normalized;
        }

        private void MovePlayer(Vector2 input)
        {
            Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
            Vector3 forward = cam != null ? cam.forward : transform.forward;
            Vector3 right   = cam != null ? cam.right   : transform.right;

            forward.y = 0f; forward.Normalize();
            right.y   = 0f; right.Normalize();

            if (input.sqrMagnitude < 0.01f)
                currentVelocity = Vector3.SmoothDamp(currentVelocity, Vector3.zero, ref smoothVelocity, smoothTime);
            else
            {
                Vector3 desiredDir = (right * input.x + forward * input.y).normalized;
                currentVelocity = Vector3.SmoothDamp(currentVelocity, desiredDir * moveSpeed, ref smoothVelocity, smoothTime);
            }

            // Gravity
            if (cc.isGrounded)
                verticalVelocity = -0.5f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 move = currentVelocity;
            move.y = verticalVelocity;
            cc.Move(move * Time.deltaTime);
        }
    }
}