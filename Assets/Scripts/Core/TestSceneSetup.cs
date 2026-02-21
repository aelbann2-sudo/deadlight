using UnityEngine;
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
            
            CreateCamera();
            CreatePlayer();
            CreateManagers();
            CreateTestLevel();
            CreateTestEnemy();
            
            Debug.Log("[TestSceneSetup] Test scene ready! Use WASD to move, mouse to aim, click to shoot.");
        }

        private void CreateCamera()
        {
            if (Camera.main != null) return;
            
            var camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8;
            cam.backgroundColor = new Color(0.2f, 0.3f, 0.4f);
            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<CameraController>();
            camObj.transform.position = new Vector3(0, 0, -10);
        }

        private Sprite LoadSprite(string resourceName, string spriteName)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourceName);
            foreach (var sprite in sprites)
            {
                if (sprite.name == spriteName)
                    return sprite;
            }
            Debug.LogWarning($"[TestSceneSetup] Sprite '{spriteName}' not found in '{resourceName}'");
            return null;
        }

        private void CreatePlayer()
        {
            if (GameObject.Find("Player") != null) return;
            
            var playerObj = new GameObject("Player");
            playerObj.transform.position = Vector3.zero;

            var sr = playerObj.AddComponent<SpriteRenderer>();
            var playerSprite = LoadSprite("Player", "Down 0");
            sr.sprite = playerSprite != null ? playerSprite : CreateCircleSprite(Color.green);
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

            var shooting = playerObj.GetComponent<Player.PlayerShooting>();
            shooting.SetFirePoint(firePoint.transform);
            
            CreateBulletPrefab(shooting);

            Debug.Log("[TestSceneSetup] Player created");
        }

        private void CreateBulletPrefab(Player.PlayerShooting shooting)
        {
            var bulletObj = new GameObject("BulletPrefab");
            bulletObj.SetActive(false);

            var sr = bulletObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(Color.yellow);
            sr.sortingOrder = 5;
            bulletObj.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

            var rb = bulletObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;

            var col = bulletObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            col.isTrigger = true;

            bulletObj.AddComponent<Player.Bullet>();

            shooting.SetBulletPrefab(bulletObj);

            var weaponData = ScriptableObject.CreateInstance<Data.WeaponData>();
            weaponData.weaponName = "Pistol";
            weaponData.damage = 25f;
            weaponData.fireRate = 0.3f;
            weaponData.magazineSize = 12;
            weaponData.reloadTime = 1.2f;
            weaponData.bulletSpeed = 20f;
            weaponData.range = 30f;
            weaponData.spread = 2f;
            weaponData.isAutomatic = false;

            shooting.SetWeapon(weaponData);
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

        private void CreateTestLevel()
        {
            var levelObj = new GameObject("TestLevel");

            CreateZone(levelObj.transform, "SafeZone", ZoneType.SafeZone, Vector3.zero, new Vector2(12, 12), Color.green);
            CreateZone(levelObj.transform, "DangerZone_North", ZoneType.DangerZone, new Vector3(0, 15, 0), new Vector2(20, 10), Color.red);
            CreateZone(levelObj.transform, "ResourceZone_East", ZoneType.ResourceZone, new Vector3(12, 0, 0), new Vector2(8, 8), Color.yellow);
            CreateZone(levelObj.transform, "ResourceZone_West", ZoneType.ResourceZone, new Vector3(-12, 0, 0), new Vector2(8, 8), Color.yellow);

            CreateSpawnPointObj(levelObj.transform, new Vector3(15, 10, 0), 1);
            CreateSpawnPointObj(levelObj.transform, new Vector3(-15, 10, 0), 1);
            CreateSpawnPointObj(levelObj.transform, new Vector3(0, 20, 0), 2);
            CreateSpawnPointObj(levelObj.transform, new Vector3(18, 0, 0), 3);
            CreateSpawnPointObj(levelObj.transform, new Vector3(-18, 0, 0), 3);

            Debug.Log("[TestSceneSetup] Test level created with zones and spawn points");
        }

        private void CreateZone(Transform parent, string name, ZoneType type, Vector3 position, Vector2 size, Color color)
        {
            var zoneObj = new GameObject($"Zone_{name}");
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.position = position;

            var zone = zoneObj.AddComponent<MapZone>();

            var visualObj = new GameObject("Visual");
            visualObj.transform.SetParent(zoneObj.transform);
            visualObj.transform.localPosition = Vector3.zero;

            var sr = visualObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite(color, size);
            sr.sortingOrder = -100;
            color.a = 0.15f;
            sr.color = color;
        }

        private void CreateSpawnPointObj(Transform parent, Vector3 position, int activationNight)
        {
            var spawnObj = new GameObject($"SpawnPoint_N{activationNight}");
            spawnObj.transform.SetParent(parent);
            spawnObj.transform.position = position;
            spawnObj.AddComponent<SpawnPoint>();
        }

        private Sprite CreateRectSprite(Color color, Vector2 size)
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(size.x * 10));
            int height = Mathf.Max(1, Mathf.RoundToInt(size.y * 10));
            
            var texture = new Texture2D(width, height);
            var pixels = new Color[width * height];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 10f);
        }

        private void CreateTestEnemy()
        {
            var enemyObj = new GameObject("TestZombie");
            enemyObj.transform.position = new Vector3(5, 3, 0);

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            var enemySprite = LoadSprite("NPC", "TopDown_NPC_0");
            sr.sprite = enemySprite != null ? enemySprite : CreateCircleSprite(new Color(0.4f, 0.5f, 0.3f));
            sr.sortingOrder = 9;

            var rb = enemyObj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = enemyObj.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            enemyObj.AddComponent<Enemy.EnemyHealth>();

            Debug.Log("[TestSceneSetup] Test enemy created at (5, 3)");
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
}
