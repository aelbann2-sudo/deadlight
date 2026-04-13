using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Deadlight.UI;
using Deadlight.Core;

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
        [SerializeField] private bool useCompactGameplayLayout = true;
        [SerializeField] private Vector2 compactGameplayPanelSize = new Vector2(740f, 108f);
        [SerializeField] private Vector2 compactGameplayPanelOffset = new Vector2(0f, 84f);
        [SerializeField] private bool forceCommsSpeaker = false;
        [SerializeField] private Color panelColor = new Color(0.03f, 0.05f, 0.09f, 0.90f);
        [SerializeField] private Color innerPanelColor = new Color(0.08f, 0.11f, 0.17f, 0.92f);
        [SerializeField] private Color dialogueTextColor = new Color(0.94f, 0.96f, 0.99f, 1f);
        [SerializeField] private Color hintColor = new Color(0.72f, 0.78f, 0.88f, 0.82f);
        [SerializeField] private Color compactPanelColor = new Color(0.03f, 0.04f, 0.06f, 0.45f);
        [SerializeField] private Color compactInnerPanelColor = new Color(0.06f, 0.08f, 0.11f, 0.32f);
        [SerializeField] private Color compactDialogueTextColor = new Color(0.95f, 0.95f, 0.90f, 1f);
        [SerializeField] private Color compactHintColor = new Color(0.70f, 0.76f, 0.84f, 0.92f);

        private CanvasGroup canvasGroup;
        private Coroutine typewriterCoroutine;
        private bool isTyping = false;
        private string currentFullText = "";

        private RectTransform panelRect;
        private RectTransform channelTagRect;
        private RectTransform speakerTagRect;
        private RectTransform dialogueBodyRect;
        private RectTransform skipRect;
        private bool compactLayoutActive;

        private Image innerPanelBackground;
        private Image topAccentImage;
        private Image channelTagBackground;
        private Image speakerTagBackground;
        private TextMeshProUGUI channelText;
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

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
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
            ApplyLayoutForState();
            ApplyVisualTheme();

            dialoguePanel.SetActive(true);

            string rawSpeaker = forceCommsSpeaker ? "COMMS" : dialogue.SpeakerName;
            string speaker = FormatSpeakerName(rawSpeaker);
            string channel = ResolveChannelLabel(speaker);
            Color speakerAccent = ResolveSpeakerAccent(speaker);
            Color channelAccent = ResolveChannelAccent(channel, speakerAccent);

            if (speakerNameText != null)
            {
                speakerNameText.text = speaker;
                speakerNameText.color = speakerAccent;
            }

            if (speakerPortrait != null)
            {
                if (!forceCommsSpeaker && !compactLayoutActive && dialogue.SpeakerPortrait != null)
                {
                    speakerPortrait.sprite = dialogue.SpeakerPortrait;
                    speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    speakerPortrait.gameObject.SetActive(false);
                }
            }

            if (channelText != null)
            {
                channelText.text = channel;
                channelText.color = channelAccent;
            }

            if (channelTagBackground != null)
            {
                float alpha = compactLayoutActive ? 0.26f : 0.22f;
                channelTagBackground.color = UITheme.WithAlpha(channelAccent, alpha);
            }

            if (speakerTagBackground != null && speakerNameText != null)
            {
                float alpha = compactLayoutActive ? 0.22f : 0.18f;
                speakerTagBackground.color = UITheme.WithAlpha(speakerNameText.color, alpha);
            }

            if (topAccentImage != null)
            {
                topAccentImage.color = UITheme.WithAlpha(channelAccent, 0.95f);
            }

            if (skipIndicator != null)
            {
                // In compact gameplay mode the prompt adds clutter; input still works without the text hint.
                skipIndicator.SetActive(!compactLayoutActive);
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
            innerPanelBackground = innerImage;

            var topLine = new GameObject("TopAccent");
            topLine.transform.SetParent(inner.transform, false);
            var topLineRt = topLine.AddComponent<RectTransform>();
            topLineRt.anchorMin = new Vector2(0f, 1f);
            topLineRt.anchorMax = new Vector2(1f, 1f);
            topLineRt.pivot = new Vector2(0.5f, 1f);
            topLineRt.anchoredPosition = Vector2.zero;
            topLineRt.sizeDelta = new Vector2(0f, 3f);
            topAccentImage = topLine.AddComponent<Image>();
            topAccentImage.color = UITheme.WithAlpha(UITheme.AccentBlue, 0.95f);
            topAccentImage.raycastTarget = false;

            var channelTag = new GameObject("ChannelTag");
            channelTag.transform.SetParent(inner.transform, false);
            var channelTagRt = channelTag.AddComponent<RectTransform>();
            channelTagRt.anchorMin = new Vector2(0f, 1f);
            channelTagRt.anchorMax = new Vector2(0f, 1f);
            channelTagRt.pivot = new Vector2(0f, 1f);
            channelTagRt.anchoredPosition = new Vector2(14f, -8f);
            channelTagRt.sizeDelta = new Vector2(86f, 20f);
            channelTagBackground = channelTag.AddComponent<Image>();
            channelTagBackground.color = UITheme.WithAlpha(UITheme.AccentGold, 0.25f);
            channelTagBackground.raycastTarget = false;

            var channelObj = new GameObject("ChannelLabel");
            channelObj.transform.SetParent(channelTag.transform, false);
            var channelRt = channelObj.AddComponent<RectTransform>();
            channelRt.anchorMin = Vector2.zero;
            channelRt.anchorMax = Vector2.one;
            channelRt.offsetMin = new Vector2(4f, 1f);
            channelRt.offsetMax = new Vector2(-4f, -1f);
            channelText = channelObj.AddComponent<TextMeshProUGUI>();
            channelText.text = "COMMS";
            channelText.fontSize = 11f;
            channelText.fontStyle = FontStyles.Bold;
            channelText.color = UITheme.AccentGold;
            channelText.alignment = TextAlignmentOptions.Center;
            channelText.textWrappingMode = TextWrappingModes.NoWrap;
            channelText.raycastTarget = false;

            var speakerTag = new GameObject("SpeakerTag");
            speakerTag.transform.SetParent(inner.transform, false);
            var speakerTagRt = speakerTag.AddComponent<RectTransform>();
            speakerTagRt.anchorMin = new Vector2(0f, 1f);
            speakerTagRt.anchorMax = new Vector2(0f, 1f);
            speakerTagRt.pivot = new Vector2(0f, 1f);
            speakerTagRt.anchoredPosition = new Vector2(106f, -8f);
            speakerTagRt.sizeDelta = new Vector2(320f, 20f);
            speakerTagBackground = speakerTag.AddComponent<Image>();
            speakerTagBackground.color = UITheme.WithAlpha(UITheme.AccentBlue, 0.22f);
            speakerTagBackground.raycastTarget = false;

            var speakerObj = new GameObject("SpeakerName");
            speakerObj.transform.SetParent(speakerTag.transform, false);
            var speakerRt = speakerObj.AddComponent<RectTransform>();
            speakerRt.anchorMin = Vector2.zero;
            speakerRt.anchorMax = Vector2.one;
            speakerRt.offsetMin = new Vector2(8f, 1f);
            speakerRt.offsetMax = new Vector2(-8f, -1f);
            speakerNameText = speakerObj.AddComponent<TextMeshProUGUI>();
            speakerNameText.text = "EVAC COMMAND";
            speakerNameText.fontSize = 18f;
            speakerNameText.fontStyle = FontStyles.Bold;
            speakerNameText.color = UITheme.AccentBlue;
            speakerNameText.alignment = TextAlignmentOptions.Left;
            speakerNameText.textWrappingMode = TextWrappingModes.NoWrap;
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
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
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
            skipText.textWrappingMode = TextWrappingModes.NoWrap;
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
            CacheRuntimeLayoutReferences();
            ApplyLayoutForState();
        }

        private void ApplyVisualTheme()
        {
            ApplyLayoutForState();

            Color appliedPanelColor = compactLayoutActive ? compactPanelColor : panelColor;
            Color appliedInnerColor = compactLayoutActive ? compactInnerPanelColor : innerPanelColor;
            Color appliedBodyColor = compactLayoutActive ? compactDialogueTextColor : dialogueTextColor;
            Color appliedHintColor = compactLayoutActive ? compactHintColor : hintColor;

            if (backgroundImage != null)
            {
                backgroundImage.color = appliedPanelColor;
            }

            if (innerPanelBackground != null)
            {
                innerPanelBackground.color = appliedInnerColor;
            }

            if (topAccentImage != null)
            {
                Color accent = compactLayoutActive ? UITheme.AccentGold : UITheme.AccentBlue;
                topAccentImage.color = UITheme.WithAlpha(accent, compactLayoutActive ? 0.68f : 0.95f);
            }

            if (dialogueText != null)
            {
                dialogueText.color = appliedBodyColor;
                dialogueText.fontSize = compactLayoutActive ? 20f : 36f;
                dialogueText.lineSpacing = compactLayoutActive ? 1f : 6f;
                dialogueText.textWrappingMode = TextWrappingModes.Normal;
            }

            if (channelText != null)
            {
                channelText.fontSize = compactLayoutActive ? 11f : 14f;
                channelText.fontStyle = FontStyles.Bold;
            }

            if (speakerNameText != null)
            {
                speakerNameText.fontSize = compactLayoutActive ? 14f : 24f;
                speakerNameText.fontStyle = FontStyles.Bold;
            }

            if (skipText != null)
            {
                skipText.color = appliedHintColor;
                skipText.fontSize = compactLayoutActive ? 10f : 20f;
                skipText.fontStyle = FontStyles.Bold;
                skipText.text = compactLayoutActive ? "SPACE / CLICK" : "SPACE / CLICK: REVEAL OR ADVANCE";
            }
        }

        private void OnGameStateChanged(GameState _)
        {
            ApplyLayoutForState();
        }

        private void CacheRuntimeLayoutReferences()
        {
            if (panelRect == null && dialoguePanel != null)
            {
                panelRect = dialoguePanel.GetComponent<RectTransform>();
            }

            if (channelTagRect == null && channelText != null && channelText.transform.parent != null)
            {
                channelTagRect = channelText.transform.parent.GetComponent<RectTransform>();
            }

            if (speakerTagRect == null && speakerNameText != null && speakerNameText.transform.parent != null)
            {
                speakerTagRect = speakerNameText.transform.parent.GetComponent<RectTransform>();
            }

            if (dialogueBodyRect == null && dialogueText != null)
            {
                dialogueBodyRect = dialogueText.rectTransform;
            }

            if (skipRect == null && skipIndicator != null)
            {
                skipRect = skipIndicator.GetComponent<RectTransform>();
            }
        }

        private void ApplyLayoutForState()
        {
            CacheRuntimeLayoutReferences();

            if (panelRect == null)
            {
                return;
            }

            bool shouldUseCompact = useCompactGameplayLayout &&
                                    GameManager.Instance != null &&
                                    GameManager.Instance.IsGameplayState;
            compactLayoutActive = shouldUseCompact;

            if (shouldUseCompact)
            {
                // Keep COMMS centered above the bottom HUD so it never fights with loadout/ammo panel.
                panelRect.anchorMin = new Vector2(0.5f, 0f);
                panelRect.anchorMax = new Vector2(0.5f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.anchoredPosition = compactGameplayPanelOffset;
                panelRect.sizeDelta = compactGameplayPanelSize;

                if (channelTagRect != null)
                {
                    channelTagRect.anchoredPosition = new Vector2(14f, -11f);
                    channelTagRect.sizeDelta = new Vector2(86f, 20f);
                }

                if (speakerTagRect != null)
                {
                    speakerTagRect.anchoredPosition = new Vector2(106f, -11f);
                    speakerTagRect.sizeDelta = new Vector2(360f, 20f);
                }

                if (dialogueBodyRect != null)
                {
                    dialogueBodyRect.offsetMin = new Vector2(16f, 16f);
                    dialogueBodyRect.offsetMax = new Vector2(-16f, -36f);
                }

                if (skipRect != null)
                {
                    skipRect.anchoredPosition = new Vector2(-12f, 6f);
                    skipRect.sizeDelta = new Vector2(210f, 14f);
                }
            }
            else
            {
                panelRect.anchorMin = new Vector2(0.5f, 0f);
                panelRect.anchorMax = new Vector2(0.5f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.anchoredPosition = panelOffset;
                panelRect.sizeDelta = panelSize;

                if (channelTagRect != null)
                {
                    channelTagRect.anchoredPosition = new Vector2(18f, -14f);
                    channelTagRect.sizeDelta = new Vector2(140f, 30f);
                }

                if (speakerTagRect != null)
                {
                    speakerTagRect.anchoredPosition = new Vector2(166f, -14f);
                    speakerTagRect.sizeDelta = new Vector2(540f, 40f);
                }

                if (dialogueBodyRect != null)
                {
                    dialogueBodyRect.offsetMin = new Vector2(22f, 24f);
                    dialogueBodyRect.offsetMax = new Vector2(-22f, -62f);
                }

                if (skipRect != null)
                {
                    skipRect.anchoredPosition = new Vector2(-16f, 10f);
                    skipRect.sizeDelta = new Vector2(430f, 24f);
                }
            }
        }

        private static string FormatSpeakerName(string speakerName)
        {
            if (string.IsNullOrWhiteSpace(speakerName))
            {
                return "COMMS";
            }

            string normalized = speakerName.Trim();
            string key = normalized.ToLowerInvariant();

            if (key.Contains("evac") && key.Contains("command"))
            {
                return "EVAC COMMAND";
            }

            if (key.Contains("comms") || key.Contains("radio"))
            {
                return "COMMS";
            }

            if (key.Contains("intercept"))
            {
                return "INTERCEPT";
            }

            if (key.Contains("alert") || key.Contains("warning"))
            {
                return "ALERT";
            }

            if (key.Contains("guide") || key.Contains("tip"))
            {
                return "GUIDE";
            }

            return normalized.ToUpperInvariant();
        }

        private static string ResolveChannelLabel(string speakerName)
        {
            if (string.IsNullOrWhiteSpace(speakerName))
            {
                return "COMMS";
            }

            string key = speakerName.ToLowerInvariant();
            if (key.Contains("alert") || key.Contains("warning"))
            {
                return "ALERT";
            }

            if (key.Contains("intercept"))
            {
                return "INTERCEPT";
            }

            if (key.Contains("medic") || key.Contains("pilot") || key.Contains("survivor"))
            {
                return "FIELD";
            }

            if (key.Contains("guide") || key.Contains("tip"))
            {
                return "GUIDE";
            }

            return "COMMS";
        }

        private static Color ResolveChannelAccent(string channel, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return fallback;
            }

            return channel.ToUpperInvariant() switch
            {
                "ALERT" => UITheme.AccentRed,
                "INTERCEPT" => UITheme.AccentPurple,
                "FIELD" => UITheme.AccentGreen,
                "GUIDE" => UITheme.AccentBlue,
                _ => UITheme.AccentGold
            };
        }

        private static Color ResolveSpeakerAccent(string speakerName)
        {
            if (string.IsNullOrEmpty(speakerName))
            {
                return UITheme.AccentBlue;
            }

            string key = speakerName.ToLowerInvariant();
            if (key.Contains("alert") || key.Contains("warning"))
            {
                return UITheme.AccentRed;
            }

            if (key.Contains("intercept"))
            {
                return UITheme.AccentPurple;
            }

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

            if (key.Contains("comms"))
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
                elapsed += Time.unscaledDeltaTime;
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
                elapsed += Time.unscaledDeltaTime;
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
