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
        [SerializeField] private int shotsFired = 0;
        [SerializeField] private int hitsLanded = 0;
        [SerializeField] private string highestNightGrade = "D";

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
        public string HighestNightGrade => highestNightGrade;

        public event Action<int> OnPointsChanged;
        public event Action<int, string> OnPointsEarned;
        public event Action<int, string> OnPointsSpent;
        public event Action<NightGradeResult> OnNightGraded;

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

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnEnemyKilled += HandleEnemyKilled;
            }

            ResolvePlayerCombatSubscriptions();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnNightChanged -= HandleNightChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.DawnPhase)
            {
                nightsSurvived++;
                AddPoints(pointsPerNightSurvived + (bonusPointsPerNight * nightsSurvived), "Night Survived");
                GradeCurrentNight();
            }

            if (newState == GameState.GameOver || newState == GameState.Victory)
            {
                var cosmetics = CosmeticUnlockSystem.Instance;
                if (cosmetics != null)
                {
                    cosmetics.RegisterRunResult(new RunSummary
                    {
                        nightsSurvived = nightsSurvived,
                        enemiesKilled = enemiesKilled,
                        finalScore = GetFinalScore(),
                        topGrade = highestNightGrade
                    });
                }
            }
        }

        private void HandleEnemyKilled(int totalKilled)
        {
            enemiesKilled++;
        }

        private void HandleNightChanged(int _)
        {
            ResolvePlayerCombatSubscriptions();
            shotsFired = 0;
            hitsLanded = 0;
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
            shotsFired = 0;
            hitsLanded = 0;
            highestNightGrade = "D";
            pointsHistory.Clear();

            OnPointsChanged?.Invoke(currentPoints);
        }

        public List<PointsEntry> GetPointsHistory()
        {
            return new List<PointsEntry>(pointsHistory);
        }

        private void ResolvePlayerCombatSubscriptions()
        {
            var shooting = FindObjectOfType<Player.PlayerShooting>();
            if (shooting != null)
            {
                shooting.OnWeaponFired -= OnWeaponFired;
                shooting.OnWeaponFired += OnWeaponFired;
            }

            if (GameEffects.Instance != null)
            {
                GameEffects.Instance.OnHitConfirmed -= OnHitConfirmed;
                GameEffects.Instance.OnHitConfirmed += OnHitConfirmed;
            }
        }

        private void OnWeaponFired()
        {
            shotsFired++;
        }

        private void OnHitConfirmed()
        {
            hitsLanded++;
        }

        private void GradeCurrentNight()
        {
            float accuracy = shotsFired <= 0 ? 0.5f : Mathf.Clamp01((float)hitsLanded / shotsFired);
            float damageTaken = 0.3f;
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var playerHealth = player.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    damageTaken = 1f - Mathf.Clamp01(playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth));
                }
            }

            float speedScore = 0.8f;
            var cycle = FindObjectOfType<DayNightCycle>();
            if (cycle != null)
            {
                speedScore = Mathf.Clamp01(1f - cycle.NormalizedTime);
            }

            bool objectiveDone = DayObjectiveSystem.Instance != null &&
                                 DayObjectiveSystem.Instance.ActiveObjective != null &&
                                 DayObjectiveSystem.Instance.ActiveObjective.IsComplete;

            var result = RunGradingSystem.ComputeNightGrade(new NightRunStats
            {
                accuracy = accuracy,
                damageTaken = damageTaken,
                clearSpeedScore = speedScore,
                objectiveCompleted = objectiveDone
            });

            highestNightGrade = CompareGrade(result.grade, highestNightGrade) < 0 ? highestNightGrade : result.grade;
            int bonus = Mathf.RoundToInt(result.bonusPoints * result.multiplier);
            AddPoints(bonus, $"Night Grade {result.grade}");
            OnNightGraded?.Invoke(result);
        }

        private static int CompareGrade(string a, string b)
        {
            int Rank(string g)
            {
                return g switch
                {
                    "S" => 5,
                    "A" => 4,
                    "B" => 3,
                    "C" => 2,
                    _ => 1
                };
            }

            return Rank(a).CompareTo(Rank(b));
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
