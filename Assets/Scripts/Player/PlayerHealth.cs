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
        [SerializeField] private AudioClip lowHealthHeartbeat;
        [SerializeField, Range(0.1f, 0.9f)] private float lowHealthThreshold = 0.3f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvincible => isInvincible;

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamageTaken;
        public event Action OnPlayerDeath;

        private Color originalColor;
        private AudioSource heartbeatSource;
        private bool lowHealthLoopActive;

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

            heartbeatSource = gameObject.AddComponent<AudioSource>();
            heartbeatSource.playOnAwake = false;
            heartbeatSource.loop = true;
            heartbeatSource.spatialBlend = 0f;
            heartbeatSource.dopplerLevel = 0f;
            heartbeatSource.volume = 0f;
        }

        private void Start()
        {
            GenerateProceduralSounds();
            ApplyCampaignBalance();
            UpdateLowHealthLoop();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void ApplyCampaignBalance()
        {
            if (GameManager.Instance?.CurrentBalance != null)
            {
                var settings = GameManager.Instance.CurrentBalance;
                maxHealth *= settings.playerHealthMultiplier;
                currentHealth = maxHealth;
            }

            if (PlayerUpgrades.Instance != null && PlayerUpgrades.Instance.BonusHealth > 0)
            {
                maxHealth += PlayerUpgrades.Instance.BonusHealth;
                currentHealth = maxHealth;
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive || isInvincible) return;

            float actualDamage = damage;

            if (PlayerArmor.Instance != null)
            {
                actualDamage = PlayerArmor.Instance.AbsorbDamage(actualDamage);
            }

            if (GameManager.Instance?.CurrentBalance != null)
            {
                actualDamage *= GameManager.Instance.CurrentBalance.playerDamageTakenMultiplier;
            }

            currentHealth = Mathf.Max(0, currentHealth - actualDamage);
            
            OnDamageTaken?.Invoke(actualDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateLowHealthLoop();

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

            StopLowHealthLoop();
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
            
            if (GameManager.Instance?.CurrentBalance != null)
            {
                actualHeal *= GameManager.Instance.CurrentBalance.healthPickupMultiplier;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + actualHeal);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateLowHealthLoop();

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
            UpdateLowHealthLoop();
        }

        public void FullHeal()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            UpdateLowHealthLoop();
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

        private void GenerateProceduralSounds()
        {
            try
            {
                if (hurtSound == null)
                    hurtSound = Deadlight.Audio.ProceduralAudioGenerator.GenerateZombieHitReact();
                if (deathSound == null)
                    deathSound = Deadlight.Audio.ProceduralAudioGenerator.GenerateExplosion();
                if (lowHealthHeartbeat == null)
                    lowHealthHeartbeat = Deadlight.Audio.ProceduralAudioGenerator.GenerateHeartbeat();
            }
            catch (System.Exception) { }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            float volumeScale = clip == deathSound ? 0.82f : 0.62f;
            float pitch = clip == deathSound ? 0.92f : 1f;
            float pitchJitter = clip == deathSound ? 0.03f : 0.06f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(clip, transform.position, volumeScale, pitch, pitchJitter);
                if (clip == deathSound)
                {
                    AudioManager.Instance.SignalCombatPeak(0.08f, 0.75f);
                }
                return;
            }

            if (audioSource != null)
            {
                audioSource.pitch = pitch + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
                audioSource.PlayOneShot(clip, volumeScale);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, transform.position, volumeScale);
            }
        }

        private void UpdateLowHealthLoop()
        {
            if (heartbeatSource == null || lowHealthHeartbeat == null || maxHealth <= 0f || !IsAlive)
            {
                StopLowHealthLoop();
                return;
            }

            float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
            bool shouldPlay = healthRatio <= lowHealthThreshold;
            if (!shouldPlay)
            {
                StopLowHealthLoop();
                return;
            }

            float normalizedDanger = 1f - Mathf.Clamp01(healthRatio / Mathf.Max(0.01f, lowHealthThreshold));
            heartbeatSource.clip = lowHealthHeartbeat;
            heartbeatSource.pitch = Mathf.Lerp(0.88f, 1.03f, normalizedDanger);
            heartbeatSource.volume = Mathf.Lerp(0.03f, 0.13f, normalizedDanger);

            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }

            lowHealthLoopActive = true;
        }

        private void StopLowHealthLoop()
        {
            if (!lowHealthLoopActive && heartbeatSource != null && !heartbeatSource.isPlaying)
            {
                return;
            }

            if (heartbeatSource != null)
            {
                heartbeatSource.Stop();
                heartbeatSource.clip = null;
            }

            lowHealthLoopActive = false;
        }

        public void SetInvincible(bool invincible)
        {
            isInvincible = invincible;
        }
    }
}
