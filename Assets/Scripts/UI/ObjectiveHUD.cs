using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;

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
            if (DayObjectiveSystem.Instance != null)
            {
                DayObjectiveSystem.Instance.OnObjectiveGenerated += OnObjectiveGenerated;
                DayObjectiveSystem.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
                DayObjectiveSystem.Instance.OnObjectiveCompleted += OnObjectiveCompleted;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (DayObjectiveSystem.Instance != null)
            {
                DayObjectiveSystem.Instance.OnObjectiveGenerated -= OnObjectiveGenerated;
                DayObjectiveSystem.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
                DayObjectiveSystem.Instance.OnObjectiveCompleted -= OnObjectiveCompleted;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.DayPhase)
            {
                if (DayObjectiveSystem.Instance?.ActiveObjective != null)
                    ShowObjective(DayObjectiveSystem.Instance.ActiveObjective);
            }
            else
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        private void OnObjectiveGenerated(DayObjective obj)
        {
            ShowObjective(obj);
        }

        private void OnObjectiveUpdated(DayObjective obj)
        {
            if (obj == null)
            {
                if (panel != null) panel.SetActive(false);
                return;
            }
            UpdateDisplay(obj);
        }

        private void OnObjectiveCompleted(DayObjective obj)
        {
            if (descText != null)
                descText.text = $"{obj.title} - COMPLETE!";
            if (progressText != null)
            {
                progressText.text = "DONE";
                progressText.color = new Color(0.3f, 1f, 0.3f);
            }
        }

        private void ShowObjective(DayObjective obj)
        {
            if (panel == null || obj == null) return;
            panel.SetActive(true);
            UpdateDisplay(obj);
        }

        private void UpdateDisplay(DayObjective obj)
        {
            if (descText != null)
                descText.text = obj.title;
            if (progressText != null)
            {
                progressText.text = $"{obj.progress}/{obj.targetCount}";
                progressText.color = obj.IsComplete ? new Color(0.3f, 1f, 0.3f) : new Color(0.4f, 1f, 0.4f);
            }
        }
    }
}
