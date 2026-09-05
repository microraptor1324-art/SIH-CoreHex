using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Investigation
{
    public enum RootCauseType
    {
        ElectricalCableInsulationFailure = 0,
        ConveyorBearingFrictionDustIgnition = 1,
        HydraulicHoseRuptureHotSurface = 2,
        TransformerOilSurgeFlashover = 3,
        VentilationFanMotorSeizure = 4
    }

    [System.Serializable]
    public class RootCauseOption
    {
        public RootCauseType causeType;
        public string title;
        public string description;
        public bool isCorrect;
        public string explanation;
    }

    [System.Serializable]
    public class IncidentReport
    {
        public string equipmentName;
        public string visualClue;
        public string thermalReading;
        public RootCauseType actualCause;
        public List<RootCauseOption> options = new List<RootCauseOption>();
    }

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
