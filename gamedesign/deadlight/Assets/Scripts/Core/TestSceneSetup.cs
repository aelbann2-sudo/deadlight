using UnityEngine;
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

        private void CreatePlayer()
        {
            if (GameObject.FindGameObjectWithTag("Player") != null) return;
            
            var playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.position = Vector3.zero;

            var sr = playerObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(Color.green);
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

            Debug.Log("[TestSceneSetup] Managers created");
        }

        private void CreateTestEnemy()
        {
            var enemyObj = new GameObject("TestZombie");
            enemyObj.tag = "Enemy";
            enemyObj.transform.position = new Vector3(5, 3, 0);

            var sr = enemyObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(new Color(0.4f, 0.5f, 0.3f));
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
