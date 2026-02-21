using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Deadlight.Audio;
using Deadlight.Data;
using Deadlight.Level;
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
            float pw = activeMapConfig != null ? activeMapConfig.pathWidth : 2f;
            bool diag = activeMapConfig != null ? activeMapConfig.hasDiagonalConcrete : true;
            Color tint = activeMapConfig != null ? activeMapConfig.groundTint : Color.white;

            if (useProceduralSprites)
            {
                var grassSprite = ProceduralSpriteGenerator.CreateGroundTile(0);
                var pathSprite = ProceduralSpriteGenerator.CreateGroundTile(1);
                var concreteSprite = ProceduralSpriteGenerator.CreateGroundTile(2);
                
                for (int x = -hw; x <= hw; x++)
                {
                    for (int y = -hh; y <= hh; y++)
                    {
                        var tile = new GameObject($"T_{x}_{y}");
                        tile.transform.SetParent(groundParent.transform);
                        tile.transform.position = new Vector3(x, y, 0);
                        
                        var sr = tile.AddComponent<SpriteRenderer>();
                        sr.sortingOrder = -200;

                        bool isPath = (Mathf.Abs(x) < pw) || (Mathf.Abs(y) < pw);
                        bool isConcrete = diag && (Mathf.Abs(x - y) < 3 && Mathf.Abs(x) < 8);
                        
                        if (isPath)
                        {
                            sr.sprite = pathSprite;
                        }
                        else if (isConcrete)
                        {
                            sr.sprite = concreteSprite;
                        }
                        else
                        {
                            sr.sprite = grassSprite;
                        }
                        
                        float shade = Random.Range(0.9f, 1.05f);
                        sr.color = new Color(tint.r * shade, tint.g * shade, tint.b * shade);
                    }
                }
            }
            else
            {
                string[] grassNames = { "Grass 0", "Grass 1", "Grass 2", "Grass 3" };
                Sprite[] grassSprites = new Sprite[grassNames.Length];
                for (int i = 0; i < grassNames.Length; i++)
                {
                    grassSprites[i] = GetSprite(tileSprites, grassNames[i]);
                }

                Sprite pathSprite = GetSprite(tileSprites, "Sand 0");
                
                for (int x = -13; x <= 13; x++)
                {
                    for (int y = -13; y <= 13; y++)
                    {
                        var tile = new GameObject($"T_{x}_{y}");
                        tile.transform.SetParent(groundParent.transform);
                        tile.transform.position = new Vector3(x, y, 0);
                        
                        var sr = tile.AddComponent<SpriteRenderer>();
                        sr.sortingOrder = -200;

                        bool isPath = (Mathf.Abs(x) < 2) || (Mathf.Abs(y) < 2);
                        
                        if (isPath && pathSprite != null)
                        {
                            sr.sprite = pathSprite;
                            sr.color = new Color(0.9f, 0.85f, 0.7f);
                        }
                        else
                        {
                            Sprite gs = grassSprites[Random.Range(0, grassSprites.Length)];
                            sr.sprite = gs != null ? gs : CreatePixelSprite(new Color(0.3f, 0.4f, 0.25f));
                            
                            float shade = Random.Range(0.85f, 1.05f);
                            sr.color = new Color(0.85f * shade, 0.95f * shade, 0.8f * shade);
                        }
                    }
                }
            }
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

        // ===================== MANAGERS =====================

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
                gmObj.transform.SetParent(managersParent);
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
                amObj.transform.SetParent(managersParent);
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
                if (managersParent != null)
                    guiObj.transform.SetParent(managersParent);
                guiObj.AddComponent<Deadlight.UI.GameUI>();
                Debug.Log("[TestSceneSetup] Created GameUI");
            }
        }

        // ===================== ENVIRONMENT =====================

        private void CreateEnvironment()
        {
            var envParent = new GameObject("Environment");
            CreateTown(envParent.transform);
            CreatePerimeter(envParent.transform);
            SpawnLorePickups(envParent.transform);
            
            var landmarksObj = new GameObject("MapLandmarks");
            landmarksObj.transform.SetParent(envParent.transform);
            var landmarks = landmarksObj.AddComponent<Level.MapLandmarks>();
            landmarks.CreateAllLandmarks(envParent.transform);
        }

        private void SpawnLorePickups(Transform parent)
        {
            var loreParent = new GameObject("LorePickups");
            loreParent.transform.SetParent(parent);

            string[] loreIds = { "lab_note_1", "chen_1", "chen_2", "journal_1", "military_1", "chen_3" };
            Vector3[] positions = activeMapConfig != null && activeMapConfig.lorePositions != null
                ? activeMapConfig.lorePositions
                : new[] {
                    new Vector3(-5, 9, 0), new Vector3(7, 9, 0),
                    new Vector3(-9, -3, 0), new Vector3(9, -3, 0),
                    new Vector3(-3, -9, 0), new Vector3(3, -9, 0)
                };

            int count = Mathf.Min(loreIds.Length, positions.Length);
            for (int i = 0; i < count; i++)
            {
                var loreObj = new GameObject($"Lore_{loreIds[i]}");
                loreObj.transform.SetParent(loreParent.transform);
                loreObj.transform.position = positions[i];

                var sr = loreObj.AddComponent<SpriteRenderer>();
                var tex = new Texture2D(8, 8);
                var pixels = new Color[64];
                for (int p = 0; p < 64; p++)
                    pixels[p] = new Color(1f, 0.9f, 0.5f, 0.9f);
                tex.SetPixels(pixels);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
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

        private void CreateTown(Transform parent)
        {
            var town = new GameObject("Town");
            town.transform.SetParent(parent);

            var housePositions = new (Vector3 pos, string sprite, float scale)[] {
                (new Vector3(-6, 8, 0), "House A0", 1.8f),
                (new Vector3(6, 8, 0), "House B0", 1.8f),
                (new Vector3(-8, -4, 0), "House A2", 1.5f),
                (new Vector3(8, -4, 0), "House B2", 1.5f),
                (new Vector3(-4, -8, 0), "House A4", 1.5f),
                (new Vector3(4, -8, 0), "House B4", 1.5f),
            };

            for (int i = 0; i < housePositions.Length; i++)
            {
                var h = housePositions[i];
                var house = new GameObject("House");
                house.transform.SetParent(town.transform);
                house.transform.position = h.pos;
                house.transform.localScale = Vector3.one * h.scale;

                var sr = house.AddComponent<SpriteRenderer>();
                if (useProceduralSprites)
                {
                    sr.sprite = ProceduralSpriteGenerator.CreateBuildingSprite(i % 3);
                }
                else
                {
                    var houseSprite = GetSprite(objectSprites, h.sprite);
                    sr.sprite = houseSprite != null ? houseSprite : CreateRectSprite(new Color(0.5f, 0.4f, 0.3f), new Vector2(2, 2));
                }
                sr.sortingOrder = Mathf.RoundToInt(-h.pos.y);

                var col = house.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.2f, 1.2f);
            }

            var treePositions = new Vector3[] {
                new Vector3(-10, 4, 0), new Vector3(10, 4, 0),
                new Vector3(-10, -2, 0), new Vector3(10, -2, 0),
                new Vector3(-3, 10, 0), new Vector3(3, 10, 0),
                new Vector3(-7, -10, 0), new Vector3(7, -10, 0),
            };

            foreach (var pos in treePositions)
            {
                var tree = new GameObject("Tree");
                tree.transform.SetParent(town.transform);
                tree.transform.position = pos;
                tree.transform.localScale = Vector3.one * Random.Range(1.2f, 2f);

                var sr = tree.AddComponent<SpriteRenderer>();
                if (useProceduralSprites)
                {
                    sr.sprite = ProceduralSpriteGenerator.CreateTreeSprite();
                }
                else
                {
                    var treeSprite = GetSprite(objectSprites, $"Tree {Random.Range(0, 4)}");
                    sr.sprite = treeSprite != null ? treeSprite : CreateCircleSprite(new Color(0.2f, 0.45f, 0.2f));
                }
                sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;

                var col = tree.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
                col.offset = new Vector2(0, -0.3f);
            }

            var rockPositions = new Vector3[] {
                new Vector3(-5, 3, 0), new Vector3(5, 3, 0),
                new Vector3(-3, -5, 0), new Vector3(3, 6, 0),
            };

            foreach (var pos in rockPositions)
            {
                var rock = new GameObject("Rock");
                rock.transform.SetParent(town.transform);
                rock.transform.position = pos;
                rock.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);

                var sr = rock.AddComponent<SpriteRenderer>();
                if (useProceduralSprites)
                {
                    sr.sprite = ProceduralSpriteGenerator.CreateRockSprite();
                }
                else
                {
                    var rockSprite = GetSprite(objectSprites, "Rock");
                    sr.sprite = rockSprite != null ? rockSprite : CreateCircleSprite(Color.gray);
                }
                sr.sortingOrder = Mathf.RoundToInt(-pos.y);

                var col = rock.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
            }

            Vector3[] cratePositions = {
                new Vector3(-3, 4, 0), new Vector3(3, -4, 0),
                new Vector3(2, 5, 0), new Vector3(-2, -3, 0),
            };

            foreach (var pos in cratePositions)
            {
                var crate = new GameObject("Crate");
                crate.transform.SetParent(town.transform);
                crate.transform.position = pos;

                var sr = crate.AddComponent<SpriteRenderer>();
                if (useProceduralSprites)
                {
                    sr.sprite = ProceduralSpriteGenerator.CreateCrateSprite();
                }
                else
                {
                    var boxSprite = GetSprite(objectSprites, "Box");
                    sr.sprite = boxSprite != null ? boxSprite : CreateRectSprite(new Color(0.6f, 0.45f, 0.25f), Vector2.one);
                }
                sr.sortingOrder = Mathf.RoundToInt(-pos.y);

                var col = crate.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.8f);
            }

            if (useProceduralSprites)
            {
                var barrelPositions = new Vector3[] {
                    new Vector3(-4, 6, 0), new Vector3(4, -2, 0),
                    new Vector3(-7, -7, 0), new Vector3(7, 7, 0),
                };

                foreach (var pos in barrelPositions)
                {
                    var barrel = new GameObject("Barrel");
                    barrel.transform.SetParent(town.transform);
                    barrel.transform.position = pos;

                    var sr = barrel.AddComponent<SpriteRenderer>();
                    bool explosive = Random.value > 0.6f;
                    sr.sprite = ProceduralSpriteGenerator.CreateBarrelSprite(explosive);
                    sr.sortingOrder = Mathf.RoundToInt(-pos.y);

                    var col = barrel.AddComponent<CircleCollider2D>();
                    col.radius = 0.25f;
                }

                var carPositions = new Vector3[] {
                    new Vector3(-8, 2, 0), new Vector3(9, -2, 0),
                };

                foreach (var pos in carPositions)
                {
                    var car = new GameObject("Car");
                    car.transform.SetParent(town.transform);
                    car.transform.position = pos;
                    car.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

                    var sr = car.AddComponent<SpriteRenderer>();
                    sr.sprite = ProceduralSpriteGenerator.CreateCarSprite(Random.Range(0, 4));
                    sr.sortingOrder = Mathf.RoundToInt(-pos.y);

                    var col = car.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(1.5f, 0.7f);
                }
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
            var canvas = new GameObject("GameCanvas");
            var canvasComp = canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasComp.sortingOrder = 100;
            var scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvas.AddComponent<GraphicRaycaster>();

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

            var weaponName = CreateUIText(canvas.transform, "WeaponName",
                new Vector2(1, 0), "PISTOL", font, 14, TextAnchor.LowerRight, new Color(1f, 0.9f, 0.6f, 0.7f),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 90), new Vector2(200, 20));

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
            goPanel.transform.SetParent(canvas.transform);
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

            // Fade overlay (starts transparent, will be used for transitions)
            var fadeOverlay = CreateUIImage(canvas.transform, "FadeOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                Color.clear);
            var fadeRect = fadeOverlay.GetComponent<RectTransform>();
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeOverlay.GetComponent<Image>().raycastTarget = false;

            // Objective HUD (upper-left)
            var objPanel = CreateUIPanel(canvas.transform, "ObjectivePanel",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(10, -70), new Vector2(380, 65));
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
