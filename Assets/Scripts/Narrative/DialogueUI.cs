using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Deadlight.Narrative
{
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image speakerPortrait;
        [SerializeField] private Image backgroundImage;
        
        [Header("Typewriter Effect")]
        [SerializeField] private bool useTypewriterEffect = true;
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private AudioClip typewriterSound;
        [SerializeField] private AudioSource typewriterAudioSource;
        
        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        [Header("Skip Indicator")]
        [SerializeField] private GameObject skipIndicator;
        [SerializeField] private TextMeshProUGUI skipText;

        private CanvasGroup canvasGroup;
        private Coroutine typewriterCoroutine;
        private bool isTyping = false;
        private string currentFullText = "";

        private void Awake()
        {
            if (dialoguePanel != null)
            {
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            HideDialogue(true);
        }

        private void Update()
        {
            if (isTyping && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                CompleteTypewriter();
            }
        }

        public void ShowDialogue(DialogueData dialogue)
        {
            if (dialoguePanel == null) return;

            dialoguePanel.SetActive(true);

            if (speakerNameText != null)
            {
                speakerNameText.text = dialogue.SpeakerName;
            }

            if (speakerPortrait != null)
            {
                if (dialogue.SpeakerPortrait != null)
                {
                    speakerPortrait.sprite = dialogue.SpeakerPortrait;
                    speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    speakerPortrait.gameObject.SetActive(false);
                }
            }

            if (skipIndicator != null)
            {
                skipIndicator.SetActive(true);
            }

            StartCoroutine(FadeIn());
        }

        public void DisplayLine(DialogueLine line)
        {
            if (dialogueText == null) return;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            currentFullText = line.text;

            if (useTypewriterEffect)
            {
                typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
            }
            else
            {
                dialogueText.text = line.text;
            }
        }

        private IEnumerator TypewriterEffect(string text)
        {
            isTyping = true;
            dialogueText.text = "";

            foreach (char c in text)
            {
                dialogueText.text += c;

                if (typewriterSound != null && typewriterAudioSource != null && c != ' ')
                {
                    typewriterAudioSource.pitch = Random.Range(0.9f, 1.1f);
                    typewriterAudioSource.PlayOneShot(typewriterSound);
                }

                yield return new WaitForSeconds(typewriterSpeed);
            }

            isTyping = false;
        }

        private void CompleteTypewriter()
        {
            if (!isTyping) return;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            dialogueText.text = currentFullText;
            isTyping = false;
        }

        public void HideDialogue(bool immediate = false)
        {
            if (dialoguePanel == null) return;

            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                isTyping = false;
            }

            if (immediate)
            {
                dialoguePanel.SetActive(false);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                }
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                dialoguePanel.SetActive(false);
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
        }

        public void SetTypewriterSpeed(float speed)
        {
            typewriterSpeed = Mathf.Max(0.01f, speed);
        }

        public void SetUseTypewriter(bool use)
        {
            useTypewriterEffect = use;
        }
    }
}
