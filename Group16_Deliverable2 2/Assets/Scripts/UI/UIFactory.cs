using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.UI
{
    /// <summary>
    /// Factory helpers that build Canvas UI elements with the Deadlight theme
    /// applied automatically. Eliminates the hundreds of lines of raw
    /// GameObject/RectTransform/Image/Text wiring that cluttered the old UI.
    /// </summary>
    public static class UIFactory
    {
        private sealed class PanelFadeState : MonoBehaviour
        {
            public Coroutine ActiveCoroutine;
        }

        private static Font _cachedFont;

        public static Font GetFont()
        {
            if (_cachedFont != null) return _cachedFont;
            _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (_cachedFont != null) return _cachedFont;
            _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_cachedFont != null) return _cachedFont;
            string[] names = Font.GetOSInstalledFontNames();
            if (names != null && names.Length > 0)
                _cachedFont = Font.CreateDynamicFontFromOSFont(names[0], 16);
            return _cachedFont;
        }

        // ── Canvas ───────────────────────────────────────────

        public static Canvas CreateScreenCanvas(Transform parent, string name, int sortOrder = 200)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ── Panels ───────────────────────────────────────────

        /// <summary>Full-screen panel that fills its parent.</summary>
        public static GameObject CreateFullPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        /// <summary>Card surface with fixed size, anchored at a position.</summary>
        public static GameObject CreateCard(Transform parent, string name,
            Vector2 anchor, Vector2 size, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            return go;
        }

        /// <summary>Stretch-anchored region inside a parent.</summary>
        public static GameObject CreateRegion(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color, Vector2 padding = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = padding;
            rt.offsetMax = -padding;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        // ── Text ─────────────────────────────────────────────

        public static Text CreateText(Transform parent, string name, string content,
            int fontSize, Color color, TextAnchor align = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();

            var txt = go.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>Positioned text element with explicit anchored position and size.</summary>
        public static Text CreateTextAt(Transform parent, string name, string content,
            int fontSize, Color color, Vector2 anchor, Vector2 anchoredPos, Vector2 size,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var txt = CreateText(parent, name, content, fontSize, color, align, style);
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return txt;
        }

        // ── Buttons ──────────────────────────────────────────

        /// <summary>
        /// Modern flat button with accent color bar on the left,
        /// title + subtitle, hover/press feedback.
        /// </summary>
        public static Button CreateActionButton(Transform parent, string name,
            string title, string subtitle, Color accent, Vector2 anchor,
            Vector2 anchoredPos, Vector2 size, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            // Background
            var bg = go.AddComponent<Image>();
            bg.color = UITheme.BgLight;

            // Button component
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            // Accent bar
            var barGo = new GameObject("Accent");
            barGo.transform.SetParent(go.transform, false);
            var barRt = barGo.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(4f, 0f);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = accent;
            barImg.raycastTarget = false;

            // Title
            CreateTextAt(go.transform, "Title", title, UITheme.FontHeading,
                UITheme.TextPrimary, new Vector2(0f, 1f), new Vector2(20f, -10f),
                new Vector2(size.x - 32f, 30f), TextAnchor.MiddleLeft, FontStyle.Bold);

            // Subtitle
            if (!string.IsNullOrEmpty(subtitle))
            {
                CreateTextAt(go.transform, "Subtitle", subtitle, UITheme.FontCaption,
                    UITheme.TextSecondary, new Vector2(0f, 1f), new Vector2(22f, -44f),
                    new Vector2(size.x - 40f, 40f), TextAnchor.UpperLeft);
            }

            return btn;
        }

        /// <summary>Simple compact button.</summary>
        public static Button CreateCompactButton(Transform parent, string name,
            string label, Color accent, Vector2 anchor, Vector2 anchoredPos,
            Vector2 size, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var bg = go.AddComponent<Image>();
            bg.color = accent;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = accent;
            colors.highlightedColor = UITheme.Brighten(accent, 0.15f);
            colors.pressedColor = UITheme.Darken(accent, 0.15f);
            colors.disabledColor = UITheme.WithAlpha(accent, 0.4f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            Stretch(txtRt);

            var txt = txtGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = UITheme.FontBody;
            txt.fontStyle = FontStyle.Bold;
            txt.color = UITheme.TextPrimary;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return btn;
        }

        /// <summary>Center-anchored button (for overlays / bottom actions).</summary>
        public static Button CreateCenteredButton(Transform parent, string name,
            string label, Color accent, Vector2 anchor, Vector2 size, Action onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var bg = go.AddComponent<Image>();
            bg.color = accent;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = accent;
            colors.highlightedColor = UITheme.Brighten(accent, 0.15f);
            colors.pressedColor = UITheme.Darken(accent, 0.15f);
            colors.disabledColor = UITheme.WithAlpha(accent, 0.4f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            Stretch(txtRt);

            var txt = txtGo.AddComponent<Text>();
            txt.font = GetFont();
            txt.text = label;
            txt.fontSize = UITheme.FontBody;
            txt.fontStyle = FontStyle.Bold;
            txt.color = UITheme.TextPrimary;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return btn;
        }

        // ── Fill Bars ────────────────────────────────────────

        public static (Image background, Image fill) CreateFillBar(Transform parent, string name,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color fillColor, Color bgColor = default)
        {
            if (bgColor == default) bgColor = new Color(0.08f, 0.08f, 0.10f, 0.9f);

            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            var cRt = container.AddComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = anchor;
            cRt.pivot = new Vector2(0f, 0.5f);
            cRt.anchoredPosition = anchoredPos;
            cRt.sizeDelta = size;

            var bgImg = container.AddComponent<Image>();
            bgImg.color = bgColor;
            bgImg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(container.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            fillRt.pivot = new Vector2(0f, 0.5f);

            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
            fillImg.raycastTarget = false;

            return (bgImg, fillImg);
        }

        // ── Layout helpers ───────────────────────────────────

        public static VerticalLayoutGroup AddVerticalLayout(GameObject go,
            float spacing = 0f, RectOffset padding = null,
            TextAnchor childAlignment = TextAnchor.UpperLeft)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding ?? new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = childAlignment;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            return vlg;
        }

        public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go,
            float spacing = 0f, RectOffset padding = null,
            TextAnchor childAlignment = TextAnchor.MiddleLeft)
        {
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = padding ?? new RectOffset(0, 0, 0, 0);
            hlg.childAlignment = childAlignment;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            return hlg;
        }

        public static LayoutElement AddLayoutElement(GameObject go,
            float preferredHeight = -1f, float preferredWidth = -1f,
            float minHeight = -1f, float minWidth = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var le = go.AddComponent<LayoutElement>();
            if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
            if (preferredWidth >= 0) le.preferredWidth = preferredWidth;
            if (minHeight >= 0) le.minHeight = minHeight;
            if (minWidth >= 0) le.minWidth = minWidth;
            if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0) le.flexibleHeight = flexibleHeight;
            return le;
        }

        // ── Transitions ──────────────────────────────────────

        /// <summary>Fade a CanvasGroup in/out. Adds CanvasGroup if missing.</summary>
        public static Coroutine FadePanel(MonoBehaviour host, GameObject panel,
            bool show, float duration = -1f)
        {
            if (host == null || panel == null) return null;
            if (duration < 0f) duration = UITheme.FadeDuration;

            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();

            var fadeState = panel.GetComponent<PanelFadeState>();
            if (fadeState == null) fadeState = panel.AddComponent<PanelFadeState>();

            if (fadeState.ActiveCoroutine != null)
            {
                host.StopCoroutine(fadeState.ActiveCoroutine);
                fadeState.ActiveCoroutine = null;
            }

            cg.interactable = false;
            cg.blocksRaycasts = false;

            if (!show && !panel.activeSelf && cg.alpha <= 0.001f)
            {
                cg.alpha = 0f;
                return null;
            }

            fadeState.ActiveCoroutine = host.StartCoroutine(FadeCoroutine(panel, cg, fadeState, show, duration));
            return fadeState.ActiveCoroutine;
        }

        private static IEnumerator FadeCoroutine(GameObject panel, CanvasGroup cg,
            PanelFadeState fadeState, bool show, float duration)
        {
            if (duration <= 0f)
            {
                if (show)
                {
                    panel.SetActive(true);
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                else
                {
                    cg.alpha = 0f;
                    panel.SetActive(false);
                }

                fadeState.ActiveCoroutine = null;
                yield break;
            }

            if (show)
            {
                panel.SetActive(true);
                if (cg.alpha <= 0.001f)
                {
                    cg.alpha = 0f;
                }
            }

            float start = cg.alpha;
            float end = show ? 1f : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cg.alpha = Mathf.Lerp(start, end, t);
                yield return null;
            }

            cg.alpha = end;

            if (show)
            {
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else
            {
                panel.SetActive(false);
            }

            fadeState.ActiveCoroutine = null;
        }

        // ── Utility ──────────────────────────────────────────

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetAnchored(RectTransform rt, Vector2 anchor,
            Vector2 anchoredPos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
