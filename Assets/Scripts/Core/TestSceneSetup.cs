using UnityEngine;
using UnityEngine.UI;
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
            Debug.Log("[TestSceneSetup] Setting up test scene...");
            
            LoadAllSprites();
            CreateCamera();
            CreateGround();
            CreatePlayer();
            CreateManagers();
            CreateEnvironment();
            CreateEnemies();
            CreateHUD();
            
            Debug.Log("[TestSceneSetup] Test scene ready!");
            Debug.Log("Controls: WASD=Move, Mouse=Aim, Click=Shoot, Shift=Sprint, R=Reload");
        }

        private void LoadAllSprites()
        {
            playerSprites = Resources.LoadAll<Sprite>("Player");
            npcSprites = Resources.LoadAll<Sprite>("NPC");
            objectSprites = Resources.LoadAll<Sprite>("Objects");
            tileSprites = Resources.LoadAll<Sprite>("Tiles");
            
            Debug.Log($"[TestSceneSetup] Loaded sprites - Player:{playerSprites?.Length ?? 0}, NPC:{npcSprites?.Length ?? 0}, Objects:{objectSprites?.Length ?? 0}, Tiles:{tileSprites?.Length ?? 0}");
        }

        private Sprite GetSprite(Sprite[] sprites, string name)
        {
            if (sprites == null) return null;
            foreach (var sprite in sprites)
            {
                if (sprite.name == name)
                    return sprite;
            }
            return null;
        }

        private void CreateCamera()
        {
            if (Camera.main != null) return;
            
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7;
            cam.backgroundColor = new Color(0.15f, 0.18f, 0.12f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CameraController>();
            camObj.transform.position = new Vector3(0, 0, -10);
        }

        private void CreateGround()
        {
            var groundParent = new GameObject("Ground");
            
            var groundSprite = GetSprite(tileSprites, "Grass 0");
            if (groundSprite == null)
            {
                groundSprite = CreateRectSprite(new Color(0.2f, 0.25f, 0.15f), new Vector2(1, 1));
            }
            
            for (int x = -20; x <= 20; x += 2)
            {
                for (int y = -15; y <= 25; y += 2)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.SetParent(groundParent.transform);
                    tile.transform.position = new Vector3(x, y, 0);
                    
                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = groundSprite;
                    sr.sortingOrder = -200;
                    sr.color = new Color(
                        0.85f + Random.Range(-0.1f, 0.1f),
                        0.9f + Random.Range(-0.1f, 0.1f),
                        0.85f + Random.Range(-0.1f, 0.1f)
                    );
                }
            }
        }

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

            var controller = playerObj.AddComponent<Player.PlayerController>();
            var shooting = playerObj.AddComponent<Player.PlayerShooting>();
            var health = playerObj.AddComponent<Player.PlayerHealth>();
            playerObj.AddComponent<AudioSource>();
            
            var animator = playerObj.AddComponent<PlayerAnimator>();
            animator.SetSprites(playerSprites);

            var firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(playerObj.transform);
            firePoint.transform.localPosition = new Vector3(0.5f, 0, 0);

            shooting.SetFirePoint(firePoint.transform);
            CreateBulletPrefab(shooting);

            Debug.Log("[TestSceneSetup] Player created with animations");
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
            trail.time = 0.1f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = new Color(1f, 0.9f, 0.3f, 0.8f);
            trail.endColor = new Color(1f, 0.5f, 0.2f, 0f);

            shooting.SetBulletPrefab(bulletObj);

            var weaponData = ScriptableObject.CreateInstance<Data.WeaponData>();
            weaponData.weaponName = "Pistol";
            weaponData.damage = 25f;
            weaponData.fireRate = 0.25f;
            weaponData.magazineSize = 12;
            weaponData.reloadTime = 1.5f;
            weaponData.bulletSpeed = 25f;
            weaponData.range = 30f;
            weaponData.spread = 3f;
            weaponData.isAutomatic = false;

            shooting.SetWeapon(weaponData);
        }

        private Sprite CreateBulletSprite()
        {
            int width = 16;
            int height = 8;
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distFromCenter = Mathf.Abs(y - height / 2f) / (height / 2f);
                    float xFade = (float)x / width;
                    
                    Color bulletColor = Color.Lerp(
                        new Color(1f, 0.9f, 0.3f),
                        new Color(1f, 0.6f, 0.2f),
                        distFromCenter
                    );
                    bulletColor.a = xFade > 0.7f ? 1f : xFade + 0.3f;
                    
                    float edgeDist = Mathf.Min(y, height - 1 - y) / 2f;
                    if (edgeDist < 1) bulletColor.a *= edgeDist;
                    
                    pixels[y * width + x] = bulletColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 16f);
        }

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

            var wmObj = new GameObject("WaveManager");
            wmObj.transform.SetParent(managersObj.transform);
            wmObj.AddComponent<WaveManager>();

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

            Debug.Log("[TestSceneSetup] Managers created");
        }

        private void CreateEnvironment()
        {
            var envParent = new GameObject("Environment");

            CreateSafeZone(envParent.transform);
            CreateTownArea(envParent.transform);
            CreateSpawnPoints(envParent.transform);
            CreateObstacles(envParent.transform);

            Debug.Log("[TestSceneSetup] Environment created");
        }

        private void CreateSafeZone(Transform parent)
        {
            var safeZone = new GameObject("SafeZone");
            safeZone.transform.SetParent(parent);
            safeZone.transform.position = Vector3.zero;
            safeZone.AddComponent<MapZone>();
            
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                Vector3 pos = Quaternion.Euler(0, 0, angle) * Vector3.right * 5f;
                CreateBarricade(safeZone.transform, pos, angle == 0 || angle == 180);
            }
        }

        private void CreateBarricade(Transform parent, Vector3 position, bool horizontal)
        {
            var barricade = new GameObject("Barricade");
            barricade.transform.SetParent(parent);
            barricade.transform.position = position;
            
            var sr = barricade.AddComponent<SpriteRenderer>();
            var boxSprite = GetSprite(objectSprites, "Box");
            sr.sprite = boxSprite != null ? boxSprite : CreateRectSprite(new Color(0.5f, 0.35f, 0.2f), new Vector2(1, 1));
            sr.sortingOrder = 5;
            sr.color = new Color(0.8f, 0.6f, 0.4f);
            
            var col = barricade.AddComponent<BoxCollider2D>();
            col.size = horizontal ? new Vector2(3f, 0.5f) : new Vector2(0.5f, 3f);
            
            barricade.transform.localScale = horizontal ? new Vector3(3f, 1f, 1f) : new Vector3(1f, 3f, 1f);
        }

        private void CreateTownArea(Transform parent)
        {
            var town = new GameObject("TownArea");
            town.transform.SetParent(parent);

            Vector3[] housePositions = {
                new Vector3(-12, 8, 0),
                new Vector3(12, 8, 0),
                new Vector3(-10, -8, 0),
                new Vector3(10, -8, 0),
                new Vector3(0, 15, 0)
            };

            for (int i = 0; i < housePositions.Length; i++)
            {
                CreateHouse(town.transform, housePositions[i], i);
            }

            Vector3[] treePositions = {
                new Vector3(-8, 5, 0), new Vector3(8, 5, 0),
                new Vector3(-15, 0, 0), new Vector3(15, 0, 0),
                new Vector3(-6, 12, 0), new Vector3(6, 12, 0),
                new Vector3(-3, -10, 0), new Vector3(3, -10, 0)
            };

            foreach (var pos in treePositions)
            {
                CreateTree(town.transform, pos);
            }

            Vector3[] rockPositions = {
                new Vector3(-5, 8, 0), new Vector3(5, 8, 0),
                new Vector3(-12, -3, 0), new Vector3(12, -3, 0)
            };

            foreach (var pos in rockPositions)
            {
                CreateRock(town.transform, pos);
            }
        }

        private void CreateHouse(Transform parent, Vector3 position, int variant)
        {
            var house = new GameObject($"House_{variant}");
            house.transform.SetParent(parent);
            house.transform.position = position;

            var sr = house.AddComponent<SpriteRenderer>();
            string spriteName = variant % 2 == 0 ? "House A0" : "House B0";
            var houseSprite = GetSprite(objectSprites, spriteName);
            sr.sprite = houseSprite != null ? houseSprite : CreateRectSprite(new Color(0.6f, 0.5f, 0.4f), new Vector2(3, 3));
            sr.sortingOrder = 2;
            
            var col = house.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.5f, 2.5f);
            
            house.transform.localScale = Vector3.one * 2f;
        }

        private void CreateTree(Transform parent, Vector3 position)
        {
            var tree = new GameObject("Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;

            var sr = tree.AddComponent<SpriteRenderer>();
            int treeVariant = Random.Range(0, 4);
            var treeSprite = GetSprite(objectSprites, $"Tree {treeVariant}");
            sr.sprite = treeSprite != null ? treeSprite : CreateCircleSprite(new Color(0.2f, 0.5f, 0.2f));
            sr.sortingOrder = (int)(-position.y);
            
            var col = tree.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.offset = new Vector2(0, -0.3f);
            
            tree.transform.localScale = Vector3.one * 1.5f;
        }

        private void CreateRock(Transform parent, Vector3 position)
        {
            var rock = new GameObject("Rock");
            rock.transform.SetParent(parent);
            rock.transform.position = position;

            var sr = rock.AddComponent<SpriteRenderer>();
            var rockSprite = GetSprite(objectSprites, "Rock");
            sr.sprite = rockSprite != null ? rockSprite : CreateCircleSprite(new Color(0.5f, 0.5f, 0.5f));
            sr.sortingOrder = 1;
            
            var col = rock.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
        }

        private void CreateSpawnPoints(Transform parent)
        {
            var spawnsObj = new GameObject("SpawnPoints");
            spawnsObj.transform.SetParent(parent);

            Vector3[] spawnPositions = {
                new Vector3(-18, 12, 0),
                new Vector3(18, 12, 0),
                new Vector3(-18, -10, 0),
                new Vector3(18, -10, 0),
                new Vector3(0, 20, 0),
                new Vector3(-20, 0, 0),
                new Vector3(20, 0, 0)
            };

            for (int i = 0; i < spawnPositions.Length; i++)
            {
                var spawnObj = new GameObject($"SpawnPoint_{i}");
                spawnObj.transform.SetParent(spawnsObj.transform);
                spawnObj.transform.position = spawnPositions[i];
                spawnObj.AddComponent<SpawnPoint>();
            }
        }

        private void CreateObstacles(Transform parent)
        {
            var obstaclesObj = new GameObject("Obstacles");
            obstaclesObj.transform.SetParent(parent);

            Vector3[] wallPositions = {
                new Vector3(-7, 0, 0),
                new Vector3(7, 0, 0),
                new Vector3(0, 8, 0)
            };

            foreach (var pos in wallPositions)
            {
                var wall = new GameObject("Wall");
                wall.transform.SetParent(obstaclesObj.transform);
                wall.transform.position = pos;
                
                var sr = wall.AddComponent<SpriteRenderer>();
                sr.sprite = CreateRectSprite(new Color(0.4f, 0.35f, 0.3f), new Vector2(0.5f, 2f));
                sr.sortingOrder = 3;
                
                var col = wall.AddComponent<BoxCollider2D>();
                col.size = new Vector2(0.5f, 2f);
            }
        }

        private void CreateEnemies()
        {
            var enemiesParent = new GameObject("Enemies");

            Vector3[] enemyPositions = {
                new Vector3(8, 6, 0),
                new Vector3(-8, 6, 0),
                new Vector3(10, -5, 0),
                new Vector3(-10, -5, 0),
                new Vector3(0, 12, 0)
            };

            string[] npcVariants = { "TopDown_NPC_0", "TopDown_NPC_1", "TopDown_NPC_2", "TopDown_NPC_3" };

            for (int i = 0; i < enemyPositions.Length; i++)
            {
                CreateEnemy(enemiesParent.transform, enemyPositions[i], npcVariants[i % npcVariants.Length], i);
            }

            Debug.Log($"[TestSceneSetup] Created {enemyPositions.Length} enemies");
        }

        private void CreateEnemy(Transform parent, Vector3 position, string spriteName, int id)
        {
            var enemyObj = new GameObject($"Zombie_{id}");
            enemyObj.transform.SetParent(parent);
            enemyObj.transform.position = position;

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            var enemySprite = GetSprite(npcSprites, spriteName);
            sr.sprite = enemySprite != null ? enemySprite : CreateCircleSprite(new Color(0.4f, 0.5f, 0.3f));
            sr.sortingOrder = 9;
            sr.color = new Color(0.7f, 0.8f, 0.6f);

            var rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = enemyObj.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            enemyObj.AddComponent<Enemy.EnemyHealth>();
            enemyObj.AddComponent<Enemy.SimpleEnemyAI>();
        }

        private void CreateHUD()
        {
            var canvas = new GameObject("GameCanvas");
            var canvasComp = canvas.AddComponent<Canvas>();
            canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            CreateHealthBar(canvas.transform);
            CreateAmmoDisplay(canvas.transform);
            CreateNightDisplay(canvas.transform);
            CreateControlsHint(canvas.transform);

            Debug.Log("[TestSceneSetup] HUD created");
        }

        private void CreateHealthBar(Transform parent)
        {
            var healthBarBg = new GameObject("HealthBarBG");
            healthBarBg.transform.SetParent(parent);
            var bgRect = healthBarBg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchoredPosition = new Vector2(20, -20);
            bgRect.sizeDelta = new Vector2(200, 25);
            
            var bgImage = healthBarBg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var healthBar = new GameObject("HealthBar");
            healthBar.transform.SetParent(healthBarBg.transform);
            var hbRect = healthBar.AddComponent<RectTransform>();
            hbRect.anchorMin = Vector2.zero;
            hbRect.anchorMax = Vector2.one;
            hbRect.offsetMin = new Vector2(3, 3);
            hbRect.offsetMax = new Vector2(-3, -3);
            
            var hbImage = healthBar.AddComponent<Image>();
            hbImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var label = new GameObject("HealthLabel");
            label.transform.SetParent(healthBarBg.transform);
            var labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            
            var text = label.AddComponent<Text>();
            text.text = "HEALTH";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        private void CreateAmmoDisplay(Transform parent)
        {
            var ammoDisplay = new GameObject("AmmoDisplay");
            ammoDisplay.transform.SetParent(parent);
            var rect = ammoDisplay.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -55);
            rect.sizeDelta = new Vector2(150, 30);
            
            var text = ammoDisplay.AddComponent<Text>();
            text.text = "AMMO: 12 / 12";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
        }

        private void CreateNightDisplay(Transform parent)
        {
            var nightDisplay = new GameObject("NightDisplay");
            nightDisplay.transform.SetParent(parent);
            var rect = nightDisplay.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(200, 40);
            
            var text = nightDisplay.AddComponent<Text>();
            text.text = "NIGHT 1";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.9f, 0.8f, 0.6f);
            text.fontStyle = FontStyle.Bold;
        }

        private void CreateControlsHint(Transform parent)
        {
            var hint = new GameObject("ControlsHint");
            hint.transform.SetParent(parent);
            var rect = hint.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-20, 20);
            rect.sizeDelta = new Vector2(250, 100);
            
            var text = hint.AddComponent<Text>();
            text.text = "WASD - Move\nMouse - Aim\nClick - Shoot\nShift - Sprint\nR - Reload";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.LowerRight;
            text.color = new Color(1f, 1f, 1f, 0.7f);
        }

        private Sprite CreateRectSprite(Color color, Vector2 size)
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(size.x * 16));
            int height = Mathf.Max(1, Mathf.RoundToInt(size.y * 16));
            
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
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
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = dist < radius ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
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
        
        private readonly string[] directions = { "Down", "Up", "Left", "Right" };

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
            {
                currentDirection = velocity.x > 0 ? "Right" : "Left";
            }
            else
            {
                currentDirection = velocity.y > 0 ? "Up" : "Down";
            }
        }

        private void UpdateAnimation()
        {
            Vector2 velocity = rb.linearVelocity;
            bool isMoving = velocity.magnitude > 0.1f;

            if (isMoving)
            {
                animTimer += Time.deltaTime * 8f;
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
            Sprite targetSprite = null;
            
            foreach (var sprite in sprites)
            {
                if (sprite.name == spriteName)
                {
                    targetSprite = sprite;
                    break;
                }
            }

            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }
    }
}
