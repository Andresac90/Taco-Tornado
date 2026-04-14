// CameraEffects.cs — Screen shake, pickup bob, and UI flash feedback
// Attach to the Camera GameObject alongside FirstPersonLook and PlayerInteraction.
//
// How to trigger shakes from other scripts:
//   CameraEffects.Instance.Shake(0.15f, 0.25f);   // magnitude, duration
//   CameraEffects.Instance.PlayPickupBob();
//   CameraEffects.Instance.PlayServePulse();

using System.Collections;
using UnityEngine;

namespace TacoTornado.Player
{
    public class CameraEffects : MonoBehaviour
    {
        public static CameraEffects Instance { get; private set; }

        [Header("Shake Settings")]
        [SerializeField] private float defaultShakeMagnitude = 0.08f;
        [SerializeField] private float defaultShakeDuration  = 0.18f;

        [Header("Pickup Bob Settings")]
        [SerializeField] private float bobMagnitude  = 0.04f;
        [SerializeField] private float bobFrequency  = 14f;
        [SerializeField] private float bobDuration   = 0.22f;

        // Internal state
        private Vector3 originLocalPos;
        private bool isBobbing = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            originLocalPos = transform.localPosition;
        }

        // ──────────────────────────────────────────────
        //  PUBLIC API
        // ──────────────────────────────────────────────

        /// <summary>Trigger a trauma-style camera shake.</summary>
        public void Shake(float magnitude = -1f, float duration = -1f)
        {
            if (magnitude < 0) magnitude = defaultShakeMagnitude;
            if (duration  < 0) duration  = defaultShakeDuration;

            StopCoroutine(nameof(ShakeRoutine));
            StartCoroutine(ShakeRoutine(magnitude, duration));
        }

        /// <summary>Quick downward bob on pickup — feels like "grabbing weight".</summary>
        public void PlayPickupBob()
        {
            if (isBobbing) return;
            StartCoroutine(BobRoutine());
        }

        /// <summary>Subtle punch on serve — satisfying confirm.</summary>
        public void PlayServePulse()
        {
            Shake(0.05f, 0.12f);
        }

        /// <summary>Harder shake for a burnt/failed event.</summary>
        public void PlayFailShake()
        {
            Shake(0.18f, 0.35f);
        }

        // ──────────────────────────────────────────────
        //  COROUTINES
        // ──────────────────────────────────────────────

        private IEnumerator ShakeRoutine(float magnitude, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = 1f - (elapsed / duration); // decay
                Vector3 offset = Random.insideUnitSphere * magnitude * t;
                offset.z = 0f; // no z shake — avoids clipping into geometry

                transform.localPosition = originLocalPos + offset;

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originLocalPos;
        }

        private IEnumerator BobRoutine()
        {
            isBobbing = true;
            float elapsed = 0f;

            while (elapsed < bobDuration)
            {
                float t = elapsed / bobDuration;
                float offset = Mathf.Sin(t * bobFrequency) * bobMagnitude * (1f - t);
                transform.localPosition = originLocalPos + Vector3.down * offset;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originLocalPos;
            isBobbing = false;
        }
    }
}
