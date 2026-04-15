using System;
using System.Reflection;
using Deadlight.Core;
using Deadlight.Data;
using Deadlight.Level.MapBuilders;
using Deadlight.Visuals;
using UnityEngine;

namespace Deadlight.Narrative
{
    public static class NarrativeWorldBuilder
    {
        private readonly struct TriggerSpec
        {
            public readonly string Id;
            public readonly string Speaker;
            public readonly Vector3 Position;
            public readonly Vector2 Size;
            public readonly int MinNight;
            public readonly int MaxNight;
            public readonly string[] Lines;

            public TriggerSpec(string id, string speaker, Vector3 position, Vector2 size, int minNight, int maxNight, params string[] lines)
            {
                Id = id;
                Speaker = speaker;
                Position = position;
                Size = size;
                MinNight = minNight;
                MaxNight = maxNight;
                Lines = lines ?? Array.Empty<string>();
            }
        }

        private static readonly BindingFlags DialogueFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        public static void PopulateWorld(MapConfig config, Transform parent)
        {
            if (config == null || parent == null)
            {
                return;
            }

            var root = new GameObject("NarrativeWorld").transform;
            root.SetParent(parent);

            SpawnLorePickups(config, root);
            SpawnStoryTriggers(config.mapType, root);
        }

        private static void SpawnLorePickups(MapConfig config, Transform parent)
        {
            var loreParent = new GameObject("LorePickups").transform;
            loreParent.SetParent(parent);

            var positions = config.lorePositions ?? Array.Empty<Vector3>();
            var loreIds = GetLoreIdsForMap(config.mapType);
            const int MaxLorePickupsPerMap = 4;
            int count = Mathf.Min(positions.Length, loreIds.Length);
            count = Mathf.Min(count, MaxLorePickupsPerMap);

            for (int i = 0; i < count; i++)
            {
                var loreObj = new GameObject($"Lore_{loreIds[i]}");
                loreObj.transform.SetParent(loreParent);
                loreObj.transform.position = positions[i];

                var sr = loreObj.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSpriteGenerator.CreatePickupSprite("lore", i);
                sr.sortingOrder = 5;

                var pickup = loreObj.AddComponent<LorePickup>();
                pickup.SetLoreId(loreIds[i]);

                var glow = new GameObject("Glow");
                glow.transform.SetParent(loreObj.transform);
                glow.transform.localPosition = Vector3.zero;
                var glowRenderer = glow.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = CreateGlowSprite();
                glowRenderer.sortingOrder = 4;
                glowRenderer.color = new Color(1f, 0.82f, 0.36f, 0.4f);
                glow.transform.localScale = Vector3.one * 1.8f;
            }
        }

        private static void SpawnStoryTriggers(MapType mapType, Transform parent)
        {
            var triggerParent = new GameObject("StoryTriggers").transform;
            triggerParent.SetParent(parent);

            var specs = GetTriggerSpecs(mapType);
            for (int i = 0; i < specs.Length; i++)
            {
                CreateTrigger(specs[i], triggerParent);
            }
        }

        private static void CreateTrigger(TriggerSpec spec, Transform parent)
        {
            if (spec.Lines == null || spec.Lines.Length == 0)
            {
                return;
            }

            var triggerObj = new GameObject($"StoryTrigger_{spec.Id}");
            triggerObj.transform.SetParent(parent);
            triggerObj.transform.position = spec.Position;

            var collider = triggerObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = spec.Size;

            var trigger = triggerObj.AddComponent<StoryTrigger>();
            trigger.SetDialogue(CreateDialogue(spec));
            trigger.ConfigureRuntime(
                spec.Id,
                once: true,
                dayOnly: true,
                minNightValue: spec.MinNight,
                maxNightValue: spec.MaxNight,
                requireUse: false,
                levelRelativeNightRange: true);
        }

        private static DialogueData CreateDialogue(TriggerSpec spec)
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();

            typeof(DialogueData).GetField("dialogueId", DialogueFlags)?.SetValue(dialogue, spec.Id);
            typeof(DialogueData).GetField("speakerName", DialogueFlags)?.SetValue(dialogue, spec.Speaker);
            typeof(DialogueData).GetField("triggerType", DialogueFlags)?.SetValue(dialogue, DialogueTriggerType.Custom);
            typeof(DialogueData).GetField("triggerNight", DialogueFlags)?.SetValue(dialogue, spec.MinNight);
            typeof(DialogueData).GetField("playOnce", DialogueFlags)?.SetValue(dialogue, true);
            typeof(DialogueData).GetField("playRadioStatic", DialogueFlags)?.SetValue(dialogue, false);
            typeof(DialogueData).GetField("priority", DialogueFlags)?.SetValue(dialogue, 25);

            var lines = new DialogueLine[spec.Lines.Length];
            for (int i = 0; i < spec.Lines.Length; i++)
            {
                lines[i] = new DialogueLine
                {
                    text = spec.Lines[i],
                    displayDuration = 3.5f,
                    autoAdvance = true
                };
            }

            typeof(DialogueData).GetField("lines", DialogueFlags)?.SetValue(dialogue, lines);
            return dialogue;
        }

        private static string[] GetLoreIdsForMap(MapType mapType)
        {
            return mapType switch
            {
                MapType.Industrial => new[]
                {
                    "lab_note_1",
                    "chen_1",
                    "chen_2",
                    "military_2",
                    "facility_1",
                    "chen_3",
                    "chen_6",
                    "chen_8"
                },
                MapType.Suburban => new[]
                {
                    "evac_notice",
                    "journal_1",
                    "survivor_log",
                    "radio_1",
                    "field_report_7",
                    "journal_2",
                    "tower_maintenance",
                    "military_1"
                },
                MapType.Research => new[]
                {
                    "chen_2",
                    "chen_3",
                    "chen_6",
                    "facility_1",
                    "lab_note_1",
                    "military_2",
                    "chen_8",
                    "field_report_7"
                },
                _ => new[]
                {
                    "evac_manifest",
                    "journal_1",
                    "military_1",
                    "lz_brief",
                    "newspaper_1",
                    "journal_2",
                    "checkpoint_report",
                    "convoy_blackbox"
                }
            };
        }

