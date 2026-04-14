// PlayerHands.cs — Two-hand interaction system
// Replaces PlayerInteraction.cs entirely.
// Attach to the Camera alongside FirstPersonLook and CameraEffects.
//
// ── HAND SYSTEM ──────────────────────────────────────────────────────────────
//
//  LEFT HAND  (LMB)  → Holds the Customer Plate. Always in hand at shift start.
//                       LMB on a station: auto-add ingredient to plate if rules allow.
//                       LMB on grill: pick up cooked protein onto plate directly.
//
//  RIGHT HAND (RMB)  → Free hand. RMB on a station: grab raw ingredient.
//                       RMB on grill while holding protein: place on grill.
//                       RMB again while holding: drop ingredient.
//
//  COMBINE    (MMB, or LMB+RMB simultaneously)
//             → Combine right-hand ingredient onto left-hand plate.
//             → Plays a quick animation: ingredient slides from right hold point to plate.
//
//  AUTO-SHORTCUT:
//             When right hand holds a Topping or Salsa and player presses LMB aimed at
//             the plate (or nowhere), it auto-combines — no need to press MMB.
//             Proteins still require explicit combine (they must be cooked first).
//
// ── PLATE RULES ──────────────────────────────────────────────────────────────
//  1. Tortilla must go on first.
//  2. Protein must be cooked (not raw, not burnt).
//  3. Serve: LMB on the Customer Window (PlateHandOffZone) while plate has 2+ ingredients.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado.Player
{
    public class PlayerHands : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Hold Points")]
        [Tooltip("Where the plate sits — slightly left of screen center.")]
        [SerializeField] private Transform leftHoldPoint;
        [Tooltip("Where a grabbed ingredient sits — slightly right of screen center.")]
        [SerializeField] private Transform rightHoldPoint;

        [Header("Interaction")]
        [SerializeField] private float       interactRange  = GameConstants.INTERACT_RANGE;
        [SerializeField] private LayerMask   interactLayer;

        [Header("Plate Prefab")]
        [Tooltip("The plate GameObject that spawns in the left hand at shift start.")]
        [SerializeField] private GameObject platePrefab;

        [Header("Visual Feedback")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.2f);

        [Header("Combine Animation")]
        [SerializeField] private float combineAnimDuration = GameConstants.COMBINE_ANIM_DURATION;

        // ── State ─────────────────────────────────────────────────────────────

        // Left hand — always holds the plate
        private PlateInHand plate;

        // Right hand — holds a raw ingredient (or cooked protein waiting to be plated)
        private Ingredient rightHandIngredient;

        // Raycast target tracking
        private IInteractable currentTarget;
        private GameObject    highlightedObject;

        // Combine animation lock
        private bool isCombining = false;

        private Camera cam;

        // ── Init ──────────────────────────────────────────────────────────────

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnShiftStarted += SpawnPlate;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnShiftStarted -= SpawnPlate;
        }

        private void SpawnPlate()
        {
            // Destroy any old plate
            if (plate != null) Destroy(plate.gameObject);

            if (platePrefab == null)
            {
                Debug.LogError("[Hands] platePrefab not assigned!");
                return;
            }

            GameObject go = Instantiate(platePrefab, leftHoldPoint.position, leftHoldPoint.rotation);
            plate = go.GetComponent<PlateInHand>();
            if (plate == null) plate = go.AddComponent<PlateInHand>();

            AttachToHold(go.transform, leftHoldPoint);
            Debug.Log("[Hands] Plate spawned in left hand.");
        }

        // ── Update ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isShiftActive) return;
            if (isCombining) return; // lock input during animation

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

            // ── RIGHT HAND (RMB) ──────────────────────────────────────────────
            if (rmb)
            {
                if (rightHandIngredient == null)
                {
                    // Grab from station
                    if (currentTarget != null)
                        currentTarget.OnRightHandInteract(this);
                }
                else
                {
                    // Holding something — try to place on grill or drop
                    if (currentTarget != null)
                        currentTarget.OnRightHandInteractWithHeld(this, rightHandIngredient);
                    else
                        DropRightHand();
                }
            }

            // ── LEFT HAND / PLATE (LMB) ───────────────────────────────────────
            if (lmb)
            {
                // Auto-shortcut: if holding a topping or salsa, combine it onto plate
                if (rightHandIngredient != null)
                {
                    var cat = IngredientData.GetCategory(rightHandIngredient.ingredientType);
                    if (cat == IngredientCategory.Topping || cat == IngredientCategory.Salsa)
                    {
                        TryCombine();
                        return;
                    }
                }

                // Otherwise, left-hand interact with world target
                if (currentTarget != null)
                    currentTarget.OnLeftHandInteract(this);
            }

            // ── COMBINE (MMB or LMB+RMB held) ────────────────────────────────
            if (mmb)
            {
                TryCombine();
            }
        }

        // ── Combine ───────────────────────────────────────────────────────────

        public void TryCombine()
        {
            if (plate == null || rightHandIngredient == null) return;
            if (isCombining) return;

            string reason;
            if (!plate.CanAddIngredient(rightHandIngredient, out reason))
            {
                Debug.Log($"[Hands] Can't combine: {reason}");

                // Flash feedback
                if (UI.GameHUD.Instance != null)
                    UI.GameHUD.Instance.ShowInteractPrompt(reason);

                if (CameraEffects.Instance != null)
                    CameraEffects.Instance.Shake(0.04f, 0.1f);

                return;
            }

            StartCoroutine(CombineAnimation(rightHandIngredient));
        }

        private IEnumerator CombineAnimation(Ingredient ingredient)
        {
            isCombining = true;

            // Detach from right hold point — animate toward left hold point / plate
            ingredient.transform.SetParent(null);

            Vector3 startPos = ingredient.transform.position;
            Vector3 startScale = ingredient.transform.localScale;

            float elapsed = 0f;

            while (elapsed < combineAnimDuration)
            {
                float t = elapsed / combineAnimDuration;
                float ease = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

                // Arc toward the plate
                Vector3 target = leftHoldPoint.position;
                Vector3 arc    = Vector3.up * 0.08f * Mathf.Sin(t * Mathf.PI);

                ingredient.transform.position   = Vector3.Lerp(startPos, target, ease) + arc;
                ingredient.transform.localScale = Vector3.Lerp(startScale, startScale * 0.6f, ease);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Commit to plate
            plate.AddIngredient(ingredient);
            rightHandIngredient = null;

            if (CameraEffects.Instance != null)
                CameraEffects.Instance.PlayPickupBob();

            isCombining = false;
        }

        // ── Right Hand API ────────────────────────────────────────────────────

        /// <summary>Called by stations to hand an ingredient to the right hand.</summary>
        public void GrabIngredient(Ingredient ingredient)
        {
            if (rightHandIngredient != null)
            {
                // Already holding — drop current first
                DropRightHand();
            }

            rightHandIngredient = ingredient;
            ingredient.OnPickedUp();

            // Detach from world
            ingredient.transform.SetParent(null);

            DisablePhysics(ingredient);

            if (CameraEffects.Instance != null)
                CameraEffects.Instance.PlayPickupBob();

            Debug.Log($"[Hands] Right hand grabbed: {ingredient.ingredientType}");
        }

        public void DropRightHand()
        {
            if (rightHandIngredient == null) return;

            rightHandIngredient.transform.SetParent(null);
            rightHandIngredient.OnDropped();
            EnablePhysics(rightHandIngredient, cam.transform.forward * 1.2f);

            Debug.Log($"[Hands] Right hand dropped: {rightHandIngredient.ingredientType}");
            rightHandIngredient = null;
        }

        public void ClearRightHand()
        {
            // Used by grill etc. when it takes ownership
            rightHandIngredient = null;
        }

        public bool IsRightHandEmpty()   => rightHandIngredient == null;
        public bool IsRightHandHolding() => rightHandIngredient != null;
        public Ingredient GetRightHandIngredient() => rightHandIngredient;

        // ── Left Hand / Plate API ─────────────────────────────────────────────

        public PlateInHand GetPlate() => plate;
        public bool HasPlate() => plate != null;

        /// <summary>
        /// Serve the plate: pass all ingredients to OrderManager, then clear & rebuild plate.
        /// </summary>
        public void ServePlate()
        {
            if (plate == null) return;

            var ingredients = plate.GetIngredientTypes();
            if (ingredients.Count < 2)
            {
                Debug.Log("[Hands] Need at least tortilla + protein to serve.");
                return;
            }

            OrderManager orderManager = FindFirstObjectByType<OrderManager>();
            if (orderManager != null)
                orderManager.TrySubmitTaco(ingredients);

            plate.ClearIngredients();

            if (CameraEffects.Instance != null)
                CameraEffects.Instance.PlayServePulse();

            Debug.Log("[Hands] Plate served!");
        }

        // ── Smooth Right Hand Follow ──────────────────────────────────────────

        private Vector3 rhVelocity;

        private void UpdateRightHandFollow()
        {
            if (rightHandIngredient == null || rightHoldPoint == null) return;

            float swayX = Mathf.Sin(Time.time * 2.2f)        * 0.005f;
            float swayY = Mathf.Sin(Time.time * 1.6f + 1.1f) * 0.003f;
            Vector3 target = rightHoldPoint.position
                + cam.transform.right * swayX
                + cam.transform.up    * swayY;

            rightHandIngredient.transform.position = Vector3.SmoothDamp(
                rightHandIngredient.transform.position, target, ref rhVelocity, 0.055f);

            rightHandIngredient.transform.rotation = Quaternion.Slerp(
                rightHandIngredient.transform.rotation, rightHoldPoint.rotation, Time.deltaTime * 20f);
        }

        // ── Physics Helpers ───────────────────────────────────────────────────

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
            if (rb != null)
            {
                rb.isKinematic      = false;
                rb.detectCollisions = true;
                rb.AddForce(impulse, ForceMode.Impulse);
            }
            var col = ing.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        private void AttachToHold(Transform obj, Transform holdPoint)
        {
            obj.SetParent(holdPoint);
            obj.localPosition = Vector3.zero;
            obj.localRotation = Quaternion.identity;
        }

        // ── Highlight ─────────────────────────────────────────────────────────

        private void SetHighlight(GameObject obj, bool on)
        {
            Renderer rend = obj.GetComponent<Renderer>() ?? obj.GetComponentInChildren<Renderer>();
            if (rend == null) return;

            if (on)
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", highlightColor * 0.35f);
            }
            else
            {
                rend.material.DisableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", Color.black);
            }
        }

        private void ClearHighlight()
        {
            if (highlightedObject != null) SetHighlight(highlightedObject, false);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  UPDATED INTERACTABLE INTERFACE — two-hand aware
    // ──────────────────────────────────────────────────────────────────────────

    public interface IInteractable
    {
        /// <summary>RMB with empty right hand — grab ingredient.</summary>
        void OnRightHandInteract(PlayerHands hands);

        /// <summary>RMB while holding ingredient — place on this station (e.g. grill).</summary>
        void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient);

        /// <summary>LMB — left-hand / plate action (e.g. serve at window, auto-add topping).</summary>
        void OnLeftHandInteract(PlayerHands hands);

        /// <summary>Prompt text shown to player when aiming at this object.</summary>
        string GetInteractPrompt(PlayerHands hands);
    }
}
