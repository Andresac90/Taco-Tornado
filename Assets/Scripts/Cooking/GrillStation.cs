// GrillStation.cs — The flat-top grill where proteins cook
// Now uses the two-hand IInteractable interface.
//
// LAYOUT NOTE: Place the grill GO on the RIGHT side of the truck counter.
//
// FLOW:
//   RMB on station → grab raw protein (right hand)
//   RMB on grill while holding protein → place on grill
//   RMB on grill with empty right hand → pick up cooked/burnt protein (right hand)
//   Then: MMB or LMB (if protein cooked) to combine onto plate

using System.Collections.Generic;
using UnityEngine;
using TacoTornado.Player;

namespace TacoTornado.Cooking
{
    public class GrillStation : MonoBehaviour, IInteractable
    {
        [Header("Grill Slots")]
        [SerializeField] private Transform[] grillSlots;
        [SerializeField] private int maxSlots = 4;

        [Header("Optional VFX")]
        [SerializeField] private ParticleSystem steamParticles;
        [SerializeField] private Light          grillLight;

        private Dictionary<int, Ingredient> slotContents = new Dictionary<int, Ingredient>();

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (grillLight == null) return;
            float target = 0.3f + slotContents.Count * 0.5f;
            grillLight.intensity = Mathf.Lerp(grillLight.intensity, target, Time.deltaTime * 3f);
        }

        // ── IInteractable ─────────────────────────────────────────────────────

        public void OnRightHandInteract(PlayerHands hands)
        {
            // Right hand empty + looking at grill → pick up cooked/burnt protein
            Ingredient ready = FindPickupable();
            if (ready != null)
            {
                RemoveFromGrill(ready);
                hands.GrabIngredient(ready);
                Debug.Log($"[Grill] Picked up {ready.ingredientType} ({ready.cookState})");
            }
            else
            {
                Debug.Log("[Grill] Nothing ready to pick up yet.");
            }
        }

        public void OnRightHandInteractWithHeld(PlayerHands hands, Ingredient ingredient)
        {
            // Right hand holding ingredient + looking at grill → place on grill
            if (IngredientData.GetCategory(ingredient.ingredientType) != IngredientCategory.Protein)
            {
                Debug.Log("[Grill] Only proteins go on the grill!");
                return;
            }

            if (ingredient.cookState == CookState.Cooked || ingredient.cookState == CookState.Burnt)
            {
                Debug.Log("[Grill] Already cooked!");
                return;
            }

            int slot = FindEmptySlot();
            if (slot < 0)
            {
                Debug.Log("[Grill] Grill is full!");
                return;
            }

            PlaceOnGrill(ingredient, slot);
            hands.ClearRightHand();

            if (steamParticles != null && !steamParticles.isPlaying)
                steamParticles.Play();
        }

        public void OnLeftHandInteract(PlayerHands hands)
        {
            // Left hand (plate) aimed at grill — not a valid action
            // Could auto-add cooked protein directly to plate if desired in future
            Debug.Log("[Grill] Use right hand (RMB) to interact with the grill.");
        }

        public string GetInteractPrompt(PlayerHands hands)
        {
            int used   = slotContents.Count;
            int cooked = 0, cooking = 0, raw = 0;
            foreach (var kvp in slotContents)
            {
                if (kvp.Value == null) continue;
                switch (kvp.Value.cookState)
                {
                    case CookState.Cooked:  cooked++;  break;
                    case CookState.Cooking: cooking++; break;
                    case CookState.Raw:     raw++;     break;
                }
            }

            bool holdingProtein = !hands.IsRightHandEmpty() &&
                IngredientData.GetCategory(hands.GetRightHandIngredient().ingredientType) == IngredientCategory.Protein;

            if (holdingProtein)    return "[RMB] Place on grill";
            if (cooked > 0)        return $"[RMB] Pick up cooked meat ({cooked} ready)";
            if (cooking + raw > 0) return $"Grill — cooking... ({used}/{maxSlots})";
            return $"[RMB] Place protein — Grill ({used}/{maxSlots})";
        }

        // ── Grill Logic ───────────────────────────────────────────────────────

        private void PlaceOnGrill(Ingredient ingredient, int slotIndex)
        {
            slotContents[slotIndex] = ingredient;

            Transform slot = (grillSlots != null && slotIndex < grillSlots.Length)
                ? grillSlots[slotIndex]
                : transform;

            ingredient.transform.SetParent(slot);
            ingredient.transform.localPosition = Vector3.zero;
            ingredient.transform.localRotation = Quaternion.identity;
            ingredient.transform.localScale    = Vector3.one; // natural prefab scale

            var rb = ingredient.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }

            // Keep collider OFF while on grill — raycast must hit grill, not ingredient
            var col = ingredient.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            ingredient.PlaceOnGrill();
            Debug.Log($"[Grill] {ingredient.ingredientType} placed on slot {slotIndex}");
        }

        private void RemoveFromGrill(Ingredient ingredient)
        {
            int key = -1;
            foreach (var kvp in slotContents)
                if (kvp.Value == ingredient) { key = kvp.Key; break; }

            if (key >= 0) slotContents.Remove(key);
            ingredient.RemoveFromGrill();

            if (slotContents.Count == 0 && steamParticles != null)
                steamParticles.Stop();
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < maxSlots; i++)
                if (!slotContents.ContainsKey(i) || slotContents[i] == null)
                    return i;
            return -1;
        }

        private Ingredient FindPickupable()
        {
            Ingredient cooked = null, burnt = null;
            float nearC = float.MaxValue, nearB = float.MaxValue;

            foreach (var kvp in slotContents)
            {
                if (kvp.Value == null) continue;
                float d = kvp.Value.cookState == CookState.Cooked ? 0f : 1f; // just pick first
                if (kvp.Value.cookState == CookState.Cooked && d < nearC) { nearC = d; cooked = kvp.Value; }
                if (kvp.Value.cookState == CookState.Burnt  && d < nearB) { nearB = d; burnt  = kvp.Value; }
            }
            return cooked ?? burnt;
        }

        public List<Ingredient> GetAllOnGrill() => new List<Ingredient>(slotContents.Values);
    }
}
