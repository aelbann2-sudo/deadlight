using UnityEngine;

namespace Deadlight.UI
{
    /// <summary>
    /// Centralized design tokens for the Deadlight UI.
    /// Every color, size, and timing constant lives here so the
    /// entire UI stays consistent and is trivial to re-skin.
    /// </summary>
    public static class UITheme
    {
        // ── Palette ──────────────────────────────────────────
        public static readonly Color BgDark       = new Color(0.06f, 0.07f, 0.09f, 1f);
        public static readonly Color BgMedium     = new Color(0.10f, 0.12f, 0.15f, 1f);
        public static readonly Color BgLight      = new Color(0.14f, 0.16f, 0.20f, 1f);
        public static readonly Color BgOverlay    = new Color(0.02f, 0.02f, 0.04f, 0.88f);

        public static readonly Color TextPrimary  = new Color(0.94f, 0.96f, 0.98f, 1f);
        public static readonly Color TextSecondary = new Color(0.70f, 0.75f, 0.82f, 1f);
        public static readonly Color TextMuted    = new Color(0.48f, 0.52f, 0.58f, 1f);

        public static readonly Color AccentGreen  = new Color(0.28f, 0.72f, 0.40f, 1f);
        public static readonly Color AccentGold   = new Color(0.92f, 0.78f, 0.28f, 1f);
        public static readonly Color AccentBlue   = new Color(0.30f, 0.58f, 0.84f, 1f);
        public static readonly Color AccentRed    = new Color(0.82f, 0.24f, 0.24f, 1f);
        public static readonly Color AccentOrange = new Color(0.88f, 0.58f, 0.20f, 1f);
        public static readonly Color AccentPurple = new Color(0.58f, 0.36f, 0.72f, 1f);

        // Level accents
        public static readonly Color[] LevelAccents =
        {
            new Color(0.28f, 0.68f, 0.42f, 1f), // L1 – Town Center
            new Color(0.52f, 0.74f, 0.32f, 1f), // L2 – Suburban
            new Color(0.88f, 0.60f, 0.22f, 1f), // L3 – Industrial
            new Color(0.82f, 0.28f, 0.28f, 1f), // L4 – Research
        };

        // ── Typography ───────────────────────────────────────
        public const int FontHero      = 52;
        public const int FontTitle     = 36;
        public const int FontHeading   = 26;
        public const int FontBody      = 18;
        public const int FontCaption   = 14;
        public const int FontSmall     = 12;

        // ── Spacing / Layout ─────────────────────────────────
        public const float PanelPad    = 32f;
        public const float CardPad     = 20f;
        public const float ItemGap     = 8f;
        public const float CornerRound = 8f;  // conceptual; Unity UI doesn't round natively

        // ── Animation ────────────────────────────────────────
        public const float FadeDuration   = 0.22f;
        public const float SlideDuration  = 0.28f;
        public const float PunchScale     = 1.06f;

        // ── Helpers ──────────────────────────────────────────
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
        public static Color Brighten(Color c, float t) =>
            Color.Lerp(c, Color.white, Mathf.Clamp01(t));
        public static Color Darken(Color c, float t) =>
            Color.Lerp(c, Color.black, Mathf.Clamp01(t));
    }
}
