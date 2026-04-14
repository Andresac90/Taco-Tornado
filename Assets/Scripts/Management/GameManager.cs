// GameManager.cs — Central game state for infinite mode
// No shift timer — game runs until the lose condition is hit (5 failed orders).
// Score = total money earned.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────
        public event Action         OnShiftStarted;
        public event Action         OnShiftEnded;
        public event Action<float>  OnMoneyChanged;
        public event Action<TacoOrder> OnOrderCompleted;
        public event Action<TacoOrder> OnOrderFailed;
        public event Action<string> OnGameOver;

        // ── State ─────────────────────────────────────────────────────────────
        [Header("Runtime State")]
        public float money;
        public bool  isShiftActive;

        [Header("Shift Stats")]
        public int   ordersCompleted;
        public int   ordersFailed;
        public int   perfectOrders;
        public float shiftRevenue;
        public float shiftTips;

        [Header("Lose Condition")]
        public int maxFailedOrders = GameConstants.MAX_FAILED_ORDERS;

        // ── Inventory ─────────────────────────────────────────────────────────
        // Infinite mode: stock is unlimited (or very large).
        public Dictionary<IngredientType, int> ingredientStock = new Dictionary<IngredientType, int>();

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            money = GameConstants.STARTING_MONEY;
            InitializeStock();
        }

        private void Update()
        {
            if (!isShiftActive) return;
            CheckLoseConditions();
        }

        // ── Shift ─────────────────────────────────────────────────────────────

        public void StartShift()
        {
            isShiftActive    = true;
            ordersCompleted  = 0;
            ordersFailed     = 0;
            perfectOrders    = 0;
            shiftRevenue     = 0f;
            shiftTips        = 0f;
            money            = GameConstants.STARTING_MONEY;

            InitializeStock();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            OnShiftStarted?.Invoke();
            Debug.Log("[GameManager] Infinite shift started!");
        }

        public void EndShift()
        {
            if (!isShiftActive) return;
            isShiftActive = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            OnShiftEnded?.Invoke();
            Debug.Log($"[GameManager] Game over — Revenue: ${shiftRevenue:F2}  Tips: ${shiftTips:F2}  " +
                      $"Completed: {ordersCompleted}  Failed: {ordersFailed}");
        }

        // ── Lose Condition ────────────────────────────────────────────────────

        private void CheckLoseConditions()
        {
            if (ordersFailed >= maxFailedOrders)
            {
                EndShift();
                OnGameOver?.Invoke($"Too many failed orders! ({ordersFailed} missed)");
            }
        }

        // ── Scoring ───────────────────────────────────────────────────────────

        public void CompleteOrder(TacoOrder order, float accuracy)
        {
            float tipMult;
            if      (accuracy >= 1f)    { tipMult = GameConstants.PERFECT_TIP_MULT; perfectOrders++; }
            else if (accuracy >= 0.75f)   tipMult = GameConstants.GOOD_TIP_MULT;
            else if (accuracy >= 0.5f)    tipMult = GameConstants.OK_TIP_MULT;
            else                          tipMult = GameConstants.BAD_TIP_MULT;

            float revenue = order.basePrice;
            float tip     = GameConstants.BASE_TIP * tipMult * order.tipMultiplier;
            float total   = revenue + tip;

            money        += total;
            shiftRevenue += revenue;
            shiftTips    += tip;
            ordersCompleted++;
            order.state   = OrderState.Completed;

            OnMoneyChanged?.Invoke(money);
            OnOrderCompleted?.Invoke(order);

            Debug.Log($"[GameManager] #{order.orderId} complete — {accuracy:P0}  +${total:F2}");
        }

        public void FailOrder(TacoOrder order)
        {
            ordersFailed++;
            order.state = OrderState.Failed;
            OnOrderFailed?.Invoke(order);
            Debug.Log($"[GameManager] Order #{order.orderId} failed! ({ordersFailed}/{maxFailedOrders})");
        }

        // ── Inventory ─────────────────────────────────────────────────────────

        private void InitializeStock()
        {
            foreach (IngredientType type in Enum.GetValues(typeof(IngredientType)))
                ingredientStock[type] = 999; // effectively unlimited in infinite mode
        }

        public bool ConsumeIngredient(IngredientType type)
        {
            if (ingredientStock.TryGetValue(type, out int count) && count > 0)
            {
                ingredientStock[type]--;
                return true;
            }
            Debug.LogWarning($"[GameManager] Out of stock: {type}");
            return false;
        }

        public int GetStock(IngredientType type)
        {
            return ingredientStock.TryGetValue(type, out int v) ? v : 0;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public float GetShiftProfit() => shiftRevenue + shiftTips;
        public void  SpendMoney(float amount) { money -= amount; OnMoneyChanged?.Invoke(money); }
        public bool  CanAfford(float amount)  => money >= amount;
    }
}
