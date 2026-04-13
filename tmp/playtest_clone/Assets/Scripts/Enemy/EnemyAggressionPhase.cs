using Deadlight.Core;

namespace Deadlight.Enemy
{
    internal enum EnemyAggressionPhase
    {
        Dormant,
        DayStalk,
        NightHunt
    }

    internal static class EnemyAggressionResolver
    {
        public static EnemyAggressionPhase Resolve(GameState? state, bool forceNightHunt = false)
        {
            if (forceNightHunt)
            {
                return EnemyAggressionPhase.NightHunt;
            }

            return state switch
            {
                GameState.DayPhase => EnemyAggressionPhase.DayStalk,
                GameState.NightPhase => EnemyAggressionPhase.NightHunt,
                _ => EnemyAggressionPhase.Dormant
            };
        }
    }
}