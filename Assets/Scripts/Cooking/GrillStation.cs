// GrillStation.cs — The flat-top grill where proteins cook
// Attach to the grill surface object. Has multiple GrillSlots (child transforms).

using System.Collections.Generic;
using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class GrillStation : MonoBehaviour, IInteractable
    {
        [Header("Grill Slots")]
        [SerializeField] private Transform[] grillSlots; // child empty transforms for positioning
        [SerializeField] private int maxSlots = 4;

        private Dictionary<int, Ingredient> slotContents = new Dictionary<int, Ingredient>();

        public void OnInteract(PlayerInteraction player)
        {
            if (player.IsHolding()) return;

            // Try to pick up the nearest cooked ingredient from the grill
            Ingredient nearest = FindNearestCooked(player.transform.position);
            if (nearest != null)
            {
                RemoveFromGrill(nearest);
                player.PickUpIngredient(nearest);
            }
        }

        public void OnInteractWithHeld(PlayerInteraction player, Ingredient ingredient)
        {
            // Only proteins go on the grill
            if (IngredientData.GetCategory(ingredient.ingredientType) != IngredientCategory.Protein)
            {
                Debug.Log("[Grill] Only proteins can be grilled!");
                return;
            }

            // Already cooked?
            if (ingredient.cookState == CookState.Cooked || ingredient.cookState == CookState.Burnt)
            {
                Debug.Log("[Grill] This is already cooked!");
                return;
            }

            int slot = FindEmptySlot();
            if (slot < 0)
            {
                Debug.Log("[Grill] Grill is full!");
                return;
            }

            // Place on grill
            PlaceOnGrill(ingredient, slot);
            player.ClearHeldIngredient();
        }

        public string GetInteractPrompt()
        {
            int used = slotContents.Count;
            return $"Grill ({used}/{maxSlots})";
        }

        // ──────────────────────────────────────────────
        //  GRILL MANAGEMENT
        // ──────────────────────────────────────────────

        private void PlaceOnGrill(Ingredient ingredient, int slotIndex)
        {
            slotContents[slotIndex] = ingredient;

            // Position on the grill slot
            Transform slot = (grillSlots != null && slotIndex < grillSlots.Length)
                ? grillSlots[slotIndex]
                : transform;

            ingredient.transform.SetParent(slot);
            ingredient.transform.localPosition = Vector3.zero;
            ingredient.transform.localRotation = Quaternion.identity;

            // Re-enable collider but keep kinematic (sits on grill)
            var rb = ingredient.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = true;
            }
            var col = ingredient.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            ingredient.PlaceOnGrill();
            Debug.Log($"[Grill] Placed {ingredient.ingredientType} on slot {slotIndex}");
        }

        private void RemoveFromGrill(Ingredient ingredient)
        {
            foreach (var kvp in slotContents)
            {
                if (kvp.Value == ingredient)
                {
                    slotContents.Remove(kvp.Key);
                    break;
                }
            }
            ingredient.RemoveFromGrill();
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (!slotContents.ContainsKey(i))
                    return i;
            }
            return -1;
        }

        private Ingredient FindNearestCooked(Vector3 playerPos)
        {
            Ingredient nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var kvp in slotContents)
            {
                if (kvp.Value != null && kvp.Value.cookState == CookState.Cooked)
                {
                    float dist = Vector3.Distance(playerPos, kvp.Value.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = kvp.Value;
                    }
                }
            }
            return nearest;
        }

        /// <summary>
        /// Get all ingredients currently on the grill (for UI display, etc.)
        /// </summary>
        public List<Ingredient> GetAllOnGrill()
        {
            return new List<Ingredient>(slotContents.Values);
        }
    }
}
