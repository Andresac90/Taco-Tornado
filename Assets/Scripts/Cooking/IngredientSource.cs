// IngredientSource.cs — Station that dispenses one ingredient type
// Updated for two-hand interface.
//
// HOW TO SET UP PER STATION:
//   1. Create a GO on the counter, add this component.
//   2. Assign ingredientType and ingredientPrefab.
//   3. Create a child GO with a mesh (colored cube/sphere) → assign to displayModel.
//      This is what's visible before the player grabs anything.
//   4. Set Layer to Interactable (Layer 8) on the station's collider.

using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class IngredientSource : MonoBehaviour, IInteractable
    {
        [Header("Configuration")]
        public IngredientType ingredientType;
        public GameObject     ingredientPrefab;

        [Header("Display (visible at rest)")]
        [Tooltip("Child mesh that shows what ingredient this station holds. Always visible.")]
        [SerializeField] private GameObject displayModel;
        [Tooltip("Material to swap to when out of stock. If null, hides displayModel instead.")]
        [SerializeField] private Material   outOfStockMaterial;

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;

        // ── Internal ──────────────────────────────────────────────────────────

        private Vector3 displayRestPos;
        private bool    outOfStock;

        private void Start()
        {
            if (displayModel != null)
                displayRestPos = displayModel.transform.localPosition;
        }

        private void Update()
        {
            if (displayModel == null || outOfStock) return;

            // Gentle idle bob + rotation
            float bob = Mathf.Sin(Time.time * 1.8f + transform.position.x) * 0.01f;
            displayModel.transform.localPosition = displayRestPos + Vector3.up * bob;
            displayModel.transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.Self);
        }

        // ── IInteractable ─────────────────────────────────────────────────────

        public void OnRightHandInteract(PlayerHands hands)
        {
            // RMB with empty right hand → grab ingredient
            if (!hands.IsRightHandEmpty())
            {
                Debug.Log("[Source] Right hand already holding something.");
                return;
            }

            if (GameManager.Instance == null || !GameManager.Instance.ConsumeIngredient(ingredientType))
            {
                Debug.Log($"[Source] Out of {ingredientType}!");
                SetOutOfStockVisual(true);
                return;
            }

            bool nowEmpty = GameManager.Instance.GetStock(ingredientType) == 0;
            SetOutOfStockVisual(nowEmpty);

            if (ingredientPrefab == null)
            {
                Debug.LogError($"[Source] No prefab assigned for {ingredientType}!");
                return;
            }

            Vector3 pos = spawnPoint != null
                ? spawnPoint.position
                : transform.position + Vector3.up * 0.3f;

            GameObject obj = Instantiate(ingredientPrefab, pos, Quaternion.identity);
            obj.SetActive(true);

            Ingredient ing = obj.GetComponent<Ingredient>() ?? obj.AddComponent<Ingredient>();
            ing.ingredientType = ingredientType;

            hands.GrabIngredient(ing);
        }

        public void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient)
        {
            // Can't place an ingredient back into a source
        }

        public void OnLeftHandInteract(PlayerHands hands)
        {
            // LMB (plate hand) on a source: auto-grab AND auto-combine if rules allow
            // This only works for non-protein ingredients (toppings/salsas/tortillas)
            var cat = IngredientData.GetCategory(ingredientType);
            if (cat == IngredientCategory.Protein)
            {
                Debug.Log("[Source] Proteins must be cooked — grab with RMB then grill.");
                return;
            }

            if (!hands.HasPlate())
            {
                Debug.Log("[Source] No plate in left hand.");
                return;
            }

            // Consume stock
            if (GameManager.Instance == null || !GameManager.Instance.ConsumeIngredient(ingredientType))
            {
                Debug.Log($"[Source] Out of {ingredientType}!");
                SetOutOfStockVisual(true);
                return;
            }

            bool nowEmpty = GameManager.Instance.GetStock(ingredientType) == 0;
            SetOutOfStockVisual(nowEmpty);

            if (ingredientPrefab == null) return;

            Vector3 pos = spawnPoint != null
                ? spawnPoint.position
                : transform.position + Vector3.up * 0.3f;

            GameObject obj = Instantiate(ingredientPrefab, pos, Quaternion.identity);
            obj.SetActive(true);

            Ingredient ing = obj.GetComponent<Ingredient>() ?? obj.AddComponent<Ingredient>();
            ing.ingredientType = ingredientType;

            // Try to add directly to plate
            string reason;
            if (hands.GetPlate().CanAddIngredient(ing, out reason))
            {
                hands.GetPlate().AddIngredient(ing);
                Debug.Log($"[Source] LMB shortcut — added {ingredientType} directly to plate.");
            }
            else
            {
                // Can't add — put in right hand instead for manual combine
                hands.GrabIngredient(ing);
                Debug.Log($"[Source] Can't auto-add ({reason}), putting in right hand.");
            }
        }

        public string GetInteractPrompt(PlayerHands hands)
        {
            if (GameManager.Instance == null) return $"[RMB] Grab {ingredientType}";

            int stock = GameManager.Instance.GetStock(ingredientType);
            if (stock == 0) return $"{ingredientType} — OUT OF STOCK";

            var cat = IngredientData.GetCategory(ingredientType);
            if (cat == IngredientCategory.Protein)
                return $"[RMB] Grab {ingredientType} ({stock} left) — then grill it";

            return $"[RMB] Grab  [LMB] Quick-add to plate — {ingredientType} ({stock} left)";
        }

        // ── Out of Stock Visual ───────────────────────────────────────────────

        private void SetOutOfStockVisual(bool empty)
        {
            if (outOfStock == empty) return;
            outOfStock = empty;
            if (displayModel == null) return;

            if (empty)
            {
                if (outOfStockMaterial != null)
                {
                    var r = displayModel.GetComponent<Renderer>();
                    if (r != null) r.material = outOfStockMaterial;
                }
                else
                {
                    displayModel.SetActive(false);
                }
            }
            else
            {
                displayModel.SetActive(true);
            }
        }
    }
}
