using UnityEngine;
using Deadlight.Core;
using Deadlight.Data;
using System;
using System.Collections.Generic;

namespace Deadlight.Systems
{
    [Serializable]
    public class WeaponUnlock
    {
        public WeaponData weapon;
        public int nightRequired;
        public int pointCost;
        public bool isUnlocked;
        public bool isPurchased;
    }

    [Serializable]
    public class NightMilestone
    {
        public int night;
        public string description;
        public List<WeaponData> weaponsUnlocked;
        public int bonusPoints;
        public bool isCompleted;
    }

    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        [Header("Weapon Unlocks")]
        [SerializeField] private List<WeaponUnlock> weaponUnlocks = new List<WeaponUnlock>();

        [Header("Night Milestones")]
        [SerializeField] private List<NightMilestone> nightMilestones = new List<NightMilestone>();

        [Header("Current Progress")]
        [SerializeField] private int highestNightReached = 0;
        [SerializeField] private List<string> completedChallenges = new List<string>();

        public int HighestNightReached => highestNightReached;
        public List<WeaponUnlock> WeaponUnlocks => weaponUnlocks;

        public event Action<WeaponData> OnWeaponUnlocked;
        public event Action<NightMilestone> OnMilestoneCompleted;
        public event Action<string> OnChallengeCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeDefaultMilestones();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged += HandleNightChanged;
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged -= HandleNightChanged;
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void InitializeDefaultMilestones()
        {
            if (nightMilestones.Count > 0) return;

            nightMilestones = new List<NightMilestone>
            {
                new NightMilestone
                {
                    night = 1,
                    description = "Survive Night 1 - Shotgun and Barricades unlocked",
                    bonusPoints = 100,
                    isCompleted = false
                },
                new NightMilestone
                {
                    night = 2,
                    description = "Survive Night 2 - Automatic Rifle and Traps unlocked",
                    bonusPoints = 150,
                    isCompleted = false
                },
                new NightMilestone
                {
                    night = 3,
                    description = "Survive Night 3 - Grenade Launcher unlocked",
                    bonusPoints = 200,
                    isCompleted = false
                },
                new NightMilestone
                {
                    night = 4,
                    description = "Survive Night 4 - Flamethrower and advanced upgrades",
                    bonusPoints = 300,
                    isCompleted = false
                },
                new NightMilestone
                {
                    night = 5,
                    description = "Survive Night 5 - Victory! All weapons mastered",
                    bonusPoints = 500,
                    isCompleted = false
                }
            };
        }

        private void HandleNightChanged(int newNight)
        {
            CheckWeaponUnlocks(newNight);
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.DawnPhase)
            {
                int currentNight = GameManager.Instance?.CurrentNight ?? 1;
                CompleteMilestone(currentNight);

                if (currentNight > highestNightReached)
                {
                    highestNightReached = currentNight;
                }
            }
            else if (newState == GameState.Victory)
            {
                CompleteMilestone(5);
                highestNightReached = 5;
            }
        }

        private void CheckWeaponUnlocks(int currentNight)
        {
            foreach (var unlock in weaponUnlocks)
            {
                if (!unlock.isUnlocked && unlock.nightRequired <= currentNight)
                {
                    unlock.isUnlocked = true;
                    OnWeaponUnlocked?.Invoke(unlock.weapon);
                    Debug.Log($"[ProgressionManager] Weapon unlocked: {unlock.weapon?.weaponName ?? "Unknown"}");
                }
            }
        }

        private void CompleteMilestone(int night)
        {
            var milestone = nightMilestones.Find(m => m.night == night && !m.isCompleted);
            if (milestone != null)
            {
                milestone.isCompleted = true;

                var pointsSystem = PointsSystem.Instance;
                if (pointsSystem != null && milestone.bonusPoints > 0)
                {
                    pointsSystem.AddPoints(milestone.bonusPoints, $"Night {night} Milestone");
                }

                OnMilestoneCompleted?.Invoke(milestone);
                Debug.Log($"[ProgressionManager] Milestone completed: {milestone.description}");
            }
        }

        public bool PurchaseWeapon(WeaponData weapon)
        {
            var unlock = weaponUnlocks.Find(u => u.weapon == weapon);
            if (unlock == null)
            {
                Debug.Log("[ProgressionManager] Weapon not found in unlock list");
                return false;
            }

            if (!unlock.isUnlocked)
            {
                Debug.Log("[ProgressionManager] Weapon not yet unlocked");
                return false;
            }

            if (unlock.isPurchased)
            {
                Debug.Log("[ProgressionManager] Weapon already purchased");
                return false;
            }

            var pointsSystem = PointsSystem.Instance;
            if (pointsSystem == null || !pointsSystem.CanAfford(unlock.pointCost))
            {
                Debug.Log("[ProgressionManager] Cannot afford weapon");
                return false;
            }

            if (pointsSystem.SpendPoints(unlock.pointCost, $"Purchase {weapon.weaponName}"))
            {
                unlock.isPurchased = true;
                Debug.Log($"[ProgressionManager] Purchased weapon: {weapon.weaponName}");
                return true;
            }

            return false;
        }

        public List<WeaponUnlock> GetAvailableWeapons()
        {
            return weaponUnlocks.FindAll(u => u.isUnlocked);
        }

        public List<WeaponUnlock> GetPurchasedWeapons()
        {
            return weaponUnlocks.FindAll(u => u.isPurchased);
        }

        public List<NightMilestone> GetCompletedMilestones()
        {
            return nightMilestones.FindAll(m => m.isCompleted);
        }

        public void CompleteChallenge(string challengeId, int bonusPoints = 0)
        {
            if (completedChallenges.Contains(challengeId)) return;

            completedChallenges.Add(challengeId);

            if (bonusPoints > 0)
            {
                PointsSystem.Instance?.AddPoints(bonusPoints, $"Challenge: {challengeId}");
            }

            OnChallengeCompleted?.Invoke(challengeId);
            Debug.Log($"[ProgressionManager] Challenge completed: {challengeId}");
        }

        public bool IsChallengeCompleted(string challengeId)
        {
            return completedChallenges.Contains(challengeId);
        }

        public void ResetProgress()
        {
            highestNightReached = 0;
            completedChallenges.Clear();

            foreach (var unlock in weaponUnlocks)
            {
                unlock.isUnlocked = false;
                unlock.isPurchased = false;
            }

            foreach (var milestone in nightMilestones)
            {
                milestone.isCompleted = false;
            }
        }

        public void AddWeaponUnlock(WeaponData weapon, int nightRequired, int pointCost)
        {
            weaponUnlocks.Add(new WeaponUnlock
            {
                weapon = weapon,
                nightRequired = nightRequired,
                pointCost = pointCost,
                isUnlocked = false,
                isPurchased = false
            });
        }
    }
}
