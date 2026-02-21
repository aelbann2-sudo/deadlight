using UnityEngine;

namespace Deadlight.Level
{
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private string spawnPointId = "";
        [SerializeField, Range(0f, 1f)] private float spawnWeight = 1f;
        [SerializeField] private int maxConcurrentEnemies = 3;
        [SerializeField] private int activationNight = 1;
        
        [Header("Spawn Properties")]
        [SerializeField] private float spawnRadius = 1f;
        [SerializeField] private bool requiresLineOfSight = true;
        [SerializeField] private float minDistanceFromPlayer = 5f;
        
        [Header("Enemy Types")]
        [SerializeField] private bool canSpawnBasicZombie = true;
        [SerializeField] private bool canSpawnFastZombie = false;
        [SerializeField] private bool canSpawnTankZombie = false;
        
        [Header("State")]
        [SerializeField] private int currentEnemiesSpawned = 0;

        public string SpawnPointId => string.IsNullOrEmpty(spawnPointId) ? gameObject.name : spawnPointId;
        public float SpawnWeight => spawnWeight;
        public int MaxConcurrentEnemies => maxConcurrentEnemies;
        public int ActivationNight => activationNight;
        public float SpawnRadius => spawnRadius;
        public bool RequiresLineOfSight => requiresLineOfSight;
        public float MinDistanceFromPlayer => minDistanceFromPlayer;
        public int CurrentEnemiesSpawned => currentEnemiesSpawned;
        public bool CanSpawn => currentEnemiesSpawned < maxConcurrentEnemies;

        private MapZone parentZone;

        private void Awake()
        {
            parentZone = GetComponentInParent<MapZone>();
            
            if (string.IsNullOrEmpty(spawnPointId))
            {
                spawnPointId = $"Spawn_{transform.position.x:F0}_{transform.position.y:F0}";
            }
        }

        public bool IsActiveForNight(int currentNight)
        {
            return currentNight >= activationNight;
        }

        public bool CanSpawnEnemyType(string enemyType)
        {
            return enemyType.ToLower() switch
            {
                "basic" => canSpawnBasicZombie,
                "fast" => canSpawnFastZombie,
                "tank" => canSpawnTankZombie,
                _ => canSpawnBasicZombie
            };
        }

        public Vector3 GetSpawnPosition()
        {
            if (spawnRadius <= 0) return transform.position;

            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
        }

        public bool IsValidSpawnPosition(Vector3 playerPosition)
        {
            float distance = Vector3.Distance(transform.position, playerPosition);
            
            if (distance < minDistanceFromPlayer) return false;

            if (requiresLineOfSight)
            {
                Vector3 direction = (transform.position - playerPosition).normalized;
                RaycastHit2D hit = Physics2D.Raycast(playerPosition, direction, distance);
                
                if (hit.collider != null && !hit.collider.CompareTag("Enemy"))
                {
                    return false;
                }
            }

            return true;
        }

        public void OnEnemySpawned()
        {
            currentEnemiesSpawned++;
        }

        public void OnEnemyDied()
        {
            currentEnemiesSpawned = Mathf.Max(0, currentEnemiesSpawned - 1);
        }

        public void ResetSpawnCount()
        {
            currentEnemiesSpawned = 0;
        }

        public MapZone GetParentZone()
        {
            return parentZone;
        }

        private void OnDrawGizmos()
        {
            bool isActive = Application.isPlaying ? 
                (Core.GameManager.Instance != null && IsActiveForNight(Core.GameManager.Instance.CurrentNight)) : 
                true;

            Gizmos.color = isActive ? new Color(1f, 0f, 0f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.3f);

            if (spawnRadius > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, minDistanceFromPlayer);
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            string info = $"{SpawnPointId}\n" +
                         $"Weight: {spawnWeight:F2}\n" +
                         $"Max: {maxConcurrentEnemies}\n" +
                         $"Night: {activationNight}+";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, info);
            #endif
        }
    }
}
