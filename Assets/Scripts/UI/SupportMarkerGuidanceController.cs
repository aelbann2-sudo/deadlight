using System.Collections;
using Deadlight.Core;
using Deadlight.Narrative;
using Deadlight.Systems;
using UnityEngine;

namespace Deadlight.UI
{
    /// <summary>
    /// Early daylight onboarding:
    /// - teach one objective action (up to maxObjectiveHints)
    /// - teach one drop action (up to maxDropHints)
    /// Stops once both requirements are completed for the run.
    /// </summary>
    public class SupportMarkerGuidanceController : MonoBehaviour
    {
        public static SupportMarkerGuidanceController Instance { get; private set; }

        [SerializeField] [Range(5f, 10f)] private float firstHintDelay = 7f;
        [SerializeField] private float repeatIntervalSeconds = 28f;
        [SerializeField] [Range(2f, 3f)] private float commsDuration = 2.6f;
        [SerializeField] [Range(2f, 3f)] private float statusSeconds = 2.6f;
        [SerializeField] private float minHintSpacingSeconds = 5.2f;
        [SerializeField] private float runtimeDropGuidePollSeconds = 0.25f;
        [SerializeField] [Range(1f, 6f)] private float dayOneDropGuideDelayAfterObjectiveSeconds = 2.4f;
        [SerializeField] [Range(0.5f, 4f)] private float dayOneDropPromptDelayAfterGuideSeconds = 1.2f;
        [SerializeField] private float dropInteractionPromptRange = 2.3f;
        [SerializeField] [Range(1, 4)] private int maxObjectiveHints = 2;
        [SerializeField] [Range(1, 4)] private int maxDropHints = 2;

        private bool objectiveLeadSecured;
        private bool contestedDropSecured;
        private bool anyCrateLooted;
        private bool dayOneDropGuideShown;
        private bool dayOneDropInteractionPromptShown;
        private bool tutorialComplete;
        private int objectiveHintsShown;
        private int dropHintsShown;
        private bool pendingDayOneDropGuidance;
        private DayContestedDropState latestDropState = DayContestedDropState.Inactive;
        private float lastObjectiveHintTime = -999f;
        private float lastDropHintTime = -999f;
        private float guidanceUnlockedAt = float.PositiveInfinity;
        private float dayOneDropGuideUnlockedAt = float.PositiveInfinity;
        private float nextRuntimeDropGuidePollAt = float.NegativeInfinity;

        private Coroutine dayLoopCoroutine;
        private GameFlowController flowController;
        private bool storyObjectiveHooked;
        private bool dayObjectiveHooked;

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

            if (DayObjectiveSystem.Instance != null)
            {
                HookDayObjectiveSystem();
            }
            else
            {
                StartCoroutine(DeferredDayObjectiveHook());
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.DayPhase)
            {
                if (GameManager.Instance.CurrentNight == 1)
                {
                    ResetRunTutorialState();
                    ArmGuidanceDelayWindow();
                    StartDayLoopIfNeeded();
                }
                else
                {
                    StopDayLoop();
                }
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

            if (dayObjectiveHooked && DayObjectiveSystem.Instance != null)
            {
                DayObjectiveSystem.Instance.OnObjectiveUpdated -= OnDayObjectiveUpdated;
                DayObjectiveSystem.Instance.OnObjectiveCompleted -= OnDayObjectiveCompleted;
            }

            StopDayLoop();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!IsDayPhaseActive() || tutorialComplete || !IsDayOne())
            {
                return;
            }

            if (Time.time < nextRuntimeDropGuidePollAt)
            {
                return;
            }

            nextRuntimeDropGuidePollAt = Time.time + Mathf.Max(0.1f, runtimeDropGuidePollSeconds);

            EvaluateObjectiveImmediate();

            if (ShouldRequireObjectiveBeforeDropGuidance() && !objectiveLeadSecured)
            {
                return;
            }

            if (!DropRequirementMet())
            {
                if (!dayOneDropGuideShown)
                {
                    bool showed = TryShowDropGuidanceHint(bypassDelayWindow: true);
                    if (showed)
                    {
                        pendingDayOneDropGuidance = false;
                    }
                }

                MaybeShowDropInteractionPrompt();
            }
        }

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

