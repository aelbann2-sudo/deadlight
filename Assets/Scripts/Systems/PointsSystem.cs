using UnityEngine;
using Deadlight.Core;
using System;
using System.Collections.Generic;

namespace Deadlight.Systems
{
    [Serializable]
    public class PointsEntry
    {
        public string source;
        public int points;
        public float timestamp;

        public PointsEntry(string source, int points)
        {
            this.source = source;
            this.points = points;
            this.timestamp = Time.time;
        }
    }

    public class PointsSystem : MonoBehaviour
    {
        public static PointsSystem Instance { get; private set; }

        [Header("Current Session")]
        [SerializeField] private int currentPoints = 0;
        [SerializeField] private int totalPointsEarned = 0;
        [SerializeField] private int totalPointsSpent = 0;

        [Header("Statistics")]
        [SerializeField] private int enemiesKilled = 0;
        [SerializeField] private int nightsSurvived = 0;

        [Header("Point Values")]
        [SerializeField] private int pointsPerKill = 10;
        [SerializeField] private int pointsPerNightSurvived = 100;
        [SerializeField] private int bonusPointsPerNight = 50;

        private List<PointsEntry> pointsHistory = new List<PointsEntry>();

        public int CurrentPoints => currentPoints;
        public int TotalEarned => totalPointsEarned;
        public int TotalSpent => totalPointsSpent;
        public int EnemiesKilled => enemiesKilled;
        public int NightsSurvived => nightsSurvived;
        public float ScoreMultiplier => GetScoreMultiplier();

        public event Action<int> OnPointsChanged;
        public event Action<int, string> OnPointsEarned;
        public event Action<int, string> OnPointsSpent;

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

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnEnemyKilled += HandleEnemyKilled;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.DawnPhase)
            {
                nightsSurvived++;
                AddPoints(pointsPerNightSurvived + (bonusPointsPerNight * nightsSurvived), "Night Survived");
            }
        }

        private void HandleEnemyKilled(int totalKilled)
        {
            enemiesKilled++;
        }

        public void AddPoints(int amount, string source = "Unknown")
        {
            // Keep spendable currency consistent across difficulties.
            // Difficulty affects leaderboard score in GetFinalScore().
            int adjustedAmount = Mathf.Max(0, amount);

            currentPoints += adjustedAmount;
            totalPointsEarned += adjustedAmount;

            pointsHistory.Add(new PointsEntry(source, adjustedAmount));

            OnPointsEarned?.Invoke(adjustedAmount, source);
            OnPointsChanged?.Invoke(currentPoints);

            Debug.Log($"[PointsSystem] +{adjustedAmount} points ({source}). Total: {currentPoints}");
        }

        public bool SpendPoints(int amount, string purpose = "Purchase")
        {
            if (amount > currentPoints)
            {
                Debug.Log($"[PointsSystem] Cannot spend {amount} points. Only have {currentPoints}");
                return false;
            }

            currentPoints -= amount;
            totalPointsSpent += amount;

            pointsHistory.Add(new PointsEntry($"-{purpose}", -amount));

            OnPointsSpent?.Invoke(amount, purpose);
            OnPointsChanged?.Invoke(currentPoints);

            Debug.Log($"[PointsSystem] -{amount} points ({purpose}). Remaining: {currentPoints}");
            return true;
        }

        public bool CanAfford(int amount)
        {
            return currentPoints >= amount;
        }

        private float GetScoreMultiplier()
        {
            if (GameManager.Instance?.CurrentSettings != null)
            {
                return GameManager.Instance.CurrentSettings.scoreMultiplier;
            }
            return 1f;
        }

        public int GetFinalScore()
        {
            int baseScore = totalPointsEarned;
            int nightBonus = nightsSurvived * 500;
            int killBonus = enemiesKilled * 5;

            return Mathf.RoundToInt((baseScore + nightBonus + killBonus) * ScoreMultiplier);
        }

        public GameStats GetGameStats()
        {
            return new GameStats
            {
                totalPoints = currentPoints,
                totalEarned = totalPointsEarned,
                totalSpent = totalPointsSpent,
                enemiesKilled = enemiesKilled,
                nightsSurvived = nightsSurvived,
                finalScore = GetFinalScore(),
                difficulty = GameManager.Instance?.CurrentDifficulty ?? Difficulty.Normal
            };
        }

        public void ResetSession()
        {
            currentPoints = 0;
            totalPointsEarned = 0;
            totalPointsSpent = 0;
            enemiesKilled = 0;
            nightsSurvived = 0;
            pointsHistory.Clear();

            OnPointsChanged?.Invoke(currentPoints);
        }

        public List<PointsEntry> GetPointsHistory()
        {
            return new List<PointsEntry>(pointsHistory);
        }
    }

    [Serializable]
    public struct GameStats
    {
        public int totalPoints;
        public int totalEarned;
        public int totalSpent;
        public int enemiesKilled;
        public int nightsSurvived;
        public int finalScore;
        public Difficulty difficulty;
    }
}
