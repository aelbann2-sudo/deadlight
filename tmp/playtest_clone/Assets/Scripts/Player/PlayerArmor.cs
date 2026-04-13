using UnityEngine;
using System;

namespace Deadlight.Player
{
    public enum ArmorTier { None, Level1, Level2, Level3 }

    public class PlayerArmor : MonoBehaviour
    {
        public static PlayerArmor Instance { get; private set; }

        private ArmorTier vestTier = ArmorTier.None;
        private ArmorTier helmetTier = ArmorTier.None;
        private float vestDurability;
        private float helmetDurability;

        private static readonly float[] VestMaxDurability = { 0f, 80f, 150f, 230f };
        private static readonly float[] VestDamageReduction = { 0f, 0.30f, 0.40f, 0.55f };
        private static readonly float[] HelmetMaxDurability = { 0f, 50f, 100f, 150f };
        private static readonly float[] HelmetDamageReduction = { 0f, 0.25f, 0.35f, 0.45f };

        public float VestDurability => vestDurability;
        public float HelmetDurability => helmetDurability;
        public ArmorTier VestTier => vestTier;
        public ArmorTier HelmetTier => helmetTier;
        public float VestMax => VestMaxDurability[(int)vestTier];
        public float HelmetMax => HelmetMaxDurability[(int)helmetTier];
        public bool HasVest => vestTier != ArmorTier.None && vestDurability > 0;
        public bool HasHelmet => helmetTier != ArmorTier.None && helmetDurability > 0;

        public event Action<float, float, float, float> OnArmorChanged;

        private AudioClip breakSound;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            try { breakSound = Audio.ProceduralAudioGenerator.GenerateExplosion(); }
            catch { }
        }

        public float AbsorbDamage(float incomingDamage)
        {
            float remaining = incomingDamage;

            if (vestTier != ArmorTier.None && vestDurability > 0)
            {
                float reduction = VestDamageReduction[(int)vestTier];
                float absorbed = remaining * reduction;
                float actualAbsorb = Mathf.Min(absorbed, vestDurability);
                vestDurability -= actualAbsorb;
                remaining -= actualAbsorb;

                if (vestDurability <= 0)
                {
                    vestDurability = 0;
                    vestTier = ArmorTier.None;
                    PlayBreakSound();
                    ShowBreakMessage("Vest destroyed!");
                }
            }

            if (helmetTier != ArmorTier.None && helmetDurability > 0)
            {
                float reduction = HelmetDamageReduction[(int)helmetTier];
                float absorbed = remaining * reduction;
                float actualAbsorb = Mathf.Min(absorbed, helmetDurability);
                helmetDurability -= actualAbsorb;
                remaining -= actualAbsorb;

                if (helmetDurability <= 0)
                {
                    helmetDurability = 0;
                    helmetTier = ArmorTier.None;
                    PlayBreakSound();
                    ShowBreakMessage("Helmet destroyed!");
                }
            }

            OnArmorChanged?.Invoke(vestDurability, VestMax, helmetDurability, HelmetMax);
            return Mathf.Max(0, remaining);
        }

        public void EquipVest(ArmorTier tier)
        {
            if (tier == ArmorTier.None) return;
            if (tier > vestTier || vestDurability <= 0)
            {
                vestTier = tier;
                vestDurability = VestMaxDurability[(int)tier];
                OnArmorChanged?.Invoke(vestDurability, VestMax, helmetDurability, HelmetMax);
            }
        }

        public void EquipHelmet(ArmorTier tier)
        {
            if (tier == ArmorTier.None) return;
            if (tier > helmetTier || helmetDurability <= 0)
            {
                helmetTier = tier;
                helmetDurability = HelmetMaxDurability[(int)tier];
                OnArmorChanged?.Invoke(vestDurability, VestMax, helmetDurability, HelmetMax);
            }
        }

        public void ResetArmor()
        {
            vestTier = ArmorTier.None;
            helmetTier = ArmorTier.None;
            vestDurability = 0;
            helmetDurability = 0;
            OnArmorChanged?.Invoke(0, 0, 0, 0);
        }

        private void PlayBreakSound()
        {
            if (breakSound != null)
                AudioSource.PlayClipAtPoint(breakSound, transform.position, 0.5f);
        }

        private void ShowBreakMessage(string msg)
        {
            if (Systems.FloatingTextManager.Instance != null)
                Systems.FloatingTextManager.Instance.SpawnText(msg, transform.position + Vector3.up * 0.5f, Color.red);
        }
    }
}
