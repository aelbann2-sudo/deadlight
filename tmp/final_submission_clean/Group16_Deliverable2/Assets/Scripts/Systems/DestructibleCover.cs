using UnityEngine;
using Deadlight.Visuals;

namespace Deadlight.Systems
{
    public class DestructibleCover : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float currentHealth;

        [Header("Visual")]
        [SerializeField] private Color damagedColor = new Color(0.6f, 0.5f, 0.4f);
        [SerializeField] private Color criticalColor = new Color(0.4f, 0.3f, 0.25f);

        private SpriteRenderer sr;
        private Color originalColor;
        private Collider2D col;
        private bool isDestroyed;

        private void Awake()
        {
            currentHealth = maxHealth;
            sr = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
            
            if (sr != null)
            {
                originalColor = sr.color;
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDestroyed) return;

            currentHealth -= damage;

            UpdateVisuals();

            if (currentHealth <= 0)
            {
                DestroyCover();
            }
        }

        private void UpdateVisuals()
        {
            if (sr == null) return;

            float healthPercent = currentHealth / maxHealth;

            if (healthPercent < 0.3f)
            {
                sr.color = criticalColor;
            }
            else if (healthPercent < 0.6f)
            {
                sr.color = damagedColor;
            }

            float shake = (1f - healthPercent) * 0.05f;
            transform.position += new Vector3(
                Random.Range(-shake, shake),
                Random.Range(-shake, shake),
                0
            );
        }

        private void DestroyCover()
        {
            isDestroyed = true;

            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayDust(transform.position);
            }

            SpawnDebris();

            if (col != null)
            {
                col.enabled = false;
            }

            if (sr != null)
            {
                StartCoroutine(FadeOut());
            }
            else
            {
                Destroy(gameObject, 0.5f);
            }
        }

        private void SpawnDebris()
        {
            for (int i = 0; i < 3; i++)
            {
                var debris = new GameObject("Debris");
                debris.transform.position = transform.position + (Vector3)Random.insideUnitCircle * 0.5f;
                
                var dsr = debris.AddComponent<SpriteRenderer>();
                dsr.sprite = CreateDebrisSprite();
                dsr.sortingOrder = sr != null ? sr.sortingOrder - 1 : 0;
                dsr.color = sr != null ? sr.color : Color.gray;

                var rb = debris.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0.5f;
                rb.AddForce(Random.insideUnitCircle * 2f, ForceMode2D.Impulse);
                rb.angularVelocity = Random.Range(-180f, 180f);

                Destroy(debris, 3f);
            }
        }

        private Sprite CreateDebrisSprite()
        {
            int size = 8;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Random.value > 0.3f)
                    {
                        pixels[y * size + x] = new Color(0.4f, 0.35f, 0.3f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private System.Collections.IEnumerator FadeOut()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            Color startColor = sr.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.name == "Player") return;

            var enemy = collision.gameObject.GetComponent<Deadlight.Enemy.SimpleEnemyAI>();
            if (enemy != null)
            {
                TakeDamage(5f);
            }
        }

        public void SetHealth(float health)
        {
            maxHealth = health;
            currentHealth = health;
        }
    }
}
