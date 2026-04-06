// ShiftStarter.cs — Auto-starts the first shift after a short delay
// Replaces SceneBootstrap's auto-start functionality
// Attach to the GameManager GameObject (or any persistent object in the scene)

using UnityEngine;

namespace TacoTornado
{
    public class ShiftStarter : MonoBehaviour
    {
        [SerializeField] private float startDelay = 0.5f;

        private void Start()
        {
            Invoke(nameof(BeginShift), startDelay);
        }

        private void BeginShift()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartShift();
                Debug.Log("[ShiftStarter] Shift auto-started.");
            }
            else
            {
                Debug.LogError("[ShiftStarter] GameManager.Instance is null! " +
                    "Make sure a GameManager GameObject with GameManager.cs exists in the scene.");
            }
        }
    }
}