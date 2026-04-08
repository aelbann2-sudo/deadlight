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
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        public DayObjective GenerateObjective(int night, int seed)
        {
            OnObjectiveGenerated?.Invoke(activeObjective);
            return activeObjective;
        }

        private DayObjective GetFixedObjectiveForNight(int night)
        {
            switch (night)
            {
                case 1:
                    return new DayObjective
                    {
                        type = ObjectiveType.RecoverSupplyCache,
                        title = "Reach the Crash Site",
                        description = "Navigate to the Flight 7 wreckage and recover the black box.",
                        targetCount = 1,
                        progress = 0,
                        pointReward = 60,
                        ammoReward = 20,
                        nightBuffMultiplier = 1.05f
                    };
                case 2:
                    return new DayObjective
                    {
                        type = ObjectiveType.ActivateBeacon,
                        title = "Search the Shelter",
                        description = "Activate 2 data terminals in the school shelter to recover evacuation records.",
                        targetCount = 2,
                        progress = 0,
                        pointReward = 90,
                        ammoReward = 28,
                        nightBuffMultiplier = 1.08f
                    };
                case 3:
                    return new DayObjective
                    {
                        type = ObjectiveType.SecureZone,
                        title = "Breach the Lab",
                        description = "Clear 3 contaminated zones around the research lab to access Lazarus data.",
                        targetCount = 3,
                        progress = 0,
                        pointReward = 120,
                        ammoReward = 36,
                        nightBuffMultiplier = 1.12f
                    };
                default:
                    return new DayObjective
                    {
                        type = ObjectiveType.RecoverSupplyCache,
                        title = "Arm the Beacon",
                        description = "Reach the main lab and arm the extraction beacon before nightfall.",
                        targetCount = 1,
                        progress = 0,
                        pointReward = 150,
                        ammoReward = 44,
                        nightBuffMultiplier = 1.15f
                    };
            }
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
                ResourceManager.Instance.AddResource(ResourceType.Salvage, 2);
                ResourceManager.Instance.AddResource(ResourceType.TechParts, 1);
            }

            ActiveNightBuffMultiplier = Mathf.Max(1f, activeObjective.nightBuffMultiplier);
            OnObjectiveCompleted?.Invoke(activeObjective);
        }

        private void HandleGameStateChanged(GameState state)
        {
        }
    }
}
