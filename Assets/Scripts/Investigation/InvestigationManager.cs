using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Environment;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Extinguisher;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Player;

namespace ARMiningSimulator.Investigation
{
    public enum InvestigationState
    {
        Inactive = 0,
        MachineSelection = 1,
        ScorecardReady = 5
    }

    /// <summary>
    /// Coordinates the post-incident investigation phase and tallies all scores across
    /// the full training scenario to generate the official MSHA Performance Scorecard.
    /// Fire-origin identification is a simple tap-on-the-machine interaction: the trainee taps
    /// machines directly in the 3D scene (see FireOriginTapDetector) until they tap the correct
    /// one, then the scorecard is generated. There is no forensic root-cause analysis stage.
    /// </summary>
    public class InvestigationManager : MonoBehaviour
    {
        public static InvestigationManager Instance { get; private set; }

        [Header("Fire-Origin Identification")]
        [Tooltip("Optional manual override for testing: if assigned, this machine is always the " +
                 "correct fire-origin answer instead of the machine dynamically resolved from the " +
                 "fire that actually ignited this run.")]
        [SerializeField] private GameObject _manualOriginOverride;

        private InvestigationState _state = InvestigationState.Inactive;
        private ScoreBreakdown _breakdown = new ScoreBreakdown();

        // Cached metrics from scenario execution
        private float _detectionTime = 0f;
        private int _detectionScore = 0;

        private int _dec1Score = 0;
        private string _dec1Choice = "None";

        private int _dec2Score = 0;
        private string _dec2Choice = "None";

        private float _evacTime = 0f;
        private int _evacBonus = 0;

        private int _dec3Score = 0;
        private string _extinguisherChoice = "None";
        private bool _wasSuppressed = false;

        private int _machineOriginScore = 0;
        private string _identifiedMachineName = "None";
        private string _machineFeedbackText = "";
        private bool _isMachineFeedbackCorrect = false;
        private float _machineFeedbackTime = -100f;
        private bool _isSmallFireFlow = false;

        private int _investigationScore = 0;
        private bool _investigationCorrect = false;
        private string _identifiedCause = "Pending";

        private GameObject _originMachine = null;
        private FireHazard _detectedHazard = null;
        private ParticleSystem _identificationSmoke = null;

        // Events
        public static event Action<InvestigationState> OnInvestigationStateChanged;
        public static event Action OnInvestigationStarted;
        public static event Action<bool, string> OnMachineSelectionEvaluated;
        public static event Action<ScoreBreakdown> OnScorecardReady;

        public InvestigationState State => _state;
        public ScoreBreakdown Breakdown => _breakdown;
        public string MachineFeedbackText => _machineFeedbackText;
        public bool IsMachineFeedbackCorrect => _isMachineFeedbackCorrect;
        public float MachineFeedbackAge => Time.time - _machineFeedbackTime;
        public bool IsSmallFireFlow => _isSmallFireFlow;

        /// <summary>
        /// The machine the trainee must tap to correctly identify the fire origin. Normally
        /// resolved dynamically each run from the fire that actually ignited (see
        /// StartMachineOriginInvestigation); _manualOriginOverride takes priority when assigned,
        /// for testing a specific machine in the Inspector without needing to trigger a real fire.
        /// </summary>
        public GameObject CorrectFireOriginMachine => _manualOriginOverride != null ? _manualOriginOverride : _originMachine;

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
            TraineeDetection.OnFireDetected += HandleFireDetected;
            DecisionManager.OnDecisionSubmitted += HandleDecisionSubmitted;
            EvacuationManager.OnSafeZoneReached += HandleSafeZoneReached;
            FireResponseManager.OnFireResponseCompleted += HandleFireResponseCompleted;
            ExtinguisherController.OnExtinguisherEquipped += HandleExtinguisherEquipped;
            ExtinguisherController.OnAllActiveFiresExtinguished += HandleAllFiresSuppressed;
            TraineeHealth.OnHealthDepleted += HandleHealthDepleted;
        }

        private void OnDisable()
        {
            TraineeDetection.OnFireDetected -= HandleFireDetected;
            DecisionManager.OnDecisionSubmitted -= HandleDecisionSubmitted;
            EvacuationManager.OnSafeZoneReached -= HandleSafeZoneReached;
            FireResponseManager.OnFireResponseCompleted -= HandleFireResponseCompleted;
            ExtinguisherController.OnExtinguisherEquipped -= HandleExtinguisherEquipped;
            ExtinguisherController.OnAllActiveFiresExtinguished -= HandleAllFiresSuppressed;
            TraineeHealth.OnHealthDepleted -= HandleHealthDepleted;
        }

