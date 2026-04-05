using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Player;

namespace Deadlight.Systems
{
    public enum CrateTier { Common, Rare, Legendary }
    public enum CrateContents { Ammo, Health, Points, Powerup, Armor }

    public class SupplyCrate : MonoBehaviour
    {
        private float interactionTime = 1.5f;
        private float interactProgress;
        private bool isLooting;
        private bool isLooted;
        private Transform player;

        private SpriteRenderer sr;
        private SpriteRenderer glowSr;
        private SpriteRenderer contentsIconSr;
        private GameObject progressBarRoot;
        private Image progressFill;
        private Text promptText;
        private Canvas worldCanvas;

        private float pulseTimer;
        private float interactRange = 1.8f;

        private CrateTier tier = CrateTier.Common;
        private CrateContents contents;
        private bool initialized;
        private bool isContestedDrop;
        private bool contestedResolved;
        private float contestedLifetime;
        private float contestedExpirySeconds = 20f;
        private System.Action<SupplyCrate> onContestedSecured;
        private System.Action<SupplyCrate> onContestedExpired;

        private static readonly Color CommonColor = new Color(0.6f, 0.45f, 0.2f);
        private static readonly Color RareColor = new Color(0.3f, 0.5f, 1f);
        private static readonly Color LegendaryColor = new Color(1f, 0.85f, 0.2f);

        public void SetTier(CrateTier t)
        {
            tier = t;
            if (initialized)
            {
                sr.color = GetTierColor();
                if (glowSr != null) glowSr.color = GetGlowColor();
                if (promptText != null) promptText.color = GetTierColor();
                if (progressFill != null) progressFill.color = GetTierColor();
            }
        }

        public void ConfigureContested(float secureHoldTime, float expirySeconds,
            System.Action<SupplyCrate> onSecured = null, System.Action<SupplyCrate> onExpired = null)
        {
            isContestedDrop = true;
            contestedResolved = false;
            contestedLifetime = 0f;
            interactionTime = Mathf.Max(0.8f, secureHoldTime);
            contestedExpirySeconds = Mathf.Max(interactionTime + 1f, expirySeconds);
            onContestedSecured = onSecured;
            onContestedExpired = onExpired;

            if (promptText != null)
            {
                promptText.text = "[F] Secure Drop";
                promptText.color = new Color(1f, 0.85f, 0.35f);
            }

            if (contentsIconSr != null)
            {
                contentsIconSr.gameObject.SetActive(false);
            }
        }

        private void Awake()
        {
            if (!initialized) InitVisuals();
        }

        private void InitVisuals()
        {
            initialized = true;
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCrateSprite();
            sr.sortingOrder = 4;
            sr.color = GetTierColor();

            var col = gameObject.GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = interactRange;
            }

            CreateGlow();
            CreateWorldUI();
            DetermineContents();
            CreateContentsIcon();
        }

        private Color GetTierColor()
        {
            return tier switch
            {
                CrateTier.Legendary => LegendaryColor,
                CrateTier.Rare => RareColor,
                _ => CommonColor
            };
        }

        private Color GetGlowColor()
        {
            return tier switch
            {
                CrateTier.Legendary => new Color(1f, 0.85f, 0.3f, 0.3f),
                CrateTier.Rare => new Color(0.3f, 0.5f, 1f, 0.3f),
                _ => new Color(1f, 0.85f, 0.3f, 0.15f)
            };
        }

        private void DetermineContents()
        {
            var playerObj = GameObject.Find("Player");
            var health = playerObj?.GetComponent<PlayerHealth>();
            var shooting = playerObj?.GetComponent<PlayerShooting>();

            float healthPct = health != null ? health.HealthPercentage : 1f;
            float ammoPct = shooting != null ? (float)shooting.ReserveAmmo / 200f : 1f;

            if (tier >= CrateTier.Rare && Random.value < 0.20f)
            {
                contents = CrateContents.Armor;
                return;
            }

            if (tier == CrateTier.Legendary && Random.value < 0.40f)
            {
                contents = CrateContents.Powerup;
                return;
            }
            if (tier == CrateTier.Rare && Random.value < 0.15f)
            {
                contents = CrateContents.Powerup;
                return;
            }

            if (healthPct < 0.3f) { contents = CrateContents.Health; return; }
            if (ammoPct < 0.2f) { contents = CrateContents.Ammo; return; }
            if (healthPct < 0.5f && Random.value < 0.6f) { contents = CrateContents.Health; return; }
            if (ammoPct < 0.4f && Random.value < 0.6f) { contents = CrateContents.Ammo; return; }

            float roll = Random.value;
            if (roll < 0.35f) contents = CrateContents.Ammo;
            else if (roll < 0.60f) contents = CrateContents.Health;
            else contents = CrateContents.Points;
        }

