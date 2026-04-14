// TrashBin.cs — Trashes whatever is in the right hand
// Place on the LEFT side of the counter per the layout drawing.

using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class TrashBin : MonoBehaviour, IInteractable
    {
        public void OnRightHandInteract(PlayerHands hands)
        {
            // Nothing in right hand, nothing to trash
        }

        public void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient)
        {
            // RMB on trash while holding → destroy the held ingredient
            hands.ClearRightHand();
            Destroy(ingredient.gameObject);

            Debug.Log($"[Trash] Trashed {ingredient.ingredientType}");

            if (Player.CameraEffects.Instance != null)
                Player.CameraEffects.Instance.Shake(0.05f, 0.1f);
        }

        public void OnLeftHandInteract(PlayerHands hands)
        {
            // Could clear the plate here if desired — left as future feature
        }

        public string GetInteractPrompt(PlayerHands hands)
        {
            if (hands.IsRightHandHolding())
                return $"[RMB] Trash {hands.GetRightHandIngredient().ingredientType}";
            return "Trash Bin";
        }
    }
}
