using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Deadlight.Core;
using Deadlight.Data;

namespace Deadlight.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        private Text healthText;
        private Image healthFill;
        private Text ammoText;
        private Image staminaFill;
        private Text waveText;
        private Text nightText;
        private Text enemyCountText;
        private Text statusText;
        private Text reloadHint;
        private Text dayTimerText;
        private Text pointsText;
        private Text throwablesText;

        private Image weaponIcon;
        private Text weaponNameText;
        private Text weaponStatsText;
        private float weaponStatsShowTime = -10f;

        private Image vestFill;
        private Image helmetFill;
        private Text vestLabel;
        private Text helmetLabel;
        private GameObject armorPanel;

        private Player.PlayerHealth playerHealth;
        private Player.PlayerShooting playerShooting;
        private Player.PlayerController playerController;
        private Player.ThrowableSystem throwableSystem;
        private Player.PlayerMedkitSystem playerMedkitSystem;
        private WaveManager waveManager;
        private WaveSpawner waveSpawner;
        private RectTransform healthFillRect;
        private float targetHealthRatio = 1f;
        private float displayedHealthRatio = 1f;
        private const float HealthBarLerpSpeed = 6f;
        private readonly List<float> molotovZoneEndTimes = new List<float>();
        private float molotovInFlightUntil = -1f;
        private const float MolotovFlightHintSeconds = 1.25f;

        public void Initialize(
            Text health, Image hFill, Text ammo, Image sFill,
            Text wave, Text night, Text enemyCount, Text status, Text reload,
            Text dayTimer = null, Text points = null, Text throwables = null)
        {
            healthText = health;
            healthFill = hFill;
            healthFillRect = healthFill != null ? healthFill.rectTransform : null;
            ammoText = ammo;
            staminaFill = sFill;
            waveText = wave;
            nightText = night;
            enemyCountText = enemyCount;
            statusText = status;
            reloadHint = reload;
            dayTimerText = dayTimer;
            pointsText = points;
            throwablesText = throwables;

            ConfigureHealthBar();
            ApplyHealthBar(displayedHealthRatio);

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

        public void SetJournalHintText(Text hint)
        {
            if (hint != null)
                hint.gameObject.SetActive(false);
        }

        private IEnumerator FindPlayerDelayed()
        {
            const float searchDuration = 1.5f;
            float elapsed = 0f;

            while (elapsed < searchDuration && (playerHealth == null || playerShooting == null || playerController == null))
            {
                FindPlayer();
                if (playerHealth != null && playerShooting != null && playerController != null)
                {
                    break;
                }

                elapsed += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            SubscribeEvents();
        }

        private void FindPlayer()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            playerHealth = player.GetComponent<Player.PlayerHealth>();
            playerShooting = player.GetComponent<Player.PlayerShooting>();
            playerController = player.GetComponent<Player.PlayerController>();
            throwableSystem = player.GetComponent<Player.ThrowableSystem>();
            playerMedkitSystem = player.GetComponent<Player.PlayerMedkitSystem>();
        }

        private void SubscribeEvents()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealth;
                playerHealth.OnDamageTaken += OnDamage;
                UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (playerShooting != null)
            {
                playerShooting.OnAmmoChanged += UpdateAmmo;
                playerShooting.OnReloadStarted += ShowReloading;
                playerShooting.OnReloadCompleted += HideReloading;
                playerShooting.OnWeaponChanged += UpdateWeaponDisplay;

                if (playerShooting.CurrentWeapon != null)
                    UpdateWeaponDisplay(playerShooting.CurrentWeapon);

                UpdateAmmo(playerShooting.CurrentAmmo, playerShooting.ReserveAmmo);
            }

            if (throwableSystem != null)
            {
                throwableSystem.OnInventoryChanged += OnThrowableInventoryChanged;
                throwableSystem.OnThrowableUsed += OnThrowableUsed;
                throwableSystem.OnMolotovFireZoneStarted += OnMolotovFireZoneStarted;
                throwableSystem.OnMolotovFireZoneEnded += OnMolotovFireZoneEnded;
            }

            if (playerMedkitSystem != null)
            {
                playerMedkitSystem.OnMedkitCountChanged += OnMedkitInventoryChanged;
            }

            if (throwableSystem != null || playerMedkitSystem != null)
            {
                UpdateUtilityInventoryDisplay();
            }
            else if (throwablesText != null)
            {
                throwablesText.gameObject.SetActive(false);
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

            waveManager = WaveManager.Instance;
            waveSpawner = WaveSpawner.Instance;

            if (waveManager != null)
            {
                waveManager.OnWaveStarted += UpdateWave;
                waveManager.OnEnemyCountChanged += UpdateEnemyCount;
                UpdateEnemyCount(waveManager.EnemiesRemaining);
                if (waveManager.CurrentWave > 0)
                {
                    UpdateWave(waveManager.CurrentWave);
                }
            }
            else if (waveSpawner != null)
            {
                waveSpawner.OnWaveChanged += UpdateWave;
                waveSpawner.OnEnemyCountChanged += UpdateEnemyCount;
                UpdateEnemyCount(waveSpawner.EnemiesAlive);
                if (waveSpawner.CurrentWave > 0)
                {
                    UpdateWave(waveSpawner.CurrentWave);
                }
            }

            if (GameFlowController.Instance != null)
            {
                GameFlowController.Instance.OnDayTimerUpdate += UpdateDayTimer;
            }

            if (Systems.PointsSystem.Instance != null)
            {
                Systems.PointsSystem.Instance.OnPointsChanged += UpdatePoints;
                UpdatePoints(Systems.PointsSystem.Instance.CurrentPoints);
            }

            if (GameManager.Instance != null)
            {
                UpdateNight(GameManager.Instance.CurrentNight);
            }

            ApplyPhaseVisibility(GameManager.Instance?.CurrentState ?? GameState.DayPhase);
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealth;
                playerHealth.OnDamageTaken -= OnDamage;
            }
            if (playerShooting != null)
            {
                playerShooting.OnAmmoChanged -= UpdateAmmo;
                playerShooting.OnReloadStarted -= ShowReloading;
                playerShooting.OnReloadCompleted -= HideReloading;
                playerShooting.OnWeaponChanged -= UpdateWeaponDisplay;
            }
            if (throwableSystem != null)
            {
                throwableSystem.OnInventoryChanged -= OnThrowableInventoryChanged;
                throwableSystem.OnThrowableUsed -= OnThrowableUsed;
                throwableSystem.OnMolotovFireZoneStarted -= OnMolotovFireZoneStarted;
                throwableSystem.OnMolotovFireZoneEnded -= OnMolotovFireZoneEnded;
            }
            if (playerMedkitSystem != null)
            {
                playerMedkitSystem.OnMedkitCountChanged -= OnMedkitInventoryChanged;
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
            if (waveManager != null)
            {
                waveManager.OnWaveStarted -= UpdateWave;
                waveManager.OnEnemyCountChanged -= UpdateEnemyCount;
            }
            else if (waveSpawner != null)
            {
                waveSpawner.OnWaveChanged -= UpdateWave;
                waveSpawner.OnEnemyCountChanged -= UpdateEnemyCount;
            }
            if (GameFlowController.Instance != null)
            {
                GameFlowController.Instance.OnDayTimerUpdate -= UpdateDayTimer;
            }
            if (Systems.PointsSystem.Instance != null)
            {
                Systems.PointsSystem.Instance.OnPointsChanged -= UpdatePoints;
            }
        }

        private void Update()
        {
            AnimateHealthBar();
            UpdateStamina();
            UpdateAmmoFromState();
            UpdateThrowablesFromState();
            UpdateWeaponStatsFade();
        }

        private void ApplyPhaseVisibility(GameState state)
        {
            bool isNight = state == GameState.NightPhase;

            if (waveText != null) waveText.gameObject.SetActive(isNight);
            if (reloadHint != null && !isNight) reloadHint.gameObject.SetActive(false);
        }

        private void UpdateHealth(float current, float max)
        {
            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

            if (healthFill != null)
            {
                float pct = current / Mathf.Max(1f, max);
                targetHealthRatio = Mathf.Clamp01(pct);
                healthFill.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(0.2f, 0.8f, 0.2f), pct);
            }
        }

        private void ConfigureHealthBar()
        {
            if (healthFillRect == null)
            {
                return;
            }

            healthFillRect.pivot = new Vector2(0f, 0.5f);
            healthFillRect.localScale = Vector3.one;
        }

        private void AnimateHealthBar()
        {
            if (healthFillRect == null)
            {
                return;
            }

            displayedHealthRatio = Mathf.MoveTowards(
                displayedHealthRatio,
                targetHealthRatio,
                HealthBarLerpSpeed * Time.deltaTime);

            ApplyHealthBar(displayedHealthRatio);
        }

        private void ApplyHealthBar(float ratio)
        {
            if (healthFillRect == null)
            {
                return;
            }

            var scale = healthFillRect.localScale;
            scale.x = Mathf.Clamp01(ratio);
            scale.y = 1f;
            scale.z = 1f;
            healthFillRect.localScale = scale;
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

        private void OnThrowableInventoryChanged(int grenades, int molotovs)
        {
            UpdateUtilityInventoryDisplay();
        }

        private void OnThrowableUsed(Player.ThrowableType type)
        {
            if (type == Player.ThrowableType.Molotov)
            {
                molotovInFlightUntil = Mathf.Max(molotovInFlightUntil, Time.time + MolotovFlightHintSeconds);
            }

            UpdateUtilityInventoryDisplay();
        }

        private void OnMolotovFireZoneStarted(float duration)
        {
            float safeDuration = Mathf.Max(0.1f, duration);
            molotovZoneEndTimes.Add(Time.time + safeDuration);
            UpdateUtilityInventoryDisplay();
        }

        private void OnMolotovFireZoneEnded()
        {
            PruneExpiredMolotovZones();
            if (molotovZoneEndTimes.Count > 0)
            {
                int earliestIndex = 0;
                float earliestEnd = molotovZoneEndTimes[0];
                for (int i = 1; i < molotovZoneEndTimes.Count; i++)
                {
                    if (molotovZoneEndTimes[i] < earliestEnd)
                    {
                        earliestEnd = molotovZoneEndTimes[i];
                        earliestIndex = i;
                    }
                }

                molotovZoneEndTimes.RemoveAt(earliestIndex);
            }

            UpdateUtilityInventoryDisplay();
        }

        private void OnMedkitInventoryChanged(int medkits, int maxMedkits)
        {
            UpdateUtilityInventoryDisplay();
        }

        private void UpdateUtilityInventoryDisplay()
        {
            if (throwablesText == null)
            {
                return;
            }

            if (throwableSystem == null && playerMedkitSystem == null)
            {
                throwablesText.gameObject.SetActive(false);
                return;
            }

            throwablesText.gameObject.SetActive(true);

            string utilityText = string.Empty;
            if (throwableSystem != null)
            {
                utilityText = $"Q GRENADE {throwableSystem.GrenadeCount}/{throwableSystem.MaxGrenades}\n" +
                              $"G MOLOTOV {throwableSystem.MolotovCount}/{throwableSystem.MaxMolotovs}";
            }

            if (playerMedkitSystem != null)
            {
                if (utilityText.Length > 0)
                {
                    utilityText += "\n";
                }

                utilityText += $"C MEDKIT {playerMedkitSystem.MedkitCount}/{playerMedkitSystem.MaxMedkits}";
                if (playerMedkitSystem.IsApplying)
                {
                    utilityText += " (APPLYING)";
                }
            }

            int activeFireZones = GetActiveMolotovZoneCount();
            if (activeFireZones > 0)
            {
                float maxRemaining = GetMaxMolotovZoneRemainingSeconds();
                if (utilityText.Length > 0)
                {
                    utilityText += "\n";
                }

                utilityText += $"<color=#FF6A33>G FIRE BURNING {maxRemaining:0.0}s x{activeFireZones}</color>";
            }
            else if (molotovInFlightUntil > Time.time)
            {
                if (utilityText.Length > 0)
                {
                    utilityText += "\n";
                }

                utilityText += "<color=#FFB347>G MOLOTOV IN FLIGHT</color>";
            }

            throwablesText.text = utilityText;
        }

        private void UpdateThrowablesFromState()
        {
            if (throwablesText == null)
            {
                return;
            }

            if (throwableSystem == null && playerMedkitSystem == null)
            {
                throwablesText.gameObject.SetActive(false);
                return;
            }

            UpdateUtilityInventoryDisplay();
        }

        private int GetActiveMolotovZoneCount()
        {
            PruneExpiredMolotovZones();
            return molotovZoneEndTimes.Count;
        }

        private float GetMaxMolotovZoneRemainingSeconds()
        {
            PruneExpiredMolotovZones();
            float now = Time.time;
            float maxRemaining = 0f;
            for (int i = 0; i < molotovZoneEndTimes.Count; i++)
            {
                float remaining = molotovZoneEndTimes[i] - now;
                if (remaining > maxRemaining)
                {
                    maxRemaining = remaining;
                }
            }

            return Mathf.Max(0f, maxRemaining);
        }

        private void PruneExpiredMolotovZones()
        {
            float now = Time.time;
            for (int i = molotovZoneEndTimes.Count - 1; i >= 0; i--)
            {
                if (molotovZoneEndTimes[i] <= now)
                {
                    molotovZoneEndTimes.RemoveAt(i);
                }
            }
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
            {
                waveText.gameObject.SetActive(true);
                waveText.text = $"WAVE {wave:00}";
            }

            ShowStatus($"WAVE {wave} INCOMING!", 2f);
        }

        private void UpdateNight(int night)
        {
            if (nightText != null)
            {
                int level = Core.GameManager.GetLevelForNight(night);
                int nwl = Core.GameManager.GetNightWithinLevel(night);
                nightText.text = $"LEVEL {level}";

                if (waveText != null && (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.NightPhase))
                {
                    waveText.text = $"NIGHT {nwl}";
                }
            }
        }

        private void UpdateEnemyCount(int count)
        {
            if (enemyCountText != null)
                enemyCountText.text = count.ToString();
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
                pointsText.text = points.ToString();
        }

        private void UpdateWeaponDisplay(WeaponData weapon)
        {
            if (weapon == null) return;

            if (weaponNameText != null)
                weaponNameText.text = weapon.weaponName.ToUpper();

            if (weaponStatsText != null)
            {
                weaponStatsText.text = $"DMG {weapon.damage:0}  ROF {weapon.fireRate:0.0}";
                weaponStatsText.gameObject.SetActive(true);
                weaponStatsShowTime = Time.time;
            }

            if (weaponIcon != null)
            {
                try { weaponIcon.sprite = Visuals.ProceduralSpriteGenerator.CreateWeaponIcon(weapon.weaponType); }
                catch { }
            }
        }

        private void UpdateWeaponStatsFade()
        {
            if (weaponStatsText == null) return;
            if (!weaponStatsText.gameObject.activeSelf && weaponStatsShowTime > -10f)
                weaponStatsText.gameObject.SetActive(true);
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
            ApplyPhaseVisibility(state);

            switch (state)
            {
                case GameState.DayPhase:
                    ShowStatus("DAYTIME - Prepare and explore!", 3f);
                    break;
                case GameState.NightPhase:
                    ShowStatus("LEVEL ACTIVE - Survive!", 3f);
                    break;
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
    }
}
