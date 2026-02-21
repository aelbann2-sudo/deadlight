using UnityEngine;
using Deadlight.Core;
using System;

namespace Deadlight.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Invincibility")]
        [SerializeField] private float invincibilityDuration = 0.5f;
        [SerializeField] private bool isInvincible = false;

        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip hurtSound;
        [SerializeField] private AudioClip deathSound;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvincible => isInvincible;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamageTaken;
        public event Action OnPlayerDeath;

        private Color originalColor;

        private void Awake()
        {
            currentHealth = maxHealth;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            ApplyDifficultyModifiers();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void ApplyDifficultyModifiers()
        {
            if (GameManager.Instance?.CurrentSettings != null)
            {
                var settings = GameManager.Instance.CurrentSettings;
                maxHealth *= settings.playerHealthMultiplier;
                currentHealth = maxHealth;
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive || isInvincible) return;

            float actualDamage = damage;
            
            if (GameManager.Instance?.CurrentSettings != null)
            {
                actualDamage *= GameManager.Instance.CurrentSettings.playerDamageTakenMultiplier;
            }

            currentHealth = Mathf.Max(0, currentHealth - actualDamage);
            
            OnDamageTaken?.Invoke(actualDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            PlaySound(hurtSound);
            StartCoroutine(DamageFlashCoroutine());
            StartCoroutine(InvincibilityCoroutine());

            Debug.Log($"[PlayerHealth] Took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("[PlayerHealth] Player died!");

            PlaySound(deathSound);
            OnPlayerDeath?.Invoke();

            var controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetCanMove(false);
            }

            var shooting = GetComponent<PlayerShooting>();
            if (shooting != null)
            {
                shooting.enabled = false;
            }

            GameManager.Instance?.OnPlayerDeath();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;

            float actualHeal = amount;
            
            if (GameManager.Instance?.CurrentSettings != null)
            {
                actualHeal *= GameManager.Instance.CurrentSettings.healthPickupMultiplier;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + actualHeal);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"[PlayerHealth] Healed {actualHeal}. Health: {currentHealth}/{maxHealth}");
        }

        public void SetMaxHealth(float newMax, bool healToFull = false)
        {
            float healthPercentage = currentHealth / maxHealth;
            maxHealth = newMax;

            if (healToFull)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = maxHealth * healthPercentage;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void FullHeal()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            if (spriteRenderer == null) yield break;

            spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }

        private System.Collections.IEnumerator InvincibilityCoroutine()
        {
            isInvincible = true;

            float elapsed = 0f;
            while (elapsed < invincibilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            isInvincible = false;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(clip);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(clip, transform.position);
                }
            }
        }

        public void SetInvincible(bool invincible)
        {
            isInvincible = invincible;
        }
    }
}
