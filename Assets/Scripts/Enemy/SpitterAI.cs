using UnityEngine;
using System.Collections;
using Deadlight.Core;

namespace Deadlight.Enemy
{
    public class SpitterAI : MonoBehaviour
    {
        [Header("Ranged Attack")]
        [SerializeField] private float spitRange = 8f;
        [SerializeField] private float spitCooldown = 2.5f;
        [SerializeField] private float spitSpeed = 7f;
        [SerializeField] private float spitDamage = 15f;
        [SerializeField] private float fleeRange = 4f;

        private Transform target;
        private Rigidbody2D rb;
        private EnemyHealth health;
        private SpriteRenderer spriteRenderer;
        private float lastSpitTime;
        private float moveSpeed = 2.5f;
        private float speedMultiplier = 1f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            FindPlayer();
        }

        void Update()
        {
            if (health != null && !health.IsAlive)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            FindPlayer();
            if (target == null) return;

            float dist = Vector2.Distance(transform.position, target.position);

            if (dist < fleeRange)
            {
                Flee();
            }
            else if (dist <= spitRange && Time.time >= lastSpitTime + spitCooldown)
            {
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(SpitAttack());
            }
            else if (dist > spitRange)
            {
                ChasePlayer();
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            UpdateVisuals();
        }

        void FindPlayer()
        {
            if (target != null) return;
            var player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }

        void ChasePlayer()
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed * speedMultiplier;
        }

        void Flee()
        {
            Vector2 dir = ((Vector2)transform.position - (Vector2)target.position).normalized;
            rb.linearVelocity = dir * moveSpeed * speedMultiplier * 1.3f;
        }

        IEnumerator SpitAttack()
        {
            lastSpitTime = Time.time;

            if (spriteRenderer != null)
            {
                Color orig = spriteRenderer.color;
                spriteRenderer.color = new Color(0.4f, 1f, 0.2f);
                yield return new WaitForSeconds(0.3f);
                spriteRenderer.color = orig;
            }

            if (target == null) yield break;

            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            SpawnAcidProjectile(dir);
        }

        void SpawnAcidProjectile(Vector2 direction)
        {
            var acid = new GameObject("AcidSpit");
            acid.transform.position = (Vector2)transform.position + direction * 0.5f;

            var sr = acid.AddComponent<SpriteRenderer>();
            int s = 6;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            Vector2 c = new Vector2(s / 2f, s / 2f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    px[y * s + x] = Vector2.Distance(new Vector2(x, y), c) < s / 2f
                        ? new Color(0.3f, 0.9f, 0.1f, 0.9f)
                        : Color.clear;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 12f);
            sr.sortingOrder = 11;

            var rb = acid.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.linearVelocity = direction * spitSpeed;

            var col = acid.AddComponent<CircleCollider2D>();
            col.radius = 0.15f;
            col.isTrigger = true;

            acid.AddComponent<AcidProjectile>().Initialize(spitDamage);
            Destroy(acid, 4f);
        }

        void UpdateVisuals()
        {
            if (spriteRenderer == null) return;
            if (rb.linearVelocity.x < -0.1f)
                spriteRenderer.flipX = true;
            else if (rb.linearVelocity.x > 0.1f)
                spriteRenderer.flipX = false;
        }

        public void ApplySpeedMultiplier(float mult) { speedMultiplier = mult; }
        public void ApplyDamageMultiplier(float mult) { spitDamage *= mult; }
    }

    public class AcidProjectile : MonoBehaviour
    {
        private float damage;

        public void Initialize(float dmg) { damage = dmg; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<EnemyHealth>() != null) return;
            if (other.GetComponent<SpitterAI>() != null) return;

            var ph = other.GetComponent<Player.PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);

                var burn = other.gameObject.GetComponent<BurnEffect>();
                if (burn == null)
                {
                    burn = other.gameObject.AddComponent<BurnEffect>();
                    burn.Initialize(2f, 2f);
                }
            }

            Destroy(gameObject);
        }
    }
}
