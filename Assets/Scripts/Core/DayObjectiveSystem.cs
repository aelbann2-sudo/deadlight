using System;
using Deadlight.Systems;
using UnityEngine;

namespace Deadlight.Core
{
    public enum ObjectiveType
    {
        SecureZone,
        ActivateBeacon,
        RecoverSupplyCache
    }

    [Serializable]
    public class DayObjective
    {
        public ObjectiveType type;
        public string title;
        public string description;
        public int targetCount;
        public int progress;
        public int pointReward;
        public int ammoReward;
        public float nightBuffMultiplier;

        public bool IsComplete => progress >= targetCount;
        public float Progress01 => targetCount <= 0 ? 0f : Mathf.Clamp01((float)progress / targetCount);
    }

    public class DayObjectiveSystem : MonoBehaviour
    {
        public static DayObjectiveSystem Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private DayObjective activeObjective;

        public DayObjective ActiveObjective => activeObjective;
        public float ActiveNightBuffMultiplier { get; private set; } = 1f;

        public event Action<DayObjective> OnObjectiveGenerated;
        public event Action<DayObjective> OnObjectiveUpdated;
        public event Action<DayObjective> OnObjectiveCompleted;

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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnNightChanged += HandleNightChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnNightChanged -= HandleNightChanged;
            }
        }

        public DayObjective GenerateObjective(int night, int seed)
        {
            var rng = new System.Random(seed + (night * 101));
            ObjectiveType type = (ObjectiveType)rng.Next(0, 3);

            int difficultyScale = Mathf.Max(1, night);
            activeObjective = new DayObjective
            {
                type = type,
                targetCount = type == ObjectiveType.RecoverSupplyCache ? 1 : 3 + Mathf.Min(3, difficultyScale),
                progress = 0,
                pointReward = 60 + (difficultyScale * 30),
                ammoReward = 20 + (difficultyScale * 8),
                nightBuffMultiplier = 1.05f + (difficultyScale * 0.03f)
            };

            switch (type)
            {
                case ObjectiveType.SecureZone:
                    activeObjective.title = "Secure Zone";
                    activeObjective.description = "Clear and hold safe sectors before nightfall.";
                    break;
                case ObjectiveType.ActivateBeacon:
                    activeObjective.title = "Activate Beacon";
                    activeObjective.description = "Activate relay nodes to improve tactical visibility.";
                    break;
                default:
                    activeObjective.title = "Recover Supply Cache";
                    activeObjective.description = "Locate and recover the hidden supply cache.";
                    break;
            }

            ActiveNightBuffMultiplier = 1f;
            OnObjectiveGenerated?.Invoke(activeObjective);
            OnObjectiveUpdated?.Invoke(activeObjective);
            return activeObjective;
        }

        public DayObjective GetActiveObjective()
        {
            return activeObjective;
        }

        public void ResetObjective()
        {
            activeObjective = null;
            ActiveNightBuffMultiplier = 1f;
            OnObjectiveUpdated?.Invoke(activeObjective);
        }

        public void AddProgress(int amount = 1)
        {
            if (activeObjective == null || amount <= 0)
            {
                return;
            }

            if (activeObjective.IsComplete)
            {
                return;
            }

            activeObjective.progress = Mathf.Min(activeObjective.targetCount, activeObjective.progress + amount);
            OnObjectiveUpdated?.Invoke(activeObjective);

            if (activeObjective.IsComplete)
            {
                CompleteObjective();
            }
        }

        public void MarkCompleted()
        {
            if (activeObjective == null || activeObjective.IsComplete)
            {
                return;
            }

            activeObjective.progress = activeObjective.targetCount;
            OnObjectiveUpdated?.Invoke(activeObjective);
            CompleteObjective();
        }

        private void CompleteObjective()
        {
            if (activeObjective == null)
            {
                return;
            }

            if (PointsSystem.Instance != null)
            {
                PointsSystem.Instance.AddPoints(activeObjective.pointReward, "Day Objective Completed");
            }

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var shooting = player.GetComponent<Deadlight.Player.PlayerShooting>();
                shooting?.AddAmmo(activeObjective.ammoReward);
            }

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddResource(ResourceType.Scrap, 2);
                ResourceManager.Instance.AddResource(ResourceType.Chemicals, 1);
            }

            ActiveNightBuffMultiplier = Mathf.Max(1f, activeObjective.nightBuffMultiplier);
            OnObjectiveCompleted?.Invoke(activeObjective);
        }

        private void HandleNightChanged(int night)
        {
            int seed = Time.frameCount + (night * 713);
            GenerateObjective(night, seed);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.DayPhase && activeObjective == null)
            {
                int night = GameManager.Instance?.CurrentNight ?? 1;
                GenerateObjective(night, Time.frameCount + (night * 97));
            }
        }
    }
}
