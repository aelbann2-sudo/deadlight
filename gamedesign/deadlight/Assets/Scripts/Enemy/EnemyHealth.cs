using UnityEngine;
using Deadlight.Core;
using Deadlight.Systems;
using System;

namespace Deadlight.Enemy
{
    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float currentHealth;

        [Header("Loot Settings")]
        [SerializeField] private float dropChance = 0.15f;
        [SerializeField] private GameObject[] possibleDrops;
        [SerializeField] private int pointsOnDeath = 10;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color damageFlashColor = Color.white;
        [SerializeField] private float flashDuration = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Death")]
        [SerializeField] private float destroyDelay = 0.5f;
        [SerializeField] private GameObject deathEffectPrefab;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;

        public event Action<float> OnDamageTaken;
        public event Action OnEnemyDeath;

        private Color originalColor;
        private bool isDead = false;

        private void Awake()
        {
            currentHealth = maxHealth;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public void ApplyHealthMultiplier(float multiplier)
        {
            maxHealth *= multiplier;
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnDamageTaken?.Invoke(damage);

            PlaySound(hurtSound);
            StartCoroutine(DamageFlashCoroutine());

            Debug.Log($"[EnemyHealth] Took {damage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("[EnemyHealth] Enemy died!");

            PlaySound(deathSound);
            OnEnemyDeath?.Invoke();

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.RegisterEnemyDeath();
            }

            var pointsSystem = FindObjectOfType<PointsSystem>();
            if (pointsSystem != null)
            {
                pointsSystem.AddPoints(pointsOnDeath, "Enemy Kill");
            }

            var enemyAI = GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.OnDeath();
            }

            TryDropLoot();
            SpawnDeathEffect();

            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Destroy(gameObject, destroyDelay);
        }

        private void TryDropLoot()
        {
            if (possibleDrops == null || possibleDrops.Length == 0) return;

            float dropRoll = UnityEngine.Random.value;
            float actualDropChance = dropChance;

            if (GameManager.Instance?.CurrentSettings != null)
            {
                actualDropChance *= GameManager.Instance.CurrentSettings.resourceSpawnMultiplier;
            }

            if (dropRoll <= actualDropChance)
            {
                int dropIndex = UnityEngine.Random.Range(0, possibleDrops.Length);
                if (possibleDrops[dropIndex] != null)
                {
                    Instantiate(possibleDrops[dropIndex], transform.position, Quaternion.identity);
                    Debug.Log("[EnemyHealth] Dropped loot!");
                }
            }
        }

        private void SpawnDeathEffect()
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            
            if (spriteRenderer != null && !isDead)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        public void SetMaxHealth(float health)
        {
            maxHealth = health;
            currentHealth = maxHealth;
        }

        public void SetDropChance(float chance)
        {
            dropChance = Mathf.Clamp01(chance);
        }

        public void SetPointsOnDeath(int points)
        {
            pointsOnDeath = points;
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
    }
}
