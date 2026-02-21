using UnityEngine;

namespace Deadlight.Core
{
    [CreateAssetMenu(fileName = "DifficultySettings", menuName = "Deadlight/Difficulty Settings")]
    public class DifficultySettings : ScriptableObject
    {
        [Header("Difficulty Info")]
        public Difficulty difficulty;
        public string displayName;
        [TextArea] public string description;

        [Header("Player Modifiers")]
        [Tooltip("Multiplier for player's starting health")]
        public float playerHealthMultiplier = 1f;
        
        [Tooltip("Multiplier for damage taken by player")]
        public float playerDamageTakenMultiplier = 1f;

        [Header("Enemy Modifiers")]
        [Tooltip("Multiplier for enemy health")]
        public float enemyHealthMultiplier = 1f;
        
        [Tooltip("Multiplier for enemy damage dealt")]
        public float enemyDamageMultiplier = 1f;
        
        [Tooltip("Multiplier for enemy movement speed")]
        public float enemySpeedMultiplier = 1f;

        [Header("Wave Modifiers")]
        [Tooltip("Multiplier for number of enemies per wave")]
        public float waveEnemyCountMultiplier = 1f;
        
        [Tooltip("Multiplier for time between spawns (lower = faster)")]
        public float spawnIntervalMultiplier = 1f;

        [Header("Resource Modifiers")]
        [Tooltip("Multiplier for resource spawn rates")]
        public float resourceSpawnMultiplier = 1f;
        
        [Tooltip("Multiplier for ammo found in pickups")]
        public float ammoDropMultiplier = 1f;
        
        [Tooltip("Multiplier for health pickup effectiveness")]
        public float healthPickupMultiplier = 1f;

        [Header("Scoring")]
        [Tooltip("Score multiplier for leaderboard")]
        public float scoreMultiplier = 1f;

        public static DifficultySettings CreateEasySettings()
        {
            var settings = CreateInstance<DifficultySettings>();
            settings.difficulty = Difficulty.Easy;
            settings.displayName = "Easy";
            settings.description = "Relaxed survival. Weaker zombies, more supplies, longer days.";
            
            settings.playerHealthMultiplier = 1.5f;
            settings.playerDamageTakenMultiplier = 0.5f;
            
            settings.enemyHealthMultiplier = 0.5f;
            settings.enemyDamageMultiplier = 0.5f;
            settings.enemySpeedMultiplier = 0.75f;
            
            settings.waveEnemyCountMultiplier = 0.5f;
            settings.spawnIntervalMultiplier = 1.8f;
            
            settings.resourceSpawnMultiplier = 2f;
            settings.ammoDropMultiplier = 2f;
            settings.healthPickupMultiplier = 1.5f;
            
            settings.scoreMultiplier = 0.75f;
            
            return settings;
        }

        public static DifficultySettings CreateNormalSettings()
        {
            var settings = CreateInstance<DifficultySettings>();
            settings.difficulty = Difficulty.Normal;
            settings.displayName = "Normal";
            settings.description = "The standard survival experience.";
            
            settings.playerHealthMultiplier = 1f;
            settings.playerDamageTakenMultiplier = 1f;
            
            settings.enemyHealthMultiplier = 1f;
            settings.enemyDamageMultiplier = 1f;
            settings.enemySpeedMultiplier = 1f;
            
            settings.waveEnemyCountMultiplier = 1f;
            settings.spawnIntervalMultiplier = 1f;
            
            settings.resourceSpawnMultiplier = 1f;
            settings.ammoDropMultiplier = 1f;
            settings.healthPickupMultiplier = 1f;
            
            settings.scoreMultiplier = 1f;
            
            return settings;
        }

        public static DifficultySettings CreateHardSettings()
        {
            var settings = CreateInstance<DifficultySettings>();
            settings.difficulty = Difficulty.Hard;
            settings.displayName = "Hard";
            settings.description = "Increased enemy stats. Scarcer resources. For experienced survivors.";
            
            settings.playerHealthMultiplier = 0.9f;
            settings.playerDamageTakenMultiplier = 1.25f;
            
            settings.enemyHealthMultiplier = 1.5f;
            settings.enemyDamageMultiplier = 1.5f;
            settings.enemySpeedMultiplier = 1.15f;
            
            settings.waveEnemyCountMultiplier = 1.4f;
            settings.spawnIntervalMultiplier = 0.7f;
            
            settings.resourceSpawnMultiplier = 0.6f;
            settings.ammoDropMultiplier = 0.7f;
            settings.healthPickupMultiplier = 0.75f;
            
            settings.scoreMultiplier = 1.5f;
            
            return settings;
        }
    }
}
