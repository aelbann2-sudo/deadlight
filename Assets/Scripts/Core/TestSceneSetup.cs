using UnityEngine;
using UnityEngine.UI;
using Deadlight.Enemy;
using Deadlight.Audio;
using Deadlight.Data;
using Deadlight.Level;
using Deadlight.Level.MapBuilders;
using Deadlight.Narrative;
using Deadlight.UI;
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
            trail.time = 0.05f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(0.96f, 0.98f, 1f, 0.85f);
            trail.endColor = new Color(0.5f, 0.72f, 1f, 0f);

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
            var wall = new GameObject("BoundaryWall");
            wall.transform.SetParent(parent);
            wall.transform.position = position;

            // Visible concrete barrier — child object so localScale doesn't affect the collider
            var visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(wall.transform);
            visualObj.transform.localPosition = Vector3.zero;
            visualObj.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = visualObj.AddComponent<SpriteRenderer>();
            bool isHorizontal = size.x >= size.y;
            sr.sprite = Visuals.ProceduralSpriteGenerator.CreateWallSprite(isHorizontal, 32);
            sr.color = new Color(0.22f, 0.19f, 0.17f, 1f);
            sr.sortingOrder = -8;

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

            Font font = UIFactory.GetFont();
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            Color cardColor = new Color(0.03f, 0.04f, 0.06f, 0.8f);
            Color cardSoft = new Color(0.06f, 0.08f, 0.11f, 0.72f);
            Color titleColor = new Color(0.95f, 0.96f, 0.98f, 1f);
            Color accentGold = new Color(0.96f, 0.82f, 0.46f, 1f);
            Color accentBlue = new Color(0.44f, 0.78f, 1f, 1f);
            Color accentGreen = new Color(0.34f, 0.88f, 0.46f, 1f);
            Color accentRed = new Color(0.96f, 0.42f, 0.36f, 1f);
            Color mutedText = new Color(0.70f, 0.76f, 0.84f, 1f);

            var hudRoot = CreateUIPanel(canvas.transform, "GameplayHUDRoot",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var hudRootRect = hudRoot.GetComponent<RectTransform>();
            hudRootRect.offsetMin = Vector2.zero;
            hudRootRect.offsetMax = Vector2.zero;

            // Survivor status card
            var healthPanel = CreateUIPanel(hudRoot.transform, "HealthPanel",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f), new Vector2(340f, 118f));
            var healthPanelBg = healthPanel.AddComponent<Image>();
            healthPanelBg.color = cardColor;

            var healthHeader = CreateUIText(healthPanel.transform, "HealthHeader",
                new Vector2(0f, 1f), "SURVIVOR STATUS", font, 16, TextAnchor.UpperLeft, mutedText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(220f, 20f));
            healthHeader.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var healthBar = CreateUIPanel(healthPanel.transform, "HealthBar",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -38f), new Vector2(308f, 36f));
            var healthBg = healthBar.AddComponent<Image>();
            healthBg.color = new Color(0.13f, 0.15f, 0.19f, 0.95f);

            var healthFill = CreateUIImage(healthBar.transform, "HealthFill",
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero,
                new Color(0.2f, 0.8f, 0.2f, 0.95f));
            var hfRect = healthFill.GetComponent<RectTransform>();
            hfRect.offsetMin = new Vector2(2f, 2f);
            hfRect.offsetMax = new Vector2(-2f, -2f);
            hfRect.pivot = new Vector2(0f, 0.5f);
            var hfImage = healthFill.GetComponent<Image>();
            hfImage.type = Image.Type.Simple;

            var healthLabel = CreateUIText(healthBar.transform, "HealthText",
                new Vector2(0.5f, 0.5f), "100 / 100", font, 19, TextAnchor.MiddleCenter, titleColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            healthLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var healthIcon = CreateUIText(healthBar.transform, "HealthIcon",
                new Vector2(0f, 0.5f), "+", font, 18, TextAnchor.MiddleCenter, accentRed,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(18f, 22f));
            healthIcon.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var staminaBar = CreateUIPanel(healthPanel.transform, "StaminaBar",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -82f), new Vector2(308f, 14f));
            var staminaBg = staminaBar.AddComponent<Image>();
            staminaBg.color = new Color(0.13f, 0.15f, 0.19f, 0.9f);

            var staminaFill = CreateUIImage(staminaBar.transform, "StaminaFill",
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero,
                accentBlue);
            var sfRect = staminaFill.GetComponent<RectTransform>();
            sfRect.offsetMin = new Vector2(1f, 1f);
            sfRect.offsetMax = new Vector2(-1f, -1f);
            var sfImage = staminaFill.GetComponent<Image>();
            sfImage.type = Image.Type.Filled;
            sfImage.fillMethod = Image.FillMethod.Horizontal;

            // Armor display
            var armorPanelObj = CreateUIPanel(healthPanel.transform, "ArmorPanel",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 12f), new Vector2(308f, 28f));

            var vestBar = CreateUIPanel(armorPanelObj.transform, "VestBar",
                new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, Vector2.zero);
            vestBar.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 2f);
            vestBar.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -2f);
            var vestBg = vestBar.AddComponent<Image>();
            vestBg.color = new Color(0.10f, 0.16f, 0.28f, 0.78f);

            var vestFillObj = new GameObject("VestFill");
            vestFillObj.transform.SetParent(vestBar.transform, false);
            var vfRect = vestFillObj.AddComponent<RectTransform>();
            vfRect.anchorMin = Vector2.zero;
            vfRect.anchorMax = Vector2.one;
            vfRect.offsetMin = new Vector2(1f, 1f);
            vfRect.offsetMax = new Vector2(-1f, -1f);
            var vestFillImage = vestFillObj.AddComponent<Image>();
            vestFillImage.color = new Color(0.26f, 0.56f, 0.94f, 0.9f);
            vestFillImage.type = Image.Type.Filled;
            vestFillImage.fillMethod = Image.FillMethod.Horizontal;
            vestFillImage.fillAmount = 0f;

            var vestLabelObj = CreateUIText(vestBar.transform, "VestLabel",
                new Vector2(0.5f, 0.5f), "VEST", font, 12, TextAnchor.MiddleCenter, titleColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            vestLabelObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
            vestLabelObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            vestLabelObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            var helmBar = CreateUIPanel(armorPanelObj.transform, "HelmBar",
                new Vector2(0.52f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, Vector2.zero);
            helmBar.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 2f);
            helmBar.GetComponent<RectTransform>().offsetMax = new Vector2(0f, -2f);
            var helmBg = helmBar.AddComponent<Image>();
            helmBg.color = new Color(0.22f, 0.22f, 0.28f, 0.78f);

            var helmFillObj = new GameObject("HelmFill");
            helmFillObj.transform.SetParent(helmBar.transform, false);
            var hfRect2 = helmFillObj.AddComponent<RectTransform>();
            hfRect2.anchorMin = Vector2.zero;
            hfRect2.anchorMax = Vector2.one;
            hfRect2.offsetMin = new Vector2(1f, 1f);
            hfRect2.offsetMax = new Vector2(-1f, -1f);
            var helmFillImage = helmFillObj.AddComponent<Image>();
            helmFillImage.color = new Color(0.78f, 0.80f, 0.90f, 0.9f);
            helmFillImage.type = Image.Type.Filled;
            helmFillImage.fillMethod = Image.FillMethod.Horizontal;
            helmFillImage.fillAmount = 0f;

            var helmLabelObj = CreateUIText(helmBar.transform, "HelmLabel",
                new Vector2(0.5f, 0.5f), "HELM", font, 12, TextAnchor.MiddleCenter, titleColor,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            helmLabelObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
            helmLabelObj.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            helmLabelObj.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            armorPanelObj.SetActive(false);

            // Mission status card
            var missionPanel = CreateUIPanel(hudRoot.transform, "MissionPanel",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(380f, 112f));
            var missionBg = missionPanel.AddComponent<Image>();
            missionBg.color = cardColor;

            var missionLabel = CreateUIText(missionPanel.transform, "MissionLabel",
                new Vector2(0.5f, 1f), "MISSION STATUS", font, 15, TextAnchor.UpperCenter, mutedText,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(260f, 18f));
            missionLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var nightText = CreateUIText(missionPanel.transform, "NightText",
                new Vector2(0.5f, 1f), "LEVEL 1", font, 40, TextAnchor.UpperCenter, accentGold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(320f, 36f));
            nightText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var dayTimerText = CreateUIText(missionPanel.transform, "DayTimer",
                new Vector2(0.5f, 1f), "", font, 24, TextAnchor.UpperCenter, titleColor,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(280f, 24f));
            dayTimerText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var waveText = CreateUIText(missionPanel.transform, "WaveText",
                new Vector2(0.5f, 0f), "", font, 18, TextAnchor.LowerCenter, mutedText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(260f, 20f));
            waveText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Hostiles card
            var enemyPanel = CreateUIPanel(hudRoot.transform, "EnemyPanel",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f), new Vector2(180f, 92f));
            var enemyBg = enemyPanel.AddComponent<Image>();
            enemyBg.color = cardColor;

            var enemyLabel = CreateUIText(enemyPanel.transform, "EnemyLabel",
                new Vector2(1f, 1f), "HOSTILES", font, 15, TextAnchor.UpperRight, mutedText,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(120f, 18f));
            enemyLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var enemyCount = CreateUIText(enemyPanel.transform, "EnemyCount",
                new Vector2(1f, 0f), "0", font, 34, TextAnchor.LowerRight, accentRed,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(100f, 40f));
            enemyCount.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Status text (center)
            var statusText = CreateUIText(hudRoot.transform, "StatusText",
                new Vector2(0.5f, 0.5f), "", font, 34, TextAnchor.MiddleCenter, titleColor,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 52f), new Vector2(560f, 60f));
            statusText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            statusText.SetActive(false);

            // Reload hint
            var reloadHint = CreateUIText(hudRoot.transform, "ReloadHint",
                new Vector2(0.5f, 0.5f), "RELOADING...", font, 24, TextAnchor.MiddleCenter,
                accentGold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(260f, 34f));
            reloadHint.GetComponent<Text>().fontStyle = FontStyle.Bold;
            reloadHint.SetActive(false);

            // Points card
            var pointsPanel = CreateUIPanel(hudRoot.transform, "PointsPanel",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 24f), new Vector2(200f, 78f));
            var pointsBg = pointsPanel.AddComponent<Image>();
            pointsBg.color = cardSoft;

            var pointsLabel = CreateUIText(pointsPanel.transform, "PointsLabel",
                new Vector2(0f, 1f), "SCORE", font, 14, TextAnchor.UpperLeft, mutedText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(100f, 18f));
            pointsLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var pointsText = CreateUIText(pointsPanel.transform, "PointsDisplay",
                new Vector2(0f, 0f), "0", font, 34, TextAnchor.LowerLeft, accentGold,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(160f, 38f));
            pointsText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Loadout card
            var weaponPanel = CreateUIPanel(hudRoot.transform, "WeaponPanel",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f), new Vector2(320f, 126f));
            var wpBg = weaponPanel.AddComponent<Image>();
            wpBg.color = cardColor;

            var weaponLabel = CreateUIText(weaponPanel.transform, "WeaponLabel",
                new Vector2(0f, 1f), "LOADOUT", font, 14, TextAnchor.UpperLeft, mutedText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(120f, 18f));
            weaponLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var weaponIconObj = new GameObject("WeaponIcon");
            weaponIconObj.transform.SetParent(weaponPanel.transform, false);
            var wiRect = weaponIconObj.AddComponent<RectTransform>();
            wiRect.anchorMin = new Vector2(0f, 1f);
            wiRect.anchorMax = new Vector2(0f, 1f);
            wiRect.pivot = new Vector2(0f, 1f);
            wiRect.anchoredPosition = new Vector2(16f, -38f);
            wiRect.sizeDelta = new Vector2(56f, 28f);
            var weaponIconImage = weaponIconObj.AddComponent<Image>();
            weaponIconImage.preserveAspect = true;
            try { weaponIconImage.sprite = ProceduralSpriteGenerator.CreateWeaponIcon(WeaponType.Pistol); }
            catch { }

            var weaponName = CreateUIText(weaponPanel.transform, "WeaponName",
                new Vector2(0f, 1f), "PISTOL", font, 22, TextAnchor.UpperLeft, accentGold,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -34f), new Vector2(150f, 28f));
            weaponName.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var weaponStats = CreateUIText(weaponPanel.transform, "WeaponStats",
                new Vector2(0f, 1f), "DMG 15  ROF 0.3", font, 14, TextAnchor.UpperLeft,
                mutedText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(84f, -62f), new Vector2(170f, 22f));

            var ammoLabel = CreateUIText(weaponPanel.transform, "AmmoLabel",
                new Vector2(1f, 1f), "AMMO", font, 13, TextAnchor.UpperRight, mutedText,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -16f), new Vector2(90f, 18f));
            ammoLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var ammoText = CreateUIText(weaponPanel.transform, "AmmoText",
                new Vector2(1f, 0f), "15 / 60", font, 44, TextAnchor.LowerRight, titleColor,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(170f, 48f));
            ammoText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Radio transmission panel
            var radioPanel = CreateUIPanel(hudRoot.transform, "RadioPanel",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 164f), new Vector2(500f, 100f));
            var radioBg = radioPanel.AddComponent<Image>();
            radioBg.color = cardSoft;

            var radioLabel = CreateUIText(radioPanel.transform, "RadioLabel",
                new Vector2(0f, 1f), "COMMS", font, 14, TextAnchor.UpperLeft,
                accentGold,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(120f, 18f));
            radioLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var radioText = CreateUIText(radioPanel.transform, "RadioText",
                new Vector2(0f, 0.5f), "", font, 20, TextAnchor.MiddleLeft, new Color(0.95f, 0.95f, 0.9f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            radioText.GetComponent<RectTransform>().offsetMin = new Vector2(16f, 16f);
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
            var objPanel = CreateUIPanel(hudRoot.transform, "ObjectivePanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(24f, -154f), new Vector2(440f, 112f));
            var objPanelBg = objPanel.AddComponent<Image>();
            objPanelBg.color = cardSoft;
            objPanel.SetActive(false);

            var objTitleText = CreateUIText(objPanel.transform, "ObjTitle",
                new Vector2(0f, 1f), "OBJECTIVE", font, 15, TextAnchor.UpperLeft,
                accentBlue,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(140f, 18f));
            objTitleText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var objDescText = CreateUIText(objPanel.transform, "ObjDesc",
                new Vector2(0f, 1f), "", font, 19, TextAnchor.UpperLeft,
                titleColor,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            var objDescRect = objDescText.GetComponent<RectTransform>();
            objDescRect.offsetMin = new Vector2(14f, 36f);
            objDescRect.offsetMax = new Vector2(-14f, -38f);
            objDescText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            objDescText.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
            objDescText.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;

            var objProgressText = CreateUIText(objPanel.transform, "ObjProgress",
                new Vector2(1f, 0f), "", font, 17, TextAnchor.LowerRight,
                accentGreen,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, 10f), new Vector2(180f, 24f));

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
            obj.transform.SetParent(parent, false);
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
            obj.transform.SetParent(parent, false);
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
            obj.transform.SetParent(parent, false);
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
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
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
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 2) + 1;
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
