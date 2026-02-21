#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace Deadlight.Editor
{
    public class DeadlightSetupWizard : EditorWindow
    {
        private bool scenesCreated = false;
        private bool prefabsCreated = false;
        private bool scriptableObjectsCreated = false;
        
        [MenuItem("Deadlight/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<DeadlightSetupWizard>("Deadlight Setup");
            window.minSize = new Vector2(400, 500);
        }

        [MenuItem("Deadlight/Quick Setup (All)")]
        public static void QuickSetupAll()
        {
            CreateAllScenes();
            CreateAllPrefabs();
            CreateAllScriptableObjects();
            SetupBuildSettings();
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "Deadlight project has been set up!\n\n" +
                "- Scenes created\n" +
                "- Prefabs created\n" +
                "- ScriptableObjects created\n" +
                "- Build settings configured\n\n" +
                "Open the Game scene to start testing.", "OK");
        }

        private void OnGUI()
        {
            GUILayout.Label("Deadlight: Survival After Dark", EditorStyles.boldLabel);
            GUILayout.Label("Project Setup Wizard", EditorStyles.miniLabel);
            GUILayout.Space(20);

            EditorGUILayout.HelpBox(
                "This wizard will create all necessary scenes, prefabs, and data for your game. " +
                "Click 'Setup All' for quick setup, or use individual buttons below.", 
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Setup All (Recommended)", GUILayout.Height(40)))
            {
                QuickSetupAll();
            }

            GUILayout.Space(20);
            GUILayout.Label("Individual Setup Steps:", EditorStyles.boldLabel);

            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Create Scenes"))
            {
                CreateAllScenes();
                scenesCreated = true;
            }
            GUILayout.Label(scenesCreated ? "✓" : "", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("2. Create Prefabs"))
            {
                CreateAllPrefabs();
                prefabsCreated = true;
            }
            GUILayout.Label(prefabsCreated ? "✓" : "", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("3. Create ScriptableObjects"))
            {
                CreateAllScriptableObjects();
                scriptableObjectsCreated = true;
            }
            GUILayout.Label(scriptableObjectsCreated ? "✓" : "", GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("4. Setup Build Settings"))
            {
                SetupBuildSettings();
            }

            GUILayout.Space(20);
            GUILayout.Label("Open Scenes:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open MainMenu"))
            {
                OpenScene("MainMenu");
            }
            if (GUILayout.Button("Open Game"))
            {
                OpenScene("Game");
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20);
            
            if (GUILayout.Button("Setup Game Scene Objects"))
            {
                SetupGameSceneObjects();
            }
        }

        private static void OpenScene(string sceneName)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            if (File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Scene Not Found", 
                    $"Scene '{sceneName}' not found. Please create scenes first.", "OK");
            }
        }

        public static void CreateAllScenes()
        {
            CreateScene("MainMenu");
            CreateScene("Game");
            CreateScene("GameOver");
            AssetDatabase.Refresh();
            Debug.Log("[DeadlightSetup] Scenes created!");
        }

        private static void CreateScene(string sceneName)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            
            if (File.Exists(path))
            {
                Debug.Log($"[DeadlightSetup] Scene {sceneName} already exists, skipping.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            EnsureDirectoryExists("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            
            Debug.Log($"[DeadlightSetup] Created scene: {path}");
        }

        public static void CreateAllPrefabs()
        {
            CreatePlayerPrefab();
            CreateBulletPrefab();
            CreateZombiePrefab();
            CreatePickupPrefabs();
            AssetDatabase.Refresh();
            Debug.Log("[DeadlightSetup] Prefabs created!");
        }

        private static void CreatePlayerPrefab()
        {
            string path = "Assets/Prefabs/Player/Player.prefab";
            if (File.Exists(path)) return;

            EnsureDirectoryExists("Assets/Prefabs/Player");

            var playerObj = new GameObject("Player");
            
            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite(Color.green, "PlayerSprite");
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = playerObj.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            playerObj.AddComponent<Player.PlayerController>();
            playerObj.AddComponent<Player.PlayerShooting>();
            playerObj.AddComponent<Player.PlayerHealth>();
            playerObj.AddComponent<AudioSource>();

            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(playerObj.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);

            playerObj.tag = "Player";

            PrefabUtility.SaveAsPrefabAsset(playerObj, path);
            DestroyImmediate(playerObj);
            
            Debug.Log($"[DeadlightSetup] Created prefab: {path}");
        }

        private static void CreateBulletPrefab()
        {
            string path = "Assets/Prefabs/Weapons/Bullet.prefab";
            if (File.Exists(path)) return;

            EnsureDirectoryExists("Assets/Prefabs/Weapons");

            var bulletObj = new GameObject("Bullet");
            
            var sr = bulletObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite(Color.yellow, "BulletSprite");
            sr.sortingOrder = 5;
            bulletObj.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            var rb = bulletObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = bulletObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            bulletObj.AddComponent<Player.Bullet>();

            PrefabUtility.SaveAsPrefabAsset(bulletObj, path);
            DestroyImmediate(bulletObj);
            
            Debug.Log($"[DeadlightSetup] Created prefab: {path}");
        }

        private static void CreateZombiePrefab()
        {
            string path = "Assets/Prefabs/Enemies/BasicZombie.prefab";
            if (File.Exists(path)) return;

            EnsureDirectoryExists("Assets/Prefabs/Enemies");

            var zombieObj = new GameObject("BasicZombie");
            
            var sr = zombieObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite(new Color(0.4f, 0.6f, 0.4f), "ZombieSprite");
            sr.sortingOrder = 9;

            var rb = zombieObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = zombieObj.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            zombieObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            zombieObj.AddComponent<Enemy.EnemyAI>();
            zombieObj.AddComponent<Enemy.EnemyHealth>();

            zombieObj.tag = "Enemy";

            PrefabUtility.SaveAsPrefabAsset(zombieObj, path);
            DestroyImmediate(zombieObj);
            
            Debug.Log($"[DeadlightSetup] Created prefab: {path}");
        }

        private static void CreatePickupPrefabs()
        {
            CreatePickupPrefab("HealthPickup", Color.red, Systems.PickupType.Health, 25);
            CreatePickupPrefab("AmmoPickup", Color.yellow, Systems.PickupType.Ammo, 30);
            CreatePickupPrefab("ScrapPickup", Color.gray, Systems.PickupType.Scrap, 5);
        }

        private static void CreatePickupPrefab(string name, Color color, Systems.PickupType type, int amount)
        {
            string path = $"Assets/Prefabs/Pickups/{name}.prefab";
            if (File.Exists(path)) return;

            EnsureDirectoryExists("Assets/Prefabs/Pickups");

            var pickupObj = new GameObject(name);
            
            var sr = pickupObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePlaceholderSprite(color, $"{name}Sprite");
            sr.sortingOrder = 3;
            pickupObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

            var col = pickupObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            var pickup = pickupObj.AddComponent<Systems.Pickup>();
            pickup.SetPickupType(type);
            pickup.SetAmount(amount);

            PrefabUtility.SaveAsPrefabAsset(pickupObj, path);
            DestroyImmediate(pickupObj);
        }

        private static Sprite CreatePlaceholderSprite(Color color, string name)
        {
            string spritePath = $"Assets/Art/Sprites/{name}.png";
            
            if (File.Exists(spritePath))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            }

            EnsureDirectoryExists("Assets/Art/Sprites");

            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                    pixels[y * 32 + x] = dist < 14 ? color : Color.clear;
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();

            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(spritePath, pngData);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        public static void CreateAllScriptableObjects()
        {
            CreateDifficultySettings();
            CreateWeaponData();
            CreateEnemyData();
            CreateNightConfigs();
            AssetDatabase.Refresh();
            Debug.Log("[DeadlightSetup] ScriptableObjects created!");
        }

        private static void CreateDifficultySettings()
        {
            EnsureDirectoryExists("Assets/ScriptableObjects/Difficulty");

            CreateAssetIfNotExists("Assets/ScriptableObjects/Difficulty/EasySettings.asset",
                Core.DifficultySettings.CreateEasySettings);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Difficulty/NormalSettings.asset",
                Core.DifficultySettings.CreateNormalSettings);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Difficulty/HardSettings.asset",
                Core.DifficultySettings.CreateHardSettings);
        }

        private static void CreateWeaponData()
        {
            EnsureDirectoryExists("Assets/ScriptableObjects/Weapons");

            CreateAssetIfNotExists("Assets/ScriptableObjects/Weapons/Pistol.asset",
                Data.WeaponData.CreatePistol);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Weapons/Shotgun.asset",
                Data.WeaponData.CreateShotgun);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Weapons/AssaultRifle.asset",
                Data.WeaponData.CreateAssaultRifle);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Weapons/SMG.asset",
                Data.WeaponData.CreateSMG);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Weapons/GrenadeLauncher.asset",
                Data.WeaponData.CreateGrenadeLauncher);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Weapons/Flamethrower.asset",
                Data.WeaponData.CreateFlamethrower);
        }

        private static void CreateEnemyData()
        {
            EnsureDirectoryExists("Assets/ScriptableObjects/Enemies");

            CreateAssetIfNotExists("Assets/ScriptableObjects/Enemies/BasicZombie.asset",
                Data.EnemyData.CreateBasicZombie);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Enemies/Runner.asset",
                Data.EnemyData.CreateRunner);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Enemies/Tank.asset",
                Data.EnemyData.CreateTank);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Enemies/Exploder.asset",
                Data.EnemyData.CreateExploder);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Enemies/Boss.asset",
                Data.EnemyData.CreateBoss);
        }

        private static void CreateNightConfigs()
        {
            EnsureDirectoryExists("Assets/ScriptableObjects/Nights");

            CreateAssetIfNotExists("Assets/ScriptableObjects/Nights/Night_1.asset",
                Core.NightConfig.CreateNight1);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Nights/Night_2.asset",
                Core.NightConfig.CreateNight2);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Nights/Night_3.asset",
                Core.NightConfig.CreateNight3);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Nights/Night_4.asset",
                Core.NightConfig.CreateNight4);
            CreateAssetIfNotExists("Assets/ScriptableObjects/Nights/Night_5.asset",
                Core.NightConfig.CreateNight5);
        }

        private static void CreateAssetIfNotExists<T>(string path, System.Func<T> creator) where T : ScriptableObject
        {
            if (File.Exists(path)) return;

            T asset = creator();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[DeadlightSetup] Created asset: {path}");
        }

        public static void SetupBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameOver.unity", true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[DeadlightSetup] Build settings configured!");
        }

        public static void SetupGameSceneObjects()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                EditorUtility.DisplayDialog("Wrong Scene", 
                    "Please open the Game scene first.", "OK");
                return;
            }

            CreateManagersObject();
            CreateSpawnPointsObject();
            SetupCamera();
            CreateUICanvas();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[DeadlightSetup] Game scene objects created!");
        }

        private static void CreateManagersObject()
        {
            if (GameObject.Find("Managers") != null) return;

            var managers = new GameObject("Managers");

            var gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(managers.transform);
            gmObj.AddComponent<Core.GameManager>();

            var dncObj = new GameObject("DayNightCycle");
            dncObj.transform.SetParent(managers.transform);
            dncObj.AddComponent<Core.DayNightCycle>();

            var wmObj = new GameObject("WaveManager");
            wmObj.transform.SetParent(managers.transform);
            wmObj.AddComponent<Core.WaveManager>();

            var rmObj = new GameObject("ResourceManager");
            rmObj.transform.SetParent(managers.transform);
            rmObj.AddComponent<Systems.ResourceManager>();

            var psObj = new GameObject("PointsSystem");
            psObj.transform.SetParent(managers.transform);
            psObj.AddComponent<Systems.PointsSystem>();

            var pmObj = new GameObject("ProgressionManager");
            pmObj.transform.SetParent(managers.transform);
            pmObj.AddComponent<Systems.ProgressionManager>();

            var amObj = new GameObject("AudioManager");
            amObj.transform.SetParent(managers.transform);
            amObj.AddComponent<Core.AudioManager>();
        }

        private static void CreateSpawnPointsObject()
        {
            if (GameObject.Find("SpawnPoints") != null) return;

            var spawnPoints = new GameObject("SpawnPoints");

            Vector2[] positions = new Vector2[]
            {
                new Vector2(-12, 0), new Vector2(12, 0),
                new Vector2(0, -8), new Vector2(0, 8),
                new Vector2(-10, -6), new Vector2(10, -6),
                new Vector2(-10, 6), new Vector2(10, 6)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var spawnObj = new GameObject($"SpawnPoint_{i + 1}");
                spawnObj.transform.SetParent(spawnPoints.transform);
                spawnObj.transform.position = new Vector3(positions[i].x, positions[i].y, 0);
                spawnObj.AddComponent<Enemy.EnemySpawner>();
            }
        }

        private static void SetupCamera()
        {
            var mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<Core.CameraController>() == null)
            {
                mainCam.gameObject.AddComponent<Core.CameraController>();
                mainCam.orthographicSize = 8;
                mainCam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }
        }

        private static void CreateUICanvas()
        {
            if (GameObject.Find("GameCanvas") != null) return;

            var canvasObj = new GameObject("GameCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var hudObj = new GameObject("HUD");
            hudObj.transform.SetParent(canvasObj.transform);
            hudObj.AddComponent<UI.HUDManager>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif
