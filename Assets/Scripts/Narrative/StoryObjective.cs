using System;
using System.Collections.Generic;
using Deadlight.Core;
using Deadlight.Level.MapBuilders;
using Deadlight.Systems;
using Deadlight.UI;
using UnityEngine;
using UnityEngine.UI;

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

        private GameObject panel;
        private Text headerText;
        private Text titleText;
        private Text descriptionText;
        private Text statusText;
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
            CreateUI();

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
            if (night < 1 || night > arc.Length)
            {
                return;
            }

            currentBeat = arc[night - 1];
            currentPhase = StoryObjectivePhase.DayInvestigation;
            currentTitle = currentBeat.DayTitle;
            currentDescription = currentBeat.DayDescription;
            currentStatus = $"Reach {currentBeat.TargetLabel} before nightfall.";
            isObjectiveActive = true;
            isObjectiveComplete = false;

            CreateTargetForBeat(currentBeat);
            ShowObjectiveAnnouncement($"DAY {night}: {currentBeat.DayTitle}");
            UpdateUI();
            RaiseChanged();
        }

        private void StartNightObjective(int night)
        {
            StoryBeatDefinition[] arc = GetStoryArc();
            if (night < 1 || night > arc.Length)
            {
                return;
            }

            StoryBeatDefinition beat = arc[night - 1];
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
            UpdateUI();
            RaiseChanged();
        }

        private void CompleteCurrentObjective()
        {
            if (!isObjectiveActive || isObjectiveComplete || currentPhase != StoryObjectivePhase.DayInvestigation)
            {
                return;
            }

            completedDayLeads.Add(currentBeat.Night);
            completedBeats.Add(new StoryBeatRecord(currentBeat.Night, currentBeat.DayTitle, currentBeat.JournalSummary));

            if (PointsSystem.Instance != null && currentBeat.RewardPoints > 0)
            {
                PointsSystem.Instance.AddPoints(currentBeat.RewardPoints, "Story Lead Secured");
            }

            if (currentBeat.GrantsPowerup && PowerupSystem.Instance != null)
            {
                PowerupSystem.Instance.GrantRandomPowerup();
            }

            RadioTransmissions.Instance?.ShowMessage(currentBeat.CompletionMessage, 4.5f);

            if (FloatingTextManager.Instance != null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    FloatingTextManager.Instance.SpawnText(
                        "STORY UPDATED",
                        player.transform.position + Vector3.up * 2.25f,
                        new Color(1f, 0.82f, 0.3f),
                        1.8f,
                        26);
                }
            }

            currentStatus = currentBeat.RewardPoints > 0
                ? $"Lead secured. +{currentBeat.RewardPoints} points."
                : "Lead secured.";
            isObjectiveComplete = true;
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
            RadioTransmissions.Instance?.ShowMessage($"MISSION LOG: {message}", 3.25f);
        }

        private void LoadFont()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
        }

        private void CreateUI()
        {
            if (panel != null)
            {
                return;
            }

            Canvas screenCanvas = null;
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenCanvas = canvases[i];
                    break;
                }
            }

            if (screenCanvas == null)
            {
                return;
            }

            panel = new GameObject("StoryObjectivePanel");
            panel.transform.SetParent(screenCanvas.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-12f, -105f);
            panelRect.sizeDelta = new Vector2(430f, 120f);

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.03f, 0.03f, 0.72f);

            headerText = CreateText("Header", panel.transform, new Vector2(0.5f, 1f), new Vector2(0f, 0.68f), new Vector2(1f, 1f), 14, TextAnchor.UpperCenter);
            headerText.text = "STORY DIRECTIVE";
            headerText.color = new Color(1f, 0.77f, 0.28f, 0.9f);

            titleText = CreateText("Title", panel.transform, new Vector2(0f, 1f), new Vector2(12f, -28f), new Vector2(1f, 1f), 20, TextAnchor.UpperLeft);
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1f, 0.95f, 0.75f);

            descriptionText = CreateText("Description", panel.transform, new Vector2(0f, 1f), new Vector2(12f, -54f), new Vector2(1f, 0.64f), 15, TextAnchor.UpperLeft);
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            descriptionText.color = new Color(0.88f, 0.88f, 0.88f);

            statusText = CreateText("Status", panel.transform, new Vector2(0f, 0f), new Vector2(12f, 0f), new Vector2(1f, 0.2f), 14, TextAnchor.LowerLeft);
            statusText.color = new Color(0.4f, 1f, 0.4f);

            panel.SetActive(false);
        }

        private Text CreateText(
            string name,
            Transform parent,
            Vector2 pivot,
            Vector2 offsetMin,
            Vector2 offsetMax,
            int fontSize,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private void UpdateUI()
        {
            if (panel == null)
            {
                return;
            }

            bool show = isObjectiveActive && !string.IsNullOrEmpty(currentTitle);
            panel.SetActive(show);

            if (!show)
            {
                return;
            }

            titleText.text = currentTitle;
            descriptionText.text = currentDescription;
            statusText.text = currentStatus;

            if (currentPhase == StoryObjectivePhase.DayInvestigation)
            {
                statusText.color = isObjectiveComplete
                    ? new Color(0.4f, 1f, 0.4f)
                    : new Color(1f, 0.85f, 0.35f);
            }
            else
            {
                statusText.color = new Color(0.4f, 0.8f, 1f);
            }
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
                    100),
                new StoryBeatDefinition(
                    4,
                    "fuel route",
                    "Mark the extraction corridor",
                    "Reach the gas station and secure fuel for the dawn approach route into the landing zone.",
                    "Protect the approach",
                    "Rescue will only risk a landing if the access lane stays open through the night.",
                    "Fuel reserves found. The dawn approach can stay lit if the route survives the night.",
                    "Marked the final refuel point needed to keep the helicopter's approach path open at dawn.",
                    TownCenterLandmarks.GasStationPosition,
                    120),
                new StoryBeatDefinition(
                    5,
                    "landing zone",
                    "Light the flare plan",
                    "Return to the crash site and lock in the flare position for fifth-dawn extraction.",
                    "Final stand",
                    "Subject 23 is tracking the signal. Hold the landing zone through the last night and the helicopter comes at dawn.",
                    "Extraction flare plan set. Dawn pickup is committed if the landing zone stays clear.",
                    "Set the final flare plan at the crash site. Dawn extraction now depends entirely on surviving one last night.",
                    TownCenterLandmarks.CrashSitePosition,
                    160,
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
                    100),
                new StoryBeatDefinition(
                    4,
                    "fuel depot",
                    "Prime the burn route",
                    "Reach the fuel depot and secure enough incendiary stock to disrupt Lazarus regeneration.",
                    "Defend the burn line",
                    "Fire is your only real answer to Lazarus tissue. Keep the depot route alive through the night.",
                    "Fuel depot secured. Enough incendiary stock remains to keep the extraction route burning at dawn.",
                    "Secured incendiary fuel from the depot, giving the final defense a way to disrupt Lazarus regeneration.",
                    IndustrialLayout.FuelDepotPosition,
                    120),
                new StoryBeatDefinition(
                    5,
                    "north extraction pad",
                    "Set the extraction beacon",
                    "Return to the north crash pad and place the final beacon for the fifth-dawn pickup.",
                    "Final containment break",
                    "Subject 23 is coming to the beacon. Hold the pad until sunrise and end the Lazarus chain here.",
                    "Beacon armed. Dawn extraction is locked to the north pad if you can keep it clear.",
                    "Placed the final extraction beacon at the north pad and committed the last stand to dawn pickup.",
                    IndustrialLayout.CrashSitePosition,
                    160,
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
                    100),
                new StoryBeatDefinition(
                    4,
                    "gas station pumps",
                    "Recover flare fuel",
                    "Reach the gas station and secure enough fuel to light the final dawn flare line.",
                    "Protect the approach lane",
                    "The helicopter can only risk the suburb if the landing flare route stays alive after dark.",
                    "Flare fuel secured. The dawn approach can be marked if you survive tonight.",
                    "Recovered the fuel needed to light a visible dawn flare line for the rescue helicopter.",
                    SuburbanLayout.GasStationPosition,
                    120),
                new StoryBeatDefinition(
                    5,
                    "cul-de-sac landing zone",
                    "Mark the landing circle",
                    "Move to the cul-de-sac and lock the fifth-dawn landing circle before the horde closes in.",
                    "Final stand in the cul-de-sac",
                    "Subject 23 is homing in on the rescue flare. Hold the landing circle until sunrise.",
                    "Landing circle marked. Dawn pickup is committed if the cul-de-sac stays clear.",
                    "Marked the cul-de-sac as the final landing circle and committed the last stand to dawn extraction.",
                    SuburbanLayout.CulDeSacPosition,
                    160,
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
