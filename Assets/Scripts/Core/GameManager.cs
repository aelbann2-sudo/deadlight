using System;
using System.Collections;
using Deadlight.Data;
using Deadlight.Enemy;
using Deadlight.Level;
using Deadlight.Narrative;
using Deadlight.Player;
using Deadlight.Systems;
using Deadlight.UI;
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
        GameOver,
        Victory
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum MapType
    {
        TownCenter,
        Industrial,
        Suburban
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private DifficultySettings easySettings;
        [SerializeField] private DifficultySettings normalSettings;
        [SerializeField] private DifficultySettings hardSettings;

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        [SerializeField] private Difficulty currentDifficulty = Difficulty.Normal;
        [SerializeField] private MapType selectedMap = MapType.TownCenter;
        [SerializeField] private int currentNight = 1;
        [SerializeField] private int maxNights = 5;
        [SerializeField] private bool isPaused;

        [Header("Runtime Fallback")]
        [SerializeField] private bool autoBootstrapGameScene = true;
        [SerializeField] private bool autoStartWhenGameSceneLoads = false;
        [SerializeField] private float dawnAutoAdvanceDelay = 2f;
        [SerializeField] private float[] dayDurationsByNight = { 70f, 60f, 55f, 50f, 45f };
        [SerializeField] private float targetNightDuration = 120f;

        public GameState CurrentState => currentState;
        public Difficulty CurrentDifficulty => currentDifficulty;
        public MapType SelectedMap => selectedMap;
        public int CurrentNight => currentNight;
        public int MaxNights => maxNights;
        public DifficultySettings CurrentSettings => GetDifficultySettings();
        public bool IsPaused => isPaused;
        public float RunStartTime { get; private set; }

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnNightChanged;
        public event Action<bool> OnPauseChanged;

        private GameObject runtimeBulletPrefab;
        private Material runtimeSpriteMaterial;
        private Coroutine dawnAdvanceCoroutine;
        private bool isBootstrappingScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntimeGameManager()
        {
            if (Instance != null)
            {
                return;
            }

            var existing = FindObjectOfType<GameManager>();
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
            EnsureDifficultySettings();
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
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == "MainMenu" && IsMainMenuSceneEmpty())
            {
                Debug.LogWarning("[GameManager] MainMenu scene is empty on startup. Loading Game scene fallback.");
                SceneManager.LoadScene("Game");
                return;
            }

            if (sceneName == "Game")
            {
                StartCoroutine(BootstrapGameSceneNextFrame());
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) &&
                (currentState == GameState.DayPhase || currentState == GameState.NightPhase))
            {
                TogglePause();
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "MainMenu" && IsMainMenuSceneEmpty())
            {
                Debug.LogWarning("[GameManager] MainMenu scene has no UI. Loading Game scene directly.");
                SceneManager.LoadScene("Game");
                return;
            }

            if (scene.name == "Game")
            {
                StartCoroutine(BootstrapGameSceneNextFrame());
            }
        }

        private IEnumerator BootstrapGameSceneNextFrame()
        {
            if (isBootstrappingScene)
            {
                yield break;
            }

            isBootstrappingScene = true;
            yield return null;

            if (autoBootstrapGameScene)
            {
                EnsureCoreManagers();
                EnsureWaveManagerConfigured();

                var player = EnsurePlayerExists();
                ConfigurePlayer(player);
                EnsureCameraTargetsPlayer(player);
            }

            var intro = FindObjectOfType<IntroSequence>();
            if (autoStartWhenGameSceneLoads && currentState == GameState.MainMenu && intro == null)
            {
                StartNewGame();
            }

            isBootstrappingScene = false;
        }

        public DifficultySettings GetDifficultySettings()
        {
            return currentDifficulty switch
            {
                Difficulty.Easy => easySettings,
                Difficulty.Normal => normalSettings,
                Difficulty.Hard => hardSettings,
                _ => normalSettings
            };
        }

        public void SetDifficulty(Difficulty difficulty)
        {
            currentDifficulty = difficulty;
        }

        public void SetMap(MapType map)
        {
            selectedMap = map;
        }

        public void StartNewGame()
        {
            EnsureDifficultySettings();
            EnsureCoreManagers();

            currentNight = 1;
            RunStartTime = Time.realtimeSinceStartup;
            ResetRunState();

            var player = EnsurePlayerExists();
            ConfigurePlayer(player);
            EnsureCameraTargetsPlayer(player);

            var shooting = player != null ? player.GetComponent<PlayerShooting>() : null;
            if (shooting != null)
            {
                shooting.ResetLoadout(WeaponData.CreatePistol());
            }

            var modifierSystem = FindObjectOfType<RunModifierSystem>();
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

            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.ResetPlayedDialogues();
                NarrativeManager.Instance.TriggerDialogue(DialogueTriggerType.GameStart, currentNight);
            }

            if (currentState == GameState.DayPhase)
            {
                currentState = GameState.MainMenu;
            }

            SetPaused(false);
            ApplyPhaseDurationsForNight(currentNight);
            ChangeState(GameState.DayPhase);
            OnNightChanged?.Invoke(currentNight);
        }

        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;

            if (newState != GameState.DayPhase && newState != GameState.NightPhase)
            {
                SetPaused(false);
            }

            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        public void StartNightPhase()
        {
            ChangeState(GameState.NightPhase);

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null && !waveManager.IsSpawning && waveManager.EnemiesRemaining <= 0)
            {
                waveManager.StartNightWaves();
            }
        }

        public void OnNightSurvived()
        {
            if (currentNight >= maxNights)
            {
                ChangeState(GameState.Victory);
                return;
            }

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

        public void AdvanceToNextNight()
        {
            if (currentState == GameState.Victory || currentState == GameState.GameOver)
            {
                return;
            }

            currentNight = Mathf.Min(currentNight + 1, maxNights);
            ApplyPhaseDurationsForNight(currentNight);
            OnNightChanged?.Invoke(currentNight);
            ChangeState(GameState.DayPhase);
        }

        public void OnPlayerDeath()
        {
            ChangeState(GameState.GameOver);
        }

        public void ReturnToMainMenu()
        {
            currentNight = 1;
            SetPaused(false);
            currentState = GameState.MainMenu;
            SceneManager.LoadScene("MainMenu");
        }

        public void RestartGame()
        {
            currentNight = 1;
            SetPaused(false);
            currentState = GameState.MainMenu;
            SceneManager.LoadScene("Game");
        }

        public void LoadGameScene()
        {
            SceneManager.LoadScene("Game");
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
            SetPaused(!isPaused);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;

            Time.timeScale = isPaused ? 0f : 1f;

            var dayNight = FindObjectOfType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.SetPaused(isPaused);
            }

            OnPauseChanged?.Invoke(isPaused);
        }

        private void EnsureDifficultySettings()
        {
            if (easySettings == null)
            {
                easySettings = DifficultySettings.CreateEasySettings();
            }

            if (normalSettings == null)
            {
                normalSettings = DifficultySettings.CreateNormalSettings();
            }

            if (hardSettings == null)
            {
                hardSettings = DifficultySettings.CreateHardSettings();
            }
        }

        private void EnsureCoreManagers()
        {
            if (FindObjectOfType<DayNightCycle>() == null)
            {
                new GameObject("DayNightCycle").AddComponent<DayNightCycle>();
            }

            if (FindObjectOfType<WaveManager>() == null)
            {
                new GameObject("WaveManager").AddComponent<WaveManager>();
            }

            if (FindObjectOfType<ResourceManager>() == null)
            {
                new GameObject("ResourceManager").AddComponent<ResourceManager>();
            }

            if (FindObjectOfType<PointsSystem>() == null)
            {
                new GameObject("PointsSystem").AddComponent<PointsSystem>();
            }

            if (FindObjectOfType<ProgressionManager>() == null)
            {
                new GameObject("ProgressionManager").AddComponent<ProgressionManager>();
            }

            if (FindObjectOfType<DayObjectiveSystem>() == null)
            {
                new GameObject("DayObjectiveSystem").AddComponent<DayObjectiveSystem>();
            }

            if (FindObjectOfType<RunModifierSystem>() == null)
            {
                new GameObject("RunModifierSystem").AddComponent<RunModifierSystem>();
            }

            if (FindObjectOfType<CosmeticUnlockSystem>() == null)
            {
                new GameObject("CosmeticUnlockSystem").AddComponent<CosmeticUnlockSystem>();
            }

            if (FindObjectOfType<AudioManager>() == null)
            {
                new GameObject("AudioManager").AddComponent<AudioManager>();
            }

            if (FindObjectOfType<EndingSequence>() == null)
            {
                new GameObject("EndingSequence").AddComponent<EndingSequence>();
            }

            if (FindObjectOfType<StoryEventManager>() == null)
            {
                new GameObject("StoryEventManager").AddComponent<StoryEventManager>();
            }

            if (FindObjectOfType<RadioTransmissions>() == null)
            {
                new GameObject("RadioTransmissions").AddComponent<RadioTransmissions>();
            }

            if (FindObjectOfType<LeaderboardManager>() == null)
            {
                new GameObject("LeaderboardManager").AddComponent<LeaderboardManager>();
            }
        }

        private GameObject EnsurePlayerExists()
        {
            var playerControllers = FindObjectsOfType<PlayerController>();
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
                spriteRenderer.sprite = CreateRuntimeCircleSprite(new Color(0.2f, 0.8f, 0.3f), 32, 14f);
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
        }

        private void EnsureCameraTargetsPlayer(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            var cameraController = FindObjectOfType<CameraController>();
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
            var waveManager = FindObjectOfType<WaveManager>();
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
            sr.sprite = CreateRuntimeCircleSprite(new Color(1f, 0.9f, 0.2f), 16, 6f);
            sr.sortingOrder = 8;

            var rb = runtimeBulletPrefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = runtimeBulletPrefab.AddComponent<CircleCollider2D>();
            col.radius = 0.12f;
            col.isTrigger = true;

            runtimeBulletPrefab.AddComponent<Bullet>();

            return runtimeBulletPrefab;
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

        private void ResetRunState()
        {
            if (dawnAdvanceCoroutine != null)
            {
                StopCoroutine(dawnAdvanceCoroutine);
                dawnAdvanceCoroutine = null;
            }

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

            var objectiveSystem = FindObjectOfType<DayObjectiveSystem>();
            objectiveSystem?.ResetObjective();

            foreach (var enemy in FindObjectsOfType<EnemyHealth>())
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            foreach (var pickup in FindObjectsOfType<Pickup>())
            {
                if (pickup != null)
                {
                    Destroy(pickup.gameObject);
                }
            }

            foreach (var pickupItem in FindObjectsOfType<PickupItem>())
            {
                if (pickupItem != null)
                {
                    Destroy(pickupItem.gameObject);
                }
            }

            foreach (var bullet in FindObjectsOfType<Bullet>())
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
            }
        }

        private bool HasInteractiveDawnUI()
        {
            return FindObjectOfType<ShopUI>() != null || FindObjectOfType<GameUI>() != null;
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

        private static bool IsMainMenuSceneEmpty()
        {
            return FindObjectOfType<MenuManager>() == null && FindObjectOfType<Canvas>() == null;
        }

        private void ApplyPhaseDurationsForNight(int night)
        {
            var dayNight = FindObjectOfType<DayNightCycle>();
            if (dayNight == null)
            {
                return;
            }

            int idx = Mathf.Clamp(night - 1, 0, Mathf.Max(0, dayDurationsByNight.Length - 1));
            float dayDuration = dayDurationsByNight.Length > 0 ? dayDurationsByNight[idx] : 60f;
            dayNight.SetDayDuration(dayDuration);
            dayNight.SetNightDuration(targetNightDuration);
        }
    }
}
