using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair settings")]
    public Image crosshairImage;
    public float defaultSize     = 20f;
    public float interactSize    = 26f;
    public float interactDistance = 2.5f; // match PlayerHands interactRange

    [Header("Crosshair color settings")]
    public Color defaultColor  = Color.white;
    public Color interactColor = Color.yellow;

    private Camera playerCamera;
    private float  targetSize;
    private Color  targetColor;

    void Start()
    {
        playerCamera = Camera.main;

        if (crosshairImage == null)
            crosshairImage = GetComponent<Image>();

        targetSize  = defaultSize;
        targetColor = defaultColor;

        if (crosshairImage != null)
        {
            RectTransform rect = crosshairImage.rectTransform;
            rect.anchoredPosition = Vector2.zero;
            // Make sure it's visible from the start
            crosshairImage.gameObject.SetActive(true);
            crosshairImage.color = defaultColor;
        }
    }

    void Update()
    {
        // Re-find camera if lost (can happen after scene reload)
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || crosshairImage == null) return;

        bool canInteract = CheckForInteractable();

        targetSize  = canInteract ? interactSize  : defaultSize;
        targetColor = canInteract ? interactColor : defaultColor;

        UpdateCrosshairAppearance();
    }

    bool CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            // Layer 8 = Interactable
            if (hit.collider.gameObject.layer == 8)
                return true;
        }
        return false;
    }

    void UpdateCrosshairAppearance()
    {
        RectTransform rect = crosshairImage.rectTransform;
        float currentSize  = rect.sizeDelta.x;
        float newSize      = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * 15f);
        rect.sizeDelta     = new Vector2(newSize, newSize);

        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * 15f);
    }
}