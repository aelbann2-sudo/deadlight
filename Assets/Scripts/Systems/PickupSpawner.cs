using UnityEngine;
using Deadlight.Player;
using Deadlight.Core;
using Deadlight.Visuals;

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
            int variant = Random.Range(0, 10);
            sr.sprite = ProceduralSpriteGenerator.CreatePickupSprite(type.ToString(), variant);
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
