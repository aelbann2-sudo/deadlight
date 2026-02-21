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
                "Emergency Broadcast",
                DialogueTriggerType.NightStart,
                1,
                new string[]
                {
                    "*static* This is an Emergency Broadcast System message...",
                    "The evacuation convoy has departed. All remaining survivors must shelter in place.",
                    "A rescue helicopter will attempt extraction at dawn on the fifth day.",
                    "Clear the landing zone of infected. Repeat: clear the LZ.",
                    "Good luck, survivor. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight2StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_2_start",
                "Radio Command",
                DialogueTriggerType.NightStart,
                2,
                new string[]
                {
                    "*static* Survivor detected in sector 7...",
                    "Hold your position. Infected activity is increasing in your area.",
                    "We're tracking larger groups moving toward your location.",
                    "Stay alert. They're getting bolder. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight3StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_3_start",
                "Radio Command",
                DialogueTriggerType.NightStart,
                3,
                new string[]
                {
                    "*static* Warning: Mutation signatures detected...",
                    "The infected are... adapting. We've never seen this before.",
                    "Some are moving faster. Others are more resilient.",
                    "Whatever you're doing, they're responding to it.",
                    "Be ready for anything tonight. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight4StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_4_start",
                "Radio Command",
                DialogueTriggerType.NightStart,
                4,
                new string[]
                {
                    "*static* Rescue ETA: 24 hours...",
                    "You've made it further than anyone else in your sector.",
                    "The helicopter is being prepped. Just one more night after this.",
                    "Stay alive. We're coming for you. *static*"
                });
            return dialogue;
        }

        private static DialogueData CreateNight5StartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "night_5_start",
                "Radio Command",
                DialogueTriggerType.NightStart,
                5,
                new string[]
                {
                    "*static* Final night, survivor...",
                    "We're detecting a massive infected signature heading your way.",
                    "Something big. Something we haven't seen before.",
                    "Hold the line. The helicopter will be there at dawn.",
                    "This is it. Make it count. *static*"
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
                5,
                new string[]
                {
                    "*helicopter sounds* Survivor, we have visual!",
                    "Landing zone is clear. Coming in hot!",
                    "Grab on! We're getting you out of here!",
                    "You made it... you actually made it.",
                    "Welcome back to the land of the living."
                });
            return dialogue;
        }

        private static DialogueData CreateGameOverDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "game_over",
                "Radio Command",
                DialogueTriggerType.GameOver,
                1,
                new string[]
                {
                    "*static* We've lost the signal...",
                    "Survivor... do you copy? Survivor!",
                    "*silence*",
                    "...Mark this location. Another one lost."
                });
            return dialogue;
        }

        private static DialogueData CreateGameStartDialogue()
        {
            var dialogue = ScriptableObject.CreateInstance<DialogueData>();
            SetDialogueValues(dialogue,
                "game_start",
                "Survivor",
                DialogueTriggerType.GameStart,
                1,
                new string[]
                {
                    "The convoy... they left without me.",
                    "Five days. I just need to survive five days.",
                    "Find supplies during the day. Hold out at night.",
                    "I can do this. I have to."
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
