using UnityEngine;
using System;
using System.Collections.Generic;
using Deadlight.Player;
using Deadlight.Systems;
using Deadlight.Level;
using Deadlight.Data;

namespace Deadlight.Core
{
    public enum DayContestedDropState
    {
        Inactive,
        Broadcast,
        Descent,
        Secure,
        Resolved,
        Expired
    }

    /// <summary>
    /// Manages the full game flow: Main Menu -> Day Phase -> Night Phase -> Dawn Phase -> repeat by campaign scope -> Victory or Game Over.
    /// Works with GameManager, WaveSpawner, and DayNightCycle.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController Instance { get; private set; }

        [Header("Phase Settings")]
        [SerializeField] private float[] dayDurationsByNight = {
            90f, 80f, 70f,
            85f, 75f, 65f,
            80f, 70f, 60f,
            75f, 65f, 55f
        };
        [SerializeField] private float[] nightDurationsByNight = {
            55f, 65f, 80f,
            55f, 70f, 85f,
            65f, 80f, 100f,
            80f, 100f, 140f
        };
        [SerializeField] private int[] healthPickupsByNight = { 4, 4, 3, 3 };
        [SerializeField] private int[] ammoPickupsByNight = { 5, 5, 4, 4 };
        [SerializeField] private int[] scrapPickupsByNight = { 3, 2, 2, 1 };
        [SerializeField] private int[] woodPickupsByNight = { 2, 1, 1, 1 };
        [SerializeField] private int[] chemicalsPickupsByNight = { 1, 1, 1, 0 };
        [SerializeField] private int[] electronicsPickupsByNight = { 0, 1, 1, 1 };
        [SerializeField] private float pickupSpawnMinDistanceFromPlayer = 10f;
        [SerializeField] private float pickupMinSpacing = 6f;

        private DayNightCycle dayNightCycle;
        private readonly List<GameObject> spawnedPickups = new List<GameObject>();
        private readonly List<GameObject> spawnedCrates = new List<GameObject>();
        private readonly List<GameObject> spawnedObjectiveObjects = new List<GameObject>();
        private bool nightWarningShown;

        [SerializeField] private float helicopterCooldown = 45f;
        [SerializeField] private int maxDropsPerPhase = 1;
        [SerializeField] private float helicopterFirstDropDelay = 35f;
        [SerializeField] private float helicopterDropJitter = 8f;
        private int dropsThisPhase;
        private float nextHelicopterDropTime = float.PositiveInfinity;

        [Header("Day Contested Drop")]
        [SerializeField] private bool enableDayContestedDrop = true;
        [SerializeField] private float contestedDropDayProgress = 0.45f;
        [SerializeField] private float contestedBroadcastDuration = 8f;
        [SerializeField] private float contestedSecureHoldTime = 4f;
        [SerializeField] private float contestedExpiryTime = 20f;
        [SerializeField] private float contestedDropRadius = 7f;
        private DayContestedDropState dayContestedDropState = DayContestedDropState.Inactive;
        private bool dayContestedDropSpawned;
        private float dayContestedDropTriggerTime = float.PositiveInfinity;
        private float dayContestedDropStateUntil = float.PositiveInfinity;
        private Systems.SupplyCrate activeContestedDropCrate;

        public event Action<float> OnDayTimerUpdate;
        public event Action<int> OnNightStarted;
        public event Action OnDawnPhaseStarted;
        public event Action OnDawnPhaseEnded;
        public event Action<string> OnStatusMessage;
        public event Action<DayContestedDropState, float> OnContestedDropStateChanged;

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
            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
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

        private void Update()
        {
            TryRunDayContestedDrop();
            TrySpawnHelicopterDrop();
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
        /// Call to start a new game. Resets everything and begins Level 1 Day Phase.
        /// </summary>
        public void StartGame()
        {
            if (GameManager.Instance == null) return;

            // Use an absolute duration so repeated restarts do not compound scaling.
            if (dayNightCycle != null)
            {
                ApplyNightPacing(1);
            }

            ClearSpawnedPickups();
            GameManager.Instance.StartNewGame();
            OnStatusMessage?.Invoke($"Day Phase - Level {GameManager.Instance.CurrentLevel}, Night {GameManager.Instance.NightWithinLevel}");
        }

        /// <summary>
        /// Call when the player clicks Continue in the Dawn/Shop phase. Awards points and advances to next level.
        /// </summary>
        public void RequestDawnContinue()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.DawnPhase)
                return;

