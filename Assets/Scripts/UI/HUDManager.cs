using Deadlight.Core;
using Deadlight.Player;
using Deadlight.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private DayNightCycle dayNightCycle;
        private WaveManager waveManager;
        private bool useRuntimeFallbackHUD;
        private bool showControls = true;

        private float cachedHealthCurrent = 100f;
        private float cachedHealthMax = 100f;
        private float cachedStaminaCurrent = 100f;
        private float cachedStaminaMax = 100f;
        private int cachedAmmoCurrent;
        private int cachedAmmoReserve;
        private int cachedMagazineSize = 12;
        private int cachedPoints;
        private int cachedWave;
        private int cachedNight = 1;
        private int cachedEnemies;
        private int cachedKills;
        private string cachedWeaponName = "Pistol";
        private string cachedPhase = "DAY";
        private string cachedDifficulty = "NORMAL";
        private float cachedTimeRemaining;
        private bool cachedReloading;

        private GUIStyle bodyStyle;
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle rightStyle;
        private GUIStyle accentStyle;
        private GUIStyle warningStyle;
        private GUIStyle ammoBigStyle;
        private GUIStyle smallStyle;

        private Texture2D panelTex;
        private Texture2D borderTex;
        private Texture2D barBackTex;
        private Texture2D healthBarTex;
        private Texture2D staminaBarTex;
        private Texture2D ammoBarTex;
        private Texture2D warningTex;
        private Texture2D reticleTex;

        private static readonly Color UiPanel = new Color(0.05f, 0.08f, 0.06f, 0.88f);
        private static readonly Color UiBorder = new Color(0.32f, 0.58f, 0.4f, 1f);
        private static readonly Color UiText = new Color(0.86f, 0.95f, 0.87f, 1f);
        private static readonly Color UiAccent = new Color(0.78f, 0.92f, 0.56f, 1f);
        private static readonly Color UiAmmo = new Color(0.98f, 0.85f, 0.4f, 1f);
        private static readonly Color UiWarning = new Color(1f, 0.35f, 0.2f, 1f);

        private void Start()
        {
            FindPlayerReferences();
            SubscribeToEvents();
            InitializeUI();
            SetupRuntimeFallbackIfNeeded();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showControls = !showControls;
            }

            if (!useRuntimeFallbackHUD)
            {
                return;
            }

            FindPlayerReferences();

            if (playerHealth != null)
            {
                cachedHealthCurrent = playerHealth.CurrentHealth;
                cachedHealthMax = playerHealth.MaxHealth;
            }

            if (playerController != null)
            {
                cachedStaminaCurrent = playerController.CurrentStamina;
                cachedStaminaMax = playerController.MaxStamina;
            }

            if (playerShooting != null)
            {
                cachedAmmoCurrent = playerShooting.CurrentAmmo;
                cachedAmmoReserve = playerShooting.ReserveAmmo;
                cachedReloading = playerShooting.IsReloading;

                if (playerShooting.CurrentWeapon != null)
                {
                    cachedWeaponName = playerShooting.CurrentWeapon.weaponName;
                    cachedMagazineSize = Mathf.Max(1, playerShooting.CurrentWeapon.magazineSize);
                }
            }

            if (PointsSystem.Instance != null)
            {
                cachedPoints = PointsSystem.Instance.CurrentPoints;
                cachedKills = PointsSystem.Instance.EnemiesKilled;
            }

            if (waveManager == null)
            {
                waveManager = FindObjectOfType<WaveManager>();
            }

            if (waveManager != null)
            {
                cachedWave = waveManager.CurrentWave;
                cachedEnemies = waveManager.EnemiesRemaining;
            }

            if (dayNightCycle == null)
            {
                dayNightCycle = FindObjectOfType<DayNightCycle>();
            }

            if (dayNightCycle != null)
            {
                cachedTimeRemaining = dayNightCycle.TimeRemaining;
                cachedPhase = dayNightCycle.IsDay ? "DAY" : "NIGHT";
            }

            if (GameManager.Instance != null)
            {
                cachedNight = GameManager.Instance.CurrentNight;
                cachedDifficulty = GameManager.Instance.CurrentDifficulty.ToString().ToUpper();

                if (GameManager.Instance.IsPaused)
                {
                    cachedPhase = "PAUSED";
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (useRuntimeFallbackHUD)
            {
                DestroyRuntimeTexture(panelTex);
                DestroyRuntimeTexture(borderTex);
                DestroyRuntimeTexture(barBackTex);
                DestroyRuntimeTexture(healthBarTex);
                DestroyRuntimeTexture(staminaBarTex);
                DestroyRuntimeTexture(ammoBarTex);
                DestroyRuntimeTexture(warningTex);
                DestroyRuntimeTexture(reticleTex);
            }
        }

        private void SetupRuntimeFallbackIfNeeded()
        {
            useRuntimeFallbackHUD =
                healthBar == null && healthText == null && staminaBar == null &&
                ammoText == null && pointsText == null && timeText == null &&
                waveText == null && nightText == null && enemiesText == null;

            if (useRuntimeFallbackHUD)
            {
                BuildRuntimeStyle();
                BuildRuntimeTextures();
            }
        }

        private void BuildRuntimeStyle()
        {
            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 15;
            bodyStyle.normal.textColor = UiText;

            headerStyle = new GUIStyle(bodyStyle);
            headerStyle.fontSize = 22;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = UiAccent;

            sectionStyle = new GUIStyle(bodyStyle);
            sectionStyle.fontSize = 17;
            sectionStyle.fontStyle = FontStyle.Bold;
            sectionStyle.normal.textColor = UiAccent;

            rightStyle = new GUIStyle(bodyStyle);
            rightStyle.alignment = TextAnchor.UpperRight;

            accentStyle = new GUIStyle(bodyStyle);
            accentStyle.normal.textColor = new Color(0.58f, 0.92f, 1f, 1f);

            warningStyle = new GUIStyle(bodyStyle);
            warningStyle.fontSize = 24;
            warningStyle.fontStyle = FontStyle.Bold;
            warningStyle.alignment = TextAnchor.MiddleCenter;
            warningStyle.normal.textColor = Color.white;

            ammoBigStyle = new GUIStyle(bodyStyle);
            ammoBigStyle.fontSize = 46;
            ammoBigStyle.fontStyle = FontStyle.Bold;
            ammoBigStyle.normal.textColor = UiAmmo;

            smallStyle = new GUIStyle(bodyStyle);
            smallStyle.fontSize = 12;
            smallStyle.normal.textColor = new Color(0.72f, 0.87f, 0.75f, 1f);
        }

        private void BuildRuntimeTextures()
        {
            panelTex = CreateSolidTexture(UiPanel);
            borderTex = CreateSolidTexture(UiBorder);
            barBackTex = CreateSolidTexture(new Color(0.14f, 0.2f, 0.15f, 0.95f));
            healthBarTex = CreateSolidTexture(new Color(0.82f, 0.27f, 0.22f, 1f));
            staminaBarTex = CreateSolidTexture(new Color(0.3f, 0.72f, 0.34f, 1f));
            ammoBarTex = CreateSolidTexture(new Color(0.86f, 0.71f, 0.22f, 1f));
            warningTex = CreateSolidTexture(UiWarning);
            reticleTex = CreateReticleTexture(48, UiAccent);
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

            dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.OnTimeUpdate += UpdateTimeUI;
                dayNightCycle.OnDayStart += OnDayStart;
                dayNightCycle.OnNightStart += OnNightStart;
            }

            waveManager = FindObjectOfType<WaveManager>();
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

            if (dayNightCycle != null)
            {
                dayNightCycle.OnTimeUpdate -= UpdateTimeUI;
                dayNightCycle.OnDayStart -= OnDayStart;
                dayNightCycle.OnNightStart -= OnNightStart;
            }

            if (waveManager != null)
            {
                waveManager.OnWaveStarted -= UpdateWaveUI;
                waveManager.OnEnemyKilled -= UpdateEnemiesUI;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnNightChanged -= UpdateNightUI;
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
            cachedHealthCurrent = current;
            cachedHealthMax = max;

            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }

            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

            if (healthFill != null)
            {
                float percentage = current / Mathf.Max(1f, max);
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
            cachedStaminaCurrent = current;
            cachedStaminaMax = playerController != null ? playerController.MaxStamina : cachedStaminaMax;

            if (staminaBar != null && playerController != null)
            {
                staminaBar.maxValue = playerController.MaxStamina;
                staminaBar.value = current;
            }

            if (staminaFill != null)
            {
                float ratio = current / Mathf.Max(1f, cachedStaminaMax);
                staminaFill.color = Color.Lerp(new Color(0.65f, 0.25f, 0.2f), new Color(0.3f, 0.72f, 0.34f), ratio);
            }
        }

        private void UpdateAmmoUI(int current, int reserve)
        {
            cachedAmmoCurrent = current;
            cachedAmmoReserve = reserve;

            if (ammoText != null)
                ammoText.text = $"{current} / {reserve}";

            if (weaponNameText != null && playerShooting?.CurrentWeapon != null)
            {
                cachedWeaponName = playerShooting.CurrentWeapon.weaponName;
                weaponNameText.text = cachedWeaponName;
            }
        }

        private void ShowReloadIndicator()
        {
            cachedReloading = true;

            if (reloadIndicator != null)
                reloadIndicator.gameObject.SetActive(true);
        }

        private void HideReloadIndicator()
        {
            cachedReloading = false;

            if (reloadIndicator != null)
                reloadIndicator.gameObject.SetActive(false);
        }

        private void UpdatePointsUI(int points)
        {
            cachedPoints = points;

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
            cachedTimeRemaining = timeRemaining;

            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (timeBar != null && dayNightCycle != null)
            {
                timeBar.value = 1f - dayNightCycle.NormalizedTime;
            }
        }

        private void OnDayStart()
        {
            cachedPhase = "DAY";

            if (phaseText != null)
                phaseText.text = "DAY";

            if (timeBarFill != null)
                timeBarFill.color = dayColor;
        }

        private void OnNightStart()
        {
            cachedPhase = "NIGHT";

            if (phaseText != null)
                phaseText.text = "NIGHT";

            if (timeBarFill != null)
                timeBarFill.color = nightColor;
        }

        private void UpdateWaveUI(int wave)
        {
            cachedWave = wave;

            if (waveText != null)
                waveText.text = $"Wave {wave}";
        }

        private void UpdateNightUI(int night)
        {
            cachedNight = night;

            if (nightText != null)
                nightText.text = $"Night {night} / {GameManager.Instance?.MaxNights ?? 5}";
        }

        private void UpdateEnemiesUI(int killed)
        {
            if (waveManager == null)
            {
                waveManager = FindObjectOfType<WaveManager>();
            }

            if (waveManager != null)
            {
                cachedEnemies = waveManager.EnemiesRemaining;
            }

            if (enemiesText != null)
                enemiesText.text = $"Enemies: {cachedEnemies}";
        }

        private void OnGUI()
        {
            if (!useRuntimeFallbackHUD)
            {
                return;
            }

            if (bodyStyle == null || panelTex == null)
            {
                BuildRuntimeStyle();
                BuildRuntimeTextures();
            }

            float pad = 16f;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            Rect commandPanel = new Rect(pad, pad, screenWidth - (pad * 2f), 86f);
            DrawPanel(commandPanel);

            GUI.Label(new Rect(commandPanel.x + 14f, commandPanel.y + 8f, 600f, 24f), "DEADLIGHT TASK FORCE // RANGER-01", headerStyle);
            GUI.Label(new Rect(commandPanel.x + 14f, commandPanel.y + 34f, 580f, 20f),
                $"PHASE {cachedPhase}  |  NIGHT {cachedNight}/{(GameManager.Instance?.MaxNights ?? 5)}  |  TIMER {FormatTime(cachedTimeRemaining)}", bodyStyle);
            GUI.Label(new Rect(commandPanel.x + 14f, commandPanel.y + 56f, 580f, 18f), GetObjectiveText(), accentStyle);

            GUI.Label(new Rect(commandPanel.xMax - 280f, commandPanel.y + 8f, 260f, 20f), $"DIFFICULTY: {cachedDifficulty}", rightStyle);
            GUI.Label(new Rect(commandPanel.xMax - 280f, commandPanel.y + 30f, 260f, 20f), $"POINTS: {cachedPoints:N0}", rightStyle);
            GUI.Label(new Rect(commandPanel.xMax - 280f, commandPanel.y + 50f, 260f, 20f), $"KILLS: {cachedKills}", rightStyle);

            Rect vitalsPanel = new Rect(pad, screenHeight - 232f, 430f, 216f);
            DrawPanel(vitalsPanel);
            GUI.Label(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 10f, 240f, 22f), "VITALS", sectionStyle);

            float healthRatio = cachedHealthCurrent / Mathf.Max(1f, cachedHealthMax);
            float staminaRatio = cachedStaminaCurrent / Mathf.Max(1f, cachedStaminaMax);
            float threatRatio = Mathf.Clamp01(cachedEnemies / 30f);

            GUI.Label(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 40f, 130f, 20f), "HEALTH", bodyStyle);
            GUI.Label(new Rect(vitalsPanel.x + 300f, vitalsPanel.y + 40f, 110f, 20f),
                $"{Mathf.CeilToInt(cachedHealthCurrent)}/{Mathf.CeilToInt(cachedHealthMax)}", rightStyle);
            DrawSegmentBar(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 62f, vitalsPanel.width - 28f, 18f), healthRatio, healthBarTex, 18);

            GUI.Label(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 88f, 130f, 20f), "STAMINA", bodyStyle);
            GUI.Label(new Rect(vitalsPanel.x + 300f, vitalsPanel.y + 88f, 110f, 20f),
                $"{Mathf.CeilToInt(cachedStaminaCurrent)}/{Mathf.CeilToInt(cachedStaminaMax)}", rightStyle);
            DrawSegmentBar(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 110f, vitalsPanel.width - 28f, 18f), staminaRatio, staminaBarTex, 18);

            GUI.Label(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 136f, 180f, 20f), "THREAT LEVEL", bodyStyle);
            GUI.Label(new Rect(vitalsPanel.x + 285f, vitalsPanel.y + 136f, 125f, 20f), GetThreatLabel(threatRatio), rightStyle);
            DrawSegmentBar(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 158f, vitalsPanel.width - 28f, 16f), threatRatio, warningTex, 12);

            GUI.Label(new Rect(vitalsPanel.x + 14f, vitalsPanel.y + 180f, vitalsPanel.width - 20f, 16f),
                "TIP: Sweep zones in daytime and hold choke points at night.", smallStyle);

            Rect weaponPanel = new Rect(screenWidth - 456f - pad, screenHeight - 232f, 456f, 216f);
            DrawPanel(weaponPanel);
            GUI.Label(new Rect(weaponPanel.x + 14f, weaponPanel.y + 10f, 280f, 22f), "WEAPON SYSTEM", sectionStyle);
            GUI.Label(new Rect(weaponPanel.x + 14f, weaponPanel.y + 38f, 270f, 20f), cachedWeaponName.ToUpper(), bodyStyle);
            GUI.Label(new Rect(weaponPanel.x + 14f, weaponPanel.y + 62f, 320f, 52f),
                $"{cachedAmmoCurrent:00} / {cachedAmmoReserve:000}", ammoBigStyle);

            float ammoRatio = cachedAmmoCurrent / Mathf.Max(1f, cachedMagazineSize);
            DrawSegmentBar(new Rect(weaponPanel.x + 14f, weaponPanel.y + 122f, weaponPanel.width - 28f, 18f), ammoRatio, ammoBarTex, 14);

            string weaponStatus = cachedReloading ? "STATUS: RELOADING" : "STATUS: COMBAT READY";
            GUI.Label(new Rect(weaponPanel.x + 14f, weaponPanel.y + 146f, 260f, 20f), weaponStatus, accentStyle);
            GUI.Label(new Rect(weaponPanel.x + 14f, weaponPanel.y + 168f, weaponPanel.width - 28f, 16f),
                "LMB FIRE  |  R RELOAD  |  SHIFT SPRINT  |  ESC PAUSE", smallStyle);

            DrawCenterReticle(screenWidth, screenHeight);
            DrawWarnings(screenWidth);
            DrawControlDock(screenWidth, pad);
        }

        private void DrawPanel(Rect rect)
        {
            GUI.DrawTexture(rect, panelTex);

            float border = 2f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, border), borderTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - border, rect.width, border), borderTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, border, rect.height), borderTex);
            GUI.DrawTexture(new Rect(rect.xMax - border, rect.y, border, rect.height), borderTex);

            float corner = 12f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, corner, 2f), borderTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, corner), borderTex);
            GUI.DrawTexture(new Rect(rect.xMax - corner, rect.y, corner, 2f), borderTex);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.y, 2f, corner), borderTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, corner, 2f), borderTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - corner, 2f, corner), borderTex);
            GUI.DrawTexture(new Rect(rect.xMax - corner, rect.yMax - 2f, corner, 2f), borderTex);
            GUI.DrawTexture(new Rect(rect.xMax - 2f, rect.yMax - corner, 2f, corner), borderTex);
        }

        private void DrawSegmentBar(Rect rect, float ratio, Texture2D fillTex, int segments)
        {
            GUI.DrawTexture(rect, barBackTex);

            float clamped = Mathf.Clamp01(ratio);
            if (clamped > 0f)
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * clamped, rect.height), fillTex);
            }

            float segmentWidth = rect.width / Mathf.Max(1, segments);
            for (int i = 1; i < segments; i++)
            {
                float lineX = rect.x + segmentWidth * i;
                GUI.DrawTexture(new Rect(lineX, rect.y, 1f, rect.height), borderTex);
            }

            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), borderTex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), borderTex);
        }

        private void DrawCenterReticle(float screenWidth, float screenHeight)
        {
            if (reticleTex == null)
            {
                return;
            }

            float size = 38f;
            Rect reticleRect = new Rect((screenWidth - size) * 0.5f, (screenHeight - size) * 0.5f, size, size);
            GUI.DrawTexture(reticleRect, reticleTex);
        }

        private void DrawWarnings(float screenWidth)
        {
            float healthRatio = cachedHealthCurrent / Mathf.Max(1f, cachedHealthMax);
            bool lowHealth = healthRatio <= 0.3f;
            bool lowTime = cachedTimeRemaining <= 30f && cachedPhase == "DAY";
            bool lowAmmo = cachedAmmoCurrent <= Mathf.Max(2, Mathf.RoundToInt(cachedMagazineSize * 0.15f));

            if (!lowHealth && !lowTime && !lowAmmo)
            {
                return;
            }

            string warningText;
            if (lowHealth)
            {
                warningText = "CRITICAL: HEALTH LOW";
            }
            else if (lowTime)
            {
                warningText = "ALERT: DAY PHASE ENDING";
            }
            else
            {
                warningText = "ALERT: AMMO LOW";
            }

            float pulse = Mathf.Lerp(0.3f, 0.85f, Mathf.PingPong(Time.unscaledTime * 2.4f, 1f));
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, pulse);

            Rect warningRect = new Rect((screenWidth * 0.5f) - 220f, 114f, 440f, 34f);
            GUI.DrawTexture(warningRect, warningTex);
            GUI.color = Color.white;
            GUI.Label(warningRect, warningText, warningStyle);

            GUI.color = previous;
        }

        private void DrawControlDock(float screenWidth, float pad)
        {
            if (!showControls)
            {
                return;
            }

            Rect controlsRect = new Rect(screenWidth - 286f - pad, 110f, 286f, 130f);
            DrawPanel(controlsRect);

            GUI.Label(new Rect(controlsRect.x + 12f, controlsRect.y + 10f, 220f, 20f), "FIELD CONTROLS", sectionStyle);
            GUI.Label(new Rect(controlsRect.x + 12f, controlsRect.y + 34f, 250f, 16f), "WASD Move", bodyStyle);
            GUI.Label(new Rect(controlsRect.x + 12f, controlsRect.y + 52f, 250f, 16f), "LMB Fire", bodyStyle);
            GUI.Label(new Rect(controlsRect.x + 12f, controlsRect.y + 70f, 250f, 16f), "R Reload", bodyStyle);
            GUI.Label(new Rect(controlsRect.x + 12f, controlsRect.y + 88f, 250f, 16f), "SHIFT Sprint", bodyStyle);
            GUI.Label(new Rect(controlsRect.x + 12f, controlsRect.y + 106f, 250f, 16f), "F1 Hide Controls", smallStyle);
        }

        private string GetObjectiveText()
        {
            if (cachedPhase == "DAY")
            {
                return "OBJECTIVE: SCAVENGE SUPPLIES AND PREPARE DEFENSES";
            }

            if (cachedPhase == "NIGHT")
            {
                return "OBJECTIVE: HOLD POSITION UNTIL DAWN";
            }

            if (cachedPhase == "PAUSED")
            {
                return "OBJECTIVE: RESUME WHEN READY";
            }

            return "OBJECTIVE: SURVIVE FIVE NIGHTS";
        }

        private string GetThreatLabel(float threatRatio)
        {
            if (threatRatio < 0.2f) return "LOW";
            if (threatRatio < 0.45f) return "ELEVATED";
            if (threatRatio < 0.7f) return "HIGH";
            return "CRITICAL";
        }

        private static string FormatTime(float timeRemaining)
        {
            int minutes = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) / 60f);
            int seconds = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateReticleTexture(int size, Color color)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontSave
            };

            Color clear = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            int center = size / 2;
            int arm = Mathf.RoundToInt(size * 0.24f);

            for (int i = -arm; i <= arm; i++)
            {
                if (Mathf.Abs(i) > 4)
                {
                    texture.SetPixel(center + i, center, color);
                    texture.SetPixel(center, center + i, color);
                }
            }

            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    if (x * x + y * y <= 3)
                    {
                        texture.SetPixel(center + x, center + y, color);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private static void DestroyRuntimeTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }
        }
    }
}
