using UnityEngine;
using System.Collections.Generic;

namespace Deadlight.Narrative
{
    [System.Serializable]
    public class LoreEntry
    {
        public string id;
        public string title;
        [TextArea(3, 6)]
        public string content;
        public LoreCategory category;
        public bool discovered;
    }

    public enum LoreCategory
    {
        LabNotes,
        SurvivorJournal,
        MilitaryOrders,
        NewspaperClipping,
        RadioLog
    }

    public class EnvironmentalLore : MonoBehaviour
    {
        public static EnvironmentalLore Instance { get; private set; }

        [Header("Lore Database")]
        [SerializeField] private List<LoreEntry> allLoreEntries = new List<LoreEntry>();

        private HashSet<string> discoveredLore = new HashSet<string>();

        public IReadOnlyList<LoreEntry> AllLore => allLoreEntries;
        public int TotalLoreCount => allLoreEntries.Count;
        public int DiscoveredLoreCount => discoveredLore.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultLore();
        }

        private void InitializeDefaultLore()
        {
            if (allLoreEntries.Count > 0) return;

            allLoreEntries.Add(new LoreEntry
            {
                id = "lab_note_1",
                title = "Lab Note #127",
                category = LoreCategory.LabNotes,
                content = "Subject 23 shows unprecedented cellular regeneration. The modified virus strain is exceeding all projections. Dr. Chen wants to proceed to Phase 3 trials immediately.\n\nI have concerns about containment protocols, but funding pressure from above is immense. We're playing with fire."
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "lab_note_2",
                title = "Lab Note #134",
                category = LoreCategory.LabNotes,
                content = "Containment breach in Sector 4. Three researchers exposed. Initial symptoms match our projections, but the aggression levels are far beyond anything we anticipated.\n\nSubject 23 has escaped. God help us all."
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "journal_1",
                title = "Sarah's Journal - Day 1",
                category = LoreCategory.SurvivorJournal,
                content = "The sirens started at 3 AM. By dawn, the streets were chaos. I saw Mrs. Patterson from next door... she wasn't Mrs. Patterson anymore.\n\nI've barricaded the apartment. The radio says help is coming. I just need to wait."
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "journal_2",
                title = "Sarah's Journal - Day 4",
                category = LoreCategory.SurvivorJournal,
                content = "No one is coming. The radio went silent yesterday. I can hear them in the hallway now, scratching at the doors.\n\nI found a gun in Mr. Chen's apartment. He won't need it anymore.\n\nIf anyone finds this, head north. There might still be safe zones."
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "military_1",
                title = "Operation Deadlight - Orders",
                category = LoreCategory.MilitaryOrders,
                content = "CLASSIFIED - PRIORITY ALPHA\n\nAll units: Containment has failed. Proceed to extraction points Delta through Golf. Civilian rescue operations are TERMINATED.\n\nDestroy all evidence of Project Lazarus. No exceptions.\n\nCommand is relocating. You're on your own."
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "military_2",
                title = "Field Report - Sgt. Morrison",
                category = LoreCategory.MilitaryOrders,
                content = "Day 3 of holding the perimeter. Lost contact with HQ 18 hours ago.\n\nThe infected are getting smarter. They're not just attacking anymore. They're probing our defenses, testing for weaknesses.\n\nPvt. Chen says they're evolving. Adapting to our tactics.\n\nWe can't hold much longer."
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "newspaper_1",
                title = "Herald Tribune - Final Edition",
                category = LoreCategory.NewspaperClipping,
                content = "CITY UNDER QUARANTINE\n\n'Aggressive Flu Strain' Claims Hundreds\n\nMilitary enforces lockdown as hospitals overflow. Officials urge calm, promise vaccine within weeks.\n\n[The rest of the article is stained with something dark]"
            });

            allLoreEntries.Add(new LoreEntry
            {
                id = "radio_1",
                title = "Intercepted Radio Transmission",
                category = LoreCategory.RadioLog,
                content = "*static* ...repeat, this is not a drill. The subjects have escaped containment. I don't know how they got past the security... *screaming in background* ...they're learning. Dear God, they're LEARNING... *static*"
            });
        }

        public void DiscoverLore(string loreId)
        {
            if (discoveredLore.Contains(loreId)) return;

            discoveredLore.Add(loreId);

            var entry = allLoreEntries.Find(l => l.id == loreId);
            if (entry != null)
            {
                entry.discovered = true;
                Debug.Log($"[Lore] Discovered: {entry.title}");

                ShowLoreNotification(entry);
            }
        }

        private void ShowLoreNotification(LoreEntry entry)
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            
            var type = typeof(DialogueData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("dialogueId", flags)?.SetValue(dialogue, $"lore_{entry.id}");
            type.GetField("speakerName", flags)?.SetValue(dialogue, entry.title);
            type.GetField("playRadioStatic", flags)?.SetValue(dialogue, false);
            type.GetField("playOnce", flags)?.SetValue(dialogue, true);

            var lines = new DialogueLine[]
            {
                new DialogueLine { text = entry.content, displayDuration = 5f, autoAdvance = false }
            };
            type.GetField("lines", flags)?.SetValue(dialogue, lines);

            if (NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.QueueDialogue(dialogue);
            }
        }

        public bool HasDiscovered(string loreId)
        {
            return discoveredLore.Contains(loreId);
        }

        public List<LoreEntry> GetDiscoveredLore()
        {
            return allLoreEntries.FindAll(l => discoveredLore.Contains(l.id));
        }

        public List<LoreEntry> GetLoreByCategory(LoreCategory category)
        {
            return allLoreEntries.FindAll(l => l.category == category);
        }

        public void ResetDiscoveries()
        {
            discoveredLore.Clear();
            foreach (var entry in allLoreEntries)
            {
                entry.discovered = false;
            }
        }
    }
}
