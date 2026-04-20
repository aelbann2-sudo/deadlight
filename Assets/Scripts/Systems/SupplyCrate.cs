using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Player;
using Deadlight.Data;
using Deadlight.Visuals;

namespace Deadlight.Systems
{
    public enum CrateTier { Common, Rare, Legendary }
    public enum CrateContents { Ammo, Health, Points, Powerup, Armor, Grenade, Molotov, Shotgun, Medkit }

    public class SupplyCrate : MonoBehaviour
    {
        /// <summary>Fired once when a crate is fully looted or a contested drop is secured (before destroy).</summary>
        public static event System.Action<SupplyCrate> OnCrateSuccessfullyLooted;

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

        public bool IsContestedSupplyDrop => isContestedDrop;
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
                promptText.text = "Hold F to Secure Drop";
                // Keep contested-drop prompt white + outline for readability; urgency is
                // conveyed by the countdown in the prompt text and the pulsing glow.
                promptText.color = Color.white;
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
            var throwable = playerObj?.GetComponent<ThrowableSystem>();
            var medkits = playerObj?.GetComponent<PlayerMedkitSystem>();

            float healthPct = health != null ? health.HealthPercentage : 1f;
            int reserveAmmo = shooting != null ? shooting.ReserveAmmo : 0;
            float ammoPressure = GetAmmoPressure01(reserveAmmo);
            bool advancedDropsUnlocked = (GameManager.Instance?.CurrentLevel ?? 1) >= 4;
            bool hasShotgun = shooting != null && shooting.HasWeaponType(WeaponType.Shotgun);
            float grenadePct = throwable != null && throwable.MaxGrenades > 0
                ? (float)throwable.GrenadeCount / throwable.MaxGrenades
                : 1f;
            float molotovPct = throwable != null && throwable.MaxMolotovs > 0
                ? (float)throwable.MolotovCount / throwable.MaxMolotovs
                : 1f;
            float medkitPct = medkits != null && medkits.MaxMedkits > 0
                ? (float)medkits.MedkitCount / medkits.MaxMedkits
                : 1f;

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
            if (reserveAmmo < 50) { contents = CrateContents.Ammo; return; }
            if (healthPct < 0.5f && Random.value < 0.6f) { contents = CrateContents.Health; return; }
            if (reserveAmmo < 110 && Random.value < 0.55f) { contents = CrateContents.Ammo; return; }
            if (advancedDropsUnlocked && !hasShotgun && tier >= CrateTier.Rare && Random.value < 0.08f)
            {
                contents = CrateContents.Shotgun;
                return;
            }
            if (advancedDropsUnlocked && throwable != null && grenadePct < 0.25f && Random.value < 0.45f)
            {
                contents = CrateContents.Grenade;
                return;
            }
            if (advancedDropsUnlocked && throwable != null && molotovPct < 0.25f && Random.value < 0.40f)
            {
                contents = CrateContents.Molotov;
                return;
            }
            if (advancedDropsUnlocked && medkits != null && medkitPct < 0.3f && Random.value < 0.45f)
            {
                contents = CrateContents.Medkit;
                return;
            }

            float ammoWeight = Mathf.Lerp(0.35f, 0.06f, ammoPressure);
            float healthWeight = healthPct < 0.75f ? 0.30f : 0.24f;
            float grenadeWeight = 0f;
            float molotovWeight = 0f;
            float shotgunWeight = 0f;
            float medkitWeight = 0f;

            if (advancedDropsUnlocked)
            {
                if (throwable != null)
                {
                    grenadeWeight = Mathf.Lerp(0.09f, 0.03f, grenadePct);
                    molotovWeight = Mathf.Lerp(0.08f, 0.02f, molotovPct);
                }

                if (!hasShotgun && tier >= CrateTier.Rare)
                {
                    shotgunWeight = 0.04f;
                }

                if (medkits != null)
                {
                    medkitWeight = Mathf.Lerp(0.10f, 0.02f, medkitPct);
                }
            }

