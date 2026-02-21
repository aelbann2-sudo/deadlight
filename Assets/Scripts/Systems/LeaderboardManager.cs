using System;
using System.Collections.Generic;
using UnityEngine;
using Deadlight.Core;

namespace Deadlight.Systems
{
    [Serializable]
    public class LeaderboardEntry
    {
        public int score;
        public int nightsReached;
        public int kills;
        public string difficulty;
        public string map;
        public float runTimeSeconds;
        public bool victory;
        public string date;
    }

    [Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        private const string PlayerPrefsKey = "Deadlight_Leaderboard";
        private const int MaxEntries = 10;

        private LeaderboardData data;

        public IReadOnlyList<LeaderboardEntry> Entries => data.entries;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Load();
        }

        public void SubmitRun(bool victory)
        {
            var entry = new LeaderboardEntry();

            int rawScore = 0;
            int kills = 0;
            int nightsReached = 1;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                rawScore = stats.totalEarned;
                kills = stats.enemiesKilled;
                nightsReached = stats.nightsSurvived;
            }

            if (GameManager.Instance != null)
            {
                nightsReached = GameManager.Instance.CurrentNight;
            }

            float difficultyMultiplier = GetDifficultyMultiplier();
            float timeBonus = CalculateTimeBonus();

            entry.score = Mathf.RoundToInt((rawScore + timeBonus) * difficultyMultiplier);
            entry.nightsReached = nightsReached;
            entry.kills = kills;
            entry.victory = victory;
            entry.runTimeSeconds = GameManager.Instance != null
                ? Time.realtimeSinceStartup - GameManager.Instance.RunStartTime
                : 0f;
            entry.difficulty = GameManager.Instance != null
                ? GameManager.Instance.CurrentDifficulty.ToString()
                : "Normal";
            entry.map = GameManager.Instance != null
                ? GameManager.Instance.SelectedMap.ToString()
                : "TownCenter";
            entry.date = DateTime.Now.ToString("yyyy-MM-dd");

            data.entries.Add(entry);
            data.entries.Sort((a, b) => b.score.CompareTo(a.score));

            if (data.entries.Count > MaxEntries)
            {
                data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);
            }

            Save();
            Debug.Log($"[Leaderboard] Submitted: score={entry.score}, nights={entry.nightsReached}, victory={entry.victory}");
        }

        public int GetRank(int score)
        {
            for (int i = 0; i < data.entries.Count; i++)
            {
                if (score >= data.entries[i].score) return i + 1;
            }
            return data.entries.Count + 1;
        }

        private float GetDifficultyMultiplier()
        {
            if (GameManager.Instance == null) return 1f;
            return GameManager.Instance.CurrentDifficulty switch
            {
                Difficulty.Easy => 0.75f,
                Difficulty.Normal => 1.0f,
                Difficulty.Hard => 1.5f,
                _ => 1f
            };
        }

        private float CalculateTimeBonus()
        {
            if (GameManager.Instance == null) return 0;
            float elapsed = Time.realtimeSinceStartup - GameManager.Instance.RunStartTime;
            float nightsCleared = GameManager.Instance.CurrentNight - 1;
            if (nightsCleared <= 0) return 0;
            float avgTimePerNight = elapsed / nightsCleared;
            float bonus = Mathf.Max(0, (60f - avgTimePerNight) * 2f);
            return bonus * nightsCleared;
        }

        private void Load()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    data = JsonUtility.FromJson<LeaderboardData>(json);
                }
                catch
                {
                    data = new LeaderboardData();
                }
            }
            else
            {
                data = new LeaderboardData();
            }
        }

        private void Save()
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
        }
    }
}
