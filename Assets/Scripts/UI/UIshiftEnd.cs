using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIshiftEnd : MonoBehaviour
{
    public Button nextShift;
    public Button backToMain;
    public Button retry;

    public TMP_Text gameOver;

    private void Awake()
    {
        nextShift.onClick.AddListener(() => print("Here for next shift"));
        backToMain.onClick.AddListener(() => print("Here for backToMain"));
        retry.onClick.AddListener(() => print("Here for retry"));
    }
    void Start()
    {
        this.gameObject.SetActive(false);
    }

}