            float roll = Random.value;
            float cursor = ammoWeight;
            if (roll < cursor) { contents = CrateContents.Ammo; return; }

            cursor += healthWeight;
            if (roll < cursor) { contents = CrateContents.Health; return; }

            cursor += grenadeWeight;
            if (roll < cursor) { contents = CrateContents.Grenade; return; }

            cursor += molotovWeight;
            if (roll < cursor) { contents = CrateContents.Molotov; return; }

            cursor += shotgunWeight;
            if (roll < cursor) { contents = CrateContents.Shotgun; return; }

            cursor += medkitWeight;
            if (roll < cursor) { contents = CrateContents.Medkit; return; }

            contents = CrateContents.Points;
        }

        private static float GetAmmoPressure01(int reserveAmmo)
        {
            const float lowReserve = 120f;
            const float highReserve = 320f;
            return Mathf.InverseLerp(lowReserve, highReserve, reserveAmmo);
        }

        private static int ScaleAmmoRewardForReserve(int baseAmmo, PlayerShooting shooting)
        {
            int amount = Mathf.Max(1, baseAmmo);
            if (shooting == null)
            {
                return amount;
            }

            int reserve = shooting.ReserveAmmo;
            float multiplier = 1f;

            if (reserve >= 360)
            {
                multiplier = 0.20f;
            }
            else if (reserve >= 280)
            {
                multiplier = 0.35f;
            }
            else if (reserve >= 200)
            {
                multiplier = 0.55f;
            }
            else if (reserve >= 140)
            {
                multiplier = 0.75f;
            }

            return Mathf.Max(1, Mathf.RoundToInt(amount * multiplier));
        }

