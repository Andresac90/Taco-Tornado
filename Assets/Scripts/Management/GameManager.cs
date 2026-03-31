// GameManager.cs — Central game state, shift management, and scoring

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Events ──
        public event Action OnShiftStarted;
        public event Action OnShiftEnded;
        public event Action<float> OnMoneyChanged;
        public event Action<TacoOrder> OnOrderCompleted;
        public event Action<TacoOrder> OnOrderFailed;

        // ── State ──
        [Header("Runtime State")]
        public float money;
        public int currentDay = 1;
        public ShiftTime currentShift = ShiftTime.Lunch;
        public bool isShiftActive;

        [Header("Shift Stats")]
        public float shiftTimer;
        public int ordersCompleted;
        public int ordersFailed;
        public int perfectOrders;
        public float shiftRevenue;
        public float shiftTips;

        // ── Inventory ──
        public Dictionary<IngredientType, int> ingredientStock = new Dictionary<IngredientType, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            money = GameConstants.STARTING_MONEY;
            InitializeStock();
        }

        private void Update()
        {
            if (!isShiftActive) return;

            shiftTimer -= Time.deltaTime;
            if (shiftTimer <= 0f)
            {
                EndShift();
            }
        }

        // ──────────────────────────────────────────────
        //  SHIFT MANAGEMENT
        // ──────────────────────────────────────────────

        public void StartShift()
        {
            isShiftActive = true;
            shiftTimer = GameConstants.SHIFT_DURATION;
            ordersCompleted = 0;
            ordersFailed = 0;
            perfectOrders = 0;
            shiftRevenue = 0f;
            shiftTips = 0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnShiftStarted?.Invoke();
            Debug.Log($"[GameManager] Shift started — Day {currentDay}, {currentShift}");
        }

        public void EndShift()
        {
            isShiftActive = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Apply spoilage to perishable stock
            ApplySpoilage();

            OnShiftEnded?.Invoke();
            Debug.Log($"[GameManager] Shift ended — Revenue: ${shiftRevenue:F2}, Tips: ${shiftTips:F2}, " +
                      $"Completed: {ordersCompleted}, Failed: {ordersFailed}, Perfect: {perfectOrders}");
        }

        // ──────────────────────────────────────────────
        //  ORDER SCORING
        // ──────────────────────────────────────────────

        public void CompleteOrder(TacoOrder order, float accuracy)
        {
            float tipMult;
            if (accuracy >= 1f)
            {
                tipMult = GameConstants.PERFECT_TIP_MULT;
                perfectOrders++;
            }
            else if (accuracy >= 0.75f)
                tipMult = GameConstants.GOOD_TIP_MULT;
            else if (accuracy >= 0.5f)
                tipMult = GameConstants.OK_TIP_MULT;
            else
                tipMult = GameConstants.BAD_TIP_MULT;

            float revenue = order.basePrice;
            float tip = GameConstants.BASE_TIP * tipMult * order.tipMultiplier;
            float total = revenue + tip;

            money += total;
            shiftRevenue += revenue;
            shiftTips += tip;
            ordersCompleted++;
            order.state = OrderState.Completed;

            OnMoneyChanged?.Invoke(money);
            OnOrderCompleted?.Invoke(order);

            Debug.Log($"[GameManager] Order #{order.orderId} completed — " +
                      $"Accuracy: {accuracy:P0}, Revenue: ${revenue:F2}, Tip: ${tip:F2}");
        }

        public void FailOrder(TacoOrder order)
        {
            ordersFailed++;
            order.state = OrderState.Failed;
            OnOrderFailed?.Invoke(order);
            Debug.Log($"[GameManager] Order #{order.orderId} failed!");
        }

        // ──────────────────────────────────────────────
        //  INVENTORY
        // ──────────────────────────────────────────────

        private void InitializeStock()
        {
            // Start with a basic stock of everything for the prototype
            foreach (IngredientType type in Enum.GetValues(typeof(IngredientType)))
            {
                ingredientStock[type] = 20;
            }
        }

        public bool ConsumeIngredient(IngredientType type)
        {
            if (ingredientStock.ContainsKey(type) && ingredientStock[type] > 0)
            {
                ingredientStock[type]--;
                return true;
            }
            Debug.LogWarning($"[GameManager] Out of stock: {type}");
            return false;
        }

        public int GetStock(IngredientType type)
        {
            return ingredientStock.ContainsKey(type) ? ingredientStock[type] : 0;
        }

        public void PurchaseIngredient(IngredientType type, int quantity, float totalCost)
        {
            if (money < totalCost)
            {
                Debug.LogWarning("[GameManager] Not enough money!");
                return;
            }

            money -= totalCost;
            if (!ingredientStock.ContainsKey(type))
                ingredientStock[type] = 0;
            ingredientStock[type] += quantity;

            OnMoneyChanged?.Invoke(money);
        }

        private void ApplySpoilage()
        {
            // Perishable proteins and produce lose stock overnight
            IngredientType[] perishables = {
                IngredientType.CarneAsada, IngredientType.Pollo,
                IngredientType.Carnitas, IngredientType.AlPastor,
                IngredientType.Cilantro, IngredientType.Lettuce,
                IngredientType.Guacamole
            };

            foreach (var type in perishables)
            {
                if (ingredientStock.ContainsKey(type) && ingredientStock[type] > 0)
                {
                    int spoiled = Mathf.CeilToInt(ingredientStock[type] * GameConstants.SPOILAGE_RATE);
                    ingredientStock[type] = Mathf.Max(0, ingredientStock[type] - spoiled);
                    if (spoiled > 0)
                        Debug.Log($"[GameManager] Spoilage: lost {spoiled}x {type}");
                }
            }
        }

        // ──────────────────────────────────────────────
        //  FINANCE HELPERS
        // ──────────────────────────────────────────────

        public float GetShiftProfit()
        {
            return shiftRevenue + shiftTips;
        }

        public void SpendMoney(float amount)
        {
            money -= amount;
            OnMoneyChanged?.Invoke(money);
        }

        public bool CanAfford(float amount)
        {
            return money >= amount;
        }
    }
}
