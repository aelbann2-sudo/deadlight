using UnityEngine;
using System.Collections;
using Deadlight.Player;

namespace Deadlight.Systems
{
    public enum PowerupType
    {
        DoubleDamage,
        SpeedBoost,
        InfiniteAmmo,
        Invincibility
    }

    public class PowerupSystem : MonoBehaviour
    {
        public static PowerupSystem Instance { get; private set; }

        [Header("Powerup Durations")]
        [SerializeField] private float doubleDamageDuration = 10f;
        [SerializeField] private float speedBoostDuration = 8f;
        [SerializeField] private float infiniteAmmoDuration = 6f;
        [SerializeField] private float invincibilityDuration = 4f;

        private PowerupType? activePowerup;
        private float powerupEndTime;
        private GameObject powerupVisual;

        public bool HasDoubleDamage => activePowerup == PowerupType.DoubleDamage && Time.time < powerupEndTime;
        public bool HasSpeedBoost => activePowerup == PowerupType.SpeedBoost && Time.time < powerupEndTime;
        public bool HasInfiniteAmmo => activePowerup == PowerupType.InfiniteAmmo && Time.time < powerupEndTime;
        public bool HasInvincibility => activePowerup == PowerupType.Invincibility && Time.time < powerupEndTime;

        public float DamageMultiplier => HasDoubleDamage ? 2f : 1f;
        public float SpeedMultiplier => HasSpeedBoost ? 1.5f : 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (activePowerup.HasValue && Time.time >= powerupEndTime)
            {
                EndPowerup();
            }
        }

        public void GrantRandomPowerup()
        {
            var types = System.Enum.GetValues(typeof(PowerupType));
            var randomType = (PowerupType)types.GetValue(Random.Range(0, types.Length));
            GrantPowerup(randomType);
        }

        public void GrantPowerup(PowerupType type)
        {
            if (activePowerup.HasValue)
            {
                EndPowerup();
            }

            activePowerup = type;
            float duration = type switch
            {
                PowerupType.DoubleDamage => doubleDamageDuration,
                PowerupType.SpeedBoost => speedBoostDuration,
                PowerupType.InfiniteAmmo => infiniteAmmoDuration,
                PowerupType.Invincibility => invincibilityDuration,
                _ => 5f
            };
            powerupEndTime = Time.time + duration;

            ApplyPowerupEffects(type);
            ShowPowerupAnnouncement(type);
            CreatePowerupVisual(type);
        }

        private void ApplyPowerupEffects(PowerupType type)
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            switch (type)
            {
                case PowerupType.SpeedBoost:
                    var controller = player.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.ApplySpeedMultiplier(1.5f);
                    }
                    break;

                case PowerupType.Invincibility:
                    var health = player.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.SetInvincible(true);
                    }
                    break;
            }
        }

        private void EndPowerup()
        {
            if (!activePowerup.HasValue) return;

            var player = GameObject.Find("Player");
            if (player != null)
            {
                switch (activePowerup.Value)
                {
                    case PowerupType.SpeedBoost:
                        var controller = player.GetComponent<PlayerController>();
                        if (controller != null)
                        {
                            controller.ApplySpeedMultiplier(1f);
                        }
                        break;

                    case PowerupType.Invincibility:
                        var health = player.GetComponent<PlayerHealth>();
                        if (health != null)
                        {
                            health.SetInvincible(false);
                        }
                        break;
                }
            }

            if (powerupVisual != null)
            {
                Destroy(powerupVisual);
            }

            activePowerup = null;
        }

        private void ShowPowerupAnnouncement(PowerupType type)
        {
            string message = type switch
            {
                PowerupType.DoubleDamage => "DOUBLE DAMAGE!",
                PowerupType.SpeedBoost => "SPEED BOOST!",
                PowerupType.InfiniteAmmo => "INFINITE AMMO!",
                PowerupType.Invincibility => "INVINCIBILITY!",
                _ => "POWERUP!"
            };

            Color color = type switch
            {
                PowerupType.DoubleDamage => new Color(1f, 0.3f, 0.3f),
                PowerupType.SpeedBoost => new Color(0.3f, 0.6f, 1f),
                PowerupType.InfiniteAmmo => new Color(1f, 0.85f, 0.2f),
                PowerupType.Invincibility => Color.white,
                _ => Color.white
            };

            if (FloatingTextManager.Instance != null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    FloatingTextManager.Instance.SpawnText(message, player.transform.position + Vector3.up, color, 1.5f, 32);
                }
            }
        }

        private void CreatePowerupVisual(PowerupType type)
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            powerupVisual = new GameObject("PowerupAura");
            powerupVisual.transform.SetParent(player.transform);
            powerupVisual.transform.localPosition = Vector3.zero;

            var sr = powerupVisual.AddComponent<SpriteRenderer>();
            sr.sprite = CreateAuraSprite(type);
            sr.sortingOrder = 8;
            
            powerupVisual.AddComponent<AuraPulse>();
        }

        private Sprite CreateAuraSprite(PowerupType type)
        {
            int size = 48;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];

            Color auraColor = type switch
            {
                PowerupType.DoubleDamage => new Color(1f, 0.2f, 0.2f, 0.3f),
                PowerupType.SpeedBoost => new Color(0.2f, 0.5f, 1f, 0.3f),
                PowerupType.InfiniteAmmo => new Color(1f, 0.85f, 0.2f, 0.3f),
                PowerupType.Invincibility => new Color(1f, 1f, 1f, 0.4f),
                _ => new Color(1f, 1f, 1f, 0.3f)
            };

            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    if (dist < 1f && dist > 0.7f)
                    {
                        float alpha = (1f - Mathf.Abs(dist - 0.85f) / 0.15f) * auraColor.a;
                        pixels[y * size + x] = new Color(auraColor.r, auraColor.g, auraColor.b, alpha);
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
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 24f);
        }
    }

    public class AuraPulse : MonoBehaviour
    {
        private void Update()
        {
            float scale = 1f + Mathf.Sin(Time.time * 4f) * 0.15f;
            transform.localScale = Vector3.one * scale;
            transform.Rotate(0, 0, Time.deltaTime * 30f);
        }
    }
}