        private int GetUtilityDropAmount()
        {
            return tier == CrateTier.Legendary ? 2 : 1;
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
                case CrateContents.Grenade:
                    iconColor = new Color(0.55f, 0.85f, 0.45f);
                    iconSprite = CreateGrenadeIcon();
                    break;
                case CrateContents.Molotov:
                    iconColor = new Color(1f, 0.55f, 0.2f);
                    iconSprite = CreateMolotovIcon();
                    break;
                case CrateContents.Shotgun:
                    iconColor = new Color(0.95f, 0.95f, 1f);
                    iconSprite = CreateShotgunIcon();
                    break;
                case CrateContents.Medkit:
                    iconColor = new Color(1f, 0.35f, 0.35f);
                    iconSprite = CreateMedkitIcon();
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
                    promptText.text = $"Hold F to Secure ({remaining}s)";
                }
                promptText.gameObject.SetActive(inRange && !isLooting);
            }

            // Show the empty bar as soon as the player is in range so the hold requirement is obvious.
            if (progressBarRoot != null && !isLooted)
            {
                progressBarRoot.SetActive(inRange);
            }

            if (inRange && Input.GetKey(KeyCode.F) && !isLooted)
            {
                isLooting = true;
                interactProgress += Time.deltaTime;

                if (progressFill != null)
                    progressFill.fillAmount = interactProgress / interactionTime;

                if (interactProgress >= interactionTime)
                    CompleteLoot();
            }
            else
            {
                if (isLooting)
                {
                    isLooting = false;
                    interactProgress = 0f;
                    if (progressFill != null)
                        progressFill.fillAmount = 0f;
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
                        var playerShooting = player?.GetComponent<PlayerShooting>();
                        int scaledAmmo = ScaleAmmoRewardForReserve(ammo, playerShooting);
                        int grantedAmmo = playerShooting?.AddAmmo(scaledAmmo) ?? 0;
                        if (grantedAmmo > 0)
                        {
                            gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Ammo, grantedAmmo);
                        }
                        reward = grantedAmmo > 0 ? $"+{grantedAmmo} Ammo" : "Ammo Full";
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
                    case CrateContents.Grenade:
                        var grenadeSystem = player?.GetComponent<ThrowableSystem>();
                        if (grenadeSystem != null)
                        {
                            int beforeGrenades = grenadeSystem.GrenadeCount;
                            int grenadeGrant = GetUtilityDropAmount();
                            grenadeSystem.AddGrenades(grenadeGrant);
                            int gainedGrenades = grenadeSystem.GrenadeCount - beforeGrenades;
                            if (gainedGrenades > 0)
                            {
                                gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Grenade, gainedGrenades);
                                reward = $"+{gainedGrenades} Grenade{(gainedGrenades > 1 ? "s" : "")}";
                            }
                            else
                            {
                                reward = "Grenades Full";
                            }
                        }
                        else
                        {
                            reward = "Utility Unavailable";
                        }
                        break;
                    case CrateContents.Molotov:
                        var molotovSystem = player?.GetComponent<ThrowableSystem>();
                        if (molotovSystem != null)
                        {
                            int beforeMolotovs = molotovSystem.MolotovCount;
                            int molotovGrant = GetUtilityDropAmount();
                            molotovSystem.AddMolotovs(molotovGrant);
                            int gainedMolotovs = molotovSystem.MolotovCount - beforeMolotovs;
                            if (gainedMolotovs > 0)
                            {
                                gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Molotov, gainedMolotovs);
                                reward = $"+{gainedMolotovs} Molotov{(gainedMolotovs > 1 ? "s" : "")}";
                            }
                            else
                            {
                                reward = "Molotovs Full";
                            }
                        }
                        else
                        {
                            reward = "Utility Unavailable";
                        }
                        break;
                    case CrateContents.Shotgun:
                        var shotgunShooting = player?.GetComponent<PlayerShooting>();
                        if (shotgunShooting == null)
                        {
                            reward = "No Weapon System";
                            break;
                        }

                        if (!shotgunShooting.HasWeaponType(WeaponType.Shotgun))
                        {
                            bool equipped = shotgunShooting.TryAddWeaponToLoadout(WeaponData.CreateShotgun(), false);
                            if (equipped)
                            {
                                gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Shotgun, 1);
                                reward = "Shotgun Acquired";
                            }
                            else
                            {
                                int fallbackAmmo = ScaleAmmoRewardForReserve(40, shotgunShooting);
                                int gainedFallbackAmmo = shotgunShooting.AddAmmo(fallbackAmmo);
                                if (gainedFallbackAmmo > 0)
                                {
                                    gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Ammo, gainedFallbackAmmo);
                                    reward = $"+{gainedFallbackAmmo} Ammo";
                                }
                                else
                                {
                                    reward = "Loadout Full";
                                }
                            }
                        }
                        else
                        {
                            int shotgunAmmo = ScaleAmmoRewardForReserve(35, shotgunShooting);
                            int gainedShotgunAmmo = shotgunShooting.AddAmmo(shotgunAmmo);
                            if (gainedShotgunAmmo > 0)
                            {
                                gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Ammo, gainedShotgunAmmo);
                                reward = $"+{gainedShotgunAmmo} Ammo";
                            }
                            else
                            {
                                reward = "Ammo Full";
                            }
                        }
                        break;
                    case CrateContents.Medkit:
                        var medkitSystem = player?.GetComponent<PlayerMedkitSystem>();
                        if (medkitSystem != null)
                        {
                            int beforeMedkits = medkitSystem.MedkitCount;
                            int medkitGrant = GetUtilityDropAmount();
                            medkitSystem.AddMedkits(medkitGrant);
                            int gainedMedkits = medkitSystem.MedkitCount - beforeMedkits;
                            if (gainedMedkits > 0)
                            {
                                gameplayHelp?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Medkit, gainedMedkits);
                                reward = $"+{gainedMedkits} Medkit{(gainedMedkits > 1 ? "s" : "")}";
                            }
                            else
                            {
                                reward = "Medkits Full";
                            }
                        }
                        else
                        {
                            reward = "Medkit System Missing";
                        }
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

            if (AudioManager.Instance != null)
            {
                try
                {
                    var clip = Audio.ProceduralAudioGenerator.GeneratePickup();
                    if (clip != null)
                    {
                        AudioManager.Instance.PlaySFXAtPosition(
                            clip,
                            transform.position,
                            isContestedDrop ? 0.72f : 0.58f,
                            1.02f,
                            0.03f);
                    }
                }
                catch
                {
                    AudioManager.Instance.PlaySFX("pickup", isContestedDrop ? 0.72f : 0.58f);
                }
            }
            else
            {
                try
                {
                    var clip = Audio.ProceduralAudioGenerator.GeneratePickup();
                    if (clip != null)
                    {
                        AudioSource.PlayClipAtPoint(clip, transform.position, isContestedDrop ? 0.72f : 0.58f);
                    }
                }
                catch { }
            }

            sr.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            OnCrateSuccessfullyLooted?.Invoke(this);
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
                    bonusPoints = 220;
                    ammo = 90;
                    heal = 45f;
                    break;
                case CrateTier.Rare:
                    bonusPoints = 140;
                    ammo = 60;
                    heal = 30f;
                    break;
                default:
                    bonusPoints = 90;
                    ammo = 35;
                    heal = 18f;
                    break;
            }

            PointsSystem.Instance?.AddPoints(bonusPoints, "Contested Drop");

            var playerObj = GameObject.Find("Player");
            var shooting = playerObj != null ? playerObj.GetComponent<PlayerShooting>() : null;
            var health = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;

            int scaledAmmo = ScaleAmmoRewardForReserve(ammo, shooting);
            int grantedAmmo = shooting?.AddAmmo(scaledAmmo) ?? 0;
            health?.Heal(heal);
            int grantedGrenades = 0;
            int grantedMolotovs = 0;
            int grantedMedkits = 0;

            if ((GameManager.Instance?.CurrentLevel ?? 1) >= 4)
            {
                var throwable = playerObj != null ? playerObj.GetComponent<ThrowableSystem>() : null;
                var medkits = playerObj != null ? playerObj.GetComponent<PlayerMedkitSystem>() : null;
                if (throwable != null)
                {
                    bool grantGrenade = Random.value < 0.5f;
                    int utilityGrant = tier == CrateTier.Legendary ? 2 : 1;
                    if (grantGrenade)
                    {
                        int before = throwable.GrenadeCount;
                        throwable.AddGrenades(utilityGrant);
                        grantedGrenades = throwable.GrenadeCount - before;
                    }
                    else
                    {
                        int before = throwable.MolotovCount;
                        throwable.AddMolotovs(utilityGrant);
                        grantedMolotovs = throwable.MolotovCount - before;
                    }
                }

                if (medkits != null && Random.value < 0.45f)
                {
                    int medkitGrant = tier == CrateTier.Legendary ? 2 : 1;
                    int beforeMedkits = medkits.MedkitCount;
                    medkits.AddMedkits(medkitGrant);
                    grantedMedkits = medkits.MedkitCount - beforeMedkits;
                }
            }

            var help = Deadlight.UI.GameplayHelpSystem.Instance;
            help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Points, bonusPoints);
            if (grantedAmmo > 0)
            {
                help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Ammo, grantedAmmo);
            }
            if (grantedGrenades > 0)
            {
                help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Grenade, grantedGrenades);
            }
            if (grantedMolotovs > 0)
            {
                help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Molotov, grantedMolotovs);
            }
            if (grantedMedkits > 0)
            {
                help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Medkit, grantedMedkits);
            }
            help?.ShowItem(Deadlight.UI.GameplayGuideContent.ItemIds.Health, Mathf.RoundToInt(heal));

            var directSummary = new StringBuilder("DROP SECURED: ");
            directSummary.Append($"+{bonusPoints} Points, +{grantedAmmo} Ammo, +{Mathf.RoundToInt(heal)} HP");
            if (grantedGrenades > 0 || grantedMolotovs > 0)
            {
                if (grantedGrenades > 0)
                {
                    directSummary.Append($", +{grantedGrenades} Grenade{(grantedGrenades == 1 ? string.Empty : "s")}");
                }

                if (grantedMolotovs > 0)
                {
                    directSummary.Append($", +{grantedMolotovs} Molotov{(grantedMolotovs == 1 ? string.Empty : "s")}");
                }
            }
            if (grantedMedkits > 0)
            {
                directSummary.Append($", +{grantedMedkits} Medkit{(grantedMedkits == 1 ? string.Empty : "s")}");
            }
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

            AudioManager.Instance?.PlaySFX("alarm_siren", 0.14f);
            AudioManager.Instance?.SignalCombatPeak(0.08f, 1.2f);
            GameEffects.Instance?.FlashScreen(new Color(0.45f, 0.08f, 0.05f, 0.16f), 0.2f);

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
            pRect.sizeDelta = new Vector2(220, 28);
            promptText = promptObj.AddComponent<Text>();
            string tierLabel = tier == CrateTier.Legendary ? "Hold F — LEGENDARY" :
                               tier == CrateTier.Rare ? "Hold F — Rare Crate" : "Hold F to Loot";
            promptText.text = tierLabel;
            promptText.font = font;
            promptText.fontSize = 18;
            promptText.fontStyle = FontStyle.Bold;
            promptText.alignment = TextAnchor.MiddleCenter;
            // White text keeps the prompt readable on every map background. The crate sprite,
            // glow, and progress bar fill continue to convey tier visually.
            promptText.color = Color.white;
            var promptOutline = promptObj.AddComponent<UnityEngine.UI.Outline>();
            promptOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            promptOutline.effectDistance = new Vector2(1.2f, -1.2f);
            var promptShadow = promptObj.AddComponent<UnityEngine.UI.Shadow>();
            promptShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            promptShadow.effectDistance = new Vector2(0f, -1.8f);
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

        private static Sprite CreateGrenadeIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            Vector2 center = new Vector2(8f, 9f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    if (Vector2.Distance(new Vector2(x, y), center) <= 4.5f)
                        px[y * s + x] = Color.white;

            for (int y = 2; y < 5; y++)
                for (int x = 7; x < 10; x++)
                    px[y * s + x] = Color.white;

            for (int x = 9; x < 12; x++)
                px[3 * s + x] = Color.white;

            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateMolotovIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            for (int y = 5; y < 13; y++)
                for (int x = 6; x < 10; x++)
                    px[y * s + x] = Color.white;

            for (int y = 3; y < 6; y++)
                for (int x = 7; x < 9; x++)
                    px[y * s + x] = Color.white;

            for (int y = 1; y < 4; y++)
                for (int x = 10; x < 12; x++)
                    px[y * s + x] = Color.white;

            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }

        private static Sprite CreateShotgunIcon()
        {
            var icon = ProceduralSpriteGenerator.CreateWeaponIcon(WeaponType.Shotgun);
            return icon != null ? icon : CreateBulletIcon();
        }

        private static Sprite CreateMedkitIcon()
        {
            int s = 16;
            var tex = new Texture2D(s, s);
            var px = new Color[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            for (int y = 3; y < 13; y++)
            {
                for (int x = 3; x < 13; x++)
                {
                    bool border = x == 3 || x == 12 || y == 3 || y == 12;
                    px[y * s + x] = border ? new Color(0.85f, 0.2f, 0.2f) : Color.white;
                }
            }

            for (int y = 6; y < 10; y++)
                for (int x = 7; x < 9; x++)
                    px[y * s + x] = new Color(0.85f, 0.2f, 0.2f);

            for (int y = 7; y < 9; y++)
                for (int x = 6; x < 10; x++)
                    px[y * s + x] = new Color(0.85f, 0.2f, 0.2f);

            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        }
    }
}
