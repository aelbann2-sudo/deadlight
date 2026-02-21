using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.Core;

namespace Deadlight.Narrative
{
    public class IntroSequence : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float charRevealInterval = 0.03f;
        [SerializeField] private float skipHoldDuration = 1f;

        private Canvas introCanvas;
        private GameObject canvasRoot;
        private Image blackBackground;
        private Text narrativeText;
        private Text skipHintText;
        private Font font;

        private bool isPlaying;
        private bool skipRequested;
        private float escapeHeldTime;

        private AudioSource introAudioSource;
        private AudioClip typeClickClip;
        private AudioClip staticClip;

        private static readonly IntroLine[] introLines =
        {
            new IntroLine("", 2f),
            new IntroLine("EVAC FLIGHT 7 - QUARANTINE ZONE EXTRACTION", 3f),
            new IntroLine("RADIO: \"All units, extraction is a go. ETA to quarantine zone: 15 minutes.\"", 4f),
            new IntroLine("RADIO: \"Wait... picking up massive biological signatures below.\"", 3f),
            new IntroLine("PILOT: \"What the hell is—\"", 1.5f),
            new IntroLine("[EXPLOSION - Screen shakes]", 2f),
            new IntroLine("RADIO: \"MAYDAY! MAYDAY! Flight 7 is going down!\"", 3f),
            new IntroLine("[Silence... then static]", 3f),
            new IntroLine("RADIO: \"...survivor... do you copy? This is EVAC Command...\"", 4f),
            new IntroLine("RADIO: \"You're the only one who made it. Another team is being assembled.\"", 4f),
            new IntroLine("RADIO: \"ETA: Five nights. You need to survive until then.\"", 4f),
            new IntroLine("RADIO: \"Good luck, soldier. EVAC Command out.\"", 3f),
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
            LoadFont();
            BuildCanvas();
            InitAudio();
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
                    skipHintText.text = $"Hold ESC to skip [{new string('|', Mathf.RoundToInt(progress * 10))}{new string('.', 10 - Mathf.RoundToInt(progress * 10))}]";
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
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasRoot.transform);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            blackBackground = bgObj.AddComponent<Image>();
            blackBackground.color = Color.black;

            var textObj = new GameObject("NarrativeText");
            textObj.transform.SetParent(canvasRoot.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.3f);
            textRect.anchorMax = new Vector2(0.9f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            narrativeText = textObj.AddComponent<Text>();
            narrativeText.font = font;
            narrativeText.fontSize = 32;
            narrativeText.alignment = TextAnchor.MiddleCenter;
            narrativeText.color = new Color(0.3f, 1f, 0.3f, 0f);
            narrativeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            narrativeText.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(2, -2);

            var hintObj = new GameObject("SkipHint");
            hintObj.transform.SetParent(canvasRoot.transform);
            var hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.05f);
            hintRect.anchorMax = new Vector2(0.5f, 0.05f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = Vector2.zero;
            hintRect.sizeDelta = new Vector2(400, 30);
            skipHintText = hintObj.AddComponent<Text>();
            skipHintText.font = font;
            skipHintText.fontSize = 18;
            skipHintText.alignment = TextAnchor.MiddleCenter;
            skipHintText.color = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            skipHintText.text = "Hold ESC to skip";
        }

        private IEnumerator PlayIntro()
        {
            yield return FadeBackground(0f, 1f, 0.5f);

            for (int i = 0; i < introLines.Length; i++)
            {
                if (skipRequested) break;

                var line = introLines[i];

                if (i == 5)
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
            narrativeText.color = new Color(0.3f, 1f, 0.3f, 1f);

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
            Color baseColor = new Color(0.3f, 1f, 0.3f);

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
            if (blackBackground == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / duration);
                blackBackground.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            blackBackground.color = new Color(0, 0, 0, to);
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
            var cam = FindObjectOfType<CameraController>();
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

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState != GameState.MainMenu)
                {
                    GameManager.Instance.ChangeState(GameState.DayPhase);
                }
            }

            Debug.Log("[IntroSequence] Intro complete, showing main menu.");
        }
    }
}
