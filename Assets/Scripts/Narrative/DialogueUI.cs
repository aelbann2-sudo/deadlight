using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.UI;
using Deadlight.Core;

namespace Deadlight.Narrative
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("Typewriter Effect")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 0.025f;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        // Runtime-built UI nodes
        private GameObject dialoguePanel;
        private CanvasGroup canvasGroup;
        private Text speakerLabel;   // "COMMS / EVAC COMMAND / ALERT"
        private Text bodyText;       // main dialogue line
        private Text skipHint;       // "SPACE to advance"
        private Image topAccent;

        private Coroutine typewriterCoroutine;
        private bool isTyping;
        private string currentFullText = string.Empty;
        private bool builtUI;

        // Compact panel dims (used during gameplay)
        private const float PanelW   = 720f;
        private const float PanelH   = 96f;
        private const float PanelY   = 80f;   // pixels above bottom edge

        private void Awake()
        {
            BuildUI();
            HideDialogue(true);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Update()
        {
            if (isTyping && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
                CompleteTypewriter();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void ShowDialogue(DialogueData dialogue)
        {
            if (dialoguePanel == null) return;

            string speaker  = BuildSpeakerLabel(dialogue.SpeakerName);
            Color  accent   = SpeakerAccent(speaker);

            speakerLabel.text  = speaker;
            speakerLabel.color = accent;
            topAccent.color    = UITheme.WithAlpha(accent, 0.85f);
            bodyText.text      = string.Empty;

            dialoguePanel.SetActive(true);
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

            StartCoroutine(FadeIn());
        }

        public void DisplayLine(DialogueLine line)
        {
            if (bodyText == null) return;

            currentFullText  = line.text;
            bodyText.text    = string.Empty;

            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

            if (useTypewriterEffect)
                typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
            else
                bodyText.text = line.text;
        }

        public void HideDialogue(bool immediate = false)
        {
            if (dialoguePanel == null) return;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                isTyping = false;
            }

            if (immediate)
            {
                dialoguePanel.SetActive(false);
                if (canvasGroup != null) canvasGroup.alpha = 0f;
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }

        public void SetTypewriterSpeed(float speed) => typewriterSpeed = Mathf.Max(0.005f, speed);
        public void SetUseTypewriter(bool use)       => useTypewriterEffect = use;

        // ── Internal helpers ───────────────────────────────────────────────────

        private void OnGameStateChanged(GameState _) { /* layout is fixed-compact; nothing to adjust */ }

        private void CompleteTypewriter()
        {
            if (!isTyping) return;
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            bodyText.text = currentFullText;
            isTyping = false;
        }

        private IEnumerator TypewriterEffect(string text)
        {
            isTyping      = true;
            bodyText.text = string.Empty;

            foreach (char c in text)
            {
                bodyText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            isTyping = false;
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                dialoguePanel.SetActive(false);
                yield break;
            }
            float start   = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
        }

        // ── UI construction ────────────────────────────────────────────────────

        private void BuildUI()
        {
            if (builtUI) return;
            builtUI = true;

            Font font = UIFactory.GetFont();

            // ── Canvas ──────────────────────────────────────────────────────────
            var canvasGO = new GameObject("DialogueCanvas");
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder  = 240;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Outer panel ─────────────────────────────────────────────────────
            dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(canvasGO.transform, false);

            var rt = dialoguePanel.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0.5f, 0f);
            rt.anchorMax       = new Vector2(0.5f, 0f);
            rt.pivot           = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, PanelY);
            rt.sizeDelta       = new Vector2(PanelW, PanelH);

            var bg = dialoguePanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.10f, 0.93f);
            bg.raycastTarget = false;

            canvasGroup       = dialoguePanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // ── Gold top accent line ─────────────────────────────────────────────
            var accentGO = new GameObject("TopAccent");
            accentGO.transform.SetParent(dialoguePanel.transform, false);
            var accentRt = accentGO.AddComponent<RectTransform>();
            accentRt.anchorMin       = new Vector2(0f, 1f);
            accentRt.anchorMax       = new Vector2(1f, 1f);
            accentRt.pivot           = new Vector2(0.5f, 1f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta       = new Vector2(0f, 2f);
            topAccent                = accentGO.AddComponent<Image>();
            topAccent.color          = UITheme.WithAlpha(UITheme.AccentGold, 0.85f);
            topAccent.raycastTarget  = false;

            // ── Speaker label (top-left, small) ─────────────────────────────────
            var speakerGO = new GameObject("SpeakerLabel");
            speakerGO.transform.SetParent(dialoguePanel.transform, false);
            var speakerRt = speakerGO.AddComponent<RectTransform>();
            speakerRt.anchorMin       = new Vector2(0f, 1f);
            speakerRt.anchorMax       = new Vector2(1f, 1f);
            speakerRt.pivot           = new Vector2(0f, 1f);
            speakerRt.anchoredPosition = new Vector2(16f, -8f);
            speakerRt.sizeDelta       = new Vector2(-32f, 22f);
            speakerLabel              = speakerGO.AddComponent<Text>();
            speakerLabel.font         = font;
            speakerLabel.text         = "COMMS";
            speakerLabel.fontSize     = 13;
            speakerLabel.fontStyle    = FontStyle.Bold;
            speakerLabel.color        = UITheme.AccentGold;
            speakerLabel.alignment    = TextAnchor.MiddleLeft;
            speakerLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            speakerLabel.verticalOverflow   = VerticalWrapMode.Overflow;
            speakerLabel.raycastTarget      = false;

            // ── Body text (main dialogue line) ───────────────────────────────────
            var bodyGO = new GameObject("BodyText");
            bodyGO.transform.SetParent(dialoguePanel.transform, false);
            var bodyRt = bodyGO.AddComponent<RectTransform>();
            bodyRt.anchorMin       = Vector2.zero;
            bodyRt.anchorMax       = Vector2.one;
            bodyRt.offsetMin       = new Vector2(16f, 10f);
            bodyRt.offsetMax       = new Vector2(-16f, -32f);
            bodyText               = bodyGO.AddComponent<Text>();
            bodyText.font          = font;
            bodyText.text          = string.Empty;
            bodyText.fontSize      = 18;
            bodyText.fontStyle     = FontStyle.Normal;
            bodyText.color         = new Color(0.94f, 0.95f, 0.97f, 1f);
            bodyText.alignment     = TextAnchor.MiddleLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow   = VerticalWrapMode.Overflow;
            bodyText.lineSpacing        = 1.15f;
            bodyText.raycastTarget      = false;

            // ── Skip hint (bottom-right, tiny) ───────────────────────────────────
            var hintGO = new GameObject("SkipHint");
            hintGO.transform.SetParent(dialoguePanel.transform, false);
            var hintRt = hintGO.AddComponent<RectTransform>();
            hintRt.anchorMin       = new Vector2(1f, 0f);
            hintRt.anchorMax       = new Vector2(1f, 0f);
            hintRt.pivot           = new Vector2(1f, 0f);
            hintRt.anchoredPosition = new Vector2(-14f, 7f);
            hintRt.sizeDelta       = new Vector2(260f, 18f);
            skipHint               = hintGO.AddComponent<Text>();
            skipHint.font          = font;
            skipHint.text          = "SPACE / CLICK  ADVANCE";
            skipHint.fontSize      = 10;
            skipHint.fontStyle     = FontStyle.Bold;
            skipHint.color         = new Color(0.55f, 0.62f, 0.72f, 0.75f);
            skipHint.alignment     = TextAnchor.LowerRight;
            skipHint.horizontalOverflow = HorizontalWrapMode.Overflow;
            skipHint.raycastTarget      = false;
        }

        // ── Speaker colour helpers ──────────────────────────────────────────────

        private static string BuildSpeakerLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "COMMS";
            string k = raw.Trim().ToLowerInvariant();
            if (k.Contains("evac") || k.Contains("command")) return "EVAC COMMAND";
            if (k.Contains("alert") || k.Contains("warning")) return "ALERT";
            if (k.Contains("intercept"))                       return "INTERCEPT";
            if (k.Contains("guide") || k.Contains("tip"))      return "GUIDE";
            if (k.Contains("medic"))                           return "MEDIC";
            if (k.Contains("pilot"))                           return "PILOT";
            return raw.Trim().ToUpperInvariant();
        }

        private static Color SpeakerAccent(string speaker)
        {
            string k = speaker.ToLowerInvariant();
            if (k.Contains("alert"))     return UITheme.AccentRed;
            if (k.Contains("intercept")) return UITheme.AccentPurple;
            if (k.Contains("medic"))     return UITheme.AccentGreen;
            if (k.Contains("pilot"))     return UITheme.AccentOrange;
            if (k.Contains("guide"))     return UITheme.AccentBlue;
            return UITheme.AccentGold;   // COMMS / EVAC COMMAND default
        }
    }
}
