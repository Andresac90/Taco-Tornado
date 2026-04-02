// GameHUD.cs — In-game heads-up display
// Attach to a Canvas (Screen Space - Overlay)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TacoTornado.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("Timer & Money")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI ordersCompletedText;

        [Header("Interaction")]
        [SerializeField] private TextMeshProUGUI interactPromptText;
        [SerializeField] private Image crosshair;

        [Header("Order Tickets")]
        [SerializeField] private Transform orderTicketContainer; // Horizontal layout group
        [SerializeField] private GameObject orderTicketPrefab;

        [Header("Shift End Panel")]
        [SerializeField] private GameObject shiftEndPanel;
        [SerializeField] private TextMeshProUGUI shiftSummaryText;

        private Dictionary<int, GameObject> ticketObjects = new Dictionary<int, GameObject>();

        #region Singleton
        private static GameHUD _instance;
        public static GameHUD Instance => _instance;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion
        private void Start()
        {
            if (shiftEndPanel != null)
                shiftEndPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted += OnShiftStarted;
                GameManager.Instance.OnShiftEnded += OnShiftEnded;
                GameManager.Instance.OnMoneyChanged += UpdateMoney;
            }
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnNewOrder += AddOrderTicket;
                OrderManager.Instance.OnOrderRemoved += RemoveOrderTicket;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted -= OnShiftStarted;
                GameManager.Instance.OnShiftEnded -= OnShiftEnded;
                GameManager.Instance.OnMoneyChanged -= UpdateMoney;
            }
            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnNewOrder -= AddOrderTicket;
                OrderManager.Instance.OnOrderRemoved -= RemoveOrderTicket;
            }
        }

        private void Update()
        {
            if (!GameManager.Instance.isShiftActive) return;

            UpdateTimer();
            UpdateOrdersCompleted();
            UpdateTicketPatience();
        }

        // ──────────────────────────────────────────────
        //  TIMER & MONEY
        // ──────────────────────────────────────────────

        private void UpdateTimer()
        {
            if (timerText == null) return;
            float t = GameManager.Instance.shiftTimer;
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Flash red in last 30 seconds
            timerText.color = t < 30f ? Color.red : Color.white;
        }

        private void UpdateMoney(float amount)
        {
            if (moneyText != null)
                moneyText.text = $"${amount:F2}";
        }

        private void UpdateOrdersCompleted()
        {
            if (ordersCompletedText != null)
                ordersCompletedText.text = $"Served: {GameManager.Instance.ordersCompleted}";
        }

        // ──────────────────────────────────────────────
        //  INTERACTION PROMPT
        // ──────────────────────────────────────────────

        public void ShowInteractPrompt(string text)
        {
            if (interactPromptText != null)
            {
                interactPromptText.gameObject.SetActive(true);
                interactPromptText.text = text;
            }
        }

        public void HideInteractPrompt()
        {
            if (interactPromptText != null)
                interactPromptText.gameObject.SetActive(false);
        }

        // ──────────────────────────────────────────────
        //  ORDER TICKETS
        // ──────────────────────────────────────────────

        private void AddOrderTicket(TacoOrder order)
        {
            if (orderTicketPrefab == null || orderTicketContainer == null) return;

            GameObject ticket = Instantiate(orderTicketPrefab, orderTicketContainer);
            ticketObjects[order.orderId] = ticket;

            // Build ticket text
            var textComponent = ticket.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                string ingredients = "";
                ingredients += ShortName(order.tortillaType) + "\n";
                ingredients += ShortName(order.proteinType) + "\n";
                foreach (var t in order.toppings)
                    ingredients += ShortName(t) + "\n";
                foreach (var s in order.salsas)
                    ingredients += ShortName(s) + "\n";

                textComponent.text = $"#{order.orderId}\n{ingredients}";
            }

            // Patience bar (child named "PatienceBar")
            var bar = ticket.transform.Find("PatienceBar");
            if (bar != null)
            {
                var image = bar.GetComponent<Image>();
                if (image != null) image.fillAmount = 1f;
            }
        }

        private void RemoveOrderTicket(TacoOrder order)
        {
            if (ticketObjects.ContainsKey(order.orderId))
            {
                Destroy(ticketObjects[order.orderId]);
                ticketObjects.Remove(order.orderId);
            }
        }

        private void UpdateTicketPatience()
        {
            if (OrderManager.Instance == null) return;

            foreach (var order in OrderManager.Instance.activeOrders)
            {
                if (ticketObjects.ContainsKey(order.orderId))
                {
                    var bar = ticketObjects[order.orderId].transform.Find("PatienceBar");
                    if (bar != null)
                    {
                        var image = bar.GetComponent<Image>();
                        if (image != null)
                        {
                            float patience = OrderManager.Instance.GetPatienceNormalized(order.orderId);
                            image.fillAmount = patience;
                            image.color = Color.Lerp(Color.red, Color.green, patience);
                        }
                    }
                }
            }
        }

        // ──────────────────────────────────────────────
        //  SHIFT END
        // ──────────────────────────────────────────────

        private void OnShiftStarted()
        {
            if (shiftEndPanel != null)
                shiftEndPanel.SetActive(false);

            // Clear old tickets
            foreach (var kvp in ticketObjects)
                Destroy(kvp.Value);
            ticketObjects.Clear();
        }

        private void OnShiftEnded()
        {
            if (shiftEndPanel == null) return;

            shiftEndPanel.SetActive(true);

            var gm = GameManager.Instance;
            if (shiftSummaryText != null)
            {
                shiftSummaryText.text =
                    $"SHIFT COMPLETE — Day {gm.currentDay}\n\n" +
                    $"Orders Served:  {gm.ordersCompleted}\n" +
                    $"Orders Failed:  {gm.ordersFailed}\n" +
                    $"Perfect Orders: {gm.perfectOrders}\n\n" +
                    $"Revenue:  ${gm.shiftRevenue:F2}\n" +
                    $"Tips:     ${gm.shiftTips:F2}\n" +
                    $"Total:    ${gm.GetShiftProfit():F2}\n\n" +
                    $"Balance:  ${gm.money:F2}";
            }
        }

        // ──────────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────────

        private string ShortName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.CornTortilla:  return "Corn";
                case IngredientType.FlourTortilla: return "Flour";
                case IngredientType.CarneAsada:    return "Asada";
                case IngredientType.Pollo:         return "Pollo";
                case IngredientType.Carnitas:      return "Carnitas";
                case IngredientType.AlPastor:      return "Pastor";
                case IngredientType.Cilantro:      return "Cilantro";
                case IngredientType.Onion:         return "Onion";
                case IngredientType.Lime:          return "Lime";
                case IngredientType.Cheese:        return "Cheese";
                case IngredientType.Lettuce:       return "Lettuce";
                case IngredientType.SalsaVerde:    return "S. Verde";
                case IngredientType.SalsaRoja:     return "S. Roja";
                case IngredientType.Guacamole:     return "Guac";
                case IngredientType.SourCream:     return "Cream";
                default: return type.ToString();
            }
        }
    }
}
