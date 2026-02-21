using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Deadlight.Core;
using Deadlight.Systems;

namespace Deadlight.Narrative
{
    public class EndingSequence : MonoBehaviour
    {
        public static EndingSequence Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float charRevealInterval = 0.03f;

        private Canvas endingCanvas;
        private GameObject canvasRoot;
        private Image background;
        private Text narrativeText;
        private Font font;
        private bool isPlaying;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            LoadFont();
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

        public void PlayVictoryEnding()
        {
            if (isPlaying) return;
            isPlaying = true;
            BuildCanvas();
            StartCoroutine(VictorySequence());
        }

        public void PlayDeathEnding()
        {
            if (isPlaying) return;
            isPlaying = true;
            BuildCanvas();
            StartCoroutine(DeathSequence());
        }

        private void BuildCanvas()
        {
            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
            }

            canvasRoot = new GameObject("EndingSequenceCanvas");
            canvasRoot.transform.SetParent(transform);

            endingCanvas = canvasRoot.AddComponent<Canvas>();
            endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            endingCanvas.sortingOrder = 250;

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
            background = bgObj.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0);

            var textObj = new GameObject("EndingText");
            textObj.transform.SetParent(canvasRoot.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.3f);
            textRect.anchorMax = new Vector2(0.9f, 0.7f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            narrativeText = textObj.AddComponent<Text>();
            narrativeText.font = font;
            narrativeText.fontSize = 30;
            narrativeText.alignment = TextAnchor.MiddleCenter;
            narrativeText.color = new Color(1f, 1f, 1f, 0f);
            narrativeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            narrativeText.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        private IEnumerator VictorySequence()
        {
            yield return FadeBackground(0f, 1f, 1f, Color.black);

            yield return ShowLine("[Subject 23 collapses]", new Color(1f, 0.9f, 0.5f), 3f);
            yield return ShowLine("RADIO: \"Biological signature terminated. You did it, survivor.\"", new Color(0.3f, 1f, 0.3f), 4f);
            yield return ShowLine("RADIO: \"Helicopter is 2 minutes out. Hold position.\"", new Color(0.3f, 1f, 0.3f), 3f);
            yield return ShowLine("[Dawn breaks]", new Color(1f, 0.85f, 0.4f), 3f);
            yield return ShowLine("RADIO: \"Welcome home, soldier.\"", new Color(0.3f, 1f, 0.3f), 4f);

            int kills = 0;
            if (PointsSystem.Instance != null)
            {
                kills = PointsSystem.Instance.EnemiesKilled;
            }

            int loreFound = 0;
            int loreTotal = 0;
            if (EnvironmentalLore.Instance != null)
            {
                loreFound = EnvironmentalLore.Instance.DiscoveredLoreCount;
                loreTotal = EnvironmentalLore.Instance.TotalLoreCount;
            }

            int maxNights = GameManager.Instance != null ? GameManager.Instance.MaxNights : 5;
            string statsLine = $"SURVIVED: {maxNights} NIGHTS  |  KILLS: {kills}  |  LORE: {loreFound}/{loreTotal}";
            yield return ShowLine(statsLine, new Color(0.9f, 0.85f, 0.5f), 5f);

            yield return new WaitForSecondsRealtime(1f);

            yield return ShowLine(
                "POST-CREDITS: [Static] \"This is Research Station Omega. Project Lazarus... continues.\"",
                new Color(0.6f, 0.2f, 0.2f), 5f);

            yield return new WaitForSecondsRealtime(1f);
            ShowEndButtons(true);
        }

        private IEnumerator DeathSequence()
        {
            yield return FadeBackground(0f, 0.9f, 1.5f, new Color(0.3f, 0f, 0f));

            yield return ShowLine("RADIO: \"Survivor? Survivor, do you copy?\"", new Color(0.3f, 1f, 0.3f), 3f);
            yield return ShowLine("RADIO: \"We've lost contact...\"", new Color(0.3f, 0.8f, 0.3f), 3f);
            yield return ShowLine("RADIO: \"Another one gone...\"", new Color(0.2f, 0.6f, 0.2f), 3f);

            int kills = 0;
            int nightsSurvived = 0;
            if (PointsSystem.Instance != null)
            {
                kills = PointsSystem.Instance.EnemiesKilled;
                nightsSurvived = PointsSystem.Instance.NightsSurvived;
            }

            string statsLine = $"SURVIVED: {nightsSurvived} NIGHTS  |  KILLS: {kills}";
            yield return ShowLine(statsLine, new Color(0.8f, 0.3f, 0.3f), 4f);

            yield return new WaitForSecondsRealtime(0.5f);
            ShowEndButtons(false);
        }

        private IEnumerator ShowLine(string text, Color textColor, float holdDuration)
        {
            if (narrativeText == null) yield break;

            narrativeText.text = "";
            narrativeText.color = new Color(textColor.r, textColor.g, textColor.b, 1f);

            for (int i = 0; i <= text.Length; i++)
            {
                narrativeText.text = text.Substring(0, i);
                yield return new WaitForSecondsRealtime(charRevealInterval);
            }

            narrativeText.text = text;
            yield return new WaitForSecondsRealtime(holdDuration);

            float fadeOut = 0.4f;
            float elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOut);
                narrativeText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                yield return null;
            }

            narrativeText.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
        }

        private IEnumerator FadeBackground(float fromAlpha, float toAlpha, float duration, Color baseColor)
        {
            if (background == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                background.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }

            background.color = new Color(baseColor.r, baseColor.g, baseColor.b, toAlpha);
        }

        private void ShowEndButtons(bool isVictory)
        {
            if (narrativeText != null)
            {
                narrativeText.text = "";
            }

            EnsureEventSystem();

            if (isVictory)
            {
                CreateEndButton("TRY AGAIN", new Vector2(0.4f, 0.3f), new Color(0.2f, 0.6f, 0.3f), OnRestart);
                CreateEndButton("MAIN MENU", new Vector2(0.6f, 0.3f), new Color(0.4f, 0.4f, 0.4f), OnMainMenu);
            }
            else
            {
                CreateEndButton("TRY AGAIN", new Vector2(0.4f, 0.3f), new Color(0.7f, 0.2f, 0.2f), OnRestart);
                CreateEndButton("MAIN MENU", new Vector2(0.6f, 0.3f), new Color(0.4f, 0.4f, 0.4f), OnMainMenu);
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

        private void CreateEndButton(string label, Vector2 anchor, Color color, System.Action onClick)
        {
            if (canvasRoot == null) return;

            var btnObj = new GameObject($"Button_{label}");
            btnObj.transform.SetParent(canvasRoot.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = anchor;
            btnRect.anchorMax = anchor;
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(220, 55);

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = color;

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = btnImage;
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = new Color(
                Mathf.Min(1f, color.r + 0.2f),
                Mathf.Min(1f, color.g + 0.2f),
                Mathf.Min(1f, color.b + 0.2f));
            colors.pressedColor = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelObj.AddComponent<Text>();
            labelText.font = font;
            labelText.fontSize = 22;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.fontStyle = FontStyle.Bold;
            labelText.text = label;
        }

        private void OnRestart()
        {
            Cleanup();
            Time.timeScale = 1f;
            GameManager.Instance?.RestartGame();
        }

        private void OnMainMenu()
        {
            Cleanup();
            Time.timeScale = 1f;
            GameManager.Instance?.ReturnToMainMenu();
        }

        private void Cleanup()
        {
            isPlaying = false;
            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
                canvasRoot = null;
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
