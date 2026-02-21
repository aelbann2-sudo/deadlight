using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Deadlight.Systems
{
    public class FloatingTextManager : MonoBehaviour
    {
        public static FloatingTextManager Instance { get; private set; }

        private Canvas worldCanvas;
        private List<FloatingText> activeTexts = new List<FloatingText>();
        private Font font;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            CreateWorldCanvas();
            LoadFont();
        }

        private void CreateWorldCanvas()
        {
            var canvasObj = new GameObject("FloatingTextCanvas");
            canvasObj.transform.SetParent(transform);
            
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 100;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;
        }

        private void LoadFont()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }
        }

        public void SpawnPointsText(int points, Vector3 position, string bonusTag = null)
        {
            Color color = points switch
            {
                >= 50 => new Color(1f, 0.6f, 0.2f),
                >= 25 => new Color(0.8f, 0.3f, 1f),
                >= 15 => new Color(1f, 0.9f, 0.3f),
                _ => Color.white
            };

            string text = $"+{points}";
            if (!string.IsNullOrEmpty(bonusTag))
            {
                text += $" {bonusTag}";
            }

            SpawnText(text, position, color, 1f, 24);
        }

        public void SpawnText(string text, Vector3 position, Color color, float duration = 1f, int fontSize = 24)
        {
            var textObj = new GameObject("FloatingText");
            textObj.transform.SetParent(worldCanvas.transform);
            textObj.transform.position = position;
            textObj.transform.localScale = Vector3.one * 0.008f;

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = font;
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            var floatingText = textObj.AddComponent<FloatingText>();
            floatingText.Initialize(duration);
            activeTexts.Add(floatingText);
        }

        public void SpawnKillStreakText(string message, Color color)
        {
            var player = GameObject.Find("Player");
            Vector3 pos = player != null ? player.transform.position + Vector3.up * 1.5f : Vector3.zero;
            SpawnText(message, pos, color, 2f, 36);
        }
    }

    public class FloatingText : MonoBehaviour
    {
        private float duration;
        private float elapsed;
        private Vector3 velocity;
        private Text textComponent;
        private Color startColor;

        public void Initialize(float dur)
        {
            duration = dur;
            velocity = new Vector3(Random.Range(-0.3f, 0.3f), 1.5f, 0);
            textComponent = GetComponent<Text>();
            if (textComponent != null)
            {
                startColor = textComponent.color;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position += velocity * Time.deltaTime;
            velocity.y -= Time.deltaTime * 2f;

            if (textComponent != null)
            {
                float alpha = 1f - Mathf.Pow(t, 2);
                textComponent.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            float scale = 0.008f * (1f + Mathf.Sin(t * Mathf.PI) * 0.3f);
            transform.localScale = Vector3.one * scale;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
