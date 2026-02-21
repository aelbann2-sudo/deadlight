using UnityEngine;

namespace Deadlight.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float speed = 20f;
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private float lifetime = 3f;

        [Header("AOE")]
        [SerializeField] private float explosionRadius = 0f;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;

        private Rigidbody2D rb;
        private Vector3 startPosition;
        private float distanceTraveled;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            startPosition = transform.position;
            rb.linearVelocity = transform.up * speed;

            if (hitSound == null)
            {
                try { hitSound = Audio.ProceduralAudioGenerator.GenerateZombieHitReact(); }
                catch (System.Exception) { }
            }

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            distanceTraveled = Vector3.Distance(startPosition, transform.position);
            
            if (distanceTraveled >= maxDistance)
            {
                DestroyBullet();
            }
        }

        public void Initialize(float bulletDamage, float bulletSpeed, float bulletRange)
        {
            damage = bulletDamage;
            speed = bulletSpeed;
            maxDistance = bulletRange;

            if (rb != null)
            {
                rb.linearVelocity = transform.up * speed;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Player.PlayerController>() != null) return;
            if (other.GetComponent<Player.PlayerHealth>() != null) return;

            if (explosionRadius > 0)
            {
                HandleExplosion();
                SpawnHitEffect();
                DestroyBullet();
                return;
            }

            var enemyHealth = other.GetComponent<Enemy.EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                if (Core.GameEffects.Instance != null)
                {
                    bool heavyHit = damage >= 24f;
                    Core.GameEffects.Instance.SpawnHitEffect(transform.position, heavyHit);
                    Core.GameEffects.Instance.ScreenShake(0.05f, 0.08f);
                    if (heavyHit)
                    {
                        Core.GameEffects.Instance.TriggerHitStop(0.035f);
                    }
                }

                ApplyKnockback(other.attachedRigidbody);
            }

            SpawnHitEffect();
            DestroyBullet();
        }

        private void HandleExplosion()
        {
            if (Visuals.VFXManager.Instance != null)
            {
                Visuals.VFXManager.Instance.PlayDeathExplosion(transform.position, false);
                Visuals.VFXManager.Instance.TriggerScreenShake(0.4f, 0.3f);
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                var eh = hit.GetComponent<Enemy.EnemyHealth>();
                if (eh != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float falloff = 1f - (dist / explosionRadius);
                    eh.TakeDamage(damage * Mathf.Max(0.2f, falloff));
                    ApplyKnockback(hit.attachedRigidbody);
                }
            }
        }

        public void SetExplosionRadius(float radius)
        {
            explosionRadius = radius;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<Player.PlayerController>() != null) return;
            if (collision.gameObject.GetComponent<Player.PlayerHealth>() != null) return;

            var enemyHealth = collision.gameObject.GetComponent<Enemy.EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                if (Core.GameEffects.Instance != null)
                {
                    bool heavyHit = damage >= 24f;
                    Core.GameEffects.Instance.SpawnHitEffect(transform.position, heavyHit);
                    Core.GameEffects.Instance.ScreenShake(0.05f, 0.08f);
                    if (heavyHit)
                    {
                        Core.GameEffects.Instance.TriggerHitStop(0.035f);
                    }
                }

                ApplyKnockback(collision.rigidbody);
            }

            SpawnHitEffect();
            DestroyBullet();
        }

        private void SpawnHitEffect()
        {
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }
        }

        private void DestroyBullet()
        {
            Destroy(gameObject);
        }

        public void SetDamage(float newDamage)
        {
            damage = newDamage;
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
            if (rb != null)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * speed;
            }
        }

        private void ApplyKnockback(Rigidbody2D targetBody)
        {
            if (targetBody == null)
            {
                return;
            }

            Vector2 direction = ((Vector2)targetBody.position - (Vector2)transform.position).normalized;
            targetBody.AddForce(direction * 1.8f, ForceMode2D.Impulse);
        }
    }
}
