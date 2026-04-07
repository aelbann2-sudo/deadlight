using System.Collections;
using UnityEngine;
using Deadlight.Narrative;
using Deadlight.Player;
using Deadlight.UI;

namespace Deadlight.Core
{
    /// <summary>
    /// One-time (per install) hint on campaign night 1: COMMS + HUD as soon as night combat starts,
    /// before wave spawning (hooked in <see cref="GameManager.OnGameStateChanged"/> while
    /// <see cref="GameManager.StartNightPhase"/> runs, so it fires before <see cref="WaveManager.StartNightWaves"/>).
    /// Dismisses on first shot, two kills since hint start, or timeout. Optional COMMS reminder if no fire.
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

        private bool offeredForCurrentNight1Step;
        private Coroutine dismissCoroutine;
        private WaveManager waveManager;

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
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.NightPhase)
            {
                return;
            }

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
