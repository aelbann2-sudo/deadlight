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
            ApplyNarrativeDefaults(config, 1, overwrite: false);
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
            ApplyNarrativeDefaults(config, 2, overwrite: false);
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
            ApplyNarrativeDefaults(config, 3, overwrite: false);
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
            ApplyNarrativeDefaults(config, 4, overwrite: false);
            return config;
        }

        private struct NightNarrativePreset
        {
            public string title;
            public string description;
            public string radioMessage;
            public string warningMessage;
        }

        private static NightNarrativePreset GetNightNarrativePreset(int nightNumber)
        {
            switch (Mathf.Clamp(nightNumber, 1, 12))
            {
                case 1:
                    return new NightNarrativePreset
                    {
                        title = "First Light",
                        description = "Tutorial night. Keep the street clear, learn the basics, and hold your first line.",
                        radioMessage = "Hold your position, medic. The zone is still quiet, but that will not last long.",
                        warningMessage = "Warning: sparse contacts are approaching."
                    };
                case 2:
                    return new NightNarrativePreset
                    {
                        title = "Running Dark",
                        description = "Runners join the horde and punish bad spacing. Use walls, cars, and corners to break pursuit.",
                        radioMessage = "Movement on the perimeter. The infected are running now, so do not let them split you off the route.",
                        warningMessage = "Warning: runners detected on the perimeter."
                    };
                case 3:
                    return new NightNarrativePreset
                    {
                        title = "The Source",
                        description = "Exploders and spitters enter the fight. The infection is mutating into a wider threat profile.",
                        radioMessage = "New mutation types confirmed. Exploders and spitters are changing the fight tonight.",
                        warningMessage = "Warning: mutation surge in progress."
                    };
                case 4:
                    return new NightNarrativePreset
                    {
                        title = "Sealed Streets",
                        description = "The suburb is splitting your sightlines. Watch the side streets and keep the perimeter sealed.",
                        radioMessage = "The suburb routes are narrowing. Keep your eyes on the side streets and do not chase too far.",
                        warningMessage = "Warning: flank routes compromised."
                    };
                case 5:
                    return new NightNarrativePreset
                    {
                        title = "Broken Shelter",
                        description = "Shelters and back alleys are forcing the horde into tighter lanes. Expect stronger pressure on each wave.",
                        radioMessage = "Shelter relay is down. The infected are funneling through the alleys, so keep moving and stay tight.",
                        warningMessage = "Warning: shelter lines are being overrun."
                    };
                case 6:
                    return new NightNarrativePreset
                    {
                        title = "Cut Power",
                        description = "The district grid is failing and the night is getting faster. Break contact before the line collapses.",
                        radioMessage = "Power is dropping across the district. Every breach will come faster from here on out.",
                        warningMessage = "Warning: power grid failing."
                    };
                case 7:
                    return new NightNarrativePreset
                    {
                        title = "Quarantine Wall",
                        description = "The quarantine line is cracking and pressure is rising from every side. Hold if you can, move if you must.",
                        radioMessage = "The quarantine wall is cracking. If the line breaks, the whole sector goes with it.",
                        warningMessage = "Warning: quarantine wall breached."
                    };
                case 8:
                    return new NightNarrativePreset
                    {
                        title = "Signal Loss",
                        description = "Facility uplinks are active again and the strain is adapting in real time. Expect sharper, faster pressure.",
                        radioMessage = "Facility logs just came back online. The infection is adapting to your tactics as we speak.",
                        warningMessage = "Warning: adaptive strain active."
                    };
                case 9:
                    return new NightNarrativePreset
                    {
                        title = "Black Sector",
                        description = "Command scrubbed this sector clean. Only archive traces remain, and the truth is starting to surface.",
                        radioMessage = "Archive signals confirm this sector was burned clean. The data survived, and that means somebody lied.",
                        warningMessage = "Warning: archive purge underway."
                    };
                case 10:
                    return new NightNarrativePreset
                    {
                        title = "Deadlight Protocol",
                        description = "The endgame is close. The site is being erased before dawn, so every second now matters.",
                        radioMessage = "Deadlight protocol is active. They are trying to wipe the site before sunrise, so hold fast.",
                        warningMessage = "Warning: Deadlight protocol engaged."
                    };
                case 11:
                    return new NightNarrativePreset
                    {
                        title = "Subject 23",
                        description = "Subject 23 is being tracked and the heavy threats are converging. This is the penultimate stand.",
                        radioMessage = "Subject 23 is close. If it reaches open ground, nothing in the zone will slow it down.",
                        warningMessage = "Warning: Subject 23 converging."
                    };
                case 12:
                default:
                    return new NightNarrativePreset
                    {
                        title = "Final Broadcast",
                        description = "Final night. The source is exposed, the beacon is armed, and the last stand begins now.",
                        radioMessage = "Final night. Bring the beacon online and survive until dawn. This is the last broadcast.",
                        warningMessage = "Warning: final night, all systems at risk."
                    };
            }
        }

        public static void ApplyNarrativeDefaults(NightConfig config, int nightNumber, bool overwrite = true)
        {
            if (config == null)
            {
                return;
            }

            var preset = GetNightNarrativePreset(nightNumber);

            if (overwrite || string.IsNullOrWhiteSpace(config.nightTitle))
            {
                config.nightTitle = preset.title;
            }

            if (overwrite || string.IsNullOrWhiteSpace(config.description))
            {
                config.description = preset.description;
            }

            if (overwrite || string.IsNullOrWhiteSpace(config.radioMessage))
            {
                config.radioMessage = preset.radioMessage;
            }

            if (overwrite || string.IsNullOrWhiteSpace(config.warningMessage))
            {
                config.warningMessage = preset.warningMessage;
            }
        }

        public static NightConfig CreateForNight(int nightNumber)
        {
            int level = Mathf.Clamp((nightNumber - 1) / 3 + 1, 1, 4);
            int nwl = ((nightNumber - 1) % 3) + 1;
            var config = CreateInstance<NightConfig>();
            config.nightNumber = nightNumber;
            config.waveCount = Mathf.Clamp(1 + level + (nwl - 1), 2, 6);
            config.baseEnemyCount = 2 + level * 2 + (nwl - 1);
            config.healthMultiplier = 0.5f + level * 0.2f + (nwl - 1) * 0.08f;
            config.damageMultiplier = 0.4f + level * 0.2f + (nwl - 1) * 0.08f;
            float baseSpeedMultiplier = 0.75f + level * 0.08f + (nwl - 1) * 0.03f;
            if (level == 1)
            {
                // Keep Level 1 pressure readable for onboarding.
                baseSpeedMultiplier *= 0.88f;
            }
            config.speedMultiplier = baseSpeedMultiplier;
            config.spawnInterval = Mathf.Max(0.8f, 2.4f - level * 0.25f - (nwl - 1) * 0.1f);
            config.timeBetweenWaves = Mathf.Max(3f, 7f - level - (nwl - 1) * 0.5f);
            config.hasBoss = nightNumber >= 12;
            config.completionBonus = 50 + level * 50 + (nwl - 1) * 30;

            // Late-campaign tuning pass:
            // - Soften Level 4 damage pressure.
            if (level >= 4)
            {
                config.damageMultiplier *= 0.9f;
            }

            // - Reduce final night volume spike.
            if (level == 4 && nwl == 3)
            {
                config.waveCount = Mathf.Min(config.waveCount, 5);
                config.baseEnemyCount = Mathf.Min(config.baseEnemyCount, 10);
            }

            ApplyNarrativeDefaults(config, nightNumber, overwrite: true);
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
