// Ingredient.cs — A single ingredient object in the world.

using UnityEngine;

namespace TacoTornado
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Ingredient : MonoBehaviour
    {
        [Header("Ingredient Info")]
        public IngredientType ingredientType;
        public CookState      cookState = CookState.Raw;

        [Header("Cooking")]
        public float cookProgress = 0f;
        public bool  isOnGrill    = false;

        [SerializeField] private Transform cookProgressBar;

        private bool     isHeld   = false;
        private bool     typeSet  = false; // tracks if type was initialized
        private float    cookTime;
        private float    burnTime;
        private Renderer rend;

        private static readonly Color RAW_COLOR     = new Color(0.88f, 0.40f, 0.35f);
        private static readonly Color COOKING_COLOR = new Color(0.92f, 0.60f, 0.20f);
        private static readonly Color COOKED_COLOR  = new Color(0.55f, 0.32f, 0.12f);
        private static readonly Color BURNT_COLOR   = new Color(0.10f, 0.07f, 0.07f);

        private void Awake()
        {
            rend     = GetComponentInChildren<Renderer>();
            cookTime = GameConstants.DEFAULT_COOK_TIME;
            burnTime = GameConstants.DEFAULT_BURN_TIME;
            // Don't initialize cookState here — wait for InitializeType()
            // which is called after ingredientType is set by the station.
        }

        /// <summary>
        /// Called by IngredientSource after setting ingredientType.
        /// Must be called before the ingredient is used.
        /// </summary>
        public void InitializeType()
        {
            typeSet = true;
            bool isProtein = IngredientData.GetCategory(ingredientType) == IngredientCategory.Protein;
            cookState = isProtein ? CookState.Raw : CookState.Cooked;
            cookProgress = 0f;
            UpdateVisual();
        }

        private void Update()
        {
            if (!isOnGrill || isHeld) return;
            if (cookState == CookState.Burnt) return;

            cookProgress += Time.deltaTime;
            CookState prev = cookState;

            if      (cookState == CookState.Raw     && cookProgress >= cookTime * 0.5f)
                cookState = CookState.Cooking;
            else if (cookState == CookState.Cooking && cookProgress >= cookTime)
            {
                cookState = CookState.Cooked;
                Debug.Log($"[Ingredient] {ingredientType} cooked! ({cookProgress:F1}s)");
                if (Player.CameraEffects.Instance != null)
                    Player.CameraEffects.Instance.Shake(0.04f, 0.1f);
            }
            else if (cookState == CookState.Cooked  && cookProgress >= cookTime + burnTime)
            {
                cookState = CookState.Burnt;
                Debug.Log($"[Ingredient] {ingredientType} BURNT!");
                if (Player.CameraEffects.Instance != null)
                    Player.CameraEffects.Instance.PlayFailShake();
            }

            if (prev != cookState) UpdateVisual();
            UpdateProgressBar();
        }

        public void PlaceOnGrill()
        {
            isOnGrill    = true;
            isHeld       = false; // critical — ensure held flag is cleared
            cookProgress = 0f;
            cookState    = CookState.Raw;
            UpdateVisual();
            Debug.Log($"[Ingredient] {ingredientType} placed on grill, cooking starts now.");
        }

        public void RemoveFromGrill() { isOnGrill = false; }

        public void OnPickedUp()
        {
            isHeld = true;
            if (isOnGrill) RemoveFromGrill();
        }

        public void OnDropped() { isHeld = false; }

        public bool IsCooked()
        {
            if (IngredientData.GetCategory(ingredientType) != IngredientCategory.Protein)
                return true;
            return cookState == CookState.Cooked;
        }

        public bool IsBurnt() => cookState == CookState.Burnt;

        private void UpdateVisual()
        {
            if (rend == null) return;
            Color c;
            switch (cookState)
            {
                case CookState.Raw:     c = RAW_COLOR;     break;
                case CookState.Cooking: c = COOKING_COLOR; break;
                case CookState.Cooked:  c = COOKED_COLOR;  break;
                case CookState.Burnt:   c = BURNT_COLOR;   break;
                default:               c = Color.white;   break;
            }
            rend.material.color = c;
        }

        private void UpdateProgressBar()
        {
            if (cookProgressBar == null) return;
            float total    = cookTime + burnTime;
            float progress = Mathf.Clamp01(cookProgress / total);
            var s          = cookProgressBar.localScale;
            s.y            = Mathf.Max(0.01f, progress);
            cookProgressBar.localScale = s;
        }
    }
}