// OrderManager.cs — Generates taco orders and scores submissions
// Attach to an empty GameObject in the scene

using System.Collections.Generic;
using UnityEngine;

namespace TacoTornado
{
    public class OrderManager : MonoBehaviour
    {
        public static OrderManager Instance { get; private set; }

        // ── Events ──
        public event System.Action<TacoOrder> OnNewOrder;
        public event System.Action<TacoOrder> OnOrderRemoved;

        [Header("Order Queue")]
        [SerializeField] private int maxQueueSize = GameConstants.MAX_QUEUE_SIZE;

        // Active orders
        public List<TacoOrder> activeOrders = new List<TacoOrder>();
        private int nextOrderId = 1;
        private float spawnTimer;
        private float currentSpawnInterval;

        // Difficulty scaling
        private float shiftProgress; // 0..1

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted += OnShiftStart;
                GameManager.Instance.OnShiftEnded += OnShiftEnd;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted -= OnShiftStart;
                GameManager.Instance.OnShiftEnded -= OnShiftEnd;
            }
        }

        private void Update() // chnaged by Akshay - now only handles patience updates, spawning is triggered by TacoQueueManager when customers arrive at the window
        {
            if (!GameManager.Instance.isShiftActive) return;

            // comment out the spawnTimer block below:
            /*
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && activeOrders.Count < maxQueueSize)
            {
                SpawnOrder();
                currentSpawnInterval = Mathf.Lerp(...);
                spawnTimer = currentSpawnInterval;
            }
            */

            // Keeping this here for reference, but actual spawning is now triggered by TacoQueueManager when customers arrive at the window
            UpdatePatience();
        }

        // ──────────────────────────────────────────────
        //  ORDER GENERATION //chnaged by Akshay 
        // ──────────────────────────────────────────────

        public void SpawnOrder()// called by TacoQueueManager when a customer arrives at the window (private to public)
        {
            TacoOrder order = new TacoOrder
            {
                orderId = nextOrderId++,
                tortillaType = RandomTortilla(),
                proteinType = RandomProtein(),
                toppings = RandomToppings(),
                salsas = RandomSalsas(),
                state = OrderState.Waiting,
                basePrice = 5f + shiftProgress * 3f, // prices go up at night
                tipMultiplier = 1f
            };

            activeOrders.Add(order);
            OnNewOrder?.Invoke(order);

            Debug.Log($"[Orders] New order #{order.orderId}: " +
                      $"{order.tortillaType}, {order.proteinType}, " +
                      $"{order.toppings.Count} toppings, {order.salsas.Count} salsas");
        }

        private IngredientType RandomTortilla()
        {
            return Random.value > 0.5f ? IngredientType.CornTortilla : IngredientType.FlourTortilla;
        }

        private IngredientType RandomProtein()
        {
            IngredientType[] proteins = {
                IngredientType.CarneAsada,
                IngredientType.Pollo,
                IngredientType.Carnitas,
                IngredientType.AlPastor
            };
            return proteins[Random.Range(0, proteins.Length)];
        }

        private List<IngredientType> RandomToppings()
        {
            IngredientType[] allToppings = {
                IngredientType.Cilantro,
                IngredientType.Onion,
                IngredientType.Lime,
                IngredientType.Cheese,
                IngredientType.Lettuce
            };

            var result = new List<IngredientType>();
            // 1-3 toppings, more later in shift
            int count = Random.Range(1, Mathf.Min(4, 2 + Mathf.FloorToInt(shiftProgress * 2)));
            var available = new List<IngredientType>(allToppings);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                result.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return result;
        }

        private List<IngredientType> RandomSalsas()
        {
            IngredientType[] allSalsas = {
                IngredientType.SalsaVerde,
                IngredientType.SalsaRoja,
                IngredientType.Guacamole,
                IngredientType.SourCream
            };

            var result = new List<IngredientType>();
            // 0-2 salsas
            int count = Random.Range(0, 3);
            var available = new List<IngredientType>(allSalsas);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                result.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return result;
        }

        // ──────────────────────────────────────────────
        //  PATIENCE
        // ──────────────────────────────────────────────

        private Dictionary<int, float> orderPatience = new Dictionary<int, float>();

        private void UpdatePatience()
        {
            var expiredOrders = new List<TacoOrder>();

            foreach (var order in activeOrders)
            {
                if (!orderPatience.ContainsKey(order.orderId))
                {
                    orderPatience[order.orderId] = GameConstants.BASE_PATIENCE;
                }

                orderPatience[order.orderId] -= Time.deltaTime;

                if (orderPatience[order.orderId] <= 0f)
                {
                    expiredOrders.Add(order);
                }
            }

            foreach (var order in expiredOrders)
            {
                GameManager.Instance.FailOrder(order);
                activeOrders.Remove(order);
                orderPatience.Remove(order.orderId);
                OnOrderRemoved?.Invoke(order);
                Debug.Log($"[Orders] Customer left! Order #{order.orderId} expired.");
            }
        }

        public float GetPatienceNormalized(int orderId)
        {
            if (orderPatience.ContainsKey(orderId))
                return Mathf.Clamp01(orderPatience[orderId] / GameConstants.BASE_PATIENCE);
            return 1f;
        }

        // ──────────────────────────────────────────────
        //  TACO SUBMISSION & SCORING
        // ──────────────────────────────────────────────

        public void TrySubmitTaco(List<IngredientType> assembled)
        {
            if (activeOrders.Count == 0)
            {
                Debug.Log("[Orders] No active orders to submit to!");
                return;
            }

            // Find the best matching order
            TacoOrder bestMatch = null;
            float bestAccuracy = 0f;

            foreach (var order in activeOrders)
            {
                float accuracy = CalculateAccuracy(order, assembled);
                if (accuracy > bestAccuracy)
                {
                    bestAccuracy = accuracy;
                    bestMatch = order;
                }
            }

            if (bestMatch != null && bestAccuracy >= 0.3f)
            {
                // Accept the taco
                activeOrders.Remove(bestMatch);
                orderPatience.Remove(bestMatch.orderId);
                GameManager.Instance.CompleteOrder(bestMatch, bestAccuracy);
                OnOrderRemoved?.Invoke(bestMatch);

                Debug.Log($"[Orders] Submitted taco matched order #{bestMatch.orderId} " +
                          $"with {bestAccuracy:P0} accuracy");
            }
            else
            {
                Debug.Log("[Orders] Submitted taco doesn't match any order well enough!");
            }
        }

        private float CalculateAccuracy(TacoOrder order, List<IngredientType> assembled)
        {
            var required = order.GetAllRequired();
            int totalRequired = required.Count;
            int matched = 0;

            var assembledCopy = new List<IngredientType>(assembled);

            foreach (var req in required)
            {
                if (assembledCopy.Contains(req))
                {
                    matched++;
                    assembledCopy.Remove(req);
                }
            }

            // Penalty for extra unwanted ingredients
            int extras = assembledCopy.Count;
            float extraPenalty = extras * 0.1f;

            float accuracy = (totalRequired > 0)
                ? (float)matched / totalRequired - extraPenalty
                : 0f;

            return Mathf.Clamp01(accuracy);
        }

        // ──────────────────────────────────────────────
        //  SHIFT LIFECYCLE
        // ──────────────────────────────────────────────

        private void OnShiftStart()
        {
            activeOrders.Clear();
            orderPatience.Clear();
            nextOrderId = 1;
            spawnTimer = 3f; // slight delay before first order
            currentSpawnInterval = GameConstants.ORDER_SPAWN_INTERVAL_BASE;
        }

        private void OnShiftEnd()
        {
            // Fail all remaining orders
            foreach (var order in activeOrders)
            {
                GameManager.Instance.FailOrder(order);
            }
            activeOrders.Clear();
            orderPatience.Clear();
        }
    }
}
