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
    /// <summary>
    /// Modernized main UI controller. Builds every screen procedurally using
    /// <see cref="UIFactory"/> helpers and the <see cref="UITheme"/> design
    /// tokens. All panels use CanvasGroup-based fade transitions instead of
    /// raw SetActive snapping.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        // ── Root ──────────────────────────────────────────────
        private Canvas _canvas;
        private GameObject _canvasRoot;

        // ── Panels ────────────────────────────────────────────
        private GameObject _mainMenuPanel;
        private GameObject _mapSelectPanel;
        private GameObject _pausePanel;
        private GameObject _guidePanel;
        private GameObject _dawnShopPanel;
        private GameObject _levelCompletePanel;
        private GameObject _gameOverPanel;
        private GameObject _victoryPanel;
        private GameObject _leaderboardPanel;

        // ── Shop state ────────────────────────────────────────
        private Text _shopPointsText;
        private Text _shopTitleText;
        private Text _shopSummaryText;
        private readonly List<Button> _shopBuyButtons = new List<Button>();

        private GameObject _weaponsTabContent;
        private GameObject _upgradesTabContent;
        private GameObject _armorTabContent;
        private Button _weaponsTabBtn;
        private Button _upgradesTabBtn;
        private Button _armorTabBtn;

        private readonly HashSet<WeaponType> _purchasedWeapons = new HashSet<WeaponType>();
        private readonly List<Text> _upgradeLabels = new List<Text>();
        private readonly List<Button> _upgradeBuyButtons = new List<Button>();
        private const int SupplyButtonCount = 4;
        private const int HealCost = 50;
        private const int AmmoRefillCost = 30;
        private const int GrenadeRefillCost = 35;
        private const int MolotovRefillCost = 45;

        // ── Campaign state ────────────────────────────────────
        private Text _mainMenuProgressText;
        private Text _mapSelectProgressText;
        private Text _levelCompleteStatsText;
        private readonly List<CampaignRouteRowBinding> _campaignRouteRows = new List<CampaignRouteRowBinding>();
        private readonly List<CampaignCardBinding> _campaignCards = new List<CampaignCardBinding>();

        private bool _waitingForEnding;
        private bool _resumeGameplayOnGuideClose;

        // ── Campaign data ─────────────────────────────────────
        private static readonly string[] levelSubtitles = { "Crash Evidence", "Shelter Records", "Lazarus Breach", "Containment Finale" };
        private static readonly string[] levelMapNames = { "Town Center", "Suburban Evacuation", "Industrial District", "Research Facility" };
        private static readonly string[] levelStageLabels = { "3 objective nights", "3 objective nights", "3 objective nights", "3 nights + boss finale" };
        private static readonly string[] levelPreviewKeys = { "TownCenter", "Suburban", "Industrial", "Research" };
        private const int MaxSelectableLevel = 1;
        private static readonly string[] levelObjectiveSummaries = {
            "Recover Flight 7's black box.",
            "Recover the shelter evacuation records.",
            "Recover Lazarus and Subject 23 data.",
            "Arm the beacon and defeat Subject 23."
        };
        private static readonly string[] levelTeasers = {
            "Primary objective: recover Flight 7's black box across three daylight leads.",
            "Primary objective: recover the school shelter evacuation records across three neighborhood leads.",
            "Primary objective: breach Lazarus and recover Subject 23 data across three industrial pushes.",
            "Primary objective: arm the extraction beacon, finish the final three pushes, and defeat Subject 23."
        };

        // ── Helper classes ────────────────────────────────────
        private sealed class CampaignRouteRowBinding
        {
            public int Level;
            public Text StatusText;
            public Image StatusBackground;
        }

        private sealed class CampaignCardBinding
        {
            public int Level;
            public Button Button;
            public Text StatusText;
            public Text ActionText;
            public Image PreviewImage;
            public Image LockOverlay;
            public Color FallbackColor;
        }

        public bool IsGuideOpen => _guidePanel != null && _guidePanel.activeSelf;

        // =====================================================================
        // LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            EnsureEventSystem();
            BuildAllUI();
            RefreshCampaignPresentation();
            HideAllPanelsImmediate();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnPauseChanged += OnPauseChanged;
            }

            RefreshForCurrentState();
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
            bool guideKey = Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1);

            if (IsGuideOpen)
            {
                if (guideKey || Input.GetKeyDown(KeyCode.Escape)) CloseGuide();
                return;
            }

            if (!guideKey) return;

            bool inMenu = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.MainMenu;
            bool inGame = GameManager.Instance != null && GameManager.Instance.IsGameplayState;
            if (!inMenu && !inGame) return;

            OpenGuide(inGame && !GameManager.Instance.IsPaused);
        }

        // =====================================================================
        // EVENT SYSTEM
        // =====================================================================

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        public void RefreshForCurrentState()
        {
            if (_mainMenuPanel == null)
            {
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.ShouldSuppressMainMenuPresentation)
            {
                HideAllPanelsImmediate();
                return;
            }

            if (GameManager.Instance != null)
            {
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                HideAllPanelsImmediate();
                ShowPanel(_mainMenuPanel);
            }
        }

        // =====================================================================
        // PANEL TRANSITION HELPERS
        // =====================================================================

        private void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            UIFactory.FadePanel(this, panel, true);
        }

        private void HidePanel(GameObject panel)
        {
            if (panel == null) return;
            UIFactory.FadePanel(this, panel, false);
        }

        private void HideAllPanelsImmediate()
        {
            GameObject[] panels = {
                _mainMenuPanel, _mapSelectPanel, _pausePanel, _guidePanel,
                _dawnShopPanel, _levelCompletePanel, _gameOverPanel,
                _victoryPanel, _leaderboardPanel
            };
            foreach (var p in panels)
            {
                if (p == null) continue;
                p.SetActive(false);
                var cg = p.GetComponent<CanvasGroup>();
                if (cg == null) cg = p.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
            _resumeGameplayOnGuideClose = false;
        }

        private void HideAllPanelsFade()
        {
            HidePanel(_mainMenuPanel);
            HidePanel(_mapSelectPanel);
            HidePanel(_pausePanel);
            HidePanel(_guidePanel);
            HidePanel(_dawnShopPanel);
            HidePanel(_levelCompletePanel);
            HidePanel(_gameOverPanel);
            HidePanel(_victoryPanel);
            HidePanel(_leaderboardPanel);
            _resumeGameplayOnGuideClose = false;
        }

        // =====================================================================
        // BUILD UI
        // =====================================================================

        private void BuildAllUI()
        {
            _canvasRoot = new GameObject("GameUICanvas");
            _canvasRoot.transform.SetParent(transform);
            _canvas = UIFactory.CreateScreenCanvas(_canvasRoot.transform, "Canvas", 200);
            // The factory wraps everything inside "Canvas" — re-parent to _canvasRoot
            _canvas.transform.SetParent(transform);
            Destroy(_canvasRoot);
            _canvasRoot = _canvas.gameObject;

            BuildMainMenu();
            BuildMapSelect();
            BuildPauseMenu();
            BuildGuidePanel();
            BuildDawnShop();
            BuildLevelCompleteScreen();
            BuildGameOverScreen();
            BuildVictoryScreen();
            BuildLeaderboardPanel();
        }

        // =====================================================================
        // MAIN MENU
        // =====================================================================

        private void BuildMainMenu()
        {
            _mainMenuPanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "MainMenuPanel", UITheme.BgDark);

            // Left column
            var left = UIFactory.CreateRegion(_mainMenuPanel.transform, "LeftCol",
                new Vector2(0f, 0f), new Vector2(0.36f, 1f),
                UITheme.Darken(UITheme.BgDark, 0.25f), new Vector2(40f, 40f));

            UIFactory.CreateTextAt(left.transform, "Title", "DEADLIGHT",
                UITheme.FontHero, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(460f, 60f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(left.transform, "Subtitle", "Survival After Dark",
                UITheme.FontHeading, UITheme.WithAlpha(UITheme.TextPrimary, 0.85f),
                new Vector2(0f, 1f), new Vector2(26f, -96f), new Vector2(400f, 32f),
                TextAnchor.UpperLeft);

            UIFactory.CreateTextAt(left.transform, "Body",
                "Deliverable 2 prototype focuses on Level 1 (three objective nights). Levels 2 to 4 are marked COMING SOON for next deliverables.",
                UITheme.FontBody, UITheme.TextSecondary,
                new Vector2(0f, 1f), new Vector2(26f, -148f), new Vector2(480f, 80f),
                TextAnchor.UpperLeft);

            UIFactory.CreateTextAt(left.transform, "ProgressLabel", "NEXT DEPLOYMENT",
                UITheme.FontCaption, UITheme.AccentBlue,
                new Vector2(0f, 1f), new Vector2(26f, -254f), new Vector2(200f, 18f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            _mainMenuProgressText = UIFactory.CreateTextAt(left.transform, "ProgressValue", "",
                UITheme.FontBody + 2, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(26f, -276f), new Vector2(480f, 50f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            // Action buttons
            UIFactory.CreateActionButton(left.transform, "StartBtn", "Continue Campaign",
                "Deploy from the highest unlocked level.",
                UITheme.AccentGreen, new Vector2(0f, 1f), new Vector2(24f, -344f),
                new Vector2(500f, 86f), StartCampaign);

            UIFactory.CreateActionButton(left.transform, "MapBtn", "Level Select",
                "Play Level 1 now, preview upcoming levels.",
                UITheme.AccentGold, new Vector2(0f, 1f), new Vector2(24f, -442f),
                new Vector2(500f, 86f), ShowCampaignMap);

            UIFactory.CreateActionButton(left.transform, "GuideBtn", "Guide",
                "Controls, systems, and survival tips.",
                UITheme.AccentBlue, new Vector2(0f, 1f), new Vector2(24f, -540f),
                new Vector2(500f, 86f), OpenGuideFromButton);

            UIFactory.CreateCompactButton(left.transform, "LeaderboardBtn", "Leaderboard",
                UITheme.Darken(UITheme.AccentBlue, 0.2f),
                new Vector2(0f, 0f), new Vector2(24f, 80f), new Vector2(240f, 48f),
                ShowLeaderboard);

            UIFactory.CreateCompactButton(left.transform, "QuitBtn", "Quit",
                UITheme.BgLight,
                new Vector2(0f, 0f), new Vector2(278f, 80f), new Vector2(160f, 48f),
                QuitGame);

            // Right column — level overview
            var right = UIFactory.CreateRegion(_mainMenuPanel.transform, "RightCol",
                new Vector2(0.38f, 0f), new Vector2(1f, 1f),
                UITheme.Darken(UITheme.BgDark, 0.15f), new Vector2(40f, 40f));

            UIFactory.CreateTextAt(right.transform, "RouteTitle", "Level Overview",
                UITheme.FontTitle, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(400f, 40f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(right.transform, "RouteDesc",
                "Level 1 is playable now with three objective nights. Levels 2 to 4 are upcoming for the next deliverables.",
                UITheme.FontBody, UITheme.TextSecondary,
                new Vector2(0f, 1f), new Vector2(24f, -72f), new Vector2(900f, 48f),
                TextAnchor.UpperLeft);

            _campaignRouteRows.Clear();
            float rowY = -148f;
            for (int i = 0; i < 4; i++)
            {
                CreateRouteRow(right.transform, i + 1, levelMapNames[i], levelTeasers[i],
                    UITheme.LevelAccents[i], new Vector2(24f, rowY));
                rowY -= 146f;
            }
        }

        private void CreateRouteRow(Transform parent, int level, string title, string teaser,
            Color accent, Vector2 pos)
        {
            var row = UIFactory.CreateCard(parent, $"RouteRow_{level}",
                new Vector2(0f, 1f), new Vector2(980f, 130f), UITheme.BgMedium);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = pos;

            // Preview area
            var preview = UIFactory.CreateCard(row.transform, "Preview",
                new Vector2(0f, 0.5f), new Vector2(106f, 106f),
                UITheme.Darken(accent, 0.4f));
            var prevRt = preview.GetComponent<RectTransform>();
            prevRt.anchoredPosition = new Vector2(12f, 0f);
            prevRt.pivot = new Vector2(0f, 0.5f);

            // Load preview sprite if available
            var prevImg = preview.GetComponent<Image>();
            var sprite = LoadMenuPreviewSprite(levelPreviewKeys[level - 1]);
            if (sprite != null) { prevImg.sprite = sprite; prevImg.color = Color.white; prevImg.preserveAspect = true; }

            float textX = 132f;
            UIFactory.CreateTextAt(row.transform, "Eyebrow", $"LEVEL {level:00}",
                UITheme.FontCaption, accent,
                new Vector2(0f, 1f), new Vector2(textX, -18f), new Vector2(180f, 18f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(row.transform, "Title", title,
                UITheme.FontHeading, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(textX, -40f), new Vector2(500f, 30f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(row.transform, "Teaser", teaser,
                UITheme.FontCaption, UITheme.TextSecondary,
                new Vector2(0f, 1f), new Vector2(textX, -76f), new Vector2(600f, 42f),
                TextAnchor.UpperLeft);

            // Status badge
            var badge = UIFactory.CreateCard(row.transform, "StatusBadge",
                new Vector2(1f, 0.5f), new Vector2(110f, 32f), UITheme.BgLight);
            badge.GetComponent<RectTransform>().anchoredPosition = new Vector2(-16f, 0f);
            badge.GetComponent<RectTransform>().pivot = new Vector2(1f, 0.5f);

            var statusTxt = UIFactory.CreateText(badge.transform, "Status", "",
                UITheme.FontCaption, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(statusTxt.GetComponent<RectTransform>());

            _campaignRouteRows.Add(new CampaignRouteRowBinding
            {
                Level = level,
                StatusText = statusTxt,
                StatusBackground = badge.GetComponent<Image>()
            });
        }

        // =====================================================================
        // MAP SELECT
        // =====================================================================

        private void BuildMapSelect()
        {
            _mapSelectPanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "MapSelectPanel", UITheme.BgDark);

            UIFactory.CreateCompactButton(_mapSelectPanel.transform, "BackBtn", "< Back to Menu",
                UITheme.BgLight, new Vector2(0f, 1f), new Vector2(48f, -36f), new Vector2(200f, 42f),
                () => { HidePanel(_mapSelectPanel); ShowPanel(_mainMenuPanel); });

            UIFactory.CreateTextAt(_mapSelectPanel.transform, "Title", "Select Level",
                UITheme.FontTitle + 8, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(52f, -100f), new Vector2(440f, 52f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(_mapSelectPanel.transform, "Desc",
                "Level 1 is playable in this prototype. Levels 2 to 4 are marked COMING SOON for next deliverables.",
                UITheme.FontBody, UITheme.TextSecondary,
                new Vector2(0f, 1f), new Vector2(54f, -158f), new Vector2(720f, 48f),
                TextAnchor.UpperLeft);

            _mapSelectProgressText = UIFactory.CreateTextAt(_mapSelectPanel.transform, "Progress", "",
                UITheme.FontBody, UITheme.AccentGold,
                new Vector2(0f, 1f), new Vector2(54f, -214f), new Vector2(500f, 24f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            // Level cards in a 2x2 grid
            _campaignCards.Clear();
            CreateLevelCard(1, new Vector2(0.26f, 0.56f));
            CreateLevelCard(2, new Vector2(0.74f, 0.56f));
            CreateLevelCard(3, new Vector2(0.26f, 0.20f));
            CreateLevelCard(4, new Vector2(0.74f, 0.20f));
        }

        private void CreateLevelCard(int level, Vector2 anchor)
        {
            int idx = level - 1;
            Color accent = UITheme.LevelAccents[idx];

            var card = UIFactory.CreateCard(_mapSelectPanel.transform, $"Card_{level}",
                anchor, new Vector2(500f, 290f), UITheme.BgMedium);

            var btn = card.AddComponent<Button>();
            btn.targetGraphic = card.GetComponent<Image>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.9f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            int lv = level;
            btn.onClick.AddListener(() => StartCampaignAtLevel(lv));

            // Preview area
            var prevGo = UIFactory.CreateRegion(card.transform, "Preview",
                new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                UITheme.Darken(accent, 0.45f), new Vector2(10f, 10f));
            var prevImg = prevGo.GetComponent<Image>();
            var sprite = LoadMenuPreviewSprite(levelPreviewKeys[idx]);
            if (sprite != null) { prevImg.sprite = sprite; prevImg.color = Color.white; prevImg.preserveAspect = true; }

            // Lock overlay
            var lockGo = UIFactory.CreateRegion(prevGo.transform, "Lock",
                Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.08f));
            var lockImg = lockGo.GetComponent<Image>();

            // Bottom info
            UIFactory.CreateTextAt(card.transform, "Eyebrow", $"LEVEL {level:00}",
                UITheme.FontCaption, accent,
                new Vector2(0f, 1f), new Vector2(16f, -142f), new Vector2(160f, 16f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(card.transform, "Title", levelMapNames[idx],
                UITheme.FontHeading, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(16f, -162f), new Vector2(340f, 30f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(card.transform, "Stages", levelStageLabels[idx],
                UITheme.FontCaption, accent,
                new Vector2(1f, 1f), new Vector2(-150f, -166f), new Vector2(140f, 20f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(card.transform, "Teaser", levelTeasers[idx],
                UITheme.FontCaption, UITheme.TextSecondary,
                new Vector2(0f, 1f), new Vector2(16f, -198f), new Vector2(468f, 36f),
                TextAnchor.UpperLeft);

            // Status badge
            var statusGo = UIFactory.CreateCard(card.transform, "StatusBadge",
                new Vector2(1f, 0f), new Vector2(120f, 30f), UITheme.BgLight);
            statusGo.GetComponent<RectTransform>().pivot = new Vector2(1f, 0f);
            statusGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-12f, 12f);

            var statusTxt = UIFactory.CreateText(statusGo.transform, "Status", "",
                UITheme.FontSmall, UITheme.TextPrimary, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(statusTxt.GetComponent<RectTransform>());

            var actionTxt = UIFactory.CreateTextAt(card.transform, "Action", "",
                UITheme.FontCaption, UITheme.TextPrimary,
                new Vector2(0f, 0f), new Vector2(16f, 14f), new Vector2(160f, 18f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            _campaignCards.Add(new CampaignCardBinding
            {
                Level = level,
                Button = btn,
                StatusText = statusTxt,
                ActionText = actionTxt,
                PreviewImage = prevImg,
                LockOverlay = lockImg,
                FallbackColor = UITheme.Darken(accent, 0.45f)
            });
        }

        // =====================================================================
        // PAUSE MENU
        // =====================================================================

        private void BuildPauseMenu()
        {
            _pausePanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "PausePanel", UITheme.BgOverlay);

            var card = UIFactory.CreateCard(_pausePanel.transform, "PauseCard",
                new Vector2(0.5f, 0.5f), new Vector2(520f, 580f), UITheme.BgDark);

            UIFactory.CreateTextAt(card.transform, "Title", "PAUSED",
                UITheme.FontHero, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(300f, 54f),
                TextAnchor.UpperLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(card.transform, "Desc",
                "Keep your run, reset it, or return to the campaign.",
                UITheme.FontBody, UITheme.TextSecondary,
                new Vector2(0f, 1f), new Vector2(38f, -100f), new Vector2(440f, 48f),
                TextAnchor.UpperLeft);

            float y = -168f;
            UIFactory.CreateActionButton(card.transform, "ResumeBtn", "Resume",
                "Return to the active operation.", UITheme.AccentGreen,
                new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(456f, 76f), OnResume);
            y -= 86f;
            UIFactory.CreateActionButton(card.transform, "GuideBtn", "Guide",
                "Review controls and survival systems.", UITheme.AccentBlue,
                new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(456f, 76f), OpenGuideFromButton);
            y -= 86f;
            UIFactory.CreateActionButton(card.transform, "RestartBtn", "Restart Run",
                "Reset from the beginning.", UITheme.AccentOrange,
                new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(456f, 76f), RestartGame);
            y -= 86f;
            UIFactory.CreateActionButton(card.transform, "MenuBtn", "Main Menu",
                "Return to the campaign landing.", UITheme.BgLight,
                new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(456f, 76f), GoToMainMenu);

            UIFactory.CreateCompactButton(card.transform, "QuitBtn", "Quit Game",
                UITheme.AccentRed, new Vector2(0f, 0f), new Vector2(32f, 28f),
                new Vector2(456f, 46f), QuitGame);
        }

        // =====================================================================
        // GUIDE PANEL
        // =====================================================================

        private void BuildGuidePanel()
        {
            _guidePanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "GuidePanel",
                new Color(0.02f, 0.03f, 0.05f, 0.96f));

            UIFactory.CreateTextAt(_guidePanel.transform, "Title", "SURVIVAL GUIDE",
                UITheme.FontTitle + 6, UITheme.TextPrimary,
                new Vector2(0.5f, 1f), new Vector2(-320f, -28f), new Vector2(640f, 54f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.CreateTextAt(_guidePanel.transform, "Subtitle",
                "Controls, loop rules, and item systems",
                UITheme.FontBody - 1, UITheme.WithAlpha(UITheme.TextSecondary, 0.95f),
                new Vector2(0.5f, 1f), new Vector2(-330f, -86f), new Vector2(660f, 26f),
                TextAnchor.MiddleCenter);

            // Three-column layout
            BuildGuideSection("ControlsCol", "CONTROLS", GameplayGuideContent.GetControlsText(),
                new Vector2(0.035f, 0.13f), new Vector2(0.34f, 0.86f), UITheme.AccentBlue);
            BuildGuideSection("LoopCol", "SURVIVAL LOOP",
                GameplayGuideContent.GetRulesText() + "\n\n" + GameplayGuideContent.GetSystemsText(),
                new Vector2(0.35f, 0.13f), new Vector2(0.665f, 0.86f), UITheme.AccentGold);
            BuildGuideSection("ItemsCol", "ITEMS & ONBOARDING",
                GameplayGuideContent.GetItemsText() + "\n\n" + GameplayGuideContent.GetAccessibilityNote(),
                new Vector2(0.675f, 0.13f), new Vector2(0.965f, 0.86f), UITheme.AccentGreen);

            UIFactory.CreateTextAt(_guidePanel.transform, "Footer",
                "Press H, F1, or Esc to close",
                UITheme.FontCaption + 1, UITheme.WithAlpha(UITheme.TextMuted, 0.95f),
                new Vector2(0.5f, 0f), new Vector2(-220f, 72f), new Vector2(440f, 22f),
                TextAnchor.MiddleCenter);

            UIFactory.CreateCenteredButton(_guidePanel.transform, "CloseBtn", "CLOSE",
                UITheme.AccentBlue, new Vector2(0.5f, 0.06f), new Vector2(220f, 46f), CloseGuide);
        }

        private void BuildGuideSection(string name, string title, string body,
            Vector2 anchorMin, Vector2 anchorMax, Color accentColor)
        {
            var section = UIFactory.CreateRegion(_guidePanel.transform, name,
                anchorMin, anchorMax, UITheme.Darken(UITheme.BgMedium, 0.10f), new Vector2(10f, 10f));

            var accent = new GameObject("Accent");
            accent.transform.SetParent(section.transform, false);
            var accentRt = accent.AddComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(0f, 4f);
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = UITheme.WithAlpha(accentColor, 0.95f);
            accentImg.raycastTarget = false;

            var titleTxt = UIFactory.CreateText(section.transform, "Title", title,
                UITheme.FontBody + 4, UITheme.WithAlpha(UITheme.TextPrimary, 0.98f), TextAnchor.MiddleLeft, FontStyle.Bold);
            var titleRt = titleTxt.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16f, -56f);
            titleRt.offsetMax = new Vector2(-16f, -12f);

            var bodyTxt = UIFactory.CreateText(section.transform, "Body", body,
                UITheme.FontBody + 1, UITheme.WithAlpha(UITheme.TextPrimary, 0.98f), TextAnchor.UpperLeft);
            var bodyRt = bodyTxt.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(18f, 18f);
            bodyRt.offsetMax = new Vector2(-18f, -68f);
            bodyTxt.lineSpacing = 1.2f;
            bodyTxt.supportRichText = true;
        }

        // =====================================================================
        // DAWN SHOP
        // =====================================================================

        private void BuildDawnShop()
        {
            _dawnShopPanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "DawnShopPanel",
                new Color(0.04f, 0.04f, 0.07f, 0.97f));

            // Header
            var header = UIFactory.CreateRegion(_dawnShopPanel.transform, "Header",
                new Vector2(0f, 0.89f), new Vector2(1f, 1f),
                UITheme.Darken(UITheme.BgDark, 0.2f));

            _shopTitleText = UIFactory.CreateTextAt(header.transform, "Title",
                "DAWN - Level 1 Cleared!", UITheme.FontHeading + 2, UITheme.AccentGold,
                new Vector2(0.5f, 0.7f), new Vector2(-280f, -6f), new Vector2(560f, 34f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            _shopSummaryText = UIFactory.CreateTextAt(header.transform, "Summary", "",
                UITheme.FontCaption, UITheme.TextMuted,
                new Vector2(0.5f, 0.3f), new Vector2(-260f, -4f), new Vector2(520f, 18f),
                TextAnchor.MiddleCenter);

            // Points display
            _shopPointsText = UIFactory.CreateTextAt(_dawnShopPanel.transform, "Points",
                "Points: 0", UITheme.FontHeading - 2, UITheme.AccentGreen,
                new Vector2(0.5f, 0.84f), new Vector2(-110f, -4f), new Vector2(220f, 28f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            // Supply row
            var supplyRow = new GameObject("SupplyRow");
            supplyRow.transform.SetParent(_dawnShopPanel.transform, false);
            var srRt = supplyRow.AddComponent<RectTransform>();
            UIFactory.SetAnchored(srRt, new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(900f, 36f));

            var healBtn = UIFactory.CreateCenteredButton(supplyRow.transform, "HealBtn",
                $"Health Kit +1 ({HealCost})", UITheme.AccentRed,
                new Vector2(0.125f, 0.5f), new Vector2(200f, 32f), BuyHealthKit);
            _shopBuyButtons.Add(healBtn);

            var ammoBtn = UIFactory.CreateCenteredButton(supplyRow.transform, "AmmoBtn",
                $"Ammo Refill ({AmmoRefillCost})", UITheme.AccentOrange,
                new Vector2(0.375f, 0.5f), new Vector2(200f, 32f), BuyAmmoRefill);
            _shopBuyButtons.Add(ammoBtn);

            var grenadeBtn = UIFactory.CreateCenteredButton(supplyRow.transform, "GrenadeBtn",
                $"Grenade +1 ({GrenadeRefillCost})", UITheme.AccentGreen,
                new Vector2(0.625f, 0.5f), new Vector2(200f, 32f), BuyGrenadeRefill);
            _shopBuyButtons.Add(grenadeBtn);

            var molotovBtn = UIFactory.CreateCenteredButton(supplyRow.transform, "MolotovBtn",
                $"Molotov +1 ({MolotovRefillCost})", UITheme.Darken(UITheme.AccentOrange, 0.18f),
                new Vector2(0.875f, 0.5f), new Vector2(200f, 32f), BuyMolotovRefill);
            _shopBuyButtons.Add(molotovBtn);

            // Tabs
            var tabRow = new GameObject("TabRow");
            tabRow.transform.SetParent(_dawnShopPanel.transform, false);
            var trRt = tabRow.AddComponent<RectTransform>();
            UIFactory.SetAnchored(trRt, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(560f, 34f));

            _weaponsTabBtn = UIFactory.CreateCenteredButton(tabRow.transform, "WeaponsTab",
                "WEAPONS", UITheme.AccentBlue,
                new Vector2(0.18f, 0.5f), new Vector2(160f, 32f), () => ShowShopTab(0));
            _upgradesTabBtn = UIFactory.CreateCenteredButton(tabRow.transform, "UpgradesTab",
                "UPGRADES", UITheme.AccentPurple,
                new Vector2(0.5f, 0.5f), new Vector2(160f, 32f), () => ShowShopTab(1));
            _armorTabBtn = UIFactory.CreateCenteredButton(tabRow.transform, "ArmorTab",
                "ARMOR", UITheme.Darken(UITheme.AccentBlue, 0.15f),
                new Vector2(0.82f, 0.5f), new Vector2(160f, 32f), () => ShowShopTab(2));

            // Tab content containers
            _weaponsTabContent = CreateTabContainer("WeaponsContent");
            _upgradesTabContent = CreateTabContainer("UpgradesContent");
            _armorTabContent = CreateTabContainer("ArmorContent");

            BuildWeaponItems();
            BuildUpgradeItems();
            BuildArmorItems();

            _upgradesTabContent.SetActive(false);
            _armorTabContent.SetActive(false);

            // Continue button
            UIFactory.CreateCenteredButton(_dawnShopPanel.transform, "ContinueBtn",
                "CONTINUE", UITheme.AccentBlue,
                new Vector2(0.5f, 0.06f), new Vector2(280f, 46f), OnContinueToNextNight);
        }

        private GameObject CreateTabContainer(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_dawnShopPanel.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.14f);
            rt.anchorMax = new Vector2(0.5f, 0.66f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(580f, 0f);
            return go;
        }

        private void BuildWeaponItems()
        {
            float y = 0f;
            const int h = 56;
            AddWeaponShopItem("Shotgun", "Close-range spread", 100, 1, WeaponType.Shotgun, ref y, h);
            AddWeaponShopItem("SMG", "Fast fire rate", 150, 2, WeaponType.SMG, ref y, h);
            AddWeaponShopItem("Sniper Rifle", "High damage, long range", 250, 2, WeaponType.SniperRifle, ref y, h);
            AddWeaponShopItem("Assault Rifle", "Balanced auto", 200, 3, WeaponType.AssaultRifle, ref y, h);
            AddWeaponShopItem("Grenade Launcher", "Area damage", 350, 3, WeaponType.GrenadeLauncher, ref y, h);
            AddWeaponShopItem("Flamethrower", "Burn DoT", 400, 4, WeaponType.Flamethrower, ref y, h);
        }

        private void AddWeaponShopItem(string name, string desc, int cost, int unlockNight,
            WeaponType wt, ref float y, int height)
        {
            var root = new GameObject($"Weapon_{name}");
            root.transform.SetParent(_weaponsTabContent.transform, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(560f, height - 4);

            root.AddComponent<Image>().color = UITheme.BgMedium;

            UIFactory.CreateTextAt(root.transform, "Name", name,
                UITheme.FontBody, UITheme.TextPrimary,
                new Vector2(0f, 0.5f), new Vector2(14f, 8f), new Vector2(260f, 24f),
                TextAnchor.MiddleLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(root.transform, "Desc", $"{desc}  ·  {cost} pts",
                UITheme.FontSmall + 1, UITheme.TextMuted,
                new Vector2(0f, 0.5f), new Vector2(14f, -12f), new Vector2(320f, 16f),
                TextAnchor.MiddleLeft);

            var weaponType = wt;
            var buyBtn = UIFactory.CreateCenteredButton(root.transform, "Buy", "BUY",
                UITheme.AccentGreen, new Vector2(1f, 0.5f), new Vector2(76f, 30f),
                () => BuyWeapon(weaponType, cost, unlockNight));
            buyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-48f, 0f);

            _shopBuyButtons.Add(buyBtn);
            y -= height;
        }

        private void BuildUpgradeItems()
        {
            float y = 0f;
            const int h = 58;
            AddUpgradeItem("Damage", ref y, h, () => { if (PlayerUpgrades.Instance?.TryUpgradeDamage() == true) RefreshShop(); });
            AddUpgradeItem("Fire Rate", ref y, h, () => { if (PlayerUpgrades.Instance?.TryUpgradeFireRate() == true) RefreshShop(); });
            AddUpgradeItem("Magazine", ref y, h, () => { if (PlayerUpgrades.Instance?.TryUpgradeMagazine() == true) RefreshShop(); });
            AddUpgradeItem("Max Health", ref y, h, () => { if (PlayerUpgrades.Instance?.TryUpgradeHealth() == true) RefreshShop(); });
            AddUpgradeItem("Sprint Speed", ref y, h, () => { if (PlayerUpgrades.Instance?.TryUpgradeSprint() == true) RefreshShop(); });
        }

        private void AddUpgradeItem(string name, ref float y, int height, System.Action onBuy)
        {
            var root = new GameObject($"Upgrade_{name}");
            root.transform.SetParent(_upgradesTabContent.transform, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(560f, height - 4);

            root.AddComponent<Image>().color = new Color(UITheme.BgMedium.r + 0.02f,
                UITheme.BgMedium.g, UITheme.BgMedium.b + 0.04f, 0.9f);

            var label = UIFactory.CreateTextAt(root.transform, "Label", name,
                UITheme.FontBody - 1, UITheme.TextPrimary,
                new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(396f, 30f),
                TextAnchor.MiddleLeft, FontStyle.Bold);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var labelOutline = label.gameObject.AddComponent<Outline>();
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            labelOutline.effectDistance = new Vector2(1f, -1f);
            _upgradeLabels.Add(label);

            var buyBtn = UIFactory.CreateCenteredButton(root.transform, "Buy", "UPGRADE",
                UITheme.AccentPurple, new Vector2(1f, 0.5f), new Vector2(90f, 30f), onBuy);
            buyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-56f, 0f);

            _upgradeBuyButtons.Add(buyBtn);
            y -= height;
        }

        private void BuildArmorItems()
        {
            float y = 0f;
            const int h = 56;
            AddArmorItem("Vest Lv1", "Light body armor", 80, ArmorTier.Level1, false, ref y, h);
            AddArmorItem("Vest Lv2", "Heavy body armor", 180, ArmorTier.Level2, false, ref y, h);
            AddArmorItem("Helmet Lv1", "Basic protection", 60, ArmorTier.Level1, true, ref y, h);
            AddArmorItem("Helmet Lv2", "Reinforced helmet", 140, ArmorTier.Level2, true, ref y, h);
        }

        private void AddArmorItem(string name, string desc, int cost,
            ArmorTier tier, bool isHelmet, ref float y, int height)
        {
            var root = new GameObject($"Armor_{name}");
            root.transform.SetParent(_armorTabContent.transform, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(560f, height - 4);

            root.AddComponent<Image>().color = new Color(UITheme.BgMedium.r,
                UITheme.BgMedium.g + 0.02f, UITheme.BgMedium.b + 0.04f, 0.9f);

            UIFactory.CreateTextAt(root.transform, "Name", name,
                UITheme.FontBody, UITheme.TextPrimary,
                new Vector2(0f, 0.5f), new Vector2(14f, 8f), new Vector2(260f, 24f),
                TextAnchor.MiddleLeft, FontStyle.Bold);

            UIFactory.CreateTextAt(root.transform, "Desc", $"{desc}  ·  {cost} pts",
                UITheme.FontSmall + 1, UITheme.TextMuted,
                new Vector2(0f, 0.5f), new Vector2(14f, -12f), new Vector2(320f, 16f),
                TextAnchor.MiddleLeft);

            var t = tier;
            var h = isHelmet;
            var c = cost;
            var buyBtn = UIFactory.CreateCenteredButton(root.transform, "Buy", "BUY",
                UITheme.AccentBlue, new Vector2(1f, 0.5f), new Vector2(76f, 30f),
                () => BuyArmor(t, h, c));
            buyBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-48f, 0f);

            _shopBuyButtons.Add(buyBtn);
            y -= height;
        }

        // =====================================================================
        // LEVEL COMPLETE / GAME OVER / VICTORY
        // =====================================================================

        private void BuildLevelCompleteScreen()
        {
            _levelCompletePanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "LevelCompletePanel", UITheme.BgOverlay);

            var title = UIFactory.CreateTextAt(_levelCompletePanel.transform, "Title", "LEVEL COMPLETE!",
                UITheme.FontHero, UITheme.AccentGold,
                new Vector2(0.5f, 0.75f), new Vector2(-280f, -10f), new Vector2(560f, 64f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<Shadow>().effectColor = UITheme.WithAlpha(UITheme.AccentGold, 0.25f);

            _levelCompleteStatsText = UIFactory.CreateTextAt(_levelCompletePanel.transform, "Stats", "",
                UITheme.FontBody + 2, UITheme.TextPrimary,
                new Vector2(0.5f, 0.48f), new Vector2(-240f, 80f), new Vector2(480f, 180f),
                TextAnchor.MiddleCenter);

            UIFactory.CreateCenteredButton(_levelCompletePanel.transform, "NextBtn", "NEXT LEVEL",
                UITheme.AccentGreen, new Vector2(0.4f, 0.18f), new Vector2(220f, 50f), OnNextLevel);

            UIFactory.CreateCenteredButton(_levelCompletePanel.transform, "MenuBtn", "MAIN MENU",
                UITheme.BgLight, new Vector2(0.6f, 0.18f), new Vector2(180f, 46f), GoToMainMenu);
        }

        private void BuildGameOverScreen()
        {
            _gameOverPanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "GameOverPanel", UITheme.BgOverlay);

            var title = UIFactory.CreateTextAt(_gameOverPanel.transform, "Title", "YOU DIED",
                UITheme.FontHero + 4, UITheme.AccentRed,
                new Vector2(0.5f, 0.76f), new Vector2(-240f, -10f), new Vector2(480f, 72f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<Shadow>().effectColor = UITheme.WithAlpha(UITheme.AccentRed, 0.3f);

            var statsText = UIFactory.CreateTextAt(_gameOverPanel.transform, "Stats", "",
                UITheme.FontBody + 2, UITheme.TextPrimary,
                new Vector2(0.5f, 0.52f), new Vector2(-240f, 80f), new Vector2(480f, 200f),
                TextAnchor.MiddleCenter);

            UIFactory.CreateCenteredButton(_gameOverPanel.transform, "RestartBtn", "Restart",
                UITheme.AccentGreen, new Vector2(0.4f, 0.20f), new Vector2(170f, 46f), RestartGame);

            UIFactory.CreateCenteredButton(_gameOverPanel.transform, "MenuBtn", "Main Menu",
                UITheme.BgLight, new Vector2(0.6f, 0.20f), new Vector2(170f, 46f), GoToMainMenu);

            _gameOverPanel.AddComponent<GameOverPanelHelper>().Initialize(statsText);
        }

        private void BuildVictoryScreen()
        {
            _victoryPanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "VictoryPanel", UITheme.BgOverlay);

            var title = UIFactory.CreateTextAt(_victoryPanel.transform, "Title", "SUBJECT 23 CONTAINED",
                UITheme.FontTitle + 10, UITheme.AccentGold,
                new Vector2(0.5f, 0.76f), new Vector2(-380f, -10f), new Vector2(760f, 72f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            title.gameObject.AddComponent<Shadow>().effectColor = UITheme.WithAlpha(UITheme.AccentGold, 0.25f);

            var statsText = UIFactory.CreateTextAt(_victoryPanel.transform, "Stats", "",
                UITheme.FontBody + 2, UITheme.TextPrimary,
                new Vector2(0.5f, 0.52f), new Vector2(-240f, 80f), new Vector2(480f, 200f),
                TextAnchor.MiddleCenter);

            UIFactory.CreateCenteredButton(_victoryPanel.transform, "RestartBtn", "Restart",
                UITheme.AccentGreen, new Vector2(0.4f, 0.20f), new Vector2(170f, 46f), RestartGame);

            UIFactory.CreateCenteredButton(_victoryPanel.transform, "MenuBtn", "Main Menu",
                UITheme.BgLight, new Vector2(0.6f, 0.20f), new Vector2(170f, 46f), GoToMainMenu);

            _victoryPanel.AddComponent<VictoryPanelHelper>().Initialize(statsText);
        }

        // =====================================================================
        // LEADERBOARD
        // =====================================================================

        private void BuildLeaderboardPanel()
        {
            _leaderboardPanel = UIFactory.CreateFullPanel(_canvasRoot.transform, "LeaderboardPanel",
                new Color(0.04f, 0.04f, 0.08f, 0.96f));

            UIFactory.CreateTextAt(_leaderboardPanel.transform, "Title", "LEADERBOARD",
                UITheme.FontTitle + 2, UITheme.AccentGold,
                new Vector2(0.5f, 0.93f), new Vector2(-220f, -10f), new Vector2(440f, 48f),
                TextAnchor.MiddleCenter, FontStyle.Bold);

            UIFactory.CreateTextAt(_leaderboardPanel.transform, "Header",
                "RANK    SCORE    LEVELS    KILLS    MAP",
                UITheme.FontCaption + 1, UITheme.TextMuted,
                new Vector2(0.5f, 0.85f), new Vector2(-360f, 0f), new Vector2(720f, 22f),
                TextAnchor.MiddleCenter);

            for (int i = 0; i < 10; i++)
            {
                float yPos = 0.79f - i * 0.065f;
                UIFactory.CreateTextAt(_leaderboardPanel.transform, $"Entry_{i}", "",
                    UITheme.FontBody - 1, UITheme.TextPrimary,
                    new Vector2(0.5f, yPos), new Vector2(-380f, 0f), new Vector2(760f, 24f),
                    TextAnchor.MiddleCenter);
            }

            UIFactory.CreateCenteredButton(_leaderboardPanel.transform, "BackBtn", "BACK",
                UITheme.BgLight, new Vector2(0.5f, 0.06f), new Vector2(180f, 42f), HideLeaderboard);
        }

        // =====================================================================
        // PUBLIC API / STATIC HELPERS
        // =====================================================================

        internal static string GetMapDisplayName(MapType mapType)
        {
            return mapType switch
            {
                MapType.TownCenter => levelMapNames[0],
                MapType.Suburban => levelMapNames[1],
                MapType.Industrial => levelMapNames[2],
                MapType.Research => levelMapNames[3],
                _ => mapType.ToString()
            };
        }

        // =====================================================================
        // CAMPAIGN LOGIC
        // =====================================================================

        private void StartCampaign()
        {
            Time.timeScale = 1f;
            HideAllPanelsImmediate();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.StartCampaignFromLevel(GetHighestUnlockedLevel());
        }

        private void StartCampaignAtLevel(int level)
        {
            if (level > MaxSelectableLevel) return;

            Time.timeScale = 1f;
            HideAllPanelsImmediate();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.StartCampaignFromLevel(level);
        }

        private void ShowCampaignMap()
        {
            RefreshCampaignPresentation();
            HideAllPanelsFade();
            ShowPanel(_mapSelectPanel);
        }

        private void RefreshCampaignPresentation()
        {
            int highest = GetHighestUnlockedLevel();
            highest = Mathf.Clamp(highest, 1, levelMapNames.Length);

            if (_mainMenuProgressText != null)
                _mainMenuProgressText.text = $"Level {highest:00} ready: {levelObjectiveSummaries[highest - 1]}";

            if (_mapSelectProgressText != null)
                _mapSelectProgressText.text = $"Unlocked through Level {highest:00}. Levels 2 to 4 are COMING SOON (next deliverables).";

            foreach (var row in _campaignRouteRows)
            {
                bool upcoming = row.Level > MaxSelectableLevel;
                bool unlocked = !upcoming && IsLevelUnlocked(row.Level);
                bool ready = unlocked && row.Level == highest;
                row.StatusText.text = ready ? "READY" : unlocked ? "UNLOCKED" : upcoming ? "UPCOMING" : "LOCKED";
                row.StatusBackground.color = ready
                    ? UITheme.Darken(UITheme.AccentGreen, 0.35f)
                    : unlocked ? UITheme.BgLight : upcoming ? UITheme.Darken(UITheme.AccentBlue, 0.55f) : UITheme.Darken(UITheme.AccentRed, 0.5f);
            }

            foreach (var card in _campaignCards)
            {
                bool upcoming = card.Level > MaxSelectableLevel;
                bool unlocked = !upcoming && IsLevelUnlocked(card.Level);
                bool ready = unlocked && card.Level == highest;

                if (card.Button != null) card.Button.interactable = unlocked;
                if (card.StatusText != null)
                {
                    card.StatusText.text = ready ? "READY" : unlocked ? "UNLOCKED" : upcoming ? "UPCOMING" : "LOCKED";
                    card.StatusText.color = unlocked ? UITheme.TextPrimary : UITheme.TextMuted;
                }
                if (card.ActionText != null)
                {
                    card.ActionText.text = unlocked ? "DEPLOY" : upcoming ? "COMING SOON" : "LOCKED";
                    card.ActionText.color = unlocked ? UITheme.TextPrimary : UITheme.TextMuted;
                }
                if (card.PreviewImage != null)
                {
                    card.PreviewImage.color = card.PreviewImage.sprite != null
                        ? (unlocked ? Color.white : new Color(0.5f, 0.5f, 0.55f))
                        : (unlocked ? card.FallbackColor : UITheme.Darken(card.FallbackColor, 0.3f));
                }
                if (card.LockOverlay != null)
                    card.LockOverlay.color = unlocked ? Color.clear : new Color(0f, 0f, 0f, 0.3f);
            }
        }

        private int GetHighestUnlockedLevel()
        {
            int h = 1;
            for (int i = 1; i <= Mathf.Min(levelMapNames.Length, MaxSelectableLevel); i++)
                if (IsLevelUnlocked(i)) h = i;
            return h;
        }

        private bool IsLevelUnlocked(int level)
        {
            return GameManager.Instance != null ? GameManager.Instance.IsLevelUnlocked(level) : level <= 1;
        }

        // =====================================================================
        // PAUSE / GUIDE
        // =====================================================================

        private void OpenGuideFromButton()
        {
            bool pause = GameManager.Instance != null && GameManager.Instance.IsGameplayState && !GameManager.Instance.IsPaused;
            OpenGuide(pause);
        }

        private void OpenGuide(bool pauseGameplay)
        {
            if (_guidePanel == null) return;
            if (pauseGameplay && GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(true);
                _resumeGameplayOnGuideClose = true;
            }
            else _resumeGameplayOnGuideClose = false;

            ShowPanel(_guidePanel);
        }

        private void CloseGuide()
        {
            HidePanel(_guidePanel);
            bool resume = _resumeGameplayOnGuideClose;
            _resumeGameplayOnGuideClose = false;
            if (resume && GameManager.Instance != null && GameManager.Instance.IsPaused)
                GameManager.Instance.SetPaused(false);
        }

        private void OnResume() { GameManager.Instance?.SetPaused(false); }

        private void OnPauseChanged(bool paused)
        {
            if (paused && GameManager.Instance != null && GameManager.Instance.IsGameplayState)
                ShowPanel(_pausePanel);
            else
            {
                HidePanel(_pausePanel);
                if (_resumeGameplayOnGuideClose) { HidePanel(_guidePanel); _resumeGameplayOnGuideClose = false; }
            }
        }

        private void QuitGame() { GameManager.Instance?.QuitGame(); }

        // =====================================================================
        // SHOP LOGIC
        // =====================================================================

        private void ShowShopTab(int idx)
        {
            if (_weaponsTabContent != null) _weaponsTabContent.SetActive(idx == 0);
            if (_upgradesTabContent != null) _upgradesTabContent.SetActive(idx == 1);
            if (_armorTabContent != null) _armorTabContent.SetActive(idx == 2);

            SetTabActive(_weaponsTabBtn, idx == 0, UITheme.AccentBlue);
            SetTabActive(_upgradesTabBtn, idx == 1, UITheme.AccentPurple);
            SetTabActive(_armorTabBtn, idx == 2, UITheme.Darken(UITheme.AccentBlue, 0.15f));
        }

        private static void SetTabActive(Button tab, bool active, Color accent)
        {
            if (tab == null) return;
            var c = tab.colors;
            c.normalColor = active ? accent : UITheme.BgLight;
            tab.colors = c;
        }

        private void BuyWeapon(WeaponType wt, int cost, int unlockNight)
        {
            if (_purchasedWeapons.Contains(wt)) return;
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(cost)) return;
            int level = GameManager.Instance?.CurrentLevel ?? 1;
            if (level < unlockNight) return;
            if (!PointsSystem.Instance.SpendPoints(cost, $"Weapon: {wt}")) return;

            _purchasedWeapons.Add(wt);

            WeaponData weapon = wt switch
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
                player?.GetComponent<PlayerShooting>()?.SetSecondWeapon(weapon);
            }

            RefreshShop();
        }

        private void BuyHealthKit()
        {
            if (!CanPurchaseHealthKit(out var medkits)) return;
            if (!PointsSystem.Instance.SpendPoints(HealCost, "Health Kit")) return;
            medkits.AddMedkits(1);
            RefreshShop();
        }

        private void BuyAmmoRefill()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(AmmoRefillCost)) return;
            if (!PointsSystem.Instance.SpendPoints(AmmoRefillCost, "Ammo Refill")) return;
            GameObject.Find("Player")?.GetComponent<PlayerShooting>()?.AddAmmo(60);
            RefreshShop();
        }

        private void BuyGrenadeRefill()
        {
            if (!CanPurchaseGrenadeRefill(out var throwable)) return;
            if (!PointsSystem.Instance.SpendPoints(GrenadeRefillCost, "Grenade Refill")) return;
            throwable.AddGrenades(1);
            RefreshShop();
        }

        private void BuyMolotovRefill()
        {
            if (!CanPurchaseMolotovRefill(out var throwable)) return;
            if (!PointsSystem.Instance.SpendPoints(MolotovRefillCost, "Molotov Refill")) return;
            throwable.AddMolotovs(1);
            RefreshShop();
        }

        private void BuyArmor(ArmorTier tier, bool isHelmet, int cost)
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(cost)) return;
            if (PlayerArmor.Instance == null) return;

            bool worth = isHelmet
                ? (tier > PlayerArmor.Instance.HelmetTier || PlayerArmor.Instance.HelmetDurability <= 0)
                : (tier > PlayerArmor.Instance.VestTier || PlayerArmor.Instance.VestDurability <= 0);
            if (!worth) return;

            if (!PointsSystem.Instance.SpendPoints(cost, isHelmet ? $"Lv{(int)tier} Helmet" : $"Lv{(int)tier} Vest")) return;

            if (isHelmet) PlayerArmor.Instance.EquipHelmet(tier);
            else PlayerArmor.Instance.EquipVest(tier);

            RefreshShop();
        }

        private void RefreshShop() => UpdateShopDisplay();

        private void UpdateShopDisplay()
        {
            if (_shopPointsText != null && PointsSystem.Instance != null)
                _shopPointsText.text = $"Points: {PointsSystem.Instance.CurrentPoints}";

            if (_shopTitleText != null && GameManager.Instance != null)
                _shopTitleText.text = $"DAWN - Level {GameManager.Instance.CurrentLevel}, Night {GameManager.Instance.NightWithinLevel} Cleared!";

            if (_shopSummaryText != null && PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                string summary = $"Kills: {stats.enemiesKilled}  |  Earned: {stats.totalEarned}";

                if (GameManager.Instance != null && GameManager.Instance.WillRetryCurrentStepOnAdvance)
                {
                    summary += "\nObjective missed: next deploy retries this step.";
                }
                else if (GameManager.Instance != null && GameManager.Instance.PendingObjectiveCarryoverPenaltyStacks > 0)
                {
                    summary += "\nPenalty queued: stronger next-night enemies and reduced carryover.";
                }

                _shopSummaryText.text = summary;
            }

            int night = GameManager.Instance?.CurrentLevel ?? 1;
            bool needsHeal = NeedsMedkits(out var medkits);
            bool needsGrenade = NeedsGrenades(out _);
            bool needsMolotov = NeedsMolotovs(out _);
            bool canHeal = needsHeal && PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(HealCost);
            UpdateSupplyButton(0, HealCost, canHeal);
            UpdateSupplyButton(1, AmmoRefillCost);
            UpdateSupplyButton(2, GrenadeRefillCost, needsGrenade);
            UpdateSupplyButton(3, MolotovRefillCost, needsMolotov);

            if (_shopBuyButtons.Count > 0)
            {
                var lbl = _shopBuyButtons[0].GetComponentInChildren<Text>();
                if (lbl != null)
                {
                    lbl.text = needsHeal
                        ? $"Health Kit +1 ({HealCost})"
                        : (medkits != null ? "Medkits Full" : "No Medkits");
                }
            }

            if (_shopBuyButtons.Count > 2)
            {
                var lbl = _shopBuyButtons[2].GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = needsGrenade ? $"Grenade +1 ({GrenadeRefillCost})" : "Grenades Full";
            }

            if (_shopBuyButtons.Count > 3)
            {
                var lbl = _shopBuyButtons[3].GetComponentInChildren<Text>();
                if (lbl != null) lbl.text = needsMolotov ? $"Molotov +1 ({MolotovRefillCost})" : "Molotovs Full";
            }

            WeaponType[] wts = { WeaponType.Shotgun, WeaponType.SMG, WeaponType.SniperRifle, WeaponType.AssaultRifle, WeaponType.GrenadeLauncher, WeaponType.Flamethrower };
            int[] costs = { 100, 150, 250, 200, 350, 400 };
            int[] nights = { 1, 2, 2, 3, 3, 4 };
            for (int i = 0; i < wts.Length; i++)
            {
                int idx = SupplyButtonCount + i;
                if (idx >= _shopBuyButtons.Count) break;
                var b = _shopBuyButtons[idx];
                bool sold = _purchasedWeapons.Contains(wts[i]);
                bool afford = PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(costs[i]);
                bool unlocked = night >= nights[i];
                b.interactable = !sold && afford && unlocked;
                var lt = b.GetComponentInChildren<Text>();
                if (lt != null) lt.text = sold ? "SOLD" : (unlocked ? "BUY" : $"Lv{nights[i]}+");
            }

            var upgrades = PlayerUpgrades.Instance;
            if (upgrades != null)
            {
                UpdateUpgradeRow(0, upgrades.DamageTier, PlayerUpgrades.MaxDamageTier, upgrades.GetDamageCost(), upgrades.GetDamageDescription(), "Damage");
                UpdateUpgradeRow(1, upgrades.FireRateTier, PlayerUpgrades.MaxFireRateTier, upgrades.GetFireRateCost(), upgrades.GetFireRateDescription(), "Fire Rate");
                UpdateUpgradeRow(2, upgrades.MagazineTier, PlayerUpgrades.MaxMagazineTier, upgrades.GetMagazineCost(), upgrades.GetMagazineDescription(), "Magazine");
                UpdateUpgradeRow(3, upgrades.HealthTier, PlayerUpgrades.MaxHealthTier, upgrades.GetHealthCost(), upgrades.GetHealthDescription(), "Max Health");
                UpdateUpgradeRow(4, upgrades.SprintTier, PlayerUpgrades.MaxSprintTier, upgrades.GetSprintCost(), upgrades.GetSprintDescription(), "Sprint Speed");
            }
        }

        private bool CanPurchaseHealthKit(out PlayerMedkitSystem medkits)
        {
            if (!NeedsMedkits(out medkits)) return false;
            return PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(HealCost);
        }

        private bool NeedsMedkits(out PlayerMedkitSystem medkits)
        {
            var player = GameObject.Find("Player");
            medkits = player != null ? player.GetComponent<PlayerMedkitSystem>() : null;
            return medkits != null && medkits.HasCapacity();
        }

        private bool CanPurchaseGrenadeRefill(out ThrowableSystem throwable)
        {
            if (!NeedsGrenades(out throwable)) return false;
            return PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(GrenadeRefillCost);
        }

        private bool NeedsGrenades(out ThrowableSystem throwable)
        {
            var player = GameObject.Find("Player");
            throwable = player != null ? player.GetComponent<ThrowableSystem>() : null;
            return throwable != null && throwable.GrenadeCount < throwable.MaxGrenades;
        }

        private bool CanPurchaseMolotovRefill(out ThrowableSystem throwable)
        {
            if (!NeedsMolotovs(out throwable)) return false;
            return PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(MolotovRefillCost);
        }

        private bool NeedsMolotovs(out ThrowableSystem throwable)
        {
            var player = GameObject.Find("Player");
            throwable = player != null ? player.GetComponent<ThrowableSystem>() : null;
            return throwable != null && throwable.MolotovCount < throwable.MaxMolotovs;
        }

        private void UpdateSupplyButton(int index, int cost, bool extra = true)
        {
            if (index >= _shopBuyButtons.Count) return;
            _shopBuyButtons[index].interactable = extra && PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);
        }

        private void UpdateUpgradeRow(int index, int tier, int maxTier, int cost, string desc, string name)
        {
            if (index >= _upgradeLabels.Count || index >= _upgradeBuyButtons.Count) return;
            bool maxed = tier >= maxTier;
            _upgradeLabels[index].text = $"{name}  T{tier}/{maxTier}  {desc}{(maxed ? "" : $"  ({cost} pts)")}";
            _upgradeBuyButtons[index].interactable = !maxed && cost > 0 && PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);
            var lt = _upgradeBuyButtons[index].GetComponentInChildren<Text>();
            if (lt != null) lt.text = maxed ? "MAX" : "UPGRADE";
        }

        private void OnContinueToNextNight()
        {
            HidePanel(_dawnShopPanel);
            Time.timeScale = 1f;
            GameManager.Instance?.AdvanceToNextNight();
        }

        // =====================================================================
        // LEVEL COMPLETE / ENDGAME
        // =====================================================================

        private void OnNextLevel()
        {
            HidePanel(_levelCompletePanel);
            Time.timeScale = 1f;
            GameManager.Instance?.StartNextLevel();
        }

        private void ShowLevelComplete()
        {
            if (_levelCompleteStatsText != null && GameManager.Instance != null)
            {
                int level = GameManager.Instance.CurrentLevel;
                string map = GetMapDisplayName(GameManager.Instance.SelectedMap);
                int kills = 0, earned = 0;
                if (PointsSystem.Instance != null)
                {
                    var stats = PointsSystem.Instance.GetGameStats();
                    kills = stats.enemiesKilled;
                    earned = stats.totalEarned;
                }
                int next = Mathf.Min(level + 1, GameManager.TotalLevels);
                string nextMap = levelMapNames[Mathf.Clamp(next - 1, 0, levelMapNames.Length - 1)];
                string nextObj = next > level
                    ? levelObjectiveSummaries[Mathf.Clamp(next - 1, 0, levelObjectiveSummaries.Length - 1)]
                    : "Final containment cleared.";
                float carryRatio = Mathf.Clamp01(GameManager.Instance.GetProjectedCarryoverRatio());
                string carryText = carryRatio >= 0.999f
                    ? "Points carryover: 100%"
                    : $"Points carryover: {Mathf.RoundToInt(carryRatio * 100f)}%";
                if (GameManager.Instance.PendingObjectiveCarryoverPenaltyStacks > 0)
                {
                    carryText += " (objective penalty)";
                }

                _levelCompleteStatsText.text =
                    $"Level {level} - {map} cleared!\n\n" +
                    $"Enemies Killed: {kills}\nPoints Earned: {earned}\n\n" +
                    $"Next: Level {next} - {nextMap}\n{nextObj}\n{carryText}";
            }
            ShowPanel(_levelCompletePanel);
            Time.timeScale = 0f;
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            HideAllPanelsImmediate();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.RestartGame();
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            HideAllPanelsImmediate();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.ReturnToMainMenu();
        }

        // =====================================================================
        // LEADERBOARD
        // =====================================================================

        private void ShowLeaderboard()
        {
            HideAllPanelsFade();
            ShowPanel(_leaderboardPanel);
            RefreshLeaderboardDisplay();
        }

        private void HideLeaderboard()
        {
            HidePanel(_leaderboardPanel);
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
                ShowPanel(_mainMenuPanel);
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
                    string vm = e.victory ? " \u2605" : "";
                    entryText.text = $"#{i + 1}      {e.score}      {e.nightsReached}      {e.kills}      {e.map}{vm}";
                    entryText.color = e.victory ? UITheme.AccentGold : UITheme.TextPrimary;
                }
                else
                {
                    entryText.text = $"#{i + 1}      ---";
                    entryText.color = UITheme.TextMuted;
                }
            }
        }

        // =====================================================================
        // GAME STATE MACHINE
        // =====================================================================

        private void OnGameStateChanged(GameState state)
        {
            HideAllPanelsFade();

            switch (state)
            {
                case GameState.MainMenu:
                    if (GameManager.Instance != null && GameManager.Instance.ShouldSuppressMainMenuPresentation)
                    {
                        break;
                    }
                    RefreshCampaignPresentation();
                    ShowPanel(_mainMenuPanel);
                    break;
                case GameState.DayPhase:
                case GameState.NightPhase:
                case GameState.Transition:
                    break;
                case GameState.DawnPhase:
                    ShowPanel(_dawnShopPanel);
                    Time.timeScale = 0f;
                    UpdateShopDisplay();
                    break;
                case GameState.LevelComplete:
                    ShowLevelComplete();
                    break;
                case GameState.GameOver:
                    HandleEndingState(false);
                    break;
                case GameState.Victory:
                    HandleEndingState(true);
                    break;
            }
        }

        private void HandleEndingState(bool victory)
        {
            LeaderboardManager.Instance?.SubmitRun(victory);
            if (TryQueueEndingSequence()) return;
            if (victory) ShowVictoryPanel(); else ShowGameOverPanel();
        }

        private bool TryQueueEndingSequence()
        {
            if (_waitingForEnding) return true;
            if (EndingSequence.Instance == null) return false;
            _waitingForEnding = true;
            EndingSequence.Instance.OnEndingComplete += OnEndingSequenceComplete;
            return true;
        }

        private void OnEndingSequenceComplete()
        {
            if (!_waitingForEnding) return;
            if (EndingSequence.Instance != null)
                EndingSequence.Instance.OnEndingComplete -= OnEndingSequenceComplete;
            _waitingForEnding = false;

            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState == GameState.Victory) ShowVictoryPanel();
            else if (GameManager.Instance.CurrentState == GameState.GameOver) ShowGameOverPanel();
        }

        private void ShowGameOverPanel() { ShowPanel(_gameOverPanel); Time.timeScale = 0f; }
        private void ShowVictoryPanel() { ShowPanel(_victoryPanel); Time.timeScale = 0f; }

        // =====================================================================
        // UTILITIES
        // =====================================================================

        private static Sprite LoadMenuPreviewSprite(string key)
        {
            return string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>($"MenuPreviews/{key}");
        }
    }

    // =====================================================================
    // HELPER COMPONENTS (unchanged functionality, tidied up)
    // =====================================================================

    internal class GameOverPanelHelper : MonoBehaviour
    {
        private Text _statsText;
        public void Initialize(Text statsText) => _statsText = statsText;

        private void OnEnable()
        {
            if (_statsText == null) return;

            int level = GameManager.Instance != null ? GameManager.Instance.CurrentLevel : 1;
            int nightInLevel = GameManager.Instance != null ? GameManager.Instance.NightWithinLevel : 1;
            string map = GameManager.Instance != null
                ? GameUI.GetMapDisplayName(GameManager.Instance.SelectedMap) : "Town Center";

            int kills = 0, earned = 0;
            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                earned = stats.totalEarned;
            }

            int rank = -1, score = 0;
            if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.Entries.Count > 0)
            {
                score = LeaderboardManager.Instance.Entries[0].score;
                rank = 1;
                foreach (var e in LeaderboardManager.Instance.Entries)
                {
                    if (e.nightsReached == level && e.kills == kills)
                    { score = e.score; rank = LeaderboardManager.Instance.GetRank(e.score); break; }
                }
            }

            _statsText.text =
                $"Level: {level}, Night: {nightInLevel}\n" +
                $"Enemies Killed: {kills}\nPoints Earned: {earned}\n" +
                $"Map: {map}" +
                (rank > 0 ? $"\nLeaderboard Rank: #{rank}  (Score: {score})" : "");
        }
    }

    internal class VictoryPanelHelper : MonoBehaviour
    {
        private Text _statsText;
        public void Initialize(Text statsText) => _statsText = statsText;

        private void OnEnable()
        {
            if (_statsText == null) return;

            string map = GameManager.Instance != null
                ? GameUI.GetMapDisplayName(GameManager.Instance.SelectedMap) : "Town Center";

            int kills = 0, earned = 0, cleared = GameManager.TotalLevels;
            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                earned = stats.totalEarned;
                cleared = stats.nightsSurvived;
            }

            int rank = -1, score = 0;
            if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.Entries.Count > 0)
            {
                var best = LeaderboardManager.Instance.Entries[0];
                foreach (var e in LeaderboardManager.Instance.Entries)
                    if (e.victory) { best = e; break; }
                score = best.score;
                rank = LeaderboardManager.Instance.GetRank(score);
            }

            _statsText.text =
                $"ALL {cleared} LEVELS CLEARED!\n" +
                "Subject 23 defeated. Extraction signal is live.\n" +
                $"Enemies Killed: {kills}\nPoints Earned: {earned}\n" +
                $"Map: {map}" +
                (rank > 0 ? $"\nLeaderboard Rank: #{rank}  (Score: {score})" : "");
        }
    }
}
