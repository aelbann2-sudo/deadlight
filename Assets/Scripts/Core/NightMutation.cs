using UnityEngine;

namespace Deadlight.Core
{
    public enum MutationType { None, ThickFog, FullMoon, Contamination, Reinforcements }

    public class NightMutation : MonoBehaviour
    {
        public static NightMutation Instance { get; private set; }

        private MutationType activeMutation = MutationType.None;
        public MutationType ActiveMutation => activeMutation;

        public System.Action<MutationType> OnMutationApplied;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RollMutation(int night)
        {
            if (night <= 1)
            {
                activeMutation = MutationType.None;
                return;
            }

            float roll = Random.value;
            activeMutation = roll switch
            {
                < 0.25f => MutationType.ThickFog,
                < 0.5f => MutationType.FullMoon,
                < 0.75f => MutationType.Contamination,
                _ => MutationType.Reinforcements
            };

            ApplyMutation();
            OnMutationApplied?.Invoke(activeMutation);
        }

        void ApplyMutation()
        {
            var cam = Camera.main;
            switch (activeMutation)
            {
                case MutationType.ThickFog:
                    if (cam != null)
                        cam.backgroundColor = new Color(0.2f, 0.22f, 0.24f);
                    if (RadioTransmissions.Instance != null)
                        RadioTransmissions.Instance.ShowMessage("RADIO: Heavy fog rolling in. Watch your corners.", 3f);
                    break;

                case MutationType.FullMoon:
                    if (cam != null)
                        cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f);
                    if (RadioTransmissions.Instance != null)
                        RadioTransmissions.Instance.ShowMessage("RADIO: Full moon tonight. They'll be more active.", 3f);
                    break;

                case MutationType.Contamination:
                    if (RadioTransmissions.Instance != null)
                        RadioTransmissions.Instance.ShowMessage("RADIO: Toxin levels rising. Dead ones are leaving pools.", 3f);
                    break;

                case MutationType.Reinforcements:
                    if (RadioTransmissions.Instance != null)
                        RadioTransmissions.Instance.ShowMessage("RADIO: Seismic activity detected. Expect a larger horde.", 3f);
                    break;
            }
        }

        public float GetSpeedMultiplier()
        {
            return activeMutation == MutationType.FullMoon ? 1.2f : 1f;
        }

        public bool ShouldLeavePool()
        {
            return activeMutation == MutationType.Contamination;
        }

        public float GetWaveCountMultiplier()
        {
            return activeMutation == MutationType.Reinforcements ? 1.5f : 1f;
        }

        public void ClearMutation()
        {
            activeMutation = MutationType.None;
            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = new Color(0.12f, 0.14f, 0.1f);
        }
    }
}
