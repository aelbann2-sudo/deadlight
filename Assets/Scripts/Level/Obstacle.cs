using UnityEngine;
using System;

namespace Deadlight.Level
{
    public enum ObstacleType
    {
        Wall,
        Barricade,
        Cover,
        Destructible
    }

    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour
    {
        [Header("Obstacle Configuration")]
        [SerializeField] private string obstacleName = "Obstacle";
        [SerializeField] private ObstacleType obstacleType = ObstacleType.Wall;
        
        [Header("Health (for destructible)")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        [SerializeField] private bool isIndestructible = false;
        
        [Header("Cover Properties")]
        [SerializeField, Range(0f, 1f)] private float coverProtection = 0.5f;
        [SerializeField] private bool blocksBullets = true;
        [SerializeField] private bool blocksMovement = true;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color damagedColor = new Color(0.8f, 0.5f, 0.5f);
        [SerializeField] private GameObject destructionEffect;
        
        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip destroySound;

        public string ObstacleName => obstacleName;
        public ObstacleType Type => obstacleType;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0;
        public bool IsDestroyed => !isIndestructible && currentHealth <= 0;
        public float CoverProtection => coverProtection;
        public bool BlocksBullets => blocksBullets;
        public bool BlocksMovement => blocksMovement;

        public event Action<Obstacle> OnDestroyed;
        public event Action<Obstacle, float> OnDamaged;

        private Collider2D obstacleCollider;
        private Color originalColor;

        private void Awake()
        {
            currentHealth = maxHealth;
            obstacleCollider = GetComponent<Collider2D>();
            
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            ConfigureCollider();
        }

        private void ConfigureCollider()
        {
            if (obstacleCollider == null) return;

            switch (obstacleType)
            {
                case ObstacleType.Wall:
                    obstacleCollider.isTrigger = false;
                    break;
                case ObstacleType.Barricade:
                    obstacleCollider.isTrigger = false;
                    break;
                case ObstacleType.Cover:
                    obstacleCollider.isTrigger = !blocksMovement;
                    break;
                case ObstacleType.Destructible:
                    obstacleCollider.isTrigger = false;
                    break;
            }
        }

        public void TakeDamage(float damage, GameObject source = null)
        {
            if (isIndestructible || IsDestroyed) return;
            if (obstacleType == ObstacleType.Wall) return;

            currentHealth -= damage;
            OnDamaged?.Invoke(this, damage);

            UpdateVisual();
            PlaySound(hitSound);

            if (currentHealth <= 0)
            {
                Destroy();
            }
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null) return;

            float healthPercent = HealthPercent;
            spriteRenderer.color = Color.Lerp(damagedColor, originalColor, healthPercent);
        }

        private void Destroy()
        {
            OnDestroyed?.Invoke(this);
            PlaySound(destroySound);

            if (destructionEffect != null)
            {
                Instantiate(destructionEffect, transform.position, Quaternion.identity);
            }

            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = false;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            Destroy(gameObject, 0.1f);
        }

        public void Repair(float amount)
        {
            if (IsDestroyed) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            UpdateVisual();
        }

        public void FullRepair()
        {
            currentHealth = maxHealth;
            UpdateVisual();
        }

        public float ApplyCoverDamageReduction(float incomingDamage)
        {
            if (obstacleType != ObstacleType.Cover) return incomingDamage;
            return incomingDamage * (1f - coverProtection);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                if (obstacleType == ObstacleType.Barricade || obstacleType == ObstacleType.Destructible)
                {
                    TakeDamage(10f, collision.gameObject);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Color gizmoColor = obstacleType switch
            {
                ObstacleType.Wall => Color.gray,
                ObstacleType.Barricade => Color.yellow,
                ObstacleType.Cover => Color.blue,
                ObstacleType.Destructible => Color.red,
                _ => Color.white
            };

            Gizmos.color = gizmoColor;
            
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }
    }
}
