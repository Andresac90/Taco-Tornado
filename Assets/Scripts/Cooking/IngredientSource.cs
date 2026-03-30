// IngredientSource.cs — A station that dispenses ingredient instances
// Attach to each station object (tortilla stack, cilantro bowl, meat tray, etc.)
// Requires a child Transform "SpawnPoint" for where the ingredient appears

using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class IngredientSource : MonoBehaviour, IInteractable
    {
        [Header("Configuration")]
        public IngredientType ingredientType;
        public GameObject ingredientPrefab; // assign per-station in inspector

        [Header("Spawn")]
        [SerializeField] private Transform spawnPoint;

        public void OnInteract(PlayerInteraction player)
        {
            if (player.IsHolding()) return;

            // Check stock
            if (!GameManager.Instance.ConsumeIngredient(ingredientType))
            {
                Debug.Log($"[Source] Out of {ingredientType}!");
                return;
            }

            // Spawn and hand to player
            GameObject obj = Instantiate(ingredientPrefab,
                spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up * 0.3f,
                Quaternion.identity);

            Ingredient ingredient = obj.GetComponent<Ingredient>();
            if (ingredient == null)
            {
                ingredient = obj.AddComponent<Ingredient>();
            }
            ingredient.ingredientType = ingredientType;

            player.PickUpIngredient(ingredient);
        }

        public void OnInteractWithHeld(PlayerInteraction player, Ingredient ingredient)
        {
            // Can't place ingredients back into a source station
        }

        public string GetInteractPrompt()
        {
            int stock = GameManager.Instance.GetStock(ingredientType);
            return $"Grab {ingredientType} ({stock} left)";
        }
    }
}
