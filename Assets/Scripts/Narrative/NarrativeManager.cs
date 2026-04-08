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
        [SerializeField] private bool playEndingStateDialogues = false;
        [SerializeField] [Range(0f, 3f)] private float minInterruptProtectSeconds = 1.2f;

        private HashSet<string> playedDialogues = new HashSet<string>();
        private HashSet<string> queuedPlayOnceDialogueIds = new HashSet<string>();
        private Queue<DialogueData> dialogueQueue = new Queue<DialogueData>();
        private DialogueData currentDialogue;
        private bool isPlaying = false;
        private Coroutine queueProcessorCoroutine;
        private float currentDialogueStartedAt = -999f;

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

            if (allDialogues.Count == 0)
            {
                var defaults = DefaultDialogues.CreateDefaultDialogues();
                allDialogues.AddRange(defaults);
                Debug.Log($"[NarrativeManager] Loaded {defaults.Count} default dialogues");
            }

            EnsureDialogueUI();
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

        private bool hasTriggeredGameStart = false;

        private void HandleGameStateChanged(Core.GameState newState)
        {
            if (!autoPlayNightDialogues) return;

            int currentNight = Core.GameManager.Instance?.CurrentNight ?? 1;

            switch (newState)
            {
                case Core.GameState.DayPhase:
                    if (!hasTriggeredGameStart)
                    {
                        hasTriggeredGameStart = true;
                        if (currentNight == 1)
                        {
                            TriggerDialogue(DialogueTriggerType.GameStart, 1);
                        }
                    }
                    TriggerDialogue(DialogueTriggerType.NightEnd, currentNight - 1);
                    break;
                case Core.GameState.NightPhase:
                    TriggerDialogue(DialogueTriggerType.NightStart, currentNight);
                    break;
                case Core.GameState.GameOver:
                    CancelAllDialoguePlayback(immediateHide: true);
                    if (playEndingStateDialogues)
                    {
                        TriggerDialogue(DialogueTriggerType.GameOver, currentNight);
                    }
                    break;
                case Core.GameState.Victory:
                    CancelAllDialoguePlayback(immediateHide: true);
                    if (playEndingStateDialogues)
                    {
                        TriggerDialogue(DialogueTriggerType.Victory, currentNight);
                    }
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

            if (dialogue.PlayOnce)
            {
                string dialogueId = dialogue.DialogueId;
                if (playedDialogues.Contains(dialogueId) || queuedPlayOnceDialogueIds.Contains(dialogueId))
                {
                    return;
                }

                if (currentDialogue != null &&
                    currentDialogue.PlayOnce &&
                    string.Equals(currentDialogue.DialogueId, dialogueId, StringComparison.Ordinal))
                {
                    return;
                }

                queuedPlayOnceDialogueIds.Add(dialogueId);
            }

            dialogueQueue.Enqueue(dialogue);
            EnsureQueueProcessorRunning();
        }

        public void PlayDialogueImmediate(DialogueData dialogue)
        {
            if (dialogue == null) return;

            StopAllCoroutines();
            queueProcessorCoroutine = null;
            dialogueQueue.Clear();
            queuedPlayOnceDialogueIds.Clear();

            if (dialogueUI != null)
            {
                dialogueUI.HideDialogue(true);
            }

            if (isPlaying)
            {
                OnDialogueEnded?.Invoke(currentDialogue);
            }

            if (currentDialogue != null && currentDialogue.IsRuntimeTransient)
            {
                Destroy(currentDialogue);
            }

            currentDialogue = null;
            isPlaying = false;
            
            StartCoroutine(PlayDialogueCoroutine(dialogue));
        }

        public void QueueSystemMessage(
            string speaker,
            string message,
            float duration = 3f,
            bool interrupt = false,
            bool playRadioStatic = false)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var runtimeDialogue = DialogueData.CreateRuntimeMessage(speaker, message, duration, playRadioStatic);
            bool shouldInterruptNow = interrupt && CanInterruptCurrentDialogue(speaker);
            if (shouldInterruptNow)
            {
                PlayDialogueImmediate(runtimeDialogue);
            }
            else
            {
                QueueDialogue(runtimeDialogue);
            }
        }

        private bool CanInterruptCurrentDialogue(string speaker)
        {
            if (!isPlaying)
            {
                return true;
            }

            if (IsHighPrioritySpeaker(speaker))
            {
                return true;
            }

            float protectedWindow = Mathf.Max(0f, minInterruptProtectSeconds);
            return Time.unscaledTime - currentDialogueStartedAt >= protectedWindow;
        }

        private static bool IsHighPrioritySpeaker(string speaker)
        {
            if (string.IsNullOrWhiteSpace(speaker))
            {
                return false;
            }

            return speaker.Trim().Equals("ALERT", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureQueueProcessorRunning()
        {
            if (queueProcessorCoroutine != null)
            {
                return;
            }

            queueProcessorCoroutine = StartCoroutine(ProcessDialogueQueue());
        }

        private System.Collections.IEnumerator ProcessDialogueQueue()
        {
            while (dialogueQueue.Count > 0)
            {
                while (isPlaying)
                {
                    yield return null;
                }

                if (dialogueDelay > 0f)
                {
                    yield return new WaitForSeconds(dialogueDelay);
                }

                while (isPlaying)
                {
                    yield return null;
                }

                if (dialogueQueue.Count == 0)
                {
                    break;
                }

                var dialogue = dialogueQueue.Dequeue();
                if (dialogue == null)
                {
                    continue;
                }

                if (dialogue.PlayOnce)
                {
                    queuedPlayOnceDialogueIds.Remove(dialogue.DialogueId);
                }

                yield return StartCoroutine(PlayDialogueCoroutine(dialogue));
                yield return new WaitForSeconds(0.5f);
            }

            queueProcessorCoroutine = null;
            if (dialogueQueue.Count > 0 && !isPlaying)
            {
                EnsureQueueProcessorRunning();
            }
        }

        private System.Collections.IEnumerator PlayDialogueCoroutine(DialogueData dialogue)
        {
            isPlaying = true;
            currentDialogue = dialogue;
            currentDialogueStartedAt = Time.unscaledTime;
            EnsureDialogueUI();

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

            bool hasDialogueUI = dialogueUI != null;

            if (hasDialogueUI)
            {
                dialogueUI.ShowDialogue(dialogue);
            }

            foreach (var line in dialogue.Lines)
            {
                OnLineDisplayed?.Invoke(line);

                if (hasDialogueUI)
                {
                    dialogueUI.DisplayLine(line);
                }
                else if (Core.RadioTransmissions.Instance != null)
                {
                    string displayText = !string.IsNullOrEmpty(dialogue.SpeakerName)
                        ? $"{dialogue.SpeakerName}: {line.text}"
                        : line.text;
                    Core.RadioTransmissions.Instance.ShowMessage(displayText, line.displayDuration);
                }

                if (line.voiceClip != null)
                {
                    narrativeAudioSource.PlayOneShot(line.voiceClip);
                }

                if (line.autoAdvance)
                {
                    yield return new WaitForSeconds(line.displayDuration + 0.5f);
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

            if (hasDialogueUI)
            {
                dialogueUI.HideDialogue();
            }

            OnDialogueEnded?.Invoke(dialogue);

            if (dialogue != null && dialogue.IsRuntimeTransient)
            {
                Destroy(dialogue);
            }

            currentDialogue = null;
            isPlaying = false;
        }

        private void EnsureDialogueUI()
        {
            if (dialogueUI != null)
            {
                return;
            }

            dialogueUI = FindFirstObjectByType<DialogueUI>();
            if (dialogueUI != null)
            {
                return;
            }

            var uiObject = new GameObject("DialogueUI");
            dialogueUI = uiObject.AddComponent<DialogueUI>();
        }

        public void SkipCurrentDialogue()
        {
            if (!isPlaying) return;

            StopAllCoroutines();
            queueProcessorCoroutine = null;

            if (dialogueUI != null)
            {
                dialogueUI.HideDialogue(true);
            }

            if (currentDialogue != null && currentDialogue.IsRuntimeTransient)
            {
                Destroy(currentDialogue);
            }

            OnDialogueEnded?.Invoke(currentDialogue);
            currentDialogue = null;
            isPlaying = false;

            if (dialogueQueue.Count > 0)
            {
                EnsureQueueProcessorRunning();
            }
        }

        public void ClearQueue()
        {
            dialogueQueue.Clear();
            queuedPlayOnceDialogueIds.Clear();

            if (!isPlaying && queueProcessorCoroutine != null)
            {
                StopCoroutine(queueProcessorCoroutine);
                queueProcessorCoroutine = null;
            }
        }

        private void CancelAllDialoguePlayback(bool immediateHide)
        {
            StopAllCoroutines();
            queueProcessorCoroutine = null;
            dialogueQueue.Clear();
            queuedPlayOnceDialogueIds.Clear();

            if (dialogueUI != null)
            {
                dialogueUI.HideDialogue(immediateHide);
            }

            if (currentDialogue != null && currentDialogue.IsRuntimeTransient)
            {
                Destroy(currentDialogue);
            }

            if (isPlaying)
            {
                OnDialogueEnded?.Invoke(currentDialogue);
            }

            currentDialogue = null;
            isPlaying = false;
        }

        public void ResetPlayedDialogues()
        {
            playedDialogues.Clear();
        }

        public void ResetRuntimeStateForNewRun(bool clearPlayedDialogues = true)
        {
            CancelAllDialoguePlayback(immediateHide: true);
            if (clearPlayedDialogues)
            {
                playedDialogues.Clear();
            }

            hasTriggeredGameStart = false;
            currentDialogueStartedAt = -999f;
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
