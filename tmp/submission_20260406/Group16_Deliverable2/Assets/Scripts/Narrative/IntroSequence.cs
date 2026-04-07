using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.Core;
using Deadlight.UI;

namespace Deadlight.Narrative
{
    public class IntroSequence : MonoBehaviour
    {
        public static IntroSequence Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float charRevealInterval = 0.03f;
        [SerializeField] private float skipHoldDuration = 1f;

        private Canvas introCanvas;
        private GameObject canvasRoot;
        private CanvasGroup introGroup;
        private Image blackBackground;
        private Image narrativeCard;
        private Text narrativeText;
        private Text headerText;
        private Text locationText;
        private Text skipHintText;
        private Font font;

        private bool isPlaying;
        private bool skipRequested;
        private float escapeHeldTime;

        public bool IsPlaying => isPlaying;

        private AudioSource introAudioSource;
        private AudioClip typeClickClip;
        private AudioClip staticClip;
        private const int ExplosionLineIndex = 6;
        private static readonly Color IntroTextColor = new Color(0.92f, 0.95f, 1f, 1f);
        private static readonly Color IntroMutedColor = new Color(0.62f, 0.68f, 0.76f, 1f);
        private static readonly Color IntroAccentColor = new Color(0.97f, 0.81f, 0.38f, 1f);

        private static readonly IntroLine[] introLines =
        {
            new IntroLine("", 1.5f),
            new IntroLine("EVAC FLIGHT 7 // QUARANTINE AIR CORRIDOR", 2.7f),
            new IntroLine("PILOT: \"Crossing perimeter now. Medic, keep those trauma kits ready.\"", 3.1f),
            new IntroLine("YOU: \"Copy. We pull survivors and we leave fast.\"", 2.7f),
            new IntroLine("PILOT: \"Thermals just spiked... that's not a crowd, that's one large target--\"", 2.7f),
            new IntroLine("[IMPACT ALERT // AIRFRAME BREACH]", 1.9f),
            new IntroLine("PILOT: \"MAYDAY! Flight 7 hit! Brace! Brace!\"", 2.3f),
            new IntroLine("[Crash. Smoke. Static. One channel survives.]", 2.7f),
            new IntroLine("RADIO: \"...Medic, this is EVAC Command. Confirm status.\"", 3.2f),
            new IntroLine("RADIO: \"Good. You're alive. Listen carefully.\"", 2.3f),
            new IntroLine("RADIO: \"Someone weaponized this zone under Project Lazarus.\"", 2.7f),
            new IntroLine("RADIO: \"Move through the sectors. Gather proof. Stay alive until extraction window opens.\"", 3.2f),
            new IntroLine("RADIO: \"Scavenge by day. Survive by night. We'll keep the channel open.\"", 3.4f),
        };

        private struct IntroLine
        {
            public string text;
            public float holdDuration;

            public IntroLine(string text, float holdDuration)
            {
                this.text = text;
                this.holdDuration = holdDuration;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadFont();
            BuildCanvas();
            InitAudio();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void InitAudio()
        {
            introAudioSource = gameObject.AddComponent<AudioSource>();
            introAudioSource.playOnAwake = false;
            introAudioSource.volume = 0.3f;

            try
            {
                typeClickClip = Audio.ProceduralAudioGenerator.GenerateEmptyClick();
                staticClip = Audio.ProceduralAudioGenerator.GenerateAmbientWind();
            }
            catch (System.Exception) { }

            if (staticClip != null)
            {
                var ambientSrc = gameObject.AddComponent<AudioSource>();
                ambientSrc.clip = staticClip;
                ambientSrc.loop = true;
                ambientSrc.volume = 0.15f;
                ambientSrc.Play();
            }
        }

        private void Start()
        {
            isPlaying = true;
            StartCoroutine(PlayIntro());
        }

        private void Update()
        {
            if (!isPlaying) return;

            if (Input.GetKey(KeyCode.Escape))
            {
                escapeHeldTime += Time.unscaledDeltaTime;
                if (escapeHeldTime >= skipHoldDuration)
                {
                    skipRequested = true;
                }

                if (skipHintText != null)
                {
                    float progress = Mathf.Clamp01(escapeHeldTime / skipHoldDuration);
                    skipHintText.text = $"Hold ESC to skip  [{new string('|', Mathf.RoundToInt(progress * 10))}{new string('.', 10 - Mathf.RoundToInt(progress * 10))}]";
                }
            }
            else
            {
                escapeHeldTime = 0f;
                if (skipHintText != null)
                {
                    skipHintText.text = "Hold ESC to skip";
                }
            }
        }

        private void LoadFont()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            if (font == null)
            {
                string[] installed = Font.GetOSInstalledFontNames();
                if (installed != null && installed.Length > 0)
                    font = Font.CreateDynamicFontFromOSFont(installed[0], 14);
            }
        }

