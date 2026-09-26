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
        Stage3A_ActionChoice = 2,
        Stage3B_AgentSelection = 3,
        PASS_Discharge = 4,
        Extinguished = 5,
        EscalatedToRescue = 6
    }

    /// <summary>
    /// Coordinates Decision Stage 3: Fight the fire.
    /// After reaching a safe distance:
    /// 1. Fire Size Assessment: Trainee evaluates whether the fire is Big or Small.
    /// 2. Tactical Action Choice:
    ///    - If Big: Fight the fire vs Stay safe (Correct: Stay safe!).
    ///    - If Small: Fight the fire vs Stay safe (Correct: Fight the fire!).
    /// 3. Extinguisher Agent Selection:
    ///    - CO2 for Electrical (Switchboards, Transformers, Panels)
    ///    - ABC Dry Chemical Powder for Heavy Machinery (Conveyors, Miner, Scooptram, Drill)
    ///    - AFFF Foam/Water for Class A solids only
    /// 4. P.A.S.S. interactive discharge sequence.
    /// </summary>
    public class FireResponseManager : MonoBehaviour
    {
        public static FireResponseManager Instance { get; private set; }

        private FireResponseState _state = FireResponseState.Inactive;
        private ExtinguisherType _selectedExtinguisher = ExtinguisherType.CO2;
        private int _responseScore = 0;
        private string _lastFeedbackText = "";
        private bool _isShowingFeedback = false;
        private bool _isFeedbackCorrect = false;

        private bool _isActuallyBig = false;
        private bool _choseBig = false;
        private string _burningTargetName = "Mining Equipment";
        private bool _isTargetElectrical = false;
        private bool _isTargetMachinery = false;

        // Events
        public static event Action<FireResponseState> OnStateChanged;
        public static event Action<string, bool, int> OnEvaluationFeedback; // text, isCorrect, score
        public static event Action OnFireResponseCompleted;

        public FireResponseState State => _state;
        public int ResponseScore => _responseScore;
        public string LastFeedbackText => _lastFeedbackText;
        public bool IsShowingFeedback => _isShowingFeedback;
        public bool IsFeedbackCorrect => _isFeedbackCorrect;
        public bool IsActuallyBig => _isActuallyBig;
        public string BurningTargetName => _burningTargetName;
        public bool IsTargetElectrical => _isTargetElectrical;
        public bool IsTargetMachinery => _isTargetMachinery;
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
            FireManager.OnAllFiresExtinguished += HandleAllFiresExtinguished;
        }

        private void OnDisable()
        {
            EvacuationManager.OnSafeZoneReached -= HandleSafeZoneReached;
            ExtinguisherController.OnAllActiveFiresExtinguished -= HandleAllFiresExtinguished;
            FireManager.OnAllFiresExtinguished -= HandleAllFiresExtinguished;
        }

        private void HandleSafeZoneReached(float duration, int bonus)
        {
            // Begin Safe Area Decision 3: Fire Size Assessment after safe arrival
            Invoke(nameof(StartStage3A), 1.0f);
        }

        public void StartStage3A()
        {
            CancelInvoke(nameof(StartStage3A));
            float growthLimit = FireManager.Instance != null ? FireManager.Instance.FireGrowthTime : 60.0f;
            _isActuallyBig = (FireManager.Instance != null && FireManager.Instance.ScenarioTimer >= growthLimit) ||
                             (FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large);

            _state = FireResponseState.Stage3A_FireSizeEval;
            _isShowingFeedback = false;
            float timer = FireManager.Instance != null ? FireManager.Instance.ScenarioTimer : 0f;
            Debug.Log($"[FireResponseManager] 📋 Starting Decision 3 at Safe Area: Fire Size Assessment. (Elapsed: {timer:F1}s / {growthLimit:F0}s limit, Fire is {(_isActuallyBig ? "BIG" : "SMALL")})");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitFireSizeAssessment(bool choseBig)
        {
            float growthLimit = FireManager.Instance != null ? FireManager.Instance.FireGrowthTime : 60.0f;
            _isActuallyBig = (FireManager.Instance != null && FireManager.Instance.ScenarioTimer >= growthLimit) ||
                             (FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large);
            _choseBig = choseBig;
            _isShowingFeedback = true;

            float elapsed = FireManager.Instance != null ? FireManager.Instance.ScenarioTimer : 0f;

            if (_choseBig == _isActuallyBig)
            {
                _responseScore += 50;
                _isFeedbackCorrect = true;
                _lastFeedbackText = "CORRECT SIZE EVALUATION! (+50 PTS)\n" +
                    (_isActuallyBig
                        ? $"You accurately evaluated that this hazard is a BIG FIRE ({elapsed:F1}s elapsed > {growthLimit:F0}s). Roaring flames, rolling toxic smoke, and intense radiant heat require remaining in safety."
                        : $"You accurately evaluated that this hazard is a SMALL FIRE ({elapsed:F1}s elapsed < {growthLimit:F0}s). The fire is an incipient flame localized to a single equipment component and can be fought with an extinguisher.");
            }
            else
            {
                _responseScore -= 25;
                _isFeedbackCorrect = false;
                _lastFeedbackText = "INCORRECT SIZE ASSESSMENT (-25 PTS):\n" +
                    (_isActuallyBig
                        ? $"More than {growthLimit:F0} seconds have elapsed ({elapsed:F1}s)! The fire has grown into a BIG FIRE with rolling smoke and intense radiant heat. Re-evaluate and select Big Fire!"
                        : $"The fire ignited less than {growthLimit:F0} seconds ago ({elapsed:F1}s) and is still a SMALL (Incipient) FIRE localized to the equipment. Re-evaluate and select Small Fire!");
            }

            OnEvaluationFeedback?.Invoke(_lastFeedbackText, _isFeedbackCorrect, _isFeedbackCorrect ? 50 : -25);
        }

        public void ProceedAfterSizeFeedback()
        {
            _isShowingFeedback = false;
            if (_isFeedbackCorrect)
            {
                ProceedToActionChoice();
            }
            else
            {
                // Re-open size evaluation modal so trainee must choose the correct option according to fire size
                _state = FireResponseState.Stage3A_FireSizeEval;
                OnStateChanged?.Invoke(_state);
            }
        }

        public void ProceedToActionChoice()
        {
            float growthLimit = FireManager.Instance != null ? FireManager.Instance.FireGrowthTime : 60.0f;
            _isActuallyBig = (FireManager.Instance != null && FireManager.Instance.ScenarioTimer >= growthLimit) ||
                             (FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large);
            _isShowingFeedback = false;
            _state = FireResponseState.Stage3A_ActionChoice;
            Debug.Log($"[FireResponseManager] 📋 Starting Stage 3A Action Choice at Safe Area. Fire is {(_isActuallyBig ? "BIG" : "SMALL")}.");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitTacticalAction(bool chooseToFight)
        {
            float growthLimit = FireManager.Instance != null ? FireManager.Instance.FireGrowthTime : 60.0f;
            _isActuallyBig = (FireManager.Instance != null && FireManager.Instance.ScenarioTimer >= growthLimit) ||
                             (FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large);
            _isShowingFeedback = true;

            if (_isActuallyBig)
            {
                if (!chooseToFight)
                {
                    // Correct: Big fire -> Stay safe!
                    _responseScore += 100;
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = $"CORRECT LIFE-SAFETY PROTOCOL! (+100 PTS)\nMSHA Mining Safety Standard: Always prioritize personal life safety. A Big underground fire (>{growthLimit:F0}s) CANNOT be fought with portable extinguishers. Stay safe in the refuge/evacuation zone and summon specialized Mine Rescue teams.\n\n🚨 AUTOMATED SUPPRESSION SYSTEM ACTIVATED: Mine deluge nozzles deployed and the fire has been safely extinguished.";
                    _state = FireResponseState.EscalatedToRescue;

                    // Automatically extinguish the fire
                    if (FireManager.Instance != null)
                    {
                        FireManager.Instance.ExtinguishAllFires();
                    }

                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, 100);
                }
                else
                {
                    // Critical Error: Trying to fight a big fire
                    _responseScore -= 60;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "CRITICAL SAFETY VIOLATION (-60 PTS)!\nNever attempt to fight a Big or fully developed mine fire alone with portable canisters. Toxic carbon monoxide and thermal flashover will overpower personnel. Re-evaluate and choose 'Stay in safety'!";
                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, false, -60);
                }
            }
            else
            {
                // Small fire (< 60s)
                if (chooseToFight)
                {
                    // Correct: Small fire -> Fight the fire!
                    _responseScore += 100;
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = $"CORRECT PROTOCOL! (+100 PTS)\nMSHA Mining Safety Standard: Incipient (Small) fires (<{growthLimit:F0}s) with an unblocked retreat path must be immediately attacked using the portable fire extinguisher stationed right here at the Safe Area before they spread.";
                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, 100);
                }
                else
                {
                    // Incorrect: Abandoning a fightable small fire
                    _responseScore -= 30;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = $"INCORRECT TACTICAL DECISION (-30 PTS):\nWhile personal safety is important, leaving a small incipient fire (<{growthLimit:F0}s) unattended underground allows it to rapidly escalate and trap working crews. You must take the fire extinguisher located at the Safe Area and fight the fire!";
                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, false, -30);
                }
            }
        }

        public void ProceedAfterActionFeedback()
        {
            _isShowingFeedback = false;

            if (_isActuallyBig)
            {
                if (_isFeedbackCorrect)
                {
                    // Stay in safety confirmed & big fire extinguished -> proceed to Machine Origin & Cause Investigation
                    Debug.Log("[FireResponseManager] 🛡️ Big fire extinguished automatically. Transitioning to Machine Origin Forensic Investigation!");
                    _state = FireResponseState.Inactive;
                    if (ARMiningSimulator.Investigation.InvestigationManager.Instance != null)
                    {
                        ARMiningSimulator.Investigation.InvestigationManager.Instance.StartMachineOriginInvestigation();
                    }
                    OnFireResponseCompleted?.Invoke();
                }
                else
                {
                    // Re-open ActionChoice so trainee MUST choose Stay in Safety
                    _state = FireResponseState.Stage3A_ActionChoice;
                    OnStateChanged?.Invoke(_state);
                }
            }
            else
            {
                if (_isFeedbackCorrect)
                {
                    // Proceed to pick extinguisher agent and equip extinguisher from station
                    ProceedToAgentSelection();
                }
                else
                {
                    // Re-open ActionChoice so trainee MUST choose Fight the Fire
                    _state = FireResponseState.Stage3A_ActionChoice;
                    OnStateChanged?.Invoke(_state);
                }
            }
        }

        public void ProceedToAgentSelection()
        {
            _isShowingFeedback = false;
            _state = FireResponseState.Stage3B_AgentSelection;

            // Resolve target equipment
            _isTargetElectrical = false;
            _isTargetMachinery = false;
            _burningTargetName = "Mining Equipment";

            if (FireManager.Instance != null)
            {
                var fires = FireManager.Instance.GetActiveFires();
                foreach (var f in fires)
                {
                    if (f == null || f.State != FireState.Burning) continue;
                    GameObject target = f.FireSource != null ? f.FireSource : f.gameObject;
                    _burningTargetName = target.name.Replace("Machine_", "").Replace("Electrical_", "").Replace("(Clone)", "").Trim();

                    if (target.GetComponentInParent<ElectricalEquipment>() != null)
                        _isTargetElectrical = true;
                    else if (target.GetComponentInParent<MiningMachine>() != null)
                        _isTargetMachinery = true;
                }
            }

            Debug.Log($"[FireResponseManager] 📋 Decision Stage 3B: Extinguisher Agent Selection. Burning target: '{_burningTargetName}' (Electrical: {_isTargetElectrical}, Machinery: {_isTargetMachinery})");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitExtinguisherSelection(ExtinguisherType chosenType)
        {
            _selectedExtinguisher = chosenType;
            _isShowingFeedback = true;

            int scoreDelta = 0;
            _isFeedbackCorrect = false;

            if (_isTargetElectrical)
            {
                if (chosenType == ExtinguisherType.CO2)
                {
                    scoreDelta = 100;
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = "CORRECT AGENT! (+100 PTS)\nCarbon Dioxide (CO₂ - Black Band) is electrically non-conductive, non-corrosive, and leaves zero residue, making it the mandatory standard agent for High-Voltage Switchboards, Transformers, and Electrical Panels.";
                }
                else if (chosenType == ExtinguisherType.DryPowder)
                {
                    scoreDelta = 50;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "ACCEPTABLE BUT SUB-OPTIMAL (+50 PTS):\nABC Dry Chemical Powder is non-conductive, but leaves corrosive chemical residue that permanently ruins electrical contacts. CO₂ is the preferred clean agent for switchgear.";
                }
                else
                {
                    scoreDelta = -80;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "CRITICAL FATAL BREACH (-80 PTS)!\nWater and foam are electrically conductive. Spraying liquid onto energized high-voltage equipment carries an extreme risk of lethal electrocution!";
                }
            }
            else if (_isTargetMachinery)
            {
                if (chosenType == ExtinguisherType.DryPowder)
                {
                    scoreDelta = 100;
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = "CORRECT AGENT! (+100 PTS)\nABC Dry Chemical Powder (Blue Band) creates a rapid smothering chemical barrier over Conveyor Belts, Continuous Miner, Scooptram (LHD), and Drilling machinery.";
                }
                else if (chosenType == ExtinguisherType.CO2)
                {
                    scoreDelta = 40;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "INEFFECTIVE (+40 PTS):\nCO₂ gas rapidly disperses in the ventilated mine drift without cooling hot metal roller bearings or preventing rubber conveyor belt re-ignition. Dry chemical powder is required.";
                }
                else
                {
                    scoreDelta = -40;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "INCORRECT AGENT (-40 PTS):\nAFFF Foam / Water is designed for Class A solid fuels and is ineffective against pressurized hydraulic fuel sprays or enclosed machine gearboxes.";
                }
            }
            else
            {
                // Class A timber/solids
                if (chosenType == ExtinguisherType.WaterFoam)
                {
                    scoreDelta = 100;
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = "CORRECT AGENT! (+100 PTS)\nAFFF Foam / Water (Cream Band) penetrates deep-seated embers in Class A timber cribbing and coal pack solids.";
                }
                else
                {
                    scoreDelta = 40;
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "PARTIAL: Water/Foam is required to penetrate Class A deep-seated timber embers.";
                }
            }

            _responseScore += scoreDelta;
            OnEvaluationFeedback?.Invoke(_lastFeedbackText, _isFeedbackCorrect, scoreDelta);
        }

        public void ProceedAfterAgentFeedback()
        {
            _isShowingFeedback = false;

            if (_isFeedbackCorrect)
            {
                ProceedToDischarge();
            }
            else
            {
                // Re-open agent selection so trainee can pick the right agent
                _state = FireResponseState.Stage3B_AgentSelection;
                OnStateChanged?.Invoke(_state);
            }
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

            // Mark the world extinguisher stationed near the safe area as taken
            if (WorldFireExtinguisher.Instance != null)
            {
                WorldFireExtinguisher.Instance.TakeExtinguisher();
            }

            // Guide trainee with green chevrons from Safe Area back to burning machine/tool
            GuideTraineeBackToFire();

            Debug.Log("[FireResponseManager] 🧯 Equipped extinguisher from Safe Area station! Commencing P.A.S.S. discharge.");
            OnStateChanged?.Invoke(_state);
        }

        public void GuideTraineeBackToFire()
        {
            if (FireManager.Instance == null) return;
            var fires = FireManager.Instance.GetActiveFires();
            if (fires == null || fires.Count == 0) return;

            FireHazard target = null;
            foreach (var f in fires)
            {
                if (f != null && f.State == FireState.Burning)
                {
                    target = f;
                    break;
                }
            }

            if (target != null && EvacuationManager.Instance != null)
            {
                var visualizer = EvacuationManager.Instance.GetComponent<EvacuationPathVisualizer>();
                if (visualizer != null)
                {
                    Vector3 startPos = Camera.main != null ? Camera.main.transform.position : EvacuationManager.Instance.SafeZonePosition;
                    visualizer.ShowDirectPath(startPos, target.transform.position);
                }
            }
        }

        private void HandleAllFiresExtinguished()
        {
            if (_state == FireResponseState.Extinguished) return;

            _state = FireResponseState.Extinguished;
            _responseScore += 150; // Suppression completion bonus

            if (EvacuationManager.Instance != null)
            {
                var visualizer = EvacuationManager.Instance.GetComponent<EvacuationPathVisualizer>();
                if (visualizer != null) visualizer.HidePath();
            }

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