        private void CreateContentsIcon()
        {
            var iconObj = new GameObject("ContentsIcon");
            iconObj.transform.SetParent(transform);
            iconObj.transform.localPosition = new Vector3(0, 0.8f, 0);
            iconObj.transform.localScale = Vector3.one * 0.6f;
            contentsIconSr = iconObj.AddComponent<SpriteRenderer>();
            contentsIconSr.sortingOrder = 6;

            Color iconColor;
            Sprite iconSprite;
            switch (contents)
            {
                case CrateContents.Ammo:
                    iconColor = new Color(1f, 0.9f, 0.3f);
                    iconSprite = CreateBulletIcon();
                    break;
                case CrateContents.Health:
                    iconColor = new Color(0.3f, 1f, 0.3f);
                    iconSprite = CreateCrossIcon();
                    break;
                case CrateContents.Points:
                    iconColor = new Color(1f, 0.85f, 0.1f);
                    iconSprite = CreateCoinIcon();
                    break;
                case CrateContents.Powerup:
                    iconColor = new Color(0.7f, 0.3f, 1f);
                    iconSprite = CreateStarIcon();
                    break;
                case CrateContents.Armor:
                    iconColor = new Color(0.3f, 0.5f, 0.9f);
                    iconSprite = CreateShieldIcon();
                    break;
                default:
                    iconColor = Color.white;
                    iconSprite = CreateCoinIcon();
                    break;
            }

            contentsIconSr.sprite = iconSprite;
            contentsIconSr.color = iconColor;
        }

