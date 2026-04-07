using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deadlight.Narrative;
using Deadlight.Player;
using Deadlight.UI;

namespace Deadlight.Core
{
    /// <summary>
    /// Campaign onboarding for early combat:
    /// 1) One-time night-1 fire hint before waves start.
    /// 2) Level-1 guidance for the first few zombies that become visible on screen.
    /// </summary>
    public class FirstCombatHintController : MonoBehaviour
    {
        public static FirstCombatHintController Instance { get; private set; }

        private const string PrefKey = "Deadlight_FirstCombatHintDismissed";

        [SerializeField] private float commsDuration = 10f;
        [SerializeField] private float statusHudSeconds = 10f;
        [SerializeField] private int killsToDismiss = 2;
        [SerializeField] private float reminderIfNoFireSeconds = 12f;
        [SerializeField] private float reminderCommsDuration = 5f;
        [SerializeField, Range(1, 2)] private int onScreenZombieHints = 2;
        [SerializeField] private float onScreenHintCommsDuration = 6f;
        [SerializeField] private float onScreenHintStatusSeconds = 5f;
        [SerializeField] private bool requireLevelOneForOnScreenHints = true;

        private bool offeredForCurrentNight1Step;
        private Coroutine dismissCoroutine;
        private WaveManager waveManager;
        private readonly List<GameObject> pendingSpawnedEnemies = new List<GameObject>();
        private int onScreenHintsShown;
        private bool onScreenGuidanceActive;

        private bool firedDuringHint;
        private int killsSinceHintBaseline;
        private int killCountAtHintStart;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            waveManager = FindFirstObjectByType<WaveManager>();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged += OnNightChanged;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged -= OnNightChanged;
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            DisableOnScreenGuidance(clearPending: true);
            UnhookPlayer();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnNightChanged(int night)
        {
            if (night != 1)
            {
                offeredForCurrentNight1Step = false;
            }

            if (GameManager.GetLevelForNight(night) > 1)
            {
                DisableOnScreenGuidance(clearPending: true);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.NightPhase)
            {
                DisableOnScreenGuidance(clearPending: true);
                return;
            }

            TryStartOnScreenGuidance();
            TryOfferNightOneFireHint();
        }

        private void Update()
        {
            ProcessOnScreenGuidance();
        }

        private void TryOfferNightOneFireHint()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.CurrentNight != 1)
            {
                return;
            }

            if (offeredForCurrentNight1Step || dismissCoroutine != null)
            {
                return;
            }

            offeredForCurrentNight1Step = true;

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            // Runs during GameManager.ChangeState, before StartNightPhase's StartNightWaves — no zombies yet.
            ShowHintImmediate();
            killCountAtHintStart = waveManager != null ? waveManager.TotalEnemiesKilled : 0;
            killsSinceHintBaseline = 0;
            firedDuringHint = false;
            HookPlayer();

