// PlayerInteraction.cs — Raycast-based pick up, hold, and place system
// Attach to the Camera alongside FirstPersonLook

using UnityEngine;

namespace TacoTornado.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private float interactRange = GameConstants.INTERACT_RANGE;
        [SerializeField] private LayerMask interactLayer;
        [SerializeField] private Transform holdPoint; // empty child of camera, slightly in front

        [Header("Visual Feedback")]
        [SerializeField] private Color highlightColor = Color.yellow;

        // State
        private IInteractable currentTarget;
        private GameObject highlightedObject;
        private Ingredient heldIngredient;
        private Camera cam;

        private void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (!GameManager.Instance.isShiftActive) return;

            HandleRaycast();
            HandleInput();
        }

        private void HandleRaycast()
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactRange, interactLayer))
            {
                var interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    if (currentTarget != interactable)
                    {
                        ClearHighlight();
                        currentTarget = interactable;
                        highlightedObject = hit.collider.gameObject;
                        SetHighlight(highlightedObject, true);
                    }
                    return;
                }
            }

            // Nothing hit — clear
            if (currentTarget != null)
            {
                ClearHighlight();
                currentTarget = null;
                highlightedObject = null;
            }
        }

        private void HandleInput()
        {
            // Left click — interact / pick up
            if (Input.GetMouseButtonDown(0))
            {
                if (heldIngredient == null && currentTarget != null)
                {
                    currentTarget.OnInteract(this);
                }
                else if (heldIngredient != null && currentTarget != null)
                {
                    // Try to place on a placement target
                    currentTarget.OnInteractWithHeld(this, heldIngredient);
                }
            }

            // Right click — drop / cancel
            if (Input.GetMouseButtonDown(1))
            {
                if (heldIngredient != null)
                {
                    DropIngredient();
                }
            }
        }

        // ──────────────────────────────────────────────
        //  HOLD / DROP
        // ──────────────────────────────────────────────

        public void PickUpIngredient(Ingredient ingredient)
        {
            if (heldIngredient != null) return; // already holding something

            heldIngredient = ingredient;
            ingredient.OnPickedUp();

            // Parent to hold point
            ingredient.transform.SetParent(holdPoint);
            ingredient.transform.localPosition = Vector3.zero;
            ingredient.transform.localRotation = Quaternion.identity;

            // Disable physics while held
            var rb = ingredient.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            var col = ingredient.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log($"[Interaction] Picked up: {ingredient.ingredientType}");
        }

        public void DropIngredient()
        {
            if (heldIngredient == null) return;

            heldIngredient.transform.SetParent(null);
            heldIngredient.OnDropped();

            // Re-enable physics
            var rb = heldIngredient.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
                rb.AddForce(cam.transform.forward * 1.5f, ForceMode.Impulse);
            }

            var col = heldIngredient.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Debug.Log($"[Interaction] Dropped: {heldIngredient.ingredientType}");
            heldIngredient = null;
        }

        public void ClearHeldIngredient()
        {
            heldIngredient = null;
        }

        public bool IsHolding()
        {
            return heldIngredient != null;
        }

        public Ingredient GetHeldIngredient()
        {
            return heldIngredient;
        }

        // ──────────────────────────────────────────────
        //  HIGHLIGHT
        // ──────────────────────────────────────────────

        private void SetHighlight(GameObject obj, bool on)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return;

            if (on)
            {
                // Store original and apply highlight via emission
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", highlightColor * 0.3f);
            }
            else
            {
                renderer.material.DisableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }

        private void ClearHighlight()
        {
            if (highlightedObject != null)
            {
                SetHighlight(highlightedObject, false);
            }
        }
    }

    // ──────────────────────────────────────────────
    //  INTERACTABLE INTERFACE
    // ──────────────────────────────────────────────

    public interface IInteractable
    {
        void OnInteract(PlayerInteraction player);
        void OnInteractWithHeld(PlayerInteraction player, Ingredient ingredient);
        string GetInteractPrompt();
    }
}
