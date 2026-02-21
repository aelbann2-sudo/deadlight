using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Deadlight.Narrative
{
    public class NarrativeManager : MonoBehaviour
    {
        public static NarrativeManager Instance { get; private set; }

        [Header("Dialogue Database")]
        [SerializeField] private List<DialogueData> allDialogues = new List<DialogueData>();
        
        [Header("UI Reference")]
        [SerializeField] private DialogueUI dialogueUI;
        
        [Header("Audio")]
        [SerializeField] private AudioSource narrativeAudioSource;
        
        [Header("Settings")]
        [SerializeField] private bool autoPlayNightDialogues = true;
        [SerializeField] private float dialogueDelay = 1f;

        private HashSet<string> playedDialogues = new HashSet<string>();
        private Queue<DialogueData> dialogueQueue = new Queue<DialogueData>();
        private DialogueData currentDialogue;
        private bool isPlaying = false;

        public bool IsPlaying => isPlaying;
        public DialogueData CurrentDialogue => currentDialogue;

        public event Action<DialogueData> OnDialogueStarted;
        public event Action<DialogueData> OnDialogueEnded;
        public event Action<DialogueLine> OnLineDisplayed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (narrativeAudioSource == null)
            {
                narrativeAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            LoadDialoguesFromResources();
            SubscribeToGameEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
        }

        private void LoadDialoguesFromResources()
        {
            var loadedDialogues = Resources.LoadAll<DialogueData>("Dialogues");
            foreach (var dialogue in loadedDialogues)
            {
                if (!allDialogues.Contains(dialogue))
                {
                    allDialogues.Add(dialogue);
                }
            }
            Debug.Log($"[NarrativeManager] Loaded {allDialogues.Count} dialogues");
        }

        private void SubscribeToGameEvents()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                Core.GameManager.Instance.OnNightChanged += HandleNightChanged;
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                Core.GameManager.Instance.OnNightChanged -= HandleNightChanged;
            }
        }

        private void HandleGameStateChanged(Core.GameState newState)
        {
            if (!autoPlayNightDialogues) return;

            int currentNight = Core.GameManager.Instance?.CurrentNight ?? 1;

            switch (newState)
            {
                case Core.GameState.DayPhase:
                    TriggerDialogue(DialogueTriggerType.NightEnd, currentNight - 1);
                    break;
                case Core.GameState.NightPhase:
                    TriggerDialogue(DialogueTriggerType.NightStart, currentNight);
                    break;
                case Core.GameState.GameOver:
                    TriggerDialogue(DialogueTriggerType.GameOver, currentNight);
                    break;
                case Core.GameState.Victory:
                    TriggerDialogue(DialogueTriggerType.Victory, currentNight);
                    break;
            }
        }

        private void HandleNightChanged(int newNight)
        {
            Debug.Log($"[NarrativeManager] Night changed to {newNight}");
        }

        public void TriggerDialogue(DialogueTriggerType triggerType, int currentNight, string zoneId = "")
        {
            var matchingDialogues = allDialogues
                .Where(d => d.ShouldTrigger(triggerType, currentNight, zoneId))
                .Where(d => !d.PlayOnce || !playedDialogues.Contains(d.DialogueId))
                .OrderByDescending(d => d.Priority)
                .ToList();

            foreach (var dialogue in matchingDialogues)
            {
                QueueDialogue(dialogue);
            }
        }

        public void QueueDialogue(DialogueData dialogue)
        {
            if (dialogue == null) return;

            dialogueQueue.Enqueue(dialogue);
            
            if (!isPlaying)
            {
                StartCoroutine(ProcessDialogueQueue());
            }
        }

        public void PlayDialogueImmediate(DialogueData dialogue)
        {
            if (dialogue == null) return;

            StopAllCoroutines();
            dialogueQueue.Clear();
            
            StartCoroutine(PlayDialogueCoroutine(dialogue));
        }

        private System.Collections.IEnumerator ProcessDialogueQueue()
        {
            yield return new WaitForSeconds(dialogueDelay);

            while (dialogueQueue.Count > 0)
            {
                var dialogue = dialogueQueue.Dequeue();
                yield return StartCoroutine(PlayDialogueCoroutine(dialogue));
                yield return new WaitForSeconds(0.5f);
            }
        }

        private System.Collections.IEnumerator PlayDialogueCoroutine(DialogueData dialogue)
        {
            isPlaying = true;
            currentDialogue = dialogue;

            if (dialogue.PlayOnce)
            {
                playedDialogues.Add(dialogue.DialogueId);
            }

            OnDialogueStarted?.Invoke(dialogue);

            if (dialogue.PlayRadioStatic && dialogue.RadioStaticSound != null)
            {
                narrativeAudioSource.PlayOneShot(dialogue.RadioStaticSound);
                yield return new WaitForSeconds(0.3f);
            }

            if (dialogueUI != null)
            {
                dialogueUI.ShowDialogue(dialogue);
            }

            foreach (var line in dialogue.Lines)
            {
                OnLineDisplayed?.Invoke(line);

                if (dialogueUI != null)
                {
                    dialogueUI.DisplayLine(line);
                }

                if (line.voiceClip != null)
                {
                    narrativeAudioSource.PlayOneShot(line.voiceClip);
                }

                if (line.autoAdvance)
                {
                    yield return new WaitForSeconds(line.displayDuration);
                }
                else
                {
                    while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButtonDown(0))
                    {
                        yield return null;
                    }
                    yield return null;
                }
            }

            if (dialogueUI != null)
            {
                dialogueUI.HideDialogue();
            }

            OnDialogueEnded?.Invoke(dialogue);

            currentDialogue = null;
            isPlaying = false;
        }

        public void SkipCurrentDialogue()
        {
            if (!isPlaying) return;

            StopAllCoroutines();
            
            if (dialogueUI != null)
            {
                dialogueUI.HideDialogue();
            }

            OnDialogueEnded?.Invoke(currentDialogue);
            currentDialogue = null;
            isPlaying = false;

            if (dialogueQueue.Count > 0)
            {
                StartCoroutine(ProcessDialogueQueue());
            }
        }

        public void ClearQueue()
        {
            dialogueQueue.Clear();
        }

        public void ResetPlayedDialogues()
        {
            playedDialogues.Clear();
        }

        public bool HasPlayed(string dialogueId)
        {
            return playedDialogues.Contains(dialogueId);
        }

        public DialogueData GetDialogueById(string id)
        {
            return allDialogues.FirstOrDefault(d => d.DialogueId == id);
        }

        public void RegisterDialogue(DialogueData dialogue)
        {
            if (!allDialogues.Contains(dialogue))
            {
                allDialogues.Add(dialogue);
            }
        }
    }
}