        private void Update()
        {
            if (isLooted) return;

            AnimateGlow();
            AnimateContentsIcon();

            if (isContestedDrop && !contestedResolved)
            {
                contestedLifetime += Time.deltaTime;
                if (contestedLifetime >= contestedExpirySeconds)
                {
                    ExpireContestedDrop();
                    return;
                }
            }

            if (player == null)
            {
                var p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }
            if (player == null) return;

            float dist = Vector2.Distance(transform.position, player.position);
            bool inRange = dist <= interactRange;

            if (promptText != null)
            {
                if (isContestedDrop && !contestedResolved)
                {
                    int remaining = Mathf.Max(0, Mathf.CeilToInt(contestedExpirySeconds - contestedLifetime));
                    promptText.text = $"Hold F Secure ({remaining}s)";
                }
                promptText.gameObject.SetActive(inRange && !isLooting);
            }

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
                    CompleteLoot();
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
            contestedResolved = true;
            if (progressBarRoot != null) progressBarRoot.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (glowSr != null) glowSr.gameObject.SetActive(false);
            if (contentsIconSr != null) contentsIconSr.gameObject.SetActive(false);

            int night = GameManager.Instance?.CurrentNight ?? 1;
            float nightMult = 1f + (night - 1) * 0.25f;
            float tierMult = tier switch { CrateTier.Legendary => 2f, CrateTier.Rare => 1.5f, _ => 1f };
            string reward = "Loot!";

            if (isContestedDrop)
            {
                reward = GrantContestedRewards();
                onContestedSecured?.Invoke(this);
            }
            else
            {
                var gameplayHelp = Deadlight.UI.GameplayHelpSystem.Instance;

                switch (contents)
                {
                    case CrateContents.Ammo:
                        int ammo = Mathf.RoundToInt(Random.Range(20, 50) * nightMult * tierMult);
                        player?.GetComponent<PlayerShooting>()?.AddAmmo(ammo);
                        gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Ammo, ammo);
                        reward = $"+{ammo} Ammo";
                        break;
                    case CrateContents.Health:
                        float heal = Random.Range(15f, 35f) * nightMult * tierMult;
                        player?.GetComponent<PlayerHealth>()?.Heal(heal);
                        gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Health, Mathf.RoundToInt(heal));
                        reward = $"+{Mathf.RoundToInt(heal)} HP";
                        break;
                    case CrateContents.Points:
                        int pts = Mathf.RoundToInt(Random.Range(30, 80) * nightMult * tierMult);
                        PointsSystem.Instance?.AddPoints(pts, "Supply Crate");
                        gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Points, pts);
                        reward = $"+{pts} Points";
                        break;
                    case CrateContents.Powerup:
                        var ps = FindFirstObjectByType<PowerupSystem>();
                        if (ps != null) ps.GrantRandomPowerup();
                        gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Powerup, 1);
                        reward = "POWERUP!";
                        break;
                    case CrateContents.Armor:
                        ArmorTier armorTier = tier == CrateTier.Legendary
                            ? (ArmorTier)Random.Range(2, 4)
                            : ArmorTier.Level1;
                        bool isHelmet = Random.value < 0.4f;
                        if (isHelmet)
                        {
                            PlayerArmor.Instance?.EquipHelmet(armorTier);
                            reward = $"Lv{(int)armorTier} Helmet!";
                        }
                        else
                        {
                            PlayerArmor.Instance?.EquipVest(armorTier);
                            reward = $"Lv{(int)armorTier} Vest!";
                        }
                        gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Armor, 1);
                        break;
                    default:
                        reward = "Loot!";
                        break;
                }
            }

            if (FloatingTextManager.Instance != null)
                FloatingTextManager.Instance.SpawnText(reward, transform.position + Vector3.up * 0.5f, GetTierColor());

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

        private string GrantContestedRewards()
        {
            int bonusPoints;
            int ammo;
            float heal;

            switch (tier)
            {
                case CrateTier.Legendary:
                    bonusPoints = 260;
                    ammo = 90;
                    heal = 45f;
                    break;
                case CrateTier.Rare:
                    bonusPoints = 170;
                    ammo = 60;
                    heal = 30f;
                    break;
                default:
                    bonusPoints = 110;
                    ammo = 35;
                    heal = 18f;
                    break;
            }

            PointsSystem.Instance?.AddPoints(bonusPoints, "Contested Drop");

            var playerObj = GameObject.Find("Player");
            var shooting = playerObj != null ? playerObj.GetComponent<PlayerShooting>() : null;
            var health = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;

            shooting?.AddAmmo(ammo);
            health?.Heal(heal);

            var help = Deadlight.UI.GameplayHelpSystem.Instance;
            help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Points, bonusPoints);
            help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Ammo, ammo);
            help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Health, Mathf.RoundToInt(heal));

            var directSummary = new StringBuilder("DROP SECURED: ");
            directSummary.Append($"+{bonusPoints} Points, +{ammo} Ammo, +{Mathf.RoundToInt(heal)} HP");
            return directSummary.ToString();
        }

        private void ExpireContestedDrop()
        {
            if (contestedResolved || isLooted)
            {
                return;
            }

            contestedResolved = true;
            isLooted = true;
            isLooting = false;
            interactProgress = 0f;

            if (progressBarRoot != null) progressBarRoot.SetActive(false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (glowSr != null) glowSr.gameObject.SetActive(false);
            if (contentsIconSr != null) contentsIconSr.gameObject.SetActive(false);

            sr.color = new Color(0.3f, 0.1f, 0.1f, 0.45f);

            if (FloatingTextManager.Instance != null)
            {
                FloatingTextManager.Instance.SpawnText("DROP EXPIRED", transform.position + Vector3.up * 0.5f, new Color(1f, 0.35f, 0.35f));
            }

            onContestedExpired?.Invoke(this);
            Destroy(gameObject, 1f);
        }

        private void AnimateGlow()
        {
            if (glowSr == null) return;
            pulseTimer += Time.deltaTime * (tier == CrateTier.Legendary ? 3f : 2f);
            float alpha = Mathf.Lerp(0.15f, tier == CrateTier.Legendary ? 0.7f : 0.5f,
                (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            var gc = GetGlowColor();
            glowSr.color = new Color(gc.r, gc.g, gc.b, alpha);
        }

        private void AnimateContentsIcon()
        {
            if (contentsIconSr == null) return;
            float bob = Mathf.Sin(Time.time * 2f) * 0.1f;
            contentsIconSr.transform.localPosition = new Vector3(0, 0.8f + bob, 0);
        }

        private void CreateGlow()
        {
            var glowObj = new GameObject("CrateGlow");
            glowObj.transform.SetParent(transform);
            glowObj.transform.localPosition = Vector3.zero;
            float glowScale = tier == CrateTier.Legendary ? 3f : tier == CrateTier.Rare ? 2.5f : 2f;
            glowObj.transform.localScale = Vector3.one * glowScale;
            glowSr = glowObj.AddComponent<SpriteRenderer>();
            glowSr.sprite = CreateCircleSprite(Color.white);
            glowSr.sortingOrder = 3;
            glowSr.color = GetGlowColor();
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
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            var promptObj = new GameObject("Prompt");
            promptObj.transform.SetParent(canvasObj.transform, false);
            var pRect = promptObj.AddComponent<RectTransform>();
            pRect.anchoredPosition = new Vector2(0, 80);
            pRect.sizeDelta = new Vector2(200, 30);
            promptText = promptObj.AddComponent<Text>();
            string tierLabel = tier == CrateTier.Legendary ? "[F] LEGENDARY" :
                               tier == CrateTier.Rare ? "[F] Rare Crate" : "[F] Loot";
            promptText.text = tierLabel;
            promptText.font = font;
            promptText.fontSize = 22;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = GetTierColor();
            promptObj.SetActive(false);

            progressBarRoot = new GameObject("ProgressBar");
            progressBarRoot.transform.SetParent(canvasObj.transform, false);
            var pbRect = progressBarRoot.AddComponent<RectTransform>();
            pbRect.anchoredPosition = new Vector2(0, 55);
            pbRect.sizeDelta = new Vector2(150, 12);

            var bgObj = new GameObject("BG");
            bgObj.transform.SetParent(progressBarRoot.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bgObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(progressBarRoot.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            progressFill = fillObj.AddComponent<Image>();
            progressFill.color = GetTierColor();
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillAmount = 0f;

            progressBarRoot.SetActive(false);
        }

        // Enhanced crate visuals based on tier and content
        private Sprite CreateCrateSprite()
        {
            int s = 24;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            
            // Different crate designs based on tier
            switch (tier)
            {
                case CrateTier.Legendary:
                    // High-tech crate with metallic look
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            bool border = x < 2 || x >= s - 2 || y < 2 || y >= s - 2;
                            bool highlight = x == 3 || x == s - 4 || y == 3 || y == s - 4;
                            px[y * s + x] = border ? new Color(0.8f, 0.7f, 0.2f) :
                                            highlight ? new Color(1f, 0.9f, 0.4f) :
                                            new Color(0.7f, 0.6f, 0.1f);
                        }
                    break;
                    
                case CrateTier.Rare:
                    // Reinforced military crate
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            bool border = x < 2 || x >= s - 2 || y < 2 || y >= s - 2;
                            bool rivet = (x == 4 || x == s-5) && (y == 4 || y == s-5);
                            px[y * s + x] = border ? new Color(0.2f, 0.3f, 0.6f) :
                                            rivet ? new Color(0.8f, 0.8f, 0.9f) :
                                            new Color(0.3f, 0.4f, 0.7f);
                        }
                    break;
                    
                default:
                    // Standard wooden crate
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            bool border = x < 2 || x >= s - 2 || y < 2 || y >= s - 2;
                            bool plank = (y % 4 == 0) && (x > 2 && x < s - 2);
                            px[y * s + x] = border ? new Color(0.35f, 0.25f, 0.1f) :
                                            plank ? new Color(0.5f, 0.4f, 0.2f) :
                                            new Color(0.55f, 0.42f, 0.2f);
                        }
                    break;
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
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float a = Mathf.Clamp01(1f - d / (s / 2f));
                    px[y * s + x] = new Color(color.r, color.g, color.b, a * 0.5f);
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
        private static Sprite CreateBulletIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int y = 2; y < 14; y++)
                for (int x = 6; x < 10; x++)
                    px[y * s + x] = Color.white;
            for (int y = 12; y < 15; y++)
                for (int x = 5; x < 11; x++)
                    px[y * s + x] = new Color(0.8f, 0.6f, 0.2f);
            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateCrossIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int y = 3; y < 13; y++)
                for (int x = 6; x < 10; x++) px[y * s + x] = Color.white;
            for (int y = 6; y < 10; y++)
                for (int x = 3; x < 13; x++) px[y * s + x] = Color.white;
            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateCoinIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            Vector2 c = new Vector2(8, 8);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    if (Vector2.Distance(new Vector2(x, y), c) < 6)
                        px[y * s + x] = Color.white;
            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateStarIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int y = 5; y < 11; y++)
                for (int x = 2; x < 14; x++) px[y * s + x] = Color.white;
            for (int y = 2; y < 14; y++)
                for (int x = 5; x < 11; x++) px[y * s + x] = Color.white;
            for (int y = 3; y < 13; y++)
                for (int x = 3; x < 13; x++)
                    if (Mathf.Abs(x - 8) + Mathf.Abs(y - 8) < 7)
                        px[y * s + x] = Color.white;
            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateShieldIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int y = 2; y < 14; y++)
            {
                float widthFrac = y < 8 ? 1f : 1f - (y - 8) / 8f;
                int hw = Mathf.RoundToInt(6 * widthFrac);
                for (int x = 8 - hw; x < 8 + hw; x++)
                    if (x >= 0 && x < s) px[y * s + x] = Color.white;
            }
            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
    }
}
