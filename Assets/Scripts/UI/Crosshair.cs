using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair settings")]
    public Image crosshairImage;
    public float defaultSize      = 16f;
    public float interactSize     = 22f;
    public float interactDistance = 2.5f;

    // Colors are hardcoded — do NOT rely on Inspector for these
    // because the Inspector default was black/transparent causing the invisible crosshair bug.
    private static readonly Color DEFAULT_COLOR  = new Color(1f, 1f, 1f, 1f);
    private static readonly Color INTERACT_COLOR = new Color(1f, 0.85f, 0f, 1f);

    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
        if (crosshairImage == null) crosshairImage = GetComponent<Image>();

        if (crosshairImage != null)
        {
            crosshairImage.gameObject.SetActive(true);
            crosshairImage.rectTransform.anchoredPosition = Vector2.zero;
            crosshairImage.rectTransform.sizeDelta = new Vector2(defaultSize, defaultSize);
            crosshairImage.color = DEFAULT_COLOR; // always start white
        }
    }

    void Update()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null || crosshairImage == null) return;

        if (!crosshairImage.gameObject.activeSelf)
            crosshairImage.gameObject.SetActive(true);

        bool hit = CheckForInteractable();

        // Smooth size transition
        float target = hit ? interactSize : defaultSize;
        crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(
            crosshairImage.rectTransform.sizeDelta,
            new Vector2(target, target),
            Time.deltaTime * 18f);

        // Hard color set — never lerps to transparent
        crosshairImage.color = hit ? INTERACT_COLOR : DEFAULT_COLOR;
    }

    bool CheckForInteractable()
    {
        if (playerCamera == null) return false;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, interactDistance)
               && hit.collider.gameObject.layer == 8;
    }
}