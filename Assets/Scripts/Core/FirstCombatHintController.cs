using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deadlight.Enemy;
using Deadlight.Narrative;
using Deadlight.Player;
using Deadlight.UI;

namespace Deadlight.Core
{
    /// <summary>
    /// Early combat onboarding for campaign night 1. Shows initial controls plus first-encounter
    /// coaching when the first zombies come on screen.
    /// </summary>
    public class FirstCombatHintController : MonoBehaviour
    {
        public static FirstCombatHintController Instance { get; private set; }

        [SerializeField] private float statusHudSeconds = 2.8f;
        [SerializeField] private int killsToDismiss = 2;
        [SerializeField] private float reminderIfNoFireSeconds = 12f;
        [SerializeField] private float reminderOverlayDuration = 2.8f;
        [SerializeField] [Range(0f, 6f)] private float firstVisibleGuidanceDelayAfterNightStartSeconds = 1.8f;
        [SerializeField] [Range(2f, 8f)] private float commsSpacingSeconds = 3.4f;
        [SerializeField] private int visibleZombieHintsToShow = 2;
        [SerializeField] private float visibleZombieHintScanInterval = 0.15f;
        [SerializeField] private float visibleZombieHintStatusSeconds = 2.8f;
        [SerializeField] private float visibleZombieHintCooldownSeconds = 5.2f;
        [SerializeField] private float unhintedEnemyFallbackDelaySeconds = 1.4f;
        [SerializeField] [Range(0f, 0.2f)] private float visibleViewportPadding = 0.06f;

        private const string ReminderLine =
            "RADIO: Keep your cursor on the target and press the left mouse button to fire.";

        private const string FirstVisibleZombieLine =
            "RADIO: Hostile in sight. Aim with the mouse and left-click to fire.";

        private const string SecondVisibleZombieLine =
            "RADIO: Hostile in range. Maintain aim and fire controlled shots.";

        private const string WaveTwoLine =
            "RADIO: Wave 2 incoming. Keep distance and place your shots carefully.";

        private const string FirstVisibleZombieStatus = "Aim with the mouse and left click to fire";
        private const string FollowupVisibleZombieStatus = "Keep the cursor on the target and left click to fire";

        private const string MovementControlsStatus = "WASD: Move   Shift: Sprint   Space: Dodge Roll";
        private const string LootControlsStatus = "Walk up to a crate and hold F to loot it";

        private bool offeredForCurrentNight1Step;
        private Coroutine dismissCoroutine;
        private Coroutine visibleZombieHintCoroutine;
        private Coroutine movementHintCoroutine;
        private WaveManager waveManager;
        private readonly HashSet<int> hintedEnemyInstanceIds = new HashSet<int>();
        private int visibleZombieHintsShown;
        private float hintSessionStartedAt;
        private float lastCommsHintAt = -999f;

        private bool firedDuringHint;
        private int killsSinceHintBaseline;
        private int killCountAtHintStart;

        /// <summary>
        /// Call from <see cref="GameManager.StartNightPhase"/> after night state is set, before waves spawn.
        /// </summary>
        public static void NotifyAfterNightPhaseEntered()
        {
            var c = Instance;
            if (c == null)
            {
                c = FindFirstObjectByType<FirstCombatHintController>();
            }

            if (c == null && GameManager.Instance != null)
            {
                var go = new GameObject("FirstCombatHintController");
                c = go.AddComponent<FirstCombatHintController>();
            }

            c?.TryBeginFirstNightCombatHint();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            waveManager = FindFirstObjectByType<WaveManager>();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged += OnNightChanged;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            if (waveManager != null)
            {
                waveManager.OnWaveStarted += OnWaveStarted;
            }
        }

        private void OnDestroy()
        {
            StopAllGuidanceCoroutines();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged -= OnNightChanged;
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            if (waveManager != null)
            {
                waveManager.OnWaveStarted -= OnWaveStarted;
            }

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
                StopActiveHintSession();
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.NightPhase)
            {
                TryBeginFirstNightCombatHint();
                return;
            }

            StopActiveHintSession();

            if (state == GameState.DayPhase || state == GameState.MainMenu)
            {
                offeredForCurrentNight1Step = false;
            }
        }

