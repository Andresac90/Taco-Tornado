// AudioManager.cs — Centralized audio system for Taco Tornado
// Manages BGM, cooking sounds, ingredient SFX, UI feedback, and ambience.
//
// SETUP:
// 1. Create empty GameObject named "AudioManager" in scene
// 2. Add this script
// 3. Assign all AudioClip fields in Inspector
// 4. The manager will persist across scenes (DontDestroyOnLoad)

using System.Collections;
using UnityEngine;

namespace TacoTornado
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource grillLoopSource;

        // ── MUSIC ─────────────────────────────────────────────────────────────
        [Header("Background Music")]
        [SerializeField] private AudioClip bgmMainMenu;
        [SerializeField] private AudioClip bgmGameplay;
        [SerializeField] private AudioClip bgmGameOver;
        [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.6f;

        // ── COOKING SOUNDS ────────────────────────────────────────────────────
        [Header("Grill & Cooking")]
        [SerializeField] private AudioClip grillSizzleLoop;        // Continuous when items on grill
        [SerializeField] private AudioClip placeOnGrill;           // Meat hits grill
        [SerializeField] private AudioClip pickupFromGrill;        // Grab cooked meat
        [SerializeField] private AudioClip foodCooked;             // Ding! Meat is done
        [SerializeField] private AudioClip foodBurnt;              // Overcook warning
        [SerializeField][Range(0f, 1f)] private float grillVolume = 0.5f;

        [Header("Chopping & Prep")]
        [SerializeField] private AudioClip chopSound;              // Knife chop (lettuce, tomato)
        [SerializeField] private AudioClip[] chopVariations;       // Multiple chop sounds for variety
        [SerializeField][Range(0f, 1f)] private float chopVolume = 0.7f;

        // ── INGREDIENT SOUNDS ─────────────────────────────────────────────────
        [Header("Ingredient Handling")]
        [SerializeField] private AudioClip grabIngredient;         // Pick up from source
        [SerializeField] private AudioClip placeOnPlate;           // Add to taco
        [SerializeField] private AudioClip platePickup;            // Grab empty plate
        [SerializeField] private AudioClip plateDrop;              // Set down plate
        [SerializeField][Range(0f, 1f)] private float ingredientVolume = 0.6f;

        // ── SPECIFIC INGREDIENT SOUNDS ────────────────────────────────────────
        [Header("Ingredient-Specific SFX")]
        [SerializeField] private AudioClip tortillaGrab;           // Soft wrap sound
        [SerializeField] private AudioClip meatGrab;               // Raw meat squish
        [SerializeField] private AudioClip veggieGrab;             // Crisp veggie sound
        [SerializeField] private AudioClip sauceSquirt;            // Liquid salsa

        // ── TRASH & FAILURES ──────────────────────────────────────────────────
        [Header("Trash & Mistakes")]
        [SerializeField] private AudioClip trashToss;              // Item thrown in bin
        [SerializeField] private AudioClip trashBinLid;            // Bin lid close
        [SerializeField] private AudioClip mistake;                // Wrong ingredient
        [SerializeField][Range(0f, 1f)] private float trashVolume = 0.7f;

        // ── ORDER & DELIVERY ──────────────────────────────────────────────────
        [Header("Orders & Delivery")]
        [SerializeField] private AudioClip orderReceived;          // New ticket arrives
        [SerializeField] private AudioClip orderDeliver;           // Hand off taco
        [SerializeField] private AudioClip orderPerfect;           // 100% accuracy!
        [SerializeField] private AudioClip orderFailed;            // Timeout/reject
        [SerializeField] private AudioClip tipEarned;              // Ka-ching!
        [SerializeField][Range(0f, 1f)] private float orderVolume = 0.8f;

        // ── UI SOUNDS ─────────────────────────────────────────────────────────
        [Header("UI Feedback")]
        [SerializeField] private AudioClip uiClick;                // Button press
        [SerializeField] private AudioClip uiHover;                // Button hover
        [SerializeField] private AudioClip uiConfirm;              // Positive action
        [SerializeField] private AudioClip uiCancel;               // Back/cancel
        [SerializeField] private AudioClip pauseIn;                // Game paused
        [SerializeField] private AudioClip pauseOut;               // Resume
        [SerializeField][Range(0f, 1f)] private float uiVolume = 0.5f;

        // ── AMBIENCE ──────────────────────────────────────────────────────────
        [Header("Ambience & Environment")]
        [SerializeField] private AudioClip kitchenAmbience;        // Background kitchen noise
        [SerializeField] private AudioClip customerChatter;        // Crowd murmur
        [SerializeField] private AudioClip truckEngine;            // Truck idle (if outdoor)
        [SerializeField][Range(0f, 1f)] private float ambienceVolume = 0.3f;

        // ── SPECIAL EVENTS ────────────────────────────────────────────────────
        [Header("Special Events")]
        [SerializeField] private AudioClip shiftStart;             // Shift begins
        [SerializeField] private AudioClip shiftEnd;               // Shift ends
        [SerializeField] private AudioClip rushModeStart;          // Rush hour begins
        [SerializeField] private AudioClip countdown;              // Last 10 seconds
        [SerializeField] private AudioClip gameOverStinger;        // Defeat sound

        // ── MASTER VOLUME ─────────────────────────────────────────────────────
        [Header("Master Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float masterSFXVolume = 1f;

        private Coroutine bgmFadeCoroutine;

        // ══════════════════════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ══════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Create audio sources if not assigned
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (ambienceSource == null)
            {
                ambienceSource = gameObject.AddComponent<AudioSource>();
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = false;
            }

            if (grillLoopSource == null)
            {
                grillLoopSource = gameObject.AddComponent<AudioSource>();
                grillLoopSource.loop = true;
                grillLoopSource.playOnAwake = false;
            }

            UpdateAllVolumes();
        }

        private void Start()
        {
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted += OnShiftStarted;
                GameManager.Instance.OnShiftEnded += OnShiftEnded;
                GameManager.Instance.OnOrderCompleted += OnOrderCompleted;
                GameManager.Instance.OnOrderFailed += OnOrderFailed;
                GameManager.Instance.OnGameOver += OnGameOver;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnShiftStarted -= OnShiftStarted;
                GameManager.Instance.OnShiftEnded -= OnShiftEnded;
                GameManager.Instance.OnOrderCompleted -= OnOrderCompleted;
                GameManager.Instance.OnOrderFailed -= OnOrderFailed;
                GameManager.Instance.OnGameOver -= OnGameOver;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // BACKGROUND MUSIC
        // ══════════════════════════════════════════════════════════════════════

        public void PlayBGM_MainMenu()
        {
            CrossfadeBGM(bgmMainMenu, bgmVolume);
        }

        public void PlayBGM_Gameplay()
        {
            CrossfadeBGM(bgmGameplay, bgmVolume);
        }

        public void PlayBGM_GameOver()
        {
            CrossfadeBGM(bgmGameOver, bgmVolume * 0.8f);
        }

        public void StopBGM(float fadeTime = 1f)
        {
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
        }

        private void CrossfadeBGM(AudioClip newClip, float volume, float fadeTime = 1f)
        {
            if (bgmSource.clip == newClip && bgmSource.isPlaying) return;

            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(CrossfadeBGMCoroutine(newClip, volume, fadeTime));
        }

        private IEnumerator CrossfadeBGMCoroutine(AudioClip newClip, float targetVolume, float fadeTime)
        {
            // Fade out current
            float startVol = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime / 2f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / (fadeTime / 2f));
                yield return null;
            }

            // Switch clip
            bgmSource.Stop();
            bgmSource.clip = newClip;
            bgmSource.volume = 0f;
            bgmSource.Play();

            // Fade in new
            elapsed = 0f;
            while (elapsed < fadeTime / 2f)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume * masterVolume, elapsed / (fadeTime / 2f));
                yield return null;
            }

            bgmSource.volume = targetVolume * masterVolume;
        }

        private IEnumerator FadeOutBGM(float fadeTime)
        {
            float startVol = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeTime);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.volume = 0f;
        }

        // ══════════════════════════════════════════════════════════════════════
        // COOKING SOUNDS
        // ══════════════════════════════════════════════════════════════════════

        public void PlayPlaceOnGrill()
        {
            PlaySFX(placeOnGrill, grillVolume);
        }

        public void PlayPickupFromGrill()
        {
            PlaySFX(pickupFromGrill, grillVolume);
        }

        public void PlayFoodCooked()
        {
            PlaySFX(foodCooked, grillVolume * 1.2f);
        }

        public void PlayFoodBurnt()
        {
            PlaySFX(foodBurnt, grillVolume * 1.3f);
        }

        public void StartGrillSizzle()
        {
            if (grillLoopSource.isPlaying) return;
            grillLoopSource.clip = grillSizzleLoop;
            grillLoopSource.volume = grillVolume * masterVolume * masterSFXVolume;
            grillLoopSource.Play();
        }

        public void StopGrillSizzle()
        {
            if (grillLoopSource.isPlaying)
                StartCoroutine(FadeOutAudioSource(grillLoopSource, 0.5f));
        }

        public void PlayChop()
        {
            if (chopVariations != null && chopVariations.Length > 0)
            {
                AudioClip clip = chopVariations[Random.Range(0, chopVariations.Length)];
                PlaySFX(clip, chopVolume);
            }
            else
            {
                PlaySFX(chopSound, chopVolume);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // INGREDIENT SOUNDS
        // ══════════════════════════════════════════════════════════════════════

        public void PlayGrabIngredient(IngredientType type)
        {
            AudioClip clip = GetIngredientGrabClip(type);
            PlaySFX(clip ?? grabIngredient, ingredientVolume);
        }

        public void PlayPlaceOnPlate(IngredientType type)
        {
            AudioClip clip = GetIngredientPlaceClip(type);
            PlaySFX(clip ?? placeOnPlate, ingredientVolume);
        }

        public void PlayPlatePickup()
        {
            PlaySFX(platePickup, ingredientVolume);
        }

        public void PlayPlateDrop()
        {
            PlaySFX(plateDrop, ingredientVolume);
        }

        private AudioClip GetIngredientGrabClip(IngredientType type)
        {
            IngredientCategory cat = IngredientData.GetCategory(type);
            switch (cat)
            {
                case IngredientCategory.Tortilla:
                    return tortillaGrab;

                case IngredientCategory.Protein:
                    return meatGrab;

                case IngredientCategory.Topping:
                    return veggieGrab;

                case IngredientCategory.Salsa:
                    return sauceSquirt;

                default:
                    return null;
            }
        }

        private AudioClip GetIngredientPlaceClip(IngredientType type)
        {
            IngredientCategory cat = IngredientData.GetCategory(type);
            if (cat == IngredientCategory.Salsa)
                return sauceSquirt;

            return null;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TRASH SOUNDS
        // ══════════════════════════════════════════════════════════════════════

        public void PlayTrashToss()
        {
            PlaySFX(trashToss, trashVolume);
        }

        public void PlayTrashBinLid()
        {
            PlaySFX(trashBinLid, trashVolume * 0.8f);
        }

        public void PlayMistake()
        {
            PlaySFX(mistake, trashVolume);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ORDER & DELIVERY SOUNDS
        // ══════════════════════════════════════════════════════════════════════

        public void PlayOrderReceived()
        {
            PlaySFX(orderReceived, orderVolume);
        }

        public void PlayOrderDeliver()
        {
            PlaySFX(orderDeliver, orderVolume);
        }

        private void OnOrderCompleted(TacoOrder order)
        {
            // Determine quality of completion
            float accuracy = CalculateOrderAccuracy(order);

            if (accuracy >= 1f)
            {
                PlaySFX(orderPerfect, orderVolume * 1.2f);
                PlaySFX(tipEarned, orderVolume * 0.8f);
            }
            else if (accuracy >= 0.75f)
            {
                PlayOrderDeliver();
                PlaySFX(tipEarned, orderVolume * 0.6f);
            }
            else
            {
                PlayOrderDeliver();
            }
        }

        private void OnOrderFailed(TacoOrder order)
        {
            PlaySFX(orderFailed, orderVolume * 1.1f);
        }

        private float CalculateOrderAccuracy(TacoOrder order)
        {
            // Simple accuracy estimation - you may have better logic in your game
            return 0.9f; // Placeholder
        }

        // ══════════════════════════════════════════════════════════════════════
        // UI SOUNDS
        // ══════════════════════════════════════════════════════════════════════

        public void PlayUIClick()
        {
            PlaySFX(uiClick, uiVolume);
        }

        public void PlayUIHover()
        {
            PlaySFX(uiHover, uiVolume * 0.5f);
        }

        public void PlayUIConfirm()
        {
            PlaySFX(uiConfirm, uiVolume);
        }

        public void PlayUICancel()
        {
            PlaySFX(uiCancel, uiVolume);
        }

        public void PlayPauseIn()
        {
            PlaySFX(pauseIn, uiVolume);
        }

        public void PlayPauseOut()
        {
            PlaySFX(pauseOut, uiVolume);
        }

        // ══════════════════════════════════════════════════════════════════════
        // AMBIENCE
        // ══════════════════════════════════════════════════════════════════════

        public void StartKitchenAmbience()
        {
            PlayAmbience(kitchenAmbience);
        }

        public void StartCustomerChatter()
        {
            if (customerChatter == null) return;
            AudioSource chatter = gameObject.AddComponent<AudioSource>();
            chatter.clip = customerChatter;
            chatter.loop = true;
            chatter.volume = ambienceVolume * 0.7f * masterVolume;
            chatter.Play();
        }

        public void StopAmbience()
        {
            if (ambienceSource.isPlaying)
                StartCoroutine(FadeOutAudioSource(ambienceSource, 1f));
        }

        private void PlayAmbience(AudioClip clip)
        {
            if (clip == null) return;
            ambienceSource.clip = clip;
            ambienceSource.volume = ambienceVolume * masterVolume;
            ambienceSource.Play();
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPECIAL EVENTS
        // ══════════════════════════════════════════════════════════════════════

        private void OnShiftStarted()
        {
            PlaySFX(shiftStart, orderVolume);
            PlayBGM_Gameplay();
            StartKitchenAmbience();
        }

        private void OnShiftEnded()
        {
            PlaySFX(shiftEnd, orderVolume);
            StopGrillSizzle();
            StopAmbience();
        }

        private void OnGameOver(string reason)
        {
            PlaySFX(gameOverStinger, orderVolume * 1.2f);
            PlayBGM_GameOver();
        }

        public void PlayRushModeStart()
        {
            PlaySFX(rushModeStart, orderVolume);
        }

        public void PlayCountdown()
        {
            PlaySFX(countdown, orderVolume * 1.1f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CORE SFX PLAYBACK
        // ══════════════════════════════════════════════════════════════════════

        private void PlaySFX(AudioClip clip, float volume)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, volume * masterVolume * masterSFXVolume);
        }

        public void PlaySFX(AudioClip clip)
        {
            PlaySFX(clip, 1f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // VOLUME CONTROL
        // ══════════════════════════════════════════════════════════════════════

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetMasterSFXVolume(float volume)
        {
            masterSFXVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            bgmSource.volume = bgmVolume * masterVolume;
        }

        private void UpdateAllVolumes()
        {
            bgmSource.volume = bgmVolume * masterVolume;
            ambienceSource.volume = ambienceVolume * masterVolume;
            grillLoopSource.volume = grillVolume * masterVolume * masterSFXVolume;
        }

        // ══════════════════════════════════════════════════════════════════════
        // UTILITIES
        // ══════════════════════════════════════════════════════════════════════

        private IEnumerator FadeOutAudioSource(AudioSource source, float fadeTime)
        {
            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeTime);
                yield return null;
            }

            source.Stop();
            source.volume = startVol; // Reset for next use
        }

        // ══════════════════════════════════════════════════════════════════════
        // PITCH VARIATION (optional - adds variety to repetitive sounds)
        // ══════════════════════════════════════════════════════════════════════

        public void PlaySFXWithPitchVariation(AudioClip clip, float volume, float pitchVariation = 0.1f)
        {
            if (clip == null) return;
            sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            sfxSource.PlayOneShot(clip, volume * masterVolume * masterSFXVolume);
            sfxSource.pitch = 1f; // Reset
        }
    }
}
