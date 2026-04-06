// UIshiftEnd.cs — Shift end / Game Over panel with working buttons
// Attach to the Panel_ShiftEnd GameObject
// NOTE: This class is intentionally NOT in a namespace because the scene
// references it as "Assembly-CSharp::UIshiftEnd" (no namespace).

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TacoTornado; // for GameManager access

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