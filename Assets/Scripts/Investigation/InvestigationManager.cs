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
        Investigating = 1,
        RootCauseReviewed = 2,
        ScorecardReady = 3
    }

    /// <summary>
    /// Coordinates the post-incident investigation phase and tallies all scores across
    /// the full training scenario to generate the official MSHA Performance Scorecard.
    /// </summary>
    public class InvestigationManager : MonoBehaviour
    {
        public static InvestigationManager Instance { get; private set; }

        private InvestigationState _state = InvestigationState.Inactive;
        private IncidentReport _currentReport;
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

        private int _investigationScore = 0;
        private bool _investigationCorrect = false;
        private string _identifiedCause = "Pending";

        // Events
        public static event Action<InvestigationState> OnInvestigationStateChanged;
        public static event Action<IncidentReport> OnInvestigationStarted;
        public static event Action<bool, int, string> OnRootCauseEvaluated; // isCorrect, deltaPoints, explanation
        public static event Action<ScoreBreakdown> OnScorecardReady;

        public InvestigationState State => _state;
        public IncidentReport CurrentReport => _currentReport;
        public ScoreBreakdown Breakdown => _breakdown;

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

            // Begin incident investigation with a brief 1.5s delay
            Invoke(nameof(StartInvestigation), 1.5f);
        }

        private void HandleHealthDepleted()
        {
            // Trainee incapacitated: Immediately finalize scorecard with Grade F
            Debug.LogWarning("[InvestigationManager] Trainee health fully depleted! Finalizing failed scorecard.");
            GenerateScorecard();
        }

        public void StartInvestigation()
        {
            _state = InvestigationState.Investigating;
            _currentReport = GenerateIncidentReport();

            Debug.Log($"[InvestigationManager] 🔍 Starting incident investigation on {_currentReport.equipmentName}!");
            OnInvestigationStateChanged?.Invoke(_state);
            OnInvestigationStarted?.Invoke(_currentReport);
        }

        private IncidentReport GenerateIncidentReport()
        {
            var report = new IncidentReport();

            // Find ignited equipment
            bool isElectrical = false;
            string equipName = "Industrial Machinery";

            if (FireManager.Instance != null)
            {
                var fires = FireManager.Instance.GetActiveFires();
                if (fires != null && fires.Count > 0)
                {
                    var f = fires[0];
                    if (f != null)
                    {
                        equipName = f.TargetName;
                        if (f.GetComponentInParent<ElectricalEquipment>() != null)
                        {
                            isElectrical = true;
                        }
                    }
                }
            }

            report.equipmentName = equipName;

            if (isElectrical)
            {
                report.visualClue = "Charred black arc-pitting on the 480V terminal bus-bar. Insulation jacket melted into pooled residue. Phase C circuit breaker tripped due to line-to-ground fault.";
                report.thermalReading = "Infrared Residual: 285°C localized to Phase C wiring lug.";
                report.actualCause = RootCauseType.ElectricalCableInsulationFailure;

                report.options = new List<RootCauseOption>
                {
                    new RootCauseOption
                    {
                        causeType = RootCauseType.ElectricalCableInsulationFailure,
                        title = "Degraded Cable Insulation & Phase Ground Fault",
                        description = "Vibration caused jacket abrasion against enclosure edge, resulting in high-energy electrical arcing.",
                        isCorrect = true,
                        explanation = "CORRECT! 30 CFR § 75.517 requires all electrical conductors to be properly insulated and guarded from mechanical abrasion. Arc fault was the direct ignition source."
                    },
                    new RootCauseOption
                    {
                        causeType = RootCauseType.ConveyorBearingFrictionDustIgnition,
                        title = "Idler Bearing Friction & Coal Dust Igniter",
                        description = "Mechanical bearing seized and friction heated adjacent combustible coal dust.",
                        isCorrect = false,
                        explanation = "INCORRECT: The equipment is an enclosed electrical unit. No mechanical roller bearings or heavy dust accumulation were present."
                    },
                    new RootCauseOption
                    {
                        causeType = RootCauseType.HydraulicHoseRuptureHotSurface,
                        title = "High-Pressure Hydraulic Fluid Spray",
                        description = "Pressurized hydraulic fluid atomized and sprayed onto an overheated exhaust manifold.",
                        isCorrect = false,
                        explanation = "INCORRECT: Electrical control enclosures do not contain pressurized hydraulic fluid circuits."
                    }
                };
            }
            else
            {
                // Machine / Conveyor / Drill
                report.visualClue = "Severely scored idler roller journal seized solid in pillow block. Underside of rubber belt has friction grooving and charred coal dust slurry encrustation.";
                report.thermalReading = "Infrared Residual: 410°C bearing housing heat soak.";
                report.actualCause = RootCauseType.ConveyorBearingFrictionDustIgnition;

                report.options = new List<RootCauseOption>
                {
                    new RootCauseOption
                    {
                        causeType = RootCauseType.ConveyorBearingFrictionDustIgnition,
                        title = "Seized Bearing Friction & Combustible Dust Build-up",
                        description = "Unlubricated roller bearing seized, friction heat exceeded coal dust ignition threshold (380°C).",
                        isCorrect = true,
                        explanation = "CORRECT! 30 CFR § 75.1100 requires inspection of belt conveyor rollers. A seized bearing can rapidly exceed 400°C, igniting float coal dust."
                    },
                    new RootCauseOption
                    {
                        causeType = RootCauseType.TransformerOilSurgeFlashover,
                        title = "Dielectric Oil Breakdown & Surge Arrester Puncture",
                        description = "Internal high-voltage transformer surge punctured dielectric cooling oil tank.",
                        isCorrect = false,
                        explanation = "INCORRECT: Conveyors and diesel machinery do not house dielectric liquid transformers."
                    },
                    new RootCauseOption
                    {
                        causeType = RootCauseType.VentilationFanMotorSeizure,
                        title = "Auxiliary Scrubber Fan Overheat",
                        description = "Axial ventilation fan blade struck housing, causing motor winding thermal run-away.",
                        isCorrect = false,
                        explanation = "INCORRECT: The seized mechanism was identified as a conveyor/machine roller journal, not an auxiliary scrubber fan."
                    }
                };
            }

            return report;
        }

        public void SubmitRootCause(RootCauseType chosenCause)
        {
            if (_currentReport == null) return;

            RootCauseOption chosenOption = null;
            foreach (var opt in _currentReport.options)
            {
                if (opt.causeType == chosenCause)
                {
                    chosenOption = opt;
                    break;
                }
            }

            bool isCorrect = (chosenCause == _currentReport.actualCause);
            int points = isCorrect ? 200 : 40;
            string explanation = chosenOption != null ? chosenOption.explanation : "Root cause evaluation submitted.";

            _investigationScore = points;
            _investigationCorrect = isCorrect;
            _identifiedCause = chosenOption != null ? chosenOption.title : chosenCause.ToString();
            _state = InvestigationState.RootCauseReviewed;

            Debug.Log($"[InvestigationManager] 📋 Root cause submitted: {chosenCause} (Correct: {isCorrect}, +{points} pts)");
            OnRootCauseEvaluated?.Invoke(isCorrect, points, explanation);
            OnInvestigationStateChanged?.Invoke(_state);
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
                        _investigationScore +
                        hpBonus;

            TraineeGrade grade;
            string gradeTitle;
            string summary;

            if (currentHp <= 0f)
            {
                grade = TraineeGrade.F_Disqualified;
                gradeTitle = "GRADE F — TRAINEE CASUALTY";
                summary = "Trainee was overcome by smoke inhalation, heat exhaustion, or electrocution. Mandatory safety retraining and oxygen rescue protocol review required.";
            }
            else if (total >= 850)
            {
                grade = TraineeGrade.A_Exemplary;
                gradeTitle = "GRADE A — EXEMPLARY MINE SAFETY OFFICER";
                summary = "Flawless incident management! Rapid detection, accurate MSHA emergency dispatch, optimal airflow control, swift evacuation, and precise root cause determination.";
            }
            else if (total >= 700)
            {
                grade = TraineeGrade.B_Qualified;
                gradeTitle = "GRADE B — QUALIFIED MINER";
                summary = "Competent response. Emergency protocols followed safely, minor delays or partial score reductions noted in decision analysis.";
            }
            else if (total >= 500)
            {
                grade = TraineeGrade.C_NeedsRetraining;
                gradeTitle = "GRADE C — NEEDS RETRAINING";
                summary = "Significant safety protocol violations or hazardous delays detected. Trainee sustained elevated smoke exposure or selected sub-optimal fire suppression agents.";
            }
            else
            {
                grade = TraineeGrade.F_Disqualified;
                gradeTitle = "GRADE F — SAFETY HAZARD / DISQUALIFIED";
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

            Debug.Log($"[InvestigationManager] 🏆 SCORECARD READY! Total Score: {total} | Grade: {gradeTitle}");
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
            _investigationScore = 0;
            _wasSuppressed = false;

            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
