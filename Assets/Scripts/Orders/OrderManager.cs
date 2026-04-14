// OrderManager.cs — Infinite mode order generation with wave-based difficulty scaling
// Orders use only the trimmed 8-ingredient set.
// Difficulty increases every ORDERS_PER_WAVE completed orders:
//   - Patience shrinks (customers wait less)
//   - Spawn interval decreases (more customers arriving)
//   - Orders add more toppings/salsas

using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<TacoOrder> OnNewOrder;
        public event System.Action<TacoOrder> OnOrderRemoved;
        public event System.Action<int>       OnWaveChanged; // fires when wave advances

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Queue")]
        [SerializeField] private int maxQueueSize = GameConstants.MAX_QUEUE_SIZE;

        // ── Runtime ───────────────────────────────────────────────────────────
        public List<TacoOrder> activeOrders = new List<TacoOrder>();

        private int   nextOrderId      = 1;
        private float spawnTimer;
        private float currentSpawnInterval;

        // Wave / difficulty
        public  int   currentWave      = 0;
        private int   ordersThisWave   = 0; // completed orders since last wave bump

        private Dictionary<int, float> orderPatience = new Dictionary<int, float>();

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted  += OnShiftStart;
                GameManager.Instance.OnShiftEnded    += OnShiftEnd;
                GameManager.Instance.OnOrderCompleted += OnOrderCompletedCallback;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted  -= OnShiftStart;
                GameManager.Instance.OnShiftEnded    -= OnShiftEnd;
                GameManager.Instance.OnOrderCompleted -= OnOrderCompletedCallback;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isShiftActive) return;

            // Auto spawn (TacoQueueManager overrides this when NPCs are in use)
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && activeOrders.Count < maxQueueSize)
            {
                SpawnOrder();
                spawnTimer = currentSpawnInterval;
            }

            UpdatePatience();
        }

        // ── Shift Lifecycle ───────────────────────────────────────────────────

        private void OnShiftStart()
        {
            activeOrders.Clear();
            orderPatience.Clear();
            nextOrderId    = 1;
            currentWave    = 0;
            ordersThisWave = 0;

            currentSpawnInterval = GameConstants.BASE_SPAWN_INTERVAL;
            spawnTimer           = 3f; // short delay before first order

            Debug.Log("[Orders] Infinite mode started — Wave 0");
        }

        private void OnShiftEnd()
        {
            foreach (var order in activeOrders)
                GameManager.Instance.FailOrder(order);

            activeOrders.Clear();
            orderPatience.Clear();
        }

        private void OnOrderCompletedCallback(TacoOrder order)
        {
            ordersThisWave++;
            if (ordersThisWave >= GameConstants.ORDERS_PER_WAVE)
            {
                ordersThisWave = 0;
                AdvanceWave();
            }
        }

        // ── Wave Difficulty ───────────────────────────────────────────────────

        private void AdvanceWave()
        {
            currentWave++;

            // Shrink spawn interval (more customers)
            currentSpawnInterval = Mathf.Max(
                GameConstants.MIN_SPAWN_INTERVAL,
                GameConstants.BASE_SPAWN_INTERVAL - currentWave * GameConstants.SPAWN_DECAY_PER_WAVE);

            Debug.Log($"[Orders] ── WAVE {currentWave} ──  " +
                      $"Spawn: {currentSpawnInterval:F1}s  " +
                      $"Patience: {GetCurrentPatience():F1}s");

            OnWaveChanged?.Invoke(currentWave);
        }

        public float GetCurrentPatience()
        {
            return Mathf.Max(
                GameConstants.MIN_PATIENCE,
                GameConstants.BASE_PATIENCE - currentWave * GameConstants.PATIENCE_DECAY_PER_WAVE);
        }

        // ── Order Generation ──────────────────────────────────────────────────

        /// <summary>Spawns a new order. Called by Update or TacoQueueManager.</summary>
        public void SpawnOrder()
        {
            if (activeOrders.Count >= maxQueueSize) return;

            TacoOrder order = new TacoOrder
            {
                orderId           = nextOrderId++,
                tortillaType      = RandomTortilla(),
                proteinType       = RandomProtein(),
                toppings          = RandomToppings(),
                salsas            = RandomSalsas(),
                state             = OrderState.Waiting,
                basePrice         = 5f + currentWave * 0.5f, // price rises with wave
                tipMultiplier     = 1f,
                patienceDuration  = GetCurrentPatience(),
            };

            activeOrders.Add(order);
            orderPatience[order.orderId] = order.patienceDuration;
            OnNewOrder?.Invoke(order);

            Debug.Log($"[Orders] #{order.orderId}  {order.tortillaType} | {order.proteinType} | " +
                      $"toppings:{order.toppings.Count} salsas:{order.salsas.Count}  " +
                      $"patience:{order.patienceDuration:F0}s  wave:{currentWave}");
        }

        // ── Ingredient Randomizers ────────────────────────────────────────────
        // Only use the 8 trimmed ingredients.

        private IngredientType RandomTortilla()
        {
            return Random.value > 0.5f ? IngredientType.CornTortilla : IngredientType.FlourTortilla;
        }

        private IngredientType RandomProtein()
        {
            return Random.value > 0.5f ? IngredientType.AlPastor : IngredientType.Discada;
        }

        private List<IngredientType> RandomToppings()
        {
            // Wave 0: 0-1 toppings. Wave 3+: 1-2 toppings.
            int maxCount  = currentWave >= 3 ? 2 : 1;
            int count     = Random.Range(currentWave == 0 ? 0 : 1, maxCount + 1);

            var all       = new List<IngredientType> { IngredientType.Cilantro, IngredientType.Onion };
            var result    = new List<IngredientType>();

            Shuffle(all);
            for (int i = 0; i < Mathf.Min(count, all.Count); i++)
                result.Add(all[i]);

            return result;
        }

        private List<IngredientType> RandomSalsas()
        {
            // Wave 0: no salsa. Wave 2+: 0-1. Wave 5+: 1-2.
            if (currentWave < 2) return new List<IngredientType>();

            int maxCount = currentWave >= 5 ? 2 : 1;
            int count    = Random.Range(0, maxCount + 1);

            var all      = new List<IngredientType> { IngredientType.SalsaVerde, IngredientType.SalsaRoja };
            var result   = new List<IngredientType>();

            Shuffle(all);
            for (int i = 0; i < Mathf.Min(count, all.Count); i++)
                result.Add(all[i]);

            return result;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Patience ──────────────────────────────────────────────────────────

        private void UpdatePatience()
        {
            var expired = new List<TacoOrder>();

            foreach (var order in activeOrders)
            {
                if (!orderPatience.ContainsKey(order.orderId))
                    orderPatience[order.orderId] = GetCurrentPatience();

                orderPatience[order.orderId] -= Time.deltaTime;

                if (orderPatience[order.orderId] <= 0f)
                    expired.Add(order);
            }

            foreach (var order in expired)
            {
                GameManager.Instance.FailOrder(order);
                activeOrders.Remove(order);
                orderPatience.Remove(order.orderId);
                OnOrderRemoved?.Invoke(order);
                Debug.Log($"[Orders] Order #{order.orderId} expired!");
            }
        }

        public float GetPatienceNormalized(int orderId)
        {
            if (!orderPatience.ContainsKey(orderId)) return 1f;
            return Mathf.Clamp01(orderPatience[orderId] / GetCurrentPatience());
        }

        // ── Scoring ───────────────────────────────────────────────────────────

        public void TrySubmitTaco(List<IngredientType> assembled)
        {
            if (activeOrders.Count == 0)
            {
                Debug.Log("[Orders] No active orders!");
                return;
            }

            TacoOrder best       = null;
            float     bestScore  = 0f;

            foreach (var order in activeOrders)
            {
                float acc = CalculateAccuracy(order, assembled);
                if (acc > bestScore) { bestScore = acc; best = order; }
            }

            if (best != null && bestScore >= 0.3f)
            {
                activeOrders.Remove(best);
                orderPatience.Remove(best.orderId);
                GameManager.Instance.CompleteOrder(best, bestScore);
                OnOrderRemoved?.Invoke(best);
                Debug.Log($"[Orders] Taco matched #{best.orderId} — {bestScore:P0}");
            }
            else
            {
                Debug.Log("[Orders] No matching order!");
            }
        }

        private float CalculateAccuracy(TacoOrder order, List<IngredientType> assembled)
        {
            var required     = order.GetAllRequired();
            int totalReq     = required.Count;
            int matched      = 0;
            var assembledCopy = new List<IngredientType>(assembled);

            foreach (var req in required)
            {
                if (assembledCopy.Contains(req))
                {
                    matched++;
                    assembledCopy.Remove(req);
                }
            }

            float extraPenalty = assembledCopy.Count * 0.1f;
            return Mathf.Clamp01(totalReq > 0 ? (float)matched / totalReq - extraPenalty : 0f);
        }
    }
}
