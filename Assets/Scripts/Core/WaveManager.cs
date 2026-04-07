using System;
using System.Collections;
using System.Collections.Generic;
using Deadlight.Data;
using Deadlight.Level;
using Deadlight.Systems;
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
        public static WaveManager Instance { get; private set; }

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
        [SerializeField] private int daySkirmishMin = 1;
        [SerializeField] private int daySkirmishMax = 3;
        [SerializeField] private float daySkirmishTriggerTime = 20f;
        [SerializeField] private float daySkirmishHealthMultiplier = 0.75f;
        [SerializeField] private float daySkirmishDamageMultiplier = 0.65f;
        [SerializeField] private float daySkirmishSpeedMultiplier = 0.9f;
        [SerializeField] private float emergencySpawnDelay = 5f;
        [SerializeField] private float waveEnemyGrowthPerWave = 0.16f;
        [SerializeField] private float waveOverlapThresholdRatio = 0.2f;

        public int CurrentWave => currentWave;
        public int EnemiesRemaining => enemiesRemaining;
        public int TotalEnemiesKilled => totalEnemiesKilled;
        public bool IsSpawning => isSpawning;

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnAllWavesCompleted;
        public event Action<GameObject, int> OnEnemySpawned;
        public event Action<int> OnEnemyKilled;
        public event Action<int> OnEnemyCountChanged;

        private NightConfig currentNightConfig;
        private Coroutine nightSequenceCoroutine;
        private DayNightCycle dayNightCycle;
        private GameObject runtimeZombiePrefab;
        private float idleNightTimer;
        private bool daySkirmishTriggered;
        private bool bossSpawned;
        private bool miniBossSpawned;

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
            EnsureRuntimeDefaults();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }

            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
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
            if (Instance == this)
            {
                Instance = null;
            }

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
            int totalNights = GameManager.TotalLevels * GameManager.NightsPerLevel;
            if (nightConfigs == null || nightConfigs.Length < totalNights)
            {
                nightConfigs = new NightConfig[totalNights];
                for (int i = 0; i < totalNights; i++)
                {
                    nightConfigs[i] = NightConfig.CreateForNight(i + 1);
                }
            }

            if (basicZombiePrefab == null)
            {
                basicZombiePrefab = GetOrCreateRuntimeZombiePrefab();
            }

            if (spawnPoints.Count == 0)
            {
                foreach (var spawner in FindObjectsByType<Enemy.EnemySpawner>(FindObjectsSortMode.None))
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
            miniBossSpawned = false;
            OnEnemyCountChanged?.Invoke(enemiesRemaining);

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
            return NightConfig.CreateForNight(night);
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
                int waveOverlapThreshold = Mathf.RoundToInt(waveEnemyCount * Mathf.Clamp01(waveOverlapThresholdRatio));
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
                    float interval = Mathf.Clamp((currentNightConfig?.timeBetweenWaves ?? 2f), 1.2f, 4f);
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
            float spawnInterval = Mathf.Max(0.55f, (currentNightConfig?.spawnInterval ?? 2f) * GetSpawnIntervalMultiplier() * GetAdaptiveSpawnIntervalMultiplier());

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
            float waveScaling = 1f + (waveNumber - 1) * Mathf.Max(0.1f, waveEnemyGrowthPerWave);
            float campaignMultiplier = GetCampaignWaveMultiplier() * GetAdaptiveEnemyCountMultiplier();
            int maxPerWave = GetNightEnemyCap();

            return Mathf.Clamp(Mathf.RoundToInt(baseCount * waveScaling * campaignMultiplier), 1, maxPerWave);
        }

        private int GetNightEnemyCap()
        {
            int night = Mathf.Max(1, GameManager.Instance?.CurrentNight ?? 1);
            int level = GameManager.GetLevelForNight(night);
            int nwl = GameManager.GetNightWithinLevel(night);
            int baseCap = level switch
            {
                1 => 6,
                2 => 12,
                3 => 18,
                _ => 24
            };
            return baseCap + (nwl - 1) * 3;
        }

        private float GetCampaignWaveMultiplier()
        {
            if (GameManager.Instance?.CurrentBalance != null)
            {
                return Mathf.Max(0.2f, GameManager.Instance.CurrentBalance.waveEnemyCountMultiplier);
            }
            return 1f;
        }

        private float GetSpawnIntervalMultiplier()
        {
            if (GameManager.Instance?.CurrentBalance != null)
            {
                return Mathf.Max(0.2f, GameManager.Instance.CurrentBalance.spawnIntervalMultiplier);
            }

            return 1f;
        }

        private float GetAdaptiveEnemyCountMultiplier()
        {
            var player = GameObject.Find("Player");
            var health = player != null ? player.GetComponent<Player.PlayerHealth>() : null;
            if (health == null || health.MaxHealth <= 0f)
            {
                return 1f;
            }

            float healthPct = health.CurrentHealth / health.MaxHealth;
            if (healthPct < 0.25f) return 0.75f;
            if (healthPct < 0.4f) return 0.85f;
            if (healthPct < 0.6f) return 0.95f;
            return 1f;
        }

        private float GetAdaptiveSpawnIntervalMultiplier()
        {
            var player = GameObject.Find("Player");
            var health = player != null ? player.GetComponent<Player.PlayerHealth>() : null;
            if (health == null || health.MaxHealth <= 0f)
            {
                return 1f;
            }

            float healthPct = health.CurrentHealth / health.MaxHealth;
            if (healthPct < 0.25f) return 1.35f;
            if (healthPct < 0.4f) return 1.2f;
            if (healthPct < 0.6f) return 1.1f;
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
            int maxNights = GameManager.Instance?.MaxNights ?? 12;
            int level = GameManager.GetLevelForNight(nightNum);
            bool isLastNight = GameManager.IsLastNightOfLevel(nightNum);
            bool isDaySkirmish = GameManager.Instance?.CurrentState == GameState.DayPhase;
            var spawnType = SelectSpawnType(nightNum, isDaySkirmish);

            bool shouldSpawnMiniBoss = level >= 2 && isLastNight &&
                                       currentWave >= (currentNightConfig?.waveCount ?? 3) &&
                                       !isDaySkirmish &&
                                       !miniBossSpawned;

            bool shouldSpawnBoss = nightNum >= maxNights &&
                                   spawnType == SpawnType.Tank &&
                                   currentWave >= (currentNightConfig?.waveCount ?? 3) &&
                                   !isDaySkirmish &&
                                   !bossSpawned;

            if (shouldSpawnMiniBoss)
            {
                spawnType = SpawnType.Tank;
            }
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
            else if (shouldSpawnMiniBoss)
            {
                enemy = CreateMiniBoss(spawnPosition);
            }
            else
            {
                enemy = CreateEnemyOfType(enemyType, spawnPosition, shouldSpawnBoss);
            }
            enemy.SetActive(true);

            if (shouldSpawnMiniBoss)
            {
                miniBossSpawned = true;
                RadioTransmissions.Instance?.ShowMessage(
                    "RADIO: Prototype 23 detected! An early-stage Lazarus host — kill it before it evolves.", 4f);
            }

            if (shouldSpawnBoss)
            {
                bossSpawned = true;
            }

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

            ApplyPhaseModifiers(enemy, isDaySkirmish);

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
            OnEnemySpawned?.Invoke(enemy, totalEnemiesSpawned);
            OnEnemyCountChanged?.Invoke(enemiesRemaining);
        }

        private enum SpawnType { Basic, Runner, Exploder, Tank, Spitter }

        private SpawnType SelectSpawnType(int night, bool isDaySkirmish)
        {
            float roll = UnityEngine.Random.value;
            int level = GameManager.GetLevelForNight(night);

            if (isDaySkirmish)
            {
                if (level >= 3 && roll < 0.12f)
                    return SpawnType.Runner;
                return SpawnType.Basic;
            }

            int maxNights = GameManager.Instance?.MaxNights ?? 12;

            if (night >= maxNights && currentWave >= (currentNightConfig?.waveCount ?? 3) && !bossSpawned)
            {
                return SpawnType.Tank;
            }

            if (level <= 1)
            {
                if (GameManager.GetNightWithinLevel(night) >= 3 && roll < 0.08f) return SpawnType.Runner;
                return SpawnType.Basic;
            }

            if (level == 2)
            {
                if (roll < 0.28f) return SpawnType.Runner;
                return SpawnType.Basic;
            }

            if (level == 3)
            {
                if (roll < 0.12f) return SpawnType.Spitter;
                if (roll < 0.28f) return SpawnType.Exploder;
                if (roll < 0.50f) return SpawnType.Runner;
                return SpawnType.Basic;
            }

            if (roll < 0.08f) return SpawnType.Tank;
            if (roll < 0.18f) return SpawnType.Spitter;
            if (roll < 0.32f) return SpawnType.Exploder;
            if (roll < 0.52f) return SpawnType.Runner;
            return SpawnType.Basic;
        }

        private Visuals.ProceduralSpriteGenerator.ZombieType SelectEnemyType(int night)
        {
            var spawn = SelectSpawnType(night, false);
            return spawn switch
            {
                SpawnType.Runner => Visuals.ProceduralSpriteGenerator.ZombieType.Runner,
                SpawnType.Exploder => Visuals.ProceduralSpriteGenerator.ZombieType.Exploder,
                SpawnType.Tank => Visuals.ProceduralSpriteGenerator.ZombieType.Tank,
                SpawnType.Spitter => Visuals.ProceduralSpriteGenerator.ZombieType.Basic,
                _ => Visuals.ProceduralSpriteGenerator.ZombieType.Basic
            };
        }

        private GameObject CreateEnemyOfType(Visuals.ProceduralSpriteGenerator.ZombieType type, Vector3 position, bool makeBoss = false)
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
                    health.SetMaxHealth(35f);
                    health.SetPointsOnDeath(15);
                    int currentLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
                    ai.ApplySpeedMultiplier(currentLevel <= 1 ? 1.35f : 1.65f);
                    break;
                case Visuals.ProceduralSpriteGenerator.ZombieType.Tank:
                    health.SetMaxHealth(200f);
                    health.SetPointsOnDeath(50);
                    ai.ApplySpeedMultiplier(0.7f);
                    ai.ApplyDamageMultiplier(2.2f);
                    go.transform.localScale = Vector3.one * 1.5f;
                    break;
                case Visuals.ProceduralSpriteGenerator.ZombieType.Exploder:
                    health.SetMaxHealth(40f);
                    health.SetPointsOnDeath(20);
                    health.SetIsExploder(true);
                    ai.ApplySpeedMultiplier(1.1f);
                    break;
                default:
                    health.SetMaxHealth(50f);
                    health.SetPointsOnDeath(10);
                    break;
            }

            if (type == Visuals.ProceduralSpriteGenerator.ZombieType.Tank && makeBoss)
            {
                health.SetMaxHealth(1000f);
                health.SetPointsOnDeath(200);
                go.AddComponent<Enemy.BossController>();
                go.name = "Subject23_Boss";
            }

            return go;
        }

        private GameObject CreateMiniBoss(Vector3 position)
        {
            var go = new GameObject("Prototype23_MiniBoss");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Visuals.ProceduralSpriteGenerator.CreateZombieSprite(
                Visuals.ProceduralSpriteGenerator.ZombieType.Tank, 0, 0);
            sr.sortingOrder = 9;
            sr.color = new Color(0.6f, 0.2f, 0.25f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            go.AddComponent<CircleCollider2D>().radius = 0.4f;

            var health = go.AddComponent<Enemy.EnemyHealth>();
            health.SetMaxHealth(400f);
            health.SetPointsOnDeath(100);

            var ai = go.AddComponent<Enemy.SimpleEnemyAI>();
            ai.ApplySpeedMultiplier(0.85f);
            ai.ApplyDamageMultiplier(1.8f);

            go.transform.localScale = Vector3.one * 1.3f;

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
            
            float mapBound = 11f;
            if (GameManager.Instance != null)
            {
                var cfg = MapConfig.GetConfigForType(GameManager.Instance.SelectedMap);
                mapBound = Mathf.Min(cfg.perimeterHalfW, cfg.perimeterHalfH) - 1f;
            }
            spawnPos.x = Mathf.Clamp(spawnPos.x, -mapBound, mapBound);
            spawnPos.y = Mathf.Clamp(spawnPos.y, -mapBound, mapBound);
            
            return spawnPos;
        }

        private void ApplyPhaseModifiers(GameObject enemy, bool isDaySkirmish)
        {
            if (currentNightConfig == null)
            {
                return;
            }

            float healthMultiplier = currentNightConfig.healthMultiplier;
            float damageMultiplier = currentNightConfig.damageMultiplier;
            float speedMultiplier = currentNightConfig.speedMultiplier;
            float objectiveBuff = 1f;

            if (GameManager.Instance?.CurrentBalance != null)
            {
                var settings = GameManager.Instance.CurrentBalance;
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

            if (CraftingSystem.Instance != null)
            {
                healthMultiplier *= CraftingSystem.Instance.GetNightEnemyHealthMultiplier();
                speedMultiplier *= CraftingSystem.Instance.GetNightEnemySpeedMultiplier();
                damageMultiplier *= CraftingSystem.Instance.GetNightEnemyDamageMultiplier();
            }

            if (!isDaySkirmish && GameManager.Instance != null)
            {
                float penalty = Mathf.Max(1f, GameManager.Instance.CurrentNightEnemyPenaltyMultiplier);
                healthMultiplier *= penalty;
                damageMultiplier *= penalty;
                speedMultiplier *= Mathf.Lerp(1f, penalty, 0.5f);
            }

            if (isDaySkirmish)
            {
                healthMultiplier = Mathf.Lerp(1f, healthMultiplier, daySkirmishHealthMultiplier);
                damageMultiplier = Mathf.Lerp(1f, damageMultiplier, daySkirmishDamageMultiplier);
                speedMultiplier = Mathf.Lerp(1f, speedMultiplier, daySkirmishSpeedMultiplier);
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

            int burstCount = Mathf.Clamp(1 + (GameManager.Instance.CurrentNight / 3), 1, 2);
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
            OnEnemyCountChanged?.Invoke(enemiesRemaining);

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

        public void ClearSpawnPoints()
        {
            spawnPoints.Clear();
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
