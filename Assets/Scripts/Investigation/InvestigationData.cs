using System;
using UnityEngine;

namespace ARMiningSimulator.Investigation
{
    public enum TraineeGrade
    {
        A_Exemplary = 0,
        B_Qualified = 1,
        C_NeedsRetraining = 2,
        F_Disqualified = 3
    }

    [System.Serializable]
    public class ScoreBreakdown
    {
        public int detectionScore;
        public float detectionTimeSeconds;

        public int decision1Score;
        public string decision1Choice;

        public int decision2Score;
        public string decision2Choice;

        public int evacuationBonus;
        public float evacuationTimeSeconds;

        public int decision3Score;
        public string extinguisherChoice;

        public int suppressionBonus;
        public bool wasSuppressed;

        public int machineOriginScore;
        public string identifiedMachineName;

        public int investigationScore;
        public bool investigationCorrect;
        public string identifiedCause;

        public int healthBonus;
        public float remainingHealth;

        public int totalScore;
        public TraineeGrade grade;
        public string gradeTitle;
        public string overallSummary;
    }
}
