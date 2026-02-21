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
        [SerializeField] private float[] dayDurationsByNight = { 70f, 60f, 55f, 50f, 45f };
        [SerializeField] private float nightDuration = 120f;
        [SerializeField] private int healthPickupCount = 3;
        [SerializeField] private int ammoPickupCount = 4;
        [SerializeField] private float pickupSpawnRadius = 8f;
        [SerializeField] private float pickupSpawnMinDistanceFromPlayer = 3f;

        private DayNightCycle dayNightCycle;
        private readonly List<GameObject> spawnedPickups = new List<GameObject>();
        private readonly List<GameObject> spawnedCrates = new List<GameObject>();
        private readonly List<GameObject> spawnedObjectiveObjects = new List<GameObject>();
        private bool nightWarningShown;

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
                ApplyNightPacing(1);
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

                if (!nightWarningShown && timeRemaining <= 15f && timeRemaining > 0f)
                {
                    nightWarningShown = true;
                    if (RadioTransmissions.Instance != null)
                        RadioTransmissions.Instance.ShowMessage("Night is approaching! Find shelter!", 3f);
                    if (GameEffects.Instance != null)
                        GameEffects.Instance.ScreenShake(0.08f, 0.3f);
                }
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.DayPhase:
                    nightWarningShown = false;
                    SpawnPickups();
                    SpawnSupplyCrates();
                    SpawnObjectiveInteractables();
                    OnStatusMessage?.Invoke($"Day Phase - Night {GameManager.Instance?.CurrentNight ?? 1}");
                    break;
                case GameState.NightPhase:
                    CleanupDayObjects();
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
            ApplyNightPacing(night);

            if (DayObjectiveSystem.Instance != null)
            {
                DayObjectiveSystem.Instance.GenerateObjective(night, Time.frameCount + night * 991);
            }

            if (RunModifierSystem.Instance != null)
            {
                RunModifierSystem.Instance.RollNightEvent(night, Time.frameCount + night * 67);
            }
        }

        private void HandleAllWavesCleared()
        {
            // WaveSpawner already calls GameManager.OnNightSurvived() - state will transition to DawnPhase or Victory
            // This listener allows GameFlowController to react; OnDawnPhaseStarted is raised via HandleGameStateChanged
        }

        private void SpawnSupplyCrates()
        {
            var player = GameObject.Find("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
            int crateCount = UnityEngine.Random.Range(5, 9);

            for (int i = 0; i < crateCount; i++)
            {
                Vector3 pos = GetRandomPickupSpawnPosition(playerPos);
                pos += new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), 0);

                var crateObj = new GameObject($"SupplyCrate_{i}");
                crateObj.transform.position = pos;
                crateObj.AddComponent<Systems.SupplyCrate>();
                spawnedCrates.Add(crateObj);
            }
        }

        private void SpawnObjectiveInteractables()
        {
            if (DayObjectiveSystem.Instance == null) return;
            var obj = DayObjectiveSystem.Instance.ActiveObjective;
            if (obj == null) return;

            var player = GameObject.Find("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            switch (obj.type)
            {
                case ObjectiveType.SecureZone:
                    SpawnSecureZones(obj.targetCount, playerPos);
                    break;
                case ObjectiveType.ActivateBeacon:
                    SpawnBeacons(obj.targetCount, playerPos);
                    break;
                case ObjectiveType.RecoverSupplyCache:
                    SpawnLargeCache(playerPos);
                    break;
            }
        }

        private void SpawnSecureZones(int count, Vector3 playerPos)
        {
            for (int i = 0; i < Mathf.Min(count, 4); i++)
            {
                Vector3 pos = GetRandomPickupSpawnPosition(playerPos);
                pos += new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-3f, 3f), 0);

                var zoneObj = new GameObject($"SecureZone_{i}");
                zoneObj.transform.position = pos;
                zoneObj.AddComponent<ObjectiveZone>();
                spawnedObjectiveObjects.Add(zoneObj);
            }
        }

        private void SpawnBeacons(int count, Vector3 playerPos)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPickupSpawnPosition(playerPos);
                pos += new Vector3(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(-4f, 4f), 0);

                var beaconObj = new GameObject($"Beacon_{i}");
                beaconObj.transform.position = pos;
                beaconObj.AddComponent<ObjectiveBeacon>();
                spawnedObjectiveObjects.Add(beaconObj);
            }
        }

        private void SpawnLargeCache(Vector3 playerPos)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(8f, 14f);
            Vector3 pos = playerPos + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0);

            var cacheObj = new GameObject("SupplyCache");
            cacheObj.transform.position = pos;
            cacheObj.AddComponent<ObjectiveCache>();
            spawnedObjectiveObjects.Add(cacheObj);
        }

        private void CleanupDayObjects()
        {
            foreach (var go in spawnedCrates)
                if (go != null) Destroy(go);
            spawnedCrates.Clear();

            foreach (var go in spawnedObjectiveObjects)
                if (go != null) Destroy(go);
            spawnedObjectiveObjects.Clear();

            ClearSpawnedPickups();
        }

        private void ApplyNightPacing(int night)
        {
            if (dayNightCycle == null)
            {
                dayNightCycle = FindObjectOfType<DayNightCycle>();
            }

            if (dayNightCycle == null)
            {
                return;
            }

            int idx = Mathf.Clamp(night - 1, 0, dayDurationsByNight.Length - 1);
            dayNightCycle.SetDayDuration(dayDurationsByNight[idx]);
            dayNightCycle.SetNightDuration(nightDuration);
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

    public class ObjectiveZone : MonoBehaviour
    {
        private float radius = 2f;
        private float tickTimer;
        private float tickInterval = 1f;
        private bool completed;
        private SpriteRenderer zoneRing;

        private void Awake()
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            zoneRing = gameObject.AddComponent<SpriteRenderer>();
            zoneRing.sprite = CreateRingSprite(new Color(0.2f, 0.7f, 1f, 0.35f), 48);
            zoneRing.sortingOrder = 2;
            transform.localScale = Vector3.one * (radius * 2f);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (completed) return;
            if (other.GetComponent<PlayerHealth>() == null) return;

            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                if (DayObjectiveSystem.Instance != null)
                {
                    DayObjectiveSystem.Instance.AddProgress(1);
                    if (DayObjectiveSystem.Instance.ActiveObjective != null && DayObjectiveSystem.Instance.ActiveObjective.IsComplete)
                    {
                        completed = true;
                        zoneRing.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<PlayerHealth>() != null)
                tickTimer = 0f;
        }

        private static Sprite CreateRingSprite(Color color, int size)
        {
            var tex = new Texture2D(size, size);
            var px = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerR = size / 2f - 1;
            float innerR = outerR - 3;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    px[y * size + x] = (d <= outerR && d >= innerR) ? color : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    public class ObjectiveBeacon : MonoBehaviour
    {
        private bool activated;
        private SpriteRenderer sr;
        private float interactRange = 1.5f;

        private void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePillarSprite(new Color(0.3f, 0.5f, 1f));
            sr.sortingOrder = 5;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = interactRange;
        }

        private void Update()
        {
            if (activated) return;
            var player = GameObject.Find("Player");
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= interactRange && Input.GetKeyDown(KeyCode.F))
            {
                activated = true;
                sr.color = new Color(0.3f, 1f, 0.3f);
                if (DayObjectiveSystem.Instance != null)
                    DayObjectiveSystem.Instance.AddProgress(1);

                try
                {
                    var clip = Audio.ProceduralAudioGenerator.GeneratePickup();
                    if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, 0.5f);
                }
                catch { }
            }
        }

        private static Sprite CreatePillarSprite(Color color)
        {
            int w = 12, h = 24;
            var tex = new Texture2D(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool body = x >= 2 && x < w - 2;
                    float glow = 1f - ((float)y / h) * 0.3f;
                    px[y * w + x] = body ? new Color(color.r * glow, color.g * glow, color.b * glow) : Color.clear;
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16);
        }
    }

    public class ObjectiveCache : MonoBehaviour
    {
        private bool looted;
        private float interactRange = 2f;
        private SpriteRenderer sr;

        private void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.9f, 0.75f, 0.2f);
            sr.sortingOrder = 5;

            int s = 28;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    bool border = x < 3 || x >= s - 3 || y < 3 || y >= s - 3;
                    px[y * s + x] = border ? new Color(0.7f, 0.55f, 0.1f) : new Color(0.9f, 0.75f, 0.2f);
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = interactRange;
        }

        private void Update()
        {
            if (looted) return;
            var player = GameObject.Find("Player");
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= interactRange && Input.GetKeyDown(KeyCode.F))
            {
                looted = true;
                sr.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                if (DayObjectiveSystem.Instance != null)
                    DayObjectiveSystem.Instance.MarkCompleted();

                try
                {
                    var clip = Audio.ProceduralAudioGenerator.GeneratePickup();
                    if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, 0.7f);
                }
                catch { }

                Destroy(gameObject, 1f);
            }
        }
    }
}
