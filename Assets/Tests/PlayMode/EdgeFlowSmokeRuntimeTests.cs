using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Deadlight.Core;
using Deadlight.Narrative;
using Deadlight.Systems;
using Deadlight.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deadlight.Tests.PlayMode
{
    public class EdgeFlowSmokeRuntimeTests
    {
        [UnityTest]
        public IEnumerator RetryDawn_DoesNotGrantNightRewardOrIncrementSurvivedCount()
        {
            yield return BootstrapDayRun();

            var gameManager = GameManager.Instance;
            var points = PointsSystem.Instance;
            var story = StoryObjective.Instance;

            Assert.IsNotNull(gameManager, "GameManager is missing.");
            Assert.IsNotNull(points, "PointsSystem is missing.");
            Assert.IsNotNull(story, "StoryObjective is missing.");

            SetField(gameManager, "maxObjectiveRetriesPerStep", 1);

            int nightsSurvivedBefore = points.NightsSurvived;
            int historyCountBefore = points.GetPointsHistory().Count;

            gameManager.ChangeState(GameState.NightPhase);
            yield return null;

            Assert.IsTrue(story.HasActiveObjective, "Story objective should be active during night.");
            Assert.IsFalse(story.IsComplete, "Story objective should be incomplete to trigger retry flow.");

            gameManager.OnNightSurvived();
            yield return null;

            Assert.AreEqual(GameState.DawnPhase, gameManager.CurrentState, "Run should transition to Dawn after night survive.");
            Assert.IsTrue(gameManager.WillRetryCurrentStepOnAdvance, "Retry flag should be enabled for first objective miss.");
            Assert.AreEqual(nightsSurvivedBefore, points.NightsSurvived, "Retry dawn should not increment nights survived.");

            var history = points.GetPointsHistory();
            for (int i = historyCountBefore; i < history.Count; i++)
            {
                string source = history[i].source ?? string.Empty;
                Assert.AreNotEqual("Night Survived", source, "Retry dawn should not award Night Survived points.");
                Assert.IsFalse(source.StartsWith("Night Grade", System.StringComparison.Ordinal), "Retry dawn should not award Night Grade points.");
            }

            gameManager.AdvanceToNextNight();
            yield return null;

            Assert.AreEqual(1, gameManager.CurrentNight, "Retry advance should keep the same campaign night.");
            Assert.AreEqual(GameState.DayPhase, gameManager.CurrentState, "Retry advance should return to day phase.");
        }

        [UnityTest]
        public IEnumerator ObjectiveMissPenalty_ForcesAdvanceAndAppliesExactlyOneNight()
        {
            yield return BootstrapDayRun();

            var gameManager = GameManager.Instance;
            var story = StoryObjective.Instance;
            Assert.IsNotNull(gameManager, "GameManager is missing.");
            Assert.IsNotNull(story, "StoryObjective is missing.");

            SetField(gameManager, "maxObjectiveRetriesPerStep", 0);

            gameManager.ChangeState(GameState.NightPhase);
            yield return null;

            Assert.IsTrue(story.HasActiveObjective, "Story objective should be active during night.");
            Assert.IsFalse(story.IsComplete, "Story objective should be incomplete to force miss penalty.");

            gameManager.OnNightSurvived();
            yield return null;

            Assert.AreEqual(GameState.DawnPhase, gameManager.CurrentState, "Expected Dawn transition after survived night.");
            Assert.IsFalse(gameManager.WillRetryCurrentStepOnAdvance, "Retry should be disabled when max retries is zero.");
            Assert.Greater(gameManager.PendingObjectiveCarryoverPenaltyStacks, 0, "Carryover penalty stack should be queued.");

            gameManager.AdvanceToNextNight();
            yield return null;

            Assert.AreEqual(2, gameManager.CurrentNight, "Forced advance should move to next campaign night.");
            Assert.IsTrue(gameManager.IsObjectivePenaltyActiveThisNight, "Enemy penalty should be active for forced-advance night.");
            Assert.Greater(gameManager.CurrentNightEnemyPenaltyMultiplier, 1f, "Enemy penalty multiplier should be above 1.");

            gameManager.ChangeState(GameState.DawnPhase);
            yield return null;
            gameManager.AdvanceToNextNight();
            yield return null;

            Assert.AreEqual(3, gameManager.CurrentNight, "Second advance should progress normally.");
            Assert.IsFalse(gameManager.IsObjectivePenaltyActiveThisNight, "Enemy penalty should apply for one night only.");
        }

        [UnityTest]
        public IEnumerator PointsSystem_RejectsNonPositiveSpendAmounts()
        {
            yield return BootstrapDayRun();

            var points = PointsSystem.Instance;
            Assert.IsNotNull(points, "PointsSystem is missing.");

            points.ResetSession();
            points.AddPoints(120, "Test Seed");
            int before = points.CurrentPoints;

            Assert.IsFalse(points.SpendPoints(0, "Invalid Zero"), "Zero-point spend should be rejected.");
            Assert.IsFalse(points.SpendPoints(-25, "Invalid Negative"), "Negative spend should be rejected.");
            Assert.AreEqual(before, points.CurrentPoints, "Invalid spends should not change current points.");
            Assert.AreEqual(120, points.TotalEarned, "Invalid spends should not alter earned total.");
            Assert.AreEqual(0, points.TotalSpent, "Invalid spends should not alter spent total.");
        }

        [UnityTest]
        public IEnumerator Marker_DropMarkerIsRemovedImmediatelyWhenCrateLooted()
        {
            yield return BootstrapDayRun();
            yield return EnsureFreshObjectiveMarker();

            DestroyAllOfType<SupplyCrate>();
            yield return null;

            var marker = Object.FindFirstObjectByType<ObjectiveMarker>();
            Assert.IsNotNull(marker, "ObjectiveMarker is missing.");

            var crateObj = new GameObject("MarkerDropCrate");
            var crate = crateObj.AddComponent<SupplyCrate>();
            crate.SetTier(CrateTier.Common);

            marker.PingContestedDrop(crate.transform);
            yield return null;

            Assert.GreaterOrEqual(CountMarkersWithPrefix(marker, "DROP "), 1, "Drop marker should be visible before looting.");

            InvokePrivate(crate, "CompleteLoot");
            yield return null;

            Assert.AreEqual(0, CountMarkersWithPrefix(marker, "DROP "), "Drop marker should be cleared immediately after loot.");
        }

        [UnityTest]
        public IEnumerator Marker_FastDayToNightTransition_DoesNotRestoreMissionMarkers()
        {
            yield return BootstrapDayRun();
            yield return EnsureFreshObjectiveMarker();

            var gameManager = GameManager.Instance;
            var marker = Object.FindFirstObjectByType<ObjectiveMarker>();
            Assert.IsNotNull(gameManager, "GameManager is missing.");
            Assert.IsNotNull(marker, "ObjectiveMarker is missing.");

            gameManager.ChangeState(GameState.DayPhase);
            yield return null;

            marker.RefreshTargets();
            yield return null;
            Assert.Greater(CountMissionMarkers(marker), 0, "Expected at least one mission marker in day phase.");

            gameManager.ChangeState(GameState.NightPhase);
            yield return null;
            yield return new WaitForSeconds(0.65f);

            Assert.AreEqual(0, CountMissionMarkers(marker), "Mission markers should remain cleared in night phase.");
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

            if (StoryObjective.Instance == null)
            {
                new GameObject("StoryObjective").AddComponent<StoryObjective>();
                yield return null;
            }

            if (GameManager.Instance.CurrentState != GameState.DayPhase)
            {
                GameManager.Instance.ChangeState(GameState.DayPhase);
            }

            yield return null;
        }

        private static IEnumerator EnsureFreshObjectiveMarker()
        {
            DestroyAllOfType<ObjectiveMarker>();
            yield return null;

            var markerObject = new GameObject("TestObjectiveMarker");
            markerObject.AddComponent<ObjectiveMarker>();
            yield return null;
        }

        private static int CountMarkersWithPrefix(ObjectiveMarker marker, string prefix)
        {
            var markerField = typeof(ObjectiveMarker).GetField("markers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(markerField, "ObjectiveMarker.markers field not found.");

            var rawList = markerField.GetValue(marker) as IEnumerable;
            Assert.IsNotNull(rawList, "ObjectiveMarker marker list is null.");

            int count = 0;
            foreach (var entry in rawList)
            {
                if (entry == null)
                {
                    continue;
                }

                var prefixField = entry.GetType().GetField("prefix", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string value = prefixField?.GetValue(entry) as string;
                if (string.Equals(value, prefix, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountMissionMarkers(ObjectiveMarker marker)
        {
            var markerField = typeof(ObjectiveMarker).GetField("markers", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(markerField, "ObjectiveMarker.markers field not found.");

            var rawList = markerField.GetValue(marker) as IEnumerable;
            Assert.IsNotNull(rawList, "ObjectiveMarker marker list is null.");

            int count = 0;
            foreach (var entry in rawList)
            {
                if (entry == null)
                {
                    continue;
                }

                var prefixField = entry.GetType().GetField("prefix", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string prefix = prefixField?.GetValue(entry) as string;

                if (string.Equals(prefix, "OBJ ", System.StringComparison.Ordinal) ||
                    string.Equals(prefix, "ZONE ", System.StringComparison.Ordinal) ||
                    string.Equals(prefix, "BEACON ", System.StringComparison.Ordinal) ||
                    string.Equals(prefix, "CACHE ", System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            Assert.IsNotNull(target, $"Cannot invoke method '{methodName}' on null target.");
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(method, $"Method '{methodName}' not found on {target.GetType().Name}.");
            return method.Invoke(target, args);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            Assert.IsNotNull(target, $"Cannot set field '{fieldName}' on null target.");
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void CleanupTransientObjects()
        {
            DestroyAllOfType<SupplyCrate>();
            DestroyAllOfType<HelicopterDrop>();
            DestroyAllOfType<FallingCrate>();
            DestroyAllOfType<Deadlight.Systems.Pickup>();
            DestroyAllOfType<Deadlight.Systems.PickupItem>();
            DestroyAllOfType<Deadlight.Core.PickupItem>();
        }

        private static void DestroyAllOfType<T>() where T : Component
        {
            var found = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null)
                {
                    Object.Destroy(found[i].gameObject);
                }
            }
        }
    }
}
