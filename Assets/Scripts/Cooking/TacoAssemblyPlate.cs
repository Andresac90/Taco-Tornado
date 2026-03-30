// TacoAssemblyPlate.cs — The assembly area where tacos are built ingredient by ingredient
// Attach to the prep mat / plate object on the counter

using System.Collections.Generic;
using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class TacoAssemblyPlate : MonoBehaviour, IInteractable
    {
        [Header("Assembly")]
        [SerializeField] private Transform[] ingredientPositions; // stacking positions on the plate
        [SerializeField] private float stackHeight = 0.08f;

        // Current taco being assembled
        private List<Ingredient> placedIngredients = new List<Ingredient>();
        private bool hasTortilla = false;

        public void OnInteract(PlayerInteraction player)
        {
            // If player clicks the plate with empty hands and there's a built taco, serve it
            if (!player.IsHolding() && placedIngredients.Count >= 2 && hasTortilla)
            {
                ServeTaco();
            }
        }

        public void OnInteractWithHeld(PlayerInteraction player, Ingredient ingredient)
        {
            // Enforce assembly order: tortilla first
            var category = IngredientData.GetCategory(ingredient.ingredientType);

            if (!hasTortilla && category != IngredientCategory.Tortilla)
            {
                Debug.Log("[Assembly] Place a tortilla first!");
                return;
            }

            // Check for burnt meat
            if (category == IngredientCategory.Protein && ingredient.IsBurnt())
            {
                Debug.Log("[Assembly] Can't serve burnt meat!");
                return;
            }

            // Check for raw meat
            if (category == IngredientCategory.Protein && !ingredient.IsCooked())
            {
                Debug.Log("[Assembly] Meat needs to be cooked first!");
                return;
            }

            // Place the ingredient
            PlaceIngredient(ingredient);
            player.ClearHeldIngredient();

            if (category == IngredientCategory.Tortilla)
                hasTortilla = true;
        }

        public string GetInteractPrompt()
        {
            if (placedIngredients.Count == 0)
                return "Assembly Plate (empty — place tortilla)";
            if (placedIngredients.Count >= 2 && hasTortilla)
                return "Serve Taco [Click] or add more";
            return $"Assembly Plate ({placedIngredients.Count} ingredients)";
        }

        // ──────────────────────────────────────────────
        //  ASSEMBLY
        // ──────────────────────────────────────────────

        private void PlaceIngredient(Ingredient ingredient)
        {
            int index = placedIngredients.Count;
            placedIngredients.Add(ingredient);

            // Stack visually on the plate
            Vector3 pos = transform.position + Vector3.up * (stackHeight * (index + 1));
            ingredient.transform.SetParent(transform);
            ingredient.transform.position = pos;
            ingredient.transform.localRotation = Quaternion.identity;
            ingredient.transform.localScale = Vector3.one * 0.8f;

            var rb = ingredient.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            var col = ingredient.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Debug.Log($"[Assembly] Placed {ingredient.ingredientType} " +
                      $"(total: {placedIngredients.Count})");
        }

        // ──────────────────────────────────────────────
        //  SERVE
        // ──────────────────────────────────────────────

        private void ServeTaco()
        {
            // Build list of what's on the plate
            List<IngredientType> assembled = new List<IngredientType>();
            foreach (var ing in placedIngredients)
            {
                assembled.Add(ing.ingredientType);
            }

            // Pass to OrderManager for scoring
            OrderManager orderManager = FindObjectOfType<OrderManager>();
            if (orderManager != null)
            {
                orderManager.TrySubmitTaco(assembled);
            }

            // Clean up plate
            ClearPlate();
        }

        public void ClearPlate()
        {
            foreach (var ing in placedIngredients)
            {
                if (ing != null)
                    Destroy(ing.gameObject);
            }
            placedIngredients.Clear();
            hasTortilla = false;
            Debug.Log("[Assembly] Plate cleared.");
        }

        public List<IngredientType> GetCurrentIngredients()
        {
            var list = new List<IngredientType>();
            foreach (var ing in placedIngredients)
            {
                list.Add(ing.ingredientType);
            }
            return list;
        }

        public bool HasTortilla()
        {
            return hasTortilla;
        }
    }
}
