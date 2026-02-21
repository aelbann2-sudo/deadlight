using System;
using System.Collections;
using System.Collections.Generic;
using Deadlight.Level;
using Deadlight.Visuals;
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

        [Header("Pacing")]
        [SerializeField] private bool enableDaySkirmish = true;
        [SerializeField] private int daySkirmishMin = 2;
        [SerializeField] private int daySkirmishMax = 4;
        [SerializeField] private float daySkirmishTriggerTime = 20f;
        [SerializeField] private float emergencySpawnDelay = 5f;

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
        private float idleNightTimer;
        private bool daySkirmishTriggered;
        private bool bossSpawned;

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

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (GameManager.Instance.CurrentState == GameState.DayPhase)
            {
                TrySpawnDaySkirmish();
                idleNightTimer = 0f;
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.NightPhase)
            {
                idleNightTimer = 0f;
                return;
            }

            bool stalled = !isSpawning && enemiesRemaining <= 0;
            if (stalled)
            {
                idleNightTimer += Time.deltaTime;
                if (idleNightTimer >= emergencySpawnDelay)
                {
                    SpawnEmergencyBurst();
                    idleNightTimer = 0f;
                }
            }
            else
            {
                idleNightTimer = 0f;
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
                daySkirmishTriggered = false;
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

            if (nightSequenceCoroutine != null)
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
                int waveEnemyCount = CalculateEnemyCount(wave);
                int waveOverlapThreshold = Mathf.RoundToInt(waveEnemyCount * 0.3f);
                OnWaveStarted?.Invoke(wave);

                yield return StartCoroutine(SpawnWave(wave));

                while (enemiesRemaining > waveOverlapThreshold)
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
                    float interval = Mathf.Clamp((currentNightConfig?.timeBetweenWaves ?? 2f), 1f, 2.5f);
                    yield return new WaitForSeconds(interval);
                }
            }

            while (enemiesRemaining > 0)
            {
                if (GameManager.Instance?.CurrentState != GameState.NightPhase)
                {
                    yield break;
                }
                yield return null;
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

            var spawnPosition = GetSpawnPosition(out SpawnPoint usedSpawnPoint);
            if (spawnPosition == Vector3.zero && spawnPoints.Count == 0)
            {
                Debug.LogWarning("[WaveManager] No spawn points available");
                return;
            }

            int nightNum = GameManager.Instance?.CurrentNight ?? 1;
            var spawnType = SelectSpawnType(nightNum);
            var enemyType = spawnType switch
            {
                SpawnType.Runner => Visuals.ProceduralSpriteGenerator.ZombieType.Runner,
                SpawnType.Exploder => Visuals.ProceduralSpriteGenerator.ZombieType.Exploder,
                SpawnType.Tank => Visuals.ProceduralSpriteGenerator.ZombieType.Tank,
                _ => Visuals.ProceduralSpriteGenerator.ZombieType.Basic
            };

            GameObject enemy;
            if (basicZombiePrefab != null && spawnType == SpawnType.Basic)
            {
                enemy = Instantiate(basicZombiePrefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                enemy = CreateEnemyOfType(enemyType, spawnPosition);
            }
            enemy.SetActive(true);

            if (spawnType == SpawnType.Spitter)
            {
                var existingAI = enemy.GetComponent<Enemy.SimpleEnemyAI>();
                if (existingAI != null) Destroy(existingAI);
                enemy.AddComponent<Enemy.SpitterAI>();
                var sr = enemy.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.4f, 0.9f, 0.2f);
            }

            if (usedSpawnPoint != null)
            {
                var tracker = enemy.GetComponent<SpawnPointOccupancyTracker>();
                if (tracker == null)
                    tracker = enemy.AddComponent<SpawnPointOccupancyTracker>();
                tracker.Initialize(usedSpawnPoint);
            }

            ApplyNightModifiers(enemy);

            if (enemy.GetComponent<Visuals.ZombieAnimator>() == null)
                enemy.AddComponent<Visuals.ZombieAnimator>();
            if (enemy.GetComponent<Audio.ZombieSounds>() == null)
                enemy.AddComponent<Audio.ZombieSounds>();

            if (UnityEngine.Random.value < 0.1f && nightNum >= 2)
            {
                var affix = enemy.AddComponent<Enemy.EnemyAffix>();
                affix.SetAffix(Enemy.EnemyAffix.GetRandomAffix());
            }

            var worldUi = enemy.GetComponent<Deadlight.UI.EnemyWorldUI>();
            if (worldUi == null)
                worldUi = enemy.AddComponent<Deadlight.UI.EnemyWorldUI>();
            var health = enemy.GetComponent<Enemy.EnemyHealth>();
            if (health != null)
                worldUi.Bind(health);

            totalEnemiesSpawned++;
            enemiesRemaining++;
        }

        private enum SpawnType { Basic, Runner, Exploder, Tank, Spitter }

        private SpawnType SelectSpawnType(int night)
        {
            float roll = UnityEngine.Random.value;

            if (night >= 5 && currentWave >= (currentNightConfig?.waveCount ?? 3) && !bossSpawned)
            {
                bossSpawned = true;
                return SpawnType.Tank;
            }

            if (night >= 4 && roll < 0.10f)
                return SpawnType.Tank;
            if (night >= 3 && roll < 0.22f)
                return SpawnType.Spitter;
            if (night >= 3 && roll < 0.35f)
                return SpawnType.Exploder;
            if (night >= 2 && roll < 0.55f)
                return SpawnType.Runner;

            return SpawnType.Basic;
        }

        private Visuals.ProceduralSpriteGenerator.ZombieType SelectEnemyType(int night)
        {
            var spawn = SelectSpawnType(night);
            return spawn switch
            {
                SpawnType.Runner => Visuals.ProceduralSpriteGenerator.ZombieType.Runner,
                SpawnType.Exploder => Visuals.ProceduralSpriteGenerator.ZombieType.Exploder,
                SpawnType.Tank => Visuals.ProceduralSpriteGenerator.ZombieType.Tank,
                SpawnType.Spitter => Visuals.ProceduralSpriteGenerator.ZombieType.Basic,
                _ => Visuals.ProceduralSpriteGenerator.ZombieType.Basic
            };
        }

        private GameObject CreateEnemyOfType(Visuals.ProceduralSpriteGenerator.ZombieType type, Vector3 position)
        {
            var go = new GameObject($"Zombie_{type}");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Visuals.ProceduralSpriteGenerator.CreateZombieSprite(type, 0, 0);
            sr.sortingOrder = 9;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            go.AddComponent<CircleCollider2D>().radius = 0.35f;

            var health = go.AddComponent<Enemy.EnemyHealth>();
            var ai = go.AddComponent<Enemy.SimpleEnemyAI>();

            switch (type)
            {
                case Visuals.ProceduralSpriteGenerator.ZombieType.Runner:
                    health.SetMaxHealth(30f);
                    health.SetPointsOnDeath(15);
                    ai.ApplySpeedMultiplier(1.8f);
                    break;
                case Visuals.ProceduralSpriteGenerator.ZombieType.Tank:
                    health.SetMaxHealth(200f);
                    health.SetPointsOnDeath(50);
                    ai.ApplySpeedMultiplier(0.6f);
                    ai.ApplyDamageMultiplier(2.5f);
                    go.transform.localScale = Vector3.one * 1.5f;
                    break;
                case Visuals.ProceduralSpriteGenerator.ZombieType.Exploder:
                    health.SetMaxHealth(40f);
                    health.SetPointsOnDeath(20);
                    health.SetIsExploder(true);
                    ai.ApplySpeedMultiplier(1.2f);
                    break;
                default:
                    health.SetMaxHealth(50f);
                    health.SetPointsOnDeath(10);
                    break;
            }

            if (type == Visuals.ProceduralSpriteGenerator.ZombieType.Tank &&
                GameManager.Instance?.CurrentNight >= 5 && !bossSpawned)
            {
                health.SetMaxHealth(1000f);
                health.SetPointsOnDeath(200);
                go.AddComponent<Enemy.BossController>();
                go.name = "Subject23_Boss";
            }

            return go;
        }

        private Vector3 GetSpawnPosition(out SpawnPoint usedSpawnPoint)
        {
            usedSpawnPoint = null;

            var player = GameObject.Find("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
            
            bool useFlankSpawn = UnityEngine.Random.value < 0.3f;
            
            if (useFlankSpawn && player != null)
            {
                Vector3 flankPos = GetFlankSpawnPosition(player);
                if (flankPos != Vector3.zero)
                {
                    return flankPos;
                }
            }

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

            return playerPos + (Vector3)(UnityEngine.Random.insideUnitCircle.normalized * 8f);
        }
        
        private Vector3 GetFlankSpawnPosition(GameObject player)
        {
            var playerRb = player.GetComponent<Rigidbody2D>();
            Vector2 playerFacing = Vector2.right;
            
            if (playerRb != null && playerRb.linearVelocity.sqrMagnitude > 0.1f)
            {
                playerFacing = playerRb.linearVelocity.normalized;
            }
            else
            {
                var shooting = player.GetComponent<Player.PlayerShooting>();
                if (shooting != null)
                {
                    Vector3 mouseWorld = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
                    playerFacing = ((Vector2)mouseWorld - (Vector2)player.transform.position).normalized;
                }
            }
            
            Vector2 behindPlayer = -playerFacing;
            Vector2 perpendicular = new Vector2(-playerFacing.y, playerFacing.x);
            
            float spawnDist = UnityEngine.Random.Range(6f, 9f);
            float lateralOffset = UnityEngine.Random.Range(-3f, 3f);
            
            Vector2 spawnDir = (behindPlayer + perpendicular * (lateralOffset / spawnDist)).normalized;
            Vector3 spawnPos = player.transform.position + (Vector3)(spawnDir * spawnDist);
            
            spawnPos.x = Mathf.Clamp(spawnPos.x, -11f, 11f);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -11f, 11f);
            
            return spawnPos;
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
            float objectiveBuff = 1f;

            if (GameManager.Instance?.CurrentSettings != null)
            {
                var settings = GameManager.Instance.CurrentSettings;
                healthMultiplier *= settings.enemyHealthMultiplier;
                damageMultiplier *= settings.enemyDamageMultiplier;
                speedMultiplier *= settings.enemySpeedMultiplier;
            }

            if (DayObjectiveSystem.Instance != null)
            {
                objectiveBuff = DayObjectiveSystem.Instance.ActiveNightBuffMultiplier;
                healthMultiplier /= objectiveBuff;
                damageMultiplier /= objectiveBuff;
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

            if (RunModifierSystem.Instance != null)
            {
                RunModifierSystem.Instance.ApplyToEnemy(enemy);
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

        private void TrySpawnDaySkirmish()
        {
            if (!enableDaySkirmish || daySkirmishTriggered || dayNightCycle == null)
            {
                return;
            }

            if (!dayNightCycle.IsDay || dayNightCycle.TimeRemaining > daySkirmishTriggerTime)
            {
                return;
            }

            daySkirmishTriggered = true;
            int count = UnityEngine.Random.Range(daySkirmishMin, daySkirmishMax + 1);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
            }
        }

        private void SpawnEmergencyBurst()
        {
            if (GameManager.Instance?.CurrentState != GameState.NightPhase)
            {
                return;
            }

            int burstCount = Mathf.Clamp(2 + (GameManager.Instance.CurrentNight / 2), 2, 6);
            for (int i = 0; i < burstCount; i++)
            {
                SpawnEnemy();
            }
        }

        public void RegisterEnemyDeath()
        {
            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            totalEnemiesKilled++;
            OnEnemyKilled?.Invoke(totalEnemiesKilled);

            if (GameManager.Instance != null &&
                GameManager.Instance.CurrentState == GameState.DayPhase &&
                DayObjectiveSystem.Instance != null)
            {
                DayObjectiveSystem.Instance.AddProgress(1);
            }
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
