using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Deadlight.UI;

namespace Deadlight.Narrative
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image speakerPortrait;
        [SerializeField] private Image backgroundImage;
        
        [Header("Typewriter Effect")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private AudioClip typewriterSound;
        [SerializeField] private AudioSource typewriterAudioSource;
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        [Header("Skip Indicator")]
        [SerializeField] private GameObject skipIndicator;
        [SerializeField] private TextMeshProUGUI skipText;

        [Header("Visual Theme")]
        [SerializeField] private Vector2 panelSize = new Vector2(1480f, 220f);
        [SerializeField] private Vector2 panelOffset = new Vector2(0f, 36f);
        [SerializeField] private Color panelColor = new Color(0.03f, 0.05f, 0.09f, 0.90f);
        [SerializeField] private Color innerPanelColor = new Color(0.08f, 0.11f, 0.17f, 0.92f);
        [SerializeField] private Color dialogueTextColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        [SerializeField] private Color hintColor = new Color(0.72f, 0.78f, 0.88f, 0.82f);

        private CanvasGroup canvasGroup;
        private Coroutine typewriterCoroutine;
        private bool isTyping = false;
        private string currentFullText = "";

        private Image speakerTagBackground;
        private bool builtRuntimePanel;

        private void Awake()
        {
            EnsureRuntimeUI();

            if (dialoguePanel != null)
            {
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            ApplyVisualTheme();
            HideDialogue(true);
        }

        private void Update()
        {
            if (isTyping && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                CompleteTypewriter();
            }
        }

        public void ShowDialogue(DialogueData dialogue)
        {
            if (dialoguePanel == null) return;

            EnsureRuntimeUI();
            ApplyVisualTheme();

            dialoguePanel.SetActive(true);

            if (speakerNameText != null)
            {
                speakerNameText.text = dialogue.SpeakerName;
                speakerNameText.color = ResolveSpeakerAccent(dialogue.SpeakerName);
            }

            if (speakerPortrait != null)
            {
                if (dialogue.SpeakerPortrait != null)
                {
                    speakerPortrait.sprite = dialogue.SpeakerPortrait;
                    speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    speakerPortrait.gameObject.SetActive(false);
                }
            }

            if (speakerTagBackground != null && speakerNameText != null)
            {
                speakerTagBackground.color = UITheme.WithAlpha(speakerNameText.color, 0.22f);
            }

            if (skipIndicator != null)
            {
                skipIndicator.SetActive(true);
            }

            StartCoroutine(FadeIn());
        }

        public void DisplayLine(DialogueLine line)
        {
            if (dialogueText == null) return;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            currentFullText = line.text;
            dialogueText.text = string.Empty;

            if (useTypewriterEffect)
            {
                typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
            }
            else
            {
                dialogueText.text = line.text;
            }
        }

        private IEnumerator TypewriterEffect(string text)
        {
            isTyping = true;
            dialogueText.text = "";

            foreach (char c in text)
            {
                dialogueText.text += c;

                if (typewriterSound != null && typewriterAudioSource != null && c != ' ')
                {
                    typewriterAudioSource.pitch = Random.Range(0.9f, 1.1f);
                    typewriterAudioSource.PlayOneShot(typewriterSound);
                }

                yield return new WaitForSeconds(typewriterSpeed);
            }

            isTyping = false;
        }

        private void CompleteTypewriter()
        {
            if (!isTyping) return;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            dialogueText.text = currentFullText;
            isTyping = false;
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
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }

        private void EnsureRuntimeUI()
        {
            if (dialoguePanel != null && speakerNameText != null && dialogueText != null && skipText != null)
            {
                return;
            }

            if (builtRuntimePanel && dialoguePanel != null)
            {
                return;
            }

            var canvasRoot = new GameObject("DialogueCanvas");
            canvasRoot.transform.SetParent(transform, false);

            var runtimeCanvas = canvasRoot.AddComponent<Canvas>();
            runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas.sortingOrder = 240;

            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot.AddComponent<GraphicRaycaster>();

            dialoguePanel = new GameObject("DialoguePanel");
            dialoguePanel.transform.SetParent(canvasRoot.transform, false);
            var panelRt = dialoguePanel.AddComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = panelOffset;
            panelRt.sizeDelta = panelSize;

            backgroundImage = dialoguePanel.AddComponent<Image>();
            backgroundImage.color = panelColor;
            backgroundImage.raycastTarget = false;
            canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();

            var inner = new GameObject("InnerPanel");
            inner.transform.SetParent(dialoguePanel.transform, false);
            var innerRt = inner.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(12f, 10f);
            innerRt.offsetMax = new Vector2(-12f, -10f);
            var innerImage = inner.AddComponent<Image>();
            innerImage.color = innerPanelColor;
            innerImage.raycastTarget = false;

            var topLine = new GameObject("TopAccent");
            topLine.transform.SetParent(inner.transform, false);
            var topLineRt = topLine.AddComponent<RectTransform>();
            topLineRt.anchorMin = new Vector2(0f, 1f);
            topLineRt.anchorMax = new Vector2(1f, 1f);
            topLineRt.pivot = new Vector2(0.5f, 1f);
            topLineRt.anchoredPosition = Vector2.zero;
            topLineRt.sizeDelta = new Vector2(0f, 3f);
            var topLineImage = topLine.AddComponent<Image>();
            topLineImage.color = UITheme.WithAlpha(UITheme.AccentBlue, 0.95f);
            topLineImage.raycastTarget = false;

            var speakerTag = new GameObject("SpeakerTag");
            speakerTag.transform.SetParent(inner.transform, false);
            var speakerTagRt = speakerTag.AddComponent<RectTransform>();
            speakerTagRt.anchorMin = new Vector2(0f, 1f);
            speakerTagRt.anchorMax = new Vector2(0f, 1f);
            speakerTagRt.pivot = new Vector2(0f, 1f);
            speakerTagRt.anchoredPosition = new Vector2(18f, -14f);
            speakerTagRt.sizeDelta = new Vector2(380f, 40f);
            speakerTagBackground = speakerTag.AddComponent<Image>();
            speakerTagBackground.color = UITheme.WithAlpha(UITheme.AccentBlue, 0.22f);
            speakerTagBackground.raycastTarget = false;

            var speakerObj = new GameObject("SpeakerName");
            speakerObj.transform.SetParent(speakerTag.transform, false);
            var speakerRt = speakerObj.AddComponent<RectTransform>();
            speakerRt.anchorMin = Vector2.zero;
            speakerRt.anchorMax = Vector2.one;
            speakerRt.offsetMin = new Vector2(12f, 2f);
            speakerRt.offsetMax = new Vector2(-10f, -2f);
            speakerNameText = speakerObj.AddComponent<TextMeshProUGUI>();
            speakerNameText.text = "EVAC COMMAND";
            speakerNameText.fontSize = 26f;
            speakerNameText.fontStyle = FontStyles.Bold;
            speakerNameText.color = UITheme.AccentBlue;
            speakerNameText.alignment = TextAlignmentOptions.Left;
            speakerNameText.enableWordWrapping = false;
            speakerNameText.raycastTarget = false;

            var bodyObj = new GameObject("DialogueText");
            bodyObj.transform.SetParent(inner.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(22f, 24f);
            bodyRt.offsetMax = new Vector2(-22f, -62f);
            dialogueText = bodyObj.AddComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 36f;
            dialogueText.color = dialogueTextColor;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.enableWordWrapping = true;
            dialogueText.lineSpacing = 6f;
            dialogueText.text = string.Empty;
            dialogueText.raycastTarget = false;

            skipIndicator = new GameObject("SkipIndicator");
            skipIndicator.transform.SetParent(inner.transform, false);
            var skipRt = skipIndicator.AddComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 0f);
            skipRt.anchorMax = new Vector2(1f, 0f);
            skipRt.pivot = new Vector2(1f, 0f);
            skipRt.anchoredPosition = new Vector2(-16f, 10f);
            skipRt.sizeDelta = new Vector2(430f, 24f);
            skipText = skipIndicator.AddComponent<TextMeshProUGUI>();
            skipText.text = "SPACE / CLICK: REVEAL OR ADVANCE";
            skipText.fontSize = 20f;
            skipText.fontStyle = FontStyles.Bold;
            skipText.color = hintColor;
            skipText.alignment = TextAlignmentOptions.Right;
            skipText.enableWordWrapping = false;
            skipText.raycastTarget = false;

            if (speakerPortrait == null)
            {
                var portraitObj = new GameObject("SpeakerPortrait");
                portraitObj.transform.SetParent(inner.transform, false);
                var portraitRt = portraitObj.AddComponent<RectTransform>();
                portraitRt.anchorMin = new Vector2(1f, 1f);
                portraitRt.anchorMax = new Vector2(1f, 1f);
                portraitRt.pivot = new Vector2(1f, 1f);
                portraitRt.anchoredPosition = new Vector2(-20f, -18f);
                portraitRt.sizeDelta = new Vector2(68f, 68f);
                speakerPortrait = portraitObj.AddComponent<Image>();
                speakerPortrait.color = UITheme.WithAlpha(UITheme.TextPrimary, 0.2f);
                speakerPortrait.gameObject.SetActive(false);
            }

            builtRuntimePanel = true;
        }

        private void ApplyVisualTheme()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = panelColor;
            }

            if (dialogueText != null)
            {
                dialogueText.color = dialogueTextColor;
                dialogueText.fontSize = Mathf.Max(30f, dialogueText.fontSize);
                dialogueText.lineSpacing = Mathf.Max(4f, dialogueText.lineSpacing);
                dialogueText.enableWordWrapping = true;
            }

            if (speakerNameText != null)
            {
                speakerNameText.fontSize = Mathf.Max(22f, speakerNameText.fontSize);
                speakerNameText.fontStyle = FontStyles.Bold;
            }

            if (skipText != null)
            {
                skipText.color = hintColor;
                skipText.fontSize = Mathf.Max(16f, skipText.fontSize);
                skipText.fontStyle = FontStyles.Bold;
            }
        }

        private static Color ResolveSpeakerAccent(string speakerName)
        {
            if (string.IsNullOrEmpty(speakerName))
            {
                return UITheme.AccentBlue;
            }

            string key = speakerName.ToLowerInvariant();
            if (key.Contains("pilot"))
            {
                return UITheme.AccentOrange;
            }

            if (key.Contains("medic") || key.Contains("you"))
            {
                return UITheme.AccentGreen;
            }

            if (key.Contains("command") || key.Contains("radio"))
            {
                return UITheme.AccentGold;
            }

            return UITheme.AccentBlue;
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
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

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
        }

        public void SetTypewriterSpeed(float speed)
        {
            typewriterSpeed = Mathf.Max(0.01f, speed);
        }

        public void SetUseTypewriter(bool use)
        {
            useTypewriterEffect = use;
        }
    }
}
