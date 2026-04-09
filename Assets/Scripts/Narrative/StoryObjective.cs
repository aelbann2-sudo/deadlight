using System;
using System.Collections.Generic;
using Deadlight.Core;
using Deadlight.Level.MapBuilders;
using Deadlight.Systems;
using Deadlight.UI;
using UnityEngine;

namespace Deadlight.Narrative
{
    public enum StoryObjectivePhase
    {
        None,
        DayInvestigation,
        NightSurvival
    }

    public readonly struct StoryBeatRecord
    {
        public readonly int Night;
        public readonly string Title;
        public readonly string Summary;

        public StoryBeatRecord(int night, string title, string summary)
        {
            Night = night;
            Title = title;
            Summary = summary;
        }
    }

    public class StoryObjective : MonoBehaviour
    {
        public static StoryObjective Instance { get; private set; }

        public event Action OnObjectiveChanged;

        public StoryObjectivePhase CurrentPhase => currentPhase;
        public string CurrentTitle => currentTitle;
        public string CurrentDescription => currentDescription;
        public string CurrentStatus => currentStatus;
        public bool HasActiveObjective => isObjectiveActive;
        public bool IsComplete => isObjectiveComplete;
        public IReadOnlyList<StoryBeatRecord> CompletedBeats => completedBeats;

        private readonly List<StoryBeatRecord> completedBeats = new List<StoryBeatRecord>();
        private readonly HashSet<int> completedDayLeads = new HashSet<int>();

        private StoryBeatDefinition currentBeat;
        private StoryObjectiveTarget activeTarget;
        private StoryObjectivePhase currentPhase = StoryObjectivePhase.None;
        private string currentTitle = string.Empty;
        private string currentDescription = string.Empty;
        private string currentStatus = string.Empty;
        private bool isObjectiveActive;
        private bool isObjectiveComplete;

        private Font font;

