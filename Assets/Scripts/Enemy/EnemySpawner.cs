using UnityEngine;
using Deadlight.Core;
using System.Collections.Generic;

namespace Deadlight.Enemy
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private float spawnRadius = 2f;
        [SerializeField] private bool isActive = true;
        [SerializeField] private bool registerWithWaveManager = true;

        [Header("Visualization")]
        [SerializeField] private Color gizmoColor = Color.red;

        private WaveManager waveManager;

        private void Start()
        {
            if (registerWithWaveManager)
            {
                waveManager = FindObjectOfType<WaveManager>();
                if (waveManager != null)
                {
                    waveManager.AddSpawnPoint(transform);
                }
            }
        }

        private void OnDestroy()
        {
            if (waveManager != null)
            {
                waveManager.RemoveSpawnPoint(transform);
            }
        }

        public Vector3 GetSpawnPosition()
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
        }

        public GameObject SpawnEnemy(GameObject enemyPrefab)
        {
            if (!isActive || enemyPrefab == null) return null;

            Vector3 spawnPos = GetSpawnPosition();
            return Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }

        public List<GameObject> SpawnEnemies(GameObject enemyPrefab, int count)
        {
            List<GameObject> spawned = new List<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject enemy = SpawnEnemy(enemyPrefab);
                if (enemy != null)
                {
                    spawned.Add(enemy);
                }
            }

            return spawned;
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

        public void SetSpawnRadius(float radius)
        {
            spawnRadius = Mathf.Max(0, radius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}
