// GameHUD.cs — HUD for infinite mode
// Shows: score (money), wave, orders completed, fail count, order tickets, interact prompt.
// NO shift timer — replaced by wave counter.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TacoTornado.UI
{
    public class GameHUD : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        private static GameHUD _instance;
        public  static GameHUD Instance => _instance;

        private void Awake()
        {
            if (_instance == null) _instance = this;
            else { Destroy(gameObject); return; }
        }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Score & Status")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI ordersCompletedText;
        [SerializeField] private TextMeshProUGUI failCountText;       // "X / 5 failed"

        [Header("Interaction")]
        [SerializeField] private TextMeshProUGUI interactPromptText;
        [SerializeField] private Image           crosshair;

        [Header("Plate Status")]
        [Tooltip("Small text showing what's on the plate right now.")]
        [SerializeField] private TextMeshProUGUI plateStatusText;

        [Header("Order Tickets")]
        [SerializeField] private Transform  orderTicketContainer;
        [SerializeField] private GameObject orderTicketPrefab;

        [Header("Game Over Panel")]
        [SerializeField] private GameObject      gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TextMeshProUGUI gameOverSummaryText;

        [Header("Wave Announce")]
        [Tooltip("Brief full-screen text that flashes when a new wave starts.")]
        [SerializeField] private TextMeshProUGUI waveAnnounceText;

        // ── Runtime ───────────────────────────────────────────────────────────

        private Dictionary<int, GameObject> ticketObjects = new Dictionary<int, GameObject>();

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Start()
        {
            if (gameOverPanel    != null) gameOverPanel.SetActive(false);
            if (interactPromptText != null) interactPromptText.gameObject.SetActive(false);
            if (waveAnnounceText != null) waveAnnounceText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Invoke(nameof(SubscribeToEvents), 0f);
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted  += OnShiftStarted;
                GameManager.Instance.OnShiftEnded    += OnShiftEnded;
                GameManager.Instance.OnMoneyChanged  += UpdateMoney;
                GameManager.Instance.OnGameOver      += OnGameOver;
            }

            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnNewOrder      += AddOrderTicket;
                OrderManager.Instance.OnOrderRemoved  += RemoveOrderTicket;
                OrderManager.Instance.OnWaveChanged   += OnWaveChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted  -= OnShiftStarted;
                GameManager.Instance.OnShiftEnded    -= OnShiftEnded;
                GameManager.Instance.OnMoneyChanged  -= UpdateMoney;
                GameManager.Instance.OnGameOver      -= OnGameOver;
            }

            if (OrderManager.Instance != null)
            {
                OrderManager.Instance.OnNewOrder     -= AddOrderTicket;
                OrderManager.Instance.OnOrderRemoved -= RemoveOrderTicket;
                OrderManager.Instance.OnWaveChanged  -= OnWaveChanged;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.isShiftActive) return;

            UpdateStats();
            UpdateTicketPatience();
            UpdatePlateStatus();
        }

        // ── Stats ─────────────────────────────────────────────────────────────

        private void UpdateStats()
        {
            var gm = GameManager.Instance;

            if (moneyText          != null) moneyText.text          = $"${gm.money:F0}";
            if (ordersCompletedText != null) ordersCompletedText.text = $"✓ {gm.ordersCompleted}";

            if (failCountText != null)
            {
                failCountText.text  = $"✗ {gm.ordersFailed} / {gm.maxFailedOrders}";
                failCountText.color = gm.ordersFailed >= gm.maxFailedOrders - 1
                    ? Color.red
                    : Color.white;
            }

            if (waveText != null && OrderManager.Instance != null)
                waveText.text = $"Wave {OrderManager.Instance.currentWave}";
        }

        private void UpdateMoney(float amount)
        {
            if (moneyText != null) moneyText.text = $"${amount:F0}";
        }

        // ── Interact Prompt ───────────────────────────────────────────────────

        public void ShowInteractPrompt(string text)
        {
            if (interactPromptText == null) return;
            interactPromptText.gameObject.SetActive(true);
            interactPromptText.text = text;
        }

        public void HideInteractPrompt()
        {
            if (interactPromptText != null)
                interactPromptText.gameObject.SetActive(false);
        }

        // ── Plate Status ──────────────────────────────────────────────────────

        private void UpdatePlateStatus()
        {
            if (plateStatusText == null) return;

            // Find PlayerHands on camera
            var hands = FindFirstObjectByType<Player.PlayerHands>();
            if (hands == null || !hands.HasPlate())
            {
                plateStatusText.text = "";
                return;
            }

            plateStatusText.text = hands.GetPlate().GetStatusText();
        }

        // ── Order Tickets ─────────────────────────────────────────────────────

        private void AddOrderTicket(TacoOrder order)
        {
            if (orderTicketPrefab == null || orderTicketContainer == null) return;

            GameObject ticket = Instantiate(orderTicketPrefab, orderTicketContainer);
            ticketObjects[order.orderId] = ticket;

            var nameComp = ticket.transform.Find("txt_tacoName")?.GetComponent<TextMeshProUGUI>();
            if (nameComp != null)
                nameComp.text = $"#{order.orderId} {ShortName(order.proteinType)}";

            var ingComp = ticket.transform.Find("txt_ingredients")?.GetComponent<TextMeshProUGUI>();
            if (ingComp != null)
            {
                string s = ShortName(order.tortillaType) + "\n";
                foreach (var t in order.toppings) s += ShortName(t) + "\n";
                foreach (var sl in order.salsas)  s += ShortName(sl) + "\n";
                ingComp.text = s;
            }
        }

        private void RemoveOrderTicket(TacoOrder order)
        {
            if (ticketObjects.TryGetValue(order.orderId, out var t))
            {
                Destroy(t);
                ticketObjects.Remove(order.orderId);
            }
        }

        private void UpdateTicketPatience()
        {
            if (OrderManager.Instance == null) return;

            foreach (var order in OrderManager.Instance.activeOrders)
            {
                if (!ticketObjects.TryGetValue(order.orderId, out var ticket)) continue;

                var bar = ticket.transform.Find("PatienceBar");
                if (bar == null) continue;

                var img = bar.GetComponent<Image>();
                if (img == null) continue;

                float p  = OrderManager.Instance.GetPatienceNormalized(order.orderId);
                img.fillAmount = p;
                img.color      = Color.Lerp(Color.red, Color.green, p);
            }
        }

        // ── Wave Announce ─────────────────────────────────────────────────────

        private void OnWaveChanged(int wave)
        {
            if (waveAnnounceText == null) return;
            StopCoroutine(nameof(FlashWaveText));
            StartCoroutine(FlashWaveText(wave));
        }

        private System.Collections.IEnumerator FlashWaveText(int wave)
        {
            waveAnnounceText.gameObject.SetActive(true);
            waveAnnounceText.text  = $"WAVE {wave}";
            waveAnnounceText.alpha = 1f;

            yield return new WaitForSeconds(1.2f);

            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                waveAnnounceText.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            waveAnnounceText.gameObject.SetActive(false);
        }

        // ── Shift Events ──────────────────────────────────────────────────────

        private void OnShiftStarted()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            foreach (var kvp in ticketObjects) Destroy(kvp.Value);
            ticketObjects.Clear();
        }

        private void OnShiftEnded()
        {
            // Game over panel is shown via OnGameOver if it was a lose
            // If somehow EndShift is called without OnGameOver (shouldn't happen in infinite mode)
            // just silently end
        }

        private void OnGameOver(string reason)
        {
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);

            var gm = GameManager.Instance;

            if (gameOverTitleText != null)
                gameOverTitleText.text = "GAME OVER";

            if (gameOverSummaryText != null)
            {
                int wave = OrderManager.Instance != null ? OrderManager.Instance.currentWave : 0;
                gameOverSummaryText.text =
                    $"{reason}\n\n" +
                    $"Wave Reached:      {wave}\n" +
                    $"Orders Served:     {gm.ordersCompleted}\n" +
                    $"Perfect Orders:    {gm.perfectOrders}\n\n" +
                    $"Revenue:  ${gm.shiftRevenue:F2}\n" +
                    $"Tips:     ${gm.shiftTips:F2}\n" +
                    $"Total:    ${gm.GetShiftProfit():F2}";
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string ShortName(IngredientType type)
        {
            switch (type)
            {
                case IngredientType.CornTortilla:  return "Corn";
                case IngredientType.FlourTortilla: return "Flour";
                case IngredientType.AlPastor:      return "Al Pastor";
                case IngredientType.Discada:       return "Discada";
                case IngredientType.Cilantro:      return "Cilantro";
                case IngredientType.Onion:         return "Onion";
                case IngredientType.SalsaVerde:    return "S. Verde";
                case IngredientType.SalsaRoja:     return "S. Roja";
                default:                           return type.ToString();
            }
        }
    }
}
