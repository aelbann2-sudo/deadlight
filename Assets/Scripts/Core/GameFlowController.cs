using UnityEngine;
using System;
using System.Collections.Generic;
using Deadlight.Player;
using Deadlight.Systems;
using Deadlight.Level;

namespace Deadlight.Core
{
    /// <summary>
    /// Manages the full game flow: Main Menu -> Day Phase -> Night Phase -> Dawn Phase -> repeat for 5 nights -> Victory or Game Over.
    /// Works with GameManager, WaveSpawner, and DayNightCycle.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController Instance { get; private set; }

        [Header("Phase Settings")]
        [SerializeField] private float dayPhaseDuration = 45f;
        [SerializeField] private int healthPickupCount = 3;
        [SerializeField] private int ammoPickupCount = 4;
        [SerializeField] private float pickupSpawnRadius = 8f;
        [SerializeField] private float pickupSpawnMinDistanceFromPlayer = 3f;

        private DayNightCycle dayNightCycle;
        private readonly List<GameObject> spawnedPickups = new List<GameObject>();

        public event Action<float> OnDayTimerUpdate;
        public event Action<int> OnNightStarted;
        public event Action OnDawnPhaseStarted;
        public event Action OnDawnPhaseEnded;
        public event Action<string> OnStatusMessage;

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
            dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnTimeUpdate += HandleDayNightTimeUpdate;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnNightChanged += HandleNightChanged;
            }

            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnAllWavesCleared += HandleAllWavesCleared;
            }
        }

        private void OnDestroy()
        {
            if (dayNightCycle != null)
            {
                dayNightCycle.OnTimeUpdate -= HandleDayNightTimeUpdate;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnNightChanged -= HandleNightChanged;
            }

            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnAllWavesCleared -= HandleAllWavesCleared;
            }
        }

        /// <summary>
        /// Call to start a new game. Resets everything and begins Night 1 Day Phase.
        /// </summary>
        public void StartGame(Difficulty difficulty)
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.SetDifficulty(difficulty);

            // Use an absolute duration so repeated restarts do not compound scaling.
            if (dayNightCycle != null)
            {
                dayNightCycle.SetDayDuration(dayPhaseDuration);
            }

            ClearSpawnedPickups();
            GameManager.Instance.StartNewGame();
            OnStatusMessage?.Invoke($"Day Phase - Night {GameManager.Instance.CurrentNight}");
        }

        /// <summary>
        /// Call when the player clicks Continue in the Dawn/Shop phase. Awards points and advances to next night.
        /// </summary>
        public void RequestDawnContinue()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.DawnPhase)
                return;

            // Award points for surviving the night
            if (PointsSystem.Instance != null)
            {
                int points = 100 + (GameManager.Instance.CurrentNight * 50);
                PointsSystem.Instance.AddPoints(points, "Night Survived");
            }

            OnDawnPhaseEnded?.Invoke();
            GameManager.Instance.AdvanceToNextNight();
            OnStatusMessage?.Invoke($"Day Phase - Night {GameManager.Instance.CurrentNight}");
        }

        /// <summary>
        /// Spawns health and ammo pickups at random positions around the map.
        /// </summary>
        public void SpawnPickups()
        {
            var player = GameObject.Find("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            for (int i = 0; i < healthPickupCount; i++)
            {
                SpawnPickupAtRandomPosition(PickupItem.PickupKind.Health, 25, Color.red, playerPos);
            }

            for (int i = 0; i < ammoPickupCount; i++)
            {
                SpawnPickupAtRandomPosition(PickupItem.PickupKind.Ammo, 30, new Color(1f, 0.9f, 0.2f), playerPos);
            }
        }

        private void SpawnPickupAtRandomPosition(PickupItem.PickupKind kind, int amount, Color color, Vector3 playerPos)
        {
            Vector3 spawnPos = GetRandomPickupSpawnPosition(playerPos);

            var go = new GameObject($"Pickup_{kind}_{Time.frameCount}");
            go.transform.position = spawnPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(color);
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            var pickupItem = go.AddComponent<PickupItem>();
            pickupItem.Initialize(kind, amount);

            spawnedPickups.Add(go);
        }

        private Vector3 GetRandomPickupSpawnPosition(Vector3 playerPos)
        {
            if (LevelManager.Instance != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    var pos = LevelManager.Instance.GetRandomPositionInLevel();
                    if (Vector3.Distance(pos, playerPos) >= pickupSpawnMinDistanceFromPlayer)
                        return pos;
                }
            }

            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(pickupSpawnMinDistanceFromPlayer, pickupSpawnRadius);
            return playerPos + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0);
        }

        private static Sprite CreateCircleSprite(Color color)
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist <= radius ? color : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void ClearSpawnedPickups()
        {
            foreach (var go in spawnedPickups)
            {
                if (go != null) Destroy(go);
            }
            spawnedPickups.Clear();
        }

        private void HandleDayNightTimeUpdate(float timeRemaining)
        {
            if (GameManager.Instance?.CurrentState == GameState.DayPhase)
            {
                OnDayTimerUpdate?.Invoke(timeRemaining);
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.DayPhase:
                    SpawnPickups();
                    OnStatusMessage?.Invoke($"Day Phase - Night {GameManager.Instance?.CurrentNight ?? 1} ({dayPhaseDuration}s)");
                    break;
                case GameState.NightPhase:
                    OnNightStarted?.Invoke(GameManager.Instance?.CurrentNight ?? 1);
                    OnStatusMessage?.Invoke($"Night {GameManager.Instance?.CurrentNight ?? 1} - Survive the waves!");
                    break;
                case GameState.DawnPhase:
                    OnDawnPhaseStarted?.Invoke();
                    OnStatusMessage?.Invoke("Dawn - Visit the shop and prepare for the next night.");
                    break;
                case GameState.Victory:
                    OnStatusMessage?.Invoke("Victory! You survived all 5 nights!");
                    break;
                case GameState.GameOver:
                    OnStatusMessage?.Invoke("Game Over");
                    break;
            }
        }

        private void HandleNightChanged(int night)
        {
            // Can be used for UI updates
        }

        private void HandleAllWavesCleared()
        {
            // WaveSpawner already calls GameManager.OnNightSurvived() - state will transition to DawnPhase or Victory
            // This listener allows GameFlowController to react; OnDawnPhaseStarted is raised via HandleGameStateChanged
        }

    }

    /// <summary>
    /// Attached to programmatically spawned pickups. Handles trigger detection and applies health/ammo effects.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PickupItem : MonoBehaviour
    {
        public enum PickupKind { Health, Ammo }

        [SerializeField] private PickupKind kind = PickupKind.Health;
        [SerializeField] private int amount = 25;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.1f;

        private Vector3 startPosition;
        private float bobOffset;

        public void Initialize(PickupKind pickupKind, int pickupAmount)
        {
            kind = pickupKind;
            amount = pickupAmount;
        }

        private void Start()
        {
            startPosition = transform.position;
            bobOffset = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponent<PlayerHealth>();
            var shooting = other.GetComponent<PlayerShooting>();

            if (health == null && shooting == null) return;

            bool consumed = false;

            if (kind == PickupKind.Health && health != null && health.IsAlive)
            {
                health.Heal(amount);
                consumed = true;
            }
            else if (kind == PickupKind.Ammo && shooting != null)
            {
                shooting.AddAmmo(amount);
                consumed = true;
            }

            if (consumed)
            {
                Destroy(gameObject);
            }
        }
    }
}
