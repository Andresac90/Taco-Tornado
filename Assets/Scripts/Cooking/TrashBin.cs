// TrashBin.cs
// RMB while holding ingredient → trash it
// LMB (plate hand) aimed at trash → clear entire plate (so player can start over)

using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class TrashBin : MonoBehaviour, IInteractable
    {
        public void OnRightHandInteract(PlayerHands hands)
        {
            // Right hand empty, nothing to trash
        }

        public void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient)
        {
            hands.ClearRightHand();
            Destroy(ingredient.gameObject);
            Debug.Log($"[Trash] Trashed {ingredient.ingredientType}");
            if (Player.CameraEffects.Instance != null)
                Player.CameraEffects.Instance.Shake(0.05f, 0.1f);
        }

        public void OnLeftHandInteract(PlayerHands hands)
        {
            // LMB on trash = clear the whole plate so player can start fresh
            if (!hands.HasPlate()) return;
            if (hands.GetPlate().IngredientCount == 0) return;

            hands.GetPlate().ClearIngredients();
            Debug.Log("[Trash] Plate cleared!");

            if (Player.CameraEffects.Instance != null)
                Player.CameraEffects.Instance.Shake(0.08f, 0.18f);

            if (TacoTornado.UI.GameHUD.Instance != null)
                TacoTornado.UI.GameHUD.Instance.ShowInteractPrompt("Plate cleared!");
        }

        public string GetInteractPrompt(PlayerHands hands)
        {
            bool hasPlateIngredients = hands.HasPlate() && hands.GetPlate().IngredientCount > 0;
            bool holdingIngredient   = hands.IsRightHandHolding();

            if (holdingIngredient && hasPlateIngredients)
                return $"[RMB] Trash {hands.GetRightHandIngredient().ingredientType}  [LMB] Clear plate";
            if (holdingIngredient)
                return $"[RMB] Trash {hands.GetRightHandIngredient().ingredientType}";
            if (hasPlateIngredients)
                return "[LMB] Clear plate and start over";
            return "Trash Bin";
        }
    }
}