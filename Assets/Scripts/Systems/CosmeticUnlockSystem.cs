using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadlight.Systems
{
    [Serializable]
    public struct RunSummary
    {
        public int nightsSurvived;
        public int enemiesKilled;
        public int finalScore;
        public string topGrade;
    }

    public class CosmeticUnlockSystem : MonoBehaviour
    {
        public static CosmeticUnlockSystem Instance { get; private set; }

        [SerializeField] private List<string> unlockedCosmetics = new List<string>();

        public IReadOnlyList<string> UnlockedCosmetics => unlockedCosmetics;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultUnlocks();
        }

        public void RegisterRunResult(RunSummary summary)
        {
            if (summary.nightsSurvived >= 1)
            {
                Unlock("Title: Night Watch");
            }

            if (summary.nightsSurvived >= 3)
            {
                Unlock("Badge: Dark Survivor");
            }

            if (summary.nightsSurvived >= 5)
            {
                Unlock("Skin: Extraction Veteran");
            }

            if (summary.topGrade == "S")
            {
                Unlock("Title: Apex Ranger");
            }

            if (summary.enemiesKilled >= 200)
            {
                Unlock("Palette: Bloodline Crimson");
            }
        }

        public IReadOnlyList<string> GetUnlockedCosmetics()
        {
            return unlockedCosmetics;
        }

        private void Unlock(string cosmetic)
        {
            if (!unlockedCosmetics.Contains(cosmetic))
            {
                unlockedCosmetics.Add(cosmetic);
            }
        }

        private void EnsureDefaultUnlocks()
        {
            if (!unlockedCosmetics.Contains("Title: Recruit"))
            {
                unlockedCosmetics.Add("Title: Recruit");
            }
        }
    }
}
