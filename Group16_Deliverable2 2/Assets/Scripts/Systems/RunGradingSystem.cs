using System;
using UnityEngine;

namespace Deadlight.Systems
{
    [Serializable]
    public struct NightRunStats
    {
        public float accuracy;
        public float damageTaken;
        public float clearSpeedScore;
        public bool objectiveCompleted;
    }

    [Serializable]
    public struct NightGradeResult
    {
        public string grade;
        public float multiplier;
        public int bonusPoints;
    }

    public static class RunGradingSystem
    {
        public static NightGradeResult ComputeNightGrade(NightRunStats stats)
        {
            float score = 0f;
            score += Mathf.Clamp01(stats.accuracy) * 35f;
            score += (1f - Mathf.Clamp01(stats.damageTaken)) * 25f;
            score += Mathf.Clamp01(stats.clearSpeedScore) * 25f;
            score += stats.objectiveCompleted ? 15f : 0f;

            if (score >= 90f)
            {
                return new NightGradeResult { grade = "S", multiplier = 1.35f, bonusPoints = 120 };
            }

            if (score >= 75f)
            {
                return new NightGradeResult { grade = "A", multiplier = 1.2f, bonusPoints = 80 };
            }

            if (score >= 60f)
            {
                return new NightGradeResult { grade = "B", multiplier = 1.1f, bonusPoints = 45 };
            }

            if (score >= 45f)
            {
                return new NightGradeResult { grade = "C", multiplier = 1f, bonusPoints = 20 };
            }

            return new NightGradeResult { grade = "D", multiplier = 0.9f, bonusPoints = 0 };
        }
    }
}
