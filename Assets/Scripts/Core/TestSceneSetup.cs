using UnityEngine;
using UnityEngine.UI;
using Deadlight.Enemy;
using Deadlight.Audio;
using Deadlight.Data;
using Deadlight.Level;
using Deadlight.Level.MapBuilders;
using Deadlight.Narrative;
using Deadlight.Visuals;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Deadlight.Core
{
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool useProceduralSprites = true;
        
        private Sprite[] playerSprites;
        private Sprite[] npcSprites;
        private MapConfig activeMapConfig;
        
#if UNITY_EDITOR
        [MenuItem("Deadlight/Create Test Scene and Play")]
        public static void CreateTestSceneAndPlay()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot create test scene while in play mode. Stop playing first.");
                return;
            }
            
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            var setupObj = new GameObject("TestSceneSetup");
            setupObj.AddComponent<TestSceneSetup>();
            
            EditorApplication.isPlaying = true;
        }
        
        [MenuItem("Deadlight/Create Test Scene (No Play)")]
        public static void CreateTestSceneNoPlay()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Cannot create test scene while in play mode. Stop playing first.");
                return;
            }
            
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            var setupObj = new GameObject("TestSceneSetup");
            var setup = setupObj.AddComponent<TestSceneSetup>();
            setup.setupOnStart = false;
            setup.SetupTestScene();
        }
