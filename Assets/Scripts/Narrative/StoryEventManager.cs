using UnityEngine;
using Deadlight.Core;
using Deadlight.Player;

namespace Deadlight.Narrative
{
    public class StoryEventManager : MonoBehaviour
    {
        public static StoryEventManager Instance { get; private set; }

        private bool firstKillTriggered;
        private bool lowHealthTriggered;
        private int killCount;
        private float lowHealthCooldown;
        private int nightsSurvived;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnStateChanged;

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
                waveManager.OnEnemyKilled += OnEnemyKilled;
        }

        void Update()
        {
            if (lowHealthCooldown > 0f) lowHealthCooldown -= Time.deltaTime;
            CheckPlayerHealth();
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.NightPhase)
            {
                int night = GameManager.Instance?.CurrentNight ?? 1;
                if (RadioTransmissions.Instance != null)
                    RadioTransmissions.Instance.ShowNightWarning(night);

                if (NightMutation.Instance != null)
                    NightMutation.Instance.RollMutation(night);
            }

            if (state == GameState.DayPhase)
            {
                nightsSurvived++;
                if (NightMutation.Instance != null)
                    NightMutation.Instance.ClearMutation();

                if (nightsSurvived > 0 && RadioTransmissions.Instance != null)
                {
                    string[] survivalMessages = {
                        "You made it through another night. Don't let your guard down.",
                        "Dawn breaks... but the next night will be worse.",
                        "Another sunrise. Use the daylight wisely, survivor.",
                        "The infected retreat at dawn. Scavenge while you can."
                    };
                    int idx = Mathf.Min(nightsSurvived - 1, survivalMessages.Length - 1);
                    RadioTransmissions.Instance.ShowMessage(survivalMessages[idx], 4f);
                }
            }

            if (state == GameState.GameOver)
            {
                if (EndingSequence.Instance != null)
                    EndingSequence.Instance.PlayDeathEnding();
            }

            if (state == GameState.Victory)
            {
                if (EndingSequence.Instance != null)
                    EndingSequence.Instance.PlayVictoryEnding();
            }
        }

        void OnEnemyKilled(int totalKills)
        {
            killCount = totalKills;

            if (!firstKillTriggered && killCount == 1)
            {
                firstKillTriggered = true;
                if (RadioTransmissions.Instance != null)
                    RadioTransmissions.Instance.TriggerFirstKill();
            }

            if (killCount % 25 == 0 && RadioTransmissions.Instance != null)
                RadioTransmissions.Instance.TriggerKillStreak();
        }

        void CheckPlayerHealth()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            var health = player.GetComponent<PlayerHealth>();
            if (health == null) return;

            if (health.CurrentHealth / health.MaxHealth < 0.2f && lowHealthCooldown <= 0f)
            {
                lowHealthCooldown = 30f;
                if (RadioTransmissions.Instance != null)
                    RadioTransmissions.Instance.TriggerLowHealth();
            }
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnStateChanged;
        }
    }
}