            HookStoryObjective();
        }

        private void HookDayObjectiveSystem()
        {
            if (dayObjectiveHooked || DayObjectiveSystem.Instance == null)
            {
                return;
            }

            dayObjectiveHooked = true;
            DayObjectiveSystem.Instance.OnObjectiveUpdated += OnDayObjectiveUpdated;
            DayObjectiveSystem.Instance.OnObjectiveCompleted += OnDayObjectiveCompleted;
            EvaluateObjectiveImmediate();
        }

        private IEnumerator DeferredDayObjectiveHook()
        {
            for (int i = 0; i < 40 && DayObjectiveSystem.Instance == null; i++)
            {
                yield return new WaitForSeconds(0.25f);
            }

            HookDayObjectiveSystem();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.DayPhase)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentNight == 1)
                {
                    ResetRunTutorialState();
                    EvaluateObjectiveImmediate();
                    latestDropState = DayContestedDropState.Inactive;
                    pendingDayOneDropGuidance = false;
                    ArmGuidanceDelayWindow();
                    StartDayLoopIfNeeded();
                }
                else
                {
                    StopDayLoop();
                }
            }
            else
            {
                StopDayLoop();
            }
        }

        private void OnObjectiveChanged()
        {
            if (!IsDayOne())
            {
                return;
            }

            EvaluateObjectiveImmediate();
            if (IsDayPhaseActive())
            {
                TryShowObjectiveGuidanceHint();
                if (ShouldRequireObjectiveBeforeDropGuidance() && objectiveLeadSecured)
                {
                    bool showed = TryShowDropGuidanceHint();
                    if (showed || DropRequirementMet())
                    {
                        pendingDayOneDropGuidance = false;
                    }
                }
            }
            TryCompleteTutorial();
        }

        private void OnDayObjectiveUpdated(DayObjective _)
        {
            HandleObjectiveProgressRefresh();
        }

        private void OnDayObjectiveCompleted(DayObjective _)
        {
            HandleObjectiveProgressRefresh();
        }

        private void HandleObjectiveProgressRefresh()
        {
            if (!IsDayOne())
            {
                return;
            }

            EvaluateObjectiveImmediate();
            if (!IsDayPhaseActive())
            {
                return;
            }

            if (ShouldRequireObjectiveBeforeDropGuidance() && objectiveLeadSecured)
            {
                bool showed = TryShowDropGuidanceHint();
                if (showed || DropRequirementMet())
                {
                    pendingDayOneDropGuidance = false;
                }
            }

            TryCompleteTutorial();
        }

        private void EvaluateObjectiveImmediate()
        {
            bool wasObjectiveLeadSecured = objectiveLeadSecured;
            bool completed = false;

            var so = StoryObjective.Instance;
            if (so != null &&
                so.CurrentPhase == StoryObjectivePhase.DayInvestigation &&
                so.IsComplete)
            {
                completed = true;
            }

            var dayObjective = DayObjectiveSystem.Instance != null
                ? DayObjectiveSystem.Instance.ActiveObjective
                : null;
            if (dayObjective != null && dayObjective.IsComplete)
            {
                completed = true;
            }

            objectiveLeadSecured = completed;

            if (IsDayOne())
            {
                if (!wasObjectiveLeadSecured && objectiveLeadSecured)
                {
                    dayOneDropGuideUnlockedAt = Time.time + Mathf.Max(0f, dayOneDropGuideDelayAfterObjectiveSeconds);
                }
                else if (!objectiveLeadSecured)
                {
                    dayOneDropGuideUnlockedAt = float.PositiveInfinity;
                }
            }
        }

        private void OnContestedDropStateChanged(DayContestedDropState state, float _)
        {
            if (!IsDayOne())
            {
                return;
            }

            latestDropState = state;

            if (state == DayContestedDropState.Resolved)
            {
                contestedDropSecured = true;
                TryCompleteTutorial();
                return;
            }

            if (state == DayContestedDropState.Secure && IsDayPhaseActive() && objectiveLeadSecured)
            {
                ShowDropSecureHoldStatus();
            }

            if (state == DayContestedDropState.Broadcast ||
                state == DayContestedDropState.Descent ||
                state == DayContestedDropState.Secure)
            {
                if (ShouldRequireObjectiveBeforeDropGuidance() && !objectiveLeadSecured)
                {
                    pendingDayOneDropGuidance = true;
                    return;
                }

                bool showed = TryShowDropGuidanceHint();
                if (showed || DropRequirementMet())
                {
                    pendingDayOneDropGuidance = false;
                }
            }
        }

        private void OnCrateSuccessfullyLooted(SupplyCrate crate)
        {
            if (crate == null)
            {
                return;
            }

            if (!IsDayOne() || !IsDayPhaseActive())
            {
                return;
            }

            if (crate.IsContestedSupplyDrop)
            {
                contestedDropSecured = true;
            }
            else
            {
                anyCrateLooted = true;
            }

            TryCompleteTutorial();
        }

        private bool DropRequirementMet()
        {
            return contestedDropSecured || anyCrateLooted;
        }

        private bool IsDayPhaseActive()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.DayPhase;
        }

        private void ResetRunTutorialState()
        {
            objectiveLeadSecured = false;
            contestedDropSecured = false;
            anyCrateLooted = false;
            dayOneDropGuideShown = false;
            dayOneDropInteractionPromptShown = false;
            tutorialComplete = false;
            objectiveHintsShown = 0;
            dropHintsShown = 0;
            pendingDayOneDropGuidance = false;
            latestDropState = DayContestedDropState.Inactive;
            lastObjectiveHintTime = -999f;
            lastDropHintTime = -999f;
            guidanceUnlockedAt = float.PositiveInfinity;
            dayOneDropGuideUnlockedAt = float.PositiveInfinity;
            nextRuntimeDropGuidePollAt = float.NegativeInfinity;
        }

        private void TryCompleteTutorial()
        {
            if (tutorialComplete)
            {
                return;
            }

            if (!objectiveLeadSecured || !DropRequirementMet())
            {
                return;
            }

            tutorialComplete = true;
            StopDayLoop();
        }

        private void StartDayLoopIfNeeded()
        {
            if (tutorialComplete)
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
            yield return new WaitForSeconds(Mathf.Clamp(firstHintDelay, 5f, 10f));
            while (IsDayPhaseActive() && !tutorialComplete)
            {
                if (objectiveLeadSecured && DropRequirementMet())
                {
                    TryCompleteTutorial();
                    dayLoopCoroutine = null;
                    yield break;
                }

                if (!IsDayOneCombatTutorialComplete())
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                bool showedObjective = TryShowObjectiveGuidanceHint();
                bool showedDrop = false;
                if (!showedObjective)
                {
                    showedDrop = TryShowDropGuidanceHint();
                }

                if (!showedDrop &&
                    pendingDayOneDropGuidance &&
                    objectiveLeadSecured &&
                    CanPromptDropNow())
                {
                    showedDrop = TryShowDropGuidanceHint();
                    if (showedDrop || DropRequirementMet())
                    {
                        pendingDayOneDropGuidance = false;
                    }
                }

                bool objectiveWindowClosed = objectiveLeadSecured || objectiveHintsShown >= Mathf.Max(1, maxObjectiveHints);
                bool dropWindowClosed = DropRequirementMet() || dropHintsShown >= Mathf.Max(1, maxDropHints);
                if (!showedObjective && !showedDrop && objectiveWindowClosed && dropWindowClosed)
                {
                    dayLoopCoroutine = null;
                    yield break;
                }

                yield return new WaitForSeconds(repeatIntervalSeconds);
            }

            dayLoopCoroutine = null;
        }

        private bool TryShowObjectiveGuidanceHint()
        {
            if (tutorialComplete || objectiveLeadSecured)
            {
                return false;
            }

            if (!CanShowGuidanceNow())
            {
                return false;
            }

            if (!IsDayOneCombatTutorialComplete())
            {
                return false;
            }

            int maxHints = Mathf.Max(1, maxObjectiveHints);
            if (objectiveHintsShown >= maxHints)
            {
                return false;
            }

            if (Time.time - lastObjectiveHintTime < Mathf.Max(0.5f, minHintSpacingSeconds))
            {
                return false;
            }

            objectiveHintsShown++;
            lastObjectiveHintTime = Time.time;

            if (ShouldSendObjectiveGuidanceInComms())
            {
                string comms = BuildObjectiveComms(objectiveHintsShown);
                SendGuidanceRadio(comms);
            }

            FindFirstObjectByType<GameplayHUD>()?.ShowTransientStatus(
                BuildObjectiveStatusLine(),
                statusSeconds);

            return true;
        }

        private bool TryShowDropGuidanceHint()
        {
            return TryShowDropGuidanceHint(bypassDelayWindow: false);
        }

        private bool TryShowDropGuidanceHint(bool bypassDelayWindow)
        {
            if (tutorialComplete || DropRequirementMet())
            {
                return false;
            }

            if (ShouldRequireObjectiveBeforeDropGuidance() && !objectiveLeadSecured)
            {
                return false;
            }

            if (!bypassDelayWindow && !CanShowGuidanceNow())
            {
                return false;
            }

            if (!IsDayOneCombatTutorialComplete())
            {
                return false;
            }

            int maxHints = GetMaxDropHintsForCurrentContext();
            if (dropHintsShown >= maxHints)
            {
                return false;
            }

            if (!bypassDelayWindow && Time.time - lastDropHintTime < Mathf.Max(0.5f, minHintSpacingSeconds))
            {
                return false;
            }

            if (!CanPromptDropNow())
            {
                return false;
            }

            if (IsDayOne() && objectiveLeadSecured && Time.time < dayOneDropGuideUnlockedAt)
            {
                return false;
            }

            dropHintsShown++;
            lastDropHintTime = Time.time;

            bool contestedOn = IsContestedDropActiveNow();
            if (ShouldSendDropGuidanceInComms())
            {
                string comms = BuildDropComms(contestedOn, dropHintsShown);
                SendGuidanceRadio(comms);
            }

            string status = contestedOn
                ? "Follow the cyan marker to the secure drop zone"
                : "Follow the cyan marker to the supply crate";
            FindFirstObjectByType<GameplayHUD>()?.ShowTransientStatus(status, statusSeconds);

            if (IsDayOne())
            {
                dayOneDropGuideShown = true;
            }

            return true;
        }

        private bool ShouldRequireObjectiveBeforeDropGuidance()
        {
            return GameManager.Instance != null &&
                   GameManager.Instance.CurrentNight == 1;
        }

        private bool IsDayOne()
        {
            return GameManager.Instance != null &&
                   GameManager.Instance.CurrentNight == 1;
        }

        private int GetMaxDropHintsForCurrentContext()
        {
            // Day 1 onboarding: one clear drop instruction, then interaction prompt handles the rest.
            return IsDayOne() ? 1 : Mathf.Max(1, maxDropHints);
        }

        private bool ShouldSendObjectiveGuidanceInComms()
        {
            // Day 1 objective guidance should stay on-screen only so COMMS stays focused on combat onboarding.
            return GameManager.Instance == null || GameManager.Instance.CurrentNight != 1;
        }

        private bool ShouldSendDropGuidanceInComms()
        {
            // Day 1 drop guidance should stay on-screen only to avoid message pile-up while learning.
            return GameManager.Instance == null || GameManager.Instance.CurrentNight != 1;
        }

        private bool CanPromptDropNow()
        {
            if (IsContestedDropActiveNow())
            {
                return true;
            }

            return FindFirstObjectByType<SupplyCrate>() != null;
        }

        private bool IsContestedDropActiveNow()
        {
            return latestDropState == DayContestedDropState.Broadcast ||
                   latestDropState == DayContestedDropState.Descent ||
                   latestDropState == DayContestedDropState.Secure;
        }

        private void MaybeShowDropInteractionPrompt()
        {
            if (!IsDayOne() || dayOneDropInteractionPromptShown)
            {
                return;
            }

            if (dayOneDropGuideShown && Time.time - lastDropHintTime < Mathf.Max(0f, dayOneDropPromptDelayAfterGuideSeconds))
            {
                return;
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player == null)
            {
                return;
            }

            var crates = FindObjectsByType<SupplyCrate>(FindObjectsSortMode.None);
            if (crates == null || crates.Length == 0)
            {
                return;
            }

            for (int i = 0; i < crates.Length; i++)
            {
                var crate = crates[i];
                if (crate == null || !crate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float range = Mathf.Max(1.5f, dropInteractionPromptRange);
                var col = crate.GetComponent<CircleCollider2D>();
                if (col != null)
                {
                    range = Mathf.Max(range, col.radius + 0.45f);
                }

                if (Vector2.Distance(player.transform.position, crate.transform.position) > range)
                {
                    continue;
                }

                string status = crate.IsContestedSupplyDrop
                    ? "Press and hold F to secure this drop"
                    : "Press and hold F to loot this drop";
                FindFirstObjectByType<GameplayHUD>()?.ShowTransientStatus(status, statusSeconds);
                dayOneDropInteractionPromptShown = true;
                return;
            }
        }

        private void ArmGuidanceDelayWindow()
        {
            guidanceUnlockedAt = Time.time + Mathf.Clamp(firstHintDelay, 5f, 10f);
        }

        private bool CanShowGuidanceNow()
        {
            return Time.time >= guidanceUnlockedAt;
        }

        private bool IsDayOneCombatTutorialComplete()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentNight != 1)
            {
                return true;
            }

            var waveManager = WaveManager.Instance != null
                ? WaveManager.Instance
                : FindFirstObjectByType<WaveManager>();
            return waveManager == null || waveManager.DayOneTutorialCombatComplete;
        }

        private void SendGuidanceRadio(string message)
        {
            float duration = Mathf.Clamp(commsDuration, 2f, 3f);
            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.QueueSystemMessage(
                    "COMMS",
                    message,
                    duration,
                    interrupt: false,
                    playRadioStatic: false);
                return;
            }

            RadioTransmissions.Instance?.ShowMessage($"RADIO: {message}", duration);
        }

        private string BuildObjectiveComms(int hintIndex)
        {
            var dayObjective = DayObjectiveSystem.Instance != null ? DayObjectiveSystem.Instance.ActiveObjective : null;
            if (dayObjective != null)
            {
                if (hintIndex <= 1)
                {
                    return dayObjective.type switch
                    {
                        ObjectiveType.RecoverSupplyCache =>
                            "Please follow the orange objective marker to the crash-site cache. Press F when you are close enough to interact.",
                        ObjectiveType.ActivateBeacon =>
                            "Please follow the orange objective marker to each terminal. Press F to activate each terminal.",
                        ObjectiveType.SecureZone =>
                            "Please follow the orange objective marker to each secure zone. Remain inside until progress is complete.",
                        _ =>
                            "Please follow the orange objective marker and complete the required interaction."
                    };
                }

                return "Objective reminder. Please continue toward the orange objective marker and complete the interaction.";
            }

            return hintIndex <= 1
                ? "Please follow the orange objective marker and complete the required action."
                : "Objective reminder. Please continue toward the orange objective marker.";
        }

        private string BuildObjectiveStatusLine()
        {
            var dayObjective = DayObjectiveSystem.Instance != null ? DayObjectiveSystem.Instance.ActiveObjective : null;
            if (dayObjective != null)
            {
                return dayObjective.type switch
                {
                    ObjectiveType.RecoverSupplyCache => "Follow the orange marker and press F at the cache",
                    ObjectiveType.ActivateBeacon => "Follow the orange marker and press F at each terminal",
                    ObjectiveType.SecureZone => "Follow the orange marker and remain in the secure zone",
                    _ => "Follow the orange marker and complete the interaction"
                };
            }

            return "Follow the orange marker";
        }

        private static string BuildDropComms(bool contestedOn, int hintIndex)
        {
            if (contestedOn)
            {
                return hintIndex <= 1
                    ? "A contested supply drop is active. Follow the cyan drop marker, enter the zone, and press and hold F until secure progress is complete."
                    : "Supply reminder. Continue toward the cyan drop marker and press and hold F until the drop is secured.";
            }

            return hintIndex <= 1
                ? "Please follow the cyan drop marker to the supply crate and press and hold F until looting is complete."
                : "Supply reminder. Keep holding F until looting is complete.";
        }

        private void ShowDropSecureHoldStatus()
        {
            // Intentionally silent per UX request.
        }
    }
}
