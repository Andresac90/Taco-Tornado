// UIshiftEnd.cs — Shift end / Game Over panel with working buttons
// Attach to the Panel_ShiftEnd GameObject

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacoTornado.UI
{
    public class UIshiftEnd : MonoBehaviour
    {
        public Button nextShift;
        public Button backToMain;
        public Button retry;

        public TMP_Text gameOver;
        public TMP_Text shiftSummaryText;

        private void Awake()
        {
            if (nextShift != null)
                nextShift.onClick.AddListener(OnNextShift);
            if (backToMain != null)
                backToMain.onClick.AddListener(OnBackToMain);
            if (retry != null)
                retry.onClick.AddListener(OnRetry);
        }

        private void OnNextShift()
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentDay++;
                GameManager.Instance.StartShift();
            }
        }

        private void OnBackToMain()
        {
            // For the prototype, reload the scene (resets everything)
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void OnRetry()
        {
            gameObject.SetActive(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartShift();
            }
        }

        void Start()
        {
            gameObject.SetActive(false);
        }
    }
}
