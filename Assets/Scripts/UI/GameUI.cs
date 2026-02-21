using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Systems;
using Deadlight.Data;
using Deadlight.Player;
using Deadlight.Narrative;
using System.Collections;
using System.Collections.Generic;

namespace Deadlight.UI
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        private Font _font;
        private Canvas _canvas;
        private GameObject _canvasRoot;

        private GameObject _mainMenuPanel;
        private GameObject _mapSelectPanel;
        private GameObject _pausePanel;
        private GameObject _dawnShopPanel;
        private GameObject _gameOverPanel;
        private GameObject _victoryPanel;
        private GameObject _leaderboardPanel;

        private Text _shopPointsText;
        private Text _shopTitleText;
        private Text _shopSummaryText;
        private List<Button> _shopBuyButtons = new List<Button>();

        private GameObject _weaponsTabContent;
        private GameObject _upgradesTabContent;
        private Button _weaponsTabBtn;
        private Button _upgradesTabBtn;

        private HashSet<WeaponType> _purchasedWeapons = new HashSet<WeaponType>();

        private List<Text> _upgradeLabels = new List<Text>();
        private List<Button> _upgradeBuyButtons = new List<Button>();

        private Difficulty _pendingDifficulty = Difficulty.Normal;
        private bool _waitingForEnding;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_font == null)
                _font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (_font == null)
            {
                string[] fallbacks = Font.GetOSInstalledFontNames();
                if (fallbacks != null && fallbacks.Length > 0)
                    _font = Font.CreateDynamicFontFromOSFont(fallbacks[0], 14);
            }
            if (_font == null)
            {
                Debug.LogError("[GameUI] Could not load any font. UI will not display correctly.");
            }

            EnsureEventSystem();
            BuildAllUI();
            HideAllPanels();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnPauseChanged += OnPauseChanged;
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                _mainMenuPanel?.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnPauseChanged -= OnPauseChanged;
            }
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void BuildAllUI()
        {
            _canvasRoot = new GameObject("GameUICanvas");
            _canvasRoot.transform.SetParent(transform);

            _canvas = _canvasRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            _canvasRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasRoot.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _canvasRoot.AddComponent<GraphicRaycaster>();

            BuildMainMenu();
            BuildMapSelect();
            BuildPauseMenu();
            BuildDawnShop();
            BuildGameOverScreen();
            BuildVictoryScreen();
            BuildLeaderboardPanel();
        }

        // ===================== MAIN MENU =====================

        private void BuildMainMenu()
        {
            _mainMenuPanel = CreatePanel(_canvasRoot.transform, "MainMenuPanel");
            var bg = _mainMenuPanel.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

            var title = CreateText(_mainMenuPanel.transform, "Title",
                "DEADLIGHT: Survival After Dark", 44, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(900, 80));
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            CreateText(_mainMenuPanel.transform, "Subtitle",
                "Select Difficulty", 24, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(400, 35));

            CreateButton(_mainMenuPanel.transform, "EasyButton", "EASY", new Color(0.2f, 0.7f, 0.2f),
                new Vector2(0.5f, 0.6f), new Vector2(260, 50), () => OnDifficultySelected(Difficulty.Easy));

            CreateButton(_mainMenuPanel.transform, "NormalButton", "NORMAL", new Color(0.85f, 0.85f, 0.2f),
                new Vector2(0.5f, 0.5f), new Vector2(260, 50), () => OnDifficultySelected(Difficulty.Normal));

            CreateButton(_mainMenuPanel.transform, "HardButton", "HARD", new Color(0.85f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.4f), new Vector2(260, 50), () => OnDifficultySelected(Difficulty.Hard));

            CreateButton(_mainMenuPanel.transform, "LeaderboardButton", "LEADERBOARD", new Color(0.3f, 0.4f, 0.7f),
                new Vector2(0.5f, 0.25f), new Vector2(260, 45), ShowLeaderboard);

            CreateButton(_mainMenuPanel.transform, "QuitButton", "QUIT", new Color(0.45f, 0.45f, 0.45f),
                new Vector2(0.5f, 0.14f), new Vector2(200, 42), QuitGame);
        }

        private void OnDifficultySelected(Difficulty difficulty)
        {
            _pendingDifficulty = difficulty;
            GameManager.Instance?.SetDifficulty(difficulty);
            _mainMenuPanel?.SetActive(false);
            _mapSelectPanel?.SetActive(true);
        }

        // ===================== MAP SELECT =====================

        private void BuildMapSelect()
        {
            _mapSelectPanel = CreatePanel(_canvasRoot.transform, "MapSelectPanel");
            _mapSelectPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

            CreateText(_mapSelectPanel.transform, "Title",
                "SELECT MAP", 40, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(500, 60));

            BuildMapOption(_mapSelectPanel.transform, "Town Center",
                "Streets, shops, and plazas.\nBalanced layout with moderate cover.",
                new Color(0.3f, 0.5f, 0.3f), new Vector2(0.5f, 0.68f), MapType.TownCenter);

            BuildMapOption(_mapSelectPanel.transform, "Industrial District",
                "Warehouses, narrow corridors.\nTight chokepoints, limited escape.",
                new Color(0.4f, 0.35f, 0.3f), new Vector2(0.5f, 0.48f), MapType.Industrial);

            BuildMapOption(_mapSelectPanel.transform, "Suburban Outskirts",
                "Houses, yards, wide spaces.\nRewards mobility, less cover.",
                new Color(0.3f, 0.4f, 0.25f), new Vector2(0.5f, 0.28f), MapType.Suburban);

            CreateButton(_mapSelectPanel.transform, "BackButton", "BACK", new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.1f), new Vector2(180, 42), () =>
                {
                    _mapSelectPanel?.SetActive(false);
                    _mainMenuPanel?.SetActive(true);
                });
        }

        private void BuildMapOption(Transform parent, string mapName, string desc, Color color, Vector2 anchor, MapType mapType)
        {
            var container = new GameObject($"MapOption_{mapName}");
            container.transform.SetParent(parent);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = anchor;
            containerRect.anchorMax = anchor;
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(600, 100);

            var containerImg = container.AddComponent<Image>();
            containerImg.color = new Color(color.r, color.g, color.b, 0.6f);

            var btn = container.AddComponent<Button>();
            btn.targetGraphic = containerImg;
            var colors = btn.colors;
            colors.highlightedColor = new Color(color.r + 0.15f, color.g + 0.15f, color.b + 0.15f, 0.8f);
            colors.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 0.9f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnMapSelected(mapType));

            CreateText(container.transform, "Name", mapName, 26, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 10), new Vector2(300, 35));

            CreateText(container.transform, "Desc", desc, 16, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, -18), new Vector2(560, 50));
        }

        private void OnMapSelected(MapType mapType)
        {
            GameManager.Instance?.SetMap(mapType);
            _mapSelectPanel?.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.StartNewGame();
        }

        // ===================== PAUSE MENU =====================

        private void BuildPauseMenu()
        {
            _pausePanel = CreatePanel(_canvasRoot.transform, "PausePanel");
            _pausePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);

            CreateText(_pausePanel.transform, "Title",
                "PAUSED", 48, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(400, 70));

            CreateButton(_pausePanel.transform, "ResumeButton", "RESUME", new Color(0.2f, 0.65f, 0.3f),
                new Vector2(0.5f, 0.55f), new Vector2(260, 50), OnResume);

            CreateButton(_pausePanel.transform, "PauseRestartButton", "RESTART", new Color(0.7f, 0.6f, 0.2f),
                new Vector2(0.5f, 0.42f), new Vector2(260, 50), RestartGame);

            CreateButton(_pausePanel.transform, "PauseMainMenuButton", "MAIN MENU", new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.29f), new Vector2(260, 50), GoToMainMenu);

            CreateButton(_pausePanel.transform, "PauseQuitButton", "QUIT GAME", new Color(0.65f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.16f), new Vector2(260, 50), QuitGame);
        }

        private void OnResume()
        {
            GameManager.Instance?.SetPaused(false);
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused && GameManager.Instance != null &&
                (GameManager.Instance.CurrentState == GameState.DayPhase ||
                 GameManager.Instance.CurrentState == GameState.NightPhase))
            {
                _pausePanel?.SetActive(true);
            }
            else
            {
                _pausePanel?.SetActive(false);
            }
        }

        private void StartGame(Difficulty difficulty)
        {
            GameManager.Instance?.SetDifficulty(difficulty);
            _mainMenuPanel?.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.StartNewGame();
        }

        private void QuitGame()
        {
            GameManager.Instance?.QuitGame();
        }

        private void BuildDawnShop()
        {
            _dawnShopPanel = CreatePanel(_canvasRoot.transform, "DawnShopPanel");
            _dawnShopPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            _shopTitleText = CreateText(_dawnShopPanel.transform, "ShopTitle",
                "DAWN - Night 1 Survived!", 34, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.4f),
                new Vector2(0.5f, 0.96f), new Vector2(0.5f, 0.96f), Vector2.zero, new Vector2(700, 45)).GetComponent<Text>();
            _shopTitleText.fontStyle = FontStyle.Bold;

            _shopSummaryText = CreateText(_dawnShopPanel.transform, "ShopSummary",
                "", 16, TextAnchor.UpperCenter, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.5f, 0.915f), new Vector2(0.5f, 0.915f), Vector2.zero, new Vector2(700, 22)).GetComponent<Text>();

            _shopPointsText = CreateText(_dawnShopPanel.transform, "ShopPoints",
                "Points: 0", 26, TextAnchor.UpperCenter, new Color(0.4f, 1f, 0.4f),
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(300, 35)).GetComponent<Text>();
            _shopPointsText.fontStyle = FontStyle.Bold;

            // Supplies row
            var suppliesRow = new GameObject("SuppliesRow");
            suppliesRow.transform.SetParent(_dawnShopPanel.transform);
            var srRect = suppliesRow.AddComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0.5f, 0.82f);
            srRect.anchorMax = new Vector2(0.5f, 0.82f);
            srRect.pivot = new Vector2(0.5f, 0.5f);
            srRect.anchoredPosition = Vector2.zero;
            srRect.sizeDelta = new Vector2(600, 45);

            var healBtn = CreateButton(suppliesRow.transform, "HealBtn", "Health Kit (50)", new Color(0.6f, 0.2f, 0.2f),
                new Vector2(0.25f, 0.5f), new Vector2(220, 40), BuyHealthKit);
            _shopBuyButtons.Add(healBtn.GetComponent<Button>());

            var ammoBtn = CreateButton(suppliesRow.transform, "AmmoBtn", "Ammo Refill (30)", new Color(0.7f, 0.6f, 0.15f),
                new Vector2(0.75f, 0.5f), new Vector2(220, 40), BuyAmmoRefill);
            _shopBuyButtons.Add(ammoBtn.GetComponent<Button>());

            // Armor row
            var armorRow = new GameObject("ArmorRow");
            armorRow.transform.SetParent(_dawnShopPanel.transform);
            var arRect = armorRow.AddComponent<RectTransform>();
            arRect.anchorMin = new Vector2(0.5f, 0.77f);
            arRect.anchorMax = new Vector2(0.5f, 0.77f);
            arRect.pivot = new Vector2(0.5f, 0.5f);
            arRect.anchoredPosition = Vector2.zero;
            arRect.sizeDelta = new Vector2(600, 45);

            var vest1Btn = CreateButton(armorRow.transform, "Vest1Btn", "Vest Lv1 (80)", new Color(0.2f, 0.4f, 0.7f),
                new Vector2(0.15f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level1, false, 80));
            _shopBuyButtons.Add(vest1Btn.GetComponent<Button>());

            var vest2Btn = CreateButton(armorRow.transform, "Vest2Btn", "Vest Lv2 (180)", new Color(0.2f, 0.4f, 0.7f),
                new Vector2(0.38f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level2, false, 180));
            _shopBuyButtons.Add(vest2Btn.GetComponent<Button>());

            var helm1Btn = CreateButton(armorRow.transform, "Helm1Btn", "Helm Lv1 (60)", new Color(0.5f, 0.5f, 0.6f),
                new Vector2(0.62f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level1, true, 60));
            _shopBuyButtons.Add(helm1Btn.GetComponent<Button>());

            var helm2Btn = CreateButton(armorRow.transform, "Helm2Btn", "Helm Lv2 (140)", new Color(0.5f, 0.5f, 0.6f),
                new Vector2(0.85f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level2, true, 140));
            _shopBuyButtons.Add(helm2Btn.GetComponent<Button>());

            // Tab buttons
            _weaponsTabBtn = CreateButton(_dawnShopPanel.transform, "WeaponsTab", "WEAPONS",
                new Color(0.3f, 0.45f, 0.6f),
                new Vector2(0.35f, 0.755f), new Vector2(200, 38), () => ShowShopTab(true)).GetComponent<Button>();
            _upgradesTabBtn = CreateButton(_dawnShopPanel.transform, "UpgradesTab", "UPGRADES",
                new Color(0.45f, 0.35f, 0.55f),
                new Vector2(0.65f, 0.755f), new Vector2(200, 38), () => ShowShopTab(false)).GetComponent<Button>();

            // Weapons content
            _weaponsTabContent = new GameObject("WeaponsContent");
            _weaponsTabContent.transform.SetParent(_dawnShopPanel.transform);
            var wcRect = _weaponsTabContent.AddComponent<RectTransform>();
            wcRect.anchorMin = new Vector2(0.5f, 0.18f);
            wcRect.anchorMax = new Vector2(0.5f, 0.72f);
            wcRect.pivot = new Vector2(0.5f, 0.5f);
            wcRect.anchoredPosition = Vector2.zero;
            wcRect.sizeDelta = new Vector2(650, 0);

            BuildWeaponItems();

            // Upgrades content
            _upgradesTabContent = new GameObject("UpgradesContent");
            _upgradesTabContent.transform.SetParent(_dawnShopPanel.transform);
            var ucRect = _upgradesTabContent.AddComponent<RectTransform>();
            ucRect.anchorMin = new Vector2(0.5f, 0.18f);
            ucRect.anchorMax = new Vector2(0.5f, 0.72f);
            ucRect.pivot = new Vector2(0.5f, 0.5f);
            ucRect.anchoredPosition = Vector2.zero;
            ucRect.sizeDelta = new Vector2(650, 0);

            BuildUpgradeItems();
            _upgradesTabContent.SetActive(false);

            CreateButton(_dawnShopPanel.transform, "ContinueButton", "Continue to Next Night",
                new Color(0.15f, 0.55f, 0.85f),
                new Vector2(0.5f, 0.08f), new Vector2(350, 55), OnContinueToNextNight);
        }

        private void BuildWeaponItems()
        {
            float y = 0;
            int h = 72;
            AddWeaponShopItem(_weaponsTabContent.transform, "Shotgun", "Close-range, 8 pellets", 100, 1,
                WeaponType.Shotgun, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "SMG", "Auto, fast fire rate", 150, 2,
                WeaponType.SMG, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Sniper Rifle", "High damage, long range", 250, 2,
                WeaponType.SniperRifle, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Assault Rifle", "Auto, balanced stats", 200, 3,
                WeaponType.AssaultRifle, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Grenade Launcher", "Explosive, area damage", 350, 4,
                WeaponType.GrenadeLauncher, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Flamethrower", "Burn damage over time", 400, 4,
                WeaponType.Flamethrower, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Railgun", "Piercing charged shot", 500, 5,
                WeaponType.Railgun, ref y, h);
        }

        private void AddWeaponShopItem(Transform parent, string name, string desc, int cost, int unlockNight,
            WeaponType weaponType, ref float y, int height)
        {
            var root = new GameObject($"Weapon_{name}");
            root.transform.SetParent(parent);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(620, height - 4);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.7f);

            CreateText(root.transform, "Name", name, 22, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, 8), new Vector2(300, 30));
            CreateText(root.transform, "Desc", $"{desc}  |  {cost} pts  |  Night {unlockNight}+", 15,
                TextAnchor.MiddleLeft, new Color(0.65f, 0.65f, 0.65f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, -12), new Vector2(400, 22));

            var wt = weaponType;
            var buyBtn = CreateButton(root.transform, "Buy", "BUY", new Color(0.25f, 0.55f, 0.25f),
                new Vector2(1, 0.5f), new Vector2(90, 38), () => BuyWeapon(wt, cost, unlockNight));
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-10, 0);

            _shopBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void BuildUpgradeItems()
        {
            float y = 0;
            int h = 65;
            AddUpgradeItem(_upgradesTabContent.transform, "Damage", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeDamage()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Fire Rate", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeFireRate()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Magazine", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeMagazine()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Max Health", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeHealth()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Sprint Speed", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeSprint()) RefreshShop();
            });
        }

        private void AddUpgradeItem(Transform parent, string name, ref float y, int height, System.Action onBuy)
        {
            var root = new GameObject($"Upgrade_{name}");
            root.transform.SetParent(parent);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(620, height - 4);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.13f, 0.22f, 0.7f);

            var label = CreateText(root.transform, "Label", name, 20, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, 0), new Vector2(420, height)).GetComponent<Text>();
            _upgradeLabels.Add(label);

            var buyBtn = CreateButton(root.transform, "Buy", "UPGRADE", new Color(0.45f, 0.3f, 0.6f),
                new Vector2(1, 0.5f), new Vector2(110, 36), onBuy);
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-10, 0);

            _upgradeBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void ShowShopTab(bool showWeapons)
        {
            if (_weaponsTabContent != null) _weaponsTabContent.SetActive(showWeapons);
            if (_upgradesTabContent != null) _upgradesTabContent.SetActive(!showWeapons);

            if (_weaponsTabBtn != null)
            {
                var c = _weaponsTabBtn.colors;
                c.normalColor = showWeapons ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.2f, 0.2f, 0.3f);
                _weaponsTabBtn.colors = c;
            }
            if (_upgradesTabBtn != null)
            {
                var c = _upgradesTabBtn.colors;
                c.normalColor = !showWeapons ? new Color(0.5f, 0.35f, 0.65f) : new Color(0.2f, 0.2f, 0.3f);
                _upgradesTabBtn.colors = c;
            }
        }

        private void BuyWeapon(WeaponType weaponType, int cost, int unlockNight)
        {
            if (_purchasedWeapons.Contains(weaponType)) return;
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(cost)) return;
            int night = GameManager.Instance?.CurrentNight ?? 1;
            if (night < unlockNight) return;
            if (!PointsSystem.Instance.SpendPoints(cost, $"Weapon: {weaponType}")) return;

            _purchasedWeapons.Add(weaponType);

            WeaponData weapon = weaponType switch
            {
                WeaponType.Shotgun => WeaponData.CreateShotgun(),
                WeaponType.SMG => WeaponData.CreateSMG(),
                WeaponType.AssaultRifle => WeaponData.CreateAssaultRifle(),
                WeaponType.GrenadeLauncher => WeaponData.CreateGrenadeLauncher(),
                WeaponType.SniperRifle => WeaponData.CreateSniperRifle(),
                WeaponType.Flamethrower => WeaponData.CreateFlamethrower(),
                WeaponType.Railgun => WeaponData.CreateRailgun(),
                _ => null
            };

            if (weapon != null)
            {
                var player = GameObject.Find("Player");
                var shooting = player?.GetComponent<PlayerShooting>();
                shooting?.SetSecondWeapon(weapon);
            }

            RefreshShop();
        }

        private void BuyHealthKit()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(50)) return;
            if (!PointsSystem.Instance.SpendPoints(50, "Health Kit")) return;
            var player = GameObject.Find("Player");
            player?.GetComponent<PlayerHealth>()?.FullHeal();
            RefreshShop();
        }

        private void BuyAmmoRefill()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(30)) return;
            if (!PointsSystem.Instance.SpendPoints(30, "Ammo Refill")) return;
            var player = GameObject.Find("Player");
            player?.GetComponent<PlayerShooting>()?.AddAmmo(60);
            RefreshShop();
        }

        private void BuyArmor(ArmorTier tier, bool isHelmet, int cost)
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(cost)) return;
            if (PlayerArmor.Instance == null) return;

            bool worthBuying = isHelmet
                ? (tier > PlayerArmor.Instance.HelmetTier || PlayerArmor.Instance.HelmetDurability <= 0)
                : (tier > PlayerArmor.Instance.VestTier || PlayerArmor.Instance.VestDurability <= 0);
            if (!worthBuying) return;

            if (!PointsSystem.Instance.SpendPoints(cost, isHelmet ? $"Lv{(int)tier} Helmet" : $"Lv{(int)tier} Vest")) return;

            if (isHelmet)
                PlayerArmor.Instance.EquipHelmet(tier);
            else
                PlayerArmor.Instance.EquipVest(tier);

            RefreshShop();
        }

        private void RefreshShop()
        {
            UpdateShopDisplay();
        }

        private void UpdateShopDisplay()
        {
            if (_shopPointsText != null && PointsSystem.Instance != null)
                _shopPointsText.text = $"Points: {PointsSystem.Instance.CurrentPoints}";

            if (_shopTitleText != null && GameManager.Instance != null)
                _shopTitleText.text = $"DAWN - Night {GameManager.Instance.CurrentNight} Survived!";

            if (_shopSummaryText != null && PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                _shopSummaryText.text = $"Kills: {stats.enemiesKilled}  |  Points Earned: {stats.totalEarned}";
            }

            int night = GameManager.Instance?.CurrentNight ?? 1;

            // Supply buttons (indices 0,1)
            UpdateSupplyButton(0, 50);
            UpdateSupplyButton(1, 30);

            // Weapon buttons (indices 2..8)
            WeaponType[] weaponTypes = { WeaponType.Shotgun, WeaponType.SMG, WeaponType.SniperRifle, WeaponType.AssaultRifle, WeaponType.GrenadeLauncher, WeaponType.Flamethrower, WeaponType.Railgun };
            int[] weaponCosts = { 100, 150, 250, 200, 350, 400, 500 };
            int[] weaponNights = { 1, 2, 2, 3, 4, 4, 5 };
            for (int i = 0; i < weaponTypes.Length; i++)
            {
                int btnIdx = 2 + i;
                if (btnIdx >= _shopBuyButtons.Count) break;
                var btn = _shopBuyButtons[btnIdx];
                bool sold = _purchasedWeapons.Contains(weaponTypes[i]);
                bool canAfford = PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(weaponCosts[i]);
                bool unlocked = night >= weaponNights[i];
                btn.interactable = !sold && canAfford && unlocked;

                var labelText = btn.GetComponentInChildren<Text>();
                if (labelText != null)
                    labelText.text = sold ? "SOLD" : "BUY";
            }

            // Upgrade buttons and labels
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades != null)
            {
                UpdateUpgradeRow(0, upgrades.DamageTier, PlayerUpgrades.MaxDamageTier,
                    upgrades.GetDamageCost(), upgrades.GetDamageDescription(), "Damage");
                UpdateUpgradeRow(1, upgrades.FireRateTier, PlayerUpgrades.MaxFireRateTier,
                    upgrades.GetFireRateCost(), upgrades.GetFireRateDescription(), "Fire Rate");
                UpdateUpgradeRow(2, upgrades.MagazineTier, PlayerUpgrades.MaxMagazineTier,
                    upgrades.GetMagazineCost(), upgrades.GetMagazineDescription(), "Magazine");
                UpdateUpgradeRow(3, upgrades.HealthTier, PlayerUpgrades.MaxHealthTier,
                    upgrades.GetHealthCost(), upgrades.GetHealthDescription(), "Max Health");
                UpdateUpgradeRow(4, upgrades.SprintTier, PlayerUpgrades.MaxSprintTier,
                    upgrades.GetSprintCost(), upgrades.GetSprintDescription(), "Sprint Speed");
            }
        }

        private void UpdateSupplyButton(int index, int cost)
        {
            if (index >= _shopBuyButtons.Count) return;
            _shopBuyButtons[index].interactable = PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);
        }

        private void UpdateUpgradeRow(int index, int currentTier, int maxTier, int cost, string desc, string name)
        {
            if (index >= _upgradeLabels.Count || index >= _upgradeBuyButtons.Count) return;

            string pips = "";
            for (int i = 0; i < maxTier; i++)
                pips += i < currentTier ? "[X]" : "[ ]";

            bool maxed = currentTier >= maxTier;
            string costStr = maxed ? "" : $"  ({cost} pts)";
            _upgradeLabels[index].text = $"{name}  {pips}  {desc}{costStr}";

            _upgradeBuyButtons[index].interactable = !maxed && cost > 0 &&
                PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);

            var labelText = _upgradeBuyButtons[index].GetComponentInChildren<Text>();
            if (labelText != null) labelText.text = maxed ? "MAX" : "UPGRADE";
        }

        private void OnContinueToNextNight()
        {
            _dawnShopPanel?.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.AdvanceToNextNight();
        }

        private void BuildGameOverScreen()
        {
            _gameOverPanel = CreatePanel(_canvasRoot.transform, "GameOverPanel");
            var bg = _gameOverPanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            var title = CreateText(_gameOverPanel.transform, "Title",
                "YOU DIED", 56, TextAnchor.MiddleCenter, new Color(0.9f, 0.15f, 0.15f),
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(500, 80));
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var statsText = CreateText(_gameOverPanel.transform, "Stats", "", 22, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(500, 200));
            statsText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            CreateButton(_gameOverPanel.transform, "RestartButton", "Restart", new Color(0.3f, 0.6f, 0.3f),
                new Vector2(0.4f, 0.2f), new Vector2(180, 50), RestartGame);

            CreateButton(_gameOverPanel.transform, "MainMenuButton", "Main Menu", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.6f, 0.2f), new Vector2(180, 50), GoToMainMenu);

            _gameOverPanel.AddComponent<GameOverPanelHelper>().Initialize(statsText.GetComponent<Text>());
        }

        private void BuildVictoryScreen()
        {
            _victoryPanel = CreatePanel(_canvasRoot.transform, "VictoryPanel");
            var bg = _victoryPanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            var title = CreateText(_victoryPanel.transform, "Title",
                "YOU SURVIVED!", 56, TextAnchor.MiddleCenter, new Color(0.9f, 0.75f, 0.2f),
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(500, 80));
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var statsText = CreateText(_victoryPanel.transform, "Stats", "", 22, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(500, 200));
            statsText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            CreateButton(_victoryPanel.transform, "RestartButton", "Restart", new Color(0.3f, 0.6f, 0.3f),
                new Vector2(0.4f, 0.2f), new Vector2(180, 50), RestartGame);

            CreateButton(_victoryPanel.transform, "MainMenuButton", "Main Menu", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.6f, 0.2f), new Vector2(180, 50), GoToMainMenu);

            _victoryPanel.AddComponent<VictoryPanelHelper>().Initialize(statsText.GetComponent<Text>());
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            HideAllPanels();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.RestartGame();
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            HideAllPanels();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.ReturnToMainMenu();
        }

        private void OnGameStateChanged(GameState newState)
        {
            HideAllPanels();

            switch (newState)
            {
                case GameState.MainMenu:
                    _mainMenuPanel?.SetActive(true);
                    break;
                case GameState.DayPhase:
                case GameState.NightPhase:
                case GameState.Transition:
                    break;
                case GameState.DawnPhase:
                    _dawnShopPanel?.SetActive(true);
                    Time.timeScale = 0f;
                    UpdateShopDisplay();
                    break;
                case GameState.GameOver:
                    LeaderboardManager.Instance?.SubmitRun(false);
                    if (EndingSequence.Instance != null && !EndingSequence.Instance.IsPlaying)
                    {
                        _waitingForEnding = true;
                        EndingSequence.Instance.OnEndingComplete += OnEndingSequenceComplete;
                    }
                    else if (EndingSequence.Instance != null && EndingSequence.Instance.IsPlaying)
                    {
                        _waitingForEnding = true;
                        EndingSequence.Instance.OnEndingComplete += OnEndingSequenceComplete;
                    }
                    else
                    {
                        _gameOverPanel?.SetActive(true);
                        Time.timeScale = 0f;
                    }
                    break;
                case GameState.Victory:
                    LeaderboardManager.Instance?.SubmitRun(true);
                    if (EndingSequence.Instance != null)
                    {
                        _waitingForEnding = true;
                        EndingSequence.Instance.OnEndingComplete += OnEndingSequenceComplete;
                    }
                    else
                    {
                        _victoryPanel?.SetActive(true);
                        Time.timeScale = 0f;
                    }
                    break;
            }
        }

        private void OnEndingSequenceComplete()
        {
            if (EndingSequence.Instance != null)
                EndingSequence.Instance.OnEndingComplete -= OnEndingSequenceComplete;

            _waitingForEnding = false;

            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.Victory)
            {
                _victoryPanel?.SetActive(true);
            }
            else if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                _gameOverPanel?.SetActive(true);
            }
        }

        private void HideAllPanels()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_mapSelectPanel != null) _mapSelectPanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_dawnShopPanel != null) _dawnShopPanel.SetActive(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_victoryPanel != null) _victoryPanel.SetActive(false);
            if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
        }

        // ===================== LEADERBOARD =====================

        private void BuildLeaderboardPanel()
        {
            _leaderboardPanel = CreatePanel(_canvasRoot.transform, "LeaderboardPanel");
            _leaderboardPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            CreateText(_leaderboardPanel.transform, "Title",
                "LEADERBOARD", 40, TextAnchor.MiddleCenter, new Color(0.9f, 0.8f, 0.3f),
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(500, 55));

            var headerText = "RANK    SCORE    NIGHTS    KILLS    DIFFICULTY    MAP";
            CreateText(_leaderboardPanel.transform, "Header", headerText, 16, TextAnchor.MiddleCenter,
                new Color(0.6f, 0.6f, 0.6f),
                new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(800, 25));

            for (int i = 0; i < 10; i++)
            {
                float yPos = 0.78f - i * 0.065f;
                CreateText(_leaderboardPanel.transform, $"Entry_{i}", "", 17, TextAnchor.MiddleCenter, Color.white,
                    new Vector2(0.5f, yPos), new Vector2(0.5f, yPos), Vector2.zero, new Vector2(800, 28));
            }

            CreateButton(_leaderboardPanel.transform, "LBBackButton", "BACK", new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.06f), new Vector2(200, 45), HideLeaderboard);
        }

        private void ShowLeaderboard()
        {
            HideAllPanels();
            _leaderboardPanel?.SetActive(true);
            RefreshLeaderboardDisplay();
        }

        private void HideLeaderboard()
        {
            _leaderboardPanel?.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
            {
                _mainMenuPanel?.SetActive(true);
            }
        }

        private void RefreshLeaderboardDisplay()
        {
            if (_leaderboardPanel == null) return;

            var entries = LeaderboardManager.Instance?.Entries;

            for (int i = 0; i < 10; i++)
            {
                var entryText = _leaderboardPanel.transform.Find($"Entry_{i}")?.GetComponent<Text>();
                if (entryText == null) continue;

                if (entries != null && i < entries.Count)
                {
                    var e = entries[i];
                    string victoryMark = e.victory ? " *" : "";
                    entryText.text = $"#{i + 1}      {e.score}      {e.nightsReached}      {e.kills}      {e.difficulty}      {e.map}{victoryMark}";
                    entryText.color = e.victory ? new Color(0.9f, 0.8f, 0.3f) : Color.white;
                }
                else
                {
                    entryText.text = $"#{i + 1}      ---";
                    entryText.color = new Color(0.4f, 0.4f, 0.4f);
                }
            }
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.85f);
            return obj;
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;

            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(1, -1);

            return obj;
        }

        private GameObject CreateButton(Transform parent, string name, string label, Color color,
            Vector2 anchor, Vector2 size, System.Action onClick)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            img.color = color;

            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(Mathf.Min(1, color.r + 0.2f), Mathf.Min(1, color.g + 0.2f), Mathf.Min(1, color.b + 0.2f));
            colors.pressedColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            var labelObj = CreateText(obj.transform, "Label", label, 20, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            labelObj.GetComponent<Text>().fontStyle = FontStyle.Bold;

            return obj;
        }
    }

    internal class GameOverPanelHelper : MonoBehaviour
    {
        private Text _statsText;

        public void Initialize(Text statsText)
        {
            _statsText = statsText;
        }

        private void OnEnable()
        {
            if (_statsText == null) return;

            int nightReached = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentNight : 1;
            string difficulty = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentDifficulty.ToString() : "Normal";
            string map = Core.GameManager.Instance != null ? Core.GameManager.Instance.SelectedMap.ToString() : "TownCenter";
            int kills = 0;
            int totalEarned = 0;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                totalEarned = stats.totalEarned;
            }

            int rank = -1;
            int finalScore = 0;
            if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.Entries.Count > 0)
            {
                finalScore = LeaderboardManager.Instance.Entries[0].score;
                rank = 1;
                foreach (var entry in LeaderboardManager.Instance.Entries)
                {
                    if (entry.nightsReached == nightReached && entry.kills == kills)
                    {
                        finalScore = entry.score;
                        rank = LeaderboardManager.Instance.GetRank(entry.score);
                        break;
                    }
                }
            }

            _statsText.text = $"Night Reached: {nightReached}\n" +
                $"Enemies Killed: {kills}\n" +
                $"Points Earned: {totalEarned}\n" +
                $"Difficulty: {difficulty}\n" +
                $"Map: {map}\n" +
                (rank > 0 ? $"Leaderboard Rank: #{rank}  (Score: {finalScore})" : "");
        }
    }

    internal class VictoryPanelHelper : MonoBehaviour
    {
        private Text _statsText;

        public void Initialize(Text statsText)
        {
            _statsText = statsText;
        }

        private void OnEnable()
        {
            if (_statsText == null) return;

            string difficulty = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentDifficulty.ToString() : "Normal";
            string map = Core.GameManager.Instance != null ? Core.GameManager.Instance.SelectedMap.ToString() : "TownCenter";
            int kills = 0;
            int totalEarned = 0;
            int nightsSurvived = 5;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                totalEarned = stats.totalEarned;
                nightsSurvived = stats.nightsSurvived;
            }

            int rank = -1;
            int finalScore = 0;
            if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.Entries.Count > 0)
            {
                var latestEntry = LeaderboardManager.Instance.Entries[0];
                for (int i = 0; i < LeaderboardManager.Instance.Entries.Count; i++)
                {
                    if (LeaderboardManager.Instance.Entries[i].victory)
                    {
                        latestEntry = LeaderboardManager.Instance.Entries[i];
                        break;
                    }
                }
                finalScore = latestEntry.score;
                rank = LeaderboardManager.Instance.GetRank(finalScore);
            }

            _statsText.text = $"ALL {nightsSurvived} NIGHTS SURVIVED!\n" +
                $"Enemies Killed: {kills}\n" +
                $"Points Earned: {totalEarned}\n" +
                $"Difficulty: {difficulty} ({GetMultiplierText(difficulty)})\n" +
                $"Map: {map}\n" +
                (rank > 0 ? $"Leaderboard Rank: #{rank}  (Score: {finalScore})" : "");
        }

        private string GetMultiplierText(string difficulty)
        {
            return difficulty switch
            {
                "Easy" => "0.75x",
                "Normal" => "1.0x",
                "Hard" => "1.5x",
                _ => "1.0x"
            };
        }
    }
}
