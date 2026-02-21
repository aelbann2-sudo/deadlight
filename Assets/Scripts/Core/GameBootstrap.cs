using UnityEngine;
using Deadlight.Player;
using Deadlight.Enemy;
using Deadlight.Systems;
using Deadlight.UI;

namespace Deadlight.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Spawn Points")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;

        [Header("Auto-Setup")]
        [SerializeField] private bool autoSetupManagers = true;
        [SerializeField] private bool autoSpawnPlayer = true;

        private void Awake()
        {
            if (autoSetupManagers)
            {
                SetupManagers();
            }
        }

        private void Start()
        {
            if (autoSpawnPlayer && playerPrefab != null)
            {
                SpawnPlayer();
            }

            SetupSpawnPoints();
            SetupWaveManager();
        }

        private void SetupManagers()
        {
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            if (FindObjectOfType<DayNightCycle>() == null)
            {
                var dncObj = new GameObject("DayNightCycle");
                dncObj.AddComponent<DayNightCycle>();
            }

            if (GameFlowController.Instance == null)
            {
                var gfcObj = new GameObject("GameFlowController");
                gfcObj.AddComponent<GameFlowController>();
            }

            if (FindObjectOfType<WaveManager>() == null)
            {
                var wmObj = new GameObject("WaveManager");
                wmObj.AddComponent<WaveManager>();
            }

            if (ResourceManager.Instance == null)
            {
                var rmObj = new GameObject("ResourceManager");
                rmObj.AddComponent<ResourceManager>();
            }

            if (PointsSystem.Instance == null)
            {
                var psObj = new GameObject("PointsSystem");
                psObj.AddComponent<PointsSystem>();
            }

            if (ProgressionManager.Instance == null)
            {
                var pmObj = new GameObject("ProgressionManager");
                pmObj.AddComponent<ProgressionManager>();
            }
        }

        private void SpawnPlayer()
        {
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            
            GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            player.tag = "Player";

            var shooting = player.GetComponent<PlayerShooting>();
            if (shooting != null && bulletPrefab != null)
            {
                shooting.SetBulletPrefab(bulletPrefab);

                Transform firePoint = player.transform.Find("FirePoint");
                if (firePoint == null)
                {
                    var fpObj = new GameObject("FirePoint");
                    fpObj.transform.SetParent(player.transform);
                    fpObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                    firePoint = fpObj.transform;
                }
                shooting.SetFirePoint(firePoint);
            }

            var cam = FindObjectOfType<CameraController>();
            if (cam != null)
            {
                cam.SetTarget(player.transform);
            }
        }

        private void SetupSpawnPoints()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                var existingSpawners = FindObjectsOfType<EnemySpawner>();
                if (existingSpawners.Length == 0)
                {
                    CreateDefaultSpawnPoints();
                }
            }
            else
            {
                foreach (var point in enemySpawnPoints)
                {
                    if (point != null && point.GetComponent<EnemySpawner>() == null)
                    {
                        point.gameObject.AddComponent<EnemySpawner>();
                    }
                }
            }
        }

        private void CreateDefaultSpawnPoints()
        {
            Vector2[] defaultPositions = new Vector2[]
            {
                new Vector2(-15, 0),
                new Vector2(15, 0),
                new Vector2(0, -15),
                new Vector2(0, 15),
                new Vector2(-10, -10),
                new Vector2(10, -10),
                new Vector2(-10, 10),
                new Vector2(10, 10)
            };

            GameObject spawnPointsParent = new GameObject("SpawnPoints");

            foreach (var pos in defaultPositions)
            {
                var spawnObj = new GameObject($"SpawnPoint_{pos.x}_{pos.y}");
                spawnObj.transform.SetParent(spawnPointsParent.transform);
                spawnObj.transform.position = new Vector3(pos.x, pos.y, 0);
                spawnObj.AddComponent<EnemySpawner>();
            }
        }

        private void SetupWaveManager()
        {
            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null && zombiePrefab != null)
            {
                waveManager.SetZombiePrefab(zombiePrefab);
            }
        }

        public void SetPlayerPrefab(GameObject prefab)
        {
            playerPrefab = prefab;
        }

        public void SetZombiePrefab(GameObject prefab)
        {
            zombiePrefab = prefab;
        }

        public void SetBulletPrefab(GameObject prefab)
        {
            bulletPrefab = prefab;
        }
    }
}
