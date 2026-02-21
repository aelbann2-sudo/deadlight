using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Deadlight.Level;
using Deadlight.Narrative;
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
        
        private Sprite[] playerSprites;
        private Sprite[] npcSprites;
        private Sprite[] objectSprites;
        private Sprite[] tileSprites;
        
#if UNITY_EDITOR
        [MenuItem("Deadlight/Create Test Scene and Play")]
        public static void CreateTestSceneAndPlay()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            var setupObj = new GameObject("TestSceneSetup");
            setupObj.AddComponent<TestSceneSetup>();
            
            EditorApplication.isPlaying = true;
        }
        
        [MenuItem("Deadlight/Create Test Scene (No Play)")]
        public static void CreateTestSceneNoPlay()
        {
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
            cam.orthographicSize = 7;
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
            
            string[] grassNames = { "Grass 0", "Grass 1", "Grass 2", "Grass 3" };
            Sprite[] grassSprites = new Sprite[grassNames.Length];
            for (int i = 0; i < grassNames.Length; i++)
            {
                grassSprites[i] = GetSprite(tileSprites, grassNames[i]);
            }

            Sprite pathSprite = GetSprite(tileSprites, "Sand 0");
            
            for (int x = -25; x <= 25; x++)
            {
                for (int y = -20; y <= 30; y++)
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

        // ===================== PLAYER =====================

        private void CreatePlayer()
        {
            if (GameObject.Find("Player") != null) return;
            
            var playerObj = new GameObject("Player");
            playerObj.transform.position = Vector3.zero;

            var sr = playerObj.AddComponent<SpriteRenderer>();
            var playerSprite = GetSprite(playerSprites, "Down 0");
            sr.sprite = playerSprite != null ? playerSprite : CreateCircleSprite(Color.green);
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
            playerObj.AddComponent<AudioSource>();
            
            var animator = playerObj.AddComponent<PlayerAnimator>();
            animator.SetSprites(playerSprites);

            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(playerObj.transform);
            firePoint.transform.localPosition = new Vector3(0.5f, 0, 0);

            var shooting = playerObj.GetComponent<Player.PlayerShooting>();
            shooting.SetFirePoint(firePoint.transform);
            CreateBulletPrefab(shooting);
        }

        private void CreateBulletPrefab(Player.PlayerShooting shooting)
        {
            var bulletObj = new GameObject("BulletPrefab");
            bulletObj.SetActive(false);

            var sr = bulletObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateBulletSprite();
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
            if (GameManager.Instance != null) return;

            var managersObj = new GameObject("Managers");

            var gmObj = new GameObject("GameManager");
            gmObj.transform.SetParent(managersObj.transform);
            gmObj.AddComponent<GameManager>();

            var dncObj = new GameObject("DayNightCycle");
            dncObj.transform.SetParent(managersObj.transform);
            dncObj.AddComponent<DayNightCycle>();

            var wsObj = new GameObject("WaveSpawner");
            wsObj.transform.SetParent(managersObj.transform);
            wsObj.AddComponent<WaveSpawner>();

            var rmObj = new GameObject("ResourceManager");
            rmObj.transform.SetParent(managersObj.transform);
            rmObj.AddComponent<Systems.ResourceManager>();

            var psObj = new GameObject("PointsSystem");
            psObj.transform.SetParent(managersObj.transform);
            psObj.AddComponent<Systems.PointsSystem>();

            var lmObj = new GameObject("LevelManager");
            lmObj.transform.SetParent(managersObj.transform);
            lmObj.AddComponent<LevelManager>();

            var nmObj = new GameObject("NarrativeManager");
            nmObj.transform.SetParent(managersObj.transform);
            nmObj.AddComponent<NarrativeManager>();
            nmObj.AddComponent<EnvironmentalLore>();

            var geObj = new GameObject("GameEffects");
            geObj.transform.SetParent(managersObj.transform);
            geObj.AddComponent<GameEffects>();

            var gfObj = new GameObject("GameFlowController");
            gfObj.transform.SetParent(managersObj.transform);
            gfObj.AddComponent<GameFlowController>();

            var rtObj = new GameObject("RadioTransmissions");
            rtObj.transform.SetParent(managersObj.transform);
            rtObj.AddComponent<RadioTransmissions>();

            var guiObj = new GameObject("GameUI");
            guiObj.transform.SetParent(managersObj.transform);
            guiObj.AddComponent<Deadlight.UI.GameUI>();
        }

        // ===================== ENVIRONMENT =====================

        private void CreateEnvironment()
        {
            var envParent = new GameObject("Environment");
            CreateTown(envParent.transform);
            CreatePerimeter(envParent.transform);
        }

        private void CreateTown(Transform parent)
        {
            var town = new GameObject("Town");
            town.transform.SetParent(parent);

            var housePositions = new (Vector3 pos, string sprite, float scale)[] {
                (new Vector3(-8, 10, 0), "House A0", 2.5f),
                (new Vector3(8, 10, 0), "House B0", 2.5f),
                (new Vector3(-12, -6, 0), "House A2", 2f),
                (new Vector3(12, -6, 0), "House B2", 2f),
                (new Vector3(-6, -12, 0), "House A4", 2f),
                (new Vector3(6, -12, 0), "House B4", 2f),
                (new Vector3(0, 18, 0), "House A0", 3f),
            };

            foreach (var h in housePositions)
            {
                var house = new GameObject("House");
                house.transform.SetParent(town.transform);
                house.transform.position = h.pos;
                house.transform.localScale = Vector3.one * h.scale;

                var sr = house.AddComponent<SpriteRenderer>();
                var houseSprite = GetSprite(objectSprites, h.sprite);
                sr.sprite = houseSprite != null ? houseSprite : CreateRectSprite(new Color(0.5f, 0.4f, 0.3f), new Vector2(2, 2));
                sr.sortingOrder = Mathf.RoundToInt(-h.pos.y);

                var col = house.AddComponent<BoxCollider2D>();
                col.size = new Vector2(1.2f, 1.2f);
            }

            var treePositions = new Vector3[] {
                new Vector3(-15, 5, 0), new Vector3(15, 5, 0),
                new Vector3(-18, -2, 0), new Vector3(18, -2, 0),
                new Vector3(-4, 14, 0), new Vector3(4, 14, 0),
                new Vector3(-10, -15, 0), new Vector3(10, -15, 0),
                new Vector3(-20, 10, 0), new Vector3(20, 10, 0),
                new Vector3(-14, 18, 0), new Vector3(14, 18, 0),
                new Vector3(-22, -8, 0), new Vector3(22, -8, 0),
            };

            foreach (var pos in treePositions)
            {
                var tree = new GameObject("Tree");
                tree.transform.SetParent(town.transform);
                tree.transform.position = pos;
                tree.transform.localScale = Vector3.one * Random.Range(1.2f, 2f);

                var sr = tree.AddComponent<SpriteRenderer>();
                var treeSprite = GetSprite(objectSprites, $"Tree {Random.Range(0, 4)}");
                sr.sprite = treeSprite != null ? treeSprite : CreateCircleSprite(new Color(0.2f, 0.45f, 0.2f));
                sr.sortingOrder = Mathf.RoundToInt(-pos.y) + 1;

                var col = tree.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
                col.offset = new Vector2(0, -0.3f);
            }

            var rockPositions = new Vector3[] {
                new Vector3(-7, 3, 0), new Vector3(7, 3, 0),
                new Vector3(-3, -7, 0), new Vector3(3, 8, 0),
                new Vector3(-16, 15, 0), new Vector3(16, -12, 0)
            };

            foreach (var pos in rockPositions)
            {
                var rock = new GameObject("Rock");
                rock.transform.SetParent(town.transform);
                rock.transform.position = pos;
                rock.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);

                var sr = rock.AddComponent<SpriteRenderer>();
                var rockSprite = GetSprite(objectSprites, "Rock");
                sr.sprite = rockSprite != null ? rockSprite : CreateCircleSprite(Color.gray);
                sr.sortingOrder = Mathf.RoundToInt(-pos.y);

                var col = rock.AddComponent<CircleCollider2D>();
                col.radius = 0.3f;
            }

            Vector3[] cratePositions = {
                new Vector3(-4, 5, 0), new Vector3(4, -5, 0),
                new Vector3(2, 6, 0), new Vector3(-3, -4, 0),
            };

            foreach (var pos in cratePositions)
            {
                var crate = new GameObject("Crate");
                crate.transform.SetParent(town.transform);
                crate.transform.position = pos;

                var sr = crate.AddComponent<SpriteRenderer>();
                var boxSprite = GetSprite(objectSprites, "Box");
                sr.sprite = boxSprite != null ? boxSprite : CreateRectSprite(new Color(0.6f, 0.45f, 0.25f), Vector2.one);
                sr.sortingOrder = Mathf.RoundToInt(-pos.y);

                var col = crate.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.8f, 0.8f);
            }
        }

        private void CreatePerimeter(Transform parent)
        {
            var perimeter = new GameObject("Perimeter");
            perimeter.transform.SetParent(parent);
            
            float halfW = 24f, halfH = 19f;
            float thickness = 2f;

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

            Vector3[] positions = {
                new Vector3(10, 8, 0),
                new Vector3(-10, 8, 0),
                new Vector3(12, -8, 0),
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
            var enemySprite = GetSprite(npcSprites, spriteName);
            sr.sprite = enemySprite != null ? enemySprite : CreateCircleSprite(new Color(0.4f, 0.5f, 0.3f));
            sr.sortingOrder = 9;
            sr.color = new Color(0.65f, 0.75f, 0.55f);

            var rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = enemyObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            enemyObj.AddComponent<Enemy.EnemyHealth>();
            enemyObj.AddComponent<Enemy.SimpleEnemyAI>();
            enemyObj.AddComponent<EnemyHealthBar>();
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

            // Ammo
            var ammoText = CreateUIText(canvas.transform, "AmmoText",
                new Vector2(0, 1), "15 / 60", font, 22, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -78), new Vector2(200, 30));

            var ammoIcon = CreateUIText(canvas.transform, "AmmoIcon",
                new Vector2(1, 0.5f), ">>", font, 16, TextAnchor.MiddleRight, new Color(1f, 0.9f, 0.4f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -78), new Vector2(50, 30));

            // Night & Wave (top center)
            var nightText = CreateUIText(canvas.transform, "NightText",
                new Vector2(0.5f, 1), "Night 1", font, 28, TextAnchor.UpperCenter, new Color(0.9f, 0.8f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(200, 35));
            nightText.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var waveText = CreateUIText(canvas.transform, "WaveText",
                new Vector2(0.5f, 1), "", font, 18, TextAnchor.UpperCenter, new Color(0.8f, 0.6f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(200, 25));

            // Enemy count (top right)
            var enemyCount = CreateUIText(canvas.transform, "EnemyCount",
                new Vector2(1, 1), "Enemies: 0", font, 18, TextAnchor.UpperRight, new Color(0.8f, 0.5f, 0.5f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(200, 30));

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
                new Vector2(1, 0), "WASD - Move    Mouse - Aim    Click - Shoot    Shift - Sprint    R - Reload",
                font, 13, TextAnchor.LowerRight, new Color(1, 1, 1, 0.5f),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 15), new Vector2(600, 25));

            // Day timer
            var dayTimerText = CreateUIText(canvas.transform, "DayTimer",
                new Vector2(0.5f, 1), "", font, 20, TextAnchor.UpperCenter, new Color(1f, 0.9f, 0.6f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -75), new Vector2(200, 25));

            // Points display
            var pointsText = CreateUIText(canvas.transform, "PointsDisplay",
                new Vector2(1, 1), "Score: 0", font, 20, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.3f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -50), new Vector2(200, 25));

            // Radio transmission panel
            var radioPanel = CreateUIPanel(canvas.transform, "RadioPanel",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(20, 60), new Vector2(600, 60));
            var radioBg = radioPanel.AddComponent<Image>();
            radioBg.color = Color.clear;
            
            var radioText = CreateUIText(radioPanel.transform, "RadioText",
                new Vector2(0, 0.5f), "", font, 16, TextAnchor.MiddleLeft, new Color(0.3f, 1f, 0.3f),
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 0), new Vector2(-20, 0));
            radioText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 5);
            radioText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -5);
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

            // Fade overlay
            var fadeOverlay = CreateUIImage(canvas.transform, "FadeOverlay",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                Color.black);
            var fadeRect = fadeOverlay.GetComponent<RectTransform>();
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeOverlay.GetComponent<Image>().raycastTarget = false;

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
                GameEffects.Instance.FadeScreen(true, 1.5f);
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
        
        private float animTimer;
        private int currentFrame;
        private string currentDirection = "Down";

        public void SetSprites(Sprite[] playerSprites)
        {
            sprites = playerSprites;
        }

        private void Start()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (sprites == null || sprites.Length == 0) return;
            UpdateDirection();
            UpdateAnimation();
        }

        private void UpdateDirection()
        {
            Vector2 velocity = rb.linearVelocity;
            if (velocity.magnitude < 0.1f) return;

            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                currentDirection = velocity.x > 0 ? "Right" : "Left";
            else
                currentDirection = velocity.y > 0 ? "Up" : "Down";
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
