using UnityEngine;

namespace Deadlight.Narrative
{
    public enum DialogueTriggerType
    {
        NightStart,
        NightEnd,
        GameStart,
        GameOver,
        Victory,
        ZoneEnter,
        ItemPickup,
        BossSpawn,
        Custom
    }

    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;
        public float displayDuration = 3f;
        public AudioClip voiceClip;
        public bool autoAdvance = true;
    }

    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Deadlight/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("Dialogue Info")]
        [SerializeField] private string dialogueId;
        [SerializeField] private string speakerName = "Radio";
        [SerializeField] private Sprite speakerPortrait;
        
        [Header("Trigger Conditions")]
        [SerializeField] private DialogueTriggerType triggerType = DialogueTriggerType.Custom;
        [SerializeField] private int triggerNight = 1;
        [SerializeField] private string triggerZoneId = "";
        [SerializeField] private bool playOnce = true;
        
        [Header("Dialogue Content")]
        [SerializeField] private DialogueLine[] lines;
        
        [Header("Audio")]
        [SerializeField] private AudioClip radioStaticSound;
        [SerializeField] private bool playRadioStatic = true;
        
        [Header("Priority")]
        [SerializeField] private int priority = 0;

        public string DialogueId => string.IsNullOrEmpty(dialogueId) ? name : dialogueId;
        public string SpeakerName => speakerName;
        public Sprite SpeakerPortrait => speakerPortrait;
        public DialogueTriggerType TriggerType => triggerType;
        public int TriggerNight => triggerNight;
        public string TriggerZoneId => triggerZoneId;
        public bool PlayOnce => playOnce;
        public DialogueLine[] Lines => lines;
        public AudioClip RadioStaticSound => radioStaticSound;
        public bool PlayRadioStatic => playRadioStatic;
        public int Priority => priority;

        public bool ShouldTrigger(DialogueTriggerType type, int currentNight, string zoneId = "")
        {
            if (triggerType != type) return false;
            
            if (triggerType == DialogueTriggerType.NightStart || triggerType == DialogueTriggerType.NightEnd)
            {
                return currentNight == triggerNight;
            }
            
            if (triggerType == DialogueTriggerType.ZoneEnter)
            {
                return zoneId == triggerZoneId;
            }
            
            return true;
        }

        public float GetTotalDuration()
        {
            float total = 0f;
            foreach (var line in lines)
            {
                total += line.displayDuration;
            }
            return total;
        }
    }
}