#endif
        
        private void Start()
        {
            if (setupOnStart)
            {
                SetupTestScene();
            }
        }

        [ContextMenu("Setup Test Scene")]
        public void SetupTestScene()
        {
            MapType selectedMap = GameManager.Instance != null
                ? GameManager.Instance.SelectedMap
                : MapType.TownCenter;
            activeMapConfig = MapConfig.GetConfigForType(selectedMap);

            LoadAllSprites();
            CreateManagers();
            CreateCamera();

            if (!ShouldBuildGameplayScene())
            {
                return;
            }

            CreateGround();
            CreatePlayer();
            CreateEnvironment();
            CreateEnemies();
            BuildHUD();
        }

        public void RebuildMap(MapType mapType, bool repositionPlayer = true)
        {
            activeMapConfig = MapConfig.GetConfigForType(mapType);

            RemoveRuntimeObject("Ground");
            RemoveRuntimeObject("Environment");
            RemoveRuntimeObject("Enemies");

            CreateGround();
            CreateEnvironment();

            LevelManager.Instance?.RefreshLevelObjects();
            RefreshWaveSpawnPoints();

            if (repositionPlayer)
            {
                RepositionPlayerToSpawn();
            }
        }

        private bool ShouldBuildGameplayScene()
        {
            if (!Application.isPlaying)
            {
                return true;
            }

            return GameManager.Instance == null || GameManager.Instance.ShouldSetupGameplayScene;
        }

        private void LoadAllSprites()
        {
            playerSprites = Resources.LoadAll<Sprite>("Player");
            npcSprites = Resources.LoadAll<Sprite>("NPC");
        }

        private Sprite GetSprite(Sprite[] sprites, string name)
        {
            if (sprites == null) return null;
            foreach (var sprite in sprites)
            {
                if (sprite.name == name) return sprite;
            }
            return null;
        }

        // ===================== CAMERA =====================

        private void CreateCamera()
        {
            if (Camera.main != null) return;
            
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.5f;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.AddComponent<AudioListener>();
            var camCtrl = camObj.AddComponent<CameraController>();
            camCtrl.SetSmoothSpeed(8f);
            camObj.transform.position = new Vector3(0, 0, -10);
        }

        // ===================== GROUND =====================

        private void CreateGround()
        {
            var groundParent = new GameObject("Ground");
            int hw = activeMapConfig != null ? activeMapConfig.halfWidth : 13;
            int hh = activeMapConfig != null ? activeMapConfig.halfHeight : 13;
            Color tint = activeMapConfig != null ? activeMapConfig.groundTint : Color.white;

            var grassSprite = ProceduralSpriteGenerator.CreateGroundTile(0);
            var pathSprite = ProceduralSpriteGenerator.CreateGroundTile(1);
            var concreteSprite = ProceduralSpriteGenerator.CreateGroundTile(2);
            var asphaltSprite = ProceduralSpriteGenerator.CreateGroundTile(3);

            for (int x = -hw; x <= hw; x++)
            {
                for (int y = -hh; y <= hh; y++)
                {
                    var tile = new GameObject($"T_{x}_{y}");
                    tile.transform.SetParent(groundParent.transform);
                    tile.transform.position = new Vector3(x, y, 0);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = -200;

                    int tileType = GetTileType(x, y);
                    sr.sprite = tileType switch
                    {
                        1 => pathSprite,
                        2 => concreteSprite,
                        3 => asphaltSprite,
                        _ => grassSprite
                    };

                    float shade = Random.Range(0.9f, 1.05f);
                    sr.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade);
                }
            }
        }

        private int GetTileType(int x, int y)
        {
            MapType type = activeMapConfig != null ? activeMapConfig.mapType : MapType.TownCenter;
            return type switch
            {
                MapType.TownCenter => GetTileType_TownCenter(x, y),
                MapType.Industrial => GetTileType_Industrial(x, y),
                MapType.Suburban => GetTileType_Suburban(x, y),
                MapType.Research => GetTileType_Research(x, y),
                _ => GetTileType_TownCenter(x, y)
            };
        }

        private int GetTileType_TownCenter(int x, int y)
        {
            return TownCenterLayout.GetTileType(activeMapConfig, x, y);
        }

        private int GetTileType_Industrial(int x, int y)
        {
            return IndustrialLayout.GetTileType(activeMapConfig, x, y);
        }

        private int GetTileType_Suburban(int x, int y)
        {
            return SuburbanLayout.GetTileType(activeMapConfig, x, y);
        }

        private int GetTileType_Research(int x, int y)
        {
            return ResearchLayout.GetTileType(activeMapConfig, x, y);
        }

        // ===================== PLAYER =====================

        private void CreatePlayer()
        {
            if (GameObject.Find("Player") != null) return;
            
            var playerObj = new GameObject("Player");
            playerObj.transform.position = Vector3.zero;

            var sr = playerObj.AddComponent<SpriteRenderer>();
            
            if (useProceduralSprites)
            {
                sr.sprite = ProceduralSpriteGenerator.CreatePlayerSprite(0, 0);
            }
            else
            {
                var playerSprite = GetSprite(playerSprites, "Down 0");
                sr.sprite = playerSprite != null ? playerSprite : CreateCircleSprite(Color.green);
            }
            sr.sortingOrder = 10;

            var rb = playerObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = playerObj.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            playerObj.AddComponent<Player.PlayerController>();
            playerObj.AddComponent<Player.PlayerShooting>();
            playerObj.AddComponent<Player.PlayerHealth>();
            playerObj.AddComponent<Player.PlayerUpgrades>();
            playerObj.AddComponent<Player.PlayerArmor>();
            playerObj.AddComponent<AudioSource>();
            
            var animator = playerObj.AddComponent<PlayerAnimator>();
            animator.SetSprites(playerSprites);
            animator.SetUseProceduralSprites(useProceduralSprites);

            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(playerObj.transform);
            firePoint.transform.localPosition = new Vector3(0.5f, 0, 0);

            playerObj.AddComponent<Narrative.PlayerVoice>();
            playerObj.AddComponent<Player.ThrowableSystem>();

            var shooting = playerObj.GetComponent<Player.PlayerShooting>();
            shooting.SetFirePoint(firePoint.transform);
            CreateBulletPrefab(shooting);

            var shotgun = ScriptableObject.CreateInstance<Data.WeaponData>();
            shotgun.weaponName = "Shotgun";
            shotgun.damage = 12f;
            shotgun.fireRate = 0.7f;
            shotgun.magazineSize = 6;
            shotgun.reloadTime = 2f;
            shotgun.bulletSpeed = 25f;
            shotgun.range = 12f;
            shotgun.spread = 12f;
            shotgun.isAutomatic = false;
            shotgun.pelletsPerShot = 5;
            shooting.SetSecondWeapon(shotgun);
        }

        private void CreateBulletPrefab(Player.PlayerShooting shooting)
        {
            var bulletObj = new GameObject("BulletPrefab");
            bulletObj.SetActive(false);

            var sr = bulletObj.AddComponent<SpriteRenderer>();
            if (useProceduralSprites)
            {
                sr.sprite = ProceduralSpriteGenerator.CreateBulletSprite();
            }
            else
            {
                sr.sprite = CreateBulletSprite();
            }
            sr.sortingOrder = 8;

            var rb = bulletObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = bulletObj.AddComponent<CircleCollider2D>();
            col.radius = 0.1f;
            col.isTrigger = true;

            bulletObj.AddComponent<Player.Bullet>();

            var trail = bulletObj.AddComponent<TrailRenderer>();
            trail.time = 0.08f;
            trail.startWidth = 0.12f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.9f, 0.4f, 0.8f);
            trail.endColor = new Color(1f, 0.5f, 0.2f, 0f);

            shooting.SetBulletPrefab(bulletObj);

            var weaponData = ScriptableObject.CreateInstance<Data.WeaponData>();
            weaponData.weaponName = "Pistol";
            weaponData.damage = 20f;
            weaponData.fireRate = 0.2f;
            weaponData.magazineSize = 15;
            weaponData.reloadTime = 1.2f;
            weaponData.bulletSpeed = 30f;
            weaponData.range = 25f;
            weaponData.spread = 2f;
            weaponData.isAutomatic = false;

            shooting.SetWeapon(weaponData);
        }
        private void CreateManagers()
        {
            Transform managersParent = null;
            
            if (GameManager.Instance != null)
            {
                managersParent = GameManager.Instance.transform.parent;
            }
            else
            {
                var managersObj = new GameObject("Managers");
                managersParent = managersObj.transform;

                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();

                var dncObj = new GameObject("DayNightCycle");
                dncObj.transform.SetParent(managersParent);
                dncObj.AddComponent<DayNightCycle>();

                var wsObj = new GameObject("WaveSpawner");
                wsObj.transform.SetParent(managersParent);
                wsObj.AddComponent<WaveSpawner>();

                var rmObj = new GameObject("ResourceManager");
                rmObj.transform.SetParent(managersParent);
                rmObj.AddComponent<Systems.ResourceManager>();

                bool craftingEnabled = GameManager.Instance == null || GameManager.Instance.CraftingEnabled;
                if (craftingEnabled)
                {
                    var csObj = new GameObject("CraftingSystem");
                    csObj.transform.SetParent(managersParent);
                    csObj.AddComponent<Systems.CraftingSystem>();
                }

                var psObj = new GameObject("PointsSystem");
                psObj.transform.SetParent(managersParent);
                psObj.AddComponent<Systems.PointsSystem>();

                var lmObj = new GameObject("LevelManager");
                lmObj.transform.SetParent(managersParent);
                lmObj.AddComponent<LevelManager>();

                var nmObj = new GameObject("NarrativeManager");
                nmObj.transform.SetParent(managersParent);
                nmObj.AddComponent<NarrativeManager>();
                nmObj.AddComponent<EnvironmentalLore>();

                var geObj = new GameObject("GameEffects");
                geObj.transform.SetParent(managersParent);
                geObj.AddComponent<GameEffects>();

                var gfObj = new GameObject("GameFlowController");
                gfObj.transform.SetParent(managersParent);
                gfObj.AddComponent<GameFlowController>();

                var rtObj = new GameObject("RadioTransmissions");
                rtObj.transform.SetParent(managersParent);
                rtObj.AddComponent<RadioTransmissions>();

                var amObj = new GameObject("AudioManager");
                amObj.AddComponent<AudioManager>();

                var vfxObj = new GameObject("VFXManager");
                vfxObj.transform.SetParent(managersParent);
                vfxObj.AddComponent<VFXManager>();

                var atmObj = new GameObject("AtmosphereController");
                atmObj.transform.SetParent(managersParent);
                atmObj.AddComponent<AtmosphereController>();

                var decalObj = new GameObject("DecalManager");
                decalObj.transform.SetParent(managersParent);
                decalObj.AddComponent<DecalManager>();

                var endingObj = new GameObject("EndingSequence");
                endingObj.transform.SetParent(managersParent);
                endingObj.AddComponent<Narrative.EndingSequence>();

                var storyObj = new GameObject("StoryEventManager");
                storyObj.transform.SetParent(managersParent);
                storyObj.AddComponent<Narrative.StoryEventManager>();
                
                var pickupObj = new GameObject("PickupSpawner");
                pickupObj.transform.SetParent(managersParent);
                pickupObj.AddComponent<Systems.PickupSpawner>();
                
                var powerupObj = new GameObject("PowerupSystem");
                powerupObj.transform.SetParent(managersParent);
                powerupObj.AddComponent<Systems.PowerupSystem>();
                
                var floatTextObj = new GameObject("FloatingTextManager");
                floatTextObj.transform.SetParent(managersParent);
                floatTextObj.AddComponent<Systems.FloatingTextManager>();
                
                var killStreakObj = new GameObject("KillStreakSystem");
                killStreakObj.transform.SetParent(managersParent);
                killStreakObj.AddComponent<Systems.KillStreakSystem>();
                
                var introObj = new GameObject("IntroSequence");
                introObj.transform.SetParent(managersParent);
                introObj.AddComponent<Narrative.IntroSequence>();
                
                var corruptionObj = new GameObject("CorruptionSystem");
                corruptionObj.transform.SetParent(managersParent);
                corruptionObj.AddComponent<Systems.CorruptionSystem>();
                
                var storyObjObj = new GameObject("StoryObjective");
                storyObjObj.transform.SetParent(managersParent);
                storyObjObj.AddComponent<Narrative.StoryObjective>();
            }

            if (VFXManager.Instance == null)
            {
                var vfxObj = new GameObject("VFXManager");
                if (managersParent != null)
                    vfxObj.transform.SetParent(managersParent);
                vfxObj.AddComponent<VFXManager>();
            }

            if (NarrativeManager.Instance == null)
            {
                var narrativeObj = new GameObject("NarrativeManager");
                if (managersParent != null)
                    narrativeObj.transform.SetParent(managersParent);
                narrativeObj.AddComponent<NarrativeManager>();
                narrativeObj.AddComponent<EnvironmentalLore>();
            }
            else if (EnvironmentalLore.Instance == null)
            {
                NarrativeManager.Instance.gameObject.AddComponent<EnvironmentalLore>();
            }

            if (RadioTransmissions.Instance == null)
            {
                var rtObj = new GameObject("RadioTransmissions");
                if (managersParent != null)
                    rtObj.transform.SetParent(managersParent);
                rtObj.AddComponent<RadioTransmissions>();
            }

            if (Narrative.EndingSequence.Instance == null)
            {
                var endingObj = new GameObject("EndingSequence");
                if (managersParent != null)
                    endingObj.transform.SetParent(managersParent);
                endingObj.AddComponent<Narrative.EndingSequence>();
            }

            if (Narrative.StoryEventManager.Instance == null)
            {
                var storyEventObj = new GameObject("StoryEventManager");
                if (managersParent != null)
                    storyEventObj.transform.SetParent(managersParent);
                storyEventObj.AddComponent<Narrative.StoryEventManager>();
            }

            if (Narrative.StoryObjective.Instance == null)
            {
                var objectiveObj = new GameObject("StoryObjective");
                if (managersParent != null)
                    objectiveObj.transform.SetParent(managersParent);
                objectiveObj.AddComponent<Narrative.StoryObjective>();
            }

            if (FindFirstObjectByType<Narrative.NarrativeJournalUI>() == null)
            {
                var journalObj = new GameObject("NarrativeJournalUI");
                if (managersParent != null)
                    journalObj.transform.SetParent(managersParent);
                journalObj.AddComponent<Narrative.NarrativeJournalUI>();
            }

            if (FindFirstObjectByType<Narrative.IntroSequence>() == null &&
                (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.MainMenu))
            {
                var introObj = new GameObject("IntroSequence");
                if (managersParent != null)
                    introObj.transform.SetParent(managersParent);
                introObj.AddComponent<Narrative.IntroSequence>();
            }

            if (AtmosphereController.Instance == null)
            {
                var atmObj = new GameObject("AtmosphereController");
                if (managersParent != null)
                    atmObj.transform.SetParent(managersParent);
                atmObj.AddComponent<AtmosphereController>();
            }

            if (DecalManager.Instance == null)
            {
                var decalObj = new GameObject("DecalManager");
                if (managersParent != null)
                    decalObj.transform.SetParent(managersParent);
                decalObj.AddComponent<DecalManager>();
            }

            if (Deadlight.UI.GameUI.Instance == null)
            {
                var guiObj = new GameObject("GameUI");
                guiObj.AddComponent<Deadlight.UI.GameUI>();
                Debug.Log("[TestSceneSetup] Created GameUI");
            }
        }

        // ===================== ENVIRONMENT =====================

        private void CreateEnvironment()
        {
            var envParent = new GameObject("Environment");
            MapBuilderBase builder = CreateMapEnvironment(envParent.transform);
            CreatePerimeter(envParent.transform);
            CreateLandmarks(envParent.transform, builder);
            NarrativeWorldBuilder.PopulateWorld(activeMapConfig, envParent.transform);
        }

        private MapBuilderBase CreateMapEnvironment(Transform parent)
        {
            MapBuilderBase builder = CreateMapBuilder();
            if (builder == null)
            {
                Debug.LogWarning("[TestSceneSetup] No map builder found for current map type.");
                return null;
            }

            builder.Build(parent, activeMapConfig, GetTileType);
            return builder;
        }

        private void CreateLandmarks(Transform parent, MapBuilderBase builder)
        {
            builder?.BuildLandmarks(parent);
        }

        private MapBuilderBase CreateMapBuilder()
        {
            MapType type = activeMapConfig != null ? activeMapConfig.mapType : MapType.TownCenter;
            return type switch
            {
                MapType.Industrial => new IndustrialBuilder(),
                MapType.Suburban => new SuburbanBuilder(),
                MapType.Research => new ResearchBuilder(),
                _ => new TownCenterBuilder()
            };
        }

        private void RemoveRuntimeObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing == null)
            {
                return;
            }

            existing.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(existing);
            }
            else
            {
                DestroyImmediate(existing);
            }
        }

        private void RefreshWaveSpawnPoints()
        {
            var waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager == null)
            {
                return;
            }

            waveManager.ClearSpawnPoints();

            var spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                if (spawner != null)
                {
                    waveManager.AddSpawnPoint(spawner.transform);
                }
            }
        }

        private void RepositionPlayerToSpawn()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return;
            }

            Vector3 spawnPosition = LevelManager.Instance != null && LevelManager.Instance.PlayerSpawnPoint != null
                ? LevelManager.Instance.PlayerSpawnPoint.position
                : Vector3.zero;

            player.transform.position = spawnPosition;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void SpawnLorePickups(Transform parent)
        {
            var loreParent = new GameObject("LorePickups");
            loreParent.transform.SetParent(parent);

            string[] loreIds = { "lab_note_1", "chen_1", "chen_2", "journal_1", "military_1", "chen_3", "facility_1", "survivor_log" };
            Vector3[] positions = activeMapConfig != null && activeMapConfig.lorePositions != null
                ? activeMapConfig.lorePositions
                : new[] {
                    new Vector3(-5, 9, 0), new Vector3(7, 9, 0),
                    new Vector3(-9, -3, 0), new Vector3(9, -3, 0),
                    new Vector3(-3, -9, 0), new Vector3(3, -9, 0),
                    new Vector3(-7, 0, 0), new Vector3(7, 0, 0)
                };

            int count = Mathf.Min(loreIds.Length, positions.Length);
            for (int i = 0; i < count; i++)
            {
                var loreObj = new GameObject($"Lore_{loreIds[i]}");
                loreObj.transform.SetParent(loreParent.transform);
                loreObj.transform.position = positions[i];

                var sr = loreObj.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreatePickupSprite("lore", i);
                sr.sortingOrder = 5;

                var pickup = loreObj.AddComponent<LorePickup>();
                pickup.SetLoreId(loreIds[i]);

                var glow = new GameObject("Glow");
                glow.transform.SetParent(loreObj.transform);
                glow.transform.localPosition = Vector3.zero;
                var glowSr = glow.AddComponent<SpriteRenderer>();
                var glowTex = new Texture2D(16, 16);
                var glowPx = new Color[256];
                Vector2 c = new Vector2(8, 8);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), c) / 8f;
                        glowPx[y * 16 + x] = d < 1f ? new Color(1f, 0.8f, 0.3f, 0.4f * (1f - d)) : Color.clear;
                    }
                glowTex.SetPixels(glowPx);
                glowTex.Apply();
                glowTex.filterMode = FilterMode.Bilinear;
                glowSr.sprite = Sprite.Create(glowTex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 8f);
                glowSr.sortingOrder = 4;
            }
        }

        private void CreatePerimeter(Transform parent)
        {
            var perimeter = new GameObject("Perimeter");
            perimeter.transform.SetParent(parent);
            
            int halfTilesW = activeMapConfig != null ? activeMapConfig.halfWidth : 12;
            int halfTilesH = activeMapConfig != null ? activeMapConfig.halfHeight : 12;
            float boundaryHalfW = halfTilesW + 0.5f;
            float boundaryHalfH = halfTilesH + 0.5f;
            float thickness = 1f;

            CreateWall(perimeter.transform, new Vector3(0, boundaryHalfH + thickness * 0.5f, 0), new Vector2(boundaryHalfW * 2f, thickness));
            CreateWall(perimeter.transform, new Vector3(0, -boundaryHalfH - thickness * 0.5f, 0), new Vector2(boundaryHalfW * 2f, thickness));
            CreateWall(perimeter.transform, new Vector3(-boundaryHalfW - thickness * 0.5f, 0, 0), new Vector2(thickness, boundaryHalfH * 2f));
            CreateWall(perimeter.transform, new Vector3(boundaryHalfW + thickness * 0.5f, 0, 0), new Vector2(thickness, boundaryHalfH * 2f));
        }

        private void CreateWall(Transform parent, Vector3 position, Vector2 size)
        {
            var wall = new GameObject("BoundaryCollider");
            wall.transform.SetParent(parent);
            wall.transform.position = position;

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        // ===================== ENEMIES =====================

        private void CreateEnemies()
        {
            var enemiesParent = new GameObject("Enemies");
            
            string[] npcVariants = { "TopDown_NPC_0", "TopDown_NPC_1", "TopDown_NPC_2", "TopDown_NPC_3" };

            Vector3[] positions = activeMapConfig != null && activeMapConfig.enemySpawnPositions != null
                ? activeMapConfig.enemySpawnPositions
                : new[] {
                    new Vector3(8, 6, 0),
                    new Vector3(-8, 6, 0),
                    new Vector3(9, -6, 0),
                };

            for (int i = 0; i < positions.Length; i++)
            {
                CreateEnemy(enemiesParent.transform, positions[i], npcVariants[i % npcVariants.Length]);
            }
        }

        private void CreateEnemy(Transform parent, Vector3 position, string spriteName)
        {
            var enemyObj = new GameObject("Zombie");
            enemyObj.transform.SetParent(parent);
            enemyObj.transform.position = position;

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            
            if (useProceduralSprites)
            {
                var zombieTypes = new[] { 
                    ProceduralSpriteGenerator.ZombieType.Basic,
                    ProceduralSpriteGenerator.ZombieType.Runner,
                    ProceduralSpriteGenerator.ZombieType.Exploder
                };
                var randomType = zombieTypes[Random.Range(0, zombieTypes.Length)];
                sr.sprite = ProceduralSpriteGenerator.CreateZombieSprite(randomType, 0, 0);
            }
            else
            {
                var enemySprite = GetSprite(npcSprites, spriteName);
                sr.sprite = enemySprite != null ? enemySprite : CreateCircleSprite(new Color(0.4f, 0.5f, 0.3f));
                sr.color = new Color(0.65f, 0.75f, 0.55f);
            }
            sr.sortingOrder = 9;

            var rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = enemyObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            enemyObj.AddComponent<Enemy.EnemyHealth>();
            enemyObj.AddComponent<Enemy.SimpleEnemyAI>();
            enemyObj.AddComponent<EnemyHealthBar>();
            enemyObj.AddComponent<ZombieAnimator>();
            enemyObj.AddComponent<Audio.ZombieSounds>();
        }

        // ===================== HUD =====================

        private void BuildHUD()
        {
            Canvas canvasComp = FindFirstObjectByType<Canvas>();
            GameObject canvas;

            if (canvasComp != null && canvasComp.GetComponent<Deadlight.UI.GameplayHUD>() != null)
            {
                return;
            }

            if (canvasComp == null)
            {
                canvas = new GameObject("GameCanvas");
                canvasComp = canvas.AddComponent<Canvas>();
                canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasComp.sortingOrder = 100;
                var scaler = canvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvas.AddComponent<GraphicRaycaster>();
            }
            else
            {
                canvas = canvasComp.gameObject;
                canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasComp.sortingOrder = Mathf.Max(canvasComp.sortingOrder, 100);

                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.AddComponent<CanvasScaler>();
                }

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    canvas.AddComponent<GraphicRaycaster>();
                }

            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (font == null)
            {
                string[] fallbacks = Font.GetOSInstalledFontNames();
                if (fallbacks != null && fallbacks.Length > 0)
                    font = Font.CreateDynamicFontFromOSFont(fallbacks[0], 14);
            }

            // Health bar
            var healthPanel = CreateUIPanel(canvas.transform, "HealthPanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -20), new Vector2(250, 30));

            var healthBg = CreateUIImage(healthPanel.transform, "HealthBG",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.15f, 0.15f, 0.85f));
            healthBg.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            healthBg.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var healthFill = CreateUIImage(healthPanel.transform, "HealthFill",
                Vector2.zero, new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero,
                new Color(0.2f, 0.8f, 0.2f, 0.9f));
            var hfRect = healthFill.GetComponent<RectTransform>();
            hfRect.offsetMin = new Vector2(2, 2);
            hfRect.offsetMax = new Vector2(-2, -2);
            hfRect.pivot = new Vector2(0f, 0.5f);
            var hfImage = healthFill.GetComponent<Image>();
            hfImage.type = Image.Type.Simple;

            var healthLabel = CreateUIText(healthPanel.transform, "HealthText",
                new Vector2(0, 0.5f), "100 / 100", font, 14, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0, 0), new Vector2(0, 0));

            var healthIcon = CreateUIText(healthPanel.transform, "HealthIcon",
                new Vector2(0, 0.5f), "+", font, 18, TextAnchor.MiddleCenter, new Color(0.9f, 0.3f, 0.3f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(-20, 0), new Vector2(20, 30));

            // Stamina bar
            var staminaPanel = CreateUIPanel(canvas.transform, "StaminaPanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -55), new Vector2(250, 12));

            var staminaBg = CreateUIImage(staminaPanel.transform, "StaminaBG",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.15f, 0.15f, 0.7f));
            staminaBg.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            staminaBg.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var staminaFill = CreateUIImage(staminaPanel.transform, "StaminaFill",
                Vector2.zero, Vector2.one, new Vector2(0, 0.5f), Vector2.zero,
                new Color(0.2f, 0.6f, 0.9f, 0.8f));
            var sfRect = staminaFill.GetComponent<RectTransform>();
            sfRect.offsetMin = new Vector2(1, 1);
            sfRect.offsetMax = new Vector2(-1, -1);
            var sfImage = staminaFill.GetComponent<Image>();
            sfImage.type = Image.Type.Filled;
            sfImage.fillMethod = Image.FillMethod.Horizontal;

            // Ammo display (bottom right, large CoD Zombies style)
            var ammoText = CreateUIText(canvas.transform, "AmmoText",
                new Vector2(1, 0), "15 / 60", font, 36, TextAnchor.LowerRight, Color.white,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 50), new Vector2(300, 45));
            ammoText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Weapon info panel (bottom right, above ammo)
            var weaponPanel = CreateUIPanel(canvas.transform, "WeaponPanel",
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-20, 100), new Vector2(220, 55));
            var wpBg = weaponPanel.AddComponent<Image>();
            wpBg.color = new Color(0.1f, 0.1f, 0.15f, 0.6f);

            var weaponIconObj = new GameObject("WeaponIcon");
            weaponIconObj.transform.SetParent(weaponPanel.transform, false);
            var wiRect = weaponIconObj.AddComponent<RectTransform>();
            wiRect.anchorMin = new Vector2(0, 0.5f);
            wiRect.anchorMax = new Vector2(0, 0.5f);
            wiRect.pivot = new Vector2(0, 0.5f);
            wiRect.anchoredPosition = new Vector2(8, 0);
            wiRect.sizeDelta = new Vector2(48, 24);
            var weaponIconImage = weaponIconObj.AddComponent<Image>();
            weaponIconImage.preserveAspect = true;
            try { weaponIconImage.sprite = Deadlight.Visuals.ProceduralSpriteGenerator.CreateWeaponIcon(Data.WeaponType.Pistol); }
            catch { }

            var weaponName = CreateUIText(weaponPanel.transform, "WeaponName",
                new Vector2(0, 1), "PISTOL", font, 18, TextAnchor.UpperLeft, new Color(1f, 0.95f, 0.7f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(62, -5), new Vector2(150, 25));
            weaponName.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var weaponStats = CreateUIText(weaponPanel.transform, "WeaponStats",
                new Vector2(0, 0), "DMG: 15  ROF: 0.3", font, 12, TextAnchor.LowerLeft,
                new Color(0.7f, 0.7f, 0.8f, 0.8f),
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(62, 5), new Vector2(150, 18));

            // Armor display (below health bar)
            var armorPanelObj = CreateUIPanel(canvas.transform, "ArmorPanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -72), new Vector2(250, 28));

            var vestBar = CreateUIPanel(armorPanelObj.transform, "VestBar",
                new Vector2(0, 0), new Vector2(0.48f, 1), new Vector2(0, 0.5f),
                Vector2.zero, Vector2.zero);
            vestBar.GetComponent<RectTransform>().offsetMin = new Vector2(0, 2);
            vestBar.GetComponent<RectTransform>().offsetMax = new Vector2(0, -2);
            var vestBg = vestBar.AddComponent<Image>();
            vestBg.color = new Color(0.12f, 0.2f, 0.35f, 0.7f);

            var vestFillObj = new GameObject("VestFill");
            vestFillObj.transform.SetParent(vestBar.transform, false);
            var vfRect = vestFillObj.AddComponent<RectTransform>();
            vfRect.anchorMin = Vector2.zero;
            vfRect.anchorMax = Vector2.one;
            vfRect.offsetMin = new Vector2(1, 1);
            vfRect.offsetMax = new Vector2(-1, -1);
            var vestFillImage = vestFillObj.AddComponent<Image>();
            vestFillImage.color = new Color(0.2f, 0.5f, 0.9f, 0.85f);
            vestFillImage.type = Image.Type.Filled;
            vestFillImage.fillMethod = Image.FillMethod.Horizontal;
            vestFillImage.fillAmount = 0;

            var vestLabelObj = CreateUIText(vestBar.transform, "VestLabel",
                new Vector2(0.5f, 0.5f), "VEST", font, 10, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            vestLabelObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            vestLabelObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var helmBar = CreateUIPanel(armorPanelObj.transform, "HelmBar",
                new Vector2(0.52f, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                Vector2.zero, Vector2.zero);
            helmBar.GetComponent<RectTransform>().offsetMin = new Vector2(0, 2);
            helmBar.GetComponent<RectTransform>().offsetMax = new Vector2(0, -2);
            var helmBg = helmBar.AddComponent<Image>();
            helmBg.color = new Color(0.25f, 0.25f, 0.3f, 0.7f);

            var helmFillObj = new GameObject("HelmFill");
            helmFillObj.transform.SetParent(helmBar.transform, false);
            var hfRect2 = helmFillObj.AddComponent<RectTransform>();
            hfRect2.anchorMin = Vector2.zero;
            hfRect2.anchorMax = Vector2.one;
            hfRect2.offsetMin = new Vector2(1, 1);
            hfRect2.offsetMax = new Vector2(-1, -1);
            var helmFillImage = helmFillObj.AddComponent<Image>();
            helmFillImage.color = new Color(0.7f, 0.7f, 0.8f, 0.85f);
            helmFillImage.type = Image.Type.Filled;
            helmFillImage.fillMethod = Image.FillMethod.Horizontal;
            helmFillImage.fillAmount = 0;

            var helmLabelObj = CreateUIText(helmBar.transform, "HelmLabel",
                new Vector2(0.5f, 0.5f), "HELM", font, 10, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            helmLabelObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            helmLabelObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            armorPanelObj.SetActive(false);

            // Level & Round (top center)
            var nightText = CreateUIText(canvas.transform, "NightText",
                new Vector2(0.5f, 1), "LEVEL 1", font, 32, TextAnchor.UpperCenter, new Color(0.9f, 0.8f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(300, 40));
            nightText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var waveText = CreateUIText(canvas.transform, "WaveText",
                new Vector2(0.5f, 1), "", font, 18, TextAnchor.UpperCenter, new Color(0.8f, 0.6f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(200, 25));

            // Enemy count (top right)
            var enemyCount = CreateUIText(canvas.transform, "EnemyCount",
                new Vector2(1, 1), "0", font, 24, TextAnchor.UpperRight, new Color(0.8f, 0.5f, 0.5f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(100, 30));
            enemyCount.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Status text (center)
            var statusText = CreateUIText(canvas.transform, "StatusText",
                new Vector2(0.5f, 0.5f), "", font, 32, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(500, 50));
            statusText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            statusText.SetActive(false);

            // Reload hint
            var reloadHint = CreateUIText(canvas.transform, "ReloadHint",
                new Vector2(0.5f, 0.5f), "RELOADING...", font, 20, TextAnchor.MiddleCenter,
                new Color(1, 0.8f, 0.4f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(200, 30));
            reloadHint.SetActive(false);

            // Day timer
            var dayTimerText = CreateUIText(canvas.transform, "DayTimer",
                new Vector2(0.5f, 1), "", font, 20, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.6f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -75), new Vector2(200, 25));

            // Points display (bottom center, CoD Zombies style)
            var pointsText = CreateUIText(canvas.transform, "PointsDisplay",
                new Vector2(0.5f, 0), "0", font, 28, TextAnchor.LowerCenter, new Color(1f, 0.85f, 0.3f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(200, 35));
            pointsText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Radio transmission panel - compact lower-right comms strip
            var radioPanel = CreateUIPanel(canvas.transform, "RadioPanel",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 118f), new Vector2(560f, 104f));
            var radioBg = radioPanel.AddComponent<Image>();
            radioBg.color = new Color(0.02f, 0.03f, 0.04f, 0.72f);

            var radioBorder = new GameObject("RadioBorder");
            radioBorder.transform.SetParent(radioPanel.transform, false);
            var borderRect = radioBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);
            var borderImg = radioBorder.AddComponent<Image>();
            borderImg.color = new Color(0.95f, 0.74f, 0.32f, 0.45f);
            radioBorder.transform.SetAsFirstSibling();

            var radioLabel = CreateUIText(radioPanel.transform, "RadioLabel",
                new Vector2(0f, 1f), "COMMS", font, 13, TextAnchor.UpperLeft,
                new Color(0.95f, 0.8f, 0.45f, 0.8f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -6f), new Vector2(-14f, 18f));
            radioLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var radioText = CreateUIText(radioPanel.transform, "RadioText",
                new Vector2(0f, 0.5f), "", font, 18, TextAnchor.MiddleLeft, new Color(0.95f, 0.95f, 0.9f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            radioText.GetComponent<RectTransform>().offsetMin = new Vector2(16f, 14f);
            radioText.GetComponent<RectTransform>().offsetMax = new Vector2(-16f, -24f);
            radioText.GetComponent<Text>().fontStyle = FontStyle.Normal;
            radioText.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
            radioText.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
            var radioOutline = radioText.AddComponent<Outline>();
            radioOutline.effectColor = Color.black;
            radioOutline.effectDistance = new Vector2(1, -1);
            radioPanel.SetActive(false);

            if (RadioTransmissions.Instance != null)
            {
                RadioTransmissions.Instance.SetUI(
                    radioText.GetComponent<Text>(),
                    radioBg,
                    radioPanel
                );
            }

            // Damage overlay
            var dmgOverlay = CreateUIImage(canvas.transform, "DamageOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                Color.clear);
            var dmgRect = dmgOverlay.GetComponent<RectTransform>();
            dmgRect.offsetMin = Vector2.zero;
            dmgRect.offsetMax = Vector2.zero;
            dmgOverlay.GetComponent<Image>().raycastTarget = false;

            // Fade overlay
            var fadeOverlay = CreateUIImage(canvas.transform, "FadeOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                Color.clear);
            var fadeRect = fadeOverlay.GetComponent<RectTransform>();
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeOverlay.GetComponent<Image>().raycastTarget = false;

            // Objective HUD
            var objPanel = CreateUIPanel(canvas.transform, "ObjectivePanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, -105), new Vector2(380, 65));
            var objPanelBg = objPanel.AddComponent<Image>();
            objPanelBg.color = new Color(0, 0, 0, 0.55f);
            objPanel.SetActive(false);

            var objTitleText = CreateUIText(objPanel.transform, "ObjTitle",
                new Vector2(0.5f, 0.7f), "OBJECTIVE", font, 14, TextAnchor.MiddleCenter,
                new Color(0.4f, 0.85f, 1f, 0.8f),
                new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(5, 0), new Vector2(0, 0));
            var objTitleRect = objTitleText.GetComponent<RectTransform>();
            objTitleRect.offsetMin = new Vector2(5, 35);
            objTitleRect.offsetMax = new Vector2(-5, 0);

            var objDescText = CreateUIText(objPanel.transform, "ObjDesc",
                new Vector2(0.5f, 0.3f), "", font, 16, TextAnchor.MiddleLeft,
                Color.white,
                new Vector2(0, 0), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
            var objDescRect = objDescText.GetComponent<RectTransform>();
            objDescRect.offsetMin = new Vector2(10, 5);
            objDescRect.offsetMax = new Vector2(-80, -2);
            objDescText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var objProgressText = CreateUIText(objPanel.transform, "ObjProgress",
                new Vector2(1, 0.3f), "", font, 18, TextAnchor.MiddleRight,
                new Color(0.4f, 1f, 0.4f),
                new Vector2(0.75f, 0), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
            var objProgressRect = objProgressText.GetComponent<RectTransform>();
            objProgressRect.offsetMin = new Vector2(0, 5);
            objProgressRect.offsetMax = new Vector2(-10, -2);

            canvas.gameObject.AddComponent<Deadlight.UI.ObjectiveMarker>();

            var objHud = canvas.AddComponent<Deadlight.UI.ObjectiveHUD>();
            objHud.Initialize(
                objPanel,
                objDescText.GetComponent<Text>(),
                objProgressText.GetComponent<Text>()
            );

            // Hook up gameplay HUD
            var hudComp = canvas.AddComponent<Deadlight.UI.GameplayHUD>();
            hudComp.Initialize(
                healthLabel.GetComponent<Text>(),
                hfImage,
                ammoText.GetComponent<Text>(),
                sfImage,
                waveText.GetComponent<Text>(),
                nightText.GetComponent<Text>(),
                enemyCount.GetComponent<Text>(),
                statusText.GetComponent<Text>(),
                reloadHint.GetComponent<Text>(),
                dayTimerText.GetComponent<Text>(),
                pointsText.GetComponent<Text>()
            );
            hudComp.SetWeaponHUD(weaponIconImage, weaponName.GetComponent<Text>(), weaponStats.GetComponent<Text>());
            hudComp.SetArmorHUD(vestFillImage, helmFillImage,
                vestLabelObj.GetComponent<Text>(), helmLabelObj.GetComponent<Text>(), armorPanelObj);

            // Hook up GameEffects
            if (GameEffects.Instance != null)
            {
                var camCtrl = Camera.main?.GetComponent<CameraController>();
                GameEffects.Instance.SetupEffects(
                    dmgOverlay.GetComponent<Image>(),
                    fadeOverlay.GetComponent<Image>(),
                    camCtrl
                );
            }
        }

        // ===================== UI HELPERS =====================

        private GameObject CreateUIPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            return obj;
        }

        private GameObject CreateUIImage(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            var img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private GameObject CreateUIText(Transform parent, string name,
            Vector2 pivot, string text, Font font, int fontSize,
            TextAnchor alignment, Color color,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);
            
            return obj;
        }

        // ===================== SPRITE HELPERS =====================

        private Sprite CreateCircleSprite(Color color)
        {
            int size = 32;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2;

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) < radius
                        ? color : Color.clear;

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateBulletSprite()
        {
            int width = 12, height = 6;
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float cx = (float)x / width;
                    float cy = Mathf.Abs(y - height / 2f) / (height / 2f);
                    float alpha = Mathf.Clamp01((1f - cy) * (0.3f + cx * 0.7f));
                    pixels[y * width + x] = new Color(1f, 0.9f, 0.4f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 12f);
        }

    }

    public class PlayerAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite[] sprites;
        private Rigidbody2D rb;
        private bool useProceduralSprites = false;
        
        private float animTimer;
        private int currentFrame;
        private string currentDirection = "Down";
        private int currentDirectionIndex = 0;

        public void SetSprites(Sprite[] playerSprites)
        {
            sprites = playerSprites;
        }

        public void SetUseProceduralSprites(bool useProcedural)
        {
            useProceduralSprites = useProcedural;
        }

        private void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!useProceduralSprites && (sprites == null || sprites.Length == 0)) return;
            UpdateDirection();
            UpdateAnimation();
        }

        private void UpdateDirection()
        {
            Vector2 velocity = rb.linearVelocity;
            if (velocity.magnitude < 0.1f) return;

            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                currentDirection = velocity.x > 0 ? "Right" : "Left";
                currentDirectionIndex = velocity.x > 0 ? 3 : 2;
            }
            else
            {
                currentDirection = velocity.y > 0 ? "Up" : "Down";
                currentDirectionIndex = velocity.y > 0 ? 1 : 0;
            }
        }

        private void UpdateAnimation()
        {
            bool isMoving = rb.linearVelocity.magnitude > 0.1f;

            if (isMoving)
            {
                float speed = rb.linearVelocity.magnitude;
                animTimer += Time.deltaTime * Mathf.Max(6f, speed * 1.5f);
                if (animTimer >= 1f)
                {
                    animTimer = 0f;
                    currentFrame = (currentFrame + 1) % 4;
                }
            }
            else
            {
                currentFrame = 0;
                animTimer = 0f;
            }

            if (useProceduralSprites)
            {
                spriteRenderer.sprite = ProceduralSpriteGenerator.CreatePlayerSprite(currentDirectionIndex, currentFrame);
            }
            else
            {
                string spriteName = $"{currentDirection} {currentFrame}";
                foreach (var sprite in sprites)
                {
                    if (sprite.name == spriteName)
                    {
                        spriteRenderer.sprite = sprite;
                        break;
                    }
                }
            }
        }
    }
}
