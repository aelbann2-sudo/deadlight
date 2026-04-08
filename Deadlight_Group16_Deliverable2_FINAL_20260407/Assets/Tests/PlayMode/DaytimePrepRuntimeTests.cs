using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deadlight.Core;
using Deadlight.Enemy;
using Deadlight.Player;
using Deadlight.Systems;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Deadlight.Tests.PlayMode
{
    public class DaytimePrepRuntimeTests
    {
        [UnityTest]
        public IEnumerator Crafting_DayOnly_SpendsResources_RespectsCaps_AndAppliesNightPrep()
        {
            yield return BootstrapDayRun();

            var gameManager = GameManager.Instance;
            var resourceManager = ResourceManager.Instance;
            var crafting = CraftingSystem.Instance;

            Assert.IsNotNull(gameManager, "GameManager is missing.");
            Assert.IsNotNull(resourceManager, "ResourceManager is missing.");
            Assert.IsNotNull(crafting, "CraftingSystem is missing.");

            resourceManager.SetResource(ResourceType.Scrap, 20);
            resourceManager.SetResource(ResourceType.Wood, 20);
            resourceManager.SetResource(ResourceType.Chemicals, 20);
            resourceManager.SetResource(ResourceType.Electronics, 20);
            resourceManager.SetResource(ResourceType.BlueprintToken, 5);

            Assert.IsTrue(crafting.CanCraft(CraftingRecipeId.AmmoCache), "Ammo Cache should be craftable in day phase.");
            Assert.IsTrue(crafting.Craft(CraftingRecipeId.AmmoCache), "First Ammo Cache craft failed.");
            Assert.IsTrue(crafting.Craft(CraftingRecipeId.AmmoCache), "Second Ammo Cache craft failed.");
            Assert.IsFalse(crafting.Craft(CraftingRecipeId.AmmoCache), "Ammo Cache per-day cap was not enforced.");

            Assert.AreEqual(12, resourceManager.GetResource(ResourceType.Scrap), "Scrap spend mismatch after Ammo Cache crafts.");
            Assert.AreEqual(16, resourceManager.GetResource(ResourceType.Wood), "Wood spend mismatch after Ammo Cache crafts.");

            Assert.IsTrue(crafting.Craft(CraftingRecipeId.FieldMed), "Field Med craft failed.");
            Assert.IsTrue(crafting.Craft(CraftingRecipeId.ShockBeacon), "Shock Beacon craft failed.");
            Assert.AreEqual(4, resourceManager.GetResource(ResourceType.BlueprintToken), "Shock Beacon should consume one blueprint token.");

            gameManager.StartNightPhase();
            yield return null;

            var snapshot = crafting.GetNightPrepSnapshot();
            Assert.AreEqual(70, snapshot.ammoReserveGrant, "Ammo Cache night grant mismatch.");
            Assert.Greater(snapshot.healGrant, 24f, "Field Med heal grant missing.");
            Assert.Less(snapshot.enemySpeedMultiplier, 1f, "Shock Beacon enemy speed modifier missing.");
            Assert.AreEqual(1f, snapshot.softPenaltyDamageMultiplier, 0.0001f, "Soft penalty should be disabled when prep exists.");
            Assert.IsFalse(crafting.CanCraft(CraftingRecipeId.AmmoCache), "Crafting should be disabled outside day phase.");
        }

        [UnityTest]
        public IEnumerator NoPrepDay_AppliesSoftPenalty_ButSecuredDropRemovesIt()
        {
            yield return BootstrapDayRun();

            var gameManager = GameManager.Instance;
            var crafting = CraftingSystem.Instance;
            Assert.IsNotNull(gameManager, "GameManager is missing.");
            Assert.IsNotNull(crafting, "CraftingSystem is missing.");

            crafting.FinalizeDayPrep();
            var noPrepSnapshot = crafting.GetNightPrepSnapshot();
            Assert.AreEqual(1.08f, noPrepSnapshot.softPenaltyDamageMultiplier, 0.0001f, "No-prep penalty multiplier mismatch.");
            Assert.Greater(noPrepSnapshot.softPenaltyDuration, 0f, "No-prep penalty duration should be active.");

            gameManager.StartNightPhase();
            yield return null;

            Assert.AreEqual(1.08f, crafting.GetCurrentPenaltyDamageMultiplier(), 0.0001f, "Night penalty multiplier should be active at night start.");

            gameManager.AdvanceToNextNight();
            yield return null;

            crafting.NotifyContestedDropSecured();
            crafting.FinalizeDayPrep();
            var securedSnapshot = crafting.GetNightPrepSnapshot();
            Assert.IsTrue(securedSnapshot.securedDrop, "Secured drop flag should be present in night snapshot.");
            Assert.AreEqual(1f, securedSnapshot.softPenaltyDamageMultiplier, 0.0001f, "Secured drop day should not receive soft penalty.");
        }

        [UnityTest]
        public IEnumerator ContestedCrate_SecureAndExpireFlows_WorkAndGrantRewards()
        {
            yield return BootstrapDayRun();

            var resourceManager = ResourceManager.Instance;
            var pointsSystem = PointsSystem.Instance;
            Assert.IsNotNull(resourceManager, "ResourceManager is missing.");
            Assert.IsNotNull(pointsSystem, "PointsSystem is missing.");

            resourceManager.SetResource(ResourceType.Scrap, 0);
            resourceManager.SetResource(ResourceType.Wood, 0);
            resourceManager.SetResource(ResourceType.Chemicals, 0);
            resourceManager.SetResource(ResourceType.Electronics, 0);
            resourceManager.SetResource(ResourceType.BlueprintToken, 0);

            int pointsBefore = pointsSystem.CurrentPoints;

            bool securedCallback = false;
            var secureCrateObj = new GameObject("ContestedSecureCrate");
            var secureCrate = secureCrateObj.AddComponent<SupplyCrate>();
            secureCrate.SetTier(CrateTier.Rare);
            secureCrate.ConfigureContested(0.01f, 1.0f, _ => securedCallback = true, _ => { });

            InvokePrivate(secureCrate, "CompleteLoot");
            yield return null;

            Assert.IsTrue(securedCallback, "Contested secure callback did not fire.");
            Assert.AreEqual(4, resourceManager.GetResource(ResourceType.Scrap), "Rare contested reward scrap mismatch.");
            Assert.AreEqual(3, resourceManager.GetResource(ResourceType.Wood), "Rare contested reward wood mismatch.");
            Assert.AreEqual(2, resourceManager.GetResource(ResourceType.Chemicals), "Rare contested reward chemicals mismatch.");
            Assert.AreEqual(1, resourceManager.GetResource(ResourceType.Electronics), "Rare contested reward electronics mismatch.");
            Assert.AreEqual(1, resourceManager.GetResource(ResourceType.BlueprintToken), "Rare contested reward blueprint mismatch.");
            Assert.AreEqual(pointsBefore + 140, pointsSystem.CurrentPoints, "Rare contested reward points mismatch.");

            bool expiredCallback = false;
            var expireCrateObj = new GameObject("ContestedExpireCrate");
            var expireCrate = expireCrateObj.AddComponent<SupplyCrate>();
            expireCrate.SetTier(CrateTier.Common);
            expireCrate.ConfigureContested(0.05f, 0.06f, _ => { }, _ => expiredCallback = true);

            yield return new WaitForSeconds(2.2f);
            Assert.IsTrue(expiredCallback, "Contested expiry callback did not fire.");
        }

        [UnityTest]
        public IEnumerator DayContestedDrop_FiresOncePerDay_AndProgressesStateFlow()
        {
            yield return BootstrapDayRun();

            var flow = GameFlowController.Instance;
            Assert.IsNotNull(flow, "GameFlowController is missing.");

            SetField(flow, "enableDayContestedDrop", true);
            SetField(flow, "contestedDropDayProgress", 0.01f);
            SetField(flow, "contestedBroadcastDuration", 0.05f);
            SetField(flow, "contestedSecureHoldTime", 0.05f);
            SetField(flow, "contestedExpiryTime", 0.25f);

            InvokePrivate(flow, "ResetDayContestedDropState");
            SetField(flow, "dayContestedDropTriggerTime", Time.time + 0.05f);

            var observedStates = new List<DayContestedDropState>();
            void Handler(DayContestedDropState state, float _) => observedStates.Add(state);
            flow.OnContestedDropStateChanged += Handler;

            try
            {
                yield return new WaitForSeconds(9f);
            }
            finally
            {
                flow.OnContestedDropStateChanged -= Handler;
            }

            int broadcastCount = observedStates.Count(s => s == DayContestedDropState.Broadcast);
            Assert.AreEqual(1, broadcastCount, "Exactly one contested broadcast should occur in a day.");
            Assert.IsTrue(observedStates.Contains(DayContestedDropState.Descent), "Contested drop never entered descent.");
            Assert.IsTrue(observedStates.Contains(DayContestedDropState.Secure), "Contested drop never entered secure state.");
            Assert.IsTrue(
                observedStates.Contains(DayContestedDropState.Expired) ||
                observedStates.Contains(DayContestedDropState.Resolved),
                "Contested drop never resolved or expired.");
        }

        [UnityTest]
        public IEnumerator ResourcePickup_AddsResource_AndShowsCraftingPurposeHint()
        {
            yield return BootstrapDayRun();

            var spawner = PickupSpawner.Instance;
            var resourceManager = ResourceManager.Instance;
            var player = GameObject.FindWithTag("Player");

            Assert.IsNotNull(spawner, "PickupSpawner is missing.");
            Assert.IsNotNull(resourceManager, "ResourceManager is missing.");
            Assert.IsNotNull(player, "Player is missing.");
            Assert.IsNotNull(FloatingTextManager.Instance, "FloatingTextManager is missing.");

            resourceManager.SetResource(ResourceType.Scrap, 0);
            spawner.SpawnPickup(player.transform.position, PickupType.Scrap);

            yield return null;
            yield return null;

            Assert.Greater(resourceManager.GetResource(ResourceType.Scrap), 0, "Scrap pickup did not add resources.");

            var texts = FloatingTextManager.Instance.GetComponentsInChildren<Text>(true);
            bool foundHint = texts.Any(t => t != null && t.text.Contains("Ammo Cache / Weakpoint Intel", StringComparison.Ordinal));
            Assert.IsTrue(foundHint, "Pickup purpose hint text was not shown.");
        }

        [UnityTest]
        public IEnumerator Regression_RegularCrateLootAndNightEmergencyDrop_StillWork()
        {
            yield return BootstrapDayRun();

            var flow = GameFlowController.Instance;
            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(flow, "GameFlowController is missing.");
            Assert.IsNotNull(player, "Player is missing.");

            var shooting = player.GetComponent<PlayerShooting>();
            Assert.IsNotNull(shooting, "PlayerShooting is missing.");

            int reserveBefore = shooting.ReserveAmmo;
            var regularCrateObj = new GameObject("RegularAmmoCrate");
            var regularCrate = regularCrateObj.AddComponent<SupplyCrate>();
            regularCrate.SetTier(CrateTier.Common);
            SetField(regularCrate, "player", player.transform);
            SetField(regularCrate, "contents", CrateContents.Ammo);
            InvokePrivate(regularCrate, "CompleteLoot");
            yield return null;

            Assert.Greater(shooting.ReserveAmmo, reserveBefore, "Regular crate loot no longer grants ammo.");

            SetField(flow, "helicopterFirstDropDelay", 0.05f);
            SetField(flow, "helicopterCooldown", 0.1f);
            SetField(flow, "helicopterDropJitter", 0f);
            SetField(flow, "maxDropsPerPhase", 1);

            GameManager.Instance.StartNightPhase();
            yield return new WaitForSeconds(0.35f);

            var helicopters = UnityEngine.Object.FindObjectsByType<HelicopterDrop>(FindObjectsSortMode.None);
            Assert.Greater(helicopters.Length, 0, "Night emergency helicopter drop did not spawn.");
        }

        private static IEnumerator BootstrapDayRun()
        {
            Time.timeScale = 1f;

            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }

            yield return null;

            CleanupTransientObjects();

            GameManager.Instance.StartNewGame();
            yield return null;
            yield return null;

            if (FloatingTextManager.Instance == null)
            {
                new GameObject("FloatingTextManager").AddComponent<FloatingTextManager>();
            }

            if (GameManager.Instance.CurrentState != GameState.DayPhase)
            {
                GameManager.Instance.ChangeState(GameState.DayPhase);
            }

            yield return null;
        }

        private static void CleanupTransientObjects()
        {
            DestroyAllOfType<SupplyCrate>();
            DestroyAllOfType<HelicopterDrop>();
            DestroyAllOfType<FallingCrate>();
            DestroyAllOfType<EnemyHealth>();
            DestroyAllOfType<Deadlight.Systems.Pickup>();
            DestroyAllOfType<Deadlight.Systems.PickupItem>();
            DestroyAllOfType<Deadlight.Core.PickupItem>();
        }

        private static void DestroyAllOfType<T>() where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    UnityEngine.Object.Destroy(found[i].gameObject);
                }
            }
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            Assert.IsNotNull(target, $"Cannot set field '{fieldName}' on null target.");
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            Assert.IsNotNull(target, $"Cannot invoke method '{methodName}' on null target.");
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on {target.GetType().Name}.");
            return method.Invoke(target, args);
        }
    }
}