            dismissCoroutine = StartCoroutine(RunDismissalTracking());
        }

        private void ShowHintImmediate()
        {
            var narrative = NarrativeManager.Instance;
            if (narrative != null)
            {
                narrative.QueueSystemMessage(
                    "COMMS",
                    "Hostiles inbound—aim with the mouse, left-click to fire before they close in.",
                    commsDuration,
                    interrupt: false,
                    playRadioStatic: false);
            }

            var hud = FindFirstObjectByType<GameplayHUD>();
            hud?.ShowTransientStatus("Mouse: aim  •  LMB: fire", statusHudSeconds);
        }

        private void TryStartOnScreenGuidance()
        {
            int maxHints = Mathf.Max(1, onScreenZombieHints);
            if (onScreenHintsShown >= maxHints)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                return;
            }

            if (requireLevelOneForOnScreenHints && GameManager.Instance.CurrentLevel != 1)
            {
                return;
            }

            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }

            if (waveManager == null || onScreenGuidanceActive)
            {
                return;
            }

            pendingSpawnedEnemies.Clear();
            waveManager.OnEnemySpawned += OnEnemySpawned;
            onScreenGuidanceActive = true;
        }

        private void DisableOnScreenGuidance(bool clearPending = false)
        {
            if (waveManager != null)
            {
                waveManager.OnEnemySpawned -= OnEnemySpawned;
            }

            onScreenGuidanceActive = false;
            if (clearPending)
            {
                pendingSpawnedEnemies.Clear();
            }
        }

        private void ProcessOnScreenGuidance()
        {
            if (!onScreenGuidanceActive)
            {
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.NightPhase)
            {
                DisableOnScreenGuidance(clearPending: true);
                return;
            }

            int maxHints = Mathf.Max(1, onScreenZombieHints);
            if (onScreenHintsShown >= maxHints)
            {
                DisableOnScreenGuidance(clearPending: true);
                return;
            }

            var gameplayCamera = Camera.main;
            if (gameplayCamera == null)
            {
                return;
            }

            for (int i = pendingSpawnedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = pendingSpawnedEnemies[i];
                if (enemy == null)
                {
                    pendingSpawnedEnemies.RemoveAt(i);
                    continue;
                }

                if (!IsVisibleOnScreen(enemy, gameplayCamera))
                {
                    continue;
                }

                pendingSpawnedEnemies.RemoveAt(i);
                onScreenHintsShown++;
                ShowOnScreenZombieHint(onScreenHintsShown);

                if (onScreenHintsShown >= maxHints)
                {
                    DisableOnScreenGuidance(clearPending: true);
                }

                break;
            }
        }

        private void OnEnemySpawned(GameObject enemy, int _totalSpawned)
        {
            if (!onScreenGuidanceActive || enemy == null)
            {
                return;
            }

            if (!pendingSpawnedEnemies.Contains(enemy))
            {
                pendingSpawnedEnemies.Add(enemy);
            }
        }

        private void ShowOnScreenZombieHint(int hintIndex)
        {
            string commsMessage;
            string statusMessage;

            if (hintIndex <= 1)
            {
                commsMessage = "Contact on screen. Keep moving with WASD and line up your aim before they close in.";
                statusMessage = "Zombie spotted: move (WASD) and track with your cursor.";
            }
            else
            {
                commsMessage = "Good tracking. Fire now - left-click while your cursor stays on the zombie.";
                statusMessage = "Take the shot: left-click when your aim is on target.";
            }

            NarrativeManager.Instance?.QueueSystemMessage(
                "COMMS",
                commsMessage,
                onScreenHintCommsDuration,
                interrupt: false,
                playRadioStatic: false);

            FindFirstObjectByType<GameplayHUD>()?.ShowTransientStatus(statusMessage, onScreenHintStatusSeconds);
        }

        private static bool IsVisibleOnScreen(GameObject target, Camera camera)
        {
            if (target == null || camera == null)
            {
                return false;
            }

            var renderer = target.GetComponentInChildren<Renderer>();
            Vector3 worldPosition = renderer != null ? renderer.bounds.center : target.transform.position;
            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition);

            return viewportPosition.z > 0f &&
                   viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                   viewportPosition.y >= 0f && viewportPosition.y <= 1f;
        }

        private IEnumerator RunDismissalTracking()
        {
            float elapsed = 0f;
            bool reminderSent = false;
            float maxWait = Mathf.Max(statusHudSeconds, reminderIfNoFireSeconds + 2f);

            while (elapsed < maxWait)
            {
                if (firedDuringHint || killsSinceHintBaseline >= killsToDismiss)
                {
                    break;
                }

                elapsed += Time.deltaTime;

                if (!reminderSent && !firedDuringHint && elapsed >= reminderIfNoFireSeconds)
                {
                    reminderSent = true;
                    NarrativeManager.Instance?.QueueSystemMessage(
                        "COMMS",
                        "If shots aren't landing, keep the cursor on the target and press the left mouse button.",
                        reminderCommsDuration,
                        interrupt: false,
                        playRadioStatic: false);
                }

                yield return null;
            }

            UnhookPlayer();
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            dismissCoroutine = null;
        }

        private void HookPlayer()
        {
            var shooting = FindPlayerShooting();
            if (shooting == null)
            {
                return;
            }

            shooting.OnWeaponFired += OnWeaponFired;
            if (waveManager != null)
            {
                waveManager.OnEnemyKilled += OnEnemyKilledDuringHint;
            }
        }

        private void UnhookPlayer()
        {
            var shooting = FindPlayerShooting();
            if (shooting != null)
            {
                shooting.OnWeaponFired -= OnWeaponFired;
            }

            if (waveManager != null)
            {
                waveManager.OnEnemyKilled -= OnEnemyKilledDuringHint;
            }
        }

        private void OnWeaponFired()
        {
            firedDuringHint = true;
        }

        private void OnEnemyKilledDuringHint(int totalKills)
        {
            killsSinceHintBaseline = Mathf.Max(0, totalKills - killCountAtHintStart);
        }

        private static PlayerShooting FindPlayerShooting()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return null;
            }

            return player.GetComponent<PlayerShooting>();
        }
    }
}
