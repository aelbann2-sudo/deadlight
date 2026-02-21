using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Deadlight.Core
{
    public class WaveSpawner : MonoBehaviour
    {
        public static WaveSpawner Instance { get; private set; }

        [Header("Spawning")]
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private int baseEnemiesPerWave = 5;
        [SerializeField] private float enemiesPerWaveScaling = 1.5f;
        [SerializeField] private float minSpawnDistance = 12f;
        [SerializeField] private float maxSpawnDistance = 20f;
        
        [Header("Difficulty")]
        [SerializeField] private float speedIncreasePerWave = 0.2f;
        [SerializeField] private float healthIncreasePerWave = 10f;

        [Header("State")]
        [SerializeField] private int currentWave = 0;
        [SerializeField] private int enemiesAlive = 0;
        [SerializeField] private int enemiesToSpawn = 0;
        [SerializeField] private bool isSpawning = false;

        private Transform playerTransform;
        private System.Action<int> onWaveChanged;
        private System.Action<int> onEnemyCountChanged;

        public int CurrentWave => currentWave;
        public int EnemiesAlive => enemiesAlive;
        public bool IsSpawning => isSpawning;

        public event System.Action<int> OnWaveChanged;
        public event System.Action<int> OnEnemyCountChanged;
        public event System.Action OnAllWavesCleared;

        private Sprite[] npcSprites;

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
            npcSprites = Resources.LoadAll<Sprite>("NPC");
            FindPlayer();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.NightPhase)
            {
                StartWaves();
            }
            else if (state == GameState.DayPhase || state == GameState.GameOver)
            {
                StopAllCoroutines();
                isSpawning = false;
            }
        }

        private void FindPlayer()
        {
            var player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        public void StartWaves()
        {
            currentWave = 0;
            StartCoroutine(WaveRoutine());
        }

        private IEnumerator WaveRoutine()
        {
            yield return new WaitForSeconds(2f);

            int totalWaves = 3 + (GameManager.Instance != null ? GameManager.Instance.CurrentNight : 1);

            for (int w = 0; w < totalWaves; w++)
            {
                currentWave = w + 1;
                OnWaveChanged?.Invoke(currentWave);

                int enemyCount = Mathf.RoundToInt(baseEnemiesPerWave + (currentWave - 1) * enemiesPerWaveScaling);
                enemiesToSpawn = enemyCount;
                isSpawning = true;

                for (int i = 0; i < enemyCount; i++)
                {
                    SpawnEnemy();
                    yield return new WaitForSeconds(spawnInterval / Mathf.Max(1, currentWave * 0.5f));
                }

                isSpawning = false;

                while (enemiesAlive > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                yield return new WaitForSeconds(2f);
            }

            OnAllWavesCleared?.Invoke();
            GameManager.Instance?.OnNightSurvived();
        }

        private void SpawnEnemy()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnPos = playerTransform.position + new Vector3(
                Mathf.Cos(angle) * dist,
                Mathf.Sin(angle) * dist,
                0
            );

            var enemyObj = new GameObject($"Zombie_W{currentWave}");
            enemyObj.transform.position = spawnPos;

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            string[] npcNames = { "TopDown_NPC_0", "TopDown_NPC_1", "TopDown_NPC_2", "TopDown_NPC_3" };
            Sprite npcSprite = GetSprite(npcNames[Random.Range(0, npcNames.Length)]);
            if (npcSprite != null) sr.sprite = npcSprite;
            else sr.sprite = CreateFallbackSprite();
            sr.sortingOrder = 9;
            sr.color = new Color(0.6f + currentWave * 0.05f, 0.7f - currentWave * 0.05f, 0.5f);

            var rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = enemyObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            var health = enemyObj.AddComponent<Enemy.EnemyHealth>();
            health.SetMaxHealth(50f + currentWave * healthIncreasePerWave);
            health.OnEnemyDeath += () => OnEnemyKilled(enemyObj);

            var ai = enemyObj.AddComponent<Enemy.SimpleEnemyAI>();
            ai.SetAggressive(true);

            var hpBar = enemyObj.AddComponent<EnemyHealthBar>();

            enemiesAlive++;
            OnEnemyCountChanged?.Invoke(enemiesAlive);
        }

        private void OnEnemyKilled(GameObject enemy)
        {
            enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
            OnEnemyCountChanged?.Invoke(enemiesAlive);

            if (GameEffects.Instance != null)
            {
                GameEffects.Instance.SpawnDeathEffect(enemy.transform.position, new Color(0.5f, 0.2f, 0.2f));
            }
        }

        private Sprite GetSprite(string name)
        {
            if (npcSprites == null) return null;
            foreach (var s in npcSprites)
            {
                if (s.name == name) return s;
            }
            return null;
        }

        private Sprite CreateFallbackSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) < size / 2f - 1
                        ? new Color(0.4f, 0.5f, 0.3f) : Color.clear;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    public class EnemyHealthBar : MonoBehaviour
    {
        private Enemy.EnemyHealth health;
        private GameObject barBg;
        private GameObject barFill;
        private SpriteRenderer bgRenderer;
        private SpriteRenderer fillRenderer;

        private void Start()
        {
            health = GetComponent<Enemy.EnemyHealth>();
            if (health == null) return;

            barBg = new GameObject("HealthBarBG");
            barBg.transform.SetParent(transform);
            barBg.transform.localPosition = new Vector3(0, 0.7f, 0);
            barBg.transform.localScale = new Vector3(0.8f, 0.1f, 1f);
            bgRenderer = barBg.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = CreatePixelSprite();
            bgRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgRenderer.sortingOrder = 20;

            barFill = new GameObject("HealthBarFill");
            barFill.transform.SetParent(barBg.transform);
            barFill.transform.localPosition = Vector3.zero;
            barFill.transform.localScale = Vector3.one;
            fillRenderer = barFill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = CreatePixelSprite();
            fillRenderer.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            fillRenderer.sortingOrder = 21;
        }

        private void Update()
        {
            if (health == null) return;
            
            if (health.HealthPercentage >= 1f)
            {
                barBg.SetActive(false);
                return;
            }

            barBg.SetActive(true);
            float pct = health.HealthPercentage;
            barFill.transform.localScale = new Vector3(pct, 1f, 1f);
            barFill.transform.localPosition = new Vector3((pct - 1f) * 0.5f, 0, 0);

            fillRenderer.color = Color.Lerp(
                new Color(0.8f, 0.2f, 0.2f),
                new Color(0.2f, 0.8f, 0.2f),
                pct
            );
        }

        private Sprite CreatePixelSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }
    }
}
