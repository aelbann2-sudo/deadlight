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
            config.nightTitle = "First Light";
            config.description = "Tutorial night. Small waves of basic zombies only.";
            config.waveCount = 2;
            config.baseEnemyCount = 3;
            config.timeBetweenWaves = 6f;
            config.spawnInterval = 2.2f;
            config.healthMultiplier = 0.6f;
            config.damageMultiplier = 0.5f;
            config.speedMultiplier = 0.8f;
            config.hasBoss = false;
            config.completionBonus = 100;
            config.radioMessage = "Hold your position, medic. Survive until dawn and we coordinate next steps.";
            return config;
        }

        public static NightConfig CreateNight2()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 2;
            config.nightTitle = "No One Left Behind";
            config.description = "Runners appear among the horde. Faster, more aggressive infected.";
            config.waveCount = 2;
            config.baseEnemyCount = 5;
            config.timeBetweenWaves = 5f;
            config.spawnInterval = 1.8f;
            config.healthMultiplier = 0.8f;
            config.damageMultiplier = 0.8f;
            config.speedMultiplier = 0.9f;
            config.hasBoss = false;
            config.completionBonus = 150;
            config.radioMessage = "The infected are evolving. Watch for runners — they hunt, not shamble.";
            config.warningMessage = "Warning: Runners detected";
            return config;
        }

        public static NightConfig CreateNight3()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 3;
            config.nightTitle = "The Source";
            config.description = "Exploders and Spitters join the horde. Full mutation spectrum active.";
            config.waveCount = 3;
            config.baseEnemyCount = 7;
            config.timeBetweenWaves = 4f;
            config.spawnInterval = 1.4f;
            config.healthMultiplier = 1.0f;
            config.damageMultiplier = 1.0f;
            config.speedMultiplier = 1.0f;
            config.hasBoss = false;
            config.completionBonus = 250;
            config.radioMessage = "New mutation types detected. Exploders, spitters — the infection is adapting to you.";
            config.warningMessage = "Warning: New mutations inbound";
            return config;
        }

        public static NightConfig CreateNight4()
        {
            var config = CreateInstance<NightConfig>();
            config.nightNumber = 4;
            config.nightTitle = "Operation Deadlight";
            config.description = "Final stand. All enemy types plus Subject 23 boss. Longest night.";
            config.waveCount = 4;
            config.baseEnemyCount = 10;
            config.timeBetweenWaves = 3f;
            config.spawnInterval = 1.0f;
            config.healthMultiplier = 1.3f;
            config.damageMultiplier = 1.3f;
            config.speedMultiplier = 1.1f;
            config.hasBoss = true;
            config.completionBonus = 500;
            config.radioMessage = "Final night. Subject 23 is converging. Hold until dawn. This is everything.";
            config.warningMessage = "SUBJECT 23 INBOUND";
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