        private readonly struct StoryBeatDefinition
        {
            public readonly int Night;
            public readonly string TargetLabel;
            public readonly string DayTitle;
            public readonly string DayDescription;
            public readonly string NightTitle;
            public readonly string NightDescription;
            public readonly string CompletionMessage;
            public readonly string JournalSummary;
            public readonly Vector3 TargetPosition;
            public readonly int RewardPoints;
            public readonly bool GrantsPowerup;

            public StoryBeatDefinition(
                int night,
                string targetLabel,
                string dayTitle,
                string dayDescription,
                string nightTitle,
                string nightDescription,
                string completionMessage,
                string journalSummary,
                Vector3 targetPosition,
                int rewardPoints,
                bool grantsPowerup = false)
            {
                Night = night;
                TargetLabel = targetLabel;
                DayTitle = dayTitle;
                DayDescription = dayDescription;
                NightTitle = nightTitle;
                NightDescription = nightDescription;
                CompletionMessage = completionMessage;
                JournalSummary = journalSummary;
                TargetPosition = targetPosition;
                RewardPoints = rewardPoints;
                GrantsPowerup = grantsPowerup;
            }
        }

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

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                SyncToState(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ResetStoryProgress()
        {
            completedBeats.Clear();
            completedDayLeads.Clear();
            currentBeat = default;
            currentPhase = StoryObjectivePhase.None;
            currentTitle = string.Empty;
            currentDescription = string.Empty;
            currentStatus = string.Empty;
            isObjectiveActive = false;
            isObjectiveComplete = false;

            ClearActiveTarget();
            UpdateUI();
            RaiseChanged();
        }

        internal void HandleDayTargetReached(StoryObjectiveTarget target)
        {
            if (target == null || target != activeTarget || currentPhase != StoryObjectivePhase.DayInvestigation)
            {
                return;
            }

            CompleteCurrentObjective();
        }

        private void HandleGameStateChanged(GameState state)
        {
            SyncToState(state);
        }

        private void SyncToState(GameState state)
        {
            switch (state)
            {
                case GameState.DayPhase:
                    StartDayObjective(GameManager.Instance?.CurrentNight ?? 1);
                    break;
                case GameState.NightPhase:
                    StartNightObjective(GameManager.Instance?.CurrentNight ?? 1);
                    break;
                default:
                    ClearActiveTarget();
                    isObjectiveActive = false;
                    isObjectiveComplete = false;
                    currentPhase = StoryObjectivePhase.None;
                    currentTitle = string.Empty;
                    currentDescription = string.Empty;
                    currentStatus = string.Empty;
                    UpdateUI();
                    RaiseChanged();
                    break;
            }
        }

        private void StartDayObjective(int night)
        {
            StoryBeatDefinition[] arc = GetStoryArc();
            if (night < 1 || arc.Length == 0)
            {
                return;
            }

            int nightInArc = GameManager.GetNightWithinLevel(night);
            int beatIndex = Mathf.Clamp(nightInArc - 1, 0, arc.Length - 1);

            currentBeat = arc[beatIndex];
            currentPhase = StoryObjectivePhase.DayInvestigation;
            currentTitle = currentBeat.DayTitle;
            currentDescription = currentBeat.DayDescription;
            currentStatus = $"Reach {currentBeat.TargetLabel} before nightfall.";
            isObjectiveActive = true;
            isObjectiveComplete = false;

            CreateTargetForBeat(currentBeat);
            ShowObjectiveAnnouncement($"NIGHT {night}: {currentBeat.DayTitle}");
            PlayObjectiveCue("radio_static", 0.2f);
            UpdateUI();
            RaiseChanged();
        }

        private void StartNightObjective(int night)
        {
            StoryBeatDefinition[] arc = GetStoryArc();
            if (night < 1 || arc.Length == 0)
            {
                return;
            }

            int nightInArc = GameManager.GetNightWithinLevel(night);
            int beatIndex = Mathf.Clamp(nightInArc - 1, 0, arc.Length - 1);

            StoryBeatDefinition beat = arc[beatIndex];
            bool intelSecured = completedDayLeads.Contains(night);

            ClearActiveTarget();
            currentBeat = beat;
            currentPhase = StoryObjectivePhase.NightSurvival;
            currentTitle = beat.NightTitle;
            currentDescription = beat.NightDescription;
            currentStatus = intelSecured
                ? "Daylight lead secured. Rescue plan updated."
                : "Lead missed. Survive anyway and keep the signal alive.";
            isObjectiveActive = true;
            isObjectiveComplete = intelSecured;

            ShowObjectiveAnnouncement($"NIGHT {night}: {beat.NightTitle}");
            if (intelSecured)
            {
                PlayObjectiveCue("pickup", 0.24f);
                TriggerObjectiveFlash(new Color(0.18f, 0.48f, 0.30f, 0.12f), 0.16f);
            }
            else
            {
                PlayObjectiveCue("alarm_siren", 0.12f);
                AudioManager.Instance?.SignalCombatPeak(0.1f, 1.6f);
                TriggerObjectiveFlash(new Color(0.48f, 0.08f, 0.05f, 0.20f), 0.22f);
            }
            UpdateUI();
            RaiseChanged();
        }

        private void CompleteCurrentObjective()
        {
            if (!isObjectiveActive || isObjectiveComplete || currentPhase != StoryObjectivePhase.DayInvestigation)
            {
                return;
            }

            int currentNight = GameManager.Instance != null ? GameManager.Instance.CurrentNight : currentBeat.Night;
            completedDayLeads.Add(currentNight);
            completedBeats.Add(new StoryBeatRecord(currentNight, currentBeat.DayTitle, currentBeat.JournalSummary));

            if (PointsSystem.Instance != null && currentBeat.RewardPoints > 0)
            {
                PointsSystem.Instance.AddPoints(currentBeat.RewardPoints, "Story Lead Secured");
            }

            if (currentBeat.GrantsPowerup && PowerupSystem.Instance != null)
            {
                PowerupSystem.Instance.GrantRandomPowerup();
            }

            RadioTransmissions.Instance?.ShowMessage(currentBeat.CompletionMessage, 4.5f);

            if (DayObjectiveSystem.Instance != null)
            {
                DayObjectiveSystem.Instance.MarkCompleted();
            }

            currentStatus = currentBeat.RewardPoints > 0
                ? $"Lead secured. +{currentBeat.RewardPoints} points."
                : "Lead secured.";
            isObjectiveComplete = true;
            PlayObjectiveCue("pickup", 0.38f);
            TriggerObjectiveFlash(new Color(0.18f, 0.55f, 0.28f, 0.18f), 0.2f);
            ClearActiveTarget();
            UpdateUI();
            RaiseChanged();
        }

        private void CreateTargetForBeat(StoryBeatDefinition beat)
        {
            ClearActiveTarget();

            GameObject targetObject = new GameObject($"StoryTarget_Night{beat.Night}");
            targetObject.transform.position = beat.TargetPosition + Vector3.up * 1.15f;

            activeTarget = targetObject.AddComponent<StoryObjectiveTarget>();
            activeTarget.Initialize(this, beat.TargetLabel, new Color(1f, 0.78f, 0.25f));

            RefreshObjectiveMarkers();
        }

        private void ClearActiveTarget()
        {
            if (activeTarget != null)
            {
                Destroy(activeTarget.gameObject);
                activeTarget = null;
                RefreshObjectiveMarkers();
            }
        }

        private void RefreshObjectiveMarkers()
        {
            ObjectiveMarker marker = FindFirstObjectByType<ObjectiveMarker>();
            marker?.RefreshTargets();
        }

        private void ShowObjectiveAnnouncement(string message)
        {
            RadioTransmissions.Instance?.ShowMessage($"MISSION LOG: {message}", 3.25f, bypassCooldown: true);
        }

        private static void PlayObjectiveCue(string clipName, float volumeScale)
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                return;
            }

