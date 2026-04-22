using UnityEngine;
using Deadlight.Core;
using Deadlight.Systems;
using Deadlight.UI;
using UnityEngine.UI;

namespace Deadlight.Narrative
{
    public class LorePickup : MonoBehaviour
    {
        [Header("Lore Configuration")]
        [SerializeField] private string loreId;
        [SerializeField] private bool destroyOnPickup = true;
        
        [Header("Interaction")]
        [SerializeField] private bool requireInteraction = true;
        [SerializeField] private KeyCode interactionKey = KeyCode.F;
        [SerializeField] private float interactionRange = 2.2f;
        
        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private GameObject glowEffect;
        
        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;

        private bool playerInRange = false;
        private bool isCollected = false;
        private Color originalColor;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
            else
            {
                CreateDefaultInteractionPrompt();
            }
        }

        private void Update()
        {
            if (isCollected) return;

            CheckPlayerDistance();

            if (!playerInRange)
            {
                return;
            }

            if (!requireInteraction || Input.GetKeyDown(interactionKey))
            {
                Collect();
            }
        }

        private void CheckPlayerDistance()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            bool wasInRange = playerInRange;
            var playerCollider = player.GetComponent<Collider2D>();
            playerInRange = playerCollider != null
                ? PickupContactUtility.IsWithinPickupRange(transform, spriteRenderer, playerCollider, interactionRange)
                : Vector3.Distance(transform.position, player.transform.position) <= interactionRange;

            if (playerInRange != wasInRange)
            {
                OnPlayerRangeChanged(playerInRange);
            }
        }

        private void OnPlayerRangeChanged(bool inRange)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(inRange && requireInteraction);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = inRange ? highlightColor : originalColor;
            }
        }

        private void Collect()
        {
            if (isCollected) return;
            if (string.IsNullOrEmpty(loreId)) return;

            isCollected = true;

            if (EnvironmentalLore.Instance != null)
            {
                EnvironmentalLore.Instance.DiscoverLore(loreId);
            }

            GameplayHelpSystem.Instance?.ShowItem(GameplayGuideContent.ItemIds.LoreIntel, 1);

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            else
            {
                AudioManager.Instance?.PlaySFX("pickup", 0.5f);
            }

            if (destroyOnPickup)
            {
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
                
                Destroy(gameObject, 0.1f);
            }
            else
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.gray;
                }
                
                if (glowEffect != null)
                {
                    glowEffect.SetActive(false);
                }
            }
        }

        private void CreateDefaultInteractionPrompt()
        {
            if (!requireInteraction || interactionPrompt != null)
            {
                return;
            }

            var root = new GameObject("LorePrompt");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0.72f, 0f);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 5) + 8;

            var canvasRect = root.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(230f, 36f);
            root.transform.localScale = Vector3.one * 0.01f;

            var textObj = new GameObject("PromptText");
            textObj.transform.SetParent(root.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var promptText = textObj.AddComponent<Text>();
            promptText.text = "Press F to Collect";
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.fontSize = 20;
            promptText.fontStyle = FontStyle.Bold;
            promptText.color = Color.white;
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                              Font.CreateDynamicFontFromOSFont("Arial", 20);

            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            interactionPrompt = root;
            interactionPrompt.SetActive(false);
        }

        public void SetLoreId(string id)
        {
            loreId = id;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isCollected ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }

        private void OnDrawGizmosSelected()
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
                $"Lore: {loreId}\nCollected: {isCollected}");
            #endif
        }
    }
}
