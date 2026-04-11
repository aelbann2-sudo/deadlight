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
            if (RunModifierSystem.Instance != null)
            {
                return;
            }

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

            OnMutationApplied?.Invoke(activeMutation);
        }

        public void SetMutationFromEvent(string eventName)
        {
            activeMutation = eventName switch
            {
                "Thick Fog" => MutationType.ThickFog,
                "Full Moon" => MutationType.FullMoon,
                "Contamination" => MutationType.Contamination,
                "Reinforcements" => MutationType.Reinforcements,
                _ => MutationType.None
            };

            OnMutationApplied?.Invoke(activeMutation);
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
