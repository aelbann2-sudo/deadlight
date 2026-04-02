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
        private GameObject _guidePanel;
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
        private GameObject _armorTabContent;
        private Button _weaponsTabBtn;
        private Button _upgradesTabBtn;

        private HashSet<WeaponType> _purchasedWeapons = new HashSet<WeaponType>();

        private List<Text> _upgradeLabels = new List<Text>();
        private List<Button> _upgradeBuyButtons = new List<Button>();

        private bool _waitingForEnding;
        private bool _resumeGameplayOnGuideClose;
        private Sprite _campaignNodeSprite;

        public bool IsGuideOpen => _guidePanel != null && _guidePanel.activeSelf;

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

        private void Update()
        {
            bool guideHotkeyPressed = Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1);

            if (IsGuideOpen)
            {
                if (guideHotkeyPressed || Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseGuide();
                }

                return;
            }

            if (!guideHotkeyPressed)
            {
                return;
            }

            bool inMenuFlow = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.MainMenu;
            bool inGameplay = GameManager.Instance != null && GameManager.Instance.IsGameplayState;
            if (!inMenuFlow && !inGameplay)
            {
                return;
            }

            OpenGuide(inGameplay && !GameManager.Instance.IsPaused);
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
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
            BuildGuidePanel();
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
            bg.color = new Color(0.04f, 0.04f, 0.07f, 1f);

            var headerBg = CreateInsetPanel(_mainMenuPanel.transform, "MenuHeaderBg",
                new Vector2(0f, 0.7f), new Vector2(1f, 1f), new Color(0.06f, 0.05f, 0.1f, 0.9f));

            var title = CreateText(headerBg.transform, "Title",
                "DEADLIGHT", 56, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.4f),
                new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(700, 70));
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;
            var titleGlow = title.AddComponent<Shadow>();
            titleGlow.effectColor = new Color(0.9f, 0.7f, 0f, 0.25f);
            titleGlow.effectDistance = new Vector2(0f, -2f);

            CreateText(headerBg.transform, "Subtitle",
                "SURVIVAL AFTER DARK", 20, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.55f),
                new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), Vector2.zero, new Vector2(400, 28));

            CreateButton(_mainMenuPanel.transform, "StartCampaignButton", "START CAMPAIGN", new Color(0.2f, 0.65f, 0.25f),
                new Vector2(0.5f, 0.55f), new Vector2(300, 55), StartCampaign);

            CreateButton(_mainMenuPanel.transform, "CampaignMapButton", "CAMPAIGN MAP", new Color(0.6f, 0.48f, 0.18f),
                new Vector2(0.5f, 0.42f), new Vector2(260, 44), ShowCampaignMap);

            CreateButton(_mainMenuPanel.transform, "LeaderboardButton", "LEADERBOARD", new Color(0.25f, 0.35f, 0.6f),
                new Vector2(0.5f, 0.32f), new Vector2(260, 44), ShowLeaderboard);

            CreateButton(_mainMenuPanel.transform, "GuideButton", "GUIDE", new Color(0.2f, 0.4f, 0.55f),
                new Vector2(0.5f, 0.22f), new Vector2(260, 44), OpenGuideFromButton);

            CreateButton(_mainMenuPanel.transform, "QuitButton", "QUIT", new Color(0.35f, 0.35f, 0.38f),
                new Vector2(0.5f, 0.12f), new Vector2(180, 38), QuitGame);
        }

        private void StartCampaign()
        {
            Time.timeScale = 1f;
            HideAllPanels();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.StartSelectedMapRun();
        }

        private void StartCampaignAtLevel(int level)
        {
            Time.timeScale = 1f;
            HideAllPanels();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.StartCampaignFromLevel(level);
        }

        private void ShowCampaignMap()
        {
            HideAllPanels();
            _mapSelectPanel?.SetActive(true);
        }

        // ===================== MAP SELECT =====================

        private static readonly string[] levelSubtitles = { "First Light", "No One Left Behind", "The Source", "Operation Deadlight" };
        private static readonly string[] levelMapNames = { "Town Center", "Suburban", "Industrial", "Research" };
        private static readonly string[] levelTeasers = {
            "Recover Flight 7's black box from the crash site.",
            "Search the school shelter for evacuation records.",
            "Breach the Lazarus lab and recover Subject 23 data.",
            "Arm the extraction beacon. Survive Subject 23."
        };

        private void BuildMapSelect()
        {
            _mapSelectPanel = CreatePanel(_canvasRoot.transform, "MapSelectPanel");
            _mapSelectPanel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 1f);

            var headerGlow = CreateInsetPanel(_mapSelectPanel.transform, "HeaderGlow",
                new Vector2(0f, 0.87f), new Vector2(1f, 1f), new Color(0.08f, 0.06f, 0.14f, 0.9f));

            var titleObj = CreateText(headerGlow.transform, "Title",
                "OPERATION DEADLIGHT", 42, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.3f),
                new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(700, 55));
            titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.8f, 0.6f, 0f, 0.3f);
            titleShadow.effectDistance = new Vector2(0f, -2f);

            CreateText(headerGlow.transform, "Subtitle",
                "Survive four levels. Reach the facility. Transmit the truth.", 16, TextAnchor.MiddleCenter, new Color(0.65f, 0.65f, 0.6f),
                new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(760, 22));

            var mapBoard = CreateInsetPanel(_mapSelectPanel.transform, "CampaignBoard",
                new Vector2(0.04f, 0.15f), new Vector2(0.96f, 0.85f), new Color(0.06f, 0.06f, 0.09f, 0.98f));
            var boardOutline = mapBoard.AddComponent<Outline>();
            boardOutline.effectColor = new Color(0.4f, 0.3f, 0.1f, 0.5f);
            boardOutline.effectDistance = new Vector2(2f, -2f);

            Vector2[] positions = {
                new Vector2(-320f, 0f),
                new Vector2(-108f, 0f),
                new Vector2(108f, 0f),
                new Vector2(320f, 0f)
            };

            Color[] nodeColors = {
                new Color(0.25f, 0.65f, 0.35f),
                new Color(0.5f, 0.7f, 0.3f),
                new Color(0.85f, 0.55f, 0.2f),
                new Color(0.75f, 0.2f, 0.2f)
            };

            Color[] glowColors = {
                new Color(0.2f, 0.8f, 0.3f, 0.15f),
                new Color(0.5f, 0.8f, 0.2f, 0.12f),
                new Color(0.9f, 0.6f, 0.1f, 0.12f),
                new Color(0.9f, 0.15f, 0.1f, 0.12f)
            };

            for (int i = 0; i < 3; i++)
            {
                CreateCampaignPathDotted(mapBoard.transform, positions[i], positions[i + 1],
                    Color.Lerp(nodeColors[i], nodeColors[i + 1], 0.5f));
            }

            for (int i = 0; i < 4; i++)
            {
                CreateCampaignNodeStyled(mapBoard.transform, i + 1, levelMapNames[i], positions[i], nodeColors[i], glowColors[i]);
            }

            CreateText(mapBoard.transform, "MapHint",
                "Click a level to begin  |  Difficulty increases left to right", 13, TextAnchor.MiddleCenter,
                new Color(0.45f, 0.45f, 0.4f),
                new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.04f), Vector2.zero, new Vector2(720f, 20f));

            CreateButton(_mapSelectPanel.transform, "BackButton", "BACK", new Color(0.25f, 0.25f, 0.3f),
                new Vector2(0.5f, 0.06f), new Vector2(200, 42), () =>
                {
                    _mapSelectPanel?.SetActive(false);
                    _mainMenuPanel?.SetActive(true);
                });
        }

        private void CreateCampaignPathDotted(Transform parent, Vector2 start, Vector2 end, Color color)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            int dotCount = Mathf.Max(3, Mathf.RoundToInt(length / 16f));
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            for (int i = 0; i <= dotCount; i++)
            {
                float t = (float)i / dotCount;
                Vector2 pos = Vector2.Lerp(start, end, t);

                var dot = new GameObject($"PathDot_{i}");
                dot.transform.SetParent(parent, false);
                var rect = dot.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                float dotSize = 6f + Mathf.Sin(t * Mathf.PI) * 3f;
                rect.sizeDelta = new Vector2(dotSize, dotSize);

                var img = dot.AddComponent<Image>();
                float fade = 0.3f + Mathf.Sin(t * Mathf.PI) * 0.4f;
                img.color = new Color(color.r, color.g, color.b, fade);
                img.raycastTarget = false;
                img.sprite = GetCampaignNodeSprite();
            }

            var arrowPos = Vector2.Lerp(start, end, 0.5f);
            var arrow = new GameObject("PathArrow");
            arrow.transform.SetParent(parent, false);
            var arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.anchoredPosition = arrowPos;
            arrowRect.sizeDelta = new Vector2(20f, 16f);
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);

            var arrowText = arrow.AddComponent<Text>();
            arrowText.text = "\u25B6";
            arrowText.font = _font;
            arrowText.fontSize = 14;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = new Color(color.r, color.g, color.b, 0.6f);
            arrowText.raycastTarget = false;
        }

        private void CreateCampaignNodeStyled(Transform parent, int levelNumber, string mapName,
            Vector2 position, Color color, Color glowColor)
        {
            var glowObj = new GameObject($"NodeGlow_{levelNumber}");
            glowObj.transform.SetParent(parent, false);
            var glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = position;
            glowRect.sizeDelta = new Vector2(160f, 160f);
            var glowImg = glowObj.AddComponent<Image>();
            glowImg.sprite = GetCampaignNodeSprite();
            glowImg.color = glowColor;
            glowImg.raycastTarget = false;

            var node = new GameObject($"LevelNode_{levelNumber}");
            node.transform.SetParent(parent, false);
            var nodeRect = node.AddComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.anchoredPosition = position;
            nodeRect.sizeDelta = new Vector2(110f, 110f);

            var nodeImage = node.AddComponent<Image>();
            nodeImage.sprite = GetCampaignNodeSprite();
            nodeImage.color = color;

            var button = node.AddComponent<Button>();
            button.targetGraphic = nodeImage;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(
                Mathf.Min(1f, color.r + 0.2f),
                Mathf.Min(1f, color.g + 0.2f),
                Mathf.Min(1f, color.b + 0.2f), 1f);
            colors.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            button.onClick.AddListener(() => StartCampaignAtLevel(levelNumber));

            var outerRing = node.AddComponent<Outline>();
            outerRing.effectColor = new Color(1f, 1f, 1f, 0.15f);
            outerRing.effectDistance = new Vector2(2f, -2f);

            var levelLabel = CreateText(node.transform, "LevelNumber",
                $"{levelNumber}", 40, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f));
            levelLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
            var numShadow = levelLabel.AddComponent<Shadow>();
            numShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            numShadow.effectDistance = new Vector2(1f, -2f);

            int subtitleIdx = Mathf.Clamp(levelNumber - 1, 0, levelSubtitles.Length - 1);
            string subtitle = levelSubtitles[subtitleIdx];
            string mapTitle = levelMapNames[subtitleIdx];

            var levelTag = CreateText(node.transform, "LevelTag",
                $"LEVEL {levelNumber}: {mapTitle.ToUpperInvariant()}", 13, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.5f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(240f, 20f));
            levelTag.GetComponent<Text>().fontStyle = FontStyle.Bold;
            var tagShadow = levelTag.AddComponent<Shadow>();
            tagShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            tagShadow.effectDistance = new Vector2(1f, -1f);

            var subtitleLabel = CreateText(node.transform, "Subtitle",
                $"\"{subtitle}\"", 14, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.75f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(240f, 22f));
            subtitleLabel.GetComponent<Text>().fontStyle = FontStyle.Italic;

            string teaser = levelTeasers[subtitleIdx];
            CreateText(node.transform, "Teaser",
                teaser, 11, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.48f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -42f), new Vector2(250f, 30f));

            string diffPips = "";
            for (int i = 0; i < 4; i++)
                diffPips += i < levelNumber ? "\u25CF " : "\u25CB ";
            Color diffColor = levelNumber <= 2 ? new Color(0.4f, 0.85f, 0.4f)
                : (levelNumber == 3 ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.95f, 0.3f, 0.3f));
            CreateText(node.transform, "Difficulty",
                diffPips.Trim(), 12, TextAnchor.MiddleCenter, diffColor,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -60f), new Vector2(120f, 18f));
        }

        private Sprite GetCampaignNodeSprite()
        {
            if (_campaignNodeSprite != null)
            {
                return _campaignNodeSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= radius)
                    {
                        float edge = Mathf.Clamp01((radius - distance) / 2f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, edge);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            _campaignNodeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _campaignNodeSprite;
        }

        private void BuildMapOption(Transform parent, string mapName, string desc,
            string tag, Color accentColor, float yAnchor, MapType mapType)
        {
            float cardWidth = 700f;
            float cardHeight = 120f;
            float accentBarWidth = 5f;

            // Card container
            var container = new GameObject($"MapOption_{mapName}");
            container.transform.SetParent(parent);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, yAnchor);
            containerRect.anchorMax = new Vector2(0.5f, yAnchor);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // Card background
            var containerImg = container.AddComponent<Image>();
            containerImg.color = new Color(0.12f, 0.13f, 0.17f, 0.95f);

            // Button behavior
            var btn = container.AddComponent<Button>();
            btn.targetGraphic = containerImg;
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.13f, 0.17f, 0.95f);
            colors.highlightedColor = new Color(0.18f, 0.19f, 0.24f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.13f, 1f);
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;
            btn.onClick.AddListener(() => OnMapSelected(mapType));

            // Left accent bar
            var accentBar = new GameObject("AccentBar");
            accentBar.transform.SetParent(container.transform);
            var accentRect = accentBar.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(accentBarWidth, 0f);
            var accentImg = accentBar.AddComponent<Image>();
            accentImg.color = accentColor;
            accentImg.raycastTarget = false;

            // Map name - positioned inside the card
            var nameObj = CreateLeftAlignedText(container.transform, "Name",
                mapName, 24, FontStyle.Bold, Color.white,
                new Vector2(accentBarWidth + 20f, -16f), new Vector2(cardWidth - 160f, 30f));

            // Description - positioned below name, inside the card
            CreateLeftAlignedText(container.transform, "Desc",
                desc, 14, FontStyle.Normal, new Color(0.6f, 0.62f, 0.68f),
                new Vector2(accentBarWidth + 20f, -50f), new Vector2(cardWidth - 160f, 50f));

            // Tag label on the right
            var tagObj = new GameObject("Tag");
            tagObj.transform.SetParent(container.transform);
            var tagRect = tagObj.AddComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(1f, 0.5f);
            tagRect.anchorMax = new Vector2(1f, 0.5f);
            tagRect.pivot = new Vector2(1f, 0.5f);
            tagRect.anchoredPosition = new Vector2(-20f, 0f);
            tagRect.sizeDelta = new Vector2(100f, 28f);
            var tagBg = tagObj.AddComponent<Image>();
            tagBg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f);
            tagBg.raycastTarget = false;

            var tagTextObj = new GameObject("TagText");
            tagTextObj.transform.SetParent(tagObj.transform);
            var tagTextRect = tagTextObj.AddComponent<RectTransform>();
            tagTextRect.anchorMin = Vector2.zero;
            tagTextRect.anchorMax = Vector2.one;
            tagTextRect.offsetMin = Vector2.zero;
            tagTextRect.offsetMax = Vector2.zero;
            var tagText = tagTextObj.AddComponent<Text>();
            tagText.text = tag;
            tagText.font = _font;
            tagText.fontSize = 13;
            tagText.fontStyle = FontStyle.Bold;
            tagText.alignment = TextAnchor.MiddleCenter;
            tagText.color = accentColor;
            tagText.raycastTarget = false;

            // Bottom border line
            var borderLine = new GameObject("Border");
            borderLine.transform.SetParent(container.transform);
            var borderRect = borderLine.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(1f, 0f);
            borderRect.pivot = new Vector2(0.5f, 0f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(0f, 1f);
            var borderImg = borderLine.AddComponent<Image>();
            borderImg.color = new Color(0.25f, 0.26f, 0.3f, 0.5f);
            borderImg.raycastTarget = false;
        }

        private GameObject CreateLeftAlignedText(Transform parent, string name,
            string text, int fontSize, FontStyle style, Color color,
            Vector2 topLeftOffset, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftOffset;
            rect.sizeDelta = size;

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.UpperLeft;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.raycastTarget = false;

            return obj;
        }

        private void OnMapSelected(MapType mapType)
        {
            GameManager.Instance?.SetMap(mapType);
            Time.timeScale = 1f;
            GameManager.Instance?.StartSelectedMapRun();
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
                new Vector2(0.5f, 0.6f), new Vector2(260, 50), OnResume);

            CreateButton(_pausePanel.transform, "PauseGuideButton", "GUIDE", new Color(0.2f, 0.45f, 0.6f),
                new Vector2(0.5f, 0.48f), new Vector2(260, 50), OpenGuideFromButton);

            CreateButton(_pausePanel.transform, "PauseRestartButton", "RESTART", new Color(0.7f, 0.6f, 0.2f),
                new Vector2(0.5f, 0.36f), new Vector2(260, 50), RestartGame);

            CreateButton(_pausePanel.transform, "PauseMainMenuButton", "MAIN MENU", new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.24f), new Vector2(260, 50), GoToMainMenu);

            CreateButton(_pausePanel.transform, "PauseQuitButton", "QUIT GAME", new Color(0.65f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.12f), new Vector2(260, 50), QuitGame);
        }

        private void BuildGuidePanel()
        {
            _guidePanel = CreatePanel(_canvasRoot.transform, "GuidePanel");
            _guidePanel.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.08f, 0.96f);

            CreateText(_guidePanel.transform, "Title",
                "SURVIVAL GUIDE", 42, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(640, 56));

            CreateText(_guidePanel.transform, "Subtitle",
                "Controls, core rules, and item explanations for the full day-night loop",
                18, TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.84f),
                new Vector2(0.5f, 0.875f), new Vector2(0.5f, 0.875f), Vector2.zero, new Vector2(900, 28));

            CreateGuideSection(_guidePanel.transform, "ControlsSection", "CONTROLS",
                GameplayGuideContent.GetControlsText(),
                new Vector2(0.05f, 0.17f), new Vector2(0.31f, 0.8f),
                new Color(0.1f, 0.14f, 0.2f, 0.95f));

            string loopText = GameplayGuideContent.GetRulesText() + "\n\n" + GameplayGuideContent.GetSystemsText();
            CreateGuideSection(_guidePanel.transform, "LoopSection", "SURVIVAL LOOP",
                loopText,
                new Vector2(0.345f, 0.17f), new Vector2(0.655f, 0.8f),
                new Color(0.1f, 0.13f, 0.17f, 0.95f));

            string itemText = GameplayGuideContent.GetItemsText() + "\n\n" + GameplayGuideContent.GetAccessibilityNote();
            CreateGuideSection(_guidePanel.transform, "ItemsSection", "ITEMS + ONBOARDING",
                itemText,
                new Vector2(0.69f, 0.17f), new Vector2(0.95f, 0.8f),
                new Color(0.12f, 0.11f, 0.16f, 0.95f));

            CreateText(_guidePanel.transform, "Footer",
                "Press H, F1, or Esc to close this guide.",
                17, TextAnchor.MiddleCenter, new Color(0.75f, 0.8f, 0.85f),
                new Vector2(0.5f, 0.11f), new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(520, 26));

            CreateButton(_guidePanel.transform, "GuideCloseButton", "CLOSE", new Color(0.25f, 0.5f, 0.68f),
                new Vector2(0.5f, 0.06f), new Vector2(220, 46), CloseGuide);
        }

        private void OpenGuideFromButton()
        {
            bool pauseGameplay = GameManager.Instance != null && GameManager.Instance.IsGameplayState && !GameManager.Instance.IsPaused;
            OpenGuide(pauseGameplay);
        }

        private void OpenGuide(bool pauseGameplay)
        {
            if (_guidePanel == null)
            {
                return;
            }

            if (pauseGameplay && GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(true);
                _resumeGameplayOnGuideClose = true;
            }
            else
            {
                _resumeGameplayOnGuideClose = false;
            }

            _guidePanel.SetActive(true);
        }

        private void CloseGuide()
        {
            if (_guidePanel != null)
            {
                _guidePanel.SetActive(false);
            }

            bool shouldResumeGameplay = _resumeGameplayOnGuideClose;
            _resumeGameplayOnGuideClose = false;

            if (shouldResumeGameplay && GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                GameManager.Instance.SetPaused(false);
            }
        }

        private void OnResume()
        {
            GameManager.Instance?.SetPaused(false);
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused && GameManager.Instance != null && GameManager.Instance.IsGameplayState)
            {
                _pausePanel?.SetActive(true);
            }
            else
            {
                _pausePanel?.SetActive(false);

                if (_resumeGameplayOnGuideClose)
                {
                    _guidePanel?.SetActive(false);
                    _resumeGameplayOnGuideClose = false;
                }
            }
        }

        private void QuitGame()
        {
            GameManager.Instance?.QuitGame();
        }

        private void BuildDawnShop()
        {
            _dawnShopPanel = CreatePanel(_canvasRoot.transform, "DawnShopPanel");
            _dawnShopPanel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.97f);

            var headerBg = CreateInsetPanel(_dawnShopPanel.transform, "HeaderBg",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), new Color(0.08f, 0.06f, 0.14f, 0.95f));

            _shopTitleText = CreateText(headerBg.transform, "ShopTitle",
                "DAWN - Level 1 Cleared!", 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.4f),
                new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(600, 36)).GetComponent<Text>();
            _shopTitleText.fontStyle = FontStyle.Bold;

            _shopSummaryText = CreateText(headerBg.transform, "ShopSummary",
                "", 14, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f),
                new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(600, 20)).GetComponent<Text>();

            _shopPointsText = CreateText(_dawnShopPanel.transform, "ShopPoints",
                "Points: 0", 24, TextAnchor.MiddleCenter, new Color(0.4f, 1f, 0.4f),
                new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(250, 30)).GetComponent<Text>();
            _shopPointsText.fontStyle = FontStyle.Bold;

            var suppliesRow = new GameObject("SuppliesRow");
            suppliesRow.transform.SetParent(_dawnShopPanel.transform);
            var srRect = suppliesRow.AddComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0.5f, 0.81f);
            srRect.anchorMax = new Vector2(0.5f, 0.81f);
            srRect.pivot = new Vector2(0.5f, 0.5f);
            srRect.anchoredPosition = Vector2.zero;
            srRect.sizeDelta = new Vector2(500, 38);

            var healBtn = CreateButton(suppliesRow.transform, "HealBtn", "Health Kit (50)", new Color(0.55f, 0.2f, 0.2f),
                new Vector2(0.25f, 0.5f), new Vector2(200, 34), BuyHealthKit);
            _shopBuyButtons.Add(healBtn.GetComponent<Button>());

            var ammoBtn = CreateButton(suppliesRow.transform, "AmmoBtn", "Ammo Refill (30)", new Color(0.6f, 0.5f, 0.12f),
                new Vector2(0.75f, 0.5f), new Vector2(200, 34), BuyAmmoRefill);
            _shopBuyButtons.Add(ammoBtn.GetComponent<Button>());

            _weaponsTabBtn = CreateButton(_dawnShopPanel.transform, "WeaponsTab", "WEAPONS",
                new Color(0.3f, 0.45f, 0.6f),
                new Vector2(0.3f, 0.76f), new Vector2(180, 34), () => ShowShopTab(true)).GetComponent<Button>();
            _upgradesTabBtn = CreateButton(_dawnShopPanel.transform, "UpgradesTab", "UPGRADES",
                new Color(0.45f, 0.35f, 0.55f),
                new Vector2(0.5f, 0.76f), new Vector2(180, 34), () => ShowShopTab(false)).GetComponent<Button>();

            var armorTabBtn = CreateButton(_dawnShopPanel.transform, "ArmorTab", "ARMOR",
                new Color(0.2f, 0.35f, 0.55f),
                new Vector2(0.7f, 0.76f), new Vector2(180, 34), () => ShowShopTab(2));

            _weaponsTabContent = new GameObject("WeaponsContent");
            _weaponsTabContent.transform.SetParent(_dawnShopPanel.transform);
            var wcRect = _weaponsTabContent.AddComponent<RectTransform>();
            wcRect.anchorMin = new Vector2(0.5f, 0.16f);
            wcRect.anchorMax = new Vector2(0.5f, 0.73f);
            wcRect.pivot = new Vector2(0.5f, 0.5f);
            wcRect.anchoredPosition = Vector2.zero;
            wcRect.sizeDelta = new Vector2(600, 0);

            BuildWeaponItems();

            _upgradesTabContent = new GameObject("UpgradesContent");
            _upgradesTabContent.transform.SetParent(_dawnShopPanel.transform);
            var ucRect = _upgradesTabContent.AddComponent<RectTransform>();
            ucRect.anchorMin = new Vector2(0.5f, 0.16f);
            ucRect.anchorMax = new Vector2(0.5f, 0.73f);
            ucRect.pivot = new Vector2(0.5f, 0.5f);
            ucRect.anchoredPosition = Vector2.zero;
            ucRect.sizeDelta = new Vector2(600, 0);

            BuildUpgradeItems();
            _upgradesTabContent.SetActive(false);

            _armorTabContent = new GameObject("ArmorContent");
            _armorTabContent.transform.SetParent(_dawnShopPanel.transform);
            var acRect = _armorTabContent.AddComponent<RectTransform>();
            acRect.anchorMin = new Vector2(0.5f, 0.16f);
            acRect.anchorMax = new Vector2(0.5f, 0.73f);
            acRect.pivot = new Vector2(0.5f, 0.5f);
            acRect.anchoredPosition = Vector2.zero;
            acRect.sizeDelta = new Vector2(600, 0);

            BuildArmorItems();
            _armorTabContent.SetActive(false);

            CreateButton(_dawnShopPanel.transform, "ContinueButton", "CONTINUE",
                new Color(0.15f, 0.55f, 0.85f),
                new Vector2(0.5f, 0.07f), new Vector2(300, 50), OnContinueToNextNight);
        }

        private void BuildWeaponItems()
        {
            float y = 0;
            int h = 60;
            AddWeaponShopItem(_weaponsTabContent.transform, "Shotgun", "Close-range spread", 100, 1,
                WeaponType.Shotgun, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "SMG", "Fast fire rate", 150, 2,
                WeaponType.SMG, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Sniper Rifle", "High damage", 250, 2,
                WeaponType.SniperRifle, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Assault Rifle", "Balanced auto", 200, 3,
                WeaponType.AssaultRifle, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Grenade Launcher", "Area damage", 350, 3,
                WeaponType.GrenadeLauncher, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Flamethrower", "Burn DoT", 400, 4,
                WeaponType.Flamethrower, ref y, h);
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
            rect.sizeDelta = new Vector2(560, height - 4);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 0.8f);

            CreateText(root.transform, "Name", name, 19, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 6), new Vector2(260, 26));
            CreateText(root.transform, "Desc", $"{desc}  |  {cost} pts", 13,
                TextAnchor.MiddleLeft, new Color(0.55f, 0.55f, 0.6f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -12), new Vector2(320, 18));

            var wt = weaponType;
            var buyBtn = CreateButton(root.transform, "Buy", "BUY", new Color(0.25f, 0.55f, 0.25f),
                new Vector2(1, 0.5f), new Vector2(80, 32), () => BuyWeapon(wt, cost, unlockNight));
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-8, 0);

            _shopBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void BuildUpgradeItems()
        {
            float y = 0;
            int h = 56;
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
            rect.sizeDelta = new Vector2(560, height - 4);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.1f, 0.2f, 0.8f);

            var label = CreateText(root.transform, "Label", name, 18, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(380, height)).GetComponent<Text>();
            _upgradeLabels.Add(label);

            var buyBtn = CreateButton(root.transform, "Buy", "UPGRADE", new Color(0.45f, 0.3f, 0.6f),
                new Vector2(1, 0.5f), new Vector2(100, 32), onBuy);
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-8, 0);

            _upgradeBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void BuildArmorItems()
        {
            float y = 0;
            int h = 60;
            AddArmorItem(_armorTabContent.transform, "Vest Lv1", "Light body armor", 80, ArmorTier.Level1, false, ref y, h);
            AddArmorItem(_armorTabContent.transform, "Vest Lv2", "Heavy body armor", 180, ArmorTier.Level2, false, ref y, h);
            AddArmorItem(_armorTabContent.transform, "Helmet Lv1", "Basic head protection", 60, ArmorTier.Level1, true, ref y, h);
            AddArmorItem(_armorTabContent.transform, "Helmet Lv2", "Reinforced helmet", 140, ArmorTier.Level2, true, ref y, h);
        }

        private void AddArmorItem(Transform parent, string name, string desc, int cost,
            ArmorTier tier, bool isHelmet, ref float y, int height)
        {
            var root = new GameObject($"Armor_{name}");
            root.transform.SetParent(parent);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(560, height - 4);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.14f, 0.2f, 0.8f);

            CreateText(root.transform, "Name", name, 19, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 6), new Vector2(260, 26));
            CreateText(root.transform, "Desc", $"{desc}  |  {cost} pts", 13,
                TextAnchor.MiddleLeft, new Color(0.55f, 0.55f, 0.6f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(12, -12), new Vector2(320, 18));

            var buyBtn = CreateButton(root.transform, "Buy", "BUY", new Color(0.2f, 0.4f, 0.65f),
                new Vector2(1, 0.5f), new Vector2(80, 32), () => BuyArmor(tier, isHelmet, cost));
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-8, 0);

            _shopBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void ShowShopTab(bool showWeapons)
        {
            ShowShopTab(showWeapons ? 0 : 1);
        }

        private void ShowShopTab(int tabIndex)
        {
            if (_weaponsTabContent != null) _weaponsTabContent.SetActive(tabIndex == 0);
            if (_upgradesTabContent != null) _upgradesTabContent.SetActive(tabIndex == 1);
            if (_armorTabContent != null) _armorTabContent.SetActive(tabIndex == 2);

            Color active = new Color(0.3f, 0.5f, 0.7f);
            Color inactive = new Color(0.15f, 0.15f, 0.22f);

            if (_weaponsTabBtn != null)
            {
                var c = _weaponsTabBtn.colors;
                c.normalColor = tabIndex == 0 ? active : inactive;
                _weaponsTabBtn.colors = c;
            }
            if (_upgradesTabBtn != null)
            {
                var c = _upgradesTabBtn.colors;
                c.normalColor = tabIndex == 1 ? new Color(0.5f, 0.35f, 0.65f) : inactive;
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
                _shopTitleText.text = $"DAWN - Level {GameManager.Instance.CurrentNight} Cleared!";

            if (_shopSummaryText != null && PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                _shopSummaryText.text = $"Kills: {stats.enemiesKilled}  |  Earned: {stats.totalEarned}";
            }

            int night = GameManager.Instance?.CurrentNight ?? 1;

            UpdateSupplyButton(0, 50);
            UpdateSupplyButton(1, 30);

            WeaponType[] weaponTypes = { WeaponType.Shotgun, WeaponType.SMG, WeaponType.SniperRifle, WeaponType.AssaultRifle, WeaponType.GrenadeLauncher, WeaponType.Flamethrower };
            int[] weaponCosts = { 100, 150, 250, 200, 350, 400 };
            int[] weaponNights = { 1, 2, 2, 3, 3, 4 };
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
                    labelText.text = sold ? "SOLD" : (unlocked ? "BUY" : $"Lv{weaponNights[i]}+");
            }

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
                    HandleEndingState(false);
                    break;
                case GameState.Victory:
                    HandleEndingState(true);
                    break;
            }
        }

        private void OnEndingSequenceComplete()
        {
            if (!_waitingForEnding)
            {
                return;
            }

            if (EndingSequence.Instance != null)
                EndingSequence.Instance.OnEndingComplete -= OnEndingSequenceComplete;

            _waitingForEnding = false;

            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.Victory)
            {
                ShowVictoryPanel();
            }
            else if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                ShowGameOverPanel();
            }
        }

        private void HandleEndingState(bool victory)
        {
            LeaderboardManager.Instance?.SubmitRun(victory);

            if (TryQueueEndingSequence())
            {
                return;
            }

            if (victory)
            {
                ShowVictoryPanel();
            }
            else
            {
                ShowGameOverPanel();
            }
        }

        private bool TryQueueEndingSequence()
        {
            if (_waitingForEnding)
            {
                return true;
            }

            if (EndingSequence.Instance == null)
            {
                return false;
            }

            _waitingForEnding = true;
            EndingSequence.Instance.OnEndingComplete += OnEndingSequenceComplete;
            return true;
        }

        private void ShowGameOverPanel()
        {
            _gameOverPanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        private void ShowVictoryPanel()
        {
            _victoryPanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        private void HideAllPanels()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_mapSelectPanel != null) _mapSelectPanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_guidePanel != null) _guidePanel.SetActive(false);
            if (_dawnShopPanel != null) _dawnShopPanel.SetActive(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_victoryPanel != null) _victoryPanel.SetActive(false);
            if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
            _resumeGameplayOnGuideClose = false;
        }

        // ===================== LEADERBOARD =====================

        private void BuildLeaderboardPanel()
        {
            _leaderboardPanel = CreatePanel(_canvasRoot.transform, "LeaderboardPanel");
            _leaderboardPanel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

            CreateText(_leaderboardPanel.transform, "Title",
                "LEADERBOARD", 40, TextAnchor.MiddleCenter, new Color(0.9f, 0.8f, 0.3f),
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(500, 55));

            var headerText = "RANK    SCORE    LEVELS    KILLS    MAP";
            CreateText(_leaderboardPanel.transform, "Header", headerText, 16, TextAnchor.MiddleCenter,
                new Color(0.6f, 0.6f, 0.6f),
                new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(760, 25));

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

        private void CreateGuideSection(Transform parent, string name, string title, string body,
            Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor)
        {
            var section = CreateInsetPanel(parent, name, anchorMin, anchorMax, backgroundColor);

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(section.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(18f, -56f);
            titleRect.offsetMax = new Vector2(-18f, -18f);

            var titleText = titleObj.AddComponent<Text>();
            titleText.font = _font;
            titleText.fontSize = 21;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = new Color(0.95f, 0.87f, 0.45f);
            titleText.text = title;

            var titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            titleShadow.effectDistance = new Vector2(1f, -1f);

            var bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(section.transform, false);
            var bodyRect = bodyObj.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(18f, 18f);
            bodyRect.offsetMax = new Vector2(-18f, -70f);

            var bodyText = bodyObj.AddComponent<Text>();
            bodyText.font = _font;
            bodyText.fontSize = 17;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.color = new Color(0.86f, 0.9f, 0.95f);
            bodyText.text = body;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;

            var bodyShadow = bodyObj.AddComponent<Shadow>();
            bodyShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
            bodyShadow.effectDistance = new Vector2(1f, -1f);
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
                    entryText.text = $"#{i + 1}      {e.score}      {e.nightsReached}      {e.kills}      {e.map}{victoryMark}";
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

        private GameObject CreateInsetPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = obj.AddComponent<Image>();
            img.color = color;
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

            int levelReached = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentNight : 1;
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
                    if (entry.nightsReached == levelReached && entry.kills == kills)
                    {
                        finalScore = entry.score;
                        rank = LeaderboardManager.Instance.GetRank(entry.score);
                        break;
                    }
                }
            }

            _statsText.text = $"Level Reached: {levelReached}\n" +
                $"Enemies Killed: {kills}\n" +
                $"Points Earned: {totalEarned}\n" +
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

            string map = Core.GameManager.Instance != null ? Core.GameManager.Instance.SelectedMap.ToString() : "TownCenter";
            int kills = 0;
            int totalEarned = 0;
            int levelsCleared = 4;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                totalEarned = stats.totalEarned;
                levelsCleared = stats.nightsSurvived;
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

            _statsText.text = $"ALL {levelsCleared} LEVELS CLEARED!\n" +
                $"Enemies Killed: {kills}\n" +
                $"Points Earned: {totalEarned}\n" +
                $"Map: {map}\n" +
                (rank > 0 ? $"Leaderboard Rank: #{rank}  (Score: {finalScore})" : "");
        }
    }
}
