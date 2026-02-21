using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Player;

namespace Deadlight.Systems
{
    public class SupplyCrate : MonoBehaviour
    {
        private float interactionTime = 1.5f;
        private float interactProgress;
        private bool isLooting;
        private bool isLooted;
        private Transform player;

        private SpriteRenderer sr;
        private SpriteRenderer glowSr;
        private GameObject progressBarRoot;
        private Image progressFill;
        private Text promptText;
        private Canvas worldCanvas;

        private float pulseTimer;
        private Color baseColor = new Color(0.6f, 0.45f, 0.2f);

        private float interactRange = 1.8f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCrateSprite();
            sr.sortingOrder = 4;
            sr.color = baseColor;

            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = interactRange;

            CreateGlow();
            CreateWorldUI();
        }

        private void Update()
        {
            if (isLooted) return;

            AnimateGlow();

            if (player == null)
            {
                var p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }

            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);
            bool inRange = dist <= interactRange;

            if (promptText != null)
                promptText.gameObject.SetActive(inRange && !isLooting);

            if (inRange && Input.GetKey(KeyCode.F) && !isLooted)
            {
                isLooting = true;
                interactProgress += Time.deltaTime;

                if (progressBarRoot != null)
                {
                    progressBarRoot.SetActive(true);
                    if (progressFill != null)
                        progressFill.fillAmount = interactProgress / interactionTime;
                }

                if (interactProgress >= interactionTime)
                {
                    CompleteLoot();
                }
            }
            else
            {
                if (isLooting)
                {
                    isLooting = false;
                    interactProgress = 0f;
                    if (progressBarRoot != null) progressBarRoot.SetActive(false);
                }
            }
        }

        private void CompleteLoot()
        {
            isLooted = true;
            if (progressBarRoot != null) progressBarRoot.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (glowSr != null) glowSr.gameObject.SetActive(false);

            float roll = Random.value;
            string reward;

            if (roll < 0.40f)
            {
                int ammo = Random.Range(20, 50);
                var shooting = player?.GetComponent<PlayerShooting>();
                shooting?.AddAmmo(ammo);
                reward = $"+{ammo} Ammo";
            }
            else if (roll < 0.65f)
            {
                float heal = Random.Range(15f, 35f);
                var health = player?.GetComponent<PlayerHealth>();
                health?.Heal(heal);
                reward = $"+{Mathf.RoundToInt(heal)} HP";
            }
            else if (roll < 0.85f)
            {
                int pts = Random.Range(30, 80);
                PointsSystem.Instance?.AddPoints(pts, "Supply Crate");
                reward = $"+{pts} Points";
            }
            else
            {
                int pts = Random.Range(50, 120);
                int ammo = Random.Range(15, 30);
                PointsSystem.Instance?.AddPoints(pts, "Supply Cache Bonus");
                var shooting = player?.GetComponent<PlayerShooting>();
                shooting?.AddAmmo(ammo);
                reward = $"+{pts} Pts +{ammo} Ammo";
            }

            if (FloatingTextManager.Instance != null)
                FloatingTextManager.Instance.SpawnText(reward, transform.position + Vector3.up * 0.5f, new Color(1f, 0.9f, 0.3f));

            if (DayObjectiveSystem.Instance != null)
                DayObjectiveSystem.Instance.AddProgress(1);

            try
            {
                var clip = Audio.ProceduralAudioGenerator.GeneratePickup();
                if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, 0.6f);
            }
            catch { }

            sr.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Destroy(gameObject, 1f);
        }

        private void AnimateGlow()
        {
            if (glowSr == null) return;
            pulseTimer += Time.deltaTime * 2f;
            float alpha = Mathf.Lerp(0.15f, 0.5f, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            glowSr.color = new Color(1f, 0.85f, 0.3f, alpha);
        }

        private void CreateGlow()
        {
            var glowObj = new GameObject("CrateGlow");
            glowObj.transform.SetParent(transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * 2f;
            glowSr = glowObj.AddComponent<SpriteRenderer>();
            glowSr.sprite = CreateCircleSprite(Color.white);
            glowSr.sortingOrder = 3;
            glowSr.color = new Color(1f, 0.85f, 0.3f, 0.3f);
        }

        private void CreateWorldUI()
        {
            var canvasObj = new GameObject("CrateCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.zero;

            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.sortingOrder = 100;

            var rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, 1);
            canvasObj.transform.localScale = Vector3.one * 0.01f;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(canvasObj.transform, false);
            var pRect = promptObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0, 80);
            pRect.sizeDelta = new Vector2(200, 30);
            promptText = promptObj.AddComponent<Text>();
            promptText.text = "[F] Loot";
            promptText.font = font;
            promptText.fontSize = 22;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = new Color(1f, 0.95f, 0.6f);
            promptObj.SetActive(false);

            progressBarRoot = new GameObject("ProgressBar");
            progressBarRoot.transform.SetParent(canvasObj.transform, false);
            var pbRect = progressBarRoot.AddComponent<RectTransform>();
            pbRect.anchoredPosition = new Vector2(0, 55);
            pbRect.sizeDelta = new Vector2(150, 12);

            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(progressBarRoot.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(progressBarRoot.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            progressFill = fillObj.AddComponent<Image>();
            progressFill.color = new Color(0.3f, 0.9f, 0.3f);
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;

            progressBarRoot.SetActive(false);
        }

        private static Sprite CreateCrateSprite()
        {
            int s = 24;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    bool border = x < 2 || x >= s - 2 || y < 2 || y >= s - 2;
                    bool cross = Mathf.Abs(x - s / 2) < 2 || Mathf.Abs(y - s / 2) < 2;
                    if (border)
                        px[y * s + x] = new Color(0.35f, 0.25f, 0.1f);
                    else if (cross)
                        px[y * s + x] = new Color(0.45f, 0.35f, 0.15f);
                    else
                        px[y * s + x] = new Color(0.55f, 0.42f, 0.2f);
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateCircleSprite(Color color)
        {
            int s = 32;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            Vector2 center = new Vector2(s / 2f, s / 2f);
            float radius = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(1f - d / radius);
                    px[y * s + x] = new Color(color.r, color.g, color.b, a * 0.5f);
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
    }
}
