// GameData.cs — Shared enums, constants, and data structures for Taco Tornado

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    public enum IngredientType
    {
        CornTortilla,
        FlourTortilla,
        AlPastor,
        Discada,
        Cilantro,
        Onion,
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
            => GetCategory(type) == IngredientCategory.Protein;
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
        public float tipMultiplier   = 1f;
        public float basePrice       = 5f;
        public float patienceDuration;

        public List<IngredientType> GetAllRequired()
        {
            var all = new List<IngredientType> { tortillaType, proteinType };
            all.AddRange(toppings);
            all.AddRange(salsas);
            return all;
        }
    }

    public static class GameConstants
    {
        // Player
        public const float PLAYER_MOVE_SPEED      = 3.5f;
        public const float PLAYER_LOOK_SPEED      = 2f;
        public const float PLAYER_LOOK_CLAMP_UP   = 55f;
        public const float PLAYER_LOOK_CLAMP_DOWN = 75f;
        public const float INTERACT_RANGE         = 2.5f;

        // Truck bounds
        public const float TRUCK_MIN_X = -2.0f;
        public const float TRUCK_MAX_X =  2.0f;
        public const float TRUCK_MIN_Z = -1.0f;
        public const float TRUCK_MAX_Z =  0.5f;

        // Cooking
        public const float DEFAULT_COOK_TIME    = 7f;
        public const float DEFAULT_BURN_TIME    = 15f;  // lots of time before burning
        public const float GRILL_CHECK_INTERVAL = 0.1f;

        // ── DIFFICULTY — deliberately slow and forgiving ──────────────────────
        //
        // Wave 0 feel: one customer every 35 seconds, 90 seconds of patience.
        // A new player can comfortably make one taco per customer with time to spare.
        // Difficulty ramps up slowly — wave 5 is roughly what wave 1 used to be.

        public const float BASE_PATIENCE           = 90f;   // 90 sec patience at wave 0
        public const float MIN_PATIENCE            = 35f;   // never goes below 35 sec
        public const float PATIENCE_DECAY_PER_WAVE = 4f;    // lose 4 sec per wave

        public const float BASE_SPAWN_INTERVAL     = 35f;   // 35 sec between customers at wave 0
        public const float MIN_SPAWN_INTERVAL      = 10f;   // never faster than 10 sec
        public const float SPAWN_DECAY_PER_WAVE    = 2f;    // 2 sec faster per wave

        public const int   ORDERS_PER_WAVE         = 5;     // complete 5 orders to advance
        public const int   MAX_QUEUE_SIZE           = 3;     // max 3 tickets on screen

        // Tips
        public const float BASE_TIP         = 2f;
        public const float PERFECT_TIP_MULT = 2.5f;
        public const float GOOD_TIP_MULT    = 1.5f;
        public const float OK_TIP_MULT      = 1f;
        public const float BAD_TIP_MULT     = 0.25f;

        // Economy
        public const float STARTING_MONEY = 0f;
        public const float SPOILAGE_RATE  = 0f;

        // Lose condition
        public const int MAX_FAILED_ORDERS = 7;

        // Combine animation
        public const float COMBINE_ANIM_DURATION = 0.3f;
    }
}