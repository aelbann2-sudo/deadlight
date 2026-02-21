using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Systems
{
    public class KillStreakSystem : MonoBehaviour
    {
        public static KillStreakSystem Instance { get; private set; }

        [Header("Streak Thresholds")]
        [SerializeField] private int killingSpreeThreshold = 10;
        [SerializeField] private int rampageThreshold = 25;
        [SerializeField] private int unstoppableThreshold = 50;
        [SerializeField] private int godlikeThreshold = 100;

        [Header("Streak Timeout")]
        [SerializeField] private float streakTimeout = 5f;

        private int currentStreak;
        private float lastKillTime;
        private int lastAnnouncedMilestone;

        public int CurrentStreak => currentStreak;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (currentStreak > 0 && Time.time - lastKillTime > streakTimeout)
            {
                ResetStreak();
            }
        }

        public void RegisterKill(Vector3 position)
        {
            currentStreak++;
            lastKillTime = Time.time;

            CheckMilestones(position);
        }

        private void CheckMilestones(Vector3 position)
        {
            if (currentStreak >= godlikeThreshold && lastAnnouncedMilestone < godlikeThreshold)
            {
                AnnounceStreak("GODLIKE!", new Color(1f, 0.2f, 0.8f), position);
                SpawnAllDrops(position);
                lastAnnouncedMilestone = godlikeThreshold;
            }
            else if (currentStreak >= unstoppableThreshold && lastAnnouncedMilestone < unstoppableThreshold)
            {
                AnnounceStreak("UNSTOPPABLE!", new Color(1f, 0.5f, 0f), position);
                if (PickupSpawner.Instance != null)
                {
                    PickupSpawner.Instance.SpawnPickup(position, PickupType.Powerup);
                }
                lastAnnouncedMilestone = unstoppableThreshold;
            }
            else if (currentStreak >= rampageThreshold && lastAnnouncedMilestone < rampageThreshold)
            {
                AnnounceStreak("RAMPAGE!", new Color(1f, 0.3f, 0.3f), position);
                if (PickupSpawner.Instance != null)
                {
                    PickupSpawner.Instance.SpawnPickup(position, PickupType.Health);
                }
                lastAnnouncedMilestone = rampageThreshold;
            }
            else if (currentStreak >= killingSpreeThreshold && lastAnnouncedMilestone < killingSpreeThreshold)
            {
                AnnounceStreak("KILLING SPREE!", new Color(1f, 0.9f, 0.3f), position);
                if (PickupSpawner.Instance != null)
                {
                    PickupSpawner.Instance.SpawnPickup(position, PickupType.Ammo);
                }
                lastAnnouncedMilestone = killingSpreeThreshold;
            }
        }

        private void AnnounceStreak(string message, Color color, Vector3 position)
        {
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.SpawnKillStreakText(message, color);
            }

            if (RadioTransmissions.Instance != null)
            {
                RadioTransmissions.Instance.ShowMessage($"{message} {currentStreak} KILLS!", 2f);
            }
        }

        private void SpawnAllDrops(Vector3 position)
        {
            if (PickupSpawner.Instance == null) return;

            PickupSpawner.Instance.SpawnPickup(position + Vector3.left, PickupType.Ammo);
            PickupSpawner.Instance.SpawnPickup(position + Vector3.right, PickupType.Health);
            PickupSpawner.Instance.SpawnPickup(position + Vector3.up, PickupType.Points);
            PickupSpawner.Instance.SpawnPickup(position + Vector3.down, PickupType.Powerup);
        }

        public void ResetStreak()
        {
            if (currentStreak >= 10)
            {
                Debug.Log($"[KillStreak] Streak ended at {currentStreak} kills");
            }
            currentStreak = 0;
            lastAnnouncedMilestone = 0;
        }
    }
}
