using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Narrative;

namespace Deadlight.UI
{
    public class ObjectiveHUD : MonoBehaviour
    {
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
                progressText.color = story.IsComplete
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(1f, 0.85f, 0.35f);
            }
        }
    }
}
