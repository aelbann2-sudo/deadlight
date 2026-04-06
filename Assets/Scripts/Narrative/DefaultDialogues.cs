using UnityEngine;
using System.Collections.Generic;

namespace Deadlight.Narrative
{
    public static class DefaultDialogues
    {
        public static List<DialogueData> CreateDefaultDialogues()
        {
            var dialogues = new List<DialogueData>();

            dialogues.Add(CreateNight1StartDialogue());
            dialogues.Add(CreateNight2StartDialogue());
            dialogues.Add(CreateNight3StartDialogue());
            dialogues.Add(CreateNight4StartDialogue());
            dialogues.Add(CreateNight5StartDialogue());
            dialogues.Add(CreateNight6StartDialogue());
            dialogues.Add(CreateVictoryDialogue());
            dialogues.Add(CreateGameOverDialogue());
            dialogues.Add(CreateGameStartDialogue());

            return dialogues;
        }

        private static DialogueData CreateNight1StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue, 
                "night_1_start",
                "EVAC Command",
                DialogueTriggerType.NightStart,
                1,
                new string[]
                {
                    "*static* Medic, night is falling. The infected become aggressive after dark.",
                    "Find cover. Use the buildings. Funnel them into kill zones.",
                    "We recovered Flight 7's black box from your crash site data.",
                    "That bird was shot down. Not by the infected — by something launched from inside the zone.",
                    "Survive tonight and we'll figure out what. EVAC Command out. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight2StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_2_start",
                "EVAC Command",
                DialogueTriggerType.NightStart,
                2,
                new string[]
                {
                    "*static* The shelter records you found... Command didn't evacuate those families.",
                    "They drew the quarantine line right through the suburb. On purpose.",
                    "And the infected are changing. Our sensors show faster ones. Runners.",
                    "Whatever Project Lazarus created, it's still evolving out there.",
                    "Two more levels, medic. Keep moving. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight3StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_3_start",
                "EVAC Command",
                DialogueTriggerType.NightStart,
                3,
                new string[]
                {
                    "*static* We decoded the Lazarus files you transmitted.",
                    "Subject 23 was a soldier. Volunteer for cellular regeneration trials.",
                    "Perfect healing. Perfect weapon. Then it escaped containment three weeks ago.",
                    "Patient zero. Every infected in this zone traces back to that one host.",
                    "New types detected tonight — exploders, spitters. They're adapting to you.",
                    "One more level after this. Stay alive. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight4StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_4_start",
                "EVAC Command",
                DialogueTriggerType.NightStart,
                4,
                new string[]
                {
                    "*static* You are now deploying into the suburban corridor.",
                    "Good work recovering the town evidence. Command has verified your uplink.",
                    "New objective chain is live. Civilian shelter and clinic logs are still missing.",
                    "Expect tighter streets, longer sight lines, and faster contact waves tonight.",
                    "Hold this sector and we keep the extraction window open. EVAC Command out. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight5StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_5_start",
                "EVAC Command",
                DialogueTriggerType.NightStart,
                5,
                new string[]
                {
                    "*static* Suburban sweep update: your checkpoint and shelter records are confirmed.",
                    "The quarantine order was deliberate. Civilians were held to delay spread.",
                    "Runners are coordinating in packs now. Keep lanes narrow and rotate positions.",
                    "One more push after tonight and we can close this operation cleanly.",
                    "Stay alive, medic. EVAC Command out. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight6StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_6_start",
                "EVAC Command",
                DialogueTriggerType.NightStart,
                6,
                new string[]
                {
                    "*static* Final night of this deployment. Complete the last suburban objective and hold to dawn.",
                    "Clinic transmissions confirm Lazarus patient transfers ran through this district.",
                    "No rescue attempts until this package is fully secured and transmitted.",
                    "Clear this night and Command marks the operation as successful.",
                    "Hold your ground. EVAC Command out. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateVictoryDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "victory",
                "Helicopter Pilot",
                DialogueTriggerType.Victory,
                4,
                new string[]
                {
                    "*rotors* EVAC Bravo to ground — we see your beacon!",
                    "Landing zone is clear. Coming in hot!",
                    "Grab on, medic! We're pulling you out!",
                    "...you actually made it. God damn.",
                    "Welcome back to the world."
                });
            return dialogue;
        }

        private static DialogueData CreateGameOverDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "game_over",
                "EVAC Command",
                DialogueTriggerType.GameOver,
                1,
                new string[]
                {
                    "*static* Medic? Medic, do you copy?",
                    "Vitals are flatlined... signal lost.",
                    "*silence*",
                    "...Log it. Another one the zone took."
                });
            return dialogue;
        }

        private static DialogueData CreateGameStartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "game_start",
                "Medic",
                DialogueTriggerType.GameStart,
                1,
                new string[]
                {
                    "Flight 7 is gone. I'm the only one still breathing.",
                    "Command wants Lazarus proof before they risk another extraction.",
                    "Daylight is for intel and supplies. Night is for survival.",
                    "I'm a medic, not a soldier. Doesn't matter anymore."
                });
            return dialogue;
        }

        private static void SetDialogueValues(DialogueData dialogue, string id, string speaker, 
            DialogueTriggerType triggerType, int triggerNight, string[] lines)
        {
            var serializedObject = new SerializedDialogueData
            {
                dialogueId = id,
                speakerName = speaker,
                triggerType = triggerType,
                triggerNight = triggerNight,
                lines = lines
            };
            
            ApplySerializedData(dialogue, serializedObject);
        }

        private static void ApplySerializedData(DialogueData dialogue, SerializedDialogueData data)
        {
            var type = typeof(DialogueData);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("dialogueId", flags)?.SetValue(dialogue, data.dialogueId);
            type.GetField("speakerName", flags)?.SetValue(dialogue, data.speakerName);
            type.GetField("triggerType", flags)?.SetValue(dialogue, data.triggerType);
            type.GetField("triggerNight", flags)?.SetValue(dialogue, data.triggerNight);
            type.GetField("playOnce", flags)?.SetValue(dialogue, true);
            type.GetField("playRadioStatic", flags)?.SetValue(dialogue, true);

            var dialogueLines = new DialogueLine[data.lines.Length];
            for (int i = 0; i < data.lines.Length; i++)
            {
                dialogueLines[i] = new DialogueLine
                {
                    text = data.lines[i],
                    displayDuration = 3f,
                    autoAdvance = true
                };
            }
            type.GetField("lines", flags)?.SetValue(dialogue, dialogueLines);
        }

        private struct SerializedDialogueData
        {
            public string dialogueId;
            public string speakerName;
            public DialogueTriggerType triggerType;
            public int triggerNight;
            public string[] lines;
        }
    }
}