        private void BuildCanvas()
        {
            canvasRoot = new GameObject("IntroSequenceCanvas");
            canvasRoot.transform.SetParent(transform);

            introCanvas = canvasRoot.AddComponent<Canvas>();
            introCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            introCanvas.sortingOrder = 200;

            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasRoot.AddComponent<GraphicRaycaster>();
            introGroup = canvasRoot.AddComponent<CanvasGroup>();
            introGroup.alpha = 0f;

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasRoot.transform);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            blackBackground = bgObj.AddComponent<Image>();
            blackBackground.color = new Color(0.01f, 0.02f, 0.04f, 1f);

            var topVignette = UIFactory.CreateRegion(
                canvasRoot.transform,
                "TopVignette",
                new Vector2(0f, 0.56f),
                new Vector2(1f, 1f),
                new Color(0f, 0f, 0f, 0.32f));
            topVignette.GetComponent<Image>().raycastTarget = false;

            var bottomVignette = UIFactory.CreateRegion(
                canvasRoot.transform,
                "BottomVignette",
                new Vector2(0f, 0f),
                new Vector2(1f, 0.44f),
                new Color(0f, 0f, 0f, 0.44f));
            bottomVignette.GetComponent<Image>().raycastTarget = false;

            var cardObj = UIFactory.CreateCard(
                canvasRoot.transform,
                "NarrativeCard",
                new Vector2(0.5f, 0.5f),
                new Vector2(1340f, 390f),
                new Color(0.07f, 0.10f, 0.15f, 0.88f));
            narrativeCard = cardObj.GetComponent<Image>();
            narrativeCard.raycastTarget = false;

            var cardInner = UIFactory.CreateRegion(
                cardObj.transform,
                "NarrativeCardInner",
                Vector2.zero,
                Vector2.one,
                new Color(0.03f, 0.05f, 0.08f, 0.86f),
                new Vector2(12f, 12f));
            cardInner.GetComponent<Image>().raycastTarget = false;

            var headerObj = new GameObject("HeaderText");
            headerObj.transform.SetParent(cardObj.transform, false);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = new Vector2(36f, -46f);
            headerRect.offsetMax = new Vector2(-36f, -16f);
            headerText = headerObj.AddComponent<Text>();
            headerText.font = font;
            headerText.fontSize = 24;
            headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.UpperLeft;
            headerText.color = IntroAccentColor;
            headerText.text = "DEADLIGHT // INCIDENT REPORT";
            headerText.raycastTarget = false;

            var locationObj = new GameObject("LocationText");
            locationObj.transform.SetParent(cardObj.transform, false);
            var locationRect = locationObj.AddComponent<RectTransform>();
            locationRect.anchorMin = new Vector2(0f, 1f);
            locationRect.anchorMax = new Vector2(1f, 1f);
            locationRect.offsetMin = new Vector2(38f, -78f);
            locationRect.offsetMax = new Vector2(-36f, -48f);
            locationText = locationObj.AddComponent<Text>();
            locationText.font = font;
            locationText.fontSize = 16;
            locationText.fontStyle = FontStyle.Bold;
            locationText.alignment = TextAnchor.UpperLeft;
            locationText.color = IntroMutedColor;
            locationText.text = "QUARANTINE ZONE // CHANNEL 07";
            locationText.raycastTarget = false;

