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
        [SerializeField] private GameObject displayModel;
        [SerializeField] private Material   outOfStockMaterial;

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;

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
            float bob = Mathf.Sin(Time.time * 1.8f + transform.position.x) * 0.01f;
            displayModel.transform.localPosition = displayRestPos + Vector3.up * bob;
            displayModel.transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.Self);
        }

        public void OnRightHandInteract(PlayerHands hands)
        {
            if (!hands.IsRightHandEmpty()) { Debug.Log("[Source] Right hand already holding something."); return; }
            Ingredient ing = SpawnIngredient();
            if (ing != null) hands.GrabIngredient(ing);
        }

        public void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient) { }

        public void OnLeftHandInteract(PlayerHands hands)
        {
            var cat = IngredientData.GetCategory(ingredientType);
            if (cat == IngredientCategory.Protein) { Debug.Log("[Source] Proteins must be cooked — grab with RMB then grill."); return; }
            if (!hands.HasPlate()) { Debug.Log("[Source] No plate in left hand."); return; }

            Ingredient ing = SpawnIngredient();
            if (ing == null) return;

            hands.GrabIngredient(ing);
            hands.TryCombine();
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

        private Ingredient SpawnIngredient()
        {
            if (GameManager.Instance == null || !GameManager.Instance.ConsumeIngredient(ingredientType))
            {
                Debug.Log($"[Source] Out of {ingredientType}!");
                SetOutOfStockVisual(true);
                return null;
            }
            SetOutOfStockVisual(GameManager.Instance.GetStock(ingredientType) == 0);

            if (ingredientPrefab == null) { Debug.LogError($"[Source] No prefab assigned for {ingredientType}!"); return null; }

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.3f;
            GameObject obj = Instantiate(ingredientPrefab, pos, Quaternion.identity);
            obj.SetActive(true);

            Ingredient ing = obj.GetComponent<Ingredient>();
            if (ing == null)
            {
                Debug.LogError($"[Source] Prefab '{ingredientPrefab.name}' has no Ingredient component! Open the prefab and add it.");
                Destroy(obj);
                return null;
            }

            // Set type THEN call InitializeType so cookState is correct
            ing.ingredientType = ingredientType;
            ing.InitializeType();
            return ing;
        }

        private void SetOutOfStockVisual(bool empty)
        {
            if (outOfStock == empty) return;
            outOfStock = empty;
            if (displayModel == null) return;
            if (empty)
            {
                if (outOfStockMaterial != null) { var r = displayModel.GetComponent<Renderer>(); if (r != null) r.material = outOfStockMaterial; }
                else displayModel.SetActive(false);
            }
            else displayModel.SetActive(true);
        }
    }
}