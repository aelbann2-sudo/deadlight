using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Deadlight.Audio;
using Deadlight.Data;
using Deadlight.Level;
using Deadlight.Narrative;
using Deadlight.Visuals;
using System.Collections.Generic;
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
        private Sprite[] objectSprites;
        private Sprite[] tileSprites;
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
            CreateCamera();
            CreateGround();
            CreatePlayer();
            CreateManagers();
            CreateEnvironment();
            CreateEnemies();
            BuildHUD();
        }

        private void LoadAllSprites()
        {
            playerSprites = Resources.LoadAll<Sprite>("Player");
            npcSprites = Resources.LoadAll<Sprite>("NPC");
            objectSprites = Resources.LoadAll<Sprite>("Objects");
            tileSprites = Resources.LoadAll<Sprite>("Tiles");
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
                _ => GetTileType_TownCenter(x, y)
            };
        }

        private int GetTileType_TownCenter(int x, int y)
        {
            float gridSpacing = activeMapConfig != null ? activeMapConfig.streetGridSpacing : 10f;
            float mainRoadW = activeMapConfig != null ? activeMapConfig.mainRoadWidth : 2f;
            float sideRoadW = activeMapConfig != null ? activeMapConfig.sideRoadWidth : 1.5f;

            // Central plaza
            float distFromCenter = Mathf.Sqrt(x * x + y * y);
            if (distFromCenter < 7f) return 2;

            // Main cross roads
            if (Mathf.Abs(x) < mainRoadW || Mathf.Abs(y) < mainRoadW) return 3;

            // Grid streets
            float xMod = ((x % gridSpacing) + gridSpacing) % gridSpacing;
            float yMod = ((y % gridSpacing) + gridSpacing) % gridSpacing;
            if (xMod < sideRoadW || xMod > gridSpacing - sideRoadW) return 1;
            if (yMod < sideRoadW || yMod > gridSpacing - sideRoadW) return 1;

            // Sidewalks near grid streets
            if (xMod < sideRoadW + 1 || xMod > gridSpacing - sideRoadW - 1) return 2;
            if (yMod < sideRoadW + 1 || yMod > gridSpacing - sideRoadW - 1) return 2;

            return 0;
        }

        private int GetTileType_Industrial(int x, int y)
        {
            int hh = activeMapConfig != null ? activeMapConfig.halfHeight : 26;

            // Loading dock area (bottom third)
            if (y < -hh * 0.3f) return 2;

            // Main horizontal road
            if (Mathf.Abs(y) < 2f) return 3;

            // Main vertical road
            if (Mathf.Abs(x) < 1.5f) return 3;

            // Vertical corridors between warehouse blocks
            float xMod = ((x % 7f) + 7f) % 7f;
            if (xMod < 1.0f || xMod > 6.0f) return 2;

            // Horizontal corridors between warehouse rows
            float yMod = ((y % 10f) + 10f) % 10f;
            if (yMod < 1.0f || yMod > 9.0f) return 2;

            // Dirt patches in far corners
            int hw = activeMapConfig != null ? activeMapConfig.halfWidth : 20;
            if (Mathf.Abs(x) > hw - 3 && Mathf.Abs(y) > hh - 3) return 1;

            // Mostly concrete
            return 2;
        }

        private int GetTileType_Suburban(int x, int y)
        {
            float roadW = activeMapConfig != null ? activeMapConfig.mainRoadWidth : 2.5f;

            // Main horizontal road
            if (Mathf.Abs(y) < roadW) return 1;

            // Winding vertical road
            float windingCenterX = Mathf.Sin(y * 0.15f) * 6f;
            if (Mathf.Abs(x - windingCenterX) < roadW) return 1;

            // Branch roads off the winding road
            for (int branchY = -24; branchY <= 24; branchY += 8)
            {
                if (Mathf.Abs(y - branchY) < 1.5f)
                {
                    float branchX = Mathf.Sin(branchY * 0.15f) * 6f;
                    if ((x > branchX && x < branchX + 14) || (x < branchX && x > branchX - 14))
                        return 1;
                }
            }

            return 0;
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
            CreateMapEnvironment(envParent.transform);
            CreatePerimeter(envParent.transform);
            SpawnLorePickups(envParent.transform);

            MapType mapType = activeMapConfig != null ? activeMapConfig.mapType : MapType.TownCenter;
            var landmarksObj = new GameObject("MapLandmarks");
            landmarksObj.transform.SetParent(envParent.transform);
            var landmarks = landmarksObj.AddComponent<Level.MapLandmarks>();
            landmarks.CreateAllLandmarks(envParent.transform, mapType);
        }

        private void CreateMapEnvironment(Transform parent)
        {
            MapType type = activeMapConfig != null ? activeMapConfig.mapType : MapType.TownCenter;
            switch (type)
            {
                case MapType.Industrial:
                    CreateIndustrialDistrict(parent);
                    break;
                case MapType.Suburban:
                    CreateSuburbanArea(parent);
                    break;
                default:
                    CreateTownCenter(parent);
                    break;
            }
        }

        // ===================== TOWN CENTER =====================

        private void CreateTownCenter(Transform parent)
        {
            var town = new GameObject("Town");
            town.transform.SetParent(parent);

            // Create central plaza
            CreateCentralPlaza(town.transform);

            // Create city blocks
            CreateCityBlocks(town.transform);

            // Add decorative elements
            AddDecorativeElements(town.transform, MapType.TownCenter);
        }

        private void CreateCentralPlaza(Transform parent)
        {
            // Plaza fountain
            var fountain = new GameObject("Fountain");
            fountain.transform.SetParent(parent);
            fountain.transform.position = Vector3.zero;
            var sr = fountain.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(2);
            sr.sortingOrder = 5;
            sr.color = new Color(0.7f, 0.7f, 0.8f);
            var col = fountain.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 3f);

            // Surrounding benches
            Vector3[] benchPositions = {
                new Vector3(-2, 0, 0), new Vector3(2, 0, 0),
                new Vector3(0, -2, 0), new Vector3(0, 2, 0)
            };
            foreach (var pos in benchPositions)
            {
                var bench = new GameObject("Bench");
                bench.transform.SetParent(parent);
                bench.transform.position = pos;
                var benchSr = bench.AddComponent<SpriteRenderer>();
                benchSr.sprite = ProceduralSpriteGenerator.CreateRockSprite();
                benchSr.sortingOrder = 4;
                var benchCol = bench.AddComponent<BoxCollider2D>();
                benchCol.size = new Vector2(1.5f, 0.5f);
            }
        }

        private void CreateCityBlocks(Transform parent)
        {
            // City block positions
            Vector3[] blockCenters = {
                new Vector3(-15, 15, 0), new Vector3(15, 15, 0),
                new Vector3(-15, -15, 0), new Vector3(15, -15, 0),
                new Vector3(0, 15, 0), new Vector3(0, -15, 0),
                new Vector3(-15, 0, 0), new Vector3(15, 0, 0)
            };

            foreach (var center in blockCenters)
            {
                CreateBlock(parent, center);
            }
        }

        private void CreateBlock(Transform parent, Vector3 center)
        {
            // Buildings around the block
            Vector3[] buildingOffsets = {
                new Vector3(-3, 3, 0), new Vector3(3, 3, 0),
                new Vector3(-3, -3, 0), new Vector3(3, -3, 0)
            };

            foreach (var offset in buildingOffsets)
            {
                var building = new GameObject("Building");
                building.transform.SetParent(parent);
                building.transform.position = center + offset;
                var sr = building.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(Random.Range(0, 3));
                sr.sortingOrder = Mathf.RoundToInt(-(center.y + offset.y));
                sr.color = activeMapConfig.buildingTint;
                var col = building.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2f, 2f);
            }

            // Trees in the block
            Vector3[] treeOffsets = {
                new Vector3(-1, 1, 0), new Vector3(1, -1, 0)
            };

            foreach (var offset in treeOffsets)
            {
                SpawnTree(parent, center + offset);
            }

            // Cover elements
            Vector3[] coverOffsets = {
                new Vector3(-2, 0, 0), new Vector3(2, 0, 0),
                new Vector3(0, -2, 0), new Vector3(0, 2, 0)
            };

            foreach (var offset in coverOffsets)
            {
                SpawnCrate(parent, center + offset);
            }
        }

        // ===================== INDUSTRIAL DISTRICT =====================

        private void CreateIndustrialDistrict(Transform parent)
        {
            var district = new GameObject("IndustrialDistrict");
            district.transform.SetParent(parent);

            // Create warehouse complex
            CreateWarehouseComplex(district.transform);

            // Create loading docks
            CreateLoadingDocks(district.transform);

            // Add industrial elements
            AddDecorativeElements(district.transform, MapType.Industrial);
        }

        private void CreateWarehouseComplex(Transform parent)
        {
            Vector3[] warehousePositions = {
                new Vector3(-20, 25, 0), new Vector3(0, 25, 0), new Vector3(20, 25, 0),
                new Vector3(-20, 10, 0), new Vector3(0, 10, 0), new Vector3(20, 10, 0),
                new Vector3(-20, -5, 0), new Vector3(0, -5, 0), new Vector3(20, -5, 0)
            };

            foreach (var pos in warehousePositions)
            {
                CreateWarehouse(parent, pos);
            }
        }

        private void CreateWarehouse(Transform parent, Vector3 position)
        {
            var warehouse = new GameObject("Warehouse");
            warehouse.transform.SetParent(parent);
            warehouse.transform.position = position;
            var sr = warehouse.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(1);
            sr.sortingOrder = Mathf.RoundToInt(-position.y);
            sr.color = new Color(0.6f, 0.6f, 0.65f);
            var col = warehouse.AddComponent<BoxCollider2D>();
            col.size = new Vector2(8f, 6f);

            // Add containers
            Vector3[] containerPositions = {
                new Vector3(-3, -2, 0), new Vector3(3, -2, 0),
                new Vector3(-3, 2, 0), new Vector3(3, 2, 0)
            };

            foreach (var offset in containerPositions)
            {
                var container = new GameObject("Container");
                container.transform.SetParent(warehouse.transform);
                container.transform.localPosition = offset;
                var containerSr = container.AddComponent<SpriteRenderer>();
                containerSr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
                containerSr.sortingOrder = Mathf.RoundToInt(-(position.y + offset.y));
                var containerCol = container.AddComponent<BoxCollider2D>();
                containerCol.size = new Vector2(1.5f, 1f);
            }
        }

        private void CreateLoadingDocks(Transform parent)
        {
            var dock = new GameObject("LoadingDock");
            dock.transform.SetParent(parent);
            dock.transform.position = new Vector3(0, -25, 0);

            // Dock platform
            var platform = new GameObject("Platform");
            platform.transform.SetParent(dock.transform);
            platform.transform.localPosition = Vector3.zero;
            var sr = platform.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateGroundTile(2);
            sr.sortingOrder = -1;
            var col = platform.AddComponent<BoxCollider2D>();
            col.size = new Vector2(20f, 8f);

            // Dock equipment
            Vector3[] equipmentPositions = {
                new Vector3(-8, 0, 0), new Vector3(0, 0, 0), new Vector3(8, 0, 0)
            };

            foreach (var offset in equipmentPositions)
            {
                SpawnBarrel(dock.transform, offset);
            }
        }

        // ===================== SUBURBAN AREA =====================

        private void CreateSuburbanArea(Transform parent)
        {
            var suburb = new GameObject("SuburbanArea");
            suburb.transform.SetParent(parent);

            // Create residential neighborhoods
            CreateResidentialNeighborhoods(suburb.transform);

            // Create parks and open spaces
            CreateParks(suburb.transform);

            // Add suburban elements
            AddDecorativeElements(suburb.transform, MapType.Suburban);
        }

        private void CreateResidentialNeighborhoods(Transform parent)
        {
            Vector3[] neighborhoodCenters = {
                new Vector3(-25, 20, 0), new Vector3(0, 20, 0), new Vector3(25, 20, 0),
                new Vector3(-25, -20, 0), new Vector3(0, -20, 0), new Vector3(25, -20, 0)
            };

            foreach (var center in neighborhoodCenters)
            {
                CreateResidentialBlock(parent, center);
            }
        }

        private void CreateResidentialBlock(Transform parent, Vector3 center)
        {
            // Houses in the block
            Vector3[] houseOffsets = {
                new Vector3(-4, 3, 0), new Vector3(0, 3, 0), new Vector3(4, 3, 0),
                new Vector3(-4, -3, 0), new Vector3(0, -3, 0), new Vector3(4, -3, 0)
            };

            foreach (var offset in houseOffsets)
            {
                var house = new GameObject("House");
                house.transform.SetParent(parent);
                house.transform.position = center + offset;
                var sr = house.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(Random.Range(0, 3));
                sr.sortingOrder = Mathf.RoundToInt(-(center.y + offset.y));
                sr.color = activeMapConfig.buildingTint;
                var col = house.AddComponent<BoxCollider2D>();
                col.size = new Vector2(2.5f, 2.5f);
            }

            // Yard elements
            Vector3[] yardElements = {
                new Vector3(-2, 0, 0), new Vector3(2, 0, 0),
                new Vector3(0, -2, 0), new Vector3(0, 2, 0)
            };

            foreach (var offset in yardElements)
            {
                if (Random.value > 0.5f)
                    SpawnTree(parent, center + offset);
                else
                    SpawnBush(parent, center + offset);
            }
        }

        private void CreateParks(Transform parent)
        {
            Vector3[] parkCenters = {
                new Vector3(-15, 0, 0), new Vector3(15, 0, 0)
            };

            foreach (var center in parkCenters)
            {
                CreatePark(parent, center);
            }
        }

        private void CreatePark(Transform parent, Vector3 center)
        {
            var park = new GameObject("Park");
            park.transform.SetParent(parent);
            park.transform.position = center;

            // Park area
            var area = new GameObject("ParkArea");
            area.transform.SetParent(park.transform);
            area.transform.localPosition = Vector3.zero;
            var sr = area.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateGroundTile(0);
            sr.sortingOrder = -2;
            sr.color = new Color(0.7f, 0.9f, 0.7f);
            var col = area.AddComponent<BoxCollider2D>();
            col.size = new Vector2(10f, 8f);

            // Park elements
            Vector3[] elements = {
                new Vector3(-3, 2, 0), new Vector3(3, 2, 0),
                new Vector3(-3, -2, 0), new Vector3(3, -2, 0),
                new Vector3(0, 0, 0)
            };

            foreach (var offset in elements)
            {
                if (Random.value > 0.3f)
                    SpawnTree(park.transform, center + offset);
                else
                    SpawnBush(park.transform, center + offset);
            }
        }

        private void AddDecorativeElements(Transform parent, MapType mapType)
        {
            int elementCount = mapType switch
            {
                MapType.TownCenter => 20,
                MapType.Industrial => 15,
                MapType.Suburban => 30,
                _ => 10
            };

            for (int i = 0; i < elementCount; i++)
            {
                Vector3 position = GetRandomPositionInMap();
                float rand = Random.value;

                if (rand < 0.4f)
                    SpawnTree(parent, position);
                else if (rand < 0.7f)
                    SpawnBush(parent, position);
                else if (rand < 0.9f)
                    SpawnRock(parent, position);
                else
                    SpawnCrate(parent, position);
            }
        }

        private Vector3 GetRandomPositionInMap()
        {
            int hw = activeMapConfig.halfWidth;
            int hh = activeMapConfig.halfHeight;
            return new Vector3(
                Random.Range(-hw + 2, hw - 2),
                Random.Range(-hh + 2, hh - 2),
                0
            );
        }

        private void SpawnTree(Transform parent, Vector3 pos)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * Random.Range(1.2f, 2f);
            var sr = tree.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateTreeSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
            var col = tree.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
            col.offset = new Vector2(0, -0.3f);
        }

        private void SpawnBush(Transform parent, Vector3 pos)
        {
            var bush = new GameObject("Bush");
            bush.transform.SetParent(parent);
            bush.transform.position = pos;
            bush.transform.localScale = Vector3.one * Random.Range(0.8f, 1.5f);
            var sr = bush.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateTreeSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;
            sr.color = new Color(0.3f, 0.6f, 0.3f);
            var col = bush.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
        }

        private void SpawnRock(Transform parent, Vector3 pos)
        {
            var rock = new GameObject("Rock");
            rock.transform.SetParent(parent);
            rock.transform.position = pos;
            rock.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            var sr = rock.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateRockSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = rock.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
        }

        private void SpawnCrate(Transform parent, Vector3 pos)
        {
            var crate = new GameObject("Crate");
            crate.transform.SetParent(parent);
            crate.transform.position = pos;
            var sr = crate.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = crate.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }

        private void SpawnBarrel(Transform parent, Vector3 pos)
        {
            var barrel = new GameObject("Barrel");
            barrel.transform.SetParent(parent);
            barrel.transform.position = pos;
            var sr = barrel.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBarrelSprite(Random.value > 0.6f);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = barrel.AddComponent<CircleCollider2D>();
            col.radius = 0.25f;
        }

        private void SpawnCar(Transform parent, Vector3 pos, float angle = 0f)
        {
            var car = new GameObject("Car");
            car.transform.SetParent(parent);
            car.transform.position = pos;
            car.transform.rotation = Quaternion.Euler(0, 0, angle);
            var sr = car.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateCarSprite(Random.Range(0, 4));
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            var col = car.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 0.7f);
        }

        private void SpawnBuilding(Transform parent, Vector3 pos, float scale, int variant)
        {
            var house = new GameObject("Building");
            house.transform.SetParent(parent);
            house.transform.position = pos;
            house.transform.localScale = Vector3.one * scale;
            var sr = house.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(variant % 3);
            sr.sortingOrder = Mathf.RoundToInt(-pos.y);
            if (activeMapConfig != null) sr.color = activeMapConfig.buildingTint;
            var col = house.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.2f, 1.2f);
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
            
            float halfW = activeMapConfig != null ? activeMapConfig.perimeterHalfW : 12f;
            float halfH = activeMapConfig != null ? activeMapConfig.perimeterHalfH : 12f;
            float thickness = 1.5f;

            CreateWall(perimeter.transform, new Vector3(0, halfH, 0), new Vector2(halfW * 2, thickness));
            CreateWall(perimeter.transform, new Vector3(0, -halfH, 0), new Vector2(halfW * 2, thickness));
            CreateWall(perimeter.transform, new Vector3(-halfW, 0, 0), new Vector2(thickness, halfH * 2));
            CreateWall(perimeter.transform, new Vector3(halfW, 0, 0), new Vector2(thickness, halfH * 2));
        }

        private void CreateWall(Transform parent, Vector3 position, Vector2 size)
        {
            var wall = new GameObject("Wall");
            wall.transform.SetParent(parent);
            wall.transform.position = position;

            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(new Color(0.35f, 0.3f, 0.25f), size);
            sr.sortingOrder = -1;

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

            if (canvasComp != null && canvasComp.GetComponent<Deadlight.UI.LiveHUD>() != null)
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
            var hfImage = healthFill.GetComponent<Image>();
            hfImage.type = Image.Type.Filled;
            hfImage.fillMethod = Image.FillMethod.Horizontal;
            hfImage.fillAmount = 1f;

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

            // Night & Round (top center)
            var nightText = CreateUIText(canvas.transform, "NightText",
                new Vector2(0.5f, 1), "NIGHT 1", font, 32, TextAnchor.UpperCenter, new Color(0.9f, 0.8f, 0.5f),
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

            // Controls hint
            var controlsHint = CreateUIText(canvas.transform, "Controls",
                new Vector2(1, 0), "WASD-Move  Mouse-Aim  Click-Shoot  Space-Dodge  Shift-Sprint  R-Reload  1/2-Switch Weapon  E-Interact",
                font, 12, TextAnchor.LowerRight, new Color(1, 1, 1, 0.5f),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 15), new Vector2(800, 25));

            // Day timer
            var dayTimerText = CreateUIText(canvas.transform, "DayTimer",
                new Vector2(0.5f, 1), "", font, 20, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.6f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -75), new Vector2(200, 25));

            // Points display (bottom center, CoD Zombies style)
            var pointsText = CreateUIText(canvas.transform, "PointsDisplay",
                new Vector2(0.5f, 0), "0", font, 28, TextAnchor.LowerCenter, new Color(1f, 0.85f, 0.3f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(200, 35));
            pointsText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Throwables display (bottom left)
            var throwablesText = CreateUIText(canvas.transform, "ThrowablesDisplay",
                new Vector2(0, 0), "Q:Grenade(2)  E:Molotov(1)", font, 14, TextAnchor.LowerLeft,
                new Color(0.8f, 0.8f, 0.8f, 0.7f),
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 50), new Vector2(300, 20));

            // Radio transmission panel - upper-center, highly visible
            var radioPanel = CreateUIPanel(canvas.transform, "RadioPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 120), new Vector2(900, 120));
            var radioBg = radioPanel.AddComponent<Image>();
            radioBg.color = new Color(0, 0, 0, 0.85f);

            var radioBorder = new GameObject("RadioBorder");
            radioBorder.transform.SetParent(radioPanel.transform, false);
            var borderRect = radioBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);
            var borderImg = radioBorder.AddComponent<Image>();
            borderImg.color = new Color(0.2f, 0.8f, 0.2f, 0.6f);
            radioBorder.transform.SetAsFirstSibling();

            var radioLabel = CreateUIText(radioPanel.transform, "RadioLabel",
                new Vector2(0.5f, 1f), "[RADIO TRANSMISSION]", font, 14, TextAnchor.UpperCenter,
                new Color(0.5f, 1f, 0.5f, 0.7f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -5), new Vector2(300, 18));

            var radioText = CreateUIText(radioPanel.transform, "RadioText",
                new Vector2(0.5f, 0.5f), "", font, 24, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.3f),
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
            radioText.GetComponent<RectTransform>().offsetMin = new Vector2(30, 15);
            radioText.GetComponent<RectTransform>().offsetMax = new Vector2(-30, -20);
            radioText.GetComponent<Text>().fontStyle = FontStyle.BoldAndItalic;
            var radioOutline = radioText.AddComponent<Outline>();
            radioOutline.effectColor = Color.black;
            radioOutline.effectDistance = new Vector2(2, -2);
            radioPanel.SetActive(false);

            if (RadioTransmissions.Instance != null)
            {
                RadioTransmissions.Instance.SetUI(
                    radioText.GetComponent<Text>(),
                    radioBg,
                    radioPanel
                );
            }

            // Game Over panel
            var goPanel = new GameObject("GameOverPanel");
            goPanel.transform.SetParent(canvas.transform, false);
            var goPanelRect = goPanel.AddComponent<RectTransform>();
            goPanelRect.anchorMin = Vector2.zero;
            goPanelRect.anchorMax = Vector2.one;
            goPanelRect.offsetMin = Vector2.zero;
            goPanelRect.offsetMax = Vector2.zero;
            var goPanelImg = goPanel.AddComponent<Image>();
            goPanelImg.color = new Color(0, 0, 0, 0.75f);

            var goText = CreateUIText(goPanel.transform, "GameOverText",
                new Vector2(0.5f, 0.5f), "GAME OVER", font, 48, TextAnchor.MiddleCenter, Color.red,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 200));
            goText.GetComponent<Text>().fontStyle = FontStyle.Bold;

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

            // Hook up LiveHUD
            var hudComp = canvas.AddComponent<Deadlight.UI.LiveHUD>();
            hudComp.Initialize(
                healthLabel.GetComponent<Text>(),
                hfImage,
                ammoText.GetComponent<Text>(),
                sfImage,
                waveText.GetComponent<Text>(),
                nightText.GetComponent<Text>(),
                enemyCount.GetComponent<Text>(),
                statusText.GetComponent<Text>(),
                goPanel,
                goText.GetComponent<Text>(),
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

        private Sprite CreateRectSprite(Color color, Vector2 size)
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(size.x * 16));
            int height = Mathf.Max(1, Mathf.RoundToInt(size.y * 16));
            
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
        }

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

        private Sprite CreatePixelSprite(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
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