            var textObj = new GameObject("NarrativeText");
            textObj.transform.SetParent(cardObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(38f, 36f);
            textRect.offsetMax = new Vector2(-38f, -92f);
            narrativeText = textObj.AddComponent<Text>();
            narrativeText.font = font;
            narrativeText.fontSize = 36;
            narrativeText.alignment = TextAnchor.UpperLeft;
            narrativeText.color = UITheme.WithAlpha(IntroTextColor, 0f);
            narrativeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            narrativeText.verticalOverflow = VerticalWrapMode.Overflow;
            narrativeText.supportRichText = true;
            narrativeText.lineSpacing = 1.14f;
            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            var hintObj = new GameObject("SkipHint");
            hintObj.transform.SetParent(canvasRoot.transform);
            var hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.05f);
            hintRect.anchorMax = new Vector2(0.5f, 0.05f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(500, 32);
            skipHintText = hintObj.AddComponent<Text>();
            skipHintText.font = font;
            skipHintText.fontSize = 18;
            skipHintText.alignment = TextAnchor.MiddleCenter;
            skipHintText.color = new Color(0.58f, 0.66f, 0.76f, 0.85f);
            skipHintText.text = "Hold ESC to skip";
            skipHintText.raycastTarget = false;
        }

        private IEnumerator PlayIntro()
        {
            yield return FadeBackground(0f, 1f, 0.5f);

            for (int i = 0; i < introLines.Length; i++)
            {
                if (skipRequested) break;

                var line = introLines[i];

                if (i == ExplosionLineIndex)
                {
                    TriggerScreenShake();
                    PlayExplosionSound();
                }

                if (string.IsNullOrEmpty(line.text))
                {
                    narrativeText.text = "";
                    yield return FadeText(0f, 1f, 0.5f);
                    yield return HoldWithSkipCheck(line.holdDuration);
                    yield return FadeText(1f, 0f, 0.3f);
                }
                else
                {
                    yield return TypewriterReveal(line.text);
                    if (skipRequested) break;
                    yield return HoldWithSkipCheck(line.holdDuration);
                    if (skipRequested) break;
                    yield return FadeText(1f, 0f, 0.4f);
                }
            }

            yield return FadeBackground(1f, 0f, 1f);
            FinishIntro();
        }

        private IEnumerator TypewriterReveal(string fullText)
        {
            narrativeText.text = "";
            narrativeText.color = IntroTextColor;

            int clickCounter = 0;
            for (int i = 0; i <= fullText.Length; i++)
            {
                if (skipRequested) break;
                narrativeText.text = fullText.Substring(0, i);

                if (typeClickClip != null && introAudioSource != null && i < fullText.Length && fullText[Mathf.Min(i, fullText.Length - 1)] != ' ')
                {
                    clickCounter++;
                    if (clickCounter % 3 == 0)
                    {
                        introAudioSource.pitch = Random.Range(0.85f, 1.15f);
                        introAudioSource.PlayOneShot(typeClickClip, 0.15f);
                    }
                }

                yield return new WaitForSecondsRealtime(charRevealInterval);
            }

            narrativeText.text = fullText;
        }

        private IEnumerator FadeText(float from, float to, float duration)
        {
            if (narrativeText == null) yield break;

            float elapsed = 0f;
            Color baseColor = IntroTextColor;

            while (elapsed < duration)
            {
                if (skipRequested) yield break;
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / duration);
                narrativeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }

            narrativeText.color = new Color(baseColor.r, baseColor.g, baseColor.b, to);
        }

        private IEnumerator FadeBackground(float from, float to, float duration)
        {
            if (introGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / duration);
                introGroup.alpha = alpha;
                yield return null;
            }

            introGroup.alpha = to;
        }

        private IEnumerator HoldWithSkipCheck(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (skipRequested) yield break;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void TriggerScreenShake()
        {
            var cam = FindFirstObjectByType<CameraController>();
            if (cam != null)
            {
                cam.Shake(0.5f, 0.3f);
            }
        }

        private void PlayExplosionSound()
        {
            try
            {
                var clip = Audio.ProceduralAudioGenerator.GenerateExplosion();
                if (clip != null && introAudioSource != null)
                {
                    introAudioSource.PlayOneShot(clip, 0.8f);
                }
            }
            catch (System.Exception) { }
        }

        private void FinishIntro()
        {
            isPlaying = false;

            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
            }

            GameManager.Instance?.NotifyStartupIntroFinished();

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState != GameState.MainMenu)
                {
                    GameManager.Instance.ChangeState(GameState.DayPhase);
                }
                else
                {
                    GameUI.Instance?.RefreshForCurrentState();
                }
            }
            else
            {
                GameUI.Instance?.RefreshForCurrentState();
            }

            Debug.Log("[IntroSequence] Intro complete, showing main menu.");
            Destroy(gameObject);
        }
    }
}
