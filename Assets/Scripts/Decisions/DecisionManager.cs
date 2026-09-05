using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Player;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.Decisions
{
    /// <summary>
    /// Coordinates the interactive decision tests for underground mining fire safety:
    /// - Stage 1: Immediate Action (Alarm & Surface Notification)
    /// - Stage 2: Ventilation Management (Airflow Direction & Smoke Extraction)
    /// Tracks scores, provides feedback based on MSHA/ISO mining safety standards,
    /// and applies physical simulation consequences (alarm beacon, spread speed, smoke rate).
    /// </summary>
    public class DecisionManager : MonoBehaviour
    {
        public static DecisionManager Instance { get; private set; }

        // State
        private DecisionStage _currentStage = DecisionStage.None;
        private DecisionQuestion _currentQuestion = null;
        private DecisionOption _lastSelectedOption = null;
        private bool _isWaitingForSelection = false;
        private bool _isShowingFeedback = false;
        private float _stageTimer = 0f;
        private int _totalScore = 0;

        private readonly List<DecisionRecord> _history = new List<DecisionRecord>();

        // Pre-configured questions
        private DecisionQuestion _questionStage1;
        private DecisionQuestion _questionStage2;

        // Events
        public static event Action<DecisionQuestion> OnQuestionPresented;
        public static event Action<DecisionRecord> OnDecisionSubmitted;
        public static event Action OnDecisionsCompleted;

        public DecisionStage CurrentStage => _currentStage;
        public DecisionQuestion CurrentQuestion => _currentQuestion;
        public DecisionOption LastSelectedOption => _lastSelectedOption;
        public bool IsWaitingForSelection => _isWaitingForSelection;
        public bool IsShowingFeedback => _isShowingFeedback;
        public float StageTimer => _stageTimer;
        public int TotalScore => _totalScore;
        public List<DecisionRecord> History => _history;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildQuestions();
        }

        private void OnEnable()
        {
            TraineeDetection.OnFireDetected += HandleFireDetected;
        }

        private void OnDisable()
        {
            TraineeDetection.OnFireDetected -= HandleFireDetected;
        }

        private void BuildQuestions()
        {
            // Stage 1: Immediate Action & Alarm
            _questionStage1 = new DecisionQuestion(
                DecisionStage.Stage1_Alarm,
                "EMERGENCY RESPONSE: IMMEDIATE ACTION",
                "You have confirmed a fire in the active mine shaft. What is your MANDATORY FIRST ACTION?",
                20f
            );
            _questionStage1.options.Add(new DecisionOption(
                "🚨 Sound Mine Evacuation Alarm & Alert Surface Control",
                true,
                100,
                "CORRECT! Mining Safety Standard Rule #1: Always activate the mine-wide evacuation alarm and notify surface control immediately to alert all underground personnel."
            ));
            _questionStage1.options.Add(new DecisionOption(
                "🧯 Attempt to fight the fire alone without notifying anyone",
                false,
                -50,
                "CRITICAL VIOLATION! Never attempt to extinguish an underground fire alone before raising the alarm. If you are overcome, nobody knows you or your crew need rescue.",
                1.35f,
                1.2f
            ));
            _questionStage1.options.Add(new DecisionOption(
                "🏃 Evacuate immediately without sounding the alarm",
                false,
                -40,
                "SERIOUS ERROR! Leaving without triggering the alarm leaves other underground miners unaware of toxic smoke filling the ventilation network.",
                1.1f,
                1.0f
            ));
            _questionStage1.options.Add(new DecisionOption(
                "⚡ Shut down equipment power isolator, then sound alarm",
                false,
                30,
                "PARTIAL: Isolating electrical energy is important, but raising the mine-wide alarm must always take first priority before attempting local switches."
            ));

            // Stage 2: Mine Ventilation Management
            _questionStage2 = new DecisionQuestion(
                DecisionStage.Stage2_Ventilation,
                "VENTILATION CONTROL: AIRFLOW & SMOKE DIRECTION",
                "Dense toxic smoke is rising. How should underground ventilation be configured?",
                20f
            );
            _questionStage2.options.Add(new DecisionOption(
                "💨 Direct airflow outbye: Exhaust smoke away from escape routes",
                true,
                100,
                "CORRECT! Proper mine ventilation keeps designated evacuation walkways in fresh air, preventing carbon monoxide poisoning and preserving visibility to the exit.",
                0.85f,
                0.5f
            ));
            _questionStage2.options.Add(new DecisionOption(
                "🛑 Shut down all main ventilation fans immediately",
                false,
                -60,
                "CRITICAL HAZARD! Halting airflow causes lethal carbon monoxide and heat to rapidly pool in the immediate chamber, drastically accelerating asphyxiation.",
                1.1f,
                2.2f
            ));
            _questionStage2.options.Add(new DecisionOption(
                "🔄 Reverse airflow into working face without authorization",
                false,
                -50,
                "DANGEROUS! Reversing airflow can blow smoke directly across fleeing personnel and introduce fresh oxygen that escalates the fire into an explosion.",
                1.5f,
                1.4f
            ));
        }

        private void HandleFireDetected(FireHazard hazard, float reactionTime)
        {
            // Trigger Stage 1 after a brief delay
            Invoke(nameof(StartStage1), 0.6f);
        }

        public void StartStage1()
        {
            _currentStage = DecisionStage.Stage1_Alarm;
            _currentQuestion = _questionStage1;
            _stageTimer = 0f;
            _isWaitingForSelection = true;
            _isShowingFeedback = false;
            _lastSelectedOption = null;

            Debug.Log("[DecisionManager] 📋 Starting Decision Stage 1: Immediate Action.");
            OnQuestionPresented?.Invoke(_currentQuestion);
        }

        public void StartStage2()
        {
            _currentStage = DecisionStage.Stage2_Ventilation;
            _currentQuestion = _questionStage2;
            _stageTimer = 0f;
            _isWaitingForSelection = true;
            _isShowingFeedback = false;
            _lastSelectedOption = null;

            Debug.Log("[DecisionManager] 📋 Starting Decision Stage 2: Ventilation Management.");
            OnQuestionPresented?.Invoke(_currentQuestion);
        }

        private void Update()
        {
            if (_isWaitingForSelection)
            {
                _stageTimer += Time.deltaTime;
            }
        }

        public void SubmitDecision(int optionIndex)
        {
            if (!_isWaitingForSelection || _currentQuestion == null) return;
            if (optionIndex < 0 || optionIndex >= _currentQuestion.options.Count) return;

            _lastSelectedOption = _currentQuestion.options[optionIndex];
            _isWaitingForSelection = false;
            _isShowingFeedback = true;

            int earnedScore = _lastSelectedOption.scoreModifier;
            // Quick-thinking bonus if answered within 8 seconds and correct
            if (_lastSelectedOption.isCorrect && _stageTimer <= 8.0f)
            {
                earnedScore += 25;
            }

            _totalScore += earnedScore;

            // Record history
            var record = new DecisionRecord
            {
                stage = _currentStage,
                questionTitle = _currentQuestion.title,
                chosenText = _lastSelectedOption.text,
                wasCorrect = _lastSelectedOption.isCorrect,
                scoreEarned = earnedScore,
                responseTime = _stageTimer,
                feedbackGiven = _lastSelectedOption.explanation
            };
            _history.Add(record);

            ApplyPhysicalConsequences(_currentStage, _lastSelectedOption);

            Debug.Log($"[DecisionManager] Decision recorded: {_lastSelectedOption.text} | Correct: {_lastSelectedOption.isCorrect} | Score: {earnedScore} | Total: {_totalScore}");
            OnDecisionSubmitted?.Invoke(record);
        }

        private void ApplyPhysicalConsequences(DecisionStage stage, DecisionOption option)
        {
            if (stage == DecisionStage.Stage1_Alarm)
            {
                // If alarm option selected, trigger emergency strobe
                if (option.isCorrect || option.scoreModifier > 0)
                {
                    if (EmergencyAlarmBeacon.Instance != null)
                    {
                        EmergencyAlarmBeacon.Instance.ActivateAlarm();
                    }
                }
            }

            // Modify spread rate and smoke damage
            if (FireSpreadSystem.Instance != null && option.spreadRateMultiplier != 1.0f)
            {
                FireSpreadSystem.Instance.TimeMultiplier = option.spreadRateMultiplier;
            }
        }

        public void ProceedAfterFeedback()
        {
            _isShowingFeedback = false;

            if (_currentStage == DecisionStage.Stage1_Alarm)
            {
                StartStage2();
            }
            else if (_currentStage == DecisionStage.Stage2_Ventilation)
            {
                _currentStage = DecisionStage.Completed;
                Debug.Log("[DecisionManager] 🏁 Decision Stages 1 & 2 completed! Ready for evacuation.");
                OnDecisionsCompleted?.Invoke();
            }
        }

        public void ResetDecisions()
        {
            _currentStage = DecisionStage.None;
            _currentQuestion = null;
            _lastSelectedOption = null;
            _isWaitingForSelection = false;
            _isShowingFeedback = false;
            _stageTimer = 0f;
            _totalScore = 0;
            _history.Clear();

            if (EmergencyAlarmBeacon.Instance != null)
            {
                EmergencyAlarmBeacon.Instance.DeactivateAlarm();
            }
        }
    }
}