        private void OnWaveStarted(int wave)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentNight != 1)
            {
                return;
            }

            if (wave == 1)
            {
                TryBeginFirstNightCombatHint();
            }
            else if (wave == 2)
            {
                SendPriorityHint(WaveTwoLine, 2.8f);
            }
        }

        private void TryBeginFirstNightCombatHint()
        {
            EnsureWaveManagerReference();

            if (GameManager.Instance == null || GameManager.Instance.CurrentNight != 1)
            {
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.NightPhase)
            {
                return;
            }

            if (offeredForCurrentNight1Step || dismissCoroutine != null)
            {
                return;
            }

            offeredForCurrentNight1Step = true;

            hintSessionStartedAt = Time.time;
            lastCommsHintAt = -999f;
            killCountAtHintStart = waveManager != null ? waveManager.TotalEnemiesKilled : 0;
            killsSinceHintBaseline = 0;
            firedDuringHint = false;
            hintedEnemyInstanceIds.Clear();
            visibleZombieHintsShown = 0;
            HookPlayer();

            if (movementHintCoroutine != null)
                StopCoroutine(movementHintCoroutine);
            movementHintCoroutine = StartCoroutine(ShowMovementControlHints());

            if (visibleZombieHintCoroutine != null)
            {
                StopCoroutine(visibleZombieHintCoroutine);
            }

            visibleZombieHintCoroutine = StartCoroutine(TrackVisibleZombiesForHints());
            dismissCoroutine = StartCoroutine(RunDismissalTracking());
        }

        private IEnumerator RunDismissalTracking()
        {
            int requiredVisibleHintsBeforeDismiss = Mathf.Max(0, visibleZombieHintsToShow);
            float elapsed = 0f;
            bool reminderSent = false;
            float minVisibleHintWindow = requiredVisibleHintsBeforeDismiss > 0
                ? (requiredVisibleHintsBeforeDismiss * Mathf.Max(0.1f, visibleZombieHintCooldownSeconds)) + 6f
                : 0f;
            float maxWait = Mathf.Max(statusHudSeconds, reminderIfNoFireSeconds + 2f, minVisibleHintWindow);

            while (elapsed < maxWait)
            {
                bool canDismissFromFiring = firedDuringHint && visibleZombieHintsShown >= requiredVisibleHintsBeforeDismiss;
                bool canDismissFromKills = killsSinceHintBaseline >= killsToDismiss &&
                                           visibleZombieHintsShown > 0;
                if (canDismissFromFiring || canDismissFromKills)
                {
                    break;
                }

                elapsed += Time.deltaTime;

                if (!reminderSent &&
                    visibleZombieHintsShown > 0 &&
                    !firedDuringHint &&
                    elapsed >= reminderIfNoFireSeconds)
                {
                    reminderSent = true;
                    SendPriorityHint(ReminderLine, reminderOverlayDuration);
                }

                yield return null;
            }

            UnhookPlayer();
            if (visibleZombieHintCoroutine != null)
            {
                StopCoroutine(visibleZombieHintCoroutine);
                visibleZombieHintCoroutine = null;
            }
            dismissCoroutine = null;
        }

        private IEnumerator TrackVisibleZombiesForHints()
        {
            int maxHints = Mathf.Max(0, visibleZombieHintsToShow);
            if (maxHints <= 0)
            {
                visibleZombieHintCoroutine = null;
                yield break;
            }

            float firstUnhintedEnemySeenAt = -1f;
            float fallbackDelay = Mathf.Max(0.2f, unhintedEnemyFallbackDelaySeconds);
            while (GameManager.Instance != null &&
                   GameManager.Instance.CurrentState == GameState.NightPhase &&
                   GameManager.Instance.CurrentNight == 1 &&
                   visibleZombieHintsShown < maxHints)
            {
                if (Time.time - hintSessionStartedAt < Mathf.Max(0f, firstVisibleGuidanceDelayAfterNightStartSeconds))
                {
                    yield return new WaitForSeconds(Mathf.Max(0.05f, visibleZombieHintScanInterval));
                    continue;
                }

                if (TryFindNewVisibleEnemy(out var enemy))
                {
                    hintedEnemyInstanceIds.Add(enemy.GetInstanceID());
                    visibleZombieHintsShown++;
                    PushVisibleZombieHint(visibleZombieHintsShown);

                    float cooldown = Mathf.Max(0.1f, visibleZombieHintCooldownSeconds);
                    yield return new WaitForSeconds(cooldown);
                    firstUnhintedEnemySeenAt = -1f;
                    continue;
                }

                if (TryFindNewAliveEnemy(out var fallbackEnemy))
                {
                    if (firstUnhintedEnemySeenAt < 0f)
                    {
                        firstUnhintedEnemySeenAt = Time.time;
                    }
                    else if (Time.time - firstUnhintedEnemySeenAt >= fallbackDelay)
                    {
                        hintedEnemyInstanceIds.Add(fallbackEnemy.GetInstanceID());
                        visibleZombieHintsShown++;
                        PushVisibleZombieHint(visibleZombieHintsShown);

                        float cooldown = Mathf.Max(0.1f, visibleZombieHintCooldownSeconds);
                        yield return new WaitForSeconds(cooldown);
                        firstUnhintedEnemySeenAt = -1f;
                        continue;
                    }
                }
                else
                {
                    firstUnhintedEnemySeenAt = -1f;
                }

                float scanDelay = Mathf.Max(0.05f, visibleZombieHintScanInterval);
                yield return new WaitForSeconds(scanDelay);
            }

            visibleZombieHintCoroutine = null;
        }

        private bool TryFindNewVisibleEnemy(out EnemyHealth enemy)
        {
            enemy = null;
            Camera cam = GetGameplayCamera();
            if (cam == null)
            {
                return false;
            }

            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth candidate = enemies[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                int enemyId = candidate.GetInstanceID();
                if (hintedEnemyInstanceIds.Contains(enemyId))
                {
                    continue;
                }

                Vector3 viewport = cam.WorldToViewportPoint(candidate.transform.position);
                float viewportPadding = Mathf.Clamp(visibleViewportPadding, 0f, 0.2f);
                bool visibleOnScreen = viewport.z > 0f &&
                                       viewport.x >= -viewportPadding && viewport.x <= 1f + viewportPadding &&
                                       viewport.y >= -viewportPadding && viewport.y <= 1f + viewportPadding;
                if (!visibleOnScreen)
                {
                    continue;
                }

                enemy = candidate;
                return true;
            }

            return false;
        }

        private bool TryFindNewAliveEnemy(out EnemyHealth enemy)
        {
            enemy = null;

            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth candidate = enemies[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    continue;
                }

                int enemyId = candidate.GetInstanceID();
                if (hintedEnemyInstanceIds.Contains(enemyId))
                {
                    continue;
                }

                enemy = candidate;
                return true;
            }

            return false;
        }

        private void PushVisibleZombieHint(int hintIndex)
        {
            var hud = FindFirstObjectByType<GameplayHUD>();
            string status = hintIndex <= 1
                ? FirstVisibleZombieStatus
                : FollowupVisibleZombieStatus;
            hud?.ShowTransientStatus(status, Mathf.Clamp(visibleZombieHintStatusSeconds, 2f, 3f));

            if (hintIndex == 1)
            {
                SendPriorityHint(FirstVisibleZombieLine, reminderOverlayDuration);
            }
            else if (hintIndex == 2 && Time.time - lastCommsHintAt >= Mathf.Max(0.5f, commsSpacingSeconds))
            {
                SendPriorityHint(SecondVisibleZombieLine, reminderOverlayDuration);
            }
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

        private void StopActiveHintSession()
        {
            if (dismissCoroutine != null)
            {
                StopCoroutine(dismissCoroutine);
                dismissCoroutine = null;
            }

            UnhookPlayer();
            hintedEnemyInstanceIds.Clear();
            visibleZombieHintsShown = 0;

            if (visibleZombieHintCoroutine != null)
            {
                StopCoroutine(visibleZombieHintCoroutine);
                visibleZombieHintCoroutine = null;
            }

            if (movementHintCoroutine != null)
            {
                StopCoroutine(movementHintCoroutine);
                movementHintCoroutine = null;
            }
        }

        private IEnumerator ShowMovementControlHints()
        {
            var hud = FindFirstObjectByType<GameplayHUD>();

            // Show movement controls immediately so the player knows how to move.
            hud?.ShowTransientStatus(MovementControlsStatus, 4f);
            yield return new WaitForSeconds(4.5f);

            // After movement, remind about the loot mechanic before enemies swarm.
            hud?.ShowTransientStatus(LootControlsStatus, 3.5f);
            movementHintCoroutine = null;
        }

        private void StopAllGuidanceCoroutines()
        {
            StopActiveHintSession();
            if (movementHintCoroutine != null)
            {
                StopCoroutine(movementHintCoroutine);
                movementHintCoroutine = null;
            }
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

        private void EnsureWaveManagerReference()
        {
            if (waveManager != null)
            {
                return;
            }

            waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnWaveStarted += OnWaveStarted;
            }
        }

        private void SendPriorityHint(string line, float durationSeconds)
        {
            if (Time.time - lastCommsHintAt < Mathf.Max(0.5f, commsSpacingSeconds))
            {
                return;
            }

            float duration = Mathf.Clamp(durationSeconds, 2f, 3f);
            if (NarrativeManager.Instance != null)
            {
                string message = line;
                const string radioPrefix = "RADIO:";
                if (message.StartsWith(radioPrefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    message = message.Substring(radioPrefix.Length).Trim();
                }

                NarrativeManager.Instance.QueueSystemMessage(
                    "COMMS",
                    message,
                    duration,
                    interrupt: false,
                    playRadioStatic: false);
                lastCommsHintAt = Time.time;
                return;
            }

            RadioTransmissions.Instance?.ShowMessage(line, duration);
            lastCommsHintAt = Time.time;
        }

        private static Camera GetGameplayCamera()
        {
            Camera main = Camera.main;
            if (main != null)
            {
                return main;
            }

            return FindFirstObjectByType<Camera>();
        }
    }
}
