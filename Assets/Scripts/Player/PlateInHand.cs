using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado.Player
{
    public class PlateInHand : MonoBehaviour
    {
        [Header("Stack Settings")]
        [SerializeField] private float stackHeight          = 0.04f;
        [SerializeField] private float ingredientDisplaySize = 0.12f;

        private List<Ingredient>     placedIngredients = new List<Ingredient>();
        private List<IngredientType> placedTypes       = new List<IngredientType>();
        private bool hasTortilla = false;
        private bool hasProtein  = false;

        public bool HasTortilla()    => hasTortilla;
        public bool HasProtein()     => hasProtein;
        public int  IngredientCount  => placedIngredients.Count;

        public List<IngredientType> GetIngredientTypes() => new List<IngredientType>(placedTypes);

        // ── Validation ────────────────────────────────────────────────────────

        public bool CanAddIngredient(Ingredient ingredient, out string reason)
        {
            var cat  = IngredientData.GetCategory(ingredient.ingredientType);
            var type = ingredient.ingredientType;

            if (!hasTortilla && cat != IngredientCategory.Tortilla)
                { reason = "Add a tortilla first!"; return false; }

            if (hasTortilla && cat == IngredientCategory.Tortilla)
                { reason = "Already have a tortilla!"; return false; }

            if (hasProtein && cat == IngredientCategory.Protein)
                { reason = "Already have a protein!"; return false; }

            if (cat == IngredientCategory.Protein)
            {
                if (ingredient.IsBurnt())   { reason = "That's burnt — trash it!"; return false; }
                if (!ingredient.IsCooked()) { reason = "Cook the meat first!";     return false; }
            }

            if (placedTypes.Contains(type))
                { reason = $"Already have {type}!"; return false; }

            reason = "";
            return true;
        }

        // ── Add ───────────────────────────────────────────────────────────────

        public void AddIngredient(Ingredient ingredient)
        {
            var cat = IngredientData.GetCategory(ingredient.ingredientType);

            placedIngredients.Add(ingredient);
            placedTypes.Add(ingredient.ingredientType);

            if (cat == IngredientCategory.Tortilla) hasTortilla = true;
            if (cat == IngredientCategory.Protein)  hasProtein  = true;

            // ── SCALE FIX ──────────────────────────────────────────────────────
            // We must set world scale BEFORE parenting.
            // After parenting, Unity applies the parent's lossyScale which stretches the ingredient.
            // So: detach → set world scale to desired size → then parent.

            ingredient.transform.SetParent(null); // detach from anything first

            // Calculate the world-space size we want
            float desiredWorldSize = ingredientDisplaySize;

            // Apply as world scale (before parenting)
            ingredient.transform.localScale = Vector3.one * desiredWorldSize;

            // Now parent to plate
            ingredient.transform.SetParent(transform);

            // Stack position in local space of the plate
            int index = placedIngredients.Count - 1;
            ingredient.transform.localPosition = new Vector3(0f, stackHeight * (index + 1), 0f);
            ingredient.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // After parenting, correct the scale to compensate for plate's lossyScale
            // This ensures the ingredient appears at the right world size regardless of plate scale
            Vector3 plateScale = transform.lossyScale;
            ingredient.transform.localScale = new Vector3(
                desiredWorldSize / Mathf.Max(plateScale.x, 0.001f),
                desiredWorldSize / Mathf.Max(plateScale.y, 0.001f),
                desiredWorldSize / Mathf.Max(plateScale.z, 0.001f));

            // Freeze physics
            var rb = ingredient.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }
            var col = ingredient.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log($"[Plate] Added {ingredient.ingredientType} (stack: {placedIngredients.Count})");
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        public void ClearIngredients()
        {
            foreach (var ing in placedIngredients)
                if (ing != null) Destroy(ing.gameObject);

            placedIngredients.Clear();
            placedTypes.Clear();
            hasTortilla = false;
            hasProtein  = false;
            Debug.Log("[Plate] Cleared.");
        }

        // ── Status ────────────────────────────────────────────────────────────

        public string GetStatusText()
        {
            if (!hasTortilla) return "Plate — add tortilla first";
            if (!hasProtein)  return "Plate — add cooked protein";
            return $"Plate ready ({placedIngredients.Count} ingredients) — serve at window";
        }
    }
}