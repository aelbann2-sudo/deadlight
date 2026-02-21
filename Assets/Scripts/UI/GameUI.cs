using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Systems;
using Deadlight.Data;
using Deadlight.Player;
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
        private GameObject _dawnShopPanel;
        private GameObject _gameOverPanel;
        private GameObject _victoryPanel;

        private Text _shopPointsText;
        private Text _shopTitleText;
        private List<Button> _shopBuyButtons = new List<Button>();

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
            {
                Debug.LogError("[GameUI] Could not load LegacyRuntime.ttf font.");
                return;
            }

            EnsureEventSystem();
            BuildAllUI();
            HideAllPanels();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
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
            BuildDawnShop();
            BuildGameOverScreen();
            BuildVictoryScreen();
        }

        private void BuildMainMenu()
        {
            _mainMenuPanel = CreatePanel(_canvasRoot.transform, "MainMenuPanel");
            var bg = _mainMenuPanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            var title = CreateText(_mainMenuPanel.transform, "Title",
                "DEADLIGHT: Survival After Dark", 42, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(900, 80));
            title.fontStyle = FontStyle.Bold;

            CreateButton(_mainMenuPanel.transform, "EasyButton", "Easy", new Color(0.2f, 0.8f, 0.2f),
                new Vector2(0.5f, 0.55f), new Vector2(250, 50), () => StartGame(Difficulty.Easy));

            CreateButton(_mainMenuPanel.transform, "NormalButton", "Normal", new Color(0.9f, 0.9f, 0.2f),
                new Vector2(0.5f, 0.45f), new Vector2(250, 50), () => StartGame(Difficulty.Normal));

            CreateButton(_mainMenuPanel.transform, "HardButton", "Hard", new Color(0.9f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.35f), new Vector2(250, 50), () => StartGame(Difficulty.Hard));

            CreateButton(_mainMenuPanel.transform, "QuitButton", "Quit", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.5f, 0.15f), new Vector2(200, 45), QuitGame);
        }

        private void StartGame(Difficulty difficulty)
        {
            GameManager.Instance?.SetDifficulty(difficulty);
            GameManager.Instance?.LoadGameScene();
            GameManager.Instance?.StartNewGame();
        }

        private void QuitGame()
        {
            GameManager.Instance?.QuitGame();
        }

        private void BuildDawnShop()
        {
            _dawnShopPanel = CreatePanel(_canvasRoot.transform, "DawnShopPanel");
            var bg = _dawnShopPanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            _shopTitleText = CreateText(_dawnShopPanel.transform, "Title",
                "DAWN - Night 1 Survived!", 36, TextAnchor.UpperCenter, new Color(0.9f, 0.8f, 0.5f),
                new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), Vector2.zero, new Vector2(600, 50)).GetComponent<Text>();
            _shopTitleText.fontStyle = FontStyle.Bold;

            _shopPointsText = CreateText(_dawnShopPanel.transform, "PointsText",
                "Points: 0", 24, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(300, 35)).GetComponent<Text>();

            var scrollContent = new GameObject("ShopItemsContent");
            scrollContent.transform.SetParent(_dawnShopPanel.transform);
            var contentRect = scrollContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.2f);
            contentRect.anchorMax = new Vector2(0.5f, 0.75f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(500, 400);

            float y = 0;
            int itemHeight = 70;

            AddShopItem(contentRect, "Health Kit", 50, ref y, itemHeight, () => BuyHealthKit());
            AddShopItem(contentRect, "Ammo Refill", 30, ref y, itemHeight, () => BuyAmmoRefill());
            AddShopItem(contentRect, "Shotgun", 100, 1, ref y, itemHeight, () => BuyShotgun());
            AddShopItem(contentRect, "Assault Rifle", 200, 2, ref y, itemHeight, () => BuyAssaultRifle());

            CreateButton(_dawnShopPanel.transform, "ContinueButton", "Continue to Next Night",
                new Color(0.2f, 0.6f, 0.9f),
                new Vector2(0.5f, 0.08f), new Vector2(350, 55), OnContinueToNextNight);
        }

        private void AddShopItem(Transform parent, string name, int cost, ref float y, int height, System.Action onBuy)
        {
            AddShopItem(parent, name, cost, 0, ref y, height, onBuy);
        }

        private void AddShopItem(Transform parent, string name, int cost, int unlockNight, ref float y, int height, System.Action onBuy)
        {
            var itemRoot = new GameObject($"ShopItem_{name}");
            itemRoot.transform.SetParent(parent);
            var itemRect = itemRoot.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0.5f, 1);
            itemRect.anchorMax = new Vector2(0.5f, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.anchoredPosition = new Vector2(0, y);
            itemRect.sizeDelta = new Vector2(480, height - 5);

            string costStr = unlockNight > 0
                ? $"{name} ({cost} pts, unlocks Night {unlockNight})"
                : $"{name} ({cost} pts)";

            CreateText(itemRoot.transform, "Name", costStr, 20, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(300, height));

            var buyBtn = CreateButton(itemRoot.transform, "Buy", "Buy", new Color(0.3f, 0.6f, 0.3f),
                new Vector2(1, 0.5f), new Vector2(80, 35), onBuy);
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-10, 0);

            _shopBuyButtons.Add(buyBtn.GetComponent<Button>());

            y -= height;
        }

        private void BuyHealthKit()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(50)) return;
            if (!PointsSystem.Instance.SpendPoints(50, "Health Kit")) return;

            var player = GameObject.Find("Player");
            var health = player?.GetComponent<PlayerHealth>();
            health?.FullHeal();
            RefreshShopButtons();
        }

        private void BuyAmmoRefill()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(30)) return;
            if (!PointsSystem.Instance.SpendPoints(30, "Ammo Refill")) return;

            var player = GameObject.Find("Player");
            var shooting = player?.GetComponent<PlayerShooting>();
            shooting?.AddAmmo(60);
            RefreshShopButtons();
        }

        private void BuyShotgun()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(100)) return;
            int night = GameManager.Instance?.CurrentNight ?? 1;
            if (night < 1) return;
            if (!PointsSystem.Instance.SpendPoints(100, "Shotgun")) return;

            var weapon = WeaponData.CreateShotgun();
            ApplyWeapon(weapon);
            RefreshShopButtons();
        }

        private void BuyAssaultRifle()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(200)) return;
            int night = GameManager.Instance?.CurrentNight ?? 1;
            if (night < 2) return;
            if (!PointsSystem.Instance.SpendPoints(200, "Assault Rifle")) return;

            var weapon = WeaponData.CreateAssaultRifle();
            ApplyWeapon(weapon);
            RefreshShopButtons();
        }

        private void ApplyWeapon(WeaponData weapon)
        {
            var player = GameObject.Find("Player");
            var shooting = player?.GetComponent<PlayerShooting>();
            shooting?.SetWeapon(weapon);
        }

        private void RefreshShopButtons()
        {
            UpdateShopDisplay();
        }

        private void UpdateShopDisplay()
        {
            if (_shopPointsText != null && PointsSystem.Instance != null)
                _shopPointsText.text = $"Points: {PointsSystem.Instance.CurrentPoints}";

            if (_shopTitleText != null && GameManager.Instance != null)
                _shopTitleText.text = $"DAWN - Night {GameManager.Instance.CurrentNight} Survived!";

            int idx = 0;
            UpdateShopButtonState(idx++, 50);
            UpdateShopButtonState(idx++, 30);
            UpdateShopButtonState(idx++, 100, 1);
            UpdateShopButtonState(idx++, 200, 2);
        }

        private void UpdateShopButtonState(int index, int cost, int unlockNight = 0)
        {
            if (index >= _shopBuyButtons.Count) return;
            var btn = _shopBuyButtons[index];
            bool canAfford = PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);
            int night = GameManager.Instance?.CurrentNight ?? 1;
            bool unlocked = unlockNight <= 0 || night >= unlockNight;
            btn.interactable = canAfford && unlocked;
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
            title.fontStyle = FontStyle.Bold;

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
            title.fontStyle = FontStyle.Bold;

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
            GameManager.Instance?.RestartGame();
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            HideAllPanels();
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
                    _gameOverPanel?.SetActive(true);
                    Time.timeScale = 0f;
                    break;
                case GameState.Victory:
                    _victoryPanel?.SetActive(true);
                    Time.timeScale = 0f;
                    break;
            }
        }

        private void HideAllPanels()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_dawnShopPanel != null) _dawnShopPanel.SetActive(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_victoryPanel != null) _victoryPanel.SetActive(false);
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
            if (_statsText != null && PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                int nightReached = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentNight : stats.nightsSurvived + 1;
                _statsText.text = $"Night Reached: {nightReached}\n" +
                    $"Enemies Killed: {stats.enemiesKilled}\n" +
                    $"Points Earned: {stats.totalEarned}\n" +
                    $"Final Score: {stats.finalScore}";
            }
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
            if (_statsText != null && PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                _statsText.text = $"Nights Survived: {stats.nightsSurvived}\n" +
                    $"Enemies Killed: {stats.enemiesKilled}\n" +
                    $"Points Earned: {stats.totalEarned}\n" +
                    $"Final Score: {stats.finalScore}";
            }
        }
    }
}
