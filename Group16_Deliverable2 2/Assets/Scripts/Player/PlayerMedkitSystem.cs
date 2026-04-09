using System.Collections;
using Deadlight.Core;
using Deadlight.UI;
using UnityEngine;

namespace Deadlight.Player
{
    public class PlayerMedkitSystem : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private int medkitCount = 0;
        [SerializeField] private int maxMedkits = 5;

        [Header("Use Settings")]
        [SerializeField] private KeyCode useKey = KeyCode.C;
        [SerializeField] private float applyDuration = 2.5f;
        [SerializeField] private float healAmount = 100f;
        [SerializeField] private bool requireMissingHealth = true;

        private PlayerHealth playerHealth;
        private PlayerShooting playerShooting;
        private bool isApplying;

        public int MedkitCount => medkitCount;
        public int MaxMedkits => maxMedkits;
        public bool IsApplying => isApplying;
        public float ApplyDuration => applyDuration;

        public event System.Action<int, int> OnMedkitCountChanged;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            playerShooting = GetComponent<PlayerShooting>();
        }

        private void Start()
        {
            OnMedkitCountChanged?.Invoke(medkitCount, maxMedkits);
        }

        private void Update()
        {
            if (isApplying)
            {
                return;
            }

            if (GameManager.Instance != null && (!GameManager.Instance.IsGameplayState || GameManager.Instance.IsPaused))
            {
                return;
            }

            if (Input.GetKeyDown(useKey))
            {
                TryUseMedkit();
            }
        }

        public bool CanUseMedkit()
        {
            if (medkitCount <= 0 || isApplying || playerHealth == null || !playerHealth.IsAlive)
            {
                return false;
            }

            if (!requireMissingHealth)
            {
                return true;
            }

            return playerHealth.CurrentHealth < playerHealth.MaxHealth - 0.01f;
        }

        public bool HasCapacity()
        {
            return medkitCount < maxMedkits;
        }

        public bool AddMedkits(int count)
        {
            if (count <= 0)
            {
                return false;
            }

            int before = medkitCount;
            medkitCount = Mathf.Clamp(medkitCount + count, 0, maxMedkits);
            if (medkitCount == before)
            {
                return false;
            }

            OnMedkitCountChanged?.Invoke(medkitCount, maxMedkits);
            return true;
        }

        public void ResetMedkits()
        {
            medkitCount = 0;
            isApplying = false;
            OnMedkitCountChanged?.Invoke(medkitCount, maxMedkits);
        }

        private void TryUseMedkit()
        {
            if (!CanUseMedkit())
            {
                return;
            }

            StartCoroutine(ApplyMedkitRoutine());
        }

        private IEnumerator ApplyMedkitRoutine()
        {
            isApplying = true;
            OnMedkitCountChanged?.Invoke(medkitCount, maxMedkits);

            bool shootingWasEnabled = playerShooting != null && playerShooting.enabled;
            if (playerShooting != null)
            {
                playerShooting.enabled = false;
            }

            float elapsed = 0f;
            bool interrupted = false;
            while (elapsed < applyDuration)
            {
                if (playerHealth == null || !playerHealth.IsAlive)
                {
                    interrupted = true;
                    break;
                }

                if (GameManager.Instance != null && !GameManager.Instance.IsGameplayState)
                {
                    interrupted = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            bool hasMissingHealth = !requireMissingHealth ||
                                    (playerHealth != null && playerHealth.CurrentHealth < playerHealth.MaxHealth - 0.01f);
            bool canApply = !interrupted && playerHealth != null && playerHealth.IsAlive && medkitCount > 0 && hasMissingHealth;
            if (canApply)
            {
                float before = playerHealth.CurrentHealth;
                medkitCount--;
                playerHealth.Heal(healAmount);
                int healed = Mathf.Max(1, Mathf.RoundToInt(playerHealth.CurrentHealth - before));
                OnMedkitCountChanged?.Invoke(medkitCount, maxMedkits);
                GameplayHelpSystem.Instance?.ShowItem(GameplayGuideContent.ItemIds.Health, healed);
            }

            if (playerShooting != null)
            {
                playerShooting.enabled = shootingWasEnabled && playerHealth != null && playerHealth.IsAlive;
            }

            isApplying = false;
            OnMedkitCountChanged?.Invoke(medkitCount, maxMedkits);
        }
    }
}
