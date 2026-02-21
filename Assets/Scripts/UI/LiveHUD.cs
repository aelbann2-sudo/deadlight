using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.Core;
using Deadlight.Data;

namespace Deadlight.UI
{
    public class LiveHUD : MonoBehaviour
    {
        private Text healthText;
        private Image healthFill;
        private Text ammoText;
        private Image staminaFill;
        private Text waveText;
        private Text nightText;
        private Text enemyCountText;
        private Text statusText;
        private GameObject gameOverPanel;
        private Text gameOverText;
        private Text reloadHint;
        private Text dayTimerText;
        private Text pointsText;

        private Image weaponIcon;
        private Text weaponNameText;
        private Text weaponStatsText;

        private Image vestFill;
        private Image helmetFill;
        private Text vestLabel;
        private Text helmetLabel;
        private GameObject armorPanel;

        private Player.PlayerHealth playerHealth;
        private Player.PlayerShooting playerShooting;
        private Player.PlayerController playerController;

        public void Initialize(
            Text health, Image hFill, Text ammo, Image sFill,
            Text wave, Text night, Text enemyCount, Text status,
            GameObject goPanel, Text goText, Text reload,
            Text dayTimer = null, Text points = null)
        {
            healthText = health;
            healthFill = hFill;
            ammoText = ammo;
            staminaFill = sFill;
            waveText = wave;
            nightText = night;
            enemyCountText = enemyCount;
            statusText = status;
            gameOverPanel = goPanel;
            gameOverText = goText;
            reloadHint = reload;
            dayTimerText = dayTimer;
            pointsText = points;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            StartCoroutine(FindPlayerDelayed());
        }

        public void SetWeaponHUD(Image icon, Text wName, Text wStats)
        {
            weaponIcon = icon;
            weaponNameText = wName;
            weaponStatsText = wStats;
        }

        public void SetArmorHUD(Image vFill, Image hFill, Text vLabel, Text hLabel, GameObject panel)
        {
            vestFill = vFill;
            helmetFill = hFill;
            vestLabel = vLabel;
            helmetLabel = hLabel;
            armorPanel = panel;
        }

        private IEnumerator FindPlayerDelayed()
        {
            yield return new WaitForSeconds(0.1f);
            FindPlayer();
            SubscribeEvents();
        }

        private void FindPlayer()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            playerHealth = player.GetComponent<Player.PlayerHealth>();
            playerShooting = player.GetComponent<Player.PlayerShooting>();
            playerController = player.GetComponent<Player.PlayerController>();
        }

