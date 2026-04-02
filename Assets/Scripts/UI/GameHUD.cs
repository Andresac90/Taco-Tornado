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
                GameManager.Instance.OnGameOver += OnGameOver; // Listen for game over to show shift end panel [Changed by Akshay]
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
                GameManager.Instance.OnGameOver -= OnGameOver; // Stop listening for game over [Changed by Akshay]
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
                ordersCompletedText.text = $" {GameManager.Instance.ordersCompleted}";//CHANGED by Akshay
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


            // Changed by Akshay
            // Build ticket text
            // Find the Taco Name component specifically
            var nameComponent = ticket.transform.Find("txt_tacoName")?.GetComponent<TextMeshProUGUI>();
            if (nameComponent != null)
            {
                // Sets the Header (Order # and Protein)
                nameComponent.text = $"#{order.orderId} {ShortName(order.proteinType)}";
            }

            // Find the Ingredients component specifically
            var ingredientsComponent = ticket.transform.Find("txt_ingredients")?.GetComponent<TextMeshProUGUI>();
            if (ingredientsComponent != null)
            {
                string ingredientsList = "";
                ingredientsList += ShortName(order.tortillaType) + "\n";

                foreach (var t in order.toppings)
                    ingredientsList += ShortName(t) + "\n";

                foreach (var s in order.salsas)
                    ingredientsList += ShortName(s) + "\n";

                ingredientsComponent.text = ingredientsList;
            }
            // End of changes by Akshay


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



        // New method to handle Game Over scenario [Added by Akshay]
        private void OnGameOver(string reason)
        {
            if (shiftEndPanel == null) return;

            shiftEndPanel.SetActive(true);

            // Update the panel to show Game Over instead of Shift Complete
            if (shiftSummaryText != null)
            {
                shiftSummaryText.text = $"<color=red>GAME OVER</color>\n\n" +
                                       $"REASON: {reason}\n\n" +
                                       $"Final Balance: ${GameManager.Instance.money:F2}";
            }

            // You can also access the gameOver text component if you have a direct reference
            var shiftEndUI = shiftEndPanel.GetComponent<UIshiftEnd>();
            if (shiftEndUI != null && shiftEndUI.gameOver != null)
            {
                shiftEndUI.gameOver.text = "GAME OVER";
            }
        }
        //End of new method [Added by Akshay]




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