            AudioManager.Instance?.PlaySFX(clipName, volumeScale);
        }

        private static void TriggerObjectiveFlash(Color color, float duration)
        {
            GameEffects.Instance?.FlashScreen(color, duration);
        }

        private void LoadFont()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
        }

        private void UpdateUI()
        {
            // UI display is now handled by ObjectiveHUD subscribing to OnObjectiveChanged
        }

        private void RaiseChanged()
        {
            OnObjectiveChanged?.Invoke();
        }

        private StoryBeatDefinition[] GetStoryArc()
        {
            MapType map = GameManager.Instance != null ? GameManager.Instance.SelectedMap : MapType.TownCenter;

            switch (map)
            {
                case MapType.Industrial:
                    return GetIndustrialArc();
                case MapType.Suburban:
                    return GetSuburbanArc();
                case MapType.Research:
                    return GetResearchArc();
                default:
                    return GetTownCenterArc();
            }
        }

        private static StoryBeatDefinition[] GetTownCenterArc()
        {
            return new[]
            {
                new StoryBeatDefinition(
                    1,
                    "Flight 7 crash site",
                    "Check the wreckage",
                    "Reach the Flight 7 crash site and confirm what took the evac run out of the sky.",
                    "Survive the first night",
                    "The district was written off after the crash. Hold out until dawn and prove someone is still alive down here.",
                    "Black box recovered. Flight 7 was hit on approach, not lost by accident.",
                    "Recovered Flight 7's black box. The evac run was destroyed on approach and the district was abandoned on purpose.",
                    TownCenterLandmarks.CrashSitePosition,
                    60),
                new StoryBeatDefinition(
                    2,
                    "military checkpoint",
                    "Search the quarantine line",
                    "Reach the military checkpoint and find out why civilians stopped moving out of the district.",
                    "Hold the perimeter",
                    "Containment has replaced rescue. Keep the streets clear and make it to another sunrise.",
                    "Checkpoint orders confirm command trapped civilians inside the quarantine line to slow the spread.",
                    "Checkpoint orders showed that containment took priority over rescue the moment the line broke.",
                    TownCenterLandmarks.MilitaryCheckpointPosition,
                    80),
                new StoryBeatDefinition(
                    3,
                    "St. Mercy hospital",
                    "Trace the outbreak",
                    "Sweep the hospital records for the first signs of Lazarus patients and mutation spread.",
                    "Survive the mutation wave",
                    "The infected are changing faster every night. Expect new behavior and heavier pressure after sunset.",
                    "Hospital triage notes link the earliest aggressive cases to Lazarus trial patients transferred under guard.",
                    "Hospital triage logs tied the first violent cases to Lazarus transfers, weeks before the public lockdown.",
                    TownCenterLandmarks.HospitalPosition,
                    100,
                    true)
            };
        }

        private static StoryBeatDefinition[] GetIndustrialArc()
        {
            return new[]
            {
                new StoryBeatDefinition(
                    1,
                    "crash pad",
                    "Inspect the crash pad",
                    "Reach the downed aircraft at the north pad and confirm why the district lost air support.",
                    "Survive the blackout",
                    "The industrial district lost both power and rescue in the same hour. Hold out until dawn.",
                    "Crash telemetry recovered. Flight 7 was hit after entering the quarantine corridor.",
                    "Recovered crash telemetry from the pad. The evac run died the moment it crossed into the sealed district.",
                    IndustrialLayout.CrashSitePosition,
                    60),
                new StoryBeatDefinition(
                    2,
                    "control office",
                    "Recover command records",
                    "Reach the control office and search for the orders that shut the district down.",
                    "Hold the yard",
                    "Command has gone silent, but the infected still remember the routes through these lanes.",
                    "Control office logs show rescue was canceled once Lazarus evidence became the priority.",
                    "Control office records confirmed that the district was quarantined to erase Lazarus, not to save survivors.",
                    IndustrialLayout.ControlOfficePosition,
                    80),
                new StoryBeatDefinition(
                    3,
                    "research lab",
                    "Breach the Lazarus lab",
                    "Reach the research lab and learn what Project Lazarus turned into once the military took over.",
                    "Survive the lab fallout",
                    "The things leaving the lab are no longer random infected. The outbreak has a source and it is still active.",
                    "Lab notes recovered. Subject 23 was the first stable Lazarus host and the start of the entire chain.",
                    "Broke into the Lazarus lab and confirmed Subject 23 as the original host that spread the networked infection.",
                    IndustrialLayout.ResearchLabPosition,
                    100,
                    true)
            };
        }

        private static StoryBeatDefinition[] GetSuburbanArc()
        {
            return new[]
            {
                new StoryBeatDefinition(
                    1,
                    "checkpoint barricade",
                    "Find the convoy route",
                    "Reach the southern checkpoint and confirm where the neighborhood convoy failed.",
                    "Survive the first breach",
                    "The barricades fell before the buses cleared the suburb. Stay alive and keep moving house to house.",
                    "Checkpoint route recovered. The convoy never made it past the quarantine turn.",
                    "Tracked the failed convoy route to the checkpoint. The neighborhood was sealed before families got out.",
                    SuburbanLayout.CheckpointPosition,
                    60),
                new StoryBeatDefinition(
                    2,
                    "school shelter",
                    "Search the shelter",
                    "Reach the school and recover the shelter roster for the civilians left behind.",
                    "Hold the neighborhood",
                    "The infected know these streets now. Survive the night and keep the evacuation story alive.",
                    "School shelter records found. Families were staged for pickup, then abandoned when the line collapsed.",
                    "Recovered the school shelter roster. Civilians were queued for extraction and then left when the line broke.",
                    SuburbanLayout.SchoolPosition,
                    80),
                new StoryBeatDefinition(
                    3,
                    "field clinic",
                    "Trace the infection",
                    "Sweep the clinic for triage notes that explain how Lazarus cases reached the suburb.",
                    "Survive the spread",
                    "The clinic saw the change before anyone named it. Night three will hit harder and faster.",
                    "Clinic notes confirm Lazarus transfers were hidden among regular evac patients until symptoms escalated.",
                    "Clinic triage notes showed Lazarus patients were moved through the suburb under evacuation cover.",
                    SuburbanLayout.HospitalPosition,
                    100,
                    true)
            };
        }

        private static StoryBeatDefinition[] GetResearchArc()
        {
            return new[]
            {
                new StoryBeatDefinition(
                    1,
                    "quarantine gate",
                    "Recover access credentials",
                    "Reach the quarantine gate and pull the final facility access log from the lockdown terminal.",
                    "Stabilize the perimeter",
                    "Containment shutters are failing. Survive the opening wave before the inner labs are exposed.",
                    "Gate logs recovered. The complex was sealed from command before evacuation arrived.",
                    "Recovered quarantine credentials that prove command locked the complex and abandoned live personnel inside.",
                    ResearchLayout.QuarantineGatePosition,
                    70),
                new StoryBeatDefinition(
                    2,
                    "data vault",
                    "Extract Lazarus evidence",
                    "Reach the data vault and secure the final Lazarus experiment records before they are purged.",
                    "Protect the archive",
                    "The horde is converging on the vault wings. Survive and keep the evidence intact.",
                    "Data archive secured. Subject 23 containment logs are intact and transmitted to EVAC.",
                    "Extracted Lazarus archive data proving Subject 23 was weaponized and deployed before collapse.",
                    ResearchLayout.DataVaultPosition,
                    120),
                new StoryBeatDefinition(
                    3,
                    "main lab",
                    "Trigger extraction beacon",
                    "Reach the main lab and arm the final extraction beacon in the containment core.",
                    "Final stand in containment",
                    "Subject 23 is tracking the beacon through the lab corridors. Hold the complex until dawn.",
                    "Beacon armed. Dawn extraction is committed if containment holds.",
                    "Armed the final beacon in the main lab and committed the run to a last stand at dawn.",
                    ResearchLayout.MainLabPosition,
                    180,
                    true)
            };
        }
    }

    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class StoryObjectiveTarget : MonoBehaviour
    {
        public string Label { get; private set; }

        private StoryObjective owner;
        private SpriteRenderer ringRenderer;
        private SpriteRenderer iconRenderer;
        private Sprite ringSprite;
        private Sprite diamondSprite;
        private Color targetColor = new Color(1f, 0.78f, 0.25f);
        private Vector3 startScale = Vector3.one;

        public void Initialize(StoryObjective objective, string label, Color color)
        {
            owner = objective;
            Label = label;
            targetColor = color;

            CircleCollider2D trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;

            ringRenderer = GetComponent<SpriteRenderer>();
            ringSprite = CreateRingSprite(targetColor);
            ringRenderer.sprite = ringSprite;
            ringRenderer.sortingOrder = 12;
            ringRenderer.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.55f);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(transform, false);
            iconObject.transform.localPosition = Vector3.up * 0.9f;
            iconObject.transform.localScale = Vector3.one * 0.75f;

            iconRenderer = iconObject.AddComponent<SpriteRenderer>();
            diamondSprite = CreateDiamondSprite(targetColor);
            iconRenderer.sprite = diamondSprite;
            iconRenderer.sortingOrder = 13;

            startScale = transform.localScale;
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.08f;
            transform.localScale = startScale * pulse;

            if (iconRenderer != null)
            {
                iconRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2f) * 6f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            owner?.HandleDayTargetReached(this);
        }

        private void OnDestroy()
        {
            if (ringSprite != null)
            {
                Destroy(ringSprite.texture);
                Destroy(ringSprite);
            }

            if (diamondSprite != null)
            {
                Destroy(diamondSprite.texture);
                Destroy(diamondSprite);
            }
        }

        private static Sprite CreateRingSprite(Color color)
        {
            const int size = 48;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outerRadius = size * 0.45f;
            float innerRadius = size * 0.32f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    bool inRing = distance <= outerRadius && distance >= innerRadius;
                    pixels[(y * size) + x] = inRing ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 24f);
        }

        private static Sprite CreateDiamondSprite(Color color)
        {
            const int size = 20;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                    pixels[(y * size) + x] = distance <= size * 0.35f ? color : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 20f);
        }
    }
}
