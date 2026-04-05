using System.Collections;
using System.Collections.Generic;
using Deadlight.Core;
using Deadlight.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.Narrative
{
    public class LevelIntroSequence : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float cardEnterDuration = 0.45f;
        [SerializeField] private float accentLineDuration = 0.28f;
        [SerializeField] private float bodyTypeInterval = 0.012f;
        [SerializeField] private float holdDuration = 2.5f;

        private Canvas introCanvas;
        private GameObject canvasRoot;
        private CanvasGroup rootGroup;
        private RectTransform cardRect;
        private RectTransform accentLineRect;
        private RectTransform sweepRect;
        private RectTransform outerDiamondRect;
        private RectTransform innerDiamondRect;

        private Image backdropImage;
        private Image glowImage;
        private Image sweepImage;
        private Image outerDiamondImage;
        private Image innerDiamondImage;
        private Text eyebrowText;
        private Text titleText;
        private Text subtitleText;
        private Text bodyText;
        private Text skipText;

        private Coroutine introCoroutine;
        private readonly HashSet<int> shownIntroNights = new HashSet<int>();

        private bool isShowing;
        private bool skipRequested;
        private bool subscribedToGameManager;
        private bool freezeActive;
        private float previousTimeScale = 1f;
        private float previousFixedDelta = 0.02f;
        private float decorativeTime;
        private Color currentAccent = UITheme.AccentGold;

        private const float DefaultFixedDeltaTime = 0.02f;
        private const float CardWidth = 860f;

        private readonly struct LevelBriefing
        {
            public readonly int Level;
            public readonly string Title;
            public readonly string Subtitle;
            public readonly string Narrative;
            public readonly string Objective;
            public readonly Color Accent;

            public LevelBriefing(int level, string title, string subtitle, string narrative, string objective, Color accent)
            {
                Level = level;
                Title = title;
                Subtitle = subtitle;
                Narrative = narrative;
                Objective = objective;
                Accent = accent;
            }
        }

        private static readonly LevelBriefing[] Briefings =
        {
            new LevelBriefing(
                1,
                "TOWN CENTER",
                "Crash Evidence",
                "Flight 7 came down inside the quarantine grid. Sweep the streets, reach the wreckage, and find out who turned an evac run into a kill box.",
                "Recover the black box and secure the first Lazarus lead before nightfall.",
                UITheme.LevelAccents[0]),
            new LevelBriefing(
                2,
                "SUBURBAN EVACUATION",
                "Shelter Records",
                "The suburb was sealed before the buses cleared out. Move house to house, track the shelter route, and prove who abandoned the families still trapped here.",
                "Recover the shelter roster and expose where the convoy failed.",
                UITheme.LevelAccents[1]),
            new LevelBriefing(
                3,
                "INDUSTRIAL DISTRICT",
                "Lazarus Breach",
                "The industrial lanes are where Lazarus stopped pretending to be a rescue project. Push through the blackout yards and tear the Subject 23 data out of the lab chain.",
                "Extract control records, breach the lab, and confirm the origin host.",
                UITheme.LevelAccents[2]),
            new LevelBriefing(
                4,
                "RESEARCH FACILITY",
                "Containment Finale",
                "Research Station Omega is the last locked door in the zone. Arm the beacon, strip the archive, and be ready for Subject 23 to answer the signal in person.",
                "Secure the final evidence, trigger extraction, and survive the containment core.",
                UITheme.LevelAccents[3])
        };

        private void Awake()
        {
            BuildCanvas();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            AbortIntroImmediate();
        }

        private void Update()
        {
            if (!subscribedToGameManager)
            {
                TrySubscribe();
            }

            if (!isShowing)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) ||
                Input.GetMouseButtonDown(0) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.Escape))
            {
                skipRequested = true;
            }

            decorativeTime += Time.unscaledDeltaTime;
            AnimateDecorativeElements();
        }

        private void TrySubscribe()
        {
            if (subscribedToGameManager || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            subscribedToGameManager = true;
        }

        private void Unsubscribe()
        {
            if (!subscribedToGameManager || GameManager.Instance == null)
            {
                subscribedToGameManager = false;
                return;
            }

            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            subscribedToGameManager = false;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                shownIntroNights.Clear();
                AbortIntroImmediate();
                return;
            }

            if (state != GameState.DayPhase)
            {
                return;
            }

            TryShowLevelIntro();
        }

        private void TryShowLevelIntro()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            int night = GameManager.Instance.CurrentNight;
            if (GameManager.GetNightWithinLevel(night) != 1)
            {
                return;
            }

            if (!shownIntroNights.Add(night))
            {
                return;
            }

            int level = Mathf.Clamp(GameManager.Instance.CurrentLevel, 1, Briefings.Length);
            LevelBriefing briefing = Briefings[level - 1];

            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
            }

            introCoroutine = StartCoroutine(PlayIntro(briefing));
        }

        private IEnumerator PlayIntro(LevelBriefing briefing)
        {
            freezeActive = false;
            skipRequested = false;
            isShowing = true;
            decorativeTime = 0f;
            currentAccent = briefing.Accent;

            PopulateBriefing(briefing);
            ResetVisualState();

            canvasRoot.SetActive(true);
            FreezeGameplay();

            yield return AnimateIn();
            yield return TypeBodyText(BuildBodyCopy(briefing));
            yield return HoldUntilSkipOrTimeout();
            yield return AnimateOut();

            UnfreezeGameplay();
            canvasRoot.SetActive(false);
            isShowing = false;
            introCoroutine = null;
        }

        private void BuildCanvas()
        {
            canvasRoot = new GameObject("LevelIntroCanvas");
            canvasRoot.transform.SetParent(transform, false);

            introCanvas = UIFactory.CreateScreenCanvas(canvasRoot.transform, "Canvas", 260);
            introCanvas.transform.SetParent(transform, false);
            Destroy(canvasRoot);
            canvasRoot = introCanvas.gameObject;

            backdropImage = UIFactory.CreateFullPanel(canvasRoot.transform, "Backdrop", new Color(0.01f, 0.02f, 0.04f, 0.9f)).GetComponent<Image>();
            rootGroup = canvasRoot.AddComponent<CanvasGroup>();

            GameObject vignetteTop = UIFactory.CreateRegion(
                canvasRoot.transform,
                "TopVignette",
                new Vector2(0f, 0.55f),
                new Vector2(1f, 1f),
                new Color(0.01f, 0.02f, 0.04f, 0.42f));
            vignetteTop.GetComponent<Image>().raycastTarget = false;

            GameObject vignetteBottom = UIFactory.CreateRegion(
                canvasRoot.transform,
                "BottomVignette",
                new Vector2(0f, 0f),
                new Vector2(1f, 0.42f),
                new Color(0.01f, 0.02f, 0.04f, 0.52f));
            vignetteBottom.GetComponent<Image>().raycastTarget = false;

            GameObject cardGlow = UIFactory.CreateCard(
                canvasRoot.transform,
                "CardGlow",
                new Vector2(0.5f, 0.5f),
                new Vector2(980f, 520f),
                new Color(0.4f, 0.7f, 1f, 0.06f));
            glowImage = cardGlow.GetComponent<Image>();

            GameObject cardOuter = UIFactory.CreateCard(
                canvasRoot.transform,
                "CardOuter",
                new Vector2(0.5f, 0.5f),
                new Vector2(860f, 430f),
                new Color(0.09f, 0.11f, 0.15f, 0.96f));
            cardRect = cardOuter.GetComponent<RectTransform>();

            GameObject cardInner = UIFactory.CreateRegion(
                cardOuter.transform,
                "CardInner",
                Vector2.zero,
                Vector2.one,
                new Color(0.05f, 0.07f, 0.10f, 0.92f),
                new Vector2(12f, 12f));
            cardInner.GetComponent<Image>().raycastTarget = false;

            GameObject accentLine = new GameObject("AccentLine");
            accentLine.transform.SetParent(cardOuter.transform, false);
            accentLineRect = accentLine.AddComponent<RectTransform>();
            accentLineRect.anchorMin = new Vector2(0f, 1f);
            accentLineRect.anchorMax = new Vector2(0f, 1f);
            accentLineRect.pivot = new Vector2(0f, 0.5f);
            accentLineRect.anchoredPosition = new Vector2(26f, -28f);
            accentLineRect.sizeDelta = new Vector2(0f, 4f);
            Image accentLineImage = accentLine.AddComponent<Image>();
            accentLineImage.raycastTarget = false;

            GameObject outerDiamond = UIFactory.CreateCard(
                cardOuter.transform,
                "OuterDiamond",
                new Vector2(1f, 1f),
                new Vector2(160f, 160f),
                new Color(1f, 1f, 1f, 0.05f));
            outerDiamondRect = outerDiamond.GetComponent<RectTransform>();
            outerDiamondRect.pivot = new Vector2(1f, 1f);
            outerDiamondRect.anchoredPosition = new Vector2(-64f, -54f);
            outerDiamondRect.localEulerAngles = new Vector3(0f, 0f, 45f);
            outerDiamondImage = outerDiamond.GetComponent<Image>();
            outerDiamondImage.raycastTarget = false;

            GameObject innerDiamond = UIFactory.CreateCard(
                cardOuter.transform,
                "InnerDiamond",
                new Vector2(1f, 1f),
                new Vector2(86f, 86f),
                new Color(1f, 1f, 1f, 0.12f));
            innerDiamondRect = innerDiamond.GetComponent<RectTransform>();
            innerDiamondRect.pivot = new Vector2(1f, 1f);
            innerDiamondRect.anchoredPosition = new Vector2(-100f, -92f);
            innerDiamondRect.localEulerAngles = new Vector3(0f, 0f, 45f);
            innerDiamondImage = innerDiamond.GetComponent<Image>();
            innerDiamondImage.raycastTarget = false;

            GameObject sweep = UIFactory.CreateCard(
                cardOuter.transform,
                "Sweep",
                new Vector2(0.5f, 0.5f),
                new Vector2(180f, 520f),
                new Color(1f, 1f, 1f, 0.05f));
            sweepRect = sweep.GetComponent<RectTransform>();
            sweepRect.localEulerAngles = new Vector3(0f, 0f, 18f);
            sweepImage = sweep.GetComponent<Image>();
            sweepImage.raycastTarget = false;

            eyebrowText = UIFactory.CreateTextAt(
                cardOuter.transform,
                "Eyebrow",
                "",
                UITheme.FontCaption,
                UITheme.AccentGold,
                new Vector2(0f, 1f),
                new Vector2(30f, -56f),
                new Vector2(420f, 22f),
                TextAnchor.UpperLeft,
                FontStyle.Bold);

            titleText = UIFactory.CreateTextAt(
                cardOuter.transform,
                "Title",
                "",
                UITheme.FontHero,
                UITheme.TextPrimary,
                new Vector2(0f, 1f),
                new Vector2(26f, -96f),
                new Vector2(580f, 72f),
                TextAnchor.UpperLeft,
                FontStyle.Bold);

            subtitleText = UIFactory.CreateTextAt(
                cardOuter.transform,
                "Subtitle",
                "",
                UITheme.FontHeading,
                UITheme.TextSecondary,
                new Vector2(0f, 1f),
                new Vector2(30f, -164f),
                new Vector2(520f, 34f),
                TextAnchor.UpperLeft,
                FontStyle.Bold);

            bodyText = UIFactory.CreateTextAt(
                cardOuter.transform,
                "Body",
                "",
                UITheme.FontBody,
                UITheme.TextPrimary,
                new Vector2(0f, 1f),
                new Vector2(30f, -230f),
                new Vector2(640f, 130f),
                TextAnchor.UpperLeft);

            skipText = UIFactory.CreateTextAt(
                cardOuter.transform,
                "Skip",
                "SPACE / CLICK TO CONTINUE",
                UITheme.FontCaption,
                UITheme.TextMuted,
                new Vector2(0f, 0f),
                new Vector2(30f, 34f),
                new Vector2(320f, 18f),
                TextAnchor.UpperLeft,
                FontStyle.Bold);

            titleText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.55f);
            subtitleText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.45f);
            bodyText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.18f);

            canvasRoot.SetActive(false);
            rootGroup.alpha = 0f;
        }

        private void PopulateBriefing(LevelBriefing briefing)
        {
            eyebrowText.text = $"LEVEL {briefing.Level:00}  //  EVAC LIVE BRIEF";
            titleText.text = briefing.Title;
            subtitleText.text = briefing.Subtitle;
            bodyText.text = string.Empty;

            outerDiamondImage.color = UITheme.WithAlpha(briefing.Accent, 0.10f);
            innerDiamondImage.color = UITheme.WithAlpha(UITheme.Brighten(briefing.Accent, 0.25f), 0.22f);
            glowImage.color = UITheme.WithAlpha(briefing.Accent, 0.08f);
            sweepImage.color = UITheme.WithAlpha(UITheme.Brighten(briefing.Accent, 0.35f), 0.08f);
            accentLineRect.GetComponent<Image>().color = briefing.Accent;
            eyebrowText.color = briefing.Accent;
            subtitleText.color = UITheme.Brighten(briefing.Accent, 0.18f);
        }

        private void ResetVisualState()
        {
            rootGroup.alpha = 0f;
            cardRect.localScale = Vector3.one * 0.92f;
            cardRect.anchoredPosition = new Vector2(0f, -12f);
            accentLineRect.sizeDelta = new Vector2(0f, accentLineRect.sizeDelta.y);
            bodyText.text = string.Empty;
            skipText.color = UITheme.WithAlpha(UITheme.TextMuted, 0.45f);
        }

        private void AnimateDecorativeElements()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(decorativeTime * 2.4f);

            if (outerDiamondRect != null)
            {
                outerDiamondRect.localEulerAngles = new Vector3(0f, 0f, 45f + decorativeTime * 14f);
            }

            if (innerDiamondRect != null)
            {
                innerDiamondRect.localEulerAngles = new Vector3(0f, 0f, 45f - decorativeTime * 28f);
                innerDiamondRect.localScale = Vector3.one * (0.95f + pulse * 0.08f);
            }

            if (sweepRect != null)
            {
                float x = -CardWidth * 0.6f + Mathf.PingPong(decorativeTime * 520f, CardWidth * 1.2f);
                sweepRect.anchoredPosition = new Vector2(x, 0f);
            }

            if (glowImage != null)
            {
                glowImage.color = UITheme.WithAlpha(currentAccent, 0.07f + pulse * 0.05f);
            }

            if (skipText != null)
            {
                skipText.color = UITheme.WithAlpha(UITheme.TextSecondary, 0.42f + pulse * 0.25f);
            }
        }

        private IEnumerator AnimateIn()
        {
            float elapsed = 0f;
            Vector2 startPos = new Vector2(0f, -12f);
            Vector2 endPos = Vector2.zero;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                rootGroup.alpha = t;
                backdropImage.color = new Color(0.01f, 0.02f, 0.04f, Mathf.Lerp(0f, 0.92f, t));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < cardEnterDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / cardEnterDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                cardRect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.92f, Vector3.one, eased);
                cardRect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < accentLineDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / accentLineDuration);
                accentLineRect.sizeDelta = new Vector2(Mathf.Lerp(0f, 250f, t), accentLineRect.sizeDelta.y);
                yield return null;
            }
        }

        private IEnumerator TypeBodyText(string fullText)
        {
            bodyText.text = string.Empty;
            for (int i = 0; i <= fullText.Length; i++)
            {
                if (skipRequested)
                {
                    bodyText.text = fullText;
                    yield break;
                }

                bodyText.text = fullText.Substring(0, i);
                if (i < fullText.Length)
                {
                    yield return new WaitForSecondsRealtime(bodyTypeInterval);
                }
            }
        }

        private IEnumerator HoldUntilSkipOrTimeout()
        {
            float elapsed = 0f;
            while (elapsed < holdDuration && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator AnimateOut()
        {
            float elapsed = 0f;
            float startAlpha = rootGroup.alpha;
            Vector3 startScale = cardRect.localScale;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                rootGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                cardRect.localScale = Vector3.LerpUnclamped(startScale, Vector3.one * 1.02f, t);
                yield return null;
            }

            rootGroup.alpha = 0f;
        }

        private string BuildBodyCopy(LevelBriefing briefing)
        {
            return $"{briefing.Narrative}\n\nPRIMARY OBJECTIVE: {briefing.Objective}";
        }

        private void FreezeGameplay()
        {
            if (freezeActive)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            previousFixedDelta = Time.fixedDeltaTime;
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;

            DayNightCycle dayNight = FindFirstObjectByType<DayNightCycle>();
            if (dayNight != null)
            {
                dayNight.SetPaused(true);
            }

            freezeActive = true;
        }

        private void UnfreezeGameplay()
        {
            if (!freezeActive)
            {
                return;
            }

            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            Time.fixedDeltaTime = previousFixedDelta > 0f ? previousFixedDelta : DefaultFixedDeltaTime * Time.timeScale;

            DayNightCycle dayNight = FindFirstObjectByType<DayNightCycle>();
            if (dayNight != null && GameManager.Instance != null && GameManager.Instance.IsGameplayState)
            {
                dayNight.SetPaused(false);
            }

            freezeActive = false;
        }

        private void AbortIntroImmediate()
        {
            if (introCoroutine != null)
            {
                StopCoroutine(introCoroutine);
                introCoroutine = null;
            }

            UnfreezeGameplay();

            isShowing = false;
            skipRequested = false;

            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }

            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
            }
        }
    }
}
