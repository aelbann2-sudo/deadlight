using System;
using System.Collections.Generic;
using System.Text;
using Deadlight.Core;
using Deadlight.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.Systems
{
    public enum CraftingRecipeId
    {
        AmmoCache,
        FieldMed,
        ShockBeacon,
        WeakpointIntel,
        TacticalPrep
    }

    [Serializable]
    public class CraftingRecipeDefinition
    {
        public CraftingRecipeId id;
        public string displayName;
        public string nightEffectDescription;
        public List<ResourceAmount> costs = new List<ResourceAmount>();
        public int perDayCap = 1;

        public CraftingRecipeDefinition(CraftingRecipeId id, string displayName, string nightEffectDescription, int perDayCap, List<ResourceAmount> costs)
        {
            this.id = id;
            this.displayName = displayName;
            this.nightEffectDescription = nightEffectDescription;
            this.perDayCap = Mathf.Max(1, perDayCap);
            this.costs = costs ?? new List<ResourceAmount>();
        }
    }

    [Serializable]
    public class NightPrepSnapshot
    {
        public int ammoReserveGrant;
        public float healGrant;
        public float enemySpeedMultiplier = 1f;
        public float enemyHealthMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;
        public float softPenaltyDamageMultiplier = 1f;
        public float softPenaltyDuration;
        public bool craftedAnything;
        public bool securedDrop;

        public NightPrepSnapshot Clone()
        {
            return new NightPrepSnapshot
            {
                ammoReserveGrant = ammoReserveGrant,
                healGrant = healGrant,
                enemySpeedMultiplier = enemySpeedMultiplier,
                enemyHealthMultiplier = enemyHealthMultiplier,
                enemyDamageMultiplier = enemyDamageMultiplier,
                softPenaltyDamageMultiplier = softPenaltyDamageMultiplier,
                softPenaltyDuration = softPenaltyDuration,
                craftedAnything = craftedAnything,
                securedDrop = securedDrop
            };
        }

        public static NightPrepSnapshot Default()
        {
            return new NightPrepSnapshot
            {
                ammoReserveGrant = 0,
                healGrant = 0f,
                enemySpeedMultiplier = 1f,
                enemyHealthMultiplier = 1f,
                enemyDamageMultiplier = 1f,
                softPenaltyDamageMultiplier = 1f,
                softPenaltyDuration = 0f,
                craftedAnything = false,
                securedDrop = false
            };
        }
    }

    public class CraftingSystem : MonoBehaviour
    {
        public static CraftingSystem Instance { get; private set; }

        [Header("Crafting Tuning")]
        [SerializeField] private int ammoCacheReserveGrant = 35;
        [SerializeField] private float fieldMedHealGrant = 25f;
        [SerializeField] private float shockBeaconEnemySpeedMultiplier = 0.92f;
        [SerializeField] private float weakpointEnemyHealthMultiplier = 0.94f;
        [SerializeField] private float weakpointEnemyDamageMultiplier = 0.94f;

        [Header("Soft Penalty")]
        [SerializeField] private float noPrepDamagePenaltyMultiplier = 1.08f;
        [SerializeField] private float noPrepPenaltyDuration = 60f;

        [Header("UI")]
        [SerializeField] private KeyCode toggleKey = KeyCode.C;

        private readonly Dictionary<CraftingRecipeId, CraftingRecipeDefinition> recipes = new Dictionary<CraftingRecipeId, CraftingRecipeDefinition>();
        private readonly Dictionary<CraftingRecipeId, int> craftedToday = new Dictionary<CraftingRecipeId, int>();
        private readonly Dictionary<ResourceType, float> hintCooldowns = new Dictionary<ResourceType, float>();

        private NightPrepSnapshot pendingNightPrep = NightPrepSnapshot.Default();
        private NightPrepSnapshot activeNightPrep = NightPrepSnapshot.Default();

        private bool craftedAnythingToday;
        private bool contestedDropSecuredToday;
        private bool isPanelVisible;
        private bool finalizedForCurrentNight;
        private bool nightBenefitsApplied;
        private float nightStartedAt = -999f;
        private bool craftingEnabled;

        private Canvas craftingCanvas;
        private GameObject panelRoot;
        private Text panelText;

        private readonly CraftingRecipeId[] recipeOrder =
        {
            CraftingRecipeId.AmmoCache,
            CraftingRecipeId.FieldMed,
            CraftingRecipeId.ShockBeacon
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            craftingEnabled = GameManager.Instance != null && GameManager.Instance.CraftingEnabled;
            if (!craftingEnabled)
            {
                Instance = null;
                Destroy(gameObject);
                return;
            }

            BuildRecipes();
            ResetDayCraftingState();
            CreateUI();
        }

        private void Start()
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (!craftingEnabled)
            {
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void Update()
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.DayPhase)
            {
                if (isPanelVisible)
                {
                    SetPanelVisible(false);
                }
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                SetPanelVisible(!isPanelVisible);
            }

            if (!isPanelVisible)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) Craft(CraftingRecipeId.AmmoCache);
            if (Input.GetKeyDown(KeyCode.Alpha2)) Craft(CraftingRecipeId.FieldMed);
            if (Input.GetKeyDown(KeyCode.Alpha3)) Craft(CraftingRecipeId.ShockBeacon);

            RefreshPanelText();
        }

        public bool CanCraft(CraftingRecipeId recipeId)
        {
            if (!craftingEnabled)
            {
                return false;
            }

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.DayPhase)
            {
                return false;
            }

            if (!recipes.TryGetValue(recipeId, out var recipe))
            {
                return false;
            }

            if (GetCraftCount(recipeId) >= recipe.perDayCap)
            {
                return false;
            }

            if (ResourceManager.Instance == null)
            {
                return false;
            }

            return ResourceManager.Instance.HasResources(recipe.costs);
        }

        public bool Craft(CraftingRecipeId recipeId)
        {
            if (!craftingEnabled)
            {
                return false;
            }

            if (!recipes.TryGetValue(recipeId, out var recipe))
            {
                return false;
            }

            if (!CanCraft(recipeId))
            {
                ShowCraftingStatus($"Cannot prepare {recipe.displayName}.");
                RefreshPanelText();
                return false;
            }

            if (ResourceManager.Instance == null || !ResourceManager.Instance.SpendResources(recipe.costs))
            {
                ShowCraftingStatus($"Missing requirements for {recipe.displayName}.");
                RefreshPanelText();
                return false;
            }

            craftedToday[recipeId] = GetCraftCount(recipeId) + 1;
            craftedAnythingToday = true;

            ApplyRecipeToPendingPrep(recipeId);

            ShowCraftingStatus($"Prepared {recipe.displayName}. {recipe.nightEffectDescription}");
            RefreshPanelText();
            return true;
        }

        public Dictionary<CraftingRecipeId, int> GetRecipeState()
        {
            if (!craftingEnabled)
            {
                return new Dictionary<CraftingRecipeId, int>();
            }

            return new Dictionary<CraftingRecipeId, int>(craftedToday);
        }

        public void FinalizeDayPrep()
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (finalizedForCurrentNight)
            {
                return;
            }

            activeNightPrep = pendingNightPrep.Clone();
            activeNightPrep.craftedAnything = craftedAnythingToday;
            activeNightPrep.securedDrop = contestedDropSecuredToday;

            if (!craftedAnythingToday && !contestedDropSecuredToday)
            {
                activeNightPrep.softPenaltyDamageMultiplier = noPrepDamagePenaltyMultiplier;
                activeNightPrep.softPenaltyDuration = noPrepPenaltyDuration;
            }
            else
            {
                activeNightPrep.softPenaltyDamageMultiplier = 1f;
                activeNightPrep.softPenaltyDuration = 0f;
            }

            finalizedForCurrentNight = true;
            nightBenefitsApplied = false;

            ShowDuskSummary(activeNightPrep);
        }

        public NightPrepSnapshot GetNightPrepSnapshot()
        {
            return activeNightPrep.Clone();
        }

        public void NotifyResourceCollected(ResourceType type, int amount, Vector3 worldPos)
        {
            if (!craftingEnabled)
            {
                return;
            }

            // Legacy crafting collectible hints are intentionally hidden from on-screen UI.
            return;
        }

        public void NotifyContestedDropSecured()
        {
            if (!craftingEnabled)
            {
                return;
            }

            contestedDropSecuredToday = true;
        }

        public float GetNightEnemySpeedMultiplier()
        {
            if (!craftingEnabled)
            {
                return 1f;
            }

            return Mathf.Clamp(activeNightPrep.enemySpeedMultiplier, 0.5f, 1f);
        }

        public float GetNightEnemyHealthMultiplier()
        {
            if (!craftingEnabled)
            {
                return 1f;
            }

            return Mathf.Clamp(activeNightPrep.enemyHealthMultiplier, 0.5f, 1.25f);
        }

        public float GetNightEnemyDamageMultiplier()
        {
            if (!craftingEnabled)
            {
                return 1f;
            }

            float penalty = GetCurrentPenaltyDamageMultiplier();
            return Mathf.Clamp(activeNightPrep.enemyDamageMultiplier * penalty, 0.5f, 1.5f);
        }

        public float GetCurrentPenaltyDamageMultiplier()
        {
            if (!craftingEnabled)
            {
                return 1f;
            }

            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.NightPhase)
            {
                return 1f;
            }

            if (activeNightPrep.softPenaltyDamageMultiplier <= 1f || activeNightPrep.softPenaltyDuration <= 0f)
            {
                return 1f;
            }

            if (nightStartedAt < 0f)
            {
                return activeNightPrep.softPenaltyDamageMultiplier;
            }

            float elapsed = Time.time - nightStartedAt;
            return elapsed <= activeNightPrep.softPenaltyDuration
                ? activeNightPrep.softPenaltyDamageMultiplier
                : 1f;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (state == GameState.DayPhase)
            {
                ResetDayCraftingState();
                RefreshPanelText();
                return;
            }

            if (state == GameState.NightPhase)
            {
                if (!finalizedForCurrentNight)
                {
                    FinalizeDayPrep();
                }

                nightStartedAt = Time.time;
                ApplyNightStartBenefitsOnce();
                SetPanelVisible(false);
                return;
            }

            if (state == GameState.DawnPhase || state == GameState.GameOver || state == GameState.Victory || state == GameState.MainMenu)
            {
                SetPanelVisible(false);
            }
        }

        private void ApplyNightStartBenefitsOnce()
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (nightBenefitsApplied)
            {
                return;
            }

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                if (activeNightPrep.ammoReserveGrant > 0)
                {
                    var shooting = player.GetComponent<PlayerShooting>();
                    shooting?.AddAmmo(activeNightPrep.ammoReserveGrant);
                }

                if (activeNightPrep.healGrant > 0f)
                {
                    var health = player.GetComponent<PlayerHealth>();
                    health?.Heal(activeNightPrep.healGrant);
                }
            }

            nightBenefitsApplied = true;
        }

        private void BuildRecipes()
        {
            if (!craftingEnabled)
            {
                return;
            }

            recipes.Clear();
            recipes[CraftingRecipeId.AmmoCache] = new CraftingRecipeDefinition(
                CraftingRecipeId.AmmoCache,
                "Ammo Cache",
                $"+{ammoCacheReserveGrant} reserve ammo at night start.",
                2,
                new List<ResourceAmount>
                {
                    new ResourceAmount(ResourceType.Scrap, 4),
                    new ResourceAmount(ResourceType.Wood, 2)
                });

            recipes[CraftingRecipeId.FieldMed] = new CraftingRecipeDefinition(
                CraftingRecipeId.FieldMed,
                "Field Med",
                $"+{Mathf.RoundToInt(fieldMedHealGrant)} HP at night start.",
                2,
                new List<ResourceAmount>
                {
                    new ResourceAmount(ResourceType.Chemicals, 2),
                    new ResourceAmount(ResourceType.Wood, 1)
                });

            recipes[CraftingRecipeId.ShockBeacon] = new CraftingRecipeDefinition(
                CraftingRecipeId.ShockBeacon,
                "Shock Beacon",
                $"Night enemies -{Mathf.RoundToInt((1f - shockBeaconEnemySpeedMultiplier) * 100f)}% speed.",
                1,
                new List<ResourceAmount>
                {
                    new ResourceAmount(ResourceType.Electronics, 1),
                    new ResourceAmount(ResourceType.BlueprintToken, 1)
                });

            foreach (var id in recipeOrder)
            {
                craftedToday[id] = 0;
            }
        }

        private void ResetDayCraftingState()
        {
            if (!craftingEnabled)
            {
                return;
            }

            foreach (var id in recipeOrder)
            {
                craftedToday[id] = 0;
            }

            pendingNightPrep = NightPrepSnapshot.Default();
            activeNightPrep = NightPrepSnapshot.Default();

            craftedAnythingToday = false;
            contestedDropSecuredToday = false;
            finalizedForCurrentNight = false;
            nightBenefitsApplied = false;
            nightStartedAt = -999f;
        }

        private void ApplyRecipeToPendingPrep(CraftingRecipeId recipeId)
        {
            if (!craftingEnabled)
            {
                return;
            }

            switch (recipeId)
            {
                case CraftingRecipeId.AmmoCache:
                    pendingNightPrep.ammoReserveGrant += ammoCacheReserveGrant;
                    break;
                case CraftingRecipeId.FieldMed:
                    pendingNightPrep.healGrant += fieldMedHealGrant;
                    break;
                case CraftingRecipeId.ShockBeacon:
                    pendingNightPrep.enemySpeedMultiplier *= shockBeaconEnemySpeedMultiplier;
                    break;
                case CraftingRecipeId.WeakpointIntel:
                    pendingNightPrep.enemyHealthMultiplier *= weakpointEnemyHealthMultiplier;
                    pendingNightPrep.enemyDamageMultiplier *= weakpointEnemyDamageMultiplier;
                    break;
                case CraftingRecipeId.TacticalPrep:
                    pendingNightPrep.enemySpeedMultiplier *= shockBeaconEnemySpeedMultiplier;
                    pendingNightPrep.enemyHealthMultiplier *= weakpointEnemyHealthMultiplier;
                    pendingNightPrep.enemyDamageMultiplier *= weakpointEnemyDamageMultiplier;
                    break;
            }
        }

        private int GetCraftCount(CraftingRecipeId recipeId)
        {
            if (!craftingEnabled)
            {
                return 0;
            }

            return craftedToday.TryGetValue(recipeId, out var count) ? count : 0;
        }

        private bool IsHintResource(ResourceType type)
        {
            if (!craftingEnabled)
            {
                return false;
            }

            return type == ResourceType.Scrap ||
                   type == ResourceType.Wood ||
                   type == ResourceType.Chemicals ||
                   type == ResourceType.Electronics ||
                   type == ResourceType.Salvage ||
                   type == ResourceType.TechParts;
        }

        private string GetResourceHint(ResourceType type)
        {
            if (!craftingEnabled)
            {
                return string.Empty;
            }

            return type switch
            {
                ResourceType.Scrap => "Ammo Cache / Weakpoint Intel",
                ResourceType.Wood => "Ammo Cache / Field Med",
                ResourceType.Chemicals => "Field Med / Shock Beacon",
                ResourceType.Electronics => "Weakpoint Intel / Shock Beacon",
                ResourceType.Salvage => "Ammo Cache / Weakpoint Intel",
                ResourceType.TechParts => "Weakpoint Intel / Shock Beacon",
                _ => "Prep"
            };
        }

        private void ShowDuskSummary(NightPrepSnapshot snapshot)
        {
            if (!craftingEnabled)
            {
                return;
            }

            var summary = BuildDuskSummary(snapshot);
            ShowCraftingStatus(summary);

            if (RadioTransmissions.Instance != null)
            {
                RadioTransmissions.Instance.ShowMessage(summary, 5f);
            }
        }

        private string BuildDuskSummary(NightPrepSnapshot snapshot)
        {
            if (!craftingEnabled)
            {
                return string.Empty;
            }

            var pieces = new List<string>();

            if (snapshot.ammoReserveGrant > 0)
            {
                pieces.Add($"+{snapshot.ammoReserveGrant} Ammo");
            }

            if (snapshot.healGrant > 0f)
            {
                pieces.Add($"+{Mathf.RoundToInt(snapshot.healGrant)} HP");
            }

            if (snapshot.enemySpeedMultiplier < 1f)
            {
                pieces.Add($"Enemy SPD -{Mathf.RoundToInt((1f - snapshot.enemySpeedMultiplier) * 100f)}%");
            }

            if (snapshot.enemyHealthMultiplier < 1f)
            {
                pieces.Add($"Enemy HP -{Mathf.RoundToInt((1f - snapshot.enemyHealthMultiplier) * 100f)}%");
            }

            if (snapshot.enemyDamageMultiplier < 1f)
            {
                pieces.Add($"Enemy DMG -{Mathf.RoundToInt((1f - snapshot.enemyDamageMultiplier) * 100f)}%");
            }

            if (snapshot.softPenaltyDamageMultiplier > 1f && snapshot.softPenaltyDuration > 0f)
            {
                pieces.Add($"Penalty: +{Mathf.RoundToInt((snapshot.softPenaltyDamageMultiplier - 1f) * 100f)}% enemy DMG ({Mathf.RoundToInt(snapshot.softPenaltyDuration)}s)");
            }

            if (pieces.Count == 0)
            {
                return snapshot.securedDrop
                    ? "Dusk Report: No prep completed, but drop secured."
                    : "Dusk Report: No prep completed.";
            }

            return "Dusk Report: " + string.Join(" | ", pieces);
        }

        private void ShowCraftingStatus(string text)
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (FloatingTextManager.Instance == null)
            {
                return;
            }

            var player = GameObject.FindWithTag("Player");
            Vector3 pos = player != null ? player.transform.position + Vector3.up * 1.8f : Vector3.zero;
            FloatingTextManager.Instance.SpawnText(text, pos, new Color(1f, 0.9f, 0.4f), 2.2f, 20);
        }

        private void CreateUI()
        {
            if (!craftingEnabled)
            {
                return;
            }

            var canvasObj = new GameObject("PrepUICanvas");
            canvasObj.transform.SetParent(transform, false);
            craftingCanvas = canvasObj.AddComponent<Canvas>();
            craftingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            craftingCanvas.sortingOrder = 120;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            panelRoot = new GameObject("PrepPanel");
            panelRoot.transform.SetParent(craftingCanvas.transform, false);
            var panelRect = panelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(18f, 20f);
            panelRect.sizeDelta = new Vector2(610f, 300f);

            var bg = panelRoot.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.12f, 0.86f);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }

            var textObj = new GameObject("PanelText");
            textObj.transform.SetParent(panelRoot.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(12f, 12f);
            textRect.offsetMax = new Vector2(-12f, -12f);

            panelText = textObj.AddComponent<Text>();
            panelText.font = font;
            panelText.fontSize = 18;
            panelText.color = Color.white;
            panelText.alignment = TextAnchor.UpperLeft;
            panelText.supportRichText = true;
            panelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            panelText.verticalOverflow = VerticalWrapMode.Overflow;

            SetPanelVisible(false);
            RefreshPanelText();
        }

        private void SetPanelVisible(bool visible)
        {
            if (!craftingEnabled)
            {
                return;
            }

            isPanelVisible = visible;
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void RefreshPanelText()
        {
            if (!craftingEnabled)
            {
                return;
            }

            if (panelText == null)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("DAY PREP [C]");
            sb.AppendLine("Prepare now to affect tonight.");
            sb.AppendLine();

            foreach (var id in recipeOrder)
            {
                if (!recipes.TryGetValue(id, out var recipe))
                {
                    continue;
                }

                int count = GetCraftCount(id);
                bool canCraft = CanCraft(id);
                string color = canCraft ? "#90ff90" : "#ff9a9a";
                string line = $"[{GetRecipeKeyLabel(id)}] {recipe.displayName} ({count}/{recipe.perDayCap})";
                sb.AppendLine($"<color={color}>{line}</color>");
                sb.AppendLine($"  Cost: {FormatCosts(recipe.costs)}");
                sb.AppendLine($"  Effect: {recipe.nightEffectDescription}");
                sb.AppendLine();
            }

            sb.AppendLine($"Prepared tonight: Ammo +{pendingNightPrep.ammoReserveGrant}, HP +{Mathf.RoundToInt(pendingNightPrep.healGrant)}");
            sb.AppendLine($"Enemy multipliers: SPD x{pendingNightPrep.enemySpeedMultiplier:0.00}, HP x{pendingNightPrep.enemyHealthMultiplier:0.00}, DMG x{pendingNightPrep.enemyDamageMultiplier:0.00}");

            if (contestedDropSecuredToday)
            {
                sb.AppendLine("Contested drop secured today: YES");
            }

            panelText.text = sb.ToString();
        }

        private string GetRecipeKeyLabel(CraftingRecipeId id)
        {
            if (!craftingEnabled)
            {
                return string.Empty;
            }

            return id switch
            {
                CraftingRecipeId.AmmoCache => "1",
                CraftingRecipeId.FieldMed => "2",
                CraftingRecipeId.ShockBeacon => "3",
                _ => "?"
            };
        }

        private static string FormatCosts(List<ResourceAmount> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return "None";
            }

            var parts = new List<string>(costs.Count);
            foreach (var c in costs)
            {
                parts.Add($"{c.amount} {c.type}");
            }

            return string.Join(", ", parts);
        }
    }
}
