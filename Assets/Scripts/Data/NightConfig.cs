using UnityEngine;
using System.Collections.Generic;

namespace Deadlight.Core
{
    [CreateAssetMenu(fileName = "Night_X", menuName = "Deadlight/Night Configuration")]
    public class NightConfig : ScriptableObject
    {
        [Header("Night Info")]
        public int nightNumber = 1;
        [TextArea] public string description;
        public string nightTitle;

        [Header("Wave Settings")]
        [Tooltip("Number of waves this night")]
        public int waveCount = 3;
        
        [Tooltip("Base number of enemies per wave")]
        public int baseEnemyCount = 10;
        
        [Tooltip("Time between waves in seconds")]
        public float timeBetweenWaves = 5f;
        
        [Tooltip("Time between enemy spawns in seconds")]
        public float spawnInterval = 2f;

        [Header("Enemy Modifiers")]
        [Tooltip("Multiplier for enemy health this night")]
        public float healthMultiplier = 1f;
        
        [Tooltip("Multiplier for enemy damage this night")]
        public float damageMultiplier = 1f;
        
        [Tooltip("Multiplier for enemy speed this night")]
        public float speedMultiplier = 1f;

        [Header("Enemy Types")]
        [Tooltip("Enemy types that can spawn this night")]
        public List<Data.EnemyData> availableEnemies = new List<Data.EnemyData>();

        [Header("Boss")]
        [Tooltip("Does this night have a boss?")]
        public bool hasBoss = false;
        
        [Tooltip("Boss enemy data (if applicable)")]
        public Data.EnemyData bossEnemy;

        [Header("Rewards")]
        [Tooltip("Bonus points for completing this night")]
        public int completionBonus = 100;
        
        [Tooltip("Weapons unlocked after this night")]
        public List<Data.WeaponData> weaponUnlocks = new List<Data.WeaponData>();

        [Header("Narrative")]
        [Tooltip("Radio message played at start of night")]
        [TextArea(3, 5)] public string radioMessage;
        
        [Tooltip("Warning message about mutations/threats")]
        public string warningMessage;

        public static NightConfig CreateNight1()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 1;
            config.nightTitle = "First Night";
            config.description = "Tutorial night. Small waves of basic zombies.";
            config.waveCount = 2;
            config.baseEnemyCount = 5;
            config.timeBetweenWaves = 8f;
            config.spawnInterval = 3f;
            config.healthMultiplier = 1f;
            config.damageMultiplier = 1f;
            config.speedMultiplier = 1f;
            config.hasBoss = false;
            config.completionBonus = 100;
            config.radioMessage = "Survivor, this is Rescue Base Alpha. Hold your position. We're tracking your signal. Survive until dawn and we'll coordinate extraction.";
            return config;
        }

        public static NightConfig CreateNight2()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 2;
            config.nightTitle = "Growing Threat";
            config.description = "Zombies attack from multiple directions. First mutation appears.";
            config.waveCount = 3;
            config.baseEnemyCount = 8;
            config.timeBetweenWaves = 6f;
            config.spawnInterval = 2.5f;
            config.healthMultiplier = 1.25f;
            config.damageMultiplier = 1.15f;
            config.speedMultiplier = 1.1f;
            config.hasBoss = false;
            config.completionBonus = 150;
            config.radioMessage = "Night two. The infected are getting stronger. We're seeing unusual behavior patterns. Stay alert.";
            config.warningMessage = "Warning: Mutation detected";
            return config;
        }

        public static NightConfig CreateNight3()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 3;
            config.nightTitle = "Escalation";
            config.description = "Runners and Exploders join the horde. Multiple mutations active.";
            config.waveCount = 4;
            config.baseEnemyCount = 12;
            config.timeBetweenWaves = 5f;
            config.spawnInterval = 2f;
            config.healthMultiplier = 1.5f;
            config.damageMultiplier = 1.3f;
            config.speedMultiplier = 1.15f;
            config.hasBoss = false;
            config.completionBonus = 200;
            config.radioMessage = "Halfway there, survivor. New infected types have been spotted. Fast ones. Explosive ones. Be ready for anything.";
            config.warningMessage = "Warning: New enemy types detected";
            return config;
        }

        public static NightConfig CreateNight4()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 4;
            config.nightTitle = "The Horde";
            config.description = "Heavy waves with Tank zombies. All mutations possible.";
            config.waveCount = 5;
            config.baseEnemyCount = 18;
            config.timeBetweenWaves = 4f;
            config.spawnInterval = 1.5f;
            config.healthMultiplier = 1.75f;
            config.damageMultiplier = 1.5f;
            config.speedMultiplier = 1.2f;
            config.hasBoss = false;
            config.completionBonus = 300;
            config.radioMessage = "One more night after this. The infected are massing. We're detecting massive life signs. Something big is coming.";
            config.warningMessage = "Warning: Elite enemies detected";
            return config;
        }

        public static NightConfig CreateNight5()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 5;
            config.nightTitle = "Final Stand";
            config.description = "The final night. Survive the boss to earn your rescue.";
            config.waveCount = 3;
            config.baseEnemyCount = 15;
            config.timeBetweenWaves = 5f;
            config.spawnInterval = 1.5f;
            config.healthMultiplier = 2f;
            config.damageMultiplier = 2f;
            config.speedMultiplier = 1.25f;
            config.hasBoss = true;
            config.completionBonus = 500;
            config.radioMessage = "This is it. Final night. Helicopter is en route but there's something massive heading your way. Everything we have depends on you surviving this. Good luck.";
            config.warningMessage = "WARNING: BOSS INCOMING";
            return config;
        }

        public int GetTotalEnemies()
        {
            int total = 0;
            for (int wave = 1; wave <= waveCount; wave++)
            {
                total += Mathf.RoundToInt(baseEnemyCount * (1 + (wave - 1) * 0.3f));
            }
            return total;
        }

        public float GetEstimatedDuration()
        {
            float spawnTime = GetTotalEnemies() * spawnInterval;
            float waveGaps = (waveCount - 1) * timeBetweenWaves;
            return spawnTime + waveGaps;
        }
    }
}
