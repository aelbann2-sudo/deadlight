using UnityEngine;
using Deadlight.Core;
using Deadlight.Player;

namespace Deadlight.Systems
{
    public class CorruptionSystem : MonoBehaviour
    {
        public static CorruptionSystem Instance { get; private set; }

        [Header("Corruption Settings")]
        [SerializeField] private float stationaryThreshold = 3f;
        [SerializeField] private float corruptionBuildRate = 0.15f;
        [SerializeField] private float corruptionDecayRate = 0.3f;
        [SerializeField] private float maxCorruption = 1f;

        [Header("Effects")]
        [SerializeField] private float dotDamagePerSecond = 2f;
        [SerializeField] private float spawnRateMultiplier = 1.5f;
        [SerializeField] private float darknessMultiplier = 0.5f;

        [Header("Visual")]
        [SerializeField] private Color corruptionTint = new Color(0.3f, 0f, 0.1f, 0.3f);

        private float currentCorruption;
        private Vector3 lastPlayerPosition;
        private float stationaryTime;
        private GameObject player;
        private SpriteRenderer corruptionOverlay;
        private bool isActive;

        public float CorruptionLevel => currentCorruption;
        public float SpawnMultiplier => 1f + (currentCorruption * (spawnRateMultiplier - 1f));
        public bool IsCorrupted => currentCorruption > 0.3f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            player = GameObject.Find("Player");
            if (player != null)
            {
                lastPlayerPosition = player.transform.position;
            }

            CreateCorruptionOverlay();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            isActive = state == GameState.NightPhase;
            if (!isActive)
            {
                currentCorruption = 0f;
                UpdateVisuals();
            }
        }

        private void Update()
        {
            if (!isActive || player == null) return;

            float distMoved = Vector3.Distance(player.transform.position, lastPlayerPosition);
            
            if (distMoved < 0.5f)
            {
                stationaryTime += Time.deltaTime;
                
                if (stationaryTime >= stationaryThreshold)
                {
                    currentCorruption += corruptionBuildRate * Time.deltaTime;
                    currentCorruption = Mathf.Min(currentCorruption, maxCorruption);
                }
            }
            else
            {
                stationaryTime = 0f;
                currentCorruption -= corruptionDecayRate * Time.deltaTime;
                currentCorruption = Mathf.Max(currentCorruption, 0f);
            }

            lastPlayerPosition = player.transform.position;

            if (currentCorruption > 0.3f)
            {
                ApplyCorruptionEffects();
            }

            UpdateVisuals();
        }

        private void ApplyCorruptionEffects()
        {
            if (currentCorruption > 0.5f)
            {
                float dotDamage = dotDamagePerSecond * (currentCorruption - 0.5f) * 2f * Time.deltaTime;
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null && dotDamage > 0.01f)
                {
                    playerHealth.TakeDamage(dotDamage);
                }
            }

            if (currentCorruption > 0.7f && Random.value < 0.01f)
            {
                ShowCorruptionWarning();
            }
        }

        private void CreateCorruptionOverlay()
        {
            var overlayObj = new GameObject("CorruptionOverlay");
            overlayObj.transform.SetParent(transform);
            
            corruptionOverlay = overlayObj.AddComponent<SpriteRenderer>();
            corruptionOverlay.sprite = CreateOverlaySprite();
            corruptionOverlay.sortingOrder = 1000;
            corruptionOverlay.color = Color.clear;

            if (Camera.main != null)
            {
                overlayObj.transform.SetParent(Camera.main.transform);
                overlayObj.transform.localPosition = new Vector3(0, 0, 5);
                overlayObj.transform.localScale = Vector3.one * 30f;
            }
        }

        private Sprite CreateOverlaySprite()
        {
            int size = 64;
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = Mathf.Pow(dist, 2f);
                    pixels[y * size + x] = new Color(0.2f, 0f, 0.05f, alpha * 0.7f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        private void UpdateVisuals()
        {
            if (corruptionOverlay == null) return;

            float alpha = currentCorruption * 0.6f;
            Color c = corruptionTint;
            c.a = alpha;
            corruptionOverlay.color = c;

            float pulse = 1f + Mathf.Sin(Time.time * 3f * currentCorruption) * currentCorruption * 0.1f;
            corruptionOverlay.transform.localScale = Vector3.one * 30f * pulse;
        }

        private void ShowCorruptionWarning()
        {
            if (RadioTransmissions.Instance != null)
            {
                string[] warnings = {
                    "MOVE! The corruption is closing in!",
                    "You can't stay there! The darkness is spreading!",
                    "GET OUT! The infection is overwhelming!",
                    "Warning: Stationary position compromised!"
                };
                RadioTransmissions.Instance.ShowMessage(warnings[Random.Range(0, warnings.Length)], 2f);
            }

            if (FloatingTextManager.Instance != null && player != null)
            {
                FloatingTextManager.Instance.SpawnText("MOVE!", player.transform.position + Vector3.up, 
                    new Color(0.8f, 0.2f, 0.3f), 1.5f, 32);
            }
        }

        public void ResetCorruption()
        {
            currentCorruption = 0f;
            stationaryTime = 0f;
        }
    }
}
