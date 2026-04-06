using UnityEngine;
using System.Collections;
using Deadlight.Core;
using Deadlight.Visuals;

namespace Deadlight.Player
{
    public enum ThrowableType { Grenade, Molotov }

    public class ThrowableSystem : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private int grenadeCount = 2;
        [SerializeField] private int molotovCount = 1;
        [SerializeField] private int maxGrenades = 5;
        [SerializeField] private int maxMolotovs = 3;

        [Header("Grenade")]
        [SerializeField] private float grenadeSpeed = 10f;
        [SerializeField] private float grenadeFuseTime = 1.5f;
        [SerializeField] private float grenadeDamage = 80f;
        [SerializeField] private float grenadeRadius = 4f;

        [Header("Molotov")]
        [SerializeField] private float molotovSpeed = 8f;
        [SerializeField] private float molotovBurnDamage = 5f;
        [SerializeField] private float molotovBurnDuration = 5f;
        [SerializeField] private float molotovRadius = 3f;

        [Header("Cooldown")]
        [SerializeField] private float throwCooldown = 1f;
        private float lastThrowTime = -999f;

        public int GrenadeCount => grenadeCount;
        public int MolotovCount => molotovCount;
        public int MaxGrenades => maxGrenades;
        public int MaxMolotovs => maxMolotovs;

        public System.Action<int, int> OnInventoryChanged;
        public System.Action<ThrowableType> OnThrowableUsed;
        public System.Action<float> OnMolotovFireZoneStarted;
        public System.Action OnMolotovFireZoneEnded;

        private void Update()
        {
            if (Time.time < lastThrowTime + throwCooldown) return;

            if (Input.GetKeyDown(KeyCode.Q) && grenadeCount > 0)
            {
                ThrowProjectile(ThrowableType.Grenade);
            }
            else if (Input.GetKeyDown(KeyCode.G) && molotovCount > 0)
            {
                ThrowProjectile(ThrowableType.Molotov);
            }
        }

        private void ThrowProjectile(ThrowableType type)
        {
            lastThrowTime = Time.time;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;

            float speed = type == ThrowableType.Grenade ? grenadeSpeed : molotovSpeed;

            var projectile = new GameObject(type.ToString());
            projectile.transform.position = (Vector2)transform.position + dir * 0.5f;

            var sr = projectile.AddComponent<SpriteRenderer>();
            sr.sprite = CreateThrowableSprite(type);
            sr.sortingOrder = 12;

            var rb = projectile.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.linearVelocity = dir * speed;
            rb.angularVelocity = 360f;

            var projectileCollider = projectile.AddComponent<CircleCollider2D>();
            projectileCollider.radius = 0.18f;
            projectileCollider.isTrigger = true;

            Destroy(projectile, 12f);

            if (type == ThrowableType.Grenade)
            {
                grenadeCount--;
                projectile.AddComponent<GrenadeProjectile>().Initialize(grenadeFuseTime, grenadeDamage, grenadeRadius);
            }
            else
            {
                molotovCount--;
                projectile.AddComponent<MolotovProjectile>().Initialize(
                    molotovBurnDamage,
                    molotovBurnDuration,
                    molotovRadius,
                    HandleMolotovFireZoneStarted,
                    HandleMolotovFireZoneEnded);
            }

            OnThrowableUsed?.Invoke(type);
            OnInventoryChanged?.Invoke(grenadeCount, molotovCount);
        }

        private void HandleMolotovFireZoneStarted(float duration)
        {
            OnMolotovFireZoneStarted?.Invoke(duration);
        }

        private void HandleMolotovFireZoneEnded()
        {
            OnMolotovFireZoneEnded?.Invoke();
        }

        private Sprite CreateThrowableSprite(ThrowableType type)
        {
            int size = 8;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color color = type == ThrowableType.Grenade
                ? new Color(0.3f, 0.4f, 0.3f)
                : new Color(0.6f, 0.3f, 0.1f);

            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Vector2.Distance(new Vector2(x, y), center) < size / 2f - 1
                        ? color : Color.clear;

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        public void AddGrenades(int count)
        {
            grenadeCount = Mathf.Min(grenadeCount + count, maxGrenades);
            OnInventoryChanged?.Invoke(grenadeCount, molotovCount);
        }

        public void AddMolotovs(int count)
        {
            molotovCount = Mathf.Min(molotovCount + count, maxMolotovs);
            OnInventoryChanged?.Invoke(grenadeCount, molotovCount);
        }
    }

