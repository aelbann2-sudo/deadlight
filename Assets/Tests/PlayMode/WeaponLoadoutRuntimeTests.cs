using System.Collections;
using System.Reflection;
using Deadlight.Data;
using Deadlight.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deadlight.Tests.PlayMode
{
    public class WeaponLoadoutRuntimeTests
    {
        [UnityTest]
        public IEnumerator Loadout_AllowsFourWeapons_AndRejectsFifth()
        {
            var player = new GameObject("LoadoutTestPlayer");
            player.AddComponent<AudioSource>();
            var shooting = player.AddComponent<PlayerShooting>();

            yield return null;

            shooting.ResetLoadout(WeaponData.CreatePistol());
            Assert.IsTrue(shooting.HasWeaponType(WeaponType.Pistol), "Pistol should be in loadout after reset.");

            Assert.IsTrue(shooting.TryAddWeaponToLoadout(WeaponData.CreateSMG()), "SMG should fill first free slot.");
            Assert.IsTrue(shooting.TryAddWeaponToLoadout(WeaponData.CreateSniperRifle()), "Sniper should fill second free slot.");
            Assert.IsTrue(shooting.TryAddWeaponToLoadout(WeaponData.CreateAssaultRifle()), "AR should fill third free slot.");

            Assert.IsTrue(shooting.HasWeaponType(WeaponType.SMG), "SMG should be present in loadout.");
            Assert.IsTrue(shooting.HasWeaponType(WeaponType.SniperRifle), "Sniper should be present in loadout.");
            Assert.IsTrue(shooting.HasWeaponType(WeaponType.AssaultRifle), "AR should be present in loadout.");
            Assert.IsFalse(shooting.HasFreeWeaponSlot(), "All 4 weapon slots should be occupied.");

            bool addedFifth = shooting.TryAddWeaponToLoadout(WeaponData.CreateFlamethrower());
            Assert.IsFalse(addedFifth, "Fifth weapon should be rejected when loadout is full.");
            Assert.IsFalse(shooting.HasWeaponType(WeaponType.Flamethrower), "Flamethrower should not be added as a fifth weapon.");

            var slotsField = typeof(PlayerShooting).GetField("weaponSlots", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(slotsField, "weaponSlots field not found.");
            var slots = (WeaponData[])slotsField.GetValue(shooting);
            Assert.IsNotNull(slots, "weaponSlots array is null.");
            Assert.AreEqual(4, slots.Length, "Loadout should expose exactly 4 weapon slots.");

            int occupied = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null) occupied++;
            }
            Assert.AreEqual(4, occupied, "Exactly 4 slots should be occupied.");

            Object.Destroy(player);
            yield return null;
        }
    }
}
