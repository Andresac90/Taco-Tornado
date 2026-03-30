// Ingredient.cs — A single ingredient object in the world
// Attach to each ingredient prefab (cube/sphere placeholder with Rigidbody + Collider)

using UnityEngine;

namespace TacoTornado
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Ingredient : MonoBehaviour
    {
        [Header("Ingredient Info")]
        public IngredientType ingredientType;
        public CookState cookState = CookState.Raw;

        [Header("Cooking (if applicable)")]
        public float cookProgress = 0f;
        public bool isOnGrill = false;

        private bool isHeld = false;
        private float cookTime;
        private float burnTime;
        private Renderer rend;

        // Colors for visual cook state (placeholder — replace with proper materials)
        private static readonly Color RAW_COLOR = new Color(0.9f, 0.4f, 0.4f);
        private static readonly Color COOKING_COLOR = new Color(0.9f, 0.6f, 0.2f);
        private static readonly Color COOKED_COLOR = new Color(0.6f, 0.35f, 0.15f);
        private static readonly Color BURNT_COLOR = new Color(0.15f, 0.1f, 0.1f);

        private void Awake()
        {
            rend = GetComponent<Renderer>();
            cookTime = GameConstants.DEFAULT_COOK_TIME;
            burnTime = GameConstants.DEFAULT_BURN_TIME;

            bool needsCook = IngredientData.GetCategory(ingredientType) == IngredientCategory.Protein;
            if (!needsCook)
            {
                cookState = CookState.Cooked; // toppings/salsas/tortillas are ready
            }
            UpdateVisual();
        }

        private void Update()
        {
            if (!isOnGrill || isHeld) return;
            if (cookState == CookState.Burnt) return;

            cookProgress += Time.deltaTime;

            if (cookState == CookState.Raw && cookProgress >= cookTime * 0.5f)
            {
                cookState = CookState.Cooking;
                UpdateVisual();
            }
            else if (cookState == CookState.Cooking && cookProgress >= cookTime)
            {
                cookState = CookState.Cooked;
                UpdateVisual();
                Debug.Log($"[Ingredient] {ingredientType} is cooked!");
            }
            else if (cookState == CookState.Cooked && cookProgress >= cookTime + burnTime)
            {
                cookState = CookState.Burnt;
                UpdateVisual();
                Debug.Log($"[Ingredient] {ingredientType} is BURNT!");
            }
        }

        public void PlaceOnGrill()
        {
            isOnGrill = true;
            cookProgress = 0f;
            cookState = CookState.Raw;
            UpdateVisual();
        }

        public void RemoveFromGrill()
        {
            isOnGrill = false;
        }

        public void OnPickedUp()
        {
            isHeld = true;
            if (isOnGrill) RemoveFromGrill();
        }

        public void OnDropped()
        {
            isHeld = false;
        }

        public bool IsCooked()
        {
            // Non-cookable items are always "cooked"
            if (IngredientData.GetCategory(ingredientType) != IngredientCategory.Protein)
                return true;
            return cookState == CookState.Cooked;
        }

        public bool IsBurnt()
        {
            return cookState == CookState.Burnt;
        }

        private void UpdateVisual()
        {
            if (rend == null) return;

            switch (cookState)
            {
                case CookState.Raw:     rend.material.color = RAW_COLOR; break;
                case CookState.Cooking: rend.material.color = COOKING_COLOR; break;
                case CookState.Cooked:  rend.material.color = COOKED_COLOR; break;
                case CookState.Burnt:   rend.material.color = BURNT_COLOR; break;
            }
        }
    }
}
