using System;
using System.Collections;
using System.Collections.Generic;
using Deadlight.Data;
using Deadlight.Enemy;
using Deadlight.Level;
using Deadlight.Narrative;
using Deadlight.Player;
using Deadlight.Systems;
using Deadlight.UI;
using Deadlight.Visuals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deadlight.Core
{
    public enum GameState
    {
        MainMenu,
        DayPhase,
        Transition,
        NightPhase,
        DawnPhase,
        LevelComplete,
        GameOver,
        Victory
    }

    public enum MapType
    {
        TownCenter,
        Industrial,
        Suburban,
        Research
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private CampaignBalanceProfile campaignBalanceProfile;

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private MapType selectedMap = MapType.TownCenter;
        [SerializeField] private int currentNight = 1;
        [SerializeField] private int maxNights = 12;
        [SerializeField] private bool isPaused;
        [Header("Playable Scope")]
        [SerializeField, Range(1, TotalLevels)] private int playableLevelCap = TotalLevels;

        public const int NightsPerLevel = 3;
        public const int TotalLevels = 4;
        private const string UnlockedLevelKey = "Deadlight_UnlockedLevel";
        private const string HighestCompletedLevelKey = "Deadlight_HighestCompletedLevel";

        [Header("Campaign Progression")]
        [SerializeField] private MapType[] campaignMapOrder =
        {
            MapType.TownCenter,
            MapType.Suburban,
            MapType.Industrial,
            MapType.Research
        };

        [Header("Runtime Fallback")]
        [SerializeField] private bool autoBootstrapGameScene = true;
        [SerializeField] private bool autoStartWhenGameSceneLoads = false;
        [SerializeField] private float dawnAutoAdvanceDelay = 2f;
        [Header("Level Progression")]
        [SerializeField, Range(0f, 1f)] private float interLevelPointCarryRatio = 0.65f;
        [Header("Objective Miss Rules")]
        [SerializeField, Min(0)] private int maxObjectiveRetriesPerStep = 1;
        [SerializeField, Range(1f, 2f)] private float missedObjectiveEnemyPenaltyMultiplier = 1.2f;
        [SerializeField, Range(0.2f, 1f)] private float missedObjectiveCarryoverPenaltyMultiplier = 0.75f;
        [Header("Feature Toggles")]
        [SerializeField] private bool enableCrafting = true;

        private const float DefaultFixedDeltaTime = 0.02f;

        public GameState CurrentState => currentState;
        public MapType SelectedMap => selectedMap;
        public int CurrentNight => currentNight;
        public int MaxNights => maxNights;
        public int CurrentLevel => GetLevelForNight(currentNight);
        public int NightWithinLevel => GetNightWithinLevel(currentNight);
        public int PlayableLevelCap => Mathf.Clamp(playableLevelCap, 1, TotalLevels);
        public CampaignBalanceProfile CurrentBalance => campaignBalanceProfile;
        public bool IsPaused => isPaused;
        public bool IsGameplayState => IsGameplayStateValue(currentState);
        public bool ShouldSetupGameplayScene => startNewRunAfterGameSceneLoad || currentState != GameState.MainMenu || autoStartWhenGameSceneLoads;
        public bool ShouldSuppressMainMenuPresentation =>
            currentState == GameState.MainMenu &&
            !startNewRunAfterGameSceneLoad &&
            !autoStartWhenGameSceneLoads &&
            (!startupIntroShown || startupIntroInProgress);
        public bool CraftingEnabled => enableCrafting;
        public float RunStartTime { get; private set; }
        public float InterLevelPointCarryRatio => interLevelPointCarryRatio;
        public bool WillRetryCurrentStepOnAdvance => repeatCurrentNightOnAdvance;
        public int PendingObjectiveCarryoverPenaltyStacks => queuedCarryoverPenaltyStacks;
        public float CurrentNightEnemyPenaltyMultiplier => objectivePenaltyActiveForCurrentNight ? missedObjectiveEnemyPenaltyMultiplier : 1f;
        public bool IsObjectivePenaltyActiveThisNight => objectivePenaltyActiveForCurrentNight;

        public static int GetLevelForNight(int night) => Mathf.Clamp((night - 1) / NightsPerLevel + 1, 1, TotalLevels);
        public static int GetNightWithinLevel(int night) => ((night - 1) % NightsPerLevel) + 1;
        public static int GetFirstNightOfLevel(int level) => (Mathf.Clamp(level, 1, TotalLevels) - 1) * NightsPerLevel + 1;
        public static bool IsLastNightOfLevel(int night) => (night % NightsPerLevel) == 0 || night >= TotalLevels * NightsPerLevel;

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnNightChanged;
        public event Action<bool> OnPauseChanged;

        private GameObject runtimeBulletPrefab;
        private Material runtimeSpriteMaterial;
        private Coroutine dawnAdvanceCoroutine;
        private Coroutine deferredRestartCoroutine;
        private bool isBootstrappingScene;
        private bool startNewRunAfterGameSceneLoad;
        private bool startupIntroShown;
        private bool startupIntroInProgress;
        private int queuedStartNight = 1;
        private readonly Dictionary<int, int> missedObjectiveRetriesByNight = new Dictionary<int, int>();
        private bool repeatCurrentNightOnAdvance;
        private bool objectivePenaltyActiveForCurrentNight;
        private int queuedEnemyPenaltyNights;
        private int queuedCarryoverPenaltyStacks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntimeGameManager()
        {
            if (Instance != null)
            {
                return;
            }

            var existing = FindFirstObjectByType<GameManager>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var managerObject = new GameObject("GameManager");
            managerObject.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCampaignMapOrder();
            EnsureCampaignBalanceProfile();
            NormalizeCampaignUnlockProgress();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == "Game")
            {
                TryBeginStartupIntro();
                StartCoroutine(BootstrapGameSceneNextFrame());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && IsGameplayState &&
                !(GameUI.Instance != null && GameUI.Instance.IsGuideOpen))
            {
                TogglePause();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Game")
            {
                TryBeginStartupIntro();
                StartCoroutine(BootstrapGameSceneNextFrame());

                if (startNewRunAfterGameSceneLoad)
                {
                    if (deferredRestartCoroutine != null)
                    {
                        StopCoroutine(deferredRestartCoroutine);
                    }

                    deferredRestartCoroutine = StartCoroutine(StartRunAfterSceneLoad());
                }
            }
        }

        private IEnumerator StartRunAfterSceneLoad()
        {
            while (isBootstrappingScene)
            {
                yield return null;
            }

            // Give newly loaded scene objects one frame to initialize.
            yield return null;

            startNewRunAfterGameSceneLoad = false;
            deferredRestartCoroutine = null;
            StartNewGame();
        }

        private IEnumerator BootstrapGameSceneNextFrame()
        {
            if (isBootstrappingScene)
            {
                yield break;
            }

            isBootstrappingScene = true;
            yield return null;

            if (autoBootstrapGameScene && ShouldSetupGameplayScene)
            {
                EnsureCoreManagers();
                EnsureWaveManagerConfigured();

                var player = EnsurePlayerExists();
                ConfigurePlayer(player);
                EnsureCameraTargetsPlayer(player);
            }

            var intro = FindFirstObjectByType<IntroSequence>();
            if (autoStartWhenGameSceneLoads && currentState == GameState.MainMenu && intro == null)
            {
                StartNewGame();
            }

            isBootstrappingScene = false;
        }

        private void TryBeginStartupIntro()
        {
            if (startupIntroShown || startupIntroInProgress || startNewRunAfterGameSceneLoad || autoStartWhenGameSceneLoads)
            {
                return;
            }

            if (SceneManager.GetActiveScene().name != "Game" || currentState != GameState.MainMenu)
            {
                return;
            }

            if (FindFirstObjectByType<IntroSequence>() != null)
            {
                startupIntroInProgress = true;
                return;
            }

            var introObject = new GameObject("IntroSequence");
            introObject.AddComponent<IntroSequence>();
            startupIntroInProgress = true;
        }

        public void NotifyStartupIntroFinished()
        {
            startupIntroInProgress = false;
            startupIntroShown = true;
        }

        public void SetMap(MapType map)
        {
            selectedMap = map;
        }

        public void StartNewGame()
        {
            EnsureCampaignMapOrder();
            EnsureCampaignBalanceProfile();
            EnsureCoreManagers();

            startNewRunAfterGameSceneLoad = false;
            if (deferredRestartCoroutine != null)
            {
                StopCoroutine(deferredRestartCoroutine);
                deferredRestartCoroutine = null;
            }

            currentNight = Mathf.Clamp(queuedStartNight, 1, maxNights);
            queuedStartNight = 1;
            selectedMap = GetCampaignMapForNight(currentNight);
            RunStartTime = Time.realtimeSinceStartup;
            ResetRunState();
            RebuildMapForCurrentLevel();

            var player = EnsurePlayerExists();
            ConfigurePlayer(player);
            EnsureCameraTargetsPlayer(player);

            var shooting = player != null ? player.GetComponent<PlayerShooting>() : null;
            if (shooting != null)
            {
                shooting.ResetLoadout(WeaponData.CreatePistol());
            }

            var modifierSystem = FindFirstObjectByType<RunModifierSystem>();
            if (modifierSystem != null)
            {
                int runSeed = DateTime.Now.Millisecond + (int)Time.realtimeSinceStartup;
                modifierSystem.GenerateRunModifiers(runSeed);
                modifierSystem.RollNightEvent(currentNight, runSeed + 37);
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.ResetAllSpawnPoints();
            }

            RadioTransmissions.Instance?.ResetRuntimeState();
            StoryEventManager.Instance?.ResetSession();

            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.ResetRuntimeStateForNewRun(clearPlayedDialogues: true);
            }

            StoryObjective.Instance?.ResetStoryProgress();
            EnvironmentalLore.Instance?.ResetDiscoveries();
            GameplayHelpSystem.Instance?.ResetSession();

            if (currentState == GameState.DayPhase)
            {
                currentState = GameState.MainMenu;
            }

            SetPaused(false);
            ChangeState(GameState.DayPhase);
            OnNightChanged?.Invoke(currentNight);
        }

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;

            if (!IsGameplayStateValue(newState))
            {
                SetPaused(false);
            }

            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        public void StartNightPhase()
        {
            ChangeState(GameState.NightPhase);

            FirstCombatHintController.NotifyAfterNightPhaseEntered();

            var waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null && !waveManager.IsSpawning && waveManager.EnemiesRemaining <= 0)
            {
                waveManager.StartNightWaves();
            }
        }

        public void OnNightSurvived()
        {
            if (currentState != GameState.NightPhase)
            {
                return;
            }

            if (ShouldRetryCurrentStepAfterNight())
            {
                TransitionToDawnPhase();
                return;
            }

            if (currentNight >= maxNights)
            {
                UnlockNextLevel();
                ChangeState(GameState.Victory);
                return;
            }

            if (IsLastNightOfLevel(currentNight))
            {
                UnlockNextLevel();
                ChangeState(GameState.LevelComplete);
                return;
            }

            TransitionToDawnPhase();
        }

        public void AdvanceToNextNight()
        {
            if (currentState == GameState.Victory || currentState == GameState.GameOver ||
                currentState == GameState.LevelComplete)
            {
                return;
            }

            bool retryingCurrentStep = repeatCurrentNightOnAdvance;
            repeatCurrentNightOnAdvance = false;

            if (!retryingCurrentStep)
            {
                currentNight = Mathf.Min(currentNight + 1, maxNights);
                selectedMap = GetCampaignMapForNight(currentNight);
                ApplyQueuedEnemyPenaltyForCurrentNight();
            }
            else
            {
                RadioTransmissions.Instance?.ShowMessage(
                    "Objective missed. Retry active: this level step repeats once.", 3.8f);
            }

            OnNightChanged?.Invoke(currentNight);
            ChangeState(GameState.DayPhase);
        }

        public void StartNextLevel()
        {
            if (currentNight >= maxNights)
            {
                ChangeState(GameState.Victory);
                return;
            }

            ResetInterLevelProgressionState();

            repeatCurrentNightOnAdvance = false;
            currentNight = Mathf.Min(currentNight + 1, maxNights);
            selectedMap = GetCampaignMapForNight(currentNight);
            ApplyQueuedEnemyPenaltyForCurrentNight();
            RebuildMapForCurrentLevel();
            OnNightChanged?.Invoke(currentNight);
            ChangeState(GameState.DayPhase);
        }

        private void ResetInterLevelProgressionState()
        {
            // Starting a new level should be a clean slate.
            ClearObjectiveMissState();
            PointsSystem.Instance?.ResetSession();
            ResourceManager.Instance?.ResetInventory();
            ProgressionManager.Instance?.ResetProgress();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player == null)
            {
                return;
            }

            player.GetComponent<PlayerUpgrades>()?.ResetUpgrades();

            float baseHealth = 100f;
            if (CurrentBalance != null)
            {
                baseHealth *= CurrentBalance.playerHealthMultiplier;
            }
            player.GetComponent<PlayerHealth>()?.SetMaxHealth(baseHealth, true);

            var shooting = player.GetComponent<PlayerShooting>();
            if (shooting != null)
            {
                shooting.ResetLoadout(WeaponData.CreatePistol());
            }

            player.GetComponent<PlayerMedkitSystem>()?.ResetMedkits();
            player.GetComponent<PlayerArmor>()?.ResetArmor();
            player.GetComponent<ThrowableSystem>()?.ResetInventory();
        }

        public float GetProjectedCarryoverRatio()
        {
            float ratio = Mathf.Clamp01(interLevelPointCarryRatio);
            if (queuedCarryoverPenaltyStacks > 0)
            {
                ratio *= Mathf.Pow(missedObjectiveCarryoverPenaltyMultiplier, queuedCarryoverPenaltyStacks);
            }

            return Mathf.Clamp01(ratio);
        }

        public void OnPlayerDeath()
        {
            ChangeState(GameState.GameOver);
        }

        public void ReturnToMainMenu()
        {
            currentNight = 1;
            queuedStartNight = 1;
            startNewRunAfterGameSceneLoad = false;
            ClearObjectiveMissState();

            if (deferredRestartCoroutine != null)
            {
                StopCoroutine(deferredRestartCoroutine);
                deferredRestartCoroutine = null;
            }

            SetPaused(false);
            ChangeState(GameState.MainMenu);
            SceneManager.LoadScene("Game");
        }

        public void RestartGame()
        {
            currentNight = 1;
            queuedStartNight = 1;
            startNewRunAfterGameSceneLoad = true;
            ClearObjectiveMissState();
            SetPaused(false);
            ChangeState(GameState.MainMenu);
            SceneManager.LoadScene("Game");
        }

        public void LoadGameScene()
        {
            SceneManager.LoadScene("Game");
        }

        public void StartSelectedMapRun()
        {
            queuedStartNight = 1;
            currentNight = 1;
            selectedMap = GetCampaignMapForNight(currentNight);
            startNewRunAfterGameSceneLoad = true;
            ClearObjectiveMissState();

            if (deferredRestartCoroutine != null)
            {
                StopCoroutine(deferredRestartCoroutine);
                deferredRestartCoroutine = null;
            }

            SetPaused(false);
            ChangeState(GameState.MainMenu);
            SceneManager.LoadScene("Game");
        }

        public void StartCampaignFromLevel(int level)
        {
            EnsureCampaignMapOrder();

            int clamped = Mathf.Clamp(level, 1, GetPlayableLevelCap());
            if (!IsLevelUnlocked(clamped)) return;

            queuedStartNight = GetFirstNightOfLevel(clamped);
            currentNight = queuedStartNight;
            selectedMap = GetCampaignMapForNight(currentNight);
            startNewRunAfterGameSceneLoad = true;
            ClearObjectiveMissState();

            if (deferredRestartCoroutine != null)
            {
                StopCoroutine(deferredRestartCoroutine);
                deferredRestartCoroutine = null;
            }

            SetPaused(false);
            ChangeState(GameState.MainMenu);
            SceneManager.LoadScene("Game");
        }

        public bool IsLevelUnlocked(int level)
        {
            if (level > GetPlayableLevelCap()) return false;
            if (level <= 1) return true;

            int highestCompleted = GetHighestCompletedLevel();
            int storedUnlocked = GetStoredHighestUnlockedLevel();
            int effectiveHighestUnlocked = Mathf.Min(storedUnlocked, highestCompleted + 1);
            return level <= effectiveHighestUnlocked;
        }

        public int HighestUnlockedLevel
        {
            get
            {
                int highestCompleted = GetHighestCompletedLevel();
                int storedUnlocked = GetStoredHighestUnlockedLevel();
                return Mathf.Clamp(Mathf.Min(storedUnlocked, highestCompleted + 1), 1, GetPlayableLevelCap());
            }
        }

        private void UnlockNextLevel()
        {
            int completedLevel = CurrentLevel;
            int highestCompleted = GetHighestCompletedLevel();
            bool changed = false;

            if (completedLevel > highestCompleted)
            {
                PlayerPrefs.SetInt(HighestCompletedLevelKey, completedLevel);
                highestCompleted = completedLevel;
                changed = true;
            }

            int next = Mathf.Min(completedLevel + 1, GetPlayableLevelCap());
            int current = GetStoredHighestUnlockedLevel();
            int maxAllowedUnlocked = Mathf.Clamp(highestCompleted + 1, 1, GetPlayableLevelCap());
            int targetUnlocked = Mathf.Min(next, maxAllowedUnlocked);
            if (targetUnlocked > current)
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, targetUnlocked);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        private int GetStoredHighestUnlockedLevel()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, GetPlayableLevelCap());
        }

        private int GetHighestCompletedLevel()
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(HighestCompletedLevelKey, 0), 0, GetPlayableLevelCap());
        }

        private void NormalizeCampaignUnlockProgress()
        {
            int playableCap = GetPlayableLevelCap();
            int highestCompleted = GetHighestCompletedLevel();
            int storedUnlocked = GetStoredHighestUnlockedLevel();
            int maxAllowedUnlocked = Mathf.Clamp(highestCompleted + 1, 1, playableCap);
            int normalizedUnlocked = Mathf.Min(storedUnlocked, maxAllowedUnlocked);

            bool changed = false;
            if (!PlayerPrefs.HasKey(UnlockedLevelKey) || storedUnlocked != normalizedUnlocked)
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, normalizedUnlocked);
                changed = true;
            }

            if (!PlayerPrefs.HasKey(HighestCompletedLevelKey))
            {
                PlayerPrefs.SetInt(HighestCompletedLevelKey, highestCompleted);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void TogglePause()
        {
            if (!CanPauseCurrentState())
            {
                return;
            }

            SetPaused(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            if (paused && !CanPauseCurrentState())
            {
                return;
            }

            isPaused = paused;

            Time.timeScale = isPaused ? 0f : 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime * Mathf.Max(Time.timeScale, 0f);

            var dayNight = FindFirstObjectByType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.SetPaused(isPaused);
            }

            OnPauseChanged?.Invoke(isPaused);
        }

        private bool CanPauseCurrentState()
        {
            return IsGameplayState;
        }

        public static bool IsGameplayStateValue(GameState state)
        {
            return state == GameState.DayPhase || state == GameState.NightPhase;
        }

        public static bool IsActivePlayState(GameState state)
        {
            return state == GameState.DayPhase || state == GameState.NightPhase ||
                   state == GameState.DawnPhase || state == GameState.LevelComplete;
        }

        private void EnsureCampaignBalanceProfile()
        {
            if (campaignBalanceProfile == null)
            {
                campaignBalanceProfile = CampaignBalanceProfile.CreateDefaultProfile();
            }
        }

        private void EnsureCampaignMapOrder()
        {
            maxNights = GetPlayableLevelCap() * NightsPerLevel;

            if (campaignMapOrder != null && campaignMapOrder.Length >= TotalLevels)
            {
                return;
            }

            campaignMapOrder = new[]
            {
                MapType.TownCenter,
                MapType.Suburban,
                MapType.Industrial,
                MapType.Research
            };
        }

        private MapType GetCampaignMapForNight(int night)
        {
            EnsureCampaignMapOrder();

            if (campaignMapOrder == null || campaignMapOrder.Length == 0)
            {
                return MapType.TownCenter;
            }

            int level = GetLevelForNight(night);
            int idx = Mathf.Clamp(level - 1, 0, campaignMapOrder.Length - 1);
            return campaignMapOrder[idx];
        }

        private int GetPlayableLevelCap()
        {
            return Mathf.Clamp(playableLevelCap, 1, TotalLevels);
        }

        private void RebuildMapForCurrentLevel()
        {
            var sceneSetup = FindFirstObjectByType<TestSceneSetup>();
            if (sceneSetup != null)
            {
                sceneSetup.RebuildMap(selectedMap, repositionPlayer: true);
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = GetPlayerSpawnPosition();
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }

        private void EnsureCoreManagers()
        {
            if (FindFirstObjectByType<DayNightCycle>() == null)
            {
                new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
            }

            if (FindFirstObjectByType<WaveManager>() == null)
            {
                new GameObject("WaveManager").AddComponent<WaveManager>();
            }

            if (FindFirstObjectByType<GameFlowController>() == null)
            {
                new GameObject("GameFlowController").AddComponent<GameFlowController>();
            }

            if (FindFirstObjectByType<ResourceManager>() == null)
            {
                new GameObject("ResourceManager").AddComponent<ResourceManager>();
            }

            if (FindFirstObjectByType<PickupSpawner>() == null)
            {
                new GameObject("PickupSpawner").AddComponent<PickupSpawner>();
            }

            if (FindFirstObjectByType<PowerupSystem>() == null)
            {
                new GameObject("PowerupSystem").AddComponent<PowerupSystem>();
            }

            if (FindFirstObjectByType<PointsSystem>() == null)
            {
                new GameObject("PointsSystem").AddComponent<PointsSystem>();
            }

            if (FindFirstObjectByType<ProgressionManager>() == null)
            {
                new GameObject("ProgressionManager").AddComponent<ProgressionManager>();
            }

            if (FindFirstObjectByType<DayObjectiveSystem>() == null)
            {
                new GameObject("DayObjectiveSystem").AddComponent<DayObjectiveSystem>();
            }

            if (FindFirstObjectByType<RunModifierSystem>() == null)
            {
                new GameObject("RunModifierSystem").AddComponent<RunModifierSystem>();
            }

            if (enableCrafting)
            {
                if (FindFirstObjectByType<CraftingSystem>() == null)
                {
                    new GameObject("CraftingSystem").AddComponent<CraftingSystem>();
                }
            }
            else
            {
                var crafting = FindFirstObjectByType<CraftingSystem>();
                if (crafting != null)
                {
                    Destroy(crafting.gameObject);
                }
            }

            var narrativeManager = FindFirstObjectByType<NarrativeManager>();
            if (narrativeManager == null)
            {
                var narrativeObject = new GameObject("NarrativeManager");
                narrativeManager = narrativeObject.AddComponent<NarrativeManager>();
                narrativeObject.AddComponent<EnvironmentalLore>();
            }
            else if (FindFirstObjectByType<EnvironmentalLore>() == null)
            {
                narrativeManager.gameObject.AddComponent<EnvironmentalLore>();
            }

            if (FindFirstObjectByType<LevelIntroSequence>() == null)
            {
                new GameObject("LevelIntroSequence").AddComponent<LevelIntroSequence>();
            }

            if (FindFirstObjectByType<AudioManager>() == null)
            {
                new GameObject("AudioManager").AddComponent<AudioManager>();
            }

            if (FindFirstObjectByType<EndingSequence>() == null)
            {
                new GameObject("EndingSequence").AddComponent<EndingSequence>();
            }

            if (FindFirstObjectByType<StoryEventManager>() == null)
            {
                new GameObject("StoryEventManager").AddComponent<StoryEventManager>();
            }

            if (FindFirstObjectByType<FirstCombatHintController>() == null)
            {
                new GameObject("FirstCombatHintController").AddComponent<FirstCombatHintController>();
            }

            if (FindFirstObjectByType<SupportMarkerGuidanceController>() == null)
            {
                new GameObject("SupportMarkerGuidanceController").AddComponent<SupportMarkerGuidanceController>();
            }

            if (FindFirstObjectByType<StoryObjective>() == null)
            {
                new GameObject("StoryObjective").AddComponent<StoryObjective>();
            }

            if (FindFirstObjectByType<NarrativeJournalUI>() == null)
            {
                new GameObject("NarrativeJournalUI").AddComponent<NarrativeJournalUI>();
            }

            if (FindFirstObjectByType<GameplayHelpSystem>() == null)
            {
                new GameObject("GameplayHelpSystem").AddComponent<GameplayHelpSystem>();
            }

            if (FindFirstObjectByType<RadioTransmissions>() == null)
            {
                new GameObject("RadioTransmissions").AddComponent<RadioTransmissions>();
            }

            if (FindFirstObjectByType<NightMutation>() == null)
            {
                new GameObject("NightMutation").AddComponent<NightMutation>();
            }

            if (FindFirstObjectByType<KillStreakSystem>() == null)
            {
                new GameObject("KillStreakSystem").AddComponent<KillStreakSystem>();
            }

            if (FindFirstObjectByType<LeaderboardManager>() == null)
            {
                new GameObject("LeaderboardManager").AddComponent<LeaderboardManager>();
            }
        }

        private GameObject EnsurePlayerExists()
        {
            var playerControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            GameObject primaryPlayer = null;

            if (playerControllers.Length > 0)
            {
                int bestIndex = 0;
                bool foundSpritePlayer = false;

                for (int i = 0; i < playerControllers.Length; i++)
                {
                    var candidateRenderer = playerControllers[i].GetComponent<SpriteRenderer>();
                    if (candidateRenderer != null && candidateRenderer.sprite != null)
                    {
                        bestIndex = i;
                        foundSpritePlayer = true;
                        break;
                    }
                }

                if (!foundSpritePlayer)
                {
                    bestIndex = 0;
                }

                primaryPlayer = playerControllers[bestIndex].gameObject;

                for (int i = 0; i < playerControllers.Length; i++)
                {
                    if (i == bestIndex) continue;
                    if (playerControllers[i] != null && playerControllers[i].gameObject.activeSelf)
                    {
                        Debug.LogWarning("[GameManager] Multiple players detected. Disabling duplicate player object.");
                        playerControllers[i].gameObject.SetActive(false);
                    }
                }
            }

            if (primaryPlayer == null)
            {
                var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                if (taggedPlayer != null)
                {
                    primaryPlayer = taggedPlayer;
                }
            }

            if (primaryPlayer == null)
            {
                var namedPlayer = GameObject.Find("Player");
                if (namedPlayer != null)
                {
                    primaryPlayer = namedPlayer;
                }
            }

            if (primaryPlayer != null)
            {
                primaryPlayer.tag = "Player";
                return primaryPlayer;
            }

            var player = new GameObject("Player")
            {
                tag = "Player"
            };

            player.transform.position = GetPlayerSpawnPosition();
            return player;
        }

        private void ConfigurePlayer(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            if (!player.CompareTag("Player"))
            {
                player.tag = "Player";
            }

            var spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = player.AddComponent<SpriteRenderer>();
            }

            bool hasAnySprite = false;
            var allRenderers = player.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] != null && allRenderers[i].sprite != null)
                {
                    hasAnySprite = true;
                    break;
                }
            }

            if (spriteRenderer.sprite == null && !hasAnySprite)
            {
                spriteRenderer.sprite = ProceduralSpriteGenerator.CreatePlayerSprite(0, 0);
                spriteRenderer.sortingOrder = 10;
            }

            if (runtimeSpriteMaterial == null)
            {
                Shader spriteShader = Shader.Find("Sprites/Default");
                if (spriteShader != null)
                {
                    runtimeSpriteMaterial = new Material(spriteShader);
                }
            }

            if (runtimeSpriteMaterial != null)
            {
                spriteRenderer.sharedMaterial = runtimeSpriteMaterial;
            }

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = player.AddComponent<Rigidbody2D>();
            }
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = player.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = player.AddComponent<CircleCollider2D>();
                collider.radius = 0.4f;
            }

            var controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                controller = player.AddComponent<PlayerController>();
            }
            controller.enabled = true;
            controller.SetCanMove(true);

            var health = player.GetComponent<PlayerHealth>();
            if (health == null)
            {
                health = player.AddComponent<PlayerHealth>();
            }
            health.FullHeal();
            health.SetInvincible(false);

            if (player.GetComponent<PlayerMedkitSystem>() == null)
            {
                player.AddComponent<PlayerMedkitSystem>();
            }

            var shooting = player.GetComponent<PlayerShooting>();
            if (shooting == null)
            {
                shooting = player.AddComponent<PlayerShooting>();
            }
            shooting.enabled = true;

            var firePoint = player.transform.Find("FirePoint");
            if (firePoint == null)
            {
                var firePointObj = new GameObject("FirePoint");
                firePointObj.transform.SetParent(player.transform);
                firePointObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                firePoint = firePointObj.transform;
            }

            shooting.SetFirePoint(firePoint);
            shooting.SetBulletPrefab(GetOrCreateRuntimeBulletPrefab());

            if (shooting.CurrentWeapon == null)
            {
                shooting.SetWeapon(WeaponData.CreatePistol());
            }

            if (player.GetComponent<AudioSource>() == null)
            {
                player.AddComponent<AudioSource>();
            }

            if (player.GetComponent<PlayerArmor>() == null)
            {
                player.AddComponent<PlayerArmor>();
            }

            if (player.GetComponent<PlayerUpgrades>() == null)
            {
                player.AddComponent<PlayerUpgrades>();
            }

            if (player.GetComponent<Narrative.PlayerVoice>() == null)
            {
                player.AddComponent<Narrative.PlayerVoice>();
            }
        }

        private void EnsureCameraTargetsPlayer(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            var cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController == null)
            {
                var mainCam = Camera.main;
                if (mainCam == null)
                {
                    var camObject = new GameObject("Main Camera");
                    camObject.tag = "MainCamera";
                    mainCam = camObject.AddComponent<Camera>();
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
                    mainCam.orthographic = true;
                    mainCam.orthographicSize = 8f;
                    camObject.AddComponent<AudioListener>();
                    camObject.transform.position = new Vector3(0f, 0f, -10f);
                }

                if (mainCam != null)
                {
                    cameraController = mainCam.GetComponent<CameraController>();
                    if (cameraController == null)
                    {
                        cameraController = mainCam.gameObject.AddComponent<CameraController>();
                    }
                }
            }

            cameraController?.SetTarget(player.transform);
        }

        private void EnsureWaveManagerConfigured()
        {
            var waveManager = FindFirstObjectByType<WaveManager>();
            waveManager?.EnsureRuntimeDefaults();
        }

        private Vector3 GetPlayerSpawnPosition()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.PlayerSpawnPoint != null)
            {
                return LevelManager.Instance.PlayerSpawnPoint.position;
            }

            return Vector3.zero;
        }

        private GameObject GetOrCreateRuntimeBulletPrefab()
        {
            if (runtimeBulletPrefab != null)
            {
                return runtimeBulletPrefab;
            }

            runtimeBulletPrefab = new GameObject("RuntimeBulletPrefab");
            runtimeBulletPrefab.SetActive(false);

            var sr = runtimeBulletPrefab.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRuntimeBulletSprite();
            sr.sortingOrder = 8;

            var trail = runtimeBulletPrefab.AddComponent<TrailRenderer>();
            trail.time = 0.055f;
            trail.startWidth = 0.18f;
            trail.endWidth = 0f;
            trail.sortingOrder = 7;
            trail.generateLightingData = false;
            var trailMat = new Material(Shader.Find("Sprites/Default"));
            trail.material = trailMat;
            var trailGradient = new Gradient();
            trailGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.1f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trail.colorGradient = trailGradient;

            var rb = runtimeBulletPrefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = runtimeBulletPrefab.AddComponent<CircleCollider2D>();
            col.radius = 0.12f;
            col.isTrigger = true;

            runtimeBulletPrefab.AddComponent<Bullet>();

            return runtimeBulletPrefab;
        }

        private static Sprite CreateRuntimeBulletSprite()
        {
            // 8x8 pixel, hot-white core fading to yellow-orange edge, PPU=24 → ~0.33 world unit
            const int size = 8;
            const float ppu = 24f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float maxRadius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(d / maxRadius);
                    if (t >= 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        // White-hot core → yellow → orange edge
                        Color core = Color.white;
                        Color mid = new Color(1f, 0.9f, 0.25f);
                        Color edge = new Color(1f, 0.55f, 0.05f, 0f);
                        float a = 1f - t * t;
                        Color c = t < 0.4f ? Color.Lerp(core, mid, t / 0.4f) : Color.Lerp(mid, edge, (t - 0.4f) / 0.6f);
                        c.a = a;
                        pixels[y * size + x] = c;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
        }

        private void ResetRunState()
        {
            if (dawnAdvanceCoroutine != null)
            {
                StopCoroutine(dawnAdvanceCoroutine);
                dawnAdvanceCoroutine = null;
            }

            ClearObjectiveMissState();

            var pointsSystem = PointsSystem.Instance;
            if (pointsSystem != null)
            {
                pointsSystem.ResetSession();
            }

            var resourceManager = ResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.ResetInventory();
            }

            var progressionManager = ProgressionManager.Instance;
            if (progressionManager != null)
            {
                progressionManager.ResetProgress();
            }

            // Always reset run-scoped upgrades on fresh run start, regardless of which UI path triggered restart.
            var playerUpgrades = FindFirstObjectByType<PlayerUpgrades>();
            if (playerUpgrades != null)
            {
                playerUpgrades.ResetUpgrades();
            }

            var objectiveSystem = FindFirstObjectByType<DayObjectiveSystem>();
            objectiveSystem?.ResetObjective();

            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            foreach (var pickup in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            {
                if (pickup != null)
                {
                    Destroy(pickup.gameObject);
                }
            }

            foreach (var pickupItem in FindObjectsByType<PickupItem>(FindObjectsSortMode.None))
            {
                if (pickupItem != null)
                {
                    Destroy(pickupItem.gameObject);
                }
            }

            foreach (var bullet in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            {
                if (bullet != null && bullet.gameObject.activeInHierarchy)
                {
                    Destroy(bullet.gameObject);
                }
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = GetPlayerSpawnPosition();
                ConfigurePlayer(player);

                var shooting = player.GetComponent<PlayerShooting>();
                if (shooting != null)
                {
                    shooting.ResetLoadout(WeaponData.CreatePistol());
                }

                player.GetComponent<PlayerMedkitSystem>()?.ResetMedkits();
            }
        }

        private void TransitionToDawnPhase()
        {
            ChangeState(GameState.DawnPhase);

            if (!HasInteractiveDawnUI())
            {
                if (dawnAdvanceCoroutine != null)
                {
                    StopCoroutine(dawnAdvanceCoroutine);
                }

                dawnAdvanceCoroutine = StartCoroutine(AutoAdvanceFromDawn());
            }
        }

        private bool ShouldRetryCurrentStepAfterNight()
        {
            if (!WasCurrentObjectiveMissed())
            {
                repeatCurrentNightOnAdvance = false;
                missedObjectiveRetriesByNight.Remove(currentNight);
                return false;
            }

            int retriesUsed = missedObjectiveRetriesByNight.TryGetValue(currentNight, out var count) ? count : 0;
            if (retriesUsed < Mathf.Max(0, maxObjectiveRetriesPerStep))
            {
                missedObjectiveRetriesByNight[currentNight] = retriesUsed + 1;
                repeatCurrentNightOnAdvance = true;
                RadioTransmissions.Instance?.ShowMessage(
                    "Objective missed. One retry granted before forced advance.", 4.2f);
                TriggerObjectivePenaltyFeedback(severe: false);
                return true;
            }

            repeatCurrentNightOnAdvance = false;
            QueueObjectiveMissPenalty();
            RadioTransmissions.Instance?.ShowMessage(
                "Objective missed again. Forced advance with penalties applied.", 4.8f);
            TriggerObjectivePenaltyFeedback(severe: true);
            return false;
        }

        private bool WasCurrentObjectiveMissed()
        {
            if (StoryObjective.Instance == null || !StoryObjective.Instance.HasActiveObjective)
            {
                return false;
            }

            return !StoryObjective.Instance.IsComplete;
        }

        private void QueueObjectiveMissPenalty()
        {
            queuedEnemyPenaltyNights = Mathf.Clamp(queuedEnemyPenaltyNights + 1, 0, 12);
            queuedCarryoverPenaltyStacks = Mathf.Clamp(queuedCarryoverPenaltyStacks + 1, 0, 12);
        }

        private void ApplyQueuedEnemyPenaltyForCurrentNight()
        {
            if (queuedEnemyPenaltyNights > 0)
            {
                objectivePenaltyActiveForCurrentNight = true;
                queuedEnemyPenaltyNights--;
                RadioTransmissions.Instance?.ShowMessage(
                    "Penalty active: enemies are stronger this night.", 3.6f);
                TriggerObjectivePenaltyFeedback(severe: false);
            }
            else
            {
                objectivePenaltyActiveForCurrentNight = false;
            }
        }

        private static void TriggerObjectivePenaltyFeedback(bool severe)
        {
            float audioVolume = severe ? 0.2f : 0.12f;
            float tensionAmount = severe ? 0.18f : 0.1f;
            float tensionHold = severe ? 2.2f : 1.4f;

            AudioManager.Instance?.PlaySFX("alarm_siren", audioVolume);
            AudioManager.Instance?.SignalCombatPeak(tensionAmount, tensionHold);

            Color flashColor = severe
                ? new Color(0.5f, 0.08f, 0.05f, 0.26f)
                : new Color(0.38f, 0.08f, 0.05f, 0.16f);
            float flashDuration = severe ? 0.28f : 0.2f;
            GameEffects.Instance?.FlashScreen(flashColor, flashDuration);
        }

        private float ConsumeProjectedCarryoverRatio()
        {
            float ratio = GetProjectedCarryoverRatio();
            if (queuedCarryoverPenaltyStacks > 0)
            {
                RadioTransmissions.Instance?.ShowMessage(
                    $"Carryover penalty: {Mathf.RoundToInt(ratio * 100f)}% points retained into next level.", 4.4f);
            }

            queuedCarryoverPenaltyStacks = 0;
            return ratio;
        }

        private void ClearObjectiveMissState()
        {
            missedObjectiveRetriesByNight.Clear();
            repeatCurrentNightOnAdvance = false;
            objectivePenaltyActiveForCurrentNight = false;
            queuedEnemyPenaltyNights = 0;
            queuedCarryoverPenaltyStacks = 0;
        }

        private bool HasInteractiveDawnUI()
        {
            return FindFirstObjectByType<GameUI>() != null;
        }

        private IEnumerator AutoAdvanceFromDawn()
        {
            float remaining = Mathf.Max(1f, dawnAutoAdvanceDelay);

            while (remaining > 0f)
            {
                if (currentState != GameState.DawnPhase)
                {
                    yield break;
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (currentState == GameState.DawnPhase)
            {
                AdvanceToNextNight();
            }

            dawnAdvanceCoroutine = null;
        }
    }
}
