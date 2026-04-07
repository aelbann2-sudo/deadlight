using System.Collections;
using UnityEngine;
using Deadlight.Core;
using Deadlight.Narrative;
using Deadlight.Systems;

namespace Deadlight.UI
{
    /// <summary>
    /// Until the player completes one daylight story lead and one drop tutorial target, periodically
    /// reminds them to follow amber OBJ and cyan DROP edge markers. Drop = contested secure when that
    /// feature is on; otherwise any supply crate loot counts.
    /// </summary>
    public class SupportMarkerGuidanceController : MonoBehaviour
    {
        public static SupportMarkerGuidanceController Instance { get; private set; }

        private const string PrefKey = "Deadlight_SupportMarkerGuidanceComplete";

        [SerializeField] private float firstHintDelay = 2.5f;
        [SerializeField] private float repeatIntervalSeconds = 42f;
        [SerializeField] private float commsDuration = 9f;
        [SerializeField] private float statusSeconds = 7f;

        private bool objectiveLeadSecured;
        private bool contestedDropSecured;
        private bool anyCrateLooted;

        private Coroutine dayLoopCoroutine;
        private GameFlowController flowController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                return;
            }

            flowController = FindFirstObjectByType<GameFlowController>();
            if (flowController != null)
            {
                flowController.OnContestedDropStateChanged += OnContestedDropStateChanged;
            }

            SupplyCrate.OnCrateSuccessfullyLooted += OnCrateSuccessfullyLooted;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            if (StoryObjective.Instance != null)
            {
                HookStoryObjective();
            }
            else
            {
                StartCoroutine(DeferredStoryHook());
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.DayPhase)
            {
                StartDayLoopIfNeeded();
            }
        }

        private void OnDestroy()
        {
            if (flowController != null)
            {
                flowController.OnContestedDropStateChanged -= OnContestedDropStateChanged;
            }

            SupplyCrate.OnCrateSuccessfullyLooted -= OnCrateSuccessfullyLooted;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            if (storyObjectiveHooked && StoryObjective.Instance != null)
            {
                StoryObjective.Instance.OnObjectiveChanged -= OnObjectiveChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool storyObjectiveHooked;

        private void HookStoryObjective()
        {
            if (storyObjectiveHooked || StoryObjective.Instance == null)
            {
                return;
            }

            storyObjectiveHooked = true;
            StoryObjective.Instance.OnObjectiveChanged += OnObjectiveChanged;
            EvaluateObjectiveImmediate();
        }

        private IEnumerator DeferredStoryHook()
        {
            for (int i = 0; i < 40 && StoryObjective.Instance == null; i++)
            {
                yield return new WaitForSeconds(0.25f);
            }

            if (PlayerPrefs.GetInt(PrefKey, 0) == 0)
            {
                HookStoryObjective();
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                return;
            }

            if (state == GameState.DayPhase)
            {
                StartDayLoopIfNeeded();
            }
            else
            {
                StopDayLoop();
            }
        }

        private void OnObjectiveChanged()
        {
            EvaluateObjectiveImmediate();
            TryCompleteTutorial();
        }

        private void EvaluateObjectiveImmediate()
        {
            var so = StoryObjective.Instance;
            if (so == null)
            {
                return;
            }

            if (so.CurrentPhase == StoryObjectivePhase.DayInvestigation && so.IsComplete)
            {
                objectiveLeadSecured = true;
            }
        }

        private void OnContestedDropStateChanged(DayContestedDropState state, float _)
        {
            if (state == DayContestedDropState.Resolved)
            {
                contestedDropSecured = true;
                TryCompleteTutorial();
            }
        }

        private void OnCrateSuccessfullyLooted(SupplyCrate crate)
        {
            if (crate == null)
            {
                return;
            }

            bool contestedOn = flowController != null && flowController.DayContestedDropsEnabled;
            if (contestedOn)
            {
                if (crate.IsContestedSupplyDrop)
                {
                    contestedDropSecured = true;
                }
            }
            else
            {
                anyCrateLooted = true;
            }

            TryCompleteTutorial();
        }

        private bool DropRequirementMet()
        {
            bool contestedOn = flowController != null && flowController.DayContestedDropsEnabled;
            if (contestedOn)
            {
                return contestedDropSecured;
            }

            return anyCrateLooted;
        }

        private void TryCompleteTutorial()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                return;
            }

            if (!objectiveLeadSecured || !DropRequirementMet())
            {
                return;
            }

            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            StopDayLoop();

            NarrativeManager.Instance?.QueueSystemMessage(
                "COMMS",
                "Solid work—those edge markers will keep guiding you through the rest of the run.",
                5f,
                interrupt: false,
                playRadioStatic: false);
        }

        private void StartDayLoopIfNeeded()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
            {
                return;
            }

            if (objectiveLeadSecured && DropRequirementMet())
            {
                TryCompleteTutorial();
                return;
            }

            if (dayLoopCoroutine != null)
            {
                return;
            }

            dayLoopCoroutine = StartCoroutine(DayGuidanceLoop());
        }

        private void StopDayLoop()
        {
            if (dayLoopCoroutine != null)
            {
                StopCoroutine(dayLoopCoroutine);
                dayLoopCoroutine = null;
            }
        }

        private IEnumerator DayGuidanceLoop()
        {
            yield return new WaitForSeconds(firstHintDelay);
            while (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.DayPhase &&
                   PlayerPrefs.GetInt(PrefKey, 0) == 0)
            {
                if (objectiveLeadSecured && DropRequirementMet())
                {
                    TryCompleteTutorial();
                    dayLoopCoroutine = null;
                    yield break;
                }

                PushGuidanceMessage();
                yield return new WaitForSeconds(repeatIntervalSeconds);
            }

            dayLoopCoroutine = null;
        }

        private void PushGuidanceMessage()
        {
            bool needObj = !objectiveLeadSecured;
            bool needDrop = !DropRequirementMet();

            string comms;
            string status;
            if (needObj && needDrop)
            {
                comms =
                    "Follow the amber OBJ markers at the screen edge to your daylight lead. Cyan DROP markers track contested supplies—move in and hold F to secure.";
                status = "Edge markers: amber OBJ = story lead  •  cyan DROP = contested crate";
            }
            else if (needObj)
            {
                comms =
                    "Your story lead still needs the amber OBJ marker—keep it on-screen and move toward it before nightfall.";
                status = "Follow the amber OBJ marker to complete the daylight objective.";
            }
            else
            {
                comms =
                    "Watch for cyan DROP markers during contested supply events—interact in zone and hold F until the crate is secured.";
                status = "Cyan DROP = contested supply—reach it and hold F to secure.";
            }

            NarrativeManager.Instance?.QueueSystemMessage(
                "COMMS",
                comms,
                commsDuration,
                interrupt: false,
                playRadioStatic: false);

            FindFirstObjectByType<GameplayHUD>()?.ShowTransientStatus(status, statusSeconds);
        }
    }
}
