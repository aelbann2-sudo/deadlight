using UnityEngine;

namespace Deadlight.Player
{
    public class PlayerUpgrades : MonoBehaviour
    {
        public static PlayerUpgrades Instance { get; private set; }

        private int damageTier;
        private int fireRateTier;
        private int magazineTier;
        private int healthTier;
        private int sprintTier;

        public const int MaxDamageTier = 3;
        public const int MaxFireRateTier = 3;
        public const int MaxMagazineTier = 3;
        public const int MaxHealthTier = 3;
        public const int MaxSprintTier = 2;

        private static readonly float[] DamageMultipliers = { 1f, 1.15f, 1.30f, 1.50f };
        private static readonly float[] FireRateMultipliers = { 1f, 0.90f, 0.80f, 0.65f };
        private static readonly int[] MagazineBonuses = { 0, 5, 10, 15 };
        private static readonly float[] HealthBonuses = { 0f, 20f, 40f, 70f };
        private static readonly float[] SprintBonuses = { 0f, 0.15f, 0.30f };

        private static readonly int[] DamageCosts = { 75, 150, 300 };
        private static readonly int[] FireRateCosts = { 75, 150, 300 };
        private static readonly int[] MagazineCosts = { 50, 100, 200 };
        private static readonly int[] HealthCosts = { 60, 120, 250 };
        private static readonly int[] SprintCosts = { 80, 160 };

        public int DamageTier => damageTier;
        public int FireRateTier => fireRateTier;
        public int MagazineTier => magazineTier;
        public int HealthTier => healthTier;
        public int SprintTier => sprintTier;

        public float DamageMultiplier => DamageMultipliers[damageTier];
        public float FireRateMultiplier => FireRateMultipliers[fireRateTier];
        public int MagazineBonus => MagazineBonuses[magazineTier];
        public float BonusHealth => HealthBonuses[healthTier];
        public float SprintBonus => SprintBonuses[sprintTier];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ResetUpgrades()
        {
            damageTier = 0;
            fireRateTier = 0;
            magazineTier = 0;
            healthTier = 0;
            sprintTier = 0;
        }

        public bool CanUpgradeDamage() => damageTier < MaxDamageTier;
        public bool CanUpgradeFireRate() => fireRateTier < MaxFireRateTier;
        public bool CanUpgradeMagazine() => magazineTier < MaxMagazineTier;
        public bool CanUpgradeHealth() => healthTier < MaxHealthTier;
        public bool CanUpgradeSprint() => sprintTier < MaxSprintTier;

        public int GetDamageCost() => damageTier < MaxDamageTier ? DamageCosts[damageTier] : -1;
        public int GetFireRateCost() => fireRateTier < MaxFireRateTier ? FireRateCosts[fireRateTier] : -1;
        public int GetMagazineCost() => magazineTier < MaxMagazineTier ? MagazineCosts[magazineTier] : -1;
        public int GetHealthCost() => healthTier < MaxHealthTier ? HealthCosts[healthTier] : -1;
        public int GetSprintCost() => sprintTier < MaxSprintTier ? SprintCosts[sprintTier] : -1;

        public bool TryUpgradeDamage()
        {
            if (!CanUpgradeDamage()) return false;
            int cost = DamageCosts[damageTier];
            if (!SpendPoints(cost, "Damage Upgrade")) return false;
            damageTier++;
            return true;
        }

        public bool TryUpgradeFireRate()
        {
            if (!CanUpgradeFireRate()) return false;
            int cost = FireRateCosts[fireRateTier];
            if (!SpendPoints(cost, "Fire Rate Upgrade")) return false;
            fireRateTier++;
            return true;
        }

        public bool TryUpgradeMagazine()
        {
            if (!CanUpgradeMagazine()) return false;
            int cost = MagazineCosts[magazineTier];
            if (!SpendPoints(cost, "Magazine Upgrade")) return false;
            magazineTier++;
            return true;
        }

        public bool TryUpgradeHealth()
        {
            if (!CanUpgradeHealth()) return false;
            int cost = HealthCosts[healthTier];
            if (!SpendPoints(cost, "Health Upgrade")) return false;
            healthTier++;
            ApplyHealthUpgrade();
            return true;
        }

        public bool TryUpgradeSprint()
        {
            if (!CanUpgradeSprint()) return false;
            int cost = SprintCosts[sprintTier];
            if (!SpendPoints(cost, "Sprint Upgrade")) return false;
            sprintTier++;
            return true;
        }

        private bool SpendPoints(int cost, string reason)
        {
            var ps = Systems.PointsSystem.Instance;
            if (ps == null || !ps.CanAfford(cost)) return false;
            return ps.SpendPoints(cost, reason);
        }

        private void ApplyHealthUpgrade()
        {
            var health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SetMaxHealth(100f + BonusHealth, true);
            }
        }

        public string GetDamageDescription()
        {
            if (!CanUpgradeDamage()) return "MAX";
            return $"+{Mathf.RoundToInt((DamageMultipliers[damageTier + 1] - 1f) * 100)}% DMG";
        }

        public string GetFireRateDescription()
        {
            if (!CanUpgradeFireRate()) return "MAX";
            return $"+{Mathf.RoundToInt((1f - FireRateMultipliers[fireRateTier + 1]) * 100)}% ROF";
        }

        public string GetMagazineDescription()
        {
            if (!CanUpgradeMagazine()) return "MAX";
            return $"+{MagazineBonuses[magazineTier + 1]} ROUNDS";
        }

        public string GetHealthDescription()
        {
            if (!CanUpgradeHealth()) return "MAX";
            return $"+{HealthBonuses[healthTier + 1]} HP";
        }

        public string GetSprintDescription()
        {
            if (!CanUpgradeSprint()) return "MAX";
            return $"+{Mathf.RoundToInt(SprintBonuses[sprintTier + 1] * 100)}% SPEED";
        }
    }
}
