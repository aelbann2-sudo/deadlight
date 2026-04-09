using System.Collections;
using Deadlight.Core;
using Deadlight.Data;
using Deadlight.Player;
using Deadlight.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deadlight.Tests.PlayMode
{
    public class LevelTransitionResetRuntimeTests
    {
        [UnityTest]
        public IEnumerator StartNextLevel_ResetsPointsUpgradesAndLoadout()
        {
            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }

            yield return null;

            CleanupTransientObjects();
            GameManager.Instance.StartNewGame();
            yield return null;
            yield return null;

            var points = PointsSystem.Instance;
            Assert.IsNotNull(points, "PointsSystem is missing.");

            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player is missing.");

            var shooting = player.GetComponent<PlayerShooting>();
            var upgrades = player.GetComponent<PlayerUpgrades>();
            if (upgrades == null) upgrades = player.AddComponent<PlayerUpgrades>();
            var throwables = player.GetComponent<ThrowableSystem>();
            if (throwables == null) throwables = player.AddComponent<ThrowableSystem>();
            var medkits = player.GetComponent<PlayerMedkitSystem>();
            if (medkits == null) medkits = player.AddComponent<PlayerMedkitSystem>();
            var armor = player.GetComponent<PlayerArmor>();
            if (armor == null) armor = player.AddComponent<PlayerArmor>();

            Assert.IsNotNull(shooting, "PlayerShooting is missing.");
            Assert.IsNotNull(upgrades, "PlayerUpgrades is missing.");
            Assert.IsNotNull(throwables, "ThrowableSystem is missing.");
            Assert.IsNotNull(medkits, "PlayerMedkitSystem is missing.");
            Assert.IsNotNull(armor, "PlayerArmor is missing.");

            points.AddPoints(600, "test seed");
            Assert.Greater(points.CurrentPoints, 0, "Failed to seed points.");

            Assert.IsTrue(upgrades.TryUpgradeDamage(), "Damage upgrade purchase failed during setup.");
            Assert.IsTrue(shooting.TryAddWeaponToLoadout(WeaponData.CreateSMG(), false), "SMG add failed during setup.");

            throwables.AddGrenades(2);
            throwables.AddMolotovs(2);
            medkits.AddMedkits(2);
            armor.EquipVest(ArmorTier.Level2);
            armor.EquipHelmet(ArmorTier.Level1);

            Assert.IsTrue(shooting.HasWeaponType(WeaponType.SMG), "Setup failed to add SMG.");
            Assert.Greater(upgrades.DamageTier, 0, "Setup failed to apply upgrade tier.");
            Assert.Greater(points.CurrentPoints, 0, "Setup failed to keep points.");

            GameManager.Instance.StartNextLevel();
            yield return null;
            yield return null;

            Assert.AreEqual(0, points.CurrentPoints, "Points should reset on inter-level transition.");
            Assert.AreEqual(0, upgrades.DamageTier, "Damage upgrade tier should reset on inter-level transition.");
            Assert.AreEqual(0, upgrades.FireRateTier, "Fire rate upgrade tier should reset on inter-level transition.");
            Assert.AreEqual(0, upgrades.MagazineTier, "Magazine upgrade tier should reset on inter-level transition.");
            Assert.AreEqual(0, upgrades.HealthTier, "Health upgrade tier should reset on inter-level transition.");
            Assert.AreEqual(0, upgrades.SprintTier, "Sprint upgrade tier should reset on inter-level transition.");

            Assert.IsTrue(shooting.HasWeaponType(WeaponType.Pistol), "Pistol should be present after reset.");
            Assert.IsFalse(shooting.HasWeaponType(WeaponType.SMG), "Purchased secondary weapon should be cleared on inter-level transition.");

            Assert.AreEqual(0, medkits.MedkitCount, "Medkits should reset on inter-level transition.");
            Assert.AreEqual(ArmorTier.None, armor.VestTier, "Vest should reset on inter-level transition.");
            Assert.AreEqual(ArmorTier.None, armor.HelmetTier, "Helmet should reset on inter-level transition.");
            Assert.AreEqual(throwables.MaxGrenades >= 2 ? 2 : throwables.MaxGrenades, throwables.GrenadeCount, "Grenade inventory should reset to default starting value.");
            Assert.AreEqual(throwables.MaxMolotovs >= 1 ? 1 : throwables.MaxMolotovs, throwables.MolotovCount, "Molotov inventory should reset to default starting value.");
        }

        private static void CleanupTransientObjects()
        {
            var pickups = Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null) Object.Destroy(pickups[i].gameObject);
            }

            var crates = Object.FindObjectsByType<SupplyCrate>(FindObjectsSortMode.None);
            for (int i = 0; i < crates.Length; i++)
            {
                if (crates[i] != null) Object.Destroy(crates[i].gameObject);
            }
        }
    }
}
