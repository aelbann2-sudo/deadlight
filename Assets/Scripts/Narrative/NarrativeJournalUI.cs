using System.Collections.Generic;
using System.Text;
using Deadlight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.Narrative
{
    public class NarrativeJournalUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private KeyCode toggleKey = KeyCode.J;
        [SerializeField] private KeyCode previousLoreKey = KeyCode.LeftBracket;
        [SerializeField] private KeyCode nextLoreKey = KeyCode.RightBracket;

        private GameObject root;
        private Text headerText;
        private Text objectiveText;
        private Text storyLogText;
        private Text loreListText;
        private Text loreTitleText;
        private Text loreContentText;
        private Text footerText;
        private Font font;

        private EnvironmentalLore boundLore;
        private StoryObjective boundStory;
        private GameManager boundGameManager;
        private int selectedLoreIndex = -1;
        private bool isVisible;
        private bool isDirty = true;

        private void Awake()
        {
            LoadFont();
            BuildUI();
            SetVisible(false);
        }

        private void Start()
        {
            RebindSources();
            RefreshContent();
        }

        private void OnDestroy()
        {
            UnbindSources();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetVisible(!isVisible);
            }

            if (!isVisible)
            {
                if (boundLore == null || boundStory == null || boundGameManager == null)
                {
                    RebindSources();
                }
                return;
            }

            if (Input.GetKeyDown(previousLoreKey))
            {
                MoveSelection(-1);
            }
            else if (Input.GetKeyDown(nextLoreKey))
            {
                MoveSelection(1);
            }

            if (boundLore == null || boundStory == null || boundGameManager == null)
            {
                RebindSources();
            }

            if (isDirty)
            {
                RefreshContent();
            }
        }

        private void LoadFont()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
        }

        private void BuildUI()
        {
            root = new GameObject("NarrativeJournalCanvas");
            root.transform.SetParent(transform, false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 220;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();

            GameObject backdrop = CreatePanel(root.transform, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image backdropImage = backdrop.AddComponent<Image>();
            backdropImage.color = new Color(0.01f, 0.01f, 0.01f, 0.82f);

            GameObject frame = CreatePanel(root.transform, "Frame", new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            Image frameImage = frame.AddComponent<Image>();
            frameImage.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

            CreateBorder(frame.transform, new Color(1f, 0.78f, 0.25f, 0.55f));

            headerText = CreateText(frame.transform, "Header", 30, TextAnchor.UpperLeft, new Color(1f, 0.9f, 0.7f));
            SetRect(headerText.rectTransform, new Vector2(0.03f, 0.9f), new Vector2(0.97f, 0.98f), Vector2.zero, Vector2.zero);
            headerText.fontStyle = FontStyle.Bold;

            objectiveText = CreateText(frame.transform, "Objective", 19, TextAnchor.UpperLeft, new Color(0.86f, 0.9f, 0.95f));
            SetRect(objectiveText.rectTransform, new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            objectiveText.verticalOverflow = VerticalWrapMode.Overflow;

            Text storyLabel = CreateText(frame.transform, "StoryLabel", 16, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.25f));
            storyLabel.text = "MISSION LOG";
            SetRect(storyLabel.rectTransform, new Vector2(0.03f, 0.66f), new Vector2(0.45f, 0.71f), Vector2.zero, Vector2.zero);

            storyLogText = CreateText(frame.transform, "StoryLog", 16, TextAnchor.UpperLeft, Color.white);
            SetRect(storyLogText.rectTransform, new Vector2(0.03f, 0.36f), new Vector2(0.45f, 0.66f), Vector2.zero, Vector2.zero);
            storyLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            storyLogText.verticalOverflow = VerticalWrapMode.Overflow;

            Text loreListLabel = CreateText(frame.transform, "LoreListLabel", 16, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.25f));
            loreListLabel.text = "RECOVERED DOCUMENTS";
            SetRect(loreListLabel.rectTransform, new Vector2(0.03f, 0.3f), new Vector2(0.45f, 0.35f), Vector2.zero, Vector2.zero);

            loreListText = CreateText(frame.transform, "LoreList", 15, TextAnchor.UpperLeft, new Color(0.88f, 0.88f, 0.88f));
            SetRect(loreListText.rectTransform, new Vector2(0.03f, 0.1f), new Vector2(0.45f, 0.3f), Vector2.zero, Vector2.zero);
            loreListText.horizontalOverflow = HorizontalWrapMode.Wrap;
            loreListText.verticalOverflow = VerticalWrapMode.Overflow;

            loreTitleText = CreateText(frame.transform, "LoreTitle", 20, TextAnchor.UpperLeft, new Color(0.92f, 0.95f, 0.78f));
            SetRect(loreTitleText.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.95f, 0.71f), Vector2.zero, Vector2.zero);
            loreTitleText.fontStyle = FontStyle.Bold;

            loreContentText = CreateText(frame.transform, "LoreContent", 17, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.9f));
            SetRect(loreContentText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(0.95f, 0.6f), Vector2.zero, Vector2.zero);
            loreContentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            loreContentText.verticalOverflow = VerticalWrapMode.Overflow;

            footerText = CreateText(frame.transform, "Footer", 15, TextAnchor.MiddleRight, new Color(0.72f, 0.72f, 0.72f));
            SetRect(footerText.rectTransform, new Vector2(0.5f, 0.03f), new Vector2(0.95f, 0.08f), Vector2.zero, Vector2.zero);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (root != null)
            {
                root.SetActive(visible);
            }

            if (visible)
            {
                isDirty = true;
                RebindSources();
                RefreshContent();
            }
        }

        private void RebindSources()
        {
            if (boundLore != EnvironmentalLore.Instance)
            {
                if (boundLore != null)
                {
                    boundLore.OnLoreDiscovered -= HandleLoreDiscovered;
                }

                boundLore = EnvironmentalLore.Instance;
                if (boundLore != null)
                {
                    boundLore.OnLoreDiscovered += HandleLoreDiscovered;
                }
            }

            if (boundStory != StoryObjective.Instance)
            {
                if (boundStory != null)
                {
                    boundStory.OnObjectiveChanged -= HandleStoryChanged;
                }

                boundStory = StoryObjective.Instance;
                if (boundStory != null)
                {
                    boundStory.OnObjectiveChanged += HandleStoryChanged;
                }
            }

            if (boundGameManager != GameManager.Instance)
            {
                if (boundGameManager != null)
                {
                    boundGameManager.OnGameStateChanged -= HandleGameStateChanged;
                    boundGameManager.OnNightChanged -= HandleNightChanged;
                }

                boundGameManager = GameManager.Instance;
                if (boundGameManager != null)
                {
                    boundGameManager.OnGameStateChanged += HandleGameStateChanged;
                    boundGameManager.OnNightChanged += HandleNightChanged;
                }
            }

            isDirty = true;
        }

        private void UnbindSources()
        {
            if (boundLore != null)
            {
                boundLore.OnLoreDiscovered -= HandleLoreDiscovered;
            }

            if (boundStory != null)
            {
                boundStory.OnObjectiveChanged -= HandleStoryChanged;
            }

            if (boundGameManager != null)
            {
                boundGameManager.OnGameStateChanged -= HandleGameStateChanged;
                boundGameManager.OnNightChanged -= HandleNightChanged;
            }
        }

        private void HandleLoreDiscovered(LoreEntry entry)
        {
            if (boundLore != null)
            {
                selectedLoreIndex = boundLore.GetDiscoveredLore().Count - 1;
            }

            isDirty = true;
        }

        private void HandleStoryChanged()
        {
            isDirty = true;
        }

        private void HandleGameStateChanged(GameState state)
        {
            isDirty = true;
        }

        private void HandleNightChanged(int night)
        {
            isDirty = true;
        }

        private void MoveSelection(int direction)
        {
            if (boundLore == null)
            {
                return;
            }

            int count = boundLore.GetDiscoveredLore().Count;
            if (count == 0)
            {
                selectedLoreIndex = -1;
                isDirty = true;
                return;
            }

            if (selectedLoreIndex < 0)
            {
                selectedLoreIndex = 0;
            }
            else
            {
                selectedLoreIndex = (selectedLoreIndex + direction + count) % count;
            }

            isDirty = true;
        }

        private void RefreshContent()
        {
            isDirty = false;

            if (headerText == null)
            {
                return;
            }

            MapType map = boundGameManager != null ? boundGameManager.SelectedMap : MapType.TownCenter;
            int level = boundGameManager != null ? boundGameManager.CurrentLevel : 1;
            int nightWithinLevel = boundGameManager != null ? boundGameManager.NightWithinLevel : 1;
            string stateLabel = boundGameManager != null ? boundGameManager.CurrentState.ToString() : "Unknown";
            headerText.text = $"FIELD JOURNAL // {GetMapLabel(map)} // LEVEL {level} // NIGHT {nightWithinLevel} // {stateLabel.ToUpperInvariant()}";

            objectiveText.text = BuildObjectiveSection();
            storyLogText.text = BuildStoryLog();
            loreListText.text = BuildLoreList();
            BuildLoreContent();
            footerText.text = "J close  |  [ previous doc  |  ] next doc";
        }

        private string BuildObjectiveSection()
        {
            if (boundStory == null || !boundStory.HasActiveObjective)
            {
                return "No active story directive. Stay alive and keep searching.";
            }

            string phase = boundStory.CurrentPhase == StoryObjectivePhase.DayInvestigation ? "DAY LEAD" : "NIGHT DIRECTIVE";
            return $"{phase}\n{boundStory.CurrentTitle}\n{boundStory.CurrentDescription}\nStatus: {boundStory.CurrentStatus}";
        }

        private string BuildStoryLog()
        {
            if (boundStory == null || boundStory.CompletedBeats.Count == 0)
            {
                return "No confirmed leads yet.\n\nFollow the active directive to recover proof of what happened in this district.";
            }

            StringBuilder builder = new StringBuilder();
            IReadOnlyList<StoryBeatRecord> records = boundStory.CompletedBeats;

            for (int i = 0; i < records.Count; i++)
            {
                StoryBeatRecord record = records[i];
                int level = GameManager.GetLevelForNight(record.Night);
                int nightWithinLevel = GameManager.GetNightWithinLevel(record.Night);
                builder.Append("Level ");
                builder.Append(level);
                builder.Append(", Night ");
                builder.Append(nightWithinLevel);
                builder.Append(": ");
                builder.AppendLine(record.Title);
                builder.AppendLine(record.Summary);

                if (i < records.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private string BuildLoreList()
        {
            if (boundLore == null)
            {
                return "Lore database offline.";
            }

            var discovered = boundLore.GetDiscoveredLore();
            if (discovered.Count == 0)
            {
                selectedLoreIndex = -1;
                return "No recovered documents yet.\n\nLook for glowing pickups and search the landmarks.";
            }

            selectedLoreIndex = Mathf.Clamp(selectedLoreIndex < 0 ? discovered.Count - 1 : selectedLoreIndex, 0, discovered.Count - 1);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < discovered.Count; i++)
            {
                builder.Append(i == selectedLoreIndex ? "> " : "  ");
                builder.Append(discovered[i].title);
                if (i < discovered.Count - 1)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private void BuildLoreContent()
        {
            if (boundLore == null)
            {
                loreTitleText.text = "NO SIGNAL";
                loreContentText.text = "Lore systems are not available yet.";
                return;
            }

            var discovered = boundLore.GetDiscoveredLore();
            if (discovered.Count == 0 || selectedLoreIndex < 0)
            {
                loreTitleText.text = "NO DOCUMENT SELECTED";
                loreContentText.text = "Recovered files appear here as you explore.\n\nUse the journal during the day to connect landmarks, logs, and the rescue plan.";
                return;
            }

            selectedLoreIndex = Mathf.Clamp(selectedLoreIndex, 0, discovered.Count - 1);
            LoreEntry selected = discovered[selectedLoreIndex];
            loreTitleText.text = $"{selected.title} [{selected.category}]";
            loreContentText.text = selected.content;
        }

        private static string GetMapLabel(MapType map)
        {
            switch (map)
            {
                case MapType.Industrial:
                    return "Industrial District";
                case MapType.Suburban:
                    return "Suburban Outskirts";
                case MapType.Research:
                    return "Research Complex";
                default:
                    return "Town Center";
            }
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return panel;
        }

        private static void CreateBorder(Transform parent, Color color)
        {
            CreateBorderEdge(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -3f), new Vector2(0f, 0f), color);
            CreateBorderEdge(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 3f), color);
            CreateBorderEdge(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(3f, 0f), color);
            CreateBorderEdge(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-3f, 0f), new Vector2(0f, 0f), color);
        }

        private static void CreateBorderEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            GameObject edge = new GameObject(name);
            edge.transform.SetParent(parent, false);
            RectTransform rect = edge.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            Image image = edge.AddComponent<Image>();
            image.color = color;
        }

        private Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
