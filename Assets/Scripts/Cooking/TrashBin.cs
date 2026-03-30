// TrashBin.cs — Discard held ingredients or dump a bad taco
// Attach to a trash can object near the prep area

using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class TrashBin : MonoBehaviour, IInteractable
    {
        public void OnInteract(PlayerInteraction player)
        {
            // Nothing to do with empty hands on trash
        }

        public void OnInteractWithHeld(PlayerInteraction player, Ingredient ingredient)
        {
            Debug.Log($"[Trash] Discarded {ingredient.ingredientType}");

            player.ClearHeldIngredient();
            Destroy(ingredient.gameObject);
        }

        public string GetInteractPrompt()
        {
            return "Trash (discard ingredient)";
        }
    }
}
