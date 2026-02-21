using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Deadlight.Core;
using Deadlight.Player;
using Deadlight.Systems;

namespace Deadlight.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        [Header("Stamina UI")]
        [SerializeField] private Slider staminaBar;
        [SerializeField] private Image staminaFill;

        [Header("Ammo UI")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private Image reloadIndicator;

        [Header("Points UI")]
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI pointsPopup;

        [Header("Time UI")]
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private Slider timeBar;
        [SerializeField] private Image timeBarFill;
        [SerializeField] private Color dayColor = new Color(1f, 0.9f, 0.5f);
        [SerializeField] private Color nightColor = new Color(0.3f, 0.3f, 0.8f);

        [Header("Wave UI")]
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI nightText;
        [SerializeField] private TextMeshProUGUI enemiesText;

        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerShooting playerShooting;

        private void Start()
        {
            FindPlayerReferences();
            SubscribeToEvents();
            InitializeUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void FindPlayerReferences()
        {
            if (playerHealth == null)
                playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerController == null)
                playerController = FindObjectOfType<PlayerController>();
            if (playerShooting == null)
                playerShooting = FindObjectOfType<PlayerShooting>();
        }

        private void SubscribeToEvents()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged += UpdateHealthUI;

            if (playerController != null)
                playerController.OnStaminaChanged += UpdateStaminaUI;

            if (playerShooting != null)
            {
                playerShooting.OnAmmoChanged += UpdateAmmoUI;
                playerShooting.OnReloadStarted += ShowReloadIndicator;
                playerShooting.OnReloadCompleted += HideReloadIndicator;
            }

            if (PointsSystem.Instance != null)
            {
                PointsSystem.Instance.OnPointsChanged += UpdatePointsUI;
                PointsSystem.Instance.OnPointsEarned += ShowPointsPopup;
            }

            var dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnTimeUpdate += UpdateTimeUI;
                dayNightCycle.OnDayStart += OnDayStart;
                dayNightCycle.OnNightStart += OnNightStart;
            }

            var waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnWaveStarted += UpdateWaveUI;
                waveManager.OnEnemyKilled += UpdateEnemiesUI;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged += UpdateNightUI;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= UpdateHealthUI;

            if (playerController != null)
                playerController.OnStaminaChanged -= UpdateStaminaUI;

            if (playerShooting != null)
            {
                playerShooting.OnAmmoChanged -= UpdateAmmoUI;
                playerShooting.OnReloadStarted -= ShowReloadIndicator;
                playerShooting.OnReloadCompleted -= HideReloadIndicator;
            }

            if (PointsSystem.Instance != null)
            {
                PointsSystem.Instance.OnPointsChanged -= UpdatePointsUI;
                PointsSystem.Instance.OnPointsEarned -= ShowPointsPopup;
            }
        }

        private void InitializeUI()
        {
            if (playerHealth != null)
                UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);

            if (playerController != null)
                UpdateStaminaUI(playerController.CurrentStamina);

            if (playerShooting != null)
                UpdateAmmoUI(playerShooting.CurrentAmmo, playerShooting.ReserveAmmo);

            if (PointsSystem.Instance != null)
                UpdatePointsUI(PointsSystem.Instance.CurrentPoints);

            UpdateNightUI(GameManager.Instance?.CurrentNight ?? 1);
            HideReloadIndicator();

            if (pointsPopup != null)
                pointsPopup.gameObject.SetActive(false);
        }

        private void UpdateHealthUI(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }

            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

            if (healthFill != null)
            {
                float percentage = current / max;
                if (percentage > 0.6f)
                    healthFill.color = healthyColor;
                else if (percentage > 0.3f)
                    healthFill.color = damagedColor;
                else
                    healthFill.color = criticalColor;
            }
        }

        private void UpdateStaminaUI(float current)
        {
            if (staminaBar != null && playerController != null)
            {
                staminaBar.maxValue = playerController.MaxStamina;
                staminaBar.value = current;
            }
        }

        private void UpdateAmmoUI(int current, int reserve)
        {
            if (ammoText != null)
                ammoText.text = $"{current} / {reserve}";

            if (weaponNameText != null && playerShooting?.CurrentWeapon != null)
                weaponNameText.text = playerShooting.CurrentWeapon.weaponName;
        }

        private void ShowReloadIndicator()
        {
            if (reloadIndicator != null)
                reloadIndicator.gameObject.SetActive(true);
        }

        private void HideReloadIndicator()
        {
            if (reloadIndicator != null)
                reloadIndicator.gameObject.SetActive(false);
        }

        private void UpdatePointsUI(int points)
        {
            if (pointsText != null)
                pointsText.text = points.ToString("N0");
        }

        private void ShowPointsPopup(int amount, string source)
        {
            if (pointsPopup != null)
            {
                pointsPopup.text = $"+{amount}";
                pointsPopup.gameObject.SetActive(true);
                CancelInvoke(nameof(HidePointsPopup));
                Invoke(nameof(HidePointsPopup), 1.5f);
            }
        }

        private void HidePointsPopup()
        {
            if (pointsPopup != null)
                pointsPopup.gameObject.SetActive(false);
        }

        private void UpdateTimeUI(float timeRemaining)
        {
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }

            var dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (timeBar != null && dayNightCycle != null)
            {
                timeBar.value = 1f - dayNightCycle.NormalizedTime;
            }
        }

        private void OnDayStart()
        {
            if (phaseText != null)
                phaseText.text = "DAY";

            if (timeBarFill != null)
                timeBarFill.color = dayColor;
        }

        private void OnNightStart()
        {
            if (phaseText != null)
                phaseText.text = "NIGHT";

            if (timeBarFill != null)
                timeBarFill.color = nightColor;
        }

        private void UpdateWaveUI(int wave)
        {
            if (waveText != null)
                waveText.text = $"Wave {wave}";
        }

        private void UpdateNightUI(int night)
        {
            if (nightText != null)
                nightText.text = $"Night {night} / {GameManager.Instance?.MaxNights ?? 5}";
        }

        private void UpdateEnemiesUI(int killed)
        {
            var waveManager = FindObjectOfType<WaveManager>();
            if (enemiesText != null && waveManager != null)
                enemiesText.text = $"Enemies: {waveManager.EnemiesRemaining}";
        }
    }
}
