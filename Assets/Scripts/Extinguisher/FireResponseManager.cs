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
        Extinguished = 5
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

        // Each decision point's score is locked in on the FIRST submission attempt only — without
        // this, retrying after a wrong answer (the question re-opens automatically) would let
        // SubmitFireSizeAssessment/SubmitTacticalAction re-apply their penalty every single retry,
        // spiraling the score arbitrarily negative instead of the one-time "-25 PTS"/"-60 PTS" etc.
        // the feedback text actually claims.
        private bool _sizeEvalScored = false;
        private int _sizeEvalLockedScore = 0;
        private bool _actionChoiceScored = false;
        private int _actionChoiceLockedScore = 0;

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

            // Fire size was already decided once, randomly, at ignition (see FireManager) and
            // growth has been frozen ever since — just read the fixed severity here.
            _isActuallyBig = FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large;

            _state = FireResponseState.Stage3A_FireSizeEval;
            _isShowingFeedback = false;
            _sizeEvalScored = false;
            _sizeEvalLockedScore = 0;
            Debug.Log($"[FireResponseManager] 📋 Starting Decision 3 at Safe Area: Fire Size Assessment. (Fire is {(_isActuallyBig ? "BIG" : "SMALL")})");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitFireSizeAssessment(bool choseBig)
        {
            _isActuallyBig = FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large;
            _choseBig = choseBig;
            _isShowingFeedback = true;

            // Score is locked in on the first attempt only — retrying after a wrong answer never
            // re-applies the penalty or re-earns the bonus, it just lets the trainee try again.
            if (!_sizeEvalScored)
            {
                _sizeEvalLockedScore = (_choseBig == _isActuallyBig) ? 50 : -25;
                _responseScore += _sizeEvalLockedScore;
                _sizeEvalScored = true;
            }

            if (_choseBig == _isActuallyBig)
            {
                _isFeedbackCorrect = true;
                _lastFeedbackText = "CORRECT SIZE EVALUATION! (+50 PTS)\n" +
                    (_isActuallyBig
                        ? "You accurately evaluated that this hazard is a BIG FIRE. Roaring flames, rolling toxic smoke, and intense radiant heat require remaining in safety."
                        : "You accurately evaluated that this hazard is a SMALL FIRE. The fire is an incipient flame localized to a single equipment component and can be fought with an extinguisher.");
            }
            else
            {
                _isFeedbackCorrect = false;
                _lastFeedbackText = "INCORRECT SIZE ASSESSMENT (-25 PTS):\n" +
                    (_isActuallyBig
                        ? "This is actually a BIG FIRE with rolling smoke and intense radiant heat. Re-evaluate and select Big Fire!"
                        : "This is actually a SMALL (Incipient) FIRE localized to the equipment. Re-evaluate and select Small Fire!");
            }

            OnEvaluationFeedback?.Invoke(_lastFeedbackText, _isFeedbackCorrect, _sizeEvalLockedScore);
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
            _isActuallyBig = FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large;
            _isShowingFeedback = false;
            _state = FireResponseState.Stage3A_ActionChoice;
            _actionChoiceScored = false;
            _actionChoiceLockedScore = 0;
            Debug.Log($"[FireResponseManager] 📋 Starting Stage 3A Action Choice at Safe Area. Fire is {(_isActuallyBig ? "BIG" : "SMALL")}.");
            OnStateChanged?.Invoke(_state);
        }

        public void SubmitTacticalAction(bool chooseToFight)
        {
            _isActuallyBig = FireManager.Instance != null && FireManager.Instance.GetHighestSeverity() >= FireSeverity.Large;
            _isShowingFeedback = true;

            // Score is locked in on the first attempt only — retrying after a wrong answer never
            // re-applies the penalty or re-earns the bonus, it just lets the trainee try again.
            bool isCorrectChoice = _isActuallyBig ? !chooseToFight : chooseToFight;
            if (!_actionChoiceScored)
            {
                if (isCorrectChoice) _actionChoiceLockedScore = 100;
                else _actionChoiceLockedScore = _isActuallyBig ? -60 : -30;
                _responseScore += _actionChoiceLockedScore;
                _actionChoiceScored = true;
            }

            if (_isActuallyBig)
            {
                if (!chooseToFight)
                {
                    // Correct: Big fire -> Stay safe!
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = "CORRECT LIFE-SAFETY PROTOCOL! (+100 PTS)\nMSHA Mining Safety Standard: Always prioritize personal life safety. A Big underground fire CANNOT be fought with portable extinguishers. Stay safe in the refuge/evacuation zone and summon specialized Mine Rescue teams.\n\n🚨 AUTOMATED SUPPRESSION SYSTEM ACTIVATED: Mine deluge nozzles deployed and the fire has been safely extinguished.";
                    // NOTE: _state deliberately stays Stage3A_ActionChoice here — the UI's feedback
                    // modal only renders while state == Stage3A_ActionChoice && IsShowingFeedback.
                    // Setting it to EscalatedToRescue here (as this used to do) changed the state
                    // away from what the UI checks before the player ever saw the feedback modal,
                    // leaving the screen blank with no way to proceed. ProceedAfterActionFeedback()
                    // (called when the player taps the feedback modal's Proceed button) is the
                    // correct place to move the flow on to the investigation stage.

                    // Automatically extinguish the fire
                    if (FireManager.Instance != null)
                    {
                        FireManager.Instance.ExtinguishAllFires();
                    }

                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, _actionChoiceLockedScore);
                }
                else
                {
                    // Critical Error: Trying to fight a big fire
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "CRITICAL SAFETY VIOLATION (-60 PTS)!\nNever attempt to fight a Big or fully developed mine fire alone with portable canisters. Toxic carbon monoxide and thermal flashover will overpower personnel. Re-evaluate and choose 'Stay in safety'!";
                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, false, _actionChoiceLockedScore);
                }
            }
            else
            {
                // Small (incipient) fire
                if (chooseToFight)
                {
                    // Correct: Small fire -> Fight the fire!
                    _isFeedbackCorrect = true;
                    _lastFeedbackText = "CORRECT PROTOCOL! (+100 PTS)\nMSHA Mining Safety Standard: Incipient (Small) fires with an unblocked retreat path must be immediately attacked using the portable fire extinguisher stationed right here at the Safe Area before they spread.";
                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, true, _actionChoiceLockedScore);
                }
                else
                {
                    // Incorrect: Abandoning a fightable small fire
                    _isFeedbackCorrect = false;
                    _lastFeedbackText = "INCORRECT TACTICAL DECISION (-30 PTS):\nWhile personal safety is important, leaving a small incipient fire unattended underground allows it to rapidly escalate and trap working crews. You must take the fire extinguisher located at the Safe Area and fight the fire!";
                    OnEvaluationFeedback?.Invoke(_lastFeedbackText, false, _actionChoiceLockedScore);
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

        /// <summary>
        /// There is only one "pick the extinguisher" action now, no 3-way agent quiz — the correct
        /// agent for the burning target is auto-resolved and always awarded full points. Still
        /// teaches which agent matches which hazard type via the feedback text, just without
        /// requiring the trainee to guess among CO2/Dry Powder/Foam.
        /// </summary>
        public void PickFireExtinguisher()
        {
            ExtinguisherType chosenType = _isTargetElectrical ? ExtinguisherType.CO2
                : _isTargetMachinery ? ExtinguisherType.DryPowder
                : ExtinguisherType.WaterFoam;

            _selectedExtinguisher = chosenType;
            _isShowingFeedback = true;
            _isFeedbackCorrect = true;

            if (_isTargetElectrical)
            {
                _lastFeedbackText = "CORRECT AGENT! (+100 PTS)\nCarbon Dioxide (CO₂ - Black Band) is electrically non-conductive, non-corrosive, and leaves zero residue, making it the mandatory standard agent for High-Voltage Switchboards, Transformers, and Electrical Panels.";
            }
            else if (_isTargetMachinery)
            {
                _lastFeedbackText = "CORRECT AGENT! (+100 PTS)\nABC Dry Chemical Powder (Blue Band) creates a rapid smothering chemical barrier over Conveyor Belts, Continuous Miner, Scooptram (LHD), and Drilling machinery.";
            }
            else
            {
                _lastFeedbackText = "CORRECT AGENT! (+100 PTS)\nAFFF Foam / Water (Cream Band) penetrates deep-seated embers in Class A timber cribbing and coal pack solids.";
            }

            const int scoreDelta = 100;
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

            // The big-fire "stay in safety" path auto-extinguishes the fire as a side effect
            // (automated suppression system) via FireManager.ExtinguishAllFires() above, which
            // fires the same OnAllFiresExtinguished event as the small-fire manual-extinguish
            // path. Don't let this handler's state jump take over here — the big-fire path has
            // its own feedback modal and completion route (ProceedAfterActionFeedback ->
            // StartMachineOriginInvestigation). Letting this handler run instead skipped straight
            // to the "ALL FIRES SUPPRESSED" success modal and its scorecard button, ending the
            // simulation before the forensic investigation stage ever ran.
            if (_isActuallyBig && _state == FireResponseState.Stage3A_ActionChoice)
            {
                return;
            }

            _state = FireResponseState.Extinguished;
            // Note: the suppression completion bonus is scored separately by InvestigationManager
            // (via wasSuppressed) — not added here too, or the total would double-count it.

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
            _sizeEvalScored = false;
            _sizeEvalLockedScore = 0;
            _actionChoiceScored = false;
            _actionChoiceLockedScore = 0;

            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.Unequip();
            }
        }
    }
}