        private void SubscribeEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealth;
                playerHealth.OnDamageTaken += OnDamage;
                playerHealth.OnPlayerDeath += OnPlayerDied;
            }

            if (playerShooting != null)
            {
                playerShooting.OnAmmoChanged += UpdateAmmo;
                playerShooting.OnReloadStarted += ShowReloading;
                playerShooting.OnReloadCompleted += HideReloading;
                playerShooting.OnWeaponChanged += UpdateWeaponDisplay;

                if (playerShooting.CurrentWeapon != null)
                    UpdateWeaponDisplay(playerShooting.CurrentWeapon);
            }

            if (Player.PlayerArmor.Instance != null)
            {
                Player.PlayerArmor.Instance.OnArmorChanged += UpdateArmorDisplay;
                UpdateArmorDisplay(
                    Player.PlayerArmor.Instance.VestDurability,
                    Player.PlayerArmor.Instance.VestMax,
                    Player.PlayerArmor.Instance.HelmetDurability,
                    Player.PlayerArmor.Instance.HelmetMax);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnNightChanged += UpdateNight;
            }

            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.OnWaveChanged += UpdateWave;
                WaveSpawner.Instance.OnEnemyCountChanged += UpdateEnemyCount;
            }

            if (GameFlowController.Instance != null)
            {
                GameFlowController.Instance.OnDayTimerUpdate += UpdateDayTimer;
            }

            if (Systems.PointsSystem.Instance != null)
            {
                Systems.PointsSystem.Instance.OnPointsChanged += UpdatePoints;
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealth;
                playerHealth.OnDamageTaken -= OnDamage;
                playerHealth.OnPlayerDeath -= OnPlayerDied;
            }
            if (playerShooting != null)
            {
                playerShooting.OnAmmoChanged -= UpdateAmmo;
                playerShooting.OnReloadStarted -= ShowReloading;
                playerShooting.OnReloadCompleted -= HideReloading;
                playerShooting.OnWeaponChanged -= UpdateWeaponDisplay;
            }
            if (Player.PlayerArmor.Instance != null)
            {
                Player.PlayerArmor.Instance.OnArmorChanged -= UpdateArmorDisplay;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnNightChanged -= UpdateNight;
            }
        }

        private void Update()
        {
            UpdateStamina();
            UpdateAmmoFromState();
            
            if (Input.GetKeyDown(KeyCode.Return) && gameOverPanel != null && gameOverPanel.activeSelf)
            {
                RestartGame();
            }
        }

        private void UpdateHealth(float current, float max)
        {
            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

            if (healthFill != null)
            {
                float pct = current / max;
                healthFill.fillAmount = pct;
                healthFill.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(0.2f, 0.8f, 0.2f), pct);
            }
        }

        private void OnDamage(float amount)
        {
            if (GameEffects.Instance != null)
            {
                GameEffects.Instance.DamageFlash();
                GameEffects.Instance.ScreenShake(0.15f, 0.15f);
            }
        }

        private void UpdateAmmo(int current, int reserve)
        {
            if (ammoText != null)
                ammoText.text = $"{current} / {reserve}";
        }

        private void UpdateAmmoFromState()
        {
            if (playerShooting == null || ammoText == null) return;
            ammoText.text = $"{playerShooting.CurrentAmmo} / {playerShooting.ReserveAmmo}";
        }

        private void UpdateStamina()
        {
            if (playerController == null || staminaFill == null) return;
            float pct = playerController.CurrentStamina / playerController.MaxStamina;
            staminaFill.fillAmount = pct;
            staminaFill.color = Color.Lerp(new Color(0.8f, 0.6f, 0.1f), new Color(0.2f, 0.6f, 0.9f), pct);
        }

        private void UpdateWave(int wave)
        {
            if (waveText != null)
                waveText.text = $"Wave {wave}";

            ShowStatus($"WAVE {wave} INCOMING!", 2f);
        }

        private void UpdateNight(int night)
        {
            if (nightText != null)
                nightText.text = $"Night {night}";
        }

        private void UpdateEnemyCount(int count)
        {
            if (enemyCountText != null)
                enemyCountText.text = $"Enemies: {count}";
        }

        private void UpdateDayTimer(float timeRemaining)
        {
            if (dayTimerText == null) return;
            if (GameManager.Instance?.CurrentState == GameState.DayPhase)
            {
                int mins = Mathf.FloorToInt(timeRemaining / 60f);
                int secs = Mathf.FloorToInt(timeRemaining % 60f);
                dayTimerText.text = $"Day ends in {mins}:{secs:00}";
                dayTimerText.gameObject.SetActive(true);
            }
            else
            {
                dayTimerText.gameObject.SetActive(false);
            }
        }

        private void UpdatePoints(int points)
        {
            if (pointsText != null)
                pointsText.text = $"Score: {points}";
        }

        private void UpdateWeaponDisplay(WeaponData weapon)
        {
            if (weapon == null) return;

            if (weaponNameText != null)
                weaponNameText.text = weapon.weaponName.ToUpper();

            if (weaponStatsText != null)
                weaponStatsText.text = $"DMG: {weapon.damage:0}  ROF: {weapon.fireRate:0.0}";

            if (weaponIcon != null)
            {
                try { weaponIcon.sprite = Visuals.ProceduralSpriteGenerator.CreateWeaponIcon(weapon.weaponType); }
                catch { }
            }
        }

        private void UpdateArmorDisplay(float vest, float vestMax, float helmet, float helmetMax)
        {
            bool hasArmor = vestMax > 0 || helmetMax > 0;
            if (armorPanel != null) armorPanel.SetActive(hasArmor);

            if (vestFill != null)
            {
                vestFill.fillAmount = vestMax > 0 ? vest / vestMax : 0;
                vestFill.gameObject.SetActive(vestMax > 0);
            }
            if (vestLabel != null)
                vestLabel.text = vestMax > 0 ? $"VEST {Mathf.CeilToInt(vest)}" : "";

            if (helmetFill != null)
            {
                helmetFill.fillAmount = helmetMax > 0 ? helmet / helmetMax : 0;
                helmetFill.gameObject.SetActive(helmetMax > 0);
            }
            if (helmetLabel != null)
                helmetLabel.text = helmetMax > 0 ? $"HELM {Mathf.CeilToInt(helmet)}" : "";
        }

        private void ShowReloading()
        {
            if (reloadHint != null)
            {
                reloadHint.gameObject.SetActive(true);
                reloadHint.text = "RELOADING...";
            }
        }

        private void HideReloading()
        {
            if (reloadHint != null)
                reloadHint.gameObject.SetActive(false);
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.DayPhase:
                    ShowStatus("DAYTIME - Prepare and explore!", 3f);
                    break;
                case GameState.NightPhase:
                    ShowStatus("NIGHT FALLS - Survive!", 3f);
                    break;
                case GameState.Victory:
                    ShowGameOver("YOU SURVIVED!", new Color(0.2f, 0.8f, 0.3f));
                    break;
            }
        }

        private void OnPlayerDied()
        {
            ShowGameOver("YOU DIED", new Color(0.8f, 0.1f, 0.1f));
        }

        private void ShowGameOver(string text, Color color)
        {
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
            {
                gameOverText.text = text + "\n\nPress ENTER to restart";
                gameOverText.color = color;
            }
        }

        private void ShowStatus(string text, float duration)
        {
            if (statusText == null) return;
            StopCoroutine("StatusRoutine");
            StartCoroutine(StatusRoutine(text, duration));
        }

        private IEnumerator StatusRoutine(string text, float duration)
        {
            statusText.text = text;
            statusText.gameObject.SetActive(true);
            
            float fadeIn = 0.3f;
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                statusText.color = new Color(1, 1, 1, elapsed / fadeIn);
                yield return null;
            }

            yield return new WaitForSeconds(duration);

            float fadeOut = 0.5f;
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                statusText.color = new Color(1, 1, 1, 1f - elapsed / fadeOut);
                yield return null;
            }

            statusText.gameObject.SetActive(false);
        }

        private void RestartGame()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
