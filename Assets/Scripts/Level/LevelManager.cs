using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Deadlight.Level
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Level Configuration")]
        [SerializeField] private string levelName = "Abandoned Town";
        [SerializeField] private Vector2 levelBounds = new Vector2(50f, 50f);
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("Zones")]
        [SerializeField] private List<MapZone> zones = new List<MapZone>();
        [SerializeField] private MapZone safeZone;
        [SerializeField] private MapZone shopZone;
        
        [Header("Spawn Points")]
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        
        [Header("Obstacles")]
        [SerializeField] private List<Obstacle> obstacles = new List<Obstacle>();
        
        [Header("Level Objects")]
        [SerializeField] private Transform levelRoot;

        public string LevelName => levelName;
        public Vector2 LevelBounds => levelBounds;
        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(levelBounds.x, levelBounds.y, 1f));
        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public MapZone SafeZone => safeZone;
        public MapZone ShopZone => shopZone;
        public IReadOnlyList<MapZone> Zones => zones;
        public IReadOnlyList<SpawnPoint> SpawnPoints => spawnPoints;
        public IReadOnlyList<Obstacle> Obstacles => obstacles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CollectLevelObjects();
        }

        private void Start()
        {
            ValidateLevel();
        }

        private void CollectLevelObjects()
        {
            if (zones.Count == 0)
            {
                zones = FindObjectsOfType<MapZone>().ToList();
            }

            if (spawnPoints.Count == 0)
            {
                spawnPoints = FindObjectsOfType<SpawnPoint>().ToList();
            }

            if (obstacles.Count == 0)
            {
                obstacles = FindObjectsOfType<Obstacle>().ToList();
            }

            if (safeZone == null)
            {
                safeZone = zones.FirstOrDefault(z => z.Type == ZoneType.SafeZone);
            }

            if (shopZone == null)
            {
                shopZone = zones.FirstOrDefault(z => z.IsShopLocation);
            }
        }

        private void ValidateLevel()
        {
            if (playerSpawnPoint == null)
            {
                Debug.LogWarning($"[LevelManager] No player spawn point set for level '{levelName}'");
                
                if (safeZone != null)
                {
                    var spawnObj = new GameObject("PlayerSpawn");
                    spawnObj.transform.position = safeZone.transform.position;
                    playerSpawnPoint = spawnObj.transform;
                }
            }

            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning($"[LevelManager] No spawn points found in level '{levelName}'");
            }

            Debug.Log($"[LevelManager] Level '{levelName}' initialized with {zones.Count} zones, {spawnPoints.Count} spawn points, {obstacles.Count} obstacles");
        }

        public List<SpawnPoint> GetActiveSpawnPoints(int currentNight)
        {
            return spawnPoints.Where(sp => sp.IsActiveForNight(currentNight) && sp.CanSpawn).ToList();
        }

        public SpawnPoint GetRandomSpawnPoint(int currentNight, Vector3 playerPosition)
        {
            var activePoints = GetActiveSpawnPoints(currentNight)
                .Where(sp => sp.IsValidSpawnPosition(playerPosition))
                .ToList();

            if (activePoints.Count == 0) return null;

            float totalWeight = activePoints.Sum(sp => sp.SpawnWeight);
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var point in activePoints)
            {
                currentWeight += point.SpawnWeight;
                if (randomValue <= currentWeight)
                {
                    return point;
                }
            }

            return activePoints[activePoints.Count - 1];
        }

        public SpawnPoint GetSpawnPointInZone(MapZone zone, int currentNight)
        {
            var zonePoints = spawnPoints
                .Where(sp => sp.GetParentZone() == zone && sp.IsActiveForNight(currentNight) && sp.CanSpawn)
                .ToList();

            if (zonePoints.Count == 0) return null;
            return zonePoints[Random.Range(0, zonePoints.Count)];
        }

        public MapZone GetZoneAtPosition(Vector3 position)
        {
            foreach (var zone in zones)
            {
                if (zone.ContainsPoint(position))
                {
                    return zone;
                }
            }
            return null;
        }

        public List<MapZone> GetZonesByType(ZoneType type)
        {
            return zones.Where(z => z.Type == type).ToList();
        }

        public Vector3 GetRandomPositionInLevel()
        {
            float x = Random.Range(-levelBounds.x / 2f, levelBounds.x / 2f);
            float y = Random.Range(-levelBounds.y / 2f, levelBounds.y / 2f);
            return transform.position + new Vector3(x, y, 0);
        }

        public Vector3 ClampToLevelBounds(Vector3 position)
        {
            float halfWidth = levelBounds.x / 2f;
            float halfHeight = levelBounds.y / 2f;

            position.x = Mathf.Clamp(position.x, transform.position.x - halfWidth, transform.position.x + halfWidth);
            position.y = Mathf.Clamp(position.y, transform.position.y - halfHeight, transform.position.y + halfHeight);

            return position;
        }

        public bool IsWithinLevelBounds(Vector3 position)
        {
            return WorldBounds.Contains(position);
        }

        public void ResetAllSpawnPoints()
        {
            foreach (var sp in spawnPoints)
            {
                sp.ResetSpawnCount();
            }
        }

        public void RegisterZone(MapZone zone)
        {
            if (!zones.Contains(zone))
            {
                zones.Add(zone);
            }
        }

        public void RegisterSpawnPoint(SpawnPoint point)
        {
            if (!spawnPoints.Contains(point))
            {
                spawnPoints.Add(point);
            }
        }

        public void RegisterObstacle(Obstacle obstacle)
        {
            if (!obstacles.Contains(obstacle))
            {
                obstacles.Add(obstacle);
                obstacle.OnDestroyed += HandleObstacleDestroyed;
            }
        }

        private void HandleObstacleDestroyed(Obstacle obstacle)
        {
            obstacles.Remove(obstacle);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
            Gizmos.DrawCube(transform.position, new Vector3(levelBounds.x, levelBounds.y, 0.1f));

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(levelBounds.x, levelBounds.y, 0.1f));

            if (playerSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(playerSpawnPoint.position, 0.5f);
                Gizmos.DrawLine(playerSpawnPoint.position, playerSpawnPoint.position + Vector3.up);
            }
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * (levelBounds.y / 2 + 1f), 
                $"{levelName}\nBounds: {levelBounds.x}x{levelBounds.y}");
            #endif
        }
    }
}
