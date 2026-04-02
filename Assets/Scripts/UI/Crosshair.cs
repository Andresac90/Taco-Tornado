using TacoTornado.Player;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Crosshair settings")]
    public Image crosshairImage;        
    public float defaultSize = 20f;     
    public float interactSize = 25f;    
    public float interactDistance = 3f; 

    [Header("Crosshair color settings")]
    public Color defaultColor = Color.white;
    public Color interactColor = Color.red;

    private Camera playerCamera;
    private float targetSize;
    private Color targetColor;

    void Start()
    {
        playerCamera = Camera.main;

        // try to find Image component if not assigned
        if (crosshairImage == null)
            crosshairImage = GetComponent<Image>();

        // initialize values
        targetSize = defaultSize;
        targetColor = defaultColor;

        // set initial position to center
        RectTransform rect = crosshairImage.rectTransform;
        rect.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        bool canInteract = CheckForInteractable();

        if (canInteract)
        {
            targetSize = interactSize;
            targetColor = interactColor;
        }
        else
        {
            targetSize = defaultSize;
            targetColor = defaultColor;
        }

        UpdateCrosshairAppearance();
    }

    bool CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.CompareTag("Interactable") ||
                hit.collider.GetComponent<IInteractable>() != null)
            {
                return true;
            }
        }
        return false;
    }

    void UpdateCrosshairAppearance()
    {
        if (crosshairImage == null) return;

        // lerp size for smooth transition
        RectTransform rect = crosshairImage.rectTransform;
        float currentSize = rect.sizeDelta.x;
        float newSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * 15f);
        rect.sizeDelta = new Vector2(newSize, newSize);

        // lerp color for smooth transition
        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * 15f);
    }
}