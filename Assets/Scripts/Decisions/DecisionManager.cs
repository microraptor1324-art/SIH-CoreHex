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
    /// - Decision 1: Immediate Action (Raise Alarm)
    /// - Decision 2: Ventilation Management (Direct Airflow Outbye)
    /// After Decision 2 is completed, evacuation begins towards the Safe Area,
    /// where Decision 3 (Fire Size Assessment & Tactical Action) takes place.
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
                "DECISION 1: FIRE SPOTTED",
                "You have confirmed a fire in the mine. What is your MANDATORY FIRST ACTION?",
                20f
            );
            _questionStage1.options.Add(new DecisionOption(
                "raise alarm",
                true,
                100,
                "CORRECT! Mining Safety Standard Rule #1: Always raise the evacuation alarm immediately to alert all underground personnel and surface control."
            ));
            _questionStage1.options.Add(new DecisionOption(
                "fight the fire",
                false,
                30,
                "INCORRECT / HAZARDOUS: Never attempt to fight an underground fire alone before raising the alarm. If you are overcome by heat or smoke, nobody knows you or your crew need rescue."
            ));
            _questionStage1.options.Add(new DecisionOption(
                "shut down the power",
                false,
                50,
                "PARTIAL: Isolating equipment power is important, but raising the mine-wide alarm must always be your mandatory first action before attempting local switches."
            ));

            // Stage 2: Mine Ventilation & Smoke Direction
            _questionStage2 = new DecisionQuestion(
                DecisionStage.Stage2_Ventilation,
                "DECISION 2: MINE VENTILATION & SMOKE DIRECTION",
                "Toxic combustion gases begin filling the tunnel drift. What is your required ventilation action?",
                20f
            );
            _questionStage2.options.Add(new DecisionOption(
                "Direct airflow outbye",
                true,
                100,
                "CORRECT (+100 PTS)! Mining Safety Standard (MSHA/ISO): Direct airflow outbye to exhaust toxic smoke and combustion gases away from escape routes, keeping designated evacuation walkways clear of toxic fumes.",
                0.85f,
                0.5f
            ));
            _questionStage2.options.Add(new DecisionOption(
                "Reverse airflow into working face",
                false,
                -30,
                "DANGEROUS PROTOCOL VIOLATION (-30 PTS)! Reversing airflow forces toxic combustion gases across personnel fleeing the working face and feeds fresh air into the fire seat.",
                1.5f,
                1.4f
            ));
            _questionStage2.options.Add(new DecisionOption(
                "Shut down all main ventilation fans immediately",
                false,
                50,
                "PARTIAL (+50 PTS): Halting main fans stops fresh oxygen from feeding the fire seat, but causes lethal carbon monoxide (CO) and smoke to rapidly stagnate in the drift. Directing airflow outbye is the superior safety standard.",
                1.1f,
                1.8f
            ));
        }

        private string _lastOrderStage1 = "";
        private string _lastFirstOptionStage1 = "";
        private string _lastOrderStage2 = "";
        private string _lastFirstOptionStage2 = "";

        private void ShuffleOptions(List<DecisionOption> options, ref string lastOrder, ref string lastFirstOption)
        {
            if (options == null || options.Count <= 1) return;

            for (int attempt = 0; attempt < 30; attempt++)
            {
                for (int i = options.Count - 1; i > 0; i--)
                {
                    int rand = UnityEngine.Random.Range(0, i + 1);
                    var temp = options[i];
                    options[i] = options[rand];
                    options[rand] = temp;
                }

                string currentOrder = string.Join("|", options.ConvertAll(o => o.text));
                if (currentOrder != lastOrder && options[0].text != lastFirstOption)
                    break;
                if (attempt > 20 && currentOrder != lastOrder)
                    break;
            }

            lastOrder = string.Join("|", options.ConvertAll(o => o.text));
            lastFirstOption = options[0].text;
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
            ShuffleOptions(_questionStage1.options, ref _lastOrderStage1, ref _lastFirstOptionStage1);
            _stageTimer = 0f;
            _isWaitingForSelection = true;
            _isShowingFeedback = false;
            _lastSelectedOption = null;

            Debug.Log($"[DecisionManager] 📋 Starting Decision Stage 1: Immediate Action (Raise Alarm).");
            OnQuestionPresented?.Invoke(_currentQuestion);
        }

        public void StartStage2()
        {
            _currentStage = DecisionStage.Stage2_Ventilation;
            _currentQuestion = _questionStage2;
            ShuffleOptions(_questionStage2.options, ref _lastOrderStage2, ref _lastFirstOptionStage2);
            _stageTimer = 0f;
            _isWaitingForSelection = true;
            _isShowingFeedback = false;
            _lastSelectedOption = null;

            Debug.Log($"[DecisionManager] 📋 Starting Decision Stage 2: Ventilation Management (Direct Airflow Outbye).");
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
                if (option.isCorrect || option.scoreModifier > 0)
                {
                    if (EmergencyAlarmBeacon.Instance != null)
                    {
                        EmergencyAlarmBeacon.Instance.ActivateAlarm();
                    }
                }
            }

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
                if (_lastSelectedOption != null && _lastSelectedOption.isCorrect)
                {
                    Debug.Log("[DecisionManager] 📋 Decision 1 Correct! Transitioning to Decision 2: Mine Ventilation & Smoke Direction.");
                    StartStage2();
                }
                else
                {
                    // Re-open question modal so trainee can select the mandatory life-saving action
                    ShuffleOptions(_questionStage1.options, ref _lastOrderStage1, ref _lastFirstOptionStage1);
                    _isWaitingForSelection = true;
                }
            }
            else if (_currentStage == DecisionStage.Stage2_Ventilation)
            {
                if (_lastSelectedOption != null && _lastSelectedOption.isCorrect)
                {
                    _currentStage = DecisionStage.Completed;
                    Debug.Log("[DecisionManager] 🏁 Decision 2 Correct! All decisions completed. Activating directions to Safety Area.");
                    OnDecisionsCompleted?.Invoke();
                }
                else
                {
                    // Re-open question modal so trainee can select the correct ventilation protocol (reshuffled)
                    ShuffleOptions(_questionStage2.options, ref _lastOrderStage2, ref _lastFirstOptionStage2);
                    _isWaitingForSelection = true;
                }
            }
            else
            {
                _currentStage = DecisionStage.Completed;
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
            _lastOrderStage1 = "";
            _lastFirstOptionStage1 = "";
            _lastOrderStage2 = "";
            _lastFirstOptionStage2 = "";

            if (EmergencyAlarmBeacon.Instance != null)
            {
                EmergencyAlarmBeacon.Instance.DeactivateAlarm();
            }
        }
    }
}