    public class GrenadeProjectile : MonoBehaviour
    {
        private float fuseTime;
        private float damage;
        private float radius;
        private float timer;
        private bool detonated;

        public void Initialize(float fuse, float dmg, float rad)
        {
            fuseTime = fuse;
            damage = dmg;
            radius = rad;
        }

        void Update()
        {
            timer += Time.deltaTime;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity *= 0.97f;
            }

            if (timer >= fuseTime && !detonated)
            {
                Detonate();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) return;
            if (timer < 0.1f) return;

            var eh = other.GetComponent<Enemy.EnemyHealth>();
            if (eh != null && !detonated)
                Detonate();
        }

        void Detonate()
        {
            detonated = true;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDeathExplosion(transform.position, false);
                VFXManager.Instance.TriggerScreenShake(0.5f, 0.4f);
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, radius);
            foreach (var hit in hits)
            {
                var eh = hit.GetComponent<Enemy.EnemyHealth>();
                if (eh != null)
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);
                    float falloff = 1f - (dist / radius);
                    eh.TakeDamage(damage * Mathf.Max(0.2f, falloff));

                    var rb = hit.attachedRigidbody;
                    if (rb != null)
                    {
                        Vector2 dir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                        rb.AddForce(dir * 5f, ForceMode2D.Impulse);
                    }
                }
            }

            Destroy(gameObject);
        }
    }

    public class MolotovProjectile : MonoBehaviour
    {
        private float burnDps;
        private float burnDuration;
        private float radius;
        private System.Action<float> onFireZoneStarted;
        private System.Action onFireZoneEnded;
        private bool shattered;
        private float airborneTimer;
        private const float MaxAirTime = 1.35f;
        private bool fireZoneStartedNotified;
        private bool fireZoneEndedNotified;

        public void Initialize(float dps, float dur, float rad,
            System.Action<float> fireZoneStarted = null,
            System.Action fireZoneEnded = null)
        {
            burnDps = dps;
            burnDuration = dur;
            radius = rad;
            onFireZoneStarted = fireZoneStarted;
            onFireZoneEnded = fireZoneEnded;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerController>() != null) return;
            if (!shattered) Shatter();
        }

        void Update()
        {
            if (shattered) return;

            airborneTimer += Time.deltaTime;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity *= 0.965f;
                if (rb.linearVelocity.magnitude < 1f)
                {
                    Shatter();
                    return;
                }
            }

            if (airborneTimer >= MaxAirTime)
            {
                Shatter();
            }
        }

        void Shatter()
        {
            if (shattered) return;
            shattered = true;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            StartCoroutine(FireZone());
        }

        IEnumerator FireZone()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.clear;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            var fireObj = new GameObject("FireZone");
            fireObj.transform.position = transform.position;
            var fireSr = fireObj.AddComponent<SpriteRenderer>();

            int size = 32;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    pixels[y * size + x] = dist < 0.9f
                        ? new Color(1f, 0.4f + Random.Range(0f, 0.3f), 0f, 0.5f * (1f - dist))
                        : Color.clear;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            fireSr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / (radius * 2f));
            fireSr.sortingOrder = Mathf.RoundToInt(-fireObj.transform.position.y) + 2;
            NotifyFireZoneStarted();

            float elapsed = 0f;
            while (elapsed < burnDuration)
            {
                var hits = Physics2D.OverlapCircleAll(fireObj.transform.position, radius);
                foreach (var hit in hits)
                {
                    var eh = hit.GetComponent<Enemy.EnemyHealth>();
                    if (eh != null && eh.IsAlive)
                    {
                        if (hit.GetComponent<Enemy.BurnEffect>() == null)
                        {
                            var burn = hit.gameObject.AddComponent<Enemy.BurnEffect>();
                            burn.Initialize(burnDps, 2f);
                        }
                    }
                }

                float alpha = 0.5f * (1f - elapsed / burnDuration);
                fireSr.color = new Color(1f, 1f, 1f, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            NotifyFireZoneEnded();
            Destroy(fireObj);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            NotifyFireZoneEnded();
        }

        private void NotifyFireZoneStarted()
        {
            if (fireZoneStartedNotified)
            {
                return;
            }

            fireZoneStartedNotified = true;
            onFireZoneStarted?.Invoke(burnDuration);
        }

        private void NotifyFireZoneEnded()
        {
            if (fireZoneEndedNotified || !fireZoneStartedNotified)
            {
                return;
            }

            fireZoneEndedNotified = true;
            onFireZoneEnded?.Invoke();
        }
    }
}
