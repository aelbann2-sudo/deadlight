using UnityEngine;
using Deadlight.Player;
using Deadlight.Core;

namespace Deadlight.Systems
{
    public class PickupSpawner : MonoBehaviour
    {
        public static PickupSpawner Instance { get; private set; }

        [Header("Drop Chances")]
        [SerializeField] private float ammoDropChance = 0.30f;
        [SerializeField] private float healthDropChance = 0.15f;
        [SerializeField] private float pointsDropChance = 0.20f;
        [SerializeField] private float powerupDropChance = 0.05f;

        [Header("Pickup Values")]
        [SerializeField] private int ammoAmount = 15;
        [SerializeField] private float healthAmount = 25f;
        [SerializeField] private int pointsAmount = 50;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SpawnRandomPickup(Vector3 position)
        {
            float roll = Random.value;
            float cumulative = 0f;

            cumulative += ammoDropChance;
            if (roll < cumulative)
            {
                SpawnPickup(position, PickupType.Ammo);
                return;
            }

            cumulative += healthDropChance;
            if (roll < cumulative)
            {
                SpawnPickup(position, PickupType.Health);
                return;
            }

            cumulative += pointsDropChance;
            if (roll < cumulative)
            {
                SpawnPickup(position, PickupType.Points);
                return;
            }

            cumulative += powerupDropChance;
            if (roll < cumulative)
            {
                SpawnPickup(position, PickupType.Powerup);
            }
        }

        public void SpawnPickup(Vector3 position, PickupType type)
        {
            var pickupObj = new GameObject($"Pickup_{type}");
            pickupObj.transform.position = position + Vector3.up * 0.2f;

            var sr = pickupObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePickupSprite(type);
            sr.sortingOrder = 15;

            var pickup = pickupObj.AddComponent<PickupItem>();
            pickup.Initialize(type, GetPickupValue(type));

            var glow = CreateGlowEffect(pickupObj.transform, GetPickupColor(type));
            
            pickupObj.AddComponent<PickupAnimation>();

            var col = pickupObj.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            Destroy(pickupObj, 10f);
        }

        private Sprite CreatePickupSprite(PickupType type)
        {
            int size = 16;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Color mainColor = GetPickupColor(type);
            Color darkColor = mainColor * 0.6f;
            darkColor.a = 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size / 2f + 0.5f;
                    float dy = y - size / 2f + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < size / 2f - 1)
                    {
                        float t = dist / (size / 2f);
                        pixels[y * size + x] = Color.Lerp(mainColor, darkColor, t * 0.5f);
                    }
                    else if (dist < size / 2f)
                    {
                        pixels[y * size + x] = darkColor;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            switch (type)
            {
                case PickupType.Ammo:
                    DrawAmmoIcon(pixels, size);
                    break;
                case PickupType.Health:
                    DrawCrossIcon(pixels, size);
                    break;
                case PickupType.Points:
                    DrawCoinIcon(pixels, size);
                    break;
                case PickupType.Powerup:
                    DrawStarIcon(pixels, size);
                    break;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private void DrawAmmoIcon(Color[] pixels, int size)
        {
            Color iconColor = new Color(0.2f, 0.15f, 0.1f);
            for (int y = 5; y < 11; y++)
            {
                for (int x = 6; x < 10; x++)
                {
                    pixels[y * size + x] = iconColor;
                }
            }
        }

        private void DrawCrossIcon(Color[] pixels, int size)
        {
            Color white = Color.white;
            for (int i = 4; i < 12; i++)
            {
                pixels[7 * size + i] = white;
                pixels[8 * size + i] = white;
                pixels[i * size + 7] = white;
                pixels[i * size + 8] = white;
            }
        }

        private void DrawCoinIcon(Color[] pixels, int size)
        {
            Color dark = new Color(0.4f, 0.35f, 0.1f);
            for (int y = 5; y < 11; y++)
            {
                pixels[y * size + 7] = dark;
                pixels[y * size + 8] = dark;
            }
        }

        private void DrawStarIcon(Color[] pixels, int size)
        {
            Color white = Color.white;
            int cx = 8, cy = 8;
            pixels[cy * size + cx] = white;
            pixels[(cy + 2) * size + cx] = white;
            pixels[(cy - 2) * size + cx] = white;
            pixels[cy * size + (cx + 2)] = white;
            pixels[cy * size + (cx - 2)] = white;
            pixels[(cy + 1) * size + (cx + 1)] = white;
            pixels[(cy + 1) * size + (cx - 1)] = white;
            pixels[(cy - 1) * size + (cx + 1)] = white;
            pixels[(cy - 1) * size + (cx - 1)] = white;
        }

        private Color GetPickupColor(PickupType type)
        {
            return type switch
            {
                PickupType.Ammo => new Color(1f, 0.85f, 0.2f),
                PickupType.Health => new Color(1f, 0.3f, 0.3f),
                PickupType.Points => new Color(0.3f, 1f, 0.4f),
                PickupType.Powerup => new Color(0.7f, 0.3f, 1f),
                _ => Color.white
            };
        }

        private float GetPickupValue(PickupType type)
        {
            return type switch
            {
                PickupType.Ammo => ammoAmount,
                PickupType.Health => healthAmount,
                PickupType.Points => pointsAmount,
                PickupType.Powerup => 1f,
                _ => 0f
            };
        }

        private GameObject CreateGlowEffect(Transform parent, Color color)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(parent);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 2f;

            var sr = glow.AddComponent<SpriteRenderer>();
            
            int size = 32;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    if (dist < 1f)
                    {
                        float alpha = (1f - dist) * 0.4f;
                        pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
            sr.sortingOrder = 14;

            return glow;
        }
    }

    public class PickupItem : MonoBehaviour
    {
        private PickupType type;
        private float value;

        public void Initialize(PickupType pickupType, float pickupValue)
        {
            type = pickupType;
            value = pickupValue;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.name != "Player") return;

            switch (type)
            {
                case PickupType.Ammo:
                    var shooting = other.GetComponent<PlayerShooting>();
                    if (shooting != null)
                    {
                        shooting.AddAmmo((int)value);
                    }
                    break;

                case PickupType.Health:
                    var health = other.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.Heal(value);
                    }
                    break;

                case PickupType.Points:
                    if (PointsSystem.Instance != null)
                    {
                        PointsSystem.Instance.AddPoints((int)value, "Pickup");
                    }
                    break;

                case PickupType.Powerup:
                    if (PowerupSystem.Instance != null)
                    {
                        PowerupSystem.Instance.GrantRandomPowerup();
                    }
                    break;
            }

            var clip = Audio.ProceduralAudioGenerator.GeneratePickup();
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, 0.5f);
            }

            Destroy(gameObject);
        }
    }

    public class PickupAnimation : MonoBehaviour
    {
        private float bobSpeed = 3f;
        private float bobHeight = 0.15f;
        private float pulseSpeed = 2f;
        private Vector3 startPos;
        private Transform glowTransform;

        private void Start()
        {
            startPos = transform.position;
            if (transform.childCount > 0)
            {
                glowTransform = transform.GetChild(0);
            }
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = startPos + Vector3.up * bob;

            if (glowTransform != null)
            {
                float pulse = 1.8f + Mathf.Sin(Time.time * pulseSpeed) * 0.4f;
                glowTransform.localScale = Vector3.one * pulse;
            }
        }
    }
}
