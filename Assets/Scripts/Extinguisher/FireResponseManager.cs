using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.Extinguisher
{
    public enum FireResponseState
    {
        Inactive = 0,
        Stage3A_FireSizeEval = 1,
        Stage3B_AgentSelection = 2,
        PASS_Discharge = 3,
        Extinguished = 4,
        EscalatedToRescue = 5
    }

    /// <summary>
    /// Coordinates Decision Stage 3 (Fire Size Assessment & Extinguisher Selection)
    /// and the subsequent P.A.S.S. interactive discharge sequence.
    /// </summary>
    public class FireResponseManager : MonoBehaviour
    {
        public static FireResponseManager Instance { get; private set; }

        private FireResponseState _state = FireResponseState.Inactive;
        private ExtinguisherType _selectedExtinguisher = ExtinguisherType.CO2;
        private int _responseScore = 0;
        private string _lastFeedbackText = "";
        private bool _isShowingFeedback = false;

        // Events
        public static event Action<FireResponseState> OnStateChanged;
        public static event Action<string, bool, int> OnEvaluationFeedback; // text, isCorrect, score
        public static event Action OnFireResponseCompleted;

        public FireResponseState State => _state;
        public int ResponseScore => _responseScore;
        public string LastFeedbackText => _lastFeedbackText;
        public bool IsShowingFeedback => _isShowingFeedback;
        public ExtinguisherType SelectedExtinguisher => _selectedExtinguisher;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EvacuationManager.OnSafeZoneReached += HandleSafeZoneReached;
            ExtinguisherController.OnAllActiveFiresExtinguished += HandleAllFiresExtinguished;
        }

        private void OnDisable()
        {
            EvacuationManager.OnSafeZoneReached -= HandleSafeZoneReached;
            ExtinguisherController.OnAllActiveFiresExtinguished -= HandleAllFiresExtinguished;
        }

        private void HandleSafeZoneReached(float duration, int bonus)
        {
            // Begin Fire Response Stage 3 after safe arrival
            Invoke(nameof(StartStage3A), 1.0f);
        }

        public void StartStage3A()
        {
            _state = FireResponseState.Stage3A_FireSizeEval;
            _isShowingFeedback = false;
            Debug.Log("[FireResponseManager] 📋 Starting Decision Stage 3A: Fire Size Assessment.");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitFireSizeAssessment(bool chooseToFight)
        {
            FireSeverity highest = FireManager.Instance != null ? FireManager.Instance.GetHighestSeverity() : FireSeverity.Small;
            bool isActuallySmall = (highest == FireSeverity.Small);

            _isShowingFeedback = true;

            if (isActuallySmall && chooseToFight)
            {
                // Correct: Small incipient fire can be attacked safely from open route
                _responseScore += 100;
                _lastFeedbackText = "CORRECT! Mining Safety Standard Rule: Incipient (Small) fires with an unblocked escapeway may be attacked using portable extinguishers before spreading.";
                OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, 100);
            }
            else if (!isActuallySmall && !chooseToFight)
            {
                // Correct: Fire has grown too large for portable extinguishers
                _responseScore += 120;
                _lastFeedbackText = "CORRECT DECISION! The fire has escalated beyond incipient stage. Portable extinguishers are ineffective against large industrial machinery blazes. Mine rescue protocols initiated.";
                _state = FireResponseState.EscalatedToRescue;
                OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, 120);
                OnFireResponseCompleted?.Invoke();
            }
            else if (!isActuallySmall && chooseToFight)
            {
                // Dangerous error: Trying to fight a large fire with a 5kg extinguisher
                _responseScore -= 60;
                _lastFeedbackText = "CRITICAL SAFETY VIOLATION! Never attempt to suppress a Medium or Large underground fire with portable canisters. Toxic smoke and flashover risk will overcome personnel.";
                OnEvaluationFeedback?.Invoke(_lastFeedbackText, false, -60);
            }
            else
            {
                // Erred on side of caution (acceptable)
                _responseScore += 40;
                _lastFeedbackText = "CAUTIOUS: While the fire was still small enough to suppress, prioritizing safety over property is an acceptable mining practice.";
                _state = FireResponseState.EscalatedToRescue;
                OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, 40);
                OnFireResponseCompleted?.Invoke();
            }
        }

        public void ProceedToAgentSelection()
        {
            _isShowingFeedback = false;
            _state = FireResponseState.Stage3B_AgentSelection;
            Debug.Log("[FireResponseManager] 📋 Starting Decision Stage 3B: Extinguisher Agent Selection.");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitExtinguisherSelection(ExtinguisherType chosenType)
        {
            _selectedExtinguisher = chosenType;
            _isShowingFeedback = true;

            bool isElectricalBurning = false;
            if (FireManager.Instance != null)
            {
                var fires = FireManager.Instance.GetActiveFires();
                foreach (var f in fires)
                {
                    if (f != null && f.GetComponentInParent<ElectricalEquipment>() != null)
                    {
                        isElectricalBurning = true;
                        break;
                    }
                }
            }

            int scoreDelta = 0;
            bool isCorrect = false;

            if (isElectricalBurning)
            {
                if (chosenType == ExtinguisherType.CO2)
                {
                    scoreDelta = 100;
                    isCorrect = true;
                    _lastFeedbackText = "PERFECT CHOICE! Carbon Dioxide (CO2 - Black Band) is clean, non-conductive, and leaves zero residue, making it the ideal agent for electrical equipment.";
                }
                else if (chosenType == ExtinguisherType.DryPowder)
                {
                    scoreDelta = 80;
                    isCorrect = true;
                    _lastFeedbackText = "EFFECTIVE CHOICE: ABC Dry Chemical Powder (Blue Band) is non-conductive and suppresses electrical fires safely, though it leaves corrosive chemical residue.";
                }
                else
                {
                    scoreDelta = -80;
                    isCorrect = false;
                    _lastFeedbackText = "FATAL HAZARD! Water/Foam (Cream Band) is electrically conductive. Spraying liquid onto energized high-voltage panels carries an extreme risk of lethal electrocution!";
                }
            }
            else
            {
                // Machine / oil fire
                if (chosenType == ExtinguisherType.DryPowder || chosenType == ExtinguisherType.WaterFoam)
                {
                    scoreDelta = 100;
                    isCorrect = true;
                    _lastFeedbackText = "CORRECT AGENT! Foam or Dry Powder creates a smothering blanket over burning hydrocarbon lubricants and mechanical diesel fuels.";
                }
                else
                {
                    scoreDelta = 70;
                    isCorrect = true;
                    _lastFeedbackText = "ACCEPTABLE: CO2 will displace oxygen, but hydrocarbon fuel may re-ignite once the gas dissipates without a cooling smothering blanket.";
                }
            }

            _responseScore += scoreDelta;
            OnEvaluationFeedback?.Invoke(_lastFeedbackText, isCorrect, scoreDelta);
        }

        public void ProceedToDischarge()
        {
            _isShowingFeedback = false;
            _state = FireResponseState.PASS_Discharge;

            // Equip the extinguisher viewmodel
            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.EquipExtinguisher(_selectedExtinguisher);
            }

            Debug.Log("[FireResponseManager] 🧯 Equipped extinguisher! Commencing P.A.S.S. discharge.");
            OnStateChanged?.Invoke(_state);
        }

        private void HandleAllFiresExtinguished()
        {
            _state = FireResponseState.Extinguished;
            _responseScore += 150; // Suppression completion bonus
            Debug.Log($"[FireResponseManager] 🎉 FIRE RESPONSE COMPLETE! Total Response Score: {_responseScore}");
            OnStateChanged?.Invoke(_state);
            OnFireResponseCompleted?.Invoke();
        }

        public void ResetResponse()
        {
            _state = FireResponseState.Inactive;
            _responseScore = 0;
            _isShowingFeedback = false;

            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.Unequip();
            }
        }
    }
}
