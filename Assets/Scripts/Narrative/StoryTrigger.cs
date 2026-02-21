using UnityEngine;

namespace Deadlight.Narrative
{
    [RequireComponent(typeof(Collider2D))]
    public class StoryTrigger : MonoBehaviour
    {
        [Header("Trigger Configuration")]
        [SerializeField] private string triggerId;
        [SerializeField] private DialogueData dialogueToPlay;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private bool requirePlayerTag = true;
        
        [Header("Trigger Conditions")]
        [SerializeField] private bool requireNightPhase = false;
        [SerializeField] private bool requireDayPhase = false;
        [SerializeField] private int minimumNight = 1;
        [SerializeField] private int maximumNight = 5;
        
        [Header("Interaction")]
        [SerializeField] private bool requireInteraction = false;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private Color triggerColor = new Color(1f, 1f, 0f, 0.3f);
        
        [Header("State")]
        [SerializeField] private bool hasTriggered = false;

        public string TriggerId => string.IsNullOrEmpty(triggerId) ? gameObject.name : triggerId;
        public bool HasTriggered => hasTriggered;

        private bool playerInRange = false;
        private Collider2D triggerCollider;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            if (requireInteraction && playerInRange && !hasTriggered)
            {
                if (Input.GetKeyDown(interactionKey))
                {
                    TryTrigger();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (requirePlayerTag && !other.CompareTag("Player")) return;
            if (hasTriggered && triggerOnce) return;

            playerInRange = true;

            if (requireInteraction)
            {
                ShowInteractionPrompt(true);
            }
            else
            {
                TryTrigger();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (requirePlayerTag && !other.CompareTag("Player")) return;

            playerInRange = false;
            ShowInteractionPrompt(false);
        }

        private void TryTrigger()
        {
            if (!CanTrigger()) return;

            hasTriggered = true;
            ShowInteractionPrompt(false);

            if (dialogueToPlay != null && NarrativeManager.Instance != null)
            {
                NarrativeManager.Instance.PlayDialogueImmediate(dialogueToPlay);
            }

            OnTriggered();
        }

        private bool CanTrigger()
        {
            if (hasTriggered && triggerOnce) return false;

            int currentNight = Core.GameManager.Instance?.CurrentNight ?? 1;
            if (currentNight < minimumNight || currentNight > maximumNight) return false;

            if (requireNightPhase || requireDayPhase)
            {
                var currentState = Core.GameManager.Instance?.CurrentState ?? Core.GameState.DayPhase;
                
                if (requireNightPhase && currentState != Core.GameState.NightPhase) return false;
                if (requireDayPhase && currentState != Core.GameState.DayPhase) return false;
            }

            return true;
        }

        protected virtual void OnTriggered()
        {
            Debug.Log($"[StoryTrigger] '{TriggerId}' triggered");
        }

        private void ShowInteractionPrompt(bool show)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(show);
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        public void SetDialogue(DialogueData dialogue)
        {
            dialogueToPlay = dialogue;
        }

        private void OnDrawGizmos()
        {
            Color gizmoColor = hasTriggered ? Color.gray : triggerColor;
            gizmoColor.a = 0.3f;
            Gizmos.color = gizmoColor;

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                if (col is BoxCollider2D box)
                {
                    Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
                }
                else if (col is CircleCollider2D circle)
                {
                    Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
                }
            }
            else
            {
                Gizmos.DrawCube(transform.position, Vector3.one);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            string info = $"{TriggerId}\n" +
                         $"Dialogue: {(dialogueToPlay != null ? dialogueToPlay.name : "None")}\n" +
                         $"Once: {triggerOnce}\n" +
                         $"Nights: {minimumNight}-{maximumNight}";
            UnityEditor.Handles.Label(transform.position + Vector3.up, info);
            #endif
        }
    }
}