        private void HandleFireDetected(FireHazard hazard, float detectionTime)
        {
            _detectionTime = detectionTime;
            if (hazard != null)
            {
                _detectedHazard = hazard;
                if (hazard.FireSource != null)
                {
                    _originMachine = hazard.FireSource;
                }
            }
            if (detectionTime <= 4.0f)
            {
                _detectionScore = 200;
            }
            else if (detectionTime <= 8.0f)
            {
                _detectionScore = 150;
            }
            else
            {
                _detectionScore = 100;
            }
            Debug.Log($"[InvestigationManager] Fire detected in {_detectionTime:F1}s (+{_detectionScore} pts)");
        }

        private void HandleDecisionSubmitted(DecisionRecord record)
        {
            if (record == null) return;

            if (record.stage == DecisionStage.Stage1_Alarm)
            {
                _dec1Choice = record.chosenText;
                _dec1Score = record.scoreEarned;
            }
            else if (record.stage == DecisionStage.Stage2_Ventilation)
            {
                _dec2Choice = record.chosenText;
                _dec2Score = record.scoreEarned;
            }
        }

        private void HandleSafeZoneReached(float duration, int speedBonus)
        {
            _evacTime = duration;
            _evacBonus = speedBonus;
        }

        private void HandleExtinguisherEquipped(ExtinguisherConfig config)
        {
            if (config != null)
            {
                _extinguisherChoice = config.displayName;
            }
        }

        private void HandleAllFiresSuppressed()
        {
            _wasSuppressed = true;
        }

        private void HandleFireResponseCompleted()
        {
            if (FireResponseManager.Instance != null)
            {
                _dec3Score = FireResponseManager.Instance.ResponseScore;
            }

            // Unequip the extinguisher now that fire is out
            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.Unequip();
            }

            // Record suppression bonus — fire was physically extinguished by the trainee
            _wasSuppressed = true;

            // Tag this as a small-fire flow so the investigation UI shows correct context
            _isSmallFireFlow = true;

