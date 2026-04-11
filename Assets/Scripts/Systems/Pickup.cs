using UnityEngine;
using Deadlight.Core;
using Deadlight.Player;

namespace Deadlight.Systems
{
    public enum PickupType
    {
        Health,
        Ammo,
        Scrap,
        Wood,
        Chemicals,
        Electronics,
        Points,
        Powerup
    }

    public class Pickup : MonoBehaviour
    {
        [Header("Pickup Settings")]
        [SerializeField] private PickupType pickupType = PickupType.Health;
        [SerializeField] private int amount = 25;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;

        [Header("Lifetime")]
        [SerializeField] private float lifetime = 0f;
        [SerializeField] private bool hasLifetime = false;

        private Collider2D pickupCollider;
        private bool consumed;
        private Collider2D playerCollider;

        public PickupType Type => pickupType;
        public int Amount => amount;

        private void Awake()
        {
            NormalizePickupType();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            pickupCollider = GetComponent<Collider2D>();
        }

        private void OnValidate()
        {
            NormalizePickupType();
        }

        private void Start()
        {
            if (pickupSound == null)
            {
                try { pickupSound = Deadlight.Audio.ProceduralAudioGenerator.GeneratePickup(); }
                catch (System.Exception) { }
            }

            if (hasLifetime && lifetime > 0)
            {
                Destroy(gameObject, lifetime);
            }

            TryConsumeNearbyPlayer();
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            TryConsumeNearbyPlayer();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryConsume(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryConsume(other);
        }

        private void TryConsumeNearbyPlayer()
        {
            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<Collider2D>();
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
                TryConsume(playerCollider);
            }
        }

        private void TryConsume(Collider2D other)
        {
            if (consumed || !other.CompareTag("Player"))
            {
                return;
            }

            if (pickupCollider == null)
            {
                pickupCollider = GetComponent<Collider2D>();
            }

            if (!PickupContactUtility.IsTightPickupContact(pickupCollider, spriteRenderer, other))
            {
                return;
            }

            ApplyPickup(other.gameObject);
        }

        private void ApplyPickup(GameObject player)
        {
            bool consumed = false;
            PickupType displayType = pickupType;
            int displayAmount = amount;

            switch (pickupType)
            {
                case PickupType.Health:
                    var health = player.GetComponent<PlayerHealth>();
                    if (health != null && health.CurrentHealth < health.MaxHealth)
                    {
                        health.Heal(amount);
                        consumed = true;
                    }
                    break;

                case PickupType.Ammo:
                    var shooting = player.GetComponent<PlayerShooting>();
                    if (shooting != null)
                    {
                        int added = shooting.AddAmmo(amount);
                        if (added > 0)
                        {
                            displayAmount = added;
                            consumed = true;
                        }
                    }
                    break;

                case PickupType.Scrap:
                case PickupType.Wood:
                case PickupType.Chemicals:
                case PickupType.Electronics:
                    if (GameManager.Instance == null || GameManager.Instance.CraftingEnabled)
                    {
                        if (ResourceManager.Instance != null)
                        {
                            ResourceType resourceType = pickupType switch
                            {
                                PickupType.Scrap => ResourceType.Scrap,
                                PickupType.Wood => ResourceType.Wood,
                                PickupType.Chemicals => ResourceType.Chemicals,
                                PickupType.Electronics => ResourceType.Electronics,
                                _ => ResourceType.Scrap
                            };

                            ResourceManager.Instance.AddResource(resourceType, Mathf.Max(1, amount));
                            CraftingSystem.Instance?.NotifyResourceCollected(resourceType, Mathf.Max(1, amount), transform.position);
                            consumed = true;
                        }
                    }
                    else if (PointsSystem.Instance != null)
                    {
                        int bonusPoints = Mathf.Max(1, amount);
                        PointsSystem.Instance.AddPoints(bonusPoints, "Resource Pickup");
                        displayType = PickupType.Points;
                        displayAmount = bonusPoints;
                        consumed = true;
                    }
                    break;

                case PickupType.Points:
                    if (PointsSystem.Instance != null)
                    {
                        PointsSystem.Instance.AddPoints(amount, "Pickup");
                        consumed = true;
                    }
                    break;
            }

            if (consumed)
            {
                this.consumed = true;
                if (Core.DayObjectiveSystem.Instance != null && Core.GameManager.Instance != null &&
                    Core.GameManager.Instance.CurrentState == Core.GameState.DayPhase)
                {
                    Core.DayObjectiveSystem.Instance.AddProgress(1);
                }

                UI.GameplayHelpSystem.Instance?.ShowPickup(displayType, displayAmount);
                PlayPickupSound();
                Destroy(gameObject);
            }
        }

        private void PlayPickupSound()
        {
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
        }

        public void SetPickupType(PickupType type)
        {
            pickupType = type;
            NormalizePickupType();
        }

        public void SetAmount(int newAmount)
        {
            amount = newAmount;
        }

        public void SetLifetime(float seconds)
        {
            lifetime = seconds;
            hasLifetime = seconds > 0;

            if (hasLifetime)
            {
                Destroy(gameObject, lifetime);
            }
        }

        private void NormalizePickupType()
        {
            pickupType = PickupSpawner.SanitizePickupType(pickupType);
        }
    }
}
