// GameData.cs — Shared enums, constants, and data structures for Taco Tornado

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    // ──────────────────────────────────────────────
    //  ENUMS
    // ──────────────────────────────────────────────

    //INGREDIENTS
    public enum IngredientType
    {
        // Tortillas
        CornTortilla,
        FlourTortilla,

        // Proteins
        CarneAsada,
        Pollo,
        Carnitas,
        AlPastor,

        // Toppings
        Cilantro,
        Onion,
        Lime,
        Cheese,
        Lettuce,

        // Salsas
        SalsaVerde,
        SalsaRoja,
        Guacamole,
        SourCream
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

    public enum ShiftTime
    {
        Morning,
        Lunch,
        Evening,
        LateNight
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
        public float cookTime;       // 0 if no cooking needed
        public float burnTime;       // time after cooked before burning
        public bool requiresCooking;

        public static IngredientCategory GetCategory(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.CornTortilla:
                case IngredientType.FlourTortilla:
                    return IngredientCategory.Tortilla;

                case IngredientType.CarneAsada:
                case IngredientType.Pollo:
                case IngredientType.Carnitas:
                case IngredientType.AlPastor:
                    return IngredientCategory.Protein;

                case IngredientType.Cilantro:
                case IngredientType.Onion:
                case IngredientType.Lime:
                case IngredientType.Cheese:
                case IngredientType.Lettuce:
                    return IngredientCategory.Topping;

                case IngredientType.SalsaVerde:
                case IngredientType.SalsaRoja:
                case IngredientType.Guacamole:
                case IngredientType.SourCream:
                    return IngredientCategory.Salsa;

                default:
                    return IngredientCategory.Topping;
            }
        }
    }

    [Serializable]
    public class TacoOrder
    {
        public int orderId;
        public IngredientType tortillaType;
        public IngredientType proteinType;
        public List<IngredientType> toppings = new List<IngredientType>();
        public List<IngredientType> salsas = new List<IngredientType>();
        public OrderState state = OrderState.Waiting;
        public float tipMultiplier = 1f;
        public float basePrice = 5f;

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
        // Player
        public const float PLAYER_LOOK_SPEED = 2f;
        public const float PLAYER_LOOK_CLAMP_UP = 60f;
        public const float PLAYER_LOOK_CLAMP_DOWN = 80f;
        public const float INTERACT_RANGE = 2.5f;

        // Cooking
        public const float DEFAULT_COOK_TIME = 4f;
        public const float DEFAULT_BURN_TIME = 3f;
        public const float GRILL_CHECK_INTERVAL = 0.1f;

        // Customers
        public const float BASE_PATIENCE = 30f;
        public const float NIGHT_OWL_PATIENCE_MULT = 0.5f;
        public const float INFLUENCER_PATIENCE_MULT = 0.75f;
        public const float REGULAR_PATIENCE_MULT = 1.5f;
        public const float FOOD_CRITIC_PATIENCE_MULT = 0.6f;

        // Tips
        public const float BASE_TIP = 2f;
        public const float PERFECT_TIP_MULT = 2.5f;
        public const float GOOD_TIP_MULT = 1.5f;
        public const float OK_TIP_MULT = 1f;
        public const float BAD_TIP_MULT = 0.25f;

        // Economy
        public const float STARTING_MONEY = 100f;
        public const float SPOILAGE_RATE = 0.15f; // 15% of unused perishables lost

        // Shift
        public const float SHIFT_DURATION = 120f; // seconds
        public const float ORDER_SPAWN_INTERVAL_BASE = 12f;
        public const float ORDER_SPAWN_INTERVAL_MIN = 5f;
        public const int MAX_QUEUE_SIZE = 6;
    }
}
