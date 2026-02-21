using UnityEngine;
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
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobAmount = 0.1f;
        [SerializeField] private bool enableBobbing = true;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;

        [Header("Lifetime")]
        [SerializeField] private float lifetime = 0f;
        [SerializeField] private bool hasLifetime = false;

        private Vector3 startPosition;
        private float bobOffset;

        public PickupType Type => pickupType;
        public int Amount => amount;

        private void Start()
        {
            startPosition = transform.position;
            bobOffset = Random.value * Mathf.PI * 2f;

            if (hasLifetime && lifetime > 0)
            {
                Destroy(gameObject, lifetime);
            }
        }

        private void Update()
        {
            if (enableBobbing)
            {
                float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
                transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            ApplyPickup(other.gameObject);
        }

        private void ApplyPickup(GameObject player)
        {
            bool consumed = false;

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
                        shooting.AddAmmo(amount);
                        consumed = true;
                    }
                    break;

                case PickupType.Scrap:
                case PickupType.Wood:
                case PickupType.Chemicals:
                case PickupType.Electronics:
                    if (ResourceManager.Instance != null)
                    {
                        ResourceType resourceType = ConvertToResourceType(pickupType);
                        ResourceManager.Instance.AddResource(resourceType, amount);
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
                if (Core.DayObjectiveSystem.Instance != null && Core.GameManager.Instance != null &&
                    Core.GameManager.Instance.CurrentState == Core.GameState.DayPhase)
                {
                    Core.DayObjectiveSystem.Instance.AddProgress(1);
                }

                PlayPickupSound();
                Destroy(gameObject);
            }
        }

        private ResourceType ConvertToResourceType(PickupType type)
        {
            return type switch
            {
                PickupType.Scrap => ResourceType.Scrap,
                PickupType.Wood => ResourceType.Wood,
                PickupType.Chemicals => ResourceType.Chemicals,
                PickupType.Electronics => ResourceType.Electronics,
                _ => ResourceType.Scrap
            };
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
    }
}
