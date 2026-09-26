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
        MachineInspectionModal = 2,
        Investigating = 3,
        RootCauseReviewed = 4,
        ScorecardReady = 5
    }

    /// <summary>
    /// Coordinates the post-incident investigation phase and tallies all scores across
    /// the full training scenario to generate the official MSHA Performance Scorecard.
    /// Supports machine origin inspection with floor burnt footprint trails and forensic root cause analysis.
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

        private int _machineOriginScore = 0;
        private string _identifiedMachineName = "None";
        private CandidateMachineData _activeInspectedMachine = null;
        private string _machineFeedbackText = "";
        private bool _isMachineFeedbackCorrect = false;
        private bool _isSmallFireFlow = false;

        private int _investigationScore = 0;
        private bool _investigationCorrect = false;
        private string _identifiedCause = "Pending";

        private GameObject _originMachine = null;
        private FireHazard _detectedHazard = null;

        // Events
        public static event Action<InvestigationState> OnInvestigationStateChanged;
        public static event Action<IncidentReport> OnInvestigationStarted;
        public static event Action<CandidateMachineData> OnMachineInspected;
        public static event Action<bool, string> OnMachineSelectionEvaluated;
        public static event Action<bool, int, string> OnRootCauseEvaluated; // isCorrect, deltaPoints, explanation
        public static event Action<ScoreBreakdown> OnScorecardReady;

        public InvestigationState State => _state;
        public IncidentReport CurrentReport => _currentReport;
        public ScoreBreakdown Breakdown => _breakdown;
        public CandidateMachineData ActiveInspectedMachine => _activeInspectedMachine;
        public string MachineFeedbackText => _machineFeedbackText;
        public bool IsMachineFeedbackCorrect => _isMachineFeedbackCorrect;
        public bool IsSmallFireFlow => _isSmallFireFlow;

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

            // Launch the forensic machine-origin investigation.
            // The trainee must follow the scorched footprint trail, inspect candidates, and
            // identify which machine caused the fire before the scorecard is shown.
            Debug.Log("[InvestigationManager] 🔥 Fire extinguished! Launching forensic origin investigation (small fire flow).");
            Invoke(nameof(StartMachineOriginInvestigation), 1.5f);
        }

        /// <summary>
        /// Fallback scorecard path used only when the trainee's health is depleted
        /// or when the big-fire safe-area evacuation path skips the investigation.
        /// </summary>
        public void EndSimulationAndShowScorecard()
        {
            CancelInvoke(nameof(StartInvestigation));
            CancelInvoke(nameof(EndSimulationAndShowScorecard));
            CancelInvoke(nameof(StartMachineOriginInvestigation));

            if (_investigationScore == 0)
            {
                bool fireExtinguished = _wasSuppressed || (FireManager.Instance != null && !FireManager.Instance.HasActiveFires);
                _investigationScore = fireExtinguished ? 200 : 150;
                _investigationCorrect = true;
                _identifiedCause = fireExtinguished
                    ? "Incipient Equipment Fire Successfully Extinguished at Source"
                    : "Critical Inferno — Safe Area Evacuation Standard Maintained";
            }

            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.Unequip();
            }

            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null)
            {
                ARMiningSimulator.Core.SimulationGameLoop.Instance.SetStage(ARMiningSimulator.Core.SimulationStage.Scorecard);
            }

            Debug.Log("[InvestigationManager] 🏁 Emergency scorecard path — presenting final marks.");
            GenerateScorecard();
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

        /// <summary>
        /// Shared investigation flow for both small and big fires.
        /// Spawns burnt footprints on the floor leading to the origin machine,
        /// creates candidate machine inspection list, and prompts trainee to
        /// inspect and identify the origin machine before showing the scorecard.
        /// _isSmallFireFlow is set by the caller before invoking this method.
        /// </summary>
        public void StartMachineOriginInvestigation()
        {
            CancelInvoke(nameof(StartMachineOriginInvestigation));
            _state = InvestigationState.MachineSelection;
            _currentReport = GenerateIncidentReport();

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

            // Spawn the visual burnt footprints leading from corridor to the origin machine!
            if (originGo != null)
            {
                Vector3 corridorStart = (EvacuationManager.Instance != null)
                    ? EvacuationManager.Instance.SafeZonePosition
                    : (Camera.main != null ? Camera.main.transform.position : originGo.transform.position + Vector3.back * 3f);
                FireFootprintTrailBuilder.SpawnTrail(originGo, corridorStart);
            }

            PopulateCandidateMachines(originGo);

            Debug.Log($"[InvestigationManager] 🔍 Starting Machine Origin Investigation! Origin machine is '{originGo?.name}'. Burnt footprints spawned.");
            OnInvestigationStateChanged?.Invoke(_state);
            OnInvestigationStarted?.Invoke(_currentReport);
        }

        private void PopulateCandidateMachines(GameObject originGo)
        {
            if (_currentReport == null) return;
            _currentReport.candidateMachines.Clear();

            string originCleanName = originGo != null ? CleanEquipmentName(originGo.name) : "Belt Conveyor";

            // 1. Add the real origin machine with charred clues
            var originCandidate = new CandidateMachineData
            {
                displayName = originCleanName,
                targetGameObject = originGo,
                isOrigin = true,
                inspectionClueText = $"👣 BURNT FOOTPRINTS DETECTED!\n{_currentReport.visualClue}",
                thermalTelemetryText = _currentReport.thermalReading
            };
            _currentReport.candidateMachines.Add(originCandidate);

            // 2. Pool other machines and electrical equipment in the scene as unburned decoys
            List<GameObject> allSceneEquipment = new List<GameObject>();
            var machines = FindObjectsByType<MiningMachine>(FindObjectsSortMode.None);
            for (int i = 0; i < machines.Length; i++)
            {
                if (machines[i] != null && machines[i].gameObject != originGo && !allSceneEquipment.Contains(machines[i].gameObject))
                    allSceneEquipment.Add(machines[i].gameObject);
            }
            var electrical = FindObjectsByType<ElectricalEquipment>(FindObjectsSortMode.None);
            for (int i = 0; i < electrical.Length; i++)
            {
                if (electrical[i] != null && electrical[i].gameObject != originGo && !allSceneEquipment.Contains(electrical[i].gameObject))
                    allSceneEquipment.Add(electrical[i].gameObject);
            }

            // Pick up to 3 decoy clean equipment units
            for (int i = 0; i < allSceneEquipment.Count && _currentReport.candidateMachines.Count < 4; i++)
            {
                var decoyGo = allSceneEquipment[i];
                string cleanName = CleanEquipmentName(decoyGo.name);
                if (_currentReport.candidateMachines.Exists(c => c.displayName == cleanName)) continue;

                var decoyCandidate = new CandidateMachineData
                {
                    displayName = cleanName,
                    targetGameObject = decoyGo,
                    isOrigin = false,
                    inspectionClueText = "Clean casing. Bearings intact, wiring harness sealed with zero thermal discoloration. Zero soot footprints.",
                    thermalTelemetryText = "Infrared Thermography: 24°C (Normal Ambient Drift Temperature)."
                };
                _currentReport.candidateMachines.Add(decoyCandidate);
            }

            // Shuffle so origin is not always at the first index
            for (int i = 0; i < _currentReport.candidateMachines.Count; i++)
            {
                int rand = UnityEngine.Random.Range(i, _currentReport.candidateMachines.Count);
                var temp = _currentReport.candidateMachines[i];
                _currentReport.candidateMachines[i] = _currentReport.candidateMachines[rand];
                _currentReport.candidateMachines[rand] = temp;
            }
        }

        public void InspectCandidateMachine(CandidateMachineData candidate)
        {
            if (candidate == null) return;
            candidate.hasBeenInspected = true;
            _activeInspectedMachine = candidate;
            _state = InvestigationState.MachineInspectionModal;
            OnInvestigationStateChanged?.Invoke(_state);
            OnMachineInspected?.Invoke(candidate);
        }

        public void CloseInspectionModal()
        {
            _activeInspectedMachine = null;
            _state = InvestigationState.MachineSelection;
            OnInvestigationStateChanged?.Invoke(_state);
        }

        public void SubmitMachineOrigin(CandidateMachineData candidate)
        {
            if (candidate == null || _currentReport == null) return;

            bool isCorrect = candidate.isOrigin;
            _currentReport.selectedMachine = candidate;
            _identifiedMachineName = candidate.displayName;

            if (isCorrect)
            {
                _machineOriginScore = 100;
                _isMachineFeedbackCorrect = true;
                _machineFeedbackText = $"CORRECT MACHINE IDENTIFIED! (+100 PTS)\n" +
                    $"Burnt footprints along the floor and severe thermal scorch marks confirm the {candidate.displayName} as the ignition point.\n\n" +
                    $"Now determine what specific failure mechanism triggered the fire:";

                // Advance to RootCauseQuestion to answer what caused the fire on that machine!
                _state = InvestigationState.Investigating;
                OnInvestigationStateChanged?.Invoke(_state);
            }
            else
            {
                _isMachineFeedbackCorrect = false;
                _machineFeedbackText = $"INCORRECT MACHINE (-20 PTS):\n" +
                    $"Inspection of the {candidate.displayName} revealed zero burn footprints, intact wiring, and normal ambient temperature.\n" +
                    $"Re-follow the floor footprint trail leading to the machine with heavy soot deposits!";
                _state = InvestigationState.MachineSelection;
                OnInvestigationStateChanged?.Invoke(_state);
            }

            OnMachineSelectionEvaluated?.Invoke(isCorrect, _machineFeedbackText);
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

        private IncidentReport GenerateIncidentReport()
        {
            var report = new IncidentReport();

            FireHazard primaryHazard = null;
            string equipName = "Industrial Mining Equipment";

            if (FireManager.Instance != null)
            {
                var fires = FireManager.Instance.GetActiveFires();
                if (fires != null && fires.Count > 0)
                {
                    primaryHazard = fires[0];
                    if (primaryHazard != null)
                    {
                        equipName = primaryHazard.TargetName;
                    }
                }
            }

            report.equipmentName = equipName;

            FireHazardType hType = primaryHazard != null ? primaryHazard.HazardType : FireHazardType.StandardEquipment;
            string clue = primaryHazard != null && !string.IsNullOrEmpty(primaryHazard.VisualClue)
                ? primaryHazard.VisualClue
                : "Charred components with evidence of high thermal stress.";
            string thermal = primaryHazard != null && !string.IsNullOrEmpty(primaryHazard.ThermalReading)
                ? primaryHazard.ThermalReading
                : "Infrared Residual: 320°C localized hotspot.";

            report.visualClue = clue;
            report.thermalReading = thermal;

            switch (hType)
            {
                case FireHazardType.ElectricalArc:
                    if (equipName.ToLower().Contains("transformer"))
                    {
                        report.actualCause = RootCauseType.TransformerOilSurgeFlashover;
                        report.options = new List<RootCauseOption>
                        {
                            new RootCauseOption
                            {
                                causeType = RootCauseType.TransformerOilSurgeFlashover,
                                title = "Dielectric Oil Breakdown & Bushing Flashover",
                                description = "Severe electrical surge compromised dielectric oil insulation, venting boiling liquid onto hot core components.",
                                isCorrect = true,
                                explanation = "CORRECT! 30 CFR § 75.807 requires regular testing of transformer dielectric fluid. Oil degradation caused insulation puncture and catastrophic flashover."
                            },
                            new RootCauseOption
                            {
                                causeType = RootCauseType.ConveyorBearingFrictionDustIgnition,
                                title = "Idler Bearing Friction & Coal Dust Igniter",
                                description = "Mechanical roller bearing seized and friction heated adjacent combustible coal dust.",
                                isCorrect = false,
                                explanation = "INCORRECT: High-voltage substation transformers do not contain mechanical conveyor roller bearings."
                            },
                            new RootCauseOption
                            {
                                causeType = RootCauseType.HydraulicHoseRuptureHotSurface,
                                title = "High-Pressure Hydraulic Line Rupture",
                                description = "Pressurized hydraulic fluid atomized onto an overheated engine exhaust manifold.",
                                isCorrect = false,
                                explanation = "INCORRECT: Substation transformers are electrical assets without high-pressure hydraulic circuits."
                            }
                        };
                    }
                    else
                    {
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
                                explanation = "INCORRECT: Electrical enclosures do not contain pressurized hydraulic fluid circuits."
                            }
                        };
                    }
                    break;

                case FireHazardType.MechanicalFriction:
                    if (equipName.ToLower().Contains("motor"))
                    {
                        report.actualCause = RootCauseType.MotorWindingOverloadFailure;
                        report.options = new List<RootCauseOption>
                        {
                            new RootCauseOption
                            {
                                causeType = RootCauseType.MotorWindingOverloadFailure,
                                title = "Motor Stator Overload & Winding Insulation Thermal Breakdown",
                                description = "Mechanical overload caused continuous overcurrent, decomposing enamel insulation and igniting motor winding lacquer.",
                                isCorrect = true,
                                explanation = "CORRECT! 30 CFR § 75.814 requires continuous overload protection. Excessive thermal stress degraded the winding insulation to the point of self-ignition."
                            },
                            new RootCauseOption
                            {
                                causeType = RootCauseType.TransformerOilSurgeFlashover,
                                title = "Dielectric Oil Surge Flashover",
                                description = "Internal high-voltage transformer surge punctured dielectric cooling oil tank.",
                                isCorrect = false,
                                explanation = "INCORRECT: Electric motors are dry-wound electromagnetic machines without dielectric oil reservoirs."
                            },
                            new RootCauseOption
                            {
                                causeType = RootCauseType.HydraulicHoseRuptureHotSurface,
                                title = "Hydraulic Line Atomized Spray",
                                description = "High pressure fluid leak sprayed on turbocharger casing.",
                                isCorrect = false,
                                explanation = "INCORRECT: The failure originated within the electric drive motor stator, not an external hydraulic circuit."
                            }
                        };
                    }
                    else
                    {
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
                    break;

                case FireHazardType.VehicleHydraulic:
                    report.actualCause = RootCauseType.HydraulicHoseRuptureHotSurface;
                    report.options = new List<RootCauseOption>
                    {
                        new RootCauseOption
                        {
                            causeType = RootCauseType.HydraulicHoseRuptureHotSurface,
                            title = "High-Pressure Hydraulic Hose Rupture onto Hot Exhaust Manifold",
                            description = "Fatigued 3,000 PSI flexible hydraulic line ruptured, atomizing fluid mist directly across the engine turbocharger.",
                            isCorrect = true,
                            explanation = "CORRECT! 30 CFR § 75.1909 requires heavy shielding of high-pressure hydraulic lines near hot diesel exhaust surfaces. The atomized spray ignited on contact."
                        },
                        new RootCauseOption
                        {
                            causeType = RootCauseType.ElectricalCableInsulationFailure,
                            title = "Trailing Cable Short Circuit",
                            description = "High-voltage trailing cable crushed beneath wheel assembly.",
                            isCorrect = false,
                            explanation = "INCORRECT: The diesel engine vehicle was ignited by atomized hydraulic oil on the exhaust manifold, not trailing cable arcing."
                        },
                        new RootCauseOption
                        {
                            causeType = RootCauseType.TransformerOilSurgeFlashover,
                            title = "Transformer Tank Oil Rupture",
                            description = "Dielectric liquid transformer ruptured from internal pressure buildup.",
                            isCorrect = false,
                            explanation = "INCORRECT: Mobile haulage vehicles and excavators do not utilize high-voltage dielectric liquid transformers."
                        }
                    };
                    break;

                case FireHazardType.BatteryThermalRunaway:
                    report.actualCause = RootCauseType.BatteryThermalRunawayOvercharge;
                    report.options = new List<RootCauseOption>
                    {
                        new RootCauseOption
                        {
                            causeType = RootCauseType.BatteryThermalRunawayOvercharge,
                            title = "Battery Cell Thermal Runaway & High-Rate Overcharge",
                            description = "Charger controller failure resulted in high-rate overcharging, puncturing cell separators and triggering catastrophic thermal runaway.",
                            isCorrect = true,
                            explanation = "CORRECT! 30 CFR § 75.340 requires automated temperature and charge cutoff safeguards on all underground charging stations."
                        },
                        new RootCauseOption
                        {
                            causeType = RootCauseType.ConveyorBearingFrictionDustIgnition,
                            title = "Idler Bearing Friction Ignition",
                            description = "Mechanical roller bearing seized and heated combustible float coal dust.",
                            isCorrect = false,
                            explanation = "INCORRECT: Battery charging stations are stationary electrical storage racks with no conveyor idlers."
                        },
                        new RootCauseOption
                        {
                            causeType = RootCauseType.HydraulicHoseRuptureHotSurface,
                            title = "Hydraulic Fluid Spray",
                            description = "Atomized hydraulic oil sprayed onto hot mechanical exhaust.",
                            isCorrect = false,
                            explanation = "INCORRECT: Stationary battery banks operate without pressurized hydraulic fluid systems."
                        }
                    };
                    break;

                default:
                    report.actualCause = RootCauseType.ElectricalCableInsulationFailure;
                    report.options = new List<RootCauseOption>
                    {
                        new RootCauseOption
                        {
                            causeType = RootCauseType.ElectricalCableInsulationFailure,
                            title = "Electrical Cable Insulation Breakdown",
                            description = "Severe overheating compromised cable insulation, generating a high-energy arc fault.",
                            isCorrect = true,
                            explanation = "CORRECT! Electrical arcing from insulation damage was the primary cause of ignition."
                        },
                        new RootCauseOption
                        {
                            causeType = RootCauseType.ConveyorBearingFrictionDustIgnition,
                            title = "Bearing Friction Overheat",
                            description = "Seized bearing heated adjacent coal dust.",
                            isCorrect = false,
                            explanation = "INCORRECT: The fault occurred in electrical circuitry."
                        },
                        new RootCauseOption
                        {
                            causeType = RootCauseType.HydraulicHoseRuptureHotSurface,
                            title = "Hydraulic Fluid Leak",
                            description = "Atomized fluid spray on hot exhaust.",
                            isCorrect = false,
                            explanation = "INCORRECT: No hydraulic fluid lines were involved."
                        }
                    };
                    break;
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
                        _machineOriginScore +
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

            Debug.Log($"[InvestigationManager] 🏆 SCORECARD READY! Total Score: {total} | Grade: {gradeTitle}");
            OnInvestigationStateChanged?.Invoke(_state);
            OnScorecardReady?.Invoke(_breakdown);
        }

        public void RestartScenario()
        {
            FireFootprintTrailBuilder.ClearTrail();

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
