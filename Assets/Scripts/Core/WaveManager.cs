using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Deadlight.Level;

namespace Deadlight.Core
{
    [Serializable]
    public class WaveData
    {
        public int enemyCount = 10;
        public float spawnInterval = 2f;
        public float timeBetweenWaves = 5f;
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Settings")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private GameObject basicZombiePrefab;

        [Header("Night Configuration")]
        [SerializeField] private NightConfig[] nightConfigs;

        [Header("Current State")]
        [SerializeField] private int currentWave = 0;
        [SerializeField] private int enemiesRemaining = 0;
        [SerializeField] private int totalEnemiesSpawned = 0;
        [SerializeField] private int totalEnemiesKilled = 0;
        [SerializeField] private bool isSpawning = false;

        public int CurrentWave => currentWave;
        public int EnemiesRemaining => enemiesRemaining;
        public int TotalEnemiesKilled => totalEnemiesKilled;
        public bool IsSpawning => isSpawning;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;
        public event Action<int> OnEnemyKilled;

        private NightConfig currentNightConfig;
        private Coroutine spawnCoroutine;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            var dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnNightStart += StartNightWaves;
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
            if (newState == GameState.DayPhase)
            {
                ResetForNewNight();
            }
            else if (newState == GameState.GameOver || newState == GameState.Victory)
            {
                StopAllSpawning();
            }
        }

        private void ResetForNewNight()
        {
            currentWave = 0;
            totalEnemiesSpawned = 0;
            enemiesRemaining = 0;
            isSpawning = false;

            LoadNightConfig();
        }

        private void LoadNightConfig()
        {
            int nightIndex = GameManager.Instance != null ? GameManager.Instance.CurrentNight - 1 : 0;

            if (nightConfigs != null && nightIndex < nightConfigs.Length && nightConfigs[nightIndex] != null)
            {
                currentNightConfig = nightConfigs[nightIndex];
            }
            else
            {
                currentNightConfig = CreateDefaultNightConfig(nightIndex + 1);
            }
        }

        private NightConfig CreateDefaultNightConfig(int night)
        {
            var config = ScriptableObject.CreateInstance<NightConfig>();
            config.nightNumber = night;
            config.waveCount = 2 + night;
            config.baseEnemyCount = 5 + (night * 3);
            config.healthMultiplier = 1f + (night * 0.25f);
            config.damageMultiplier = 1f + (night * 0.15f);
            config.spawnInterval = Mathf.Max(1f, 2.5f - (night * 0.2f));
            return config;
        }

        public void StartNightWaves()
        {
            LoadNightConfig();
            StartCoroutine(NightWaveSequence());
        }

        private IEnumerator NightWaveSequence()
        {
            int waveCount = currentNightConfig?.waveCount ?? 3;

            for (int wave = 1; wave <= waveCount; wave++)
            {
                currentWave = wave;
                OnWaveStarted?.Invoke(wave);

                Debug.Log($"[WaveManager] Starting wave {wave} of {waveCount}");

                yield return StartCoroutine(SpawnWave(wave));

                yield return new WaitUntil(() => enemiesRemaining <= 0);

                OnWaveCompleted?.Invoke(wave);

                if (wave < waveCount)
                {
                    yield return new WaitForSeconds(currentNightConfig?.timeBetweenWaves ?? 5f);
                }
            }

            OnAllWavesCompleted?.Invoke();
            Debug.Log("[WaveManager] All waves completed!");
        }

        private IEnumerator SpawnWave(int waveNumber)
        {
            isSpawning = true;

            int enemyCount = CalculateEnemyCount(waveNumber);
            float spawnInterval = currentNightConfig?.spawnInterval ?? 2f;

            for (int i = 0; i < enemyCount; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            isSpawning = false;
        }

        private int CalculateEnemyCount(int waveNumber)
        {
            int baseCount = currentNightConfig?.baseEnemyCount ?? 10;
            float difficultyMultiplier = GetDifficultyMultiplier();

            return Mathf.RoundToInt(baseCount * (1 + (waveNumber - 1) * 0.3f) * difficultyMultiplier);
        }

        private float GetDifficultyMultiplier()
        {
            if (GameManager.Instance == null) return 1f;

            return GameManager.Instance.CurrentDifficulty switch
            {
                Difficulty.Easy => 0.7f,
                Difficulty.Normal => 1f,
                Difficulty.Hard => 1.4f,
                _ => 1f
            };
        }

        private void SpawnEnemy()
        {
            if (basicZombiePrefab == null)
            {
                Debug.LogWarning("[WaveManager] No zombie prefab assigned!");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition();
            if (spawnPosition == Vector3.zero && spawnPoints.Count == 0)
            {
                Debug.LogWarning("[WaveManager] No spawn points available!");
                return;
            }

            GameObject enemy = Instantiate(basicZombiePrefab, spawnPosition, Quaternion.identity);

            ApplyNightModifiers(enemy);

            totalEnemiesSpawned++;
            enemiesRemaining++;
        }

        private Vector3 GetSpawnPosition()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            if (LevelManager.Instance != null)
            {
                int currentNight = GameManager.Instance?.CurrentNight ?? 1;
                var spawnPoint = LevelManager.Instance.GetRandomSpawnPoint(currentNight, playerPos);
                
                if (spawnPoint != null)
                {
                    spawnPoint.OnEnemySpawned();
                    return spawnPoint.GetSpawnPosition();
                }
            }

            Transform legacyPoint = GetRandomSpawnPoint();
            if (legacyPoint != null)
            {
                return legacyPoint.position + (Vector3)UnityEngine.Random.insideUnitCircle * 2f;
            }

            return playerPos + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * 10f);
        }

        private void ApplyNightModifiers(GameObject enemy)
        {
            if (currentNightConfig == null) return;

            var enemyHealth = enemy.GetComponent<Enemy.EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ApplyHealthMultiplier(currentNightConfig.healthMultiplier);
            }

            var enemyAI = enemy.GetComponent<Enemy.EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.ApplyDamageMultiplier(currentNightConfig.damageMultiplier);
            }
        }

        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints.Count == 0) return null;
            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        }

        public void RegisterEnemyDeath()
        {
            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            totalEnemiesKilled++;
            OnEnemyKilled?.Invoke(totalEnemiesKilled);

            Debug.Log($"[WaveManager] Enemy killed. Remaining: {enemiesRemaining}");
        }

        public void AddSpawnPoint(Transform point)
        {
            if (!spawnPoints.Contains(point))
            {
                spawnPoints.Add(point);
            }
        }

        public void RemoveSpawnPoint(Transform point)
        {
            spawnPoints.Remove(point);
        }

        private void StopAllSpawning()
        {
            StopAllCoroutines();
            isSpawning = false;
        }

        public void SetZombiePrefab(GameObject prefab)
        {
            basicZombiePrefab = prefab;
        }
    }
}
