// PlateInHand.cs — The customer plate that always lives in the player's left hand.
// Attach to the plate prefab (a flat disc mesh with Renderer).
// PlayerHands instantiates and manages this. Do NOT place in scene manually.
//
// ASSEMBLY RULES enforced here:
//   1. Tortilla must be first.
//   2. Only one tortilla.
//   3. Only one protein, and it must be cooked (not raw, not burnt).
//   4. Max one of each topping/salsa type.

using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado.Player
{
    public class PlateInHand : MonoBehaviour
    {
        [Header("Stack Settings")]
        [Tooltip("How high each ingredient sits above the previous one on the plate.")]
        [SerializeField] private float stackHeight = 0.025f;

        [Tooltip("World size ingredients should appear at when placed on the plate.")]
        [SerializeField] private float ingredientDisplaySize = 0.07f;

        // Internal state
        private List<Ingredient>     placedIngredients = new List<Ingredient>();
        private List<IngredientType> placedTypes       = new List<IngredientType>();

        private bool hasTortilla = false;
        private bool hasProtein  = false;

        // ── Query API ─────────────────────────────────────────────────────────

        public bool HasTortilla() => hasTortilla;
        public bool HasProtein()  => hasProtein;
        public int  IngredientCount => placedIngredients.Count;

        public List<IngredientType> GetIngredientTypes()
        {
            return new List<IngredientType>(placedTypes);
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the ingredient can be added. Sets reason string on failure.
        /// </summary>
        public bool CanAddIngredient(Ingredient ingredient, out string reason)
        {
            var cat  = IngredientData.GetCategory(ingredient.ingredientType);
            var type = ingredient.ingredientType;

            // Tortilla must be first
            if (!hasTortilla && cat != IngredientCategory.Tortilla)
            {
                reason = "Add a tortilla first!";
                return false;
            }

            // No second tortilla
            if (hasTortilla && cat == IngredientCategory.Tortilla)
            {
                reason = "Already have a tortilla!";
                return false;
            }

            // No second protein
            if (hasProtein && cat == IngredientCategory.Protein)
            {
                reason = "Already have a protein!";
                return false;
            }

            // Protein must be cooked
            if (cat == IngredientCategory.Protein)
            {
                if (ingredient.IsBurnt())  { reason = "That's burnt — trash it!"; return false; }
                if (!ingredient.IsCooked()) { reason = "Cook the meat first!";     return false; }
            }

            // No duplicate toppings/salsas
            if (placedTypes.Contains(type))
            {
                reason = $"Already have {type}!";
                return false;
            }

            reason = "";
            return true;
        }

        // ── Add Ingredient ────────────────────────────────────────────────────

        /// <summary>Physically place ingredient on the plate stack.</summary>
        public void AddIngredient(Ingredient ingredient)
        {
            var cat = IngredientData.GetCategory(ingredient.ingredientType);

            placedIngredients.Add(ingredient);
            placedTypes.Add(ingredient.ingredientType);

            if (cat == IngredientCategory.Tortilla) hasTortilla = true;
            if (cat == IngredientCategory.Protein)  hasProtein  = true;

            // Position: stack upward on the plate
            int index = placedIngredients.Count - 1;
            float yOffset = stackHeight * (index + 1);

            // Set world scale BEFORE parenting to avoid inherited-scale explosion
            ingredient.transform.localScale = Vector3.one * ingredientDisplaySize;

            // Parent to plate
            ingredient.transform.SetParent(transform);

            // Local position: centered on plate, stacked up
            ingredient.transform.localPosition = new Vector3(0f, yOffset, 0f);
            ingredient.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Freeze
            var rb = ingredient.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
            var col = ingredient.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log($"[Plate] Added {ingredient.ingredientType} (stack: {placedIngredients.Count})");
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        /// <summary>Destroy all placed ingredient objects and reset state.</summary>
        public void ClearIngredients()
        {
            foreach (var ing in placedIngredients)
            {
                if (ing != null) Destroy(ing.gameObject);
            }
            placedIngredients.Clear();
            placedTypes.Clear();
            hasTortilla = false;
            hasProtein  = false;
            Debug.Log("[Plate] Cleared.");
        }

        // ── Prompt helper ─────────────────────────────────────────────────────

        public string GetStatusText()
        {
            if (!hasTortilla) return "Plate — add tortilla first";
            if (!hasProtein)  return "Plate — add cooked protein";
            return $"Plate ready ({placedIngredients.Count} ingredients) — serve at window";
        }
    }
}