        private static TriggerSpec[] GetTriggerSpecs(MapType mapType)
        {
            return mapType switch
            {
                MapType.Industrial => new[]
                {
                    new TriggerSpec(
                        "industrial_crash_story",
                        "Wreckage Recorder",
                        IndustrialLayout.CrashSitePosition + new Vector3(0f, -2.4f, 0f),
                        new Vector2(6f, 4f),
                        1,
                        4,
                        "Another aircraft torn apart before landing. Whatever brought Flight 7 down was already here.",
                        "If extraction comes at final dawn, the landing zone has to stay clear."),
                    new TriggerSpec(
                        "industrial_lab_story",
                        "Facility Intercom",
                        IndustrialLayout.ResearchLabPosition + new Vector3(0f, 2.2f, 0f),
                        new Vector2(7f, 4f),
                        1,
                        4,
                        "PROJECT LAZARUS. Authorized personnel only. This is where regeneration research became a weapon.",
                        "Dr. Chen built the cure she wanted. The military turned it into Subject 23."),
                    new TriggerSpec(
                        "industrial_office_story",
                        "Control Office Memo",
                        IndustrialLayout.ControlOfficePosition + new Vector3(0f, -2.2f, 0f),
                        new Vector2(6f, 4f),
                        1,
                        4,
                        "Containment failed, and command switched from rescue to erasure.",
                        "They were not trying to save the district. They were trying to delete the evidence.")
                },
                MapType.Suburban => new[]
                {
                    new TriggerSpec(
                        "suburban_checkpoint_story",
                        "Checkpoint Log",
                        SuburbanLayout.CheckpointPosition + new Vector3(0f, 2.2f, 0f),
                        new Vector2(6f, 4f),
                        1,
                        4,
                        "The convoy route ran through this neighborhood first.",
                        "Families were screened, tagged, and left behind when the quarantine line broke."),
                    new TriggerSpec(
                        "suburban_school_story",
                        "School Shelter Board",
                        SuburbanLayout.SchoolPosition + new Vector3(0f, 2.4f, 0f),
                        new Vector2(7f, 4f),
                        1,
                        4,
                        "The school became a shelter before the buses stopped coming.",
                        "Classrooms turned into cots, then triage, then silence."),
                    new TriggerSpec(
                        "suburban_hospital_story",
                        "Clinic Triage Note",
                        SuburbanLayout.HospitalPosition + new Vector3(0f, 2.4f, 0f),
                        new Vector2(7f, 4f),
                        1,
                        4,
                        "The clinic held as long as it could.",
                        "By the time they understood the infected were changing, there was nothing left to treat them with.")
                },
                MapType.Research => new[]
                {
                    new TriggerSpec(
                        "research_gate_story",
                        "Quarantine Console",
                        ResearchLayout.QuarantineGatePosition + new Vector3(0f, -2.4f, 0f),
                        new Vector2(7f, 4f),
                        1,
                        4,
                        "Gate records show the facility was sealed before any civilian extraction arrived.",
                        "Command trapped everyone inside once Lazarus results became strategic."),
                    new TriggerSpec(
                        "research_reactor_story",
                        "Power Relay Log",
                        ResearchLayout.ReactorYardPosition + new Vector3(0f, -2.2f, 0f),
                        new Vector2(7f, 4f),
                        1,
                        4,
                        "Emergency power was rerouted to containment shutters, not medical systems.",
                        "They chose to lock subjects in before they chose to treat anyone."),
                    new TriggerSpec(
                        "research_lab_story",
                        "Main Lab Terminal",
                        ResearchLayout.MainLabPosition + new Vector3(0f, 2.3f, 0f),
                        new Vector2(8f, 4f),
                        1,
                        4,
                        "Subject 23 wasn't an accident. It was the final planned Lazarus deployment.",
                        "If this signal gets out, the cover-up ends here.")
                },
                _ => new[]
                {
                    new TriggerSpec(
                        "town_crash_story",
                        "Flight 7 Black Box",
                        TownCenterLandmarks.CrashSitePosition + new Vector3(0f, -2.4f, 0f),
                        new Vector2(7f, 4f),
                        1,
                        4,
                        "Flight 7 never made the extraction run. The convoy left, and this district was written off.",
                        "If another helicopter reaches the city at final dawn, the landing zone has to stay open."),
                    new TriggerSpec(
                        "town_checkpoint_story",
                        "Checkpoint Graffiti",
                        TownCenterLandmarks.MilitaryCheckpointPosition + new Vector3(0f, -2.1f, 0f),
                        new Vector2(6f, 4f),
                        1,
                        4,
                        "Quarantine line broken. No civilian passage.",
                        "This was containment long before it was rescue."),
                    new TriggerSpec(
                        "town_gas_story",
                        "Supply Route Note",
                        TownCenterLandmarks.GasStationPosition + new Vector3(-1.6f, 2f, 0f),
                        new Vector2(6f, 4f),
                        1,
                        4,
                        "The gas station was the last relief point in town.",
                        "Somebody stacked supplies here and marked routes out. No one came back for them.")
                }
            };
        }

        private static Sprite CreateGlowSprite()
        {
            const int size = 18;
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float alpha = Mathf.Clamp01(1f - distance);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size * 0.5f);
        }
    }
}
