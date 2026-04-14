// PlayerHands.cs — Two-hand interaction system
// Attach to the Camera alongside FirstPersonLook and CameraEffects.
//
// CONTROLS:
//   LMB  — Left hand / plate action. Quick-add toppings/salsas if holding one.
//   RMB  — Right hand. Grab from station. Place on grill. Drop if no target.
//   MMB  — Combine right-hand ingredient onto plate.
//   Q    — Clear plate (start over). Useful when you have wrong ingredients.
//   E    — Drop right-hand ingredient without throwing.

using System.Collections;
using UnityEngine;

namespace TacoTornado.Player
{
    public class PlayerHands : MonoBehaviour
    {
        [Header("Hold Points")]
        [SerializeField] private Transform leftHoldPoint;
        [SerializeField] private Transform rightHoldPoint;

        [Header("Interaction")]
        [SerializeField] private float     interactRange = GameConstants.INTERACT_RANGE;
        [SerializeField] private LayerMask interactLayer;

        [Header("Plate Prefab")]
        [SerializeField] private GameObject platePrefab;

        [Header("Visual Feedback")]
        [SerializeField] private Color highlightTint = new Color(1.5f, 1.5f, 0.5f);

        [Header("Combine Animation")]
        [SerializeField] private float combineAnimDuration = GameConstants.COMBINE_ANIM_DURATION;

        // ── State ─────────────────────────────────────────────────────────────
        private PlateInHand   plate;
        private Ingredient    rightHandIngredient;
        private IInteractable currentTarget;
        private GameObject    highlightedObject;
        private Color         savedColor;
        private bool          hasSavedColor;
        private bool          isCombining = false;
        private Camera        cam;

        // ── Init ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnShiftStarted += SpawnPlate;
            else
                Debug.LogError("[Hands] GameManager.Instance is null in Start!");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnShiftStarted -= SpawnPlate;
        }

