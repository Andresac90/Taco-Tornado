// GameData.cs — Shared enums, constants, and data structures for Taco Tornado

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    // ──────────────────────────────────────────────
    //  ENUMS
    // ──────────────────────────────────────────────

    // TRIMMED INGREDIENT SET — 8 total, 4 stations
    // Station A (Left):   Salsa Verde, Salsa Roja
    // Station B:          Cilantro, Onion
    // Station C (Middle): Corn Tortilla, Flour Tortilla  (+ Customer Plate hand-off window)
    // Station D (Right):  Al Pastor, Discada  +  Grill
    public enum IngredientType
    {
        // Tortillas (Station C)
        CornTortilla,
        FlourTortilla,

        // Proteins (Station D — need grill)
        AlPastor,
        Discada,

        // Toppings (Station B)
        Cilantro,
        Onion,

        // Salsas (Station A)
        SalsaVerde,
        SalsaRoja,
    }

    public enum IngredientCategory
    {
        Tortilla,
        Protein,
        Topping,
        Salsa
    }

    public enum CookState
    {
        Raw,
        Cooking,
        Cooked,
        Burnt
    }

    public enum OrderState
    {
        Waiting,
        InProgress,
        Completed,
        Failed
    }

    public enum CustomerType
    {
        Regular,
        NightOwl,
        Influencer,
        FoodCritic
    }

    // ──────────────────────────────────────────────
    //  DATA STRUCTURES
    // ──────────────────────────────────────────────

    [Serializable]
    public class IngredientData
    {
        public IngredientType type;
        public IngredientCategory category;
        public string displayName;
        public float baseCost;
        public float cookTime;
        public float burnTime;
        public bool requiresCooking;

        public static IngredientCategory GetCategory(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.CornTortilla:
                case IngredientType.FlourTortilla:
                    return IngredientCategory.Tortilla;

                case IngredientType.AlPastor:
                case IngredientType.Discada:
                    return IngredientCategory.Protein;

                case IngredientType.Cilantro:
                case IngredientType.Onion:
                    return IngredientCategory.Topping;

                case IngredientType.SalsaVerde:
                case IngredientType.SalsaRoja:
                    return IngredientCategory.Salsa;

                default:
                    return IngredientCategory.Topping;
            }
        }

        public static bool RequiresCooking(IngredientType type)
        {
            return GetCategory(type) == IngredientCategory.Protein;
        }
    }

    [Serializable]
    public class TacoOrder
    {
        public int orderId;
        public IngredientType tortillaType;
        public IngredientType proteinType;
        public List<IngredientType> toppings = new List<IngredientType>();
        public List<IngredientType> salsas   = new List<IngredientType>();
        public OrderState state = OrderState.Waiting;
        public float tipMultiplier = 1f;
        public float basePrice     = 5f;
        public float patienceDuration; // set per-order based on difficulty

        public List<IngredientType> GetAllRequired()
        {
            var all = new List<IngredientType> { tortillaType, proteinType };
            all.AddRange(toppings);
            all.AddRange(salsas);
            return all;
        }
    }

    // ──────────────────────────────────────────────
    //  CONSTANTS
    // ──────────────────────────────────────────────

    public static class GameConstants
    {
        // Player movement
        public const float PLAYER_MOVE_SPEED     = 3.5f;
        public const float PLAYER_LOOK_SPEED     = 2f;
        public const float PLAYER_LOOK_CLAMP_UP  = 55f;
        public const float PLAYER_LOOK_CLAMP_DOWN = 75f;
        public const float INTERACT_RANGE        = 2.5f;

        // Truck bounds (how far the player can walk, in local X)
        // Adjust these to match your actual truck interior width
        public const float TRUCK_MIN_X = -2.0f;
        public const float TRUCK_MAX_X =  2.0f;
        public const float TRUCK_MIN_Z = -1.0f;
        public const float TRUCK_MAX_Z =  0.5f;

        // Cooking
        public const float DEFAULT_COOK_TIME       = 5f;
        public const float DEFAULT_BURN_TIME       = 4f;
        public const float GRILL_CHECK_INTERVAL    = 0.1f;

        // Difficulty — infinite mode
        // Patience shrinks and spawn rate increases as score grows
        public const float BASE_PATIENCE           = 40f;   // seconds at wave 0
        public const float MIN_PATIENCE            = 18f;   // floor
        public const float PATIENCE_DECAY_PER_WAVE = 2.5f;  // seconds less per wave

        public const float BASE_SPAWN_INTERVAL     = 14f;   // seconds between customers at wave 0
        public const float MIN_SPAWN_INTERVAL      = 5f;    // floor
        public const float SPAWN_DECAY_PER_WAVE    = 1.0f;  // seconds faster per wave

        public const int   ORDERS_PER_WAVE         = 5;     // orders to complete before next wave
        public const int   MAX_QUEUE_SIZE           = 4;     // max simultaneous tickets

        // Tips
        public const float BASE_TIP          = 2f;
        public const float PERFECT_TIP_MULT  = 2.5f;
        public const float GOOD_TIP_MULT     = 1.5f;
        public const float OK_TIP_MULT       = 1f;
        public const float BAD_TIP_MULT      = 0.25f;

        // Economy
        public const float STARTING_MONEY    = 0f;   // infinite mode — score is money
        public const float SPOILAGE_RATE     = 0f;   // no spoilage in infinite mode

        // Lose condition
        public const int   MAX_FAILED_ORDERS = 5;

        // Combine animation
        public const float COMBINE_ANIM_DURATION = 0.35f;
    }
}
