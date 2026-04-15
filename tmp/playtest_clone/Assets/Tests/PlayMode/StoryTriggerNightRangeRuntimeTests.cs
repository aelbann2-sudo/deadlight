using System.Collections;
using System.Reflection;
using Deadlight.Core;
using Deadlight.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deadlight.Tests.PlayMode
{
    public class StoryTriggerNightRangeRuntimeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;

            if (GameManager.Instance == null)
            {
                new GameObject("GameManager").AddComponent<GameManager>();
            }

            yield return null;

            SetCurrentNight(10); // Level 4, night-within-level = 1
            GameManager.Instance.ChangeState(GameState.DayPhase);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LevelRelativeRange_AllowsTrigger_OnLaterCampaignNight()
        {
            var trigger = CreateTrigger("LevelRelativeAllowed");
            trigger.ConfigureRuntime(
                "level_relative_allowed",
                once: true,
                dayOnly: false,
                nightOnly: false,
                minNightValue: 1,
                maxNightValue: 3,
                requireUse: false,
                levelRelativeNightRange: true);

            yield return null;

            Assert.IsTrue(InvokeCanTrigger(trigger),
                "Expected trigger to pass when using level-relative night range (night 10 -> within-level 1).");

            Object.Destroy(trigger.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CampaignRelativeRange_BlocksTrigger_OnLaterCampaignNight()
        {
            var trigger = CreateTrigger("CampaignRelativeBlocked");
            trigger.ConfigureRuntime(
                "campaign_relative_blocked",
                once: true,
                dayOnly: false,
                nightOnly: false,
                minNightValue: 1,
                maxNightValue: 3,
                requireUse: false,
                levelRelativeNightRange: false);

            yield return null;

            Assert.IsFalse(InvokeCanTrigger(trigger),
                "Expected trigger to fail when using campaign-relative range on night 10 with bounds 1-3.");

            Object.Destroy(trigger.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LevelRelativeRange_RespectsWithinLevelBounds()
        {
            SetCurrentNight(11); // night-within-level = 2

            var trigger = CreateTrigger("LevelRelativeBounds");
            trigger.ConfigureRuntime(
                "level_relative_bounds",
                once: true,
                dayOnly: false,
                nightOnly: false,
                minNightValue: 2,
                maxNightValue: 3,
                requireUse: false,
                levelRelativeNightRange: true);

            yield return null;

            Assert.IsTrue(InvokeCanTrigger(trigger),
                "Expected trigger to pass for within-level night 2 in range 2-3.");

            SetCurrentNight(10); // night-within-level = 1
            Assert.IsFalse(InvokeCanTrigger(trigger),
                "Expected trigger to fail for within-level night 1 when range is 2-3.");

            Object.Destroy(trigger.gameObject);
            yield return null;
        }

        private static StoryTrigger CreateTrigger(string name)
        {
            var go = new GameObject(name);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            return go.AddComponent<StoryTrigger>();
        }

        private static bool InvokeCanTrigger(StoryTrigger trigger)
        {
            var method = typeof(StoryTrigger).GetMethod("CanTrigger", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "StoryTrigger.CanTrigger reflection lookup failed.");
            return (bool)method.Invoke(trigger, null);
        }

        private static void SetCurrentNight(int night)
        {
            var field = typeof(GameManager).GetField("currentNight", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "GameManager.currentNight field not found.");
            field.SetValue(GameManager.Instance, night);
        }
    }
}
