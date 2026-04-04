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
            config.nightTitle = "Sealed Streets";
            config.description = "Suburban night 1. Runners appear from side streets.";
            config.waveCount = 2;
            config.baseEnemyCount = 5;
            config.timeBetweenWaves = 5f;
            config.spawnInterval = 1.8f;
            config.healthMultiplier = 0.85f;
            config.damageMultiplier = 0.85f;
            config.speedMultiplier = 0.95f;
            config.hasBoss = false;
            config.completionBonus = 120;
            config.radioMessage = "Suburb perimeter breached. Runners closing in from the side streets.";
            return config;
        }

        public static NightConfig CreateForNight(int nightNumber)
        {
            int level = Mathf.Clamp((nightNumber - 1) / 3 + 1, 1, 4);
            int nwl = ((nightNumber - 1) % 3) + 1;
            var config = CreateInstance<NightConfig>();
            config.nightNumber = nightNumber;

            switch (level)
            {
                case 1:
                    switch (nwl)
                    {
                        case 1:
                            config.waveCount = 2;
                            config.baseEnemyCount = 3;
                            config.healthMultiplier = 0.65f;
                            config.damageMultiplier = 0.55f;
                            config.speedMultiplier = 0.80f;
                            config.spawnInterval = 2.25f;
                            config.timeBetweenWaves = 6f;
                            break;
                        case 2:
                            config.waveCount = 2;
                            config.baseEnemyCount = 4;
                            config.healthMultiplier = 0.78f;
                            config.damageMultiplier = 0.68f;
                            config.speedMultiplier = 0.86f;
                            config.spawnInterval = 2f;
                            config.timeBetweenWaves = 5.5f;
                            break;
                        default:
                            config.waveCount = 3;
                            config.baseEnemyCount = 5;
                            config.healthMultiplier = 0.90f;
                            config.damageMultiplier = 0.80f;
                            config.speedMultiplier = 0.92f;
                            config.spawnInterval = 1.75f;
                            config.timeBetweenWaves = 5f;
                            break;
                    }
                    break;
                case 2:
                    switch (nwl)
                    {
                        case 1:
                            config.waveCount = 3;
                            config.baseEnemyCount = 5;
                            config.healthMultiplier = 0.95f;
                            config.damageMultiplier = 0.85f;
                            config.speedMultiplier = 0.95f;
                            config.spawnInterval = 1.75f;
                            config.timeBetweenWaves = 4.75f;
                            break;
                        case 2:
                            config.waveCount = 3;
                            config.baseEnemyCount = 6;
                            config.healthMultiplier = 1.02f;
                            config.damageMultiplier = 0.92f;
                            config.speedMultiplier = 1.00f;
                            config.spawnInterval = 1.6f;
                            config.timeBetweenWaves = 4.3f;
                            break;
                        default:
                            config.waveCount = 4;
                            config.baseEnemyCount = 7;
                            config.healthMultiplier = 1.10f;
                            config.damageMultiplier = 1.00f;
                            config.speedMultiplier = 1.03f;
                            config.spawnInterval = 1.45f;
                            config.timeBetweenWaves = 4f;
                            break;
                    }
                    break;
                case 3:
                    switch (nwl)
                    {
                        case 1:
                            config.waveCount = 3;
                            config.baseEnemyCount = 7;
                            config.healthMultiplier = 1.08f;
                            config.damageMultiplier = 0.98f;
                            config.speedMultiplier = 1.02f;
                            config.spawnInterval = 1.55f;
                            config.timeBetweenWaves = 4f;
                            break;
                        case 2:
                            config.waveCount = 4;
                            config.baseEnemyCount = 8;
                            config.healthMultiplier = 1.15f;
                            config.damageMultiplier = 1.05f;
                            config.speedMultiplier = 1.05f;
                            config.spawnInterval = 1.4f;
                            config.timeBetweenWaves = 3.8f;
                            break;
                        default:
                            config.waveCount = 4;
                            config.baseEnemyCount = 9;
                            config.healthMultiplier = 1.22f;
                            config.damageMultiplier = 1.12f;
                            config.speedMultiplier = 1.09f;
                            config.spawnInterval = 1.3f;
                            config.timeBetweenWaves = 3.6f;
                            break;
                    }
                    break;
                default:
                    switch (nwl)
                    {
                        case 1:
                            config.waveCount = 4;
                            config.baseEnemyCount = 9;
                            config.healthMultiplier = 1.18f;
                            config.damageMultiplier = 1.05f;
                            config.speedMultiplier = 1.04f;
                            config.spawnInterval = 1.45f;
                            config.timeBetweenWaves = 3.8f;
                            break;
                        case 2:
                            config.waveCount = 5;
                            config.baseEnemyCount = 10;
                            config.healthMultiplier = 1.28f;
                            config.damageMultiplier = 1.14f;
                            config.speedMultiplier = 1.08f;
                            config.spawnInterval = 1.3f;
                            config.timeBetweenWaves = 3.5f;
                            break;
                        default:
                            config.waveCount = 5;
                            config.baseEnemyCount = 11;
                            config.healthMultiplier = 1.38f;
                            config.damageMultiplier = 1.22f;
                            config.speedMultiplier = 1.12f;
                            config.spawnInterval = 1.2f;
                            config.timeBetweenWaves = 3.2f;
                            break;
                    }
                    break;
            }

            config.hasBoss = nightNumber >= 12;
            config.completionBonus = 50 + level * 50 + (nwl - 1) * 30;
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