            // Launch the fire-origin machine identification. The trainee must tap the correct
            // machine in the 3D scene before the scorecard is shown.
            Debug.Log("[InvestigationManager] 🔥 Fire extinguished! Launching fire-origin identification (small fire flow).");
            Invoke(nameof(StartMachineOriginInvestigation), 1.5f);
        }

        /// <summary>
        /// Auto-scores the "incident containment" scorecard line from whether the fire was
        /// actually suppressed — used both as the emergency fallback (see
        /// EndSimulationAndShowScorecard) and after a correct fire-origin tap (see
        /// SubmitMachineOriginTap), since there is no root-cause question to score it from anymore.
        /// </summary>
        private void AutoScoreIncidentContainment()
        {
            if (_investigationScore != 0) return;

            bool fireExtinguished = _wasSuppressed || (FireManager.Instance != null && !FireManager.Instance.HasActiveFires);
            _investigationScore = fireExtinguished ? 200 : 150;
            _investigationCorrect = true;
            _identifiedCause = fireExtinguished
                ? "Incipient Equipment Fire Successfully Extinguished at Source"
                : "Critical Inferno — Safe Area Evacuation Standard Maintained";
        }

        /// <summary>
        /// Fallback scorecard path used only when the trainee's health is depleted
        /// or when the big-fire safe-area evacuation path skips the investigation.
        /// </summary>
        public void EndSimulationAndShowScorecard()
        {
            CancelInvoke(nameof(EndSimulationAndShowScorecard));
            CancelInvoke(nameof(StartMachineOriginInvestigation));
            CancelInvoke(nameof(FinalizeAfterCorrectIdentification));

            AutoScoreIncidentContainment();

            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.Unequip();
            }

            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null)
            {
                ARMiningSimulator.Core.SimulationGameLoop.Instance.SetStage(ARMiningSimulator.Core.SimulationStage.Scorecard);
            }

            Debug.Log("[InvestigationManager] Emergency scorecard path - presenting final marks.");
            GenerateScorecard();
        }

        private void HandleHealthDepleted()
        {
            // Trainee incapacitated: Immediately finalize scorecard with Grade F
            Debug.LogWarning("[InvestigationManager] Trainee health fully depleted! Finalizing failed scorecard.");
            GenerateScorecard();
        }

        /// <summary>
        /// Shared investigation flow for both small and big fires. Resolves the machine that
        /// actually ignited this run, spawns burnt footprints leading to it as an environmental
        /// clue, and puts the manager into MachineSelection - waiting for the trainee to tap the
        /// correct machine directly in the 3D scene (see FireOriginTapDetector / SubmitMachineOriginTap).
        /// _isSmallFireFlow is set by the caller before invoking this method.
        /// </summary>
        public void StartMachineOriginInvestigation()
        {
            CancelInvoke(nameof(StartMachineOriginInvestigation));
            _state = InvestigationState.MachineSelection;
            _machineFeedbackText = "";

            // Locate origin machine
            GameObject originGo = _originMachine;
            if (originGo == null && _detectedHazard != null)
            {
                originGo = _detectedHazard.FireSource != null ? _detectedHazard.FireSource : _detectedHazard.gameObject;
            }
            if (originGo == null && FireManager.Instance != null)
            {
                var fires = FireManager.Instance.GetActiveFires();
                if (fires != null && fires.Count > 0 && fires[0] != null)
                {
                    originGo = fires[0].FireSource != null ? fires[0].FireSource : fires[0].gameObject;
                }
            }

            if (originGo != null)
            {
                var m = originGo.GetComponentInParent<MiningMachine>();
                if (m != null) originGo = m.gameObject;
                else
                {
                    var e = originGo.GetComponentInParent<ElectricalEquipment>();
                    if (e != null) originGo = e.gameObject;
                }
            }

            _originMachine = originGo;

            // Findability clue: the flames are already out by this point, so spawn a tall,
            // slow-rising smoke plume on the correct machine that's visible across the room —
            // otherwise the trainee has to hunt through every machine up close to find it.
            if (_identificationSmoke != null)
            {
                UnityEngine.Object.Destroy(_identificationSmoke.gameObject);
                _identificationSmoke = null;
            }
            if (CorrectFireOriginMachine != null)
            {
                _identificationSmoke = ProceduralFireParticleBuilder.BuildIdentificationSmoke(CorrectFireOriginMachine.transform);
            }

            Debug.Log($"[InvestigationManager] Starting Machine Origin Investigation! Origin machine is '{CorrectFireOriginMachine?.name}'. Waiting for trainee to tap the machine in the scene.");
            OnInvestigationStateChanged?.Invoke(_state);
            OnInvestigationStarted?.Invoke();
        }

        /// <summary>Stops and removes the identification-stage smoke clue once it's no longer needed.</summary>
        private void ClearIdentificationSmoke()
        {
            if (_identificationSmoke == null) return;
            _identificationSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            UnityEngine.Object.Destroy(_identificationSmoke.gameObject, 4f);
            _identificationSmoke = null;
        }

        /// <summary>
        /// Called by FireOriginTapDetector when the trainee taps a machine/equipment GameObject in
        /// the 3D scene during the MachineSelection stage. Compares it against CorrectFireOriginMachine
        /// (the machine that actually ignited this run, or the manual override if one is assigned).
        /// Wrong taps never fail the scenario - the trainee can simply try another machine.
        /// </summary>
        public void SubmitMachineOriginTap(GameObject tappedGo)
        {
            if (_state != InvestigationState.MachineSelection || tappedGo == null) return;

            bool isCorrect = CorrectFireOriginMachine != null && tappedGo == CorrectFireOriginMachine;
            _machineFeedbackTime = Time.time;

            if (isCorrect)
            {
                _machineOriginScore = 100;
                _isMachineFeedbackCorrect = true;
                _identifiedMachineName = CleanEquipmentName(tappedGo.name);
                _machineFeedbackText = "You're right!";

                Debug.Log($"[InvestigationManager] Correct fire-origin machine identified: {tappedGo.name}");
                ClearIdentificationSmoke();

                // Stay in MachineSelection briefly so the "You're right!" toast is visible, then
                // jump straight to the scorecard - there is no root-cause question anymore.
                Invoke(nameof(FinalizeAfterCorrectIdentification), 1.2f);
            }
            else
            {
                _isMachineFeedbackCorrect = false;
                _machineFeedbackText = "Try again.";
                Debug.Log($"[InvestigationManager] Wrong machine tapped: {tappedGo.name}");
            }

            OnMachineSelectionEvaluated?.Invoke(isCorrect, _machineFeedbackText);
        }

        private void FinalizeAfterCorrectIdentification()
        {
            AutoScoreIncidentContainment();
            GenerateScorecard();
        }

        private string CleanEquipmentName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return "Industrial Mining Unit";
            return rawName.Replace("Machine_", "")
                          .Replace("Electrical_", "")
                          .Replace("(Clone)", "")
                          .Replace("_", " ")
                          .Trim();
        }

        public void FinalizeAndDisplayScorecard()
        {
            GenerateScorecard();
        }

        private void GenerateScorecard()
        {
            float currentHp = TraineeHealth.Instance != null ? TraineeHealth.Instance.CurrentHealth : 100f;
            int hpBonus = Mathf.Max(0, Mathf.RoundToInt(currentHp));

            int total = _detectionScore +
                        _dec1Score +
                        _dec2Score +
                        _evacBonus +
                        _dec3Score +
                        (_wasSuppressed ? 150 : 0) +
                        _machineOriginScore +
                        _investigationScore +
                        hpBonus;

            TraineeGrade grade;
            string gradeTitle;
            string summary;

            if (currentHp <= 0f)
            {
                grade = TraineeGrade.F_Disqualified;
                gradeTitle = "GRADE F - TRAINEE CASUALTY";
                summary = "Trainee was overcome by smoke inhalation, heat exhaustion, or electrocution. Mandatory safety retraining and oxygen rescue protocol review required.";
            }
            else if (total >= 850)
            {
                grade = TraineeGrade.A_Exemplary;
                gradeTitle = "GRADE A - EXEMPLARY MINE SAFETY OFFICER";
                summary = "Flawless incident management! Rapid detection, accurate MSHA emergency dispatch, optimal airflow control, swift evacuation, and precise root cause determination.";
            }
            else if (total >= 700)
            {
                grade = TraineeGrade.B_Qualified;
                gradeTitle = "GRADE B - QUALIFIED MINER";
                summary = "Competent response. Emergency protocols followed safely, minor delays or partial score reductions noted in decision analysis.";
            }
            else if (total >= 500)
            {
                grade = TraineeGrade.C_NeedsRetraining;
                gradeTitle = "GRADE C - NEEDS RETRAINING";
                summary = "Significant safety protocol violations or hazardous delays detected. Trainee sustained elevated smoke exposure or selected sub-optimal fire suppression agents.";
            }
            else
            {
                grade = TraineeGrade.F_Disqualified;
                gradeTitle = "GRADE F - SAFETY HAZARD / DISQUALIFIED";
                summary = "Critical failure to adhere to underground mine fire safety standards. Dangerous actions posed immediate threat to mine workforce.";
            }

            _breakdown.detectionScore = _detectionScore;
            _breakdown.detectionTimeSeconds = _detectionTime;
            _breakdown.decision1Score = _dec1Score;
            _breakdown.decision1Choice = _dec1Choice;
            _breakdown.decision2Score = _dec2Score;
            _breakdown.decision2Choice = _dec2Choice;
            _breakdown.evacuationBonus = _evacBonus;
            _breakdown.evacuationTimeSeconds = _evacTime;
            _breakdown.decision3Score = _dec3Score;
            _breakdown.extinguisherChoice = _extinguisherChoice;
            _breakdown.suppressionBonus = _wasSuppressed ? 150 : 0;
            _breakdown.wasSuppressed = _wasSuppressed;
            _breakdown.machineOriginScore = _machineOriginScore;
            _breakdown.identifiedMachineName = _identifiedMachineName;
            _breakdown.investigationScore = _investigationScore;
            _breakdown.investigationCorrect = _investigationCorrect;
            _breakdown.identifiedCause = _identifiedCause;
            _breakdown.healthBonus = hpBonus;
            _breakdown.remainingHealth = currentHp;
            _breakdown.totalScore = total;
            _breakdown.grade = grade;
            _breakdown.gradeTitle = gradeTitle;
            _breakdown.overallSummary = summary;

            _state = InvestigationState.ScorecardReady;
            ClearIdentificationSmoke();

            Debug.Log($"[InvestigationManager] SCORECARD READY! Total Score: {total} | Grade: {gradeTitle}");
            OnInvestigationStateChanged?.Invoke(_state);
            OnScorecardReady?.Invoke(_breakdown);
        }

        public void RestartScenario()
        {
            _state = InvestigationState.Inactive;
            _detectionScore = 0;
            _dec1Score = 0;
            _dec2Score = 0;
            _evacBonus = 0;
            _dec3Score = 0;
            _machineOriginScore = 0;
            _investigationScore = 0;
            _wasSuppressed = false;

            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
