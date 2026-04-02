using System.Collections;
using System.Collections.Generic;
using System.Text;
using Deadlight.Core;
using Deadlight.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.UI
{
    public sealed class GameplayHelpEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string EasyDescription { get; }
        public string ShortHint { get; }

        public GameplayHelpEntry(string id, string displayName, string easyDescription, string shortHint)
        {
            Id = id;
            DisplayName = displayName;
            EasyDescription = easyDescription;
            ShortHint = shortHint;
        }
    }

    public static class GameplayGuideContent
    {
        public static class ItemIds
        {
            public const string Health = "health";
            public const string Ammo = "ammo";
            public const string Scrap = "scrap";
            public const string Wood = "wood";
            public const string Chemicals = "chemicals";
            public const string Electronics = "electronics";
            public const string Points = "points";
            public const string Powerup = "powerup";
            public const string BlueprintToken = "blueprint_token";
            public const string Armor = "armor";
        }

        private static readonly Dictionary<string, GameplayHelpEntry> Entries = new Dictionary<string, GameplayHelpEntry>
        {
            [ItemIds.Health] = new GameplayHelpEntry(
                ItemIds.Health,
                "Health Pack",
                "Restores lost health so you can stay alive through the next fight.",
                "Restores HP."),
            [ItemIds.Ammo] = new GameplayHelpEntry(
                ItemIds.Ammo,
                "Ammo",
                "Adds reserve ammunition for your weapons. Reload with R when the magazine runs dry.",
                "Adds reserve ammo."),
            [ItemIds.Scrap] = new GameplayHelpEntry(
                ItemIds.Scrap,
                "Scrap",
                "Common salvage used in daytime crafting, especially Ammo Cache and Weakpoint Intel prep.",
                "Core crafting salvage."),
            [ItemIds.Wood] = new GameplayHelpEntry(
                ItemIds.Wood,
                "Wood",
                "Support material used for early prep and fortification-style crafting during the day.",
                "Basic support material."),
            [ItemIds.Chemicals] = new GameplayHelpEntry(
                ItemIds.Chemicals,
                "Chemicals",
                "Needed for Field Med and Shock Beacon crafting recipes before nightfall.",
                "Medical and tech crafting."),
            [ItemIds.Electronics] = new GameplayHelpEntry(
                ItemIds.Electronics,
                "Electronics",
                "Advanced parts used in higher-tier recipes like Shock Beacon and Weakpoint Intel.",
                "Advanced crafting part."),
            [ItemIds.Points] = new GameplayHelpEntry(
                ItemIds.Points,
                "Points",
                "Spend these at dawn on weapons, armor, and permanent upgrades for later levels.",
                "Spend at dawn shop."),
            [ItemIds.Powerup] = new GameplayHelpEntry(
                ItemIds.Powerup,
                "Powerup",
                "Activates a random temporary combat bonus such as Double Damage, Speed Boost, Infinite Ammo, or Invincibility.",
                "Random temporary buff."),
            [ItemIds.BlueprintToken] = new GameplayHelpEntry(
                ItemIds.BlueprintToken,
                "Blueprint Token",
                "Unlocks advanced daytime recipes that improve your odds in the next level.",
                "Needed for advanced crafting."),
            [ItemIds.Armor] = new GameplayHelpEntry(
                ItemIds.Armor,
                "Armor",
                "Vests and helmets absorb incoming damage before your health does.",
                "Absorbs incoming damage.")
        };

        public static string GetItemId(PickupType pickupType)
        {
            return pickupType switch
            {
                PickupType.Health => ItemIds.Health,
                PickupType.Ammo => ItemIds.Ammo,
                PickupType.Scrap => ItemIds.Scrap,
                PickupType.Wood => ItemIds.Wood,
                PickupType.Chemicals => ItemIds.Chemicals,
                PickupType.Electronics => ItemIds.Electronics,
                PickupType.Points => ItemIds.Points,
                PickupType.Powerup => ItemIds.Powerup,
                _ => string.Empty
            };
        }

        public static bool TryGetEntry(string itemId, out GameplayHelpEntry entry)
        {
            return Entries.TryGetValue(itemId, out entry);
        }

        public static string GetControlsText()
        {
            return "WASD  Move\n" +
                   "Mouse  Aim\n" +
                   "Left Click  Fire\n" +
                   "R  Reload\n" +
                   "1 / 2 / Wheel  Swap weapon\n" +
                   "Left Shift  Sprint\n" +
                   "Space  Dodge\n" +
                   "Q  Throw grenade\n" +
                   "E  Throw molotov / interact\n" +
                   "C  Open crafting (day only)\n" +
                   "J  Open journal\n" +
                   "[ and ]  Cycle journal pages\n" +
                   "H or F1  Open guide\n" +
                   "Esc  Pause";
        }

        public static string GetRulesText()
        {
            return "Daytime is for scavenging, story leads, and prep. Collect supplies, recover lore, and secure contested drops before sunset.\n\n" +
                   "Nighttime is pure survival. Hold out through the waves until dawn while using the resources and upgrades you prepared earlier.\n\n" +
                   "At dawn, spend points on weapons, armor, and upgrades. Strong day prep makes later levels manageable.";
        }

        public static string GetSystemsText()
        {
            return "Crafting: Press C during the day to convert resources into night bonuses like extra ammo, healing, slower enemies, or weaker enemies.\n\n" +
                   "Contested Drops: High-value crates during the day that pay out bundled resources, points, and blueprint tokens if you secure them.\n\n" +
                   "Journal: Press J to review recovered lore and the current story arc so players can reconnect mechanics with the world.";
        }

        public static string GetItemsText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Health Packs: restore lost HP.");
            builder.AppendLine("Ammo: refills reserve ammo.");
            builder.AppendLine("Scrap / Wood / Chemicals / Electronics: daytime crafting resources.");
            builder.AppendLine("Points: currency for the dawn shop and upgrades.");
            builder.AppendLine("Powerups: temporary combat boosts.");
            builder.AppendLine("Blueprint Tokens: unlock advanced recipes from contested drops.");
            builder.Append("Armor: helmets and vests soak damage before health.");
            return builder.ToString();
        }

        public static string GetAccessibilityNote()
        {
            return "First-time discoveries show fuller guidance; repeat pickups stay compact so the HUD remains readable.";
        }
    }

    public class GameplayHelpSystem : MonoBehaviour
    {
        private const float DetailedDisplaySeconds = 3.1f;
        private const float CompactDisplaySeconds = 1.8f;

        private struct HintRequest
        {
            public string itemId;
            public int amount;
        }

        public static GameplayHelpSystem Instance { get; private set; }

        private readonly Queue<HintRequest> queuedHints = new Queue<HintRequest>();
        private readonly HashSet<string> easyModeExplainedItems = new HashSet<string>();

        private CanvasGroup hintGroup;
        private RectTransform hintPanel;
        private Text itemLabelText;
        private Text itemHintText;
        private Font font;
        private Coroutine displayRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildUI();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        public void ResetSession()
        {
            queuedHints.Clear();
            easyModeExplainedItems.Clear();

            if (displayRoutine != null)
            {
                StopCoroutine(displayRoutine);
                displayRoutine = null;
            }

            SetVisible(false);
        }

        public void ShowPickup(PickupType pickupType, int amount)
        {
            string itemId = GameplayGuideContent.GetItemId(pickupType);
            if (!string.IsNullOrEmpty(itemId))
            {
                ShowItem(itemId, amount);
            }
        }

        public void ShowItem(string itemId, int amount = 0)
        {
            if (!GameplayGuideContent.TryGetEntry(itemId, out _))
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayState)
            {
                return;
            }

            queuedHints.Enqueue(new HintRequest
            {
                itemId = itemId,
                amount = amount
            });

            if (displayRoutine == null)
            {
                displayRoutine = StartCoroutine(ProcessQueue());
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (!GameManager.IsGameplayStateValue(newState))
            {
                queuedHints.Clear();
                SetVisible(false);
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (queuedHints.Count > 0)
            {
                HintRequest request = queuedHints.Dequeue();
                if (!GameplayGuideContent.TryGetEntry(request.itemId, out var entry))
                {
                    continue;
                }

                bool useDetailedHint = !easyModeExplainedItems.Contains(request.itemId);
                if (useDetailedHint)
                {
                    easyModeExplainedItems.Add(request.itemId);
                }

                string label = BuildLabel(entry.DisplayName, request.amount);
                string hintText = BuildHintBody(entry, useDetailedHint);
                float holdTime = useDetailedHint ? DetailedDisplaySeconds : CompactDisplaySeconds;

                ApplyHint(label, hintText);
                yield return FadeHint(0f, 1f, 0.15f);
                yield return WaitRealtime(holdTime);
                yield return FadeHint(1f, 0f, 0.18f);
                SetVisible(false);
                yield return WaitRealtime(0.05f);
            }

            displayRoutine = null;
        }

        private string BuildLabel(string displayName, int amount)
        {
            return amount > 0 ? $"{displayName} x{amount}" : displayName;
        }

        private string BuildHintBody(GameplayHelpEntry entry, bool useDetailedHint)
        {
            if (useDetailedHint)
            {
                return entry.EasyDescription;
            }

            return entry.ShortHint;
        }

        private void ApplyHint(string label, string hintText)
        {
            if (itemLabelText == null || itemHintText == null || hintPanel == null || hintGroup == null)
            {
                return;
            }

            itemLabelText.text = label.ToUpperInvariant();
            bool hasBody = !string.IsNullOrWhiteSpace(hintText);
            itemHintText.text = hintText;
            itemHintText.gameObject.SetActive(hasBody);
            hintPanel.sizeDelta = new Vector2(420f, hasBody ? 104f : 64f);
            SetVisible(true);
            hintGroup.alpha = 0f;
        }

        private IEnumerator FadeHint(float startAlpha, float targetAlpha, float duration)
        {
            if (hintGroup == null)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0.001f ? 1f : Mathf.Clamp01(elapsed / duration);
                hintGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            hintGroup.alpha = targetAlpha;
        }

        private IEnumerator WaitRealtime(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetVisible(bool visible)
        {
            if (hintPanel != null)
            {
                hintPanel.gameObject.SetActive(visible);
            }
        }

        private void BuildUI()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            }

            var canvasObject = new GameObject("GameplayHelpCanvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();

            hintPanel = CreateUIRect(canvasObject.transform, "PickupHintPanel",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 18f), new Vector2(420f, 104f));
            var panelImage = hintPanel.gameObject.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.06f, 0.09f, 0.9f);
            panelImage.raycastTarget = false;

            hintGroup = hintPanel.gameObject.AddComponent<CanvasGroup>();
            hintGroup.alpha = 0f;

            var accent = CreateUIRect(hintPanel, "AccentBar",
                new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
            var accentImage = accent.gameObject.AddComponent<Image>();
            accentImage.color = new Color(0.95f, 0.78f, 0.28f, 0.95f);

            itemLabelText = CreateText(hintPanel, "ItemLabel",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -44f), new Vector2(-16f, -14f),
                21, TextAnchor.UpperLeft, new Color(0.98f, 0.96f, 0.92f), FontStyle.Bold);

            itemHintText = CreateText(hintPanel, "ItemHint",
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(18f, 12f), new Vector2(-16f, -42f),
                16, TextAnchor.UpperLeft, new Color(0.79f, 0.82f, 0.86f), FontStyle.Normal);

            SetVisible(false);
        }

        private RectTransform CreateUIRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            int fontSize, TextAnchor alignment, Color color, FontStyle fontStyle)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = obj.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = fontStyle;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return text;
        }
    }
}
