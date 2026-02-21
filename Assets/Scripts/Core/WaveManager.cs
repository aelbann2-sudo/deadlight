using System;
using System.Collections;
using System.Collections.Generic;
using Deadlight.Level;
using UnityEngine;

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
        private Coroutine nightSequenceCoroutine;
        private DayNightCycle dayNightCycle;
        private GameObject runtimeZombiePrefab;

        private void Start()
        {
            EnsureRuntimeDefaults();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            dayNightCycle = FindObjectOfType<DayNightCycle>();
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

            if (dayNightCycle != null)
            {
                dayNightCycle.OnNightStart -= StartNightWaves;
            }
        }

        public void EnsureRuntimeDefaults()
        {
            if (nightConfigs == null || nightConfigs.Length == 0)
            {
                nightConfigs = new[]
                {
                    NightConfig.CreateNight1(),
                    NightConfig.CreateNight2(),
                    NightConfig.CreateNight3(),
                    NightConfig.CreateNight4(),
                    NightConfig.CreateNight5()
                };
            }

            if (basicZombiePrefab == null)
            {
                basicZombiePrefab = GetOrCreateRuntimeZombiePrefab();
            }

            if (spawnPoints.Count == 0)
            {
                foreach (var spawner in FindObjectsOfType<Enemy.EnemySpawner>())
                {
                    AddSpawnPoint(spawner.transform);
                }
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.DayPhase)
            {
                ResetForNewNight();
                StopAllSpawning();
            }
            else if (newState != GameState.NightPhase)
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

            if (nightConfigs != null && nightIndex >= 0 && nightIndex < nightConfigs.Length && nightConfigs[nightIndex] != null)
            {
                currentNightConfig = nightConfigs[nightIndex];
            }
            else
            {
                currentNightConfig = CreateDefaultNightConfig(Mathf.Max(1, nightIndex + 1));
            }
        }

        private NightConfig CreateDefaultNightConfig(int night)
        {
            var config = ScriptableObject.CreateInstance<NightConfig>();
            config.nightNumber = night;
            config.waveCount = Mathf.Clamp(2 + night, 2, 6);
            config.baseEnemyCount = 5 + (night * 3);
            config.healthMultiplier = 1f + (night * 0.25f);
            config.damageMultiplier = 1f + (night * 0.15f);
            config.speedMultiplier = 1f + (night * 0.1f);
            config.spawnInterval = Mathf.Max(0.8f, 2.5f - (night * 0.2f));
            config.timeBetweenWaves = Mathf.Max(2f, 6f - night);
            return config;
        }

        public void StartNightWaves()
        {
            if (GameManager.Instance?.CurrentState != GameState.NightPhase)
            {
                return;
            }

            EnsureRuntimeDefaults();
            LoadNightConfig();

            StopAllSpawning();
            nightSequenceCoroutine = StartCoroutine(NightWaveSequence());
        }

        private IEnumerator NightWaveSequence()
        {
            int waveCount = Mathf.Max(1, currentNightConfig?.waveCount ?? 3);

            for (int wave = 1; wave <= waveCount; wave++)
            {
                if (GameManager.Instance?.CurrentState != GameState.NightPhase)
                {
                    yield break;
                }

                currentWave = wave;
                OnWaveStarted?.Invoke(wave);

                yield return StartCoroutine(SpawnWave(wave));

                while (enemiesRemaining > 0)
                {
                    if (GameManager.Instance?.CurrentState != GameState.NightPhase)
                    {
                        yield break;
                    }

                    yield return null;
                }

                OnWaveCompleted?.Invoke(wave);

                if (wave < waveCount)
                {
                    float interval = (currentNightConfig?.timeBetweenWaves ?? 5f);
                    yield return new WaitForSeconds(interval);
                }
            }

            OnAllWavesCompleted?.Invoke();
            nightSequenceCoroutine = null;
        }

        private IEnumerator SpawnWave(int waveNumber)
        {
            isSpawning = true;

            int enemyCount = CalculateEnemyCount(waveNumber);
            float spawnInterval = Mathf.Max(0.1f, (currentNightConfig?.spawnInterval ?? 2f) * GetSpawnIntervalMultiplier());

            for (int i = 0; i < enemyCount; i++)
            {
                if (GameManager.Instance?.CurrentState != GameState.NightPhase)
                {
                    break;
                }

                SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }

            isSpawning = false;
        }

        private int CalculateEnemyCount(int waveNumber)
        {
            int baseCount = currentNightConfig?.baseEnemyCount ?? 10;
            float waveScaling = 1f + (waveNumber - 1) * 0.3f;
            float difficultyMultiplier = GetDifficultyWaveMultiplier();

            return Mathf.Max(1, Mathf.RoundToInt(baseCount * waveScaling * difficultyMultiplier));
        }

        private float GetDifficultyWaveMultiplier()
        {
            if (GameManager.Instance?.CurrentSettings != null)
            {
                return Mathf.Max(0.2f, GameManager.Instance.CurrentSettings.waveEnemyCountMultiplier);
            }

            if (GameManager.Instance == null)
            {
                return 1f;
            }

            return GameManager.Instance.CurrentDifficulty switch
            {
                Difficulty.Easy => 0.7f,
                Difficulty.Normal => 1f,
                Difficulty.Hard => 1.4f,
                _ => 1f
            };
        }

        private float GetSpawnIntervalMultiplier()
        {
            if (GameManager.Instance?.CurrentSettings != null)
            {
                return Mathf.Max(0.2f, GameManager.Instance.CurrentSettings.spawnIntervalMultiplier);
            }

            return 1f;
        }

        private void SpawnEnemy()
        {
            EnsureRuntimeDefaults();

            if (basicZombiePrefab == null)
            {
                Debug.LogWarning("[WaveManager] No zombie prefab available");
                return;
            }

            var spawnPosition = GetSpawnPosition(out SpawnPoint usedSpawnPoint);
            if (spawnPosition == Vector3.zero && spawnPoints.Count == 0)
            {
                Debug.LogWarning("[WaveManager] No spawn points available");
                return;
            }

            GameObject enemy = Instantiate(basicZombiePrefab, spawnPosition, Quaternion.identity);
            enemy.SetActive(true);

            if (!enemy.CompareTag("Enemy"))
            {
                enemy.tag = "Enemy";
            }

            if (usedSpawnPoint != null)
            {
                var tracker = enemy.GetComponent<SpawnPointOccupancyTracker>();
                if (tracker == null)
                {
                    tracker = enemy.AddComponent<SpawnPointOccupancyTracker>();
                }

                tracker.Initialize(usedSpawnPoint);
            }

            ApplyNightModifiers(enemy);

            totalEnemiesSpawned++;
            enemiesRemaining++;
        }

        private Vector3 GetSpawnPosition(out SpawnPoint usedSpawnPoint)
        {
            usedSpawnPoint = null;

            var player = GameObject.FindGameObjectWithTag("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            if (LevelManager.Instance != null)
            {
                int night = GameManager.Instance?.CurrentNight ?? 1;
                var spawnPoint = LevelManager.Instance.GetRandomSpawnPoint(night, playerPos);

                if (spawnPoint != null)
                {
                    usedSpawnPoint = spawnPoint;
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
            if (currentNightConfig == null)
            {
                return;
            }

            float healthMultiplier = currentNightConfig.healthMultiplier;
            float damageMultiplier = currentNightConfig.damageMultiplier;
            float speedMultiplier = currentNightConfig.speedMultiplier;

            if (GameManager.Instance?.CurrentSettings != null)
            {
                var settings = GameManager.Instance.CurrentSettings;
                healthMultiplier *= settings.enemyHealthMultiplier;
                damageMultiplier *= settings.enemyDamageMultiplier;
                speedMultiplier *= settings.enemySpeedMultiplier;
            }

            var enemyHealth = enemy.GetComponent<Enemy.EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ApplyHealthMultiplier(healthMultiplier);
            }

            var enemyAI = enemy.GetComponent<Enemy.EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.ApplyDamageMultiplier(damageMultiplier);
                enemyAI.ApplySpeedMultiplier(speedMultiplier);
            }

            var simpleAI = enemy.GetComponent<Enemy.SimpleEnemyAI>();
            if (simpleAI != null)
            {
                simpleAI.ApplyDamageMultiplier(damageMultiplier);
                simpleAI.ApplySpeedMultiplier(speedMultiplier);
            }
        }

        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints.Count == 0)
            {
                return null;
            }

            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        }

        public void RegisterEnemyDeath()
        {
            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            totalEnemiesKilled++;
            OnEnemyKilled?.Invoke(totalEnemiesKilled);
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
            if (nightSequenceCoroutine != null)
            {
                StopCoroutine(nightSequenceCoroutine);
                nightSequenceCoroutine = null;
            }

            StopAllCoroutines();
            isSpawning = false;
        }

        public void SetZombiePrefab(GameObject prefab)
        {
            basicZombiePrefab = prefab;
        }

        private GameObject GetOrCreateRuntimeZombiePrefab()
        {
            if (runtimeZombiePrefab != null)
            {
                return runtimeZombiePrefab;
            }

            runtimeZombiePrefab = new GameObject("RuntimeZombiePrefab");
            runtimeZombiePrefab.SetActive(false);

            var spriteRenderer = runtimeZombiePrefab.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateRuntimeCircleSprite(new Color(0.35f, 0.75f, 0.35f), 24, 10f);
            spriteRenderer.sortingOrder = 9;

            var rb = runtimeZombiePrefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = runtimeZombiePrefab.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            runtimeZombiePrefab.AddComponent<Enemy.SimpleEnemyAI>();
            runtimeZombiePrefab.AddComponent<Enemy.EnemyHealth>();
            runtimeZombiePrefab.tag = "Enemy";

            return runtimeZombiePrefab;
        }

        private static Sprite CreateRuntimeCircleSprite(Color color, int size, float radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            var center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = distance <= radius ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    public class SpawnPointOccupancyTracker : MonoBehaviour
    {
        private SpawnPoint spawnPoint;
        private bool released;

        public void Initialize(SpawnPoint point)
        {
            spawnPoint = point;
            released = false;
        }

        public void Release()
        {
            if (released)
            {
                return;
            }

            released = true;
            spawnPoint?.OnEnemyDied();
        }

        private void OnDestroy()
        {
            Release();
        }
    }
}
