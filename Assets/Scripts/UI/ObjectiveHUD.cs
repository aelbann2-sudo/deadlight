using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Narrative;

namespace Deadlight.UI
{
    public class ObjectiveHUD : MonoBehaviour
    {
        private static readonly Color ProgressColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color CompleteColor = new Color(0.3f, 1f, 0.3f);
        private static readonly Color DangerColor = new Color(1f, 0.42f, 0.38f);

        private GameObject panel;
        private Text descText;
        private Text progressText;

        public void Initialize(GameObject panelObj, Text desc, Text progress)
        {
            panel = panelObj;
            descText = desc;
            progressText = progress;
        }

        private void Start()
        {
            if (StoryObjective.Instance != null)
                StoryObjective.Instance.OnObjectiveChanged += RefreshFromStory;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            RefreshFromStory();
        }

        private void OnDestroy()
        {
            if (StoryObjective.Instance != null)
                StoryObjective.Instance.OnObjectiveChanged -= RefreshFromStory;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.DayPhase || state == GameState.NightPhase)
            {
                RefreshFromStory();
            }
            else
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        private void RefreshFromStory()
        {
            if (panel == null) return;

            var story = StoryObjective.Instance;
            if (story == null || !story.HasActiveObjective || string.IsNullOrEmpty(story.CurrentTitle))
            {
                panel.SetActive(false);
                return;
            }

            panel.SetActive(true);

            if (descText != null)
                descText.text = story.CurrentTitle;

            if (progressText != null)
            {
                progressText.text = story.CurrentStatus;
                progressText.color = ResolveStatusColor(story);
            }

            ApplyPanelStateTint(story);
        }

        private static Color ResolveStatusColor(StoryObjective story)
        {
            if (story == null)
            {
                return ProgressColor;
            }

            if (story.IsComplete)
            {
                return CompleteColor;
            }

            return story.CurrentPhase == StoryObjectivePhase.NightSurvival
                ? DangerColor
                : ProgressColor;
        }

        private void ApplyPanelStateTint(StoryObjective story)
        {
            if (panel == null || story == null)
            {
                return;
            }

            var panelImage = panel.GetComponent<Image>();
            if (panelImage == null)
            {
                return;
            }

            float alpha = panelImage.color.a > 0.01f ? panelImage.color.a : 0.84f;
            Color tint = story.IsComplete
                ? new Color(0.08f, 0.20f, 0.12f, alpha)
                : story.CurrentPhase == StoryObjectivePhase.NightSurvival
                    ? new Color(0.22f, 0.06f, 0.06f, alpha)
                    : new Color(0.18f, 0.14f, 0.08f, alpha);

            panelImage.color = tint;
        }
    }
}
