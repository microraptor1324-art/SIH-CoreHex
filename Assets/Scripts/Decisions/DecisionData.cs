using System;
using System.Collections.Generic;

namespace ARMiningSimulator.Decisions
{
    public enum DecisionStage
    {
        None = 0,
        Stage1_Alarm = 1,
        Stage2_Ventilation = 2,
        Stage3_FireResponse = 3, // For Phase 8 (Extinguisher selection)
        Completed = 4
    }

    [Serializable]
    public class DecisionOption
    {
        public string text;
        public bool isCorrect;
        public int scoreModifier;
        public string explanation;
        public float spreadRateMultiplier = 1.0f;
        public float smokeDamageMultiplier = 1.0f;

        public DecisionOption(string text, bool isCorrect, int score, string explanation, float spreadMult = 1.0f, float smokeMult = 1.0f)
        {
            this.text = text;
            this.isCorrect = isCorrect;
            this.scoreModifier = score;
            this.explanation = explanation;
            this.spreadRateMultiplier = spreadMult;
            this.smokeDamageMultiplier = smokeMult;
        }
    }

    [Serializable]
    public class DecisionQuestion
    {
        public DecisionStage stage;
        public string title;
        public string questionText;
        public float timeLimit = 20f;
        public List<DecisionOption> options = new List<DecisionOption>();

        public DecisionQuestion(DecisionStage stage, string title, string questionText, float timeLimit = 20f)
        {
            this.stage = stage;
            this.title = title;
            this.questionText = questionText;
            this.timeLimit = timeLimit;
        }
    }

    [Serializable]
    public class DecisionRecord
    {
        public DecisionStage stage;
        public string questionTitle;
        public string chosenText;
        public bool wasCorrect;
        public int scoreEarned;
        public float responseTime;
        public string feedbackGiven;
    }
}
