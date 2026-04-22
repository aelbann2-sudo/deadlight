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
        [SerializeField] private float ammoDropChance = 0.20f;
        [SerializeField] private float healthDropChance = 0.10f;
        [SerializeField] private float pointsDropChance = 0.10f;
        [SerializeField] private float powerupDropChance = 0.03f;
        [SerializeField] private int ammoReserveSoftTarget = 140;
        [SerializeField] private int ammoReserveHardTarget = 300;
        [SerializeField, Range(0f, 1f)] private float minAmmoDropMultiplierAtHighReserve = 0.15f;

        [Header("Pickup Values")]
        [SerializeField] private int ammoAmount = 15;
        [SerializeField] private float healthAmount = 25f;
        [SerializeField] private int pointsAmount = 50;
        [SerializeField] private int scrapAmount = 2;
        [SerializeField] private int chemicalsAmount = 1;
        [SerializeField] private int electronicsAmount = 1;

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
            float ammoMultiplier = GetAmmoDropChanceMultiplier();
            float effectiveAmmoDropChance = ammoDropChance * ammoMultiplier;
            float effectivePointsDropChance = pointsDropChance + (ammoDropChance - effectiveAmmoDropChance);

            cumulative += effectiveAmmoDropChance;
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

            cumulative += effectivePointsDropChance;
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

        private float GetAmmoDropChanceMultiplier()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            var shooting = player != null ? player.GetComponent<PlayerShooting>() : null;
            if (shooting == null)
            {
                return 1f;
            }

            float reserve = shooting.ReserveAmmo;
            float soft = Mathf.Max(1f, ammoReserveSoftTarget);
            float hard = Mathf.Max(soft + 1f, ammoReserveHardTarget);
            float floor = Mathf.Clamp01(minAmmoDropMultiplierAtHighReserve);

            if (reserve <= soft)
            {
                return 1f;
            }

            if (reserve >= hard)
            {
                return floor;
            }

            float t = Mathf.InverseLerp(soft, hard, reserve);
            return Mathf.Lerp(1f, floor, t);
        }

        public void SpawnPickup(Vector3 position, PickupType type)
        {
            type = SanitizePickupType(type);

            var pickupObj = new GameObject($"Pickup_{type}");
            pickupObj.transform.position = position;

            var sr = pickupObj.AddComponent<SpriteRenderer>();
            int variant = Random.Range(0, 10);
            sr.sprite = ProceduralSpriteGenerator.CreatePickupSprite(type.ToString(), variant);
            sr.sortingOrder = 15;

            var col = pickupObj.AddComponent<CircleCollider2D>();
            col.radius = 0.24f;
            col.isTrigger = true;

            var pickup = pickupObj.AddComponent<PickupItem>();
            pickup.Initialize(type, GetPickupValue(type));

            CreateGlowEffect(pickupObj.transform, GetPickupColor(type));
            
            pickupObj.AddComponent<PickupAnimation>();

            Destroy(pickupObj, 10f);
        }

        public static PickupType SanitizePickupType(PickupType type)
        {
            if (!IsCraftingPickup(type))
            {
                return type;
            }

            return PickupType.Points;
        }

        public static bool IsCraftingPickup(PickupType type)
        {
            return type == PickupType.Scrap ||
                   type == PickupType.Wood ||
                   type == PickupType.Chemicals ||
                   type == PickupType.Electronics;
        }

        private Color GetPickupColor(PickupType type)
        {
            return type switch
            {
                PickupType.Ammo => new Color(1f, 0.85f, 0.2f),
                PickupType.Health => new Color(1f, 0.3f, 0.3f),
                PickupType.Scrap => new Color(0.55f, 0.85f, 1f),
                PickupType.Wood => new Color(0.72f, 0.58f, 0.35f),
                PickupType.Chemicals => new Color(0.45f, 1f, 0.85f),
                PickupType.Electronics => new Color(0.45f, 0.75f, 1f),
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
                PickupType.Scrap => scrapAmount,
                PickupType.Wood => Mathf.Max(1, scrapAmount),
                PickupType.Chemicals => chemicalsAmount,
                PickupType.Electronics => electronicsAmount,
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
        private Collider2D playerCollider;
        private CircleCollider2D pickupCollider;
        private SpriteRenderer spriteRenderer;
        private bool consumed;

        public void Initialize(PickupType pickupType, float pickupValue)
        {
            type = PickupSpawner.SanitizePickupType(pickupType);
            value = pickupValue;
        }

        private void Start()
        {
            EnsureReferences();
            TryCollectNearbyPlayer();
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            TryCollectNearbyPlayer();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void EnsureReferences()
        {
            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<CircleCollider2D>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void TryCollectNearbyPlayer()
        {
            EnsureReferences();
            if (pickupCollider == null)
            {
                return;
            }

            if (playerCollider == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerCollider = player.GetComponent<Collider2D>();
                }
            }

            if (playerCollider != null)
            {
                TryCollect(playerCollider);
            }
        }

        private void TryCollect(Collider2D other)
        {
            if (consumed)
            {
                return;
            }

            EnsureReferences();

            var playerHealth = other.GetComponent<PlayerHealth>();
            var shooting = other.GetComponent<PlayerShooting>();
            if (playerHealth == null && shooting == null)
            {
                return;
            }

            if (!PickupContactUtility.IsTightPickupContact(pickupCollider, spriteRenderer, other))
            {
                return;
            }

            bool didCollect = false;
            int displayAmount = Mathf.Max(1, Mathf.RoundToInt(value));

            switch (type)
            {
                case PickupType.Ammo:
                    if (shooting != null)
                    {
                        int added = shooting.AddAmmo(Mathf.Max(1, Mathf.RoundToInt(value)));
                        if (added > 0)
                        {
                            displayAmount = added;
                            didCollect = true;
                        }
                    }
                    break;

                case PickupType.Health:
                    if (playerHealth != null)
                    {
                        playerHealth.Heal(value);
                        didCollect = true;
                    }
                    break;

                case PickupType.Scrap:
                case PickupType.Wood:
                case PickupType.Chemicals:
                case PickupType.Electronics:
                    if (PointsSystem.Instance != null)
                    {
                        int amount = Mathf.Max(1, Mathf.RoundToInt(value));
                        PointsSystem.Instance.AddPoints(amount, "Resource Pickup");
                        didCollect = true;
                    }
                    break;

                case PickupType.Points:
                    if (PointsSystem.Instance != null)
                    {
                        PointsSystem.Instance.AddPoints((int)value, "Pickup");
                        didCollect = true;
                    }
                    break;

                case PickupType.Powerup:
                    if (PowerupSystem.Instance != null)
                    {
                        PowerupSystem.Instance.GrantRandomPowerup();
                        didCollect = true;
                    }
                    break;
            }

            if (!didCollect)
            {
                return;
            }

            consumed = true;
            UI.GameplayHelpSystem.Instance?.ShowPickup(type, displayAmount);

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
        private float pulseSpeed = 2f;
        private Transform glowTransform;

        private void Start()
        {
            if (transform.childCount > 0)
            {
                glowTransform = transform.GetChild(0);
            }
        }

        private void Update()
        {
            if (glowTransform != null)
            {
                float pulse = 1.8f + Mathf.Sin(Time.time * pulseSpeed) * 0.4f;
                glowTransform.localScale = Vector3.one * pulse;
            }
        }
    }
}
