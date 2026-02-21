using UnityEngine;
using Deadlight.Core;
using Deadlight.Systems;
using Deadlight.Visuals;
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

        [Header("Type")]
        [SerializeField] private bool isExploder = false;
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionDamage = 30f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;

        public event Action<float> OnDamageTaken;
        public event Action OnEnemyDeath;
        public event Action<float, float> OnHealthChanged;

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

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyHealthMultiplier(float multiplier)
        {
            maxHealth *= multiplier;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnDamageTaken?.Invoke(damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            PlaySound(hurtSound);
            StartCoroutine(DamageFlashCoroutine());

            if (DecalManager.Instance != null)
                DecalManager.Instance.SpawnBloodDecal(transform.position, 0.6f);

            var zombieSounds = GetComponent<Audio.ZombieSounds>();
            if (zombieSounds != null) zombieSounds.PlayHitReact();

            var zombieAnim = GetComponent<ZombieAnimator>();
            if (zombieAnim != null) zombieAnim.PlayHit();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            PlaySound(deathSound);
            OnEnemyDeath?.Invoke();

            var zombieSounds = GetComponent<Audio.ZombieSounds>();
            if (zombieSounds != null) zombieSounds.PlayDeath();

            var zombieAnim = GetComponent<ZombieAnimator>();
            if (zombieAnim != null) zombieAnim.PlayDeath();

            if (isExploder) HandleExploderDeath();

            if (DecalManager.Instance != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                DecalManager.Instance.SpawnBloodDecal(transform.position, 1.2f);
                DecalManager.Instance.SpawnCorpse(transform.position,
                    sr != null ? sr.sprite : null,
                    sr != null ? sr.color : Color.gray);
            }

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
                waveManager.RegisterEnemyDeath();

            var pointsSystem = FindObjectOfType<PointsSystem>();
            if (pointsSystem != null)
                pointsSystem.AddPoints(pointsOnDeath, "Enemy Kill");
            
            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.SpawnPointsText(pointsOnDeath, transform.position + Vector3.up * 0.5f);
            }
            
            if (KillStreakSystem.Instance != null)
            {
                KillStreakSystem.Instance.RegisterKill(transform.position);
            }
            
            if (PickupSpawner.Instance != null)
            {
                PickupSpawner.Instance.SpawnRandomPickup(transform.position);
            }

            var enemyAI = GetComponent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.OnDeath();

            var spawnTracker = GetComponent<Core.SpawnPointOccupancyTracker>();
            if (spawnTracker != null)
                spawnTracker.Release();

            TryDropLoot();
            SpawnDeathEffect();

            var collider2d = GetComponent<Collider2D>();
            if (collider2d != null) collider2d.enabled = false;

            Destroy(gameObject, destroyDelay);
        }

        private void HandleExploderDeath()
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeathExplosion(transform.position, true);
                VFXManager.Instance.TriggerScreenShake(0.4f, 0.3f);
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var playerHealth = hit.GetComponent<Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float falloff = 1f - (dist / explosionRadius);
                    playerHealth.TakeDamage(explosionDamage * Mathf.Max(0, falloff));
                }

                var otherEnemy = hit.GetComponent<EnemyHealth>();
                if (otherEnemy != null && otherEnemy != this)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float falloff = 1f - (dist / explosionRadius);
                    otherEnemy.TakeDamage(explosionDamage * 0.5f * Mathf.Max(0, falloff));
                }
            }
        }

        public void SetIsExploder(bool exploder)
        {
            isExploder = exploder;
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

            if (RunModifierSystem.Instance != null)
            {
                actualDropChance *= RunModifierSystem.Instance.GetAmmoDropMultiplier();
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
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
