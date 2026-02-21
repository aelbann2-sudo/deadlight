using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deadlight.Core
{
    public enum RunModifierType
    {
        FastFragile,
        TankySlow,
        ScarceAmmoHighDrops,
        BloodMoon,
        HordeNight,
        Scavenger,
        GlassCannon,
        Marathon
    }

    [Serializable]
    public class RunModifier
    {
        public RunModifierType type;
        public string title;
        public string description;
        public float enemyHealthMultiplier = 1f;
        public float enemySpeedMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;
        public float ammoDropMultiplier = 1f;
    }

    public class RunModifierSystem : MonoBehaviour
    {
        public static RunModifierSystem Instance { get; private set; }

        [Header("Runtime")]
        [SerializeField] private List<RunModifier> activeModifiers = new List<RunModifier>();
        [SerializeField] private string activeWorldEvent = "";

        public IReadOnlyList<RunModifier> ActiveModifiers => activeModifiers;
        public string ActiveWorldEvent => activeWorldEvent;

        public event Action<IReadOnlyList<RunModifier>> OnModifiersGenerated;
        public event Action<string> OnWorldEventChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void GenerateRunModifiers(int seed)
        {
            activeModifiers.Clear();
            var rng = new System.Random(seed);

            int typeCount = System.Enum.GetValues(typeof(RunModifierType)).Length;
            RunModifierType type = (RunModifierType)rng.Next(0, typeCount);
            activeModifiers.Add(CreateModifier(type));
            OnModifiersGenerated?.Invoke(activeModifiers);
        }

        public IReadOnlyList<RunModifier> GetActiveModifiers()
        {
            return activeModifiers;
        }

        public void ApplyToEnemy(GameObject enemy)
        {
            if (enemy == null || activeModifiers.Count == 0)
            {
                return;
            }

            float health = 1f;
            float speed = 1f;
            float damage = 1f;
            foreach (var modifier in activeModifiers)
            {
                health *= modifier.enemyHealthMultiplier;
                speed *= modifier.enemySpeedMultiplier;
                damage *= modifier.enemyDamageMultiplier;
            }

            var enemyHealth = enemy.GetComponent<Enemy.EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.ApplyHealthMultiplier(health);
            }

            var enemyAI = enemy.GetComponent<Enemy.EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.ApplySpeedMultiplier(speed);
                enemyAI.ApplyDamageMultiplier(damage);
            }

            var simpleAI = enemy.GetComponent<Enemy.SimpleEnemyAI>();
            if (simpleAI != null)
            {
                simpleAI.ApplySpeedMultiplier(speed);
                simpleAI.ApplyDamageMultiplier(damage);
            }
        }

        public void RollNightEvent(int night, int seed)
        {
            var events = new[]
            {
                "Fog Front",
                "Blackout",
                "Supply Drop",
                "Roaming Elite"
            };

            var rng = new System.Random(seed + night * 389);
            activeWorldEvent = events[rng.Next(0, events.Length)];
            OnWorldEventChanged?.Invoke(activeWorldEvent);

            if (activeWorldEvent == "Supply Drop")
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    var shooting = player.GetComponent<Deadlight.Player.PlayerShooting>();
                    shooting?.AddAmmo(25 + night * 5);
                }
            }

            var cam = Camera.main;
            if (cam != null)
            {
                if (activeWorldEvent == "Blackout")
                {
                    cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f, 1f);
                }
                else if (activeWorldEvent == "Fog Front")
                {
                    cam.backgroundColor = new Color(0.18f, 0.2f, 0.22f, 1f);
                }
            }
        }

        public float GetAmmoDropMultiplier()
        {
            float multiplier = 1f;
            foreach (var modifier in activeModifiers)
            {
                multiplier *= Mathf.Max(0.2f, modifier.ammoDropMultiplier);
            }

            return multiplier;
        }

        private static RunModifier CreateModifier(RunModifierType type)
        {
            switch (type)
            {
                case RunModifierType.FastFragile:
                    return new RunModifier
                    {
                        type = type,
                        title = "Sprint Plague",
                        description = "Zombies move faster but are fragile.",
                        enemyHealthMultiplier = 0.8f,
                        enemySpeedMultiplier = 1.25f,
                        enemyDamageMultiplier = 1f,
                        ammoDropMultiplier = 1f
                    };
                case RunModifierType.TankySlow:
                    return new RunModifier
                    {
                        type = type,
                        title = "Heavy Rot",
                        description = "Zombies are tougher but slower.",
                        enemyHealthMultiplier = 1.35f,
                        enemySpeedMultiplier = 0.82f,
                        enemyDamageMultiplier = 1.1f,
                        ammoDropMultiplier = 1f
                    };
                case RunModifierType.ScarceAmmoHighDrops:
                    return new RunModifier
                    {
                        type = type,
                        title = "Supply Drought",
                        description = "Ammo is scarce, but enemy drops are richer.",
                        enemyHealthMultiplier = 1f,
                        enemySpeedMultiplier = 1f,
                        enemyDamageMultiplier = 1f,
                        ammoDropMultiplier = 1.35f
                    };
                case RunModifierType.BloodMoon:
                    return new RunModifier
                    {
                        type = type,
                        title = "Blood Moon",
                        description = "All enemies are always aggressive. +25% points.",
                        enemyHealthMultiplier = 1f,
                        enemySpeedMultiplier = 1.1f,
                        enemyDamageMultiplier = 1.1f,
                        ammoDropMultiplier = 1f
                    };
                case RunModifierType.HordeNight:
                    return new RunModifier
                    {
                        type = type,
                        title = "Horde Night",
                        description = "2x enemies, but each has half health.",
                        enemyHealthMultiplier = 0.5f,
                        enemySpeedMultiplier = 1f,
                        enemyDamageMultiplier = 0.8f,
                        ammoDropMultiplier = 1.2f
                    };
                case RunModifierType.Scavenger:
                    return new RunModifier
                    {
                        type = type,
                        title = "Scavenger",
                        description = "50% more drops, 25% less ammo capacity.",
                        enemyHealthMultiplier = 1f,
                        enemySpeedMultiplier = 1f,
                        enemyDamageMultiplier = 1f,
                        ammoDropMultiplier = 1.5f
                    };
                case RunModifierType.GlassCannon:
                    return new RunModifier
                    {
                        type = type,
                        title = "Glass Cannon",
                        description = "Deal 50% more damage, take 50% more.",
                        enemyHealthMultiplier = 0.67f,
                        enemySpeedMultiplier = 1f,
                        enemyDamageMultiplier = 1.5f,
                        ammoDropMultiplier = 1f
                    };
                case RunModifierType.Marathon:
                    return new RunModifier
                    {
                        type = type,
                        title = "Marathon",
                        description = "Nights are 50% longer. More waves.",
                        enemyHealthMultiplier = 1f,
                        enemySpeedMultiplier = 1f,
                        enemyDamageMultiplier = 1f,
                        ammoDropMultiplier = 1.1f
                    };
                default:
                    return new RunModifier
                    {
                        type = type,
                        title = "Standard",
                        description = "No modifiers.",
                        enemyHealthMultiplier = 1f,
                        enemySpeedMultiplier = 1f,
                        enemyDamageMultiplier = 1f,
                        ammoDropMultiplier = 1f
                    };
            }
        }
    }
}
