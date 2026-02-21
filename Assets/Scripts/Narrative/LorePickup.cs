using UnityEngine;

namespace Deadlight.Narrative
{
    public class LorePickup : MonoBehaviour
    {
        [Header("Lore Configuration")]
        [SerializeField] private string loreId;
        [SerializeField] private bool destroyOnPickup = true;
        
        [Header("Interaction")]
        [SerializeField] private bool requireInteraction = true;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;
        [SerializeField] private float interactionRange = 1.5f;
        
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
        }

        private void Update()
        {
            if (isCollected) return;

            CheckPlayerDistance();

            if (playerInRange && Input.GetKeyDown(interactionKey))
            {
                Collect();
            }
        }

        private void CheckPlayerDistance()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = playerInRange;
            playerInRange = distance <= interactionRange;

            if (playerInRange != wasInRange)
            {
                OnPlayerRangeChanged(playerInRange);
            }
        }

        private void OnPlayerRangeChanged(bool inRange)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(inRange);
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

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
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