            OnDawnPhaseEnded?.Invoke();
            GameManager.Instance.AdvanceToNextNight();
            int dl = GameManager.Instance.CurrentLevel;
            int dn = GameManager.Instance.NightWithinLevel;
            OnStatusMessage?.Invoke($"Day Phase - Level {dl}, Night {dn}");
        }

        /// <summary>
        /// Spawns health and ammo pickups at random positions around the map.
        /// </summary>
        public void SpawnPickups()
        {
            if (PickupSpawner.Instance == null) return;

            var player = GameObject.Find("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            int nightIdx = Mathf.Clamp((GameManager.Instance?.CurrentLevel ?? 1) - 1, 0, 3);

            var usedPositions = new List<Vector3>();

            SpawnTypeScattered(PickupType.Health, GetPickupCount(healthPickupsByNight, nightIdx), playerPos, usedPositions);
            SpawnTypeScattered(PickupType.Ammo, GetPickupCount(ammoPickupsByNight, nightIdx), playerPos, usedPositions);
        }

        private int GetPickupCount(int[] perNight, int nightIdx)
        {
            if (perNight == null || perNight.Length == 0) return 0;
            return perNight[Mathf.Clamp(nightIdx, 0, perNight.Length - 1)];
        }

        private void SpawnTypeScattered(PickupType type, int count, Vector3 playerPos, List<Vector3> usedPositions)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetScatteredPosition(playerPos, usedPositions);
                PickupSpawner.Instance.SpawnPickup(pos, type);
                usedPositions.Add(pos);

                // Track for cleanup — find the most recently created pickup of this type
                var all = GameObject.FindObjectsByType<Deadlight.Systems.PickupItem>(FindObjectsSortMode.None);
                if (all.Length > 0)
                {
                    var go = all[all.Length - 1].gameObject;
                    if (!spawnedPickups.Contains(go))
                        spawnedPickups.Add(go);
                }
            }
        }

        private Vector3 GetScatteredPosition(Vector3 playerPos, List<Vector3> usedPositions)
        {
            // Get actual map perimeter bounds (not LevelManager default 50x50)
            float halfW = 30f;
            float halfH = 30f;
            Vector3 mapCenter = Vector3.zero;

            if (LevelManager.Instance != null)
            {
                halfW = LevelManager.Instance.LevelBounds.x / 2f;
                halfH = LevelManager.Instance.LevelBounds.y / 2f;
                mapCenter = LevelManager.Instance.transform.position;
            }

            // Inset from walls so pickups aren't flush against perimeter
            float margin = 2.5f;
            float usableHalfW = halfW - margin;
            float usableHalfH = halfH - margin;

            // Collect obstacle colliders once for overlap checks
            var obstacles = LevelManager.Instance != null
                ? LevelManager.Instance.Obstacles
                : null;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                // Bias distribution toward outer regions of the map.
                // Pick a random angle and a distance biased outward (sqrt for uniform area,
                // then bias further out so pickups feel scattered, not clustered at center).
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float t = Mathf.Sqrt(UnityEngine.Random.value); // uniform area distribution
                t = Mathf.Lerp(t, 1f, 0.4f); // push 40% further toward edges

                float x = mapCenter.x + Mathf.Cos(angle) * t * usableHalfW;
                float y = mapCenter.y + Mathf.Sin(angle) * t * usableHalfH;
                Vector3 candidate = new Vector3(x, y, 0);

                // Reject if too close to player spawn
                if (Vector3.Distance(candidate, playerPos) < pickupSpawnMinDistanceFromPlayer)
                    continue;

                // Reject if too close to map center (keep the center area clear)
                if (Vector3.Distance(candidate, mapCenter) < Mathf.Min(usableHalfW, usableHalfH) * 0.15f)
                    continue;

                // Reject if too close to any existing pickup
                bool tooClose = false;
                for (int j = 0; j < usedPositions.Count; j++)
                {
                    if (Vector3.Distance(candidate, usedPositions[j]) < pickupMinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Reject if overlapping a building or obstacle collider
                if (obstacles != null)
                {
                    bool blocked = false;
                    foreach (var obs in obstacles)
                    {
                        if (obs == null) continue;
                        var col = obs.GetComponent<Collider2D>();
                        if (col != null && col.OverlapPoint(candidate))
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (blocked) continue;
                }

                // Also check against buildings (they have BoxCollider2D)
                var hit = Physics2D.OverlapCircle(candidate, 0.6f);
                if (hit != null && !hit.isTrigger)
                    continue;

                return candidate;
            }

            // Fallback after 100 attempts: pick a position in a corner quadrant
            int quadrant = UnityEngine.Random.Range(0, 4);
            float fx = (quadrant % 2 == 0 ? 1f : -1f) * UnityEngine.Random.Range(usableHalfW * 0.5f, usableHalfW);
            float fy = (quadrant < 2 ? 1f : -1f) * UnityEngine.Random.Range(usableHalfH * 0.5f, usableHalfH);
            return mapCenter + new Vector3(fx, fy, 0);
        }

        private Vector3 GetRandomPickupSpawnPosition(Vector3 playerPos)
        {
            return GetScatteredPosition(playerPos, new List<Vector3>());
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
                    nextHelicopterDropTime = float.PositiveInfinity;
                    ResetDayContestedDropState();
                    SpawnPickups();
                    SpawnSupplyCrates();
                    ScheduleDayContestedDrop();
                    int lvl = GameManager.Instance?.CurrentLevel ?? 1;
                    int nwl = GameManager.Instance?.NightWithinLevel ?? 1;
                    OnStatusMessage?.Invoke($"Day Phase - Level {lvl}, Night {nwl}");
                    break;
                case GameState.NightPhase:
                    if (IsCraftingEnabled() && CraftingSystem.Instance != null)
                    {
                        CraftingSystem.Instance.FinalizeDayPrep();
                    }

                    dayContestedDropState = DayContestedDropState.Inactive;
                    dayContestedDropStateUntil = float.PositiveInfinity;
                    CleanupDayObjects();
                    dropsThisPhase = 0;
                    ScheduleNextHelicopterDrop(Time.time + helicopterFirstDropDelay);
                    OnNightStarted?.Invoke(GameManager.Instance?.CurrentNight ?? 1);
                    int nl = GameManager.Instance?.CurrentLevel ?? 1;
                    int nn = GameManager.Instance?.NightWithinLevel ?? 1;
                    OnStatusMessage?.Invoke($"Level {nl}, Night {nn} - Survive the waves!");
                    break;
                case GameState.DawnPhase:
                    nextHelicopterDropTime = float.PositiveInfinity;
                    dayContestedDropState = DayContestedDropState.Inactive;
                    OnDawnPhaseStarted?.Invoke();
                    if (GameManager.Instance != null && GameManager.Instance.WillRetryCurrentStepOnAdvance)
                    {
                        OnStatusMessage?.Invoke("Dawn - Objective missed. Next deploy retries this step.");
                    }
                    else if (GameManager.Instance != null && GameManager.Instance.PendingObjectiveCarryoverPenaltyStacks > 0)
                    {
                        OnStatusMessage?.Invoke("Dawn - Penalty queued: stronger next-night enemies and reduced level carryover.");
                    }
                    else
                    {
                        OnStatusMessage?.Invoke("Dawn - Visit the shop and prepare for the next level.");
                    }
                    break;
                case GameState.LevelComplete:
                    nextHelicopterDropTime = float.PositiveInfinity;
                    dayContestedDropState = DayContestedDropState.Inactive;
                    CleanupDayObjects();
                    break;
                case GameState.Victory:
                    nextHelicopterDropTime = float.PositiveInfinity;
                    dayContestedDropState = DayContestedDropState.Inactive;
                    int clearedLevels = GameManager.Instance != null ? GameManager.Instance.PlayableLevelCap : 2;
                    OnStatusMessage?.Invoke($"Victory! Subject 23 contained. All {clearedLevels} playable levels cleared.");
                    break;
                case GameState.GameOver:
                    nextHelicopterDropTime = float.PositiveInfinity;
                    dayContestedDropState = DayContestedDropState.Inactive;
                    OnStatusMessage?.Invoke("Game Over");
                    break;
            }
        }

        private void HandleNightChanged(int night)
        {
            ApplyNightPacing(night);

            if (GameManager.Instance?.CurrentState == GameState.DayPhase && !dayContestedDropSpawned)
            {
                ScheduleDayContestedDrop();
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

        private void ResetDayContestedDropState()
        {
            dayContestedDropState = DayContestedDropState.Inactive;
            dayContestedDropSpawned = false;
            dayContestedDropTriggerTime = float.PositiveInfinity;
            dayContestedDropStateUntil = float.PositiveInfinity;
            activeContestedDropCrate = null;
            OnContestedDropStateChanged?.Invoke(dayContestedDropState, 0f);
        }

        private void ScheduleDayContestedDrop()
        {
            if (!enableDayContestedDrop || GameManager.Instance?.CurrentState != GameState.DayPhase)
            {
                return;
            }

            int night = GameManager.Instance?.CurrentNight ?? 1;
            if (night < 2) return;

            float dayDuration = dayNightCycle != null ? dayNightCycle.DayDuration : 60f;
            float progress = Mathf.Clamp(contestedDropDayProgress, 0.2f, 0.85f);
            dayContestedDropTriggerTime = Time.time + (dayDuration * progress);
        }

        private void TryRunDayContestedDrop()
        {
            if (!enableDayContestedDrop || GameManager.Instance?.CurrentState != GameState.DayPhase)
            {
                return;
            }

            if (!dayContestedDropSpawned)
            {
                if (!float.IsPositiveInfinity(dayContestedDropTriggerTime) && Time.time >= dayContestedDropTriggerTime)
                {
                    StartDayContestedBroadcast();
                }
                return;
            }

            if (dayContestedDropState == DayContestedDropState.Broadcast && Time.time >= dayContestedDropStateUntil)
            {
                StartDayContestedDescent();
            }
        }

        private void StartDayContestedBroadcast()
        {
            if (dayContestedDropSpawned)
            {
                return;
            }

            dayContestedDropSpawned = true;
            dayContestedDropState = DayContestedDropState.Broadcast;
            dayContestedDropStateUntil = Time.time + Mathf.Max(2f, contestedBroadcastDuration);
            OnContestedDropStateChanged?.Invoke(dayContestedDropState, Mathf.Max(0f, dayContestedDropStateUntil - Time.time));

            OnStatusMessage?.Invoke("Radio ping: incoming contested supply drop.");
            RadioTransmissions.Instance?.ShowMessage("RADIO: Contested day drop inbound. Get ready to secure it.", 3f);
        }

        private void StartDayContestedDescent()
        {
            if (GameManager.Instance?.CurrentState != GameState.DayPhase)
            {
                return;
            }

            dayContestedDropState = DayContestedDropState.Descent;
            dayContestedDropStateUntil = float.PositiveInfinity;
            OnContestedDropStateChanged?.Invoke(dayContestedDropState, 0f);

            var player = GameObject.Find("Player");
            Vector3 basePos = player != null ? player.transform.position : Vector3.zero;
            Vector3 dropPos = GetRandomPickupSpawnPosition(basePos);

            if (GameManager.Instance != null)
            {
                var cfg = MapConfig.GetConfigForType(GameManager.Instance.SelectedMap);
                float halfW = Mathf.Max(2f, cfg.perimeterHalfW - 1f);
                float halfH = Mathf.Max(2f, cfg.perimeterHalfH - 1f);
                dropPos.x = Mathf.Clamp(dropPos.x, -halfW, halfW);
                dropPos.y = Mathf.Clamp(dropPos.y, -halfH, halfH);
            }

            int night = GameManager.Instance?.CurrentNight ?? 1;
            CrateTier tier = RollCrateTier(night + 1);

            var heli = new GameObject("DayContestedHelicopter");
            var drop = heli.AddComponent<HelicopterDrop>();
            drop.Initialize(dropPos, tier, HandleContestedDropCrateLanded);

            OnStatusMessage?.Invoke("Contested drop descending. Move to secure.");
            RadioTransmissions.Instance?.ShowMessage("RADIO: Contested drop in descent. Secure it before it expires.", 3f);
        }

        private void HandleContestedDropCrateLanded(SupplyCrate crate)
        {
            if (crate == null || GameManager.Instance?.CurrentState != GameState.DayPhase)
            {
                return;
            }

            activeContestedDropCrate = crate;
            if (!spawnedCrates.Contains(crate.gameObject))
            {
                spawnedCrates.Add(crate.gameObject);
            }

            crate.ConfigureContested(
                contestedSecureHoldTime,
                contestedExpiryTime,
                HandleContestedDropSecured,
                HandleContestedDropExpired);

            dayContestedDropState = DayContestedDropState.Secure;
            dayContestedDropStateUntil = Time.time + Mathf.Max(contestedExpiryTime, contestedSecureHoldTime);
            OnContestedDropStateChanged?.Invoke(dayContestedDropState, Mathf.Max(0f, dayContestedDropStateUntil - Time.time));

            OnStatusMessage?.Invoke("Contested drop landed. Hold F to secure.");
            RadioTransmissions.Instance?.ShowMessage("RADIO: Drop landed. Hold position and secure it now!", 3f);

            var marker = FindFirstObjectByType<Deadlight.UI.ObjectiveMarker>();
            if (marker != null && crate != null)
                marker.PingContestedDrop(crate.transform);
        }

        private void HandleContestedDropSecured(SupplyCrate crate)
        {
            if (dayContestedDropState == DayContestedDropState.Resolved)
            {
                return;
            }

            dayContestedDropState = DayContestedDropState.Resolved;
            dayContestedDropStateUntil = float.PositiveInfinity;
            activeContestedDropCrate = null;
            OnContestedDropStateChanged?.Invoke(dayContestedDropState, 0f);

            if (IsCraftingEnabled())
            {
                CraftingSystem.Instance?.NotifyContestedDropSecured();
            }
            OnStatusMessage?.Invoke("Contested drop secured. Bonus supplies acquired.");
            RadioTransmissions.Instance?.ShowMessage("RADIO: Drop secured. Excellent work.", 2.5f);
        }

        private void HandleContestedDropExpired(SupplyCrate crate)
        {
            if (dayContestedDropState == DayContestedDropState.Resolved || dayContestedDropState == DayContestedDropState.Expired)
            {
                return;
            }

            dayContestedDropState = DayContestedDropState.Expired;
            dayContestedDropStateUntil = float.PositiveInfinity;
            activeContestedDropCrate = null;
            OnContestedDropStateChanged?.Invoke(dayContestedDropState, 0f);

            OnStatusMessage?.Invoke("Contested drop expired.");
            RadioTransmissions.Instance?.ShowMessage("RADIO: Contested drop lost.", 2f);
        }

        private static readonly int[] crateCountsByNight = { 2, 2, 3, 3 };

        private void SpawnSupplyCrates()
        {
            var player = GameObject.Find("Player");
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
            int night = GameManager.Instance?.CurrentNight ?? 1;
            int nightIdx = Mathf.Clamp(night - 1, 0, crateCountsByNight.Length - 1);
            int crateCount = crateCountsByNight[nightIdx];

            for (int i = 0; i < crateCount; i++)
            {
                Vector3 pos = GetRandomPickupSpawnPosition(playerPos);
                pos += new Vector3(UnityEngine.Random.Range(-2f, 2f), UnityEngine.Random.Range(-2f, 2f), 0);

                CrateTier tier = RollCrateTier(night);
                var crateObj = new GameObject($"SupplyCrate_{i}");
                crateObj.transform.position = pos;
                var crate = crateObj.AddComponent<Systems.SupplyCrate>();
                crate.SetTier(tier);
                spawnedCrates.Add(crateObj);
            }
        }

        private CrateTier RollCrateTier(int night)
        {
            float roll = UnityEngine.Random.value;
            float legendaryChance = Mathf.Clamp01(0.02f + night * 0.03f);
            float rareChance = Mathf.Clamp01(0.10f + night * 0.05f);

            if (roll < legendaryChance) return CrateTier.Legendary;
            if (roll < legendaryChance + rareChance) return CrateTier.Rare;
            return CrateTier.Common;
        }

        private void TrySpawnHelicopterDrop()
        {
            if (GameManager.Instance?.CurrentState != GameState.NightPhase) return;
            if (dropsThisPhase >= maxDropsPerPhase) return;
            if (float.IsPositiveInfinity(nextHelicopterDropTime)) return;
            if (Time.time < nextHelicopterDropTime) return;

            var player = GameObject.Find("Player");
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            Vector3 dropPos = GetRandomPickupSpawnPosition(playerPos);

            if (GameManager.Instance != null)
            {
                var cfg = MapConfig.GetConfigForType(GameManager.Instance.SelectedMap);
                float halfW = Mathf.Max(2f, cfg.perimeterHalfW - 1f);
                float halfH = Mathf.Max(2f, cfg.perimeterHalfH - 1f);
                dropPos.x = Mathf.Clamp(dropPos.x, -halfW, halfW);
                dropPos.y = Mathf.Clamp(dropPos.y, -halfH, halfH);
            }

            int night = GameManager.Instance.CurrentNight;
            CrateTier tier = RollCrateTier(night);

            var heli = new GameObject("Helicopter");
            var drop = heli.AddComponent<HelicopterDrop>();
            drop.Initialize(dropPos, tier);

            dropsThisPhase++;
            if (dropsThisPhase < maxDropsPerPhase)
            {
                ScheduleNextHelicopterDrop(Time.time + helicopterCooldown);
            }
            else
            {
                nextHelicopterDropTime = float.PositiveInfinity;
            }

            if (RadioTransmissions.Instance != null)
                RadioTransmissions.Instance.ShowMessage("Supply drop incoming! Look for the helicopter!", 3f);
        }

        private void ScheduleNextHelicopterDrop(float baseTime)
        {
            float jitter = Mathf.Max(0f, helicopterDropJitter);
            nextHelicopterDropTime = baseTime + (jitter > 0f ? UnityEngine.Random.Range(0f, jitter) : 0f);
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
            activeContestedDropCrate = null;

            foreach (var go in spawnedObjectiveObjects)
                if (go != null) Destroy(go);
            spawnedObjectiveObjects.Clear();

            ClearSpawnedPickups();
        }

        private void ApplyNightPacing(int night)
        {
            if (dayNightCycle == null)
            {
                dayNightCycle = FindFirstObjectByType<DayNightCycle>();
            }

            if (dayNightCycle == null)
            {
                return;
            }

            int idx = Mathf.Clamp(night - 1, 0, Mathf.Max(0, dayDurationsByNight.Length - 1));
            dayNightCycle.SetDayDuration(dayDurationsByNight.Length > 0 ? dayDurationsByNight[idx] : 60f);

            int nightIdx = Mathf.Clamp(night - 1, 0, Mathf.Max(0, nightDurationsByNight.Length - 1));
            dayNightCycle.SetNightDuration(nightDurationsByNight.Length > 0 ? nightDurationsByNight[nightIdx] : 120f);
        }

        private bool IsCraftingEnabled()
        {
            return GameManager.Instance != null && GameManager.Instance.CraftingEnabled;
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

        private Collider2D playerCollider;
        private CircleCollider2D pickupCollider;
        private SpriteRenderer pickupRenderer;
        private bool consumed;

        public void Initialize(PickupKind pickupKind, int pickupAmount)
        {
            kind = pickupKind;
            amount = pickupAmount;
        }

        private void Awake()
        {
            pickupCollider = GetComponent<CircleCollider2D>();
            pickupRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            TryConsumeNearbyPlayer();
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            TryConsumeNearbyPlayer();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryConsume(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryConsume(other);
        }

        private void TryConsumeNearbyPlayer()
        {
            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<CircleCollider2D>();
            }

            if (pickupRenderer == null)
            {
                pickupRenderer = GetComponent<SpriteRenderer>();
            }

            if (playerCollider == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerCollider = player.GetComponent<Collider2D>();
                }
            }

            if (playerCollider != null)
            {
                TryConsume(playerCollider);
            }
        }

        private void TryConsume(Collider2D other)
        {
            if (consumed)
            {
                return;
            }

            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<CircleCollider2D>();
            }

            if (pickupRenderer == null)
            {
                pickupRenderer = GetComponent<SpriteRenderer>();
            }

            var health = other.GetComponent<PlayerHealth>();
            var shooting = other.GetComponent<PlayerShooting>();

            if (health == null && shooting == null) return;
            if (!PickupContactUtility.IsTightPickupContact(pickupCollider, pickupRenderer, other)) return;

            bool didConsume = false;

            if (kind == PickupKind.Health && health != null && health.IsAlive)
            {
                health.Heal(amount);
                didConsume = true;
            }
            else if (kind == PickupKind.Ammo && shooting != null)
            {
                shooting.AddAmmo(amount);
                didConsume = true;
            }

            if (didConsume)
            {
                this.consumed = true;
                Deadlight.UI.GameplayHelpSystem.Instance?.ShowPickup(
                    kind == PickupKind.Health ? PickupType.Health : PickupType.Ammo,
                    amount);
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