        private void SpawnPlate()
        {
            if (plate != null) Destroy(plate.gameObject);
            if (platePrefab == null)   { Debug.LogError("[Hands] platePrefab not assigned!"); return; }
            if (leftHoldPoint == null) { Debug.LogError("[Hands] leftHoldPoint not assigned!"); return; }

            GameObject go = Instantiate(platePrefab, leftHoldPoint.position, leftHoldPoint.rotation);
            plate = go.GetComponent<PlateInHand>();
            if (plate == null) plate = go.AddComponent<PlateInHand>();

            go.transform.SetParent(leftHoldPoint);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Debug.Log("[Hands] Plate spawned in left hand.");
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isShiftActive) return;
            if (isCombining) return;
            HandleRaycast();
            HandleInput();
            UpdateRightHandFollow();
        }

        // ── Raycast ───────────────────────────────────────────────────────────

        private void HandleRaycast()
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>()
                                ?? hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    if (currentTarget != interactable)
                    {
                        ClearHighlight();
                        currentTarget     = interactable;
                        highlightedObject = hit.collider.gameObject;
                        SetHighlight(highlightedObject, true);
                    }
                    if (UI.GameHUD.Instance != null)
                        UI.GameHUD.Instance.ShowInteractPrompt(currentTarget.GetInteractPrompt(this));
                    return;
                }
            }

            if (currentTarget != null)
            {
                ClearHighlight();
                currentTarget     = null;
                highlightedObject = null;
                if (UI.GameHUD.Instance != null)
                    UI.GameHUD.Instance.HideInteractPrompt();
            }
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void HandleInput()
        {
            bool lmb = Input.GetMouseButtonDown(0);
            bool rmb = Input.GetMouseButtonDown(1);
            bool mmb = Input.GetMouseButtonDown(2);
            bool q   = Input.GetKeyDown(KeyCode.Q);
            bool e   = Input.GetKeyDown(KeyCode.E);

            // Q — clear the plate (start over)
            if (q && plate != null && plate.IngredientCount > 0)
            {
                plate.ClearIngredients();
                if (CameraEffects.Instance != null) CameraEffects.Instance.Shake(0.06f, 0.15f);
                if (UI.GameHUD.Instance != null) UI.GameHUD.Instance.ShowInteractPrompt("Plate cleared! Start over.");
                Debug.Log("[Hands] Plate cleared with Q key.");
                return;
            }

            // E — drop right-hand ingredient gently (no impulse)
            if (e && rightHandIngredient != null)
            {
                rightHandIngredient.transform.SetParent(null);
                rightHandIngredient.OnDropped();
                // Small drop, no forward impulse
                var rb = rightHandIngredient.GetComponent<Rigidbody>();
                if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; }
                var col = rightHandIngredient.GetComponent<Collider>();
                if (col != null) col.enabled = true;
                Debug.Log($"[Hands] Dropped (E): {rightHandIngredient.ingredientType}");
                rightHandIngredient = null;
                return;
            }

            // RMB — right hand
            if (rmb)
            {
                if (rightHandIngredient == null)
                    { if (currentTarget != null) currentTarget.OnRightHandInteract(this); }
                else
                    { if (currentTarget != null) currentTarget.OnRightHandInteractWithHeld(this, rightHandIngredient); else DropRightHand(); }
            }

            // LMB — left hand / plate
            if (lmb)
            {
                // Auto-shortcut: toppings/salsas in right hand → combine directly onto plate
                if (rightHandIngredient != null)
                {
                    var cat = IngredientData.GetCategory(rightHandIngredient.ingredientType);
                    if (cat == IngredientCategory.Topping || cat == IngredientCategory.Salsa)
                        { TryCombine(); return; }
                }
                if (currentTarget != null) currentTarget.OnLeftHandInteract(this);
            }

            // MMB — manual combine
            if (mmb) TryCombine();
        }

        // ── Combine ───────────────────────────────────────────────────────────

        public void TryCombine()
        {
            if (plate == null || rightHandIngredient == null || isCombining) return;

            string reason;
            if (!plate.CanAddIngredient(rightHandIngredient, out reason))
            {
                Debug.Log($"[Hands] Can't combine: {reason}");
                if (UI.GameHUD.Instance != null) UI.GameHUD.Instance.ShowInteractPrompt(reason);
                if (CameraEffects.Instance != null) CameraEffects.Instance.Shake(0.04f, 0.1f);
                return;
            }
            StartCoroutine(CombineAnimation(rightHandIngredient));
        }

        private IEnumerator CombineAnimation(Ingredient ingredient)
        {
            isCombining = true;
            ingredient.transform.SetParent(null);

            Vector3 startPos   = ingredient.transform.position;
            Vector3 startScale = ingredient.transform.localScale;
            float   elapsed    = 0f;

            while (elapsed < combineAnimDuration)
            {
                float t    = elapsed / combineAnimDuration;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 arc = Vector3.up * 0.1f * Mathf.Sin(t * Mathf.PI);
                ingredient.transform.position   = Vector3.Lerp(startPos, leftHoldPoint.position, ease) + arc;
                ingredient.transform.localScale = Vector3.Lerp(startScale, startScale * 0.5f, ease);
                elapsed += Time.deltaTime;
                yield return null;
            }

            plate.AddIngredient(ingredient);
            rightHandIngredient = null;
            if (CameraEffects.Instance != null) CameraEffects.Instance.PlayPickupBob();
            isCombining = false;
        }

        // ── Right Hand API ────────────────────────────────────────────────────

        public void GrabIngredient(Ingredient ingredient)
        {
            if (rightHandIngredient != null) DropRightHand();
            rightHandIngredient = ingredient;
            ingredient.OnPickedUp();
            ingredient.transform.SetParent(null);
            DisablePhysics(ingredient);
            if (CameraEffects.Instance != null) CameraEffects.Instance.PlayPickupBob();
            Debug.Log($"[Hands] Right hand grabbed: {ingredient.ingredientType}");
        }

        public void DropRightHand()
        {
            if (rightHandIngredient == null) return;
            rightHandIngredient.transform.SetParent(null);
            rightHandIngredient.OnDropped();
            EnablePhysics(rightHandIngredient, cam.transform.forward * 1.5f);
            Debug.Log($"[Hands] Right hand dropped: {rightHandIngredient.ingredientType}");
            rightHandIngredient = null;
        }

        public void ClearRightHand()              => rightHandIngredient = null;
        public bool IsRightHandEmpty()             => rightHandIngredient == null;
        public bool IsRightHandHolding()           => rightHandIngredient != null;
        public Ingredient GetRightHandIngredient() => rightHandIngredient;
        public PlateInHand GetPlate()              => plate;
        public bool        HasPlate()              => plate != null;

        public void ServePlate()
        {
            if (plate == null) return;
            var ingredients = plate.GetIngredientTypes();
            if (ingredients.Count < 2) { Debug.Log("[Hands] Need at least tortilla + protein."); return; }
            OrderManager.Instance?.TrySubmitTaco(ingredients);
            plate.ClearIngredients();
            if (CameraEffects.Instance != null) CameraEffects.Instance.PlayServePulse();
            Debug.Log("[Hands] Plate served!");
        }

        // ── Smooth Right Hand Follow ──────────────────────────────────────────

        private Vector3 rhVelocity;

        private void UpdateRightHandFollow()
        {
            if (rightHandIngredient == null || rightHoldPoint == null) return;
            float swayX = Mathf.Sin(Time.time * 2.2f)        * 0.005f;
            float swayY = Mathf.Sin(Time.time * 1.6f + 1.1f) * 0.003f;
            Vector3 target = rightHoldPoint.position + cam.transform.right * swayX + cam.transform.up * swayY;
            rightHandIngredient.transform.position = Vector3.SmoothDamp(
                rightHandIngredient.transform.position, target, ref rhVelocity, 0.055f);
            rightHandIngredient.transform.rotation = Quaternion.Slerp(
                rightHandIngredient.transform.rotation, rightHoldPoint.rotation, Time.deltaTime * 20f);
        }

        // ── Physics ───────────────────────────────────────────────────────────

        private void DisablePhysics(Ingredient ing)
        {
            var rb = ing.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
            var col = ing.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private void EnablePhysics(Ingredient ing, Vector3 impulse)
        {
            var rb = ing.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; rb.AddForce(impulse, ForceMode.Impulse); }
            var col = ing.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        // ── Highlight ─────────────────────────────────────────────────────────

        private Renderer FindRenderer(GameObject obj)
        {
            Renderer r = obj.GetComponent<Renderer>();
            if (r != null) return r;
            r = obj.GetComponentInChildren<Renderer>();
            if (r != null) return r;
            if (obj.transform.parent != null)
                r = obj.transform.parent.GetComponentInChildren<Renderer>();
            return r;
        }

        private void SetHighlight(GameObject obj, bool on)
        {
            Renderer rend = FindRenderer(obj);
            if (rend == null) return;

            if (on)
            {
                savedColor    = rend.material.color;
                hasSavedColor = true;
                Color bright = new Color(
                    Mathf.Min(savedColor.r + 0.35f, 1f),
                    Mathf.Min(savedColor.g + 0.35f, 1f),
                    Mathf.Max(savedColor.b - 0.1f,  0f),
                    savedColor.a);
                rend.material.color = bright;
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.EnableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", Color.yellow * 0.2f);
                }
            }
            else
            {
                if (hasSavedColor) { rend.material.color = savedColor; hasSavedColor = false; }
                if (rend.material.HasProperty("_EmissionColor"))
                {
                    rend.material.DisableKeyword("_EMISSION");
                    rend.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        private void ClearHighlight()
        {
            if (highlightedObject != null) SetHighlight(highlightedObject, false);
        }
    }

    // ── IInteractable ─────────────────────────────────────────────────────────

    public interface IInteractable
    {
        void OnRightHandInteract(PlayerHands hands);
        void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient);
        void OnLeftHandInteract(PlayerHands hands);
        string GetInteractPrompt(PlayerHands hands);
    }
}