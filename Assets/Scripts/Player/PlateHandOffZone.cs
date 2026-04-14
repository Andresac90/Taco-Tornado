// PlateHandOffZone.cs — The customer-facing window in the middle of the counter.
// LMB while holding a built plate → serve it.
// Place this on a trigger collider / visual indicator at the serving window.

using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class PlateHandOffZone : MonoBehaviour, IInteractable
    {
        public void OnRightHandInteract(PlayerHands hands)
        {
            // Right hand at the window — nothing to grab here
        }

        public void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient)
        {
            // Can't place raw ingredients at the window
        }

        public void OnLeftHandInteract(PlayerHands hands)
        {
            // LMB at the window = serve the plate
            if (!hands.HasPlate())
            {
                Debug.Log("[Window] No plate!");
                return;
            }

            var plate = hands.GetPlate();

            if (!plate.HasTortilla())
            {
                if (UI.GameHUD.Instance != null)
                    UI.GameHUD.Instance.ShowInteractPrompt("Need a tortilla first!");
                return;
            }

            if (!plate.HasProtein())
            {
                if (UI.GameHUD.Instance != null)
                    UI.GameHUD.Instance.ShowInteractPrompt("Need a cooked protein!");
                return;
            }

            if (plate.IngredientCount < 2)
            {
                if (UI.GameHUD.Instance != null)
                    UI.GameHUD.Instance.ShowInteractPrompt("Need at least tortilla + protein!");
                return;
            }

            hands.ServePlate();
        }

        public string GetInteractPrompt(PlayerHands hands)
        {
            if (!hands.HasPlate()) return "Customer Window";

            var plate = hands.GetPlate();
            if (!plate.HasTortilla()) return "Customer Window — add tortilla first";
            if (!plate.HasProtein())  return "Customer Window — add cooked protein";
            return $"[LMB] Serve taco! ({plate.IngredientCount} ingredients)";
        }
    }
}
