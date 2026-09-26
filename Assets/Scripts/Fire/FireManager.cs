using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Environment;
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Fire
{
    public enum FireScenarioPreset
    {
        RandomAdaptive = 0,
        ScenarioA_CableJunctionBoxArc = 1,
        ScenarioB_ConveyorRollerFriction = 2,
        ScenarioC_TransformerInsulation = 3,
        ScenarioD_LHDHydraulicLeak = 4,
        ScenarioE_BatteryChargingRunaway = 5,
        ScenarioF_ConveyorMotorOverload = 6
    }

    /// <summary>
    /// Central manager for underground mining fire scenarios, fire generation, and queries.
    /// Uses distinct fire profiles (electrical arcs with sparks, bearing friction, hydraulic fluid blaze,
    /// battery thermal runaway) with equipment-specific flame positions, causes, and thermographic readings.
    /// </summary>
    [RequireComponent(typeof(FireSpreadSystem))]
    public class FireManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private EquipmentSpawner _equipmentSpawner;
        [SerializeField] private FireSpreadSystem _spreadSystem;

        [Header("Scenario Settings")]
        [SerializeField] private FireScenarioPreset _selectedPreset = FireScenarioPreset.RandomAdaptive;
        [SerializeField] private FireSeverity _scenarioSeverity = FireSeverity.Small;
        [SerializeField] private bool _autoStartOnEnvironmentReady = false;
        [SerializeField] private float _delayBeforeIgnition = 1.5f;

        public FireSeverity ScenarioSeverity => _scenarioSeverity;

        // Active Fires
        private readonly List<FireHazard> _activeFires = new List<FireHazard>();
        private bool _isScenarioActive = false;
        private float _scenarioTimer = 0f;
        private string _activeScenarioTitle = "Underground Incident";
        private FireSeverity _lastNotifiedSeverity = FireSeverity.Small;

        // Events
        public event Action<List<FireHazard>> OnFiresStarted;
        public event Action<FireSeverity> OnHighestSeverityChanged;
        public static event Action OnAllFiresExtinguished;
        public static event Action<FireHazard> OnFireIgnited;

        public static FireManager Instance { get; private set; }

        public IReadOnlyList<FireHazard> ActiveFires => _activeFires;
        public List<FireHazard> GetActiveFires() => new List<FireHazard>(_activeFires);
        public bool HasActiveFires => _activeFires.Count > 0;
        public float ScenarioTimer => _scenarioTimer;
        public string ActiveScenarioTitle => _activeScenarioTitle;
        public FireSpreadSystem SpreadSystem => _spreadSystem;
        public float FireGrowthTime => _spreadSystem != null ? _spreadSystem.FireGrowthTime : 60.0f;
        public bool IsSmallPhase => _scenarioTimer < FireGrowthTime;
        public float RemainingSmallTime => Mathf.Max(0f, FireGrowthTime - _scenarioTimer);
        public bool IsBigPhase => _scenarioTimer >= FireGrowthTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_spreadSystem == null)
                _spreadSystem = GetComponent<FireSpreadSystem>();
            if (_equipmentSpawner == null)
                _equipmentSpawner = FindFirstObjectByType<EquipmentSpawner>();
        }

        public void ExtinguishAll() => ExtinguishAllFires();
        public void IgniteRandomFires() => StartScenarioFires();
        public void SetScenarioPreset(FireScenarioPreset preset) => _selectedPreset = preset;

        private void Start()
        {
            // Only auto-start if explicitly enabled AND equipment is already confirmed spawned
            if (_autoStartOnEnvironmentReady)
            {
                var spawner = _equipmentSpawner != null ? _equipmentSpawner : FindAnyObjectByType<EquipmentSpawner>();
                if (spawner != null && (spawner.SpawnedMachines.Count > 0 || spawner.SpawnedElectrical.Count > 0))
                {
                    Invoke(nameof(StartScenarioFires), _delayBeforeIgnition);
                }
            }
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(StartScenarioFires));
        }

        private void Update()
        {
            if (!_isScenarioActive) return;

            _scenarioTimer += Time.deltaTime;

            if (_spreadSystem != null && _activeFires.Count > 0)
            {
                _spreadSystem.UpdateFires(_activeFires);

                FireSeverity currentHighest = GetHighestSeverity();
                if (currentHighest != _lastNotifiedSeverity)
                {
                    _lastNotifiedSeverity = currentHighest;
                    _scenarioSeverity = currentHighest;
                    OnHighestSeverityChanged?.Invoke(currentHighest);
                    Debug.Log($"[FireManager] 📈 Fire escalated to {currentHighest} at {_scenarioTimer:F1}s!");
                }
            }

            CheckExtinguishCompletion();
        }

        /// <summary>
        /// Selects a realistic training scenario matching spawned equipment and ignites
        /// the appropriate machinery with believable root causes and unique visual profiles.
        /// Guaranteed to always start a fire without hanging or entering infinite retry loops.
        /// </summary>
        public void StartScenarioFires()
        {
            CancelInvoke(nameof(StartScenarioFires));
            ClearAllFires();

            // Fire always starts as an incipient Small fire and remains small for at least 1 full minute (60 seconds)
            _scenarioSeverity = FireSeverity.Small;
            _lastNotifiedSeverity = FireSeverity.Small;
            _scenarioTimer = 0f;

            if (_spreadSystem != null)
            {
                _spreadSystem.TimeMultiplier = 1.0f;
                _spreadSystem.SmallToMediumTime = Mathf.Max(60.0f, _spreadSystem.SmallToMediumTime);
                _spreadSystem.MediumToLargeTime = Mathf.Max(60.0f, _spreadSystem.MediumToLargeTime);
            }

            if (_equipmentSpawner == null)
                _equipmentSpawner = FindAnyObjectByType<EquipmentSpawner>();

            List<MiningMachine> machines = new List<MiningMachine>();
            List<ElectricalEquipment> electrical = new List<ElectricalEquipment>();

            if (_equipmentSpawner != null)
            {
                machines.AddRange(_equipmentSpawner.SpawnedMachines);
                electrical.AddRange(_equipmentSpawner.SpawnedElectrical);
            }

            // Also search scene directly in case equipment was spawned or registered outside the spawner reference
            var sceneMachines = FindObjectsByType<MiningMachine>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneMachines.Length; i++)
            {
                if (sceneMachines[i] != null && !machines.Contains(sceneMachines[i]))
                    machines.Add(sceneMachines[i]);
            }

            var sceneElectrical = FindObjectsByType<ElectricalEquipment>(FindObjectsSortMode.None);
            for (int i = 0; i < sceneElectrical.Length; i++)
            {
                if (sceneElectrical[i] != null && !electrical.Contains(sceneElectrical[i]))
                    electrical.Add(sceneElectrical[i]);
            }

            // If no equipment found yet, trigger immediate deployment from MiningEnvironmentGenerator
            if (machines.Count == 0 && electrical.Count == 0)
            {
                var generator = FindAnyObjectByType<ARMiningSimulator.Environment.MiningEnvironmentGenerator>();
                if (generator != null)
                {
                    if (!generator.IsEnvironmentReady)
                    {
                        generator.GenerateEnvironment();
                    }
                    else if (!generator.IsEquipmentSpawned)
                    {
                        generator.DeployEquipmentNow();
                    }
                }

                // Re-poll spawner and scene after immediate deployment
                if (_equipmentSpawner != null)
                {
                    machines.AddRange(_equipmentSpawner.SpawnedMachines);
                    electrical.AddRange(_equipmentSpawner.SpawnedElectrical);
                }
                sceneMachines = FindObjectsByType<MiningMachine>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneMachines.Length; i++)
                {
                    if (sceneMachines[i] != null && !machines.Contains(sceneMachines[i]))
                        machines.Add(sceneMachines[i]);
                }
                sceneElectrical = FindObjectsByType<ElectricalEquipment>(FindObjectsSortMode.None);
                for (int i = 0; i < sceneElectrical.Length; i++)
                {
                    if (sceneElectrical[i] != null && !electrical.Contains(sceneElectrical[i]))
                        electrical.Add(sceneElectrical[i]);
                }
            }

            // Pool all valid spawned equipment (both heavy machines and electrical tools)
            List<GameObject> candidateTargets = new List<GameObject>();
            for (int i = 0; i < machines.Count; i++)
            {
                if (machines[i] != null && machines[i].gameObject != null)
                    candidateTargets.Add(machines[i].gameObject);
            }
            for (int i = 0; i < electrical.Count; i++)
            {
                if (electrical[i] != null && electrical[i].gameObject != null)
                    candidateTargets.Add(electrical[i].gameObject);
            }

            // Emergency Fallback: If still empty (e.g. testing in empty room), synthesize an emergency electrical unit on the side rib
            if (candidateTargets.Count == 0)
            {
                Debug.LogWarning("[FireManager] No equipment found in scene; synthesizing an emergency Electrical Junction Box on the side rib.");
                GameObject fallbackGo = new GameObject("Electrical_EmergencyJunctionBox");
                
                Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                camForward.y = 0f;
                if (camForward.sqrMagnitude < 0.01f) camForward = Vector3.forward;
                camForward.Normalize();

                Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
                camRight.y = 0f;
                camRight.Normalize();

                // Position strictly on the side rib (+1.35m lateral right, +1.5m forward), leaving central evacuation corridor clear
                Vector3 fallbackPos = (Camera.main != null ? Camera.main.transform.position : Vector3.zero) 
                    + camForward * 1.5f + camRight * 1.35f;
                fallbackPos.y = 0.5f;
                fallbackGo.transform.position = fallbackPos;

                var fallbackElec = fallbackGo.AddComponent<ElectricalEquipment>();
                fallbackElec.Initialize(1, ElectricalType.CableBox, "High-Voltage Junction Box", 0.5f);
                candidateTargets.Add(fallbackGo);
                electrical.Add(fallbackElec);
            }

            // Resolve Exit and Player positions for safety corridor calculation
            Vector3 exitPos = Vector3.forward * 2.5f;
            var envGen = FindFirstObjectByType<ARMiningSimulator.Environment.MiningEnvironmentGenerator>();
            if (envGen != null && envGen.IsGenerated)
            {
                exitPos = envGen.EmergencyExitPosition;
            }
            else if (ARMiningSimulator.Evacuation.EvacuationManager.Instance != null)
            {
                exitPos = ARMiningSimulator.Evacuation.EvacuationManager.Instance.ExitPosition;
            }

            Vector3 playerPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;

            // Filter candidates: fire must be on the side rib or inbye working face, and NOT on the evacuation path to safety
            List<GameObject> sideCandidates = candidateTargets.FindAll(t => IsTargetOnSideAndClearOfExitPath(t, playerPos, exitPos));
            if (sideCandidates.Count == 0)
            {
                // Fallback to whichever candidate is furthest from the evacuation corridor line
                candidateTargets.Sort((a, b) => DistanceToLineSegment(b.transform.position, playerPos, exitPos).CompareTo(DistanceToLineSegment(a.transform.position, playerPos, exitPos)));
                sideCandidates.Add(candidateTargets[0]);
            }

            GameObject primaryTarget = null;

            // Pick a random machine or electrical tool uniformly across side equipment
            if (_selectedPreset == FireScenarioPreset.RandomAdaptive)
            {
                int randomIndex = UnityEngine.Random.Range(0, sideCandidates.Count);
                primaryTarget = sideCandidates[randomIndex];
                // Always start as an incipient Small fire; escalates after 1 whole minute (60 seconds)
                _scenarioSeverity = FireSeverity.Small;
            }
            else
            {
                switch (_selectedPreset)
                {
                    case FireScenarioPreset.ScenarioA_CableJunctionBoxArc:
                        primaryTarget = FindTarget(electrical, ElectricalType.CableBox) ?? FindTarget(electrical, ElectricalType.ElectricalPanel);
                        break;

                    case FireScenarioPreset.ScenarioB_ConveyorRollerFriction:
                        primaryTarget = FindTarget(machines, MachineType.Conveyor);
                        break;

                    case FireScenarioPreset.ScenarioC_TransformerInsulation:
                        primaryTarget = FindTarget(electrical, ElectricalType.Transformer) ?? FindTarget(electrical, ElectricalType.Switchboard);
                        break;

                    case FireScenarioPreset.ScenarioD_LHDHydraulicLeak:
                        primaryTarget = FindTarget(machines, MachineType.Scooptram);
                        break;

                    case FireScenarioPreset.ScenarioE_BatteryChargingRunaway:
                        primaryTarget = FindTarget(electrical, ElectricalType.BatteryChargingStation) ?? FindTarget(electrical, ElectricalType.Switchboard);
                        break;

                    case FireScenarioPreset.ScenarioF_ConveyorMotorOverload:
                    default:
                        primaryTarget = FindTarget(electrical, ElectricalType.ElectricalMotor) ?? FindTarget(machines, MachineType.ContinuousMiner);
                        break;
                }

                // If preset target is missing or located too close to the evacuation path to safety, pick safe side candidate
                if (primaryTarget == null || !sideCandidates.Contains(primaryTarget))
                {
                    primaryTarget = sideCandidates[UnityEngine.Random.Range(0, sideCandidates.Count)];
                }
            }

            if (primaryTarget != null)
            {
                var profile = ResolveEquipmentFireProfile(primaryTarget);
                string cleanTargetName = primaryTarget.name.Replace("Machine_", "").Replace("Electrical_", "").Replace("(Clone)", "").Trim();
                _activeScenarioTitle = $"Incident: Fire on {cleanTargetName} ({profile.hazardCause})";

                IgniteTargetWithProfile(primaryTarget);
            }

            _isScenarioActive = true;
            _scenarioTimer = 0f;
            _lastNotifiedSeverity = FireSeverity.Small;

            Debug.Log($"[FireManager] 🔥 {_activeScenarioTitle} started! Target: {primaryTarget?.name} | Total active fires: {_activeFires.Count}");
            OnFiresStarted?.Invoke(_activeFires);

            // Synchronize with global loop, reaction timer, and trainee HUD
            ARMiningSimulator.Core.SimulationGameLoop.Instance?.SetStage(ARMiningSimulator.Core.SimulationStage.FireIgnited);
            ARMiningSimulator.Player.TraineeDetection.Instance?.StartStopwatch();
            FindFirstObjectByType<ARMiningSimulator.UI.TraineeStatusHUD>()?.ResetFireVisibility();
        }

        private GameObject FindTarget(List<MiningMachine> machines, MachineType type)
        {
            for (int i = 0; i < machines.Count; i++)
            {
                if (machines[i] != null && machines[i].Type == type)
                    return machines[i].gameObject;
            }
            return null;
        }

        private GameObject FindTarget(List<ElectricalEquipment> electrical, ElectricalType type)
        {
            for (int i = 0; i < electrical.Count; i++)
            {
                if (electrical[i] != null && electrical[i].Type == type)
                    return electrical[i].gameObject;
            }
            return null;
        }

        private void IgniteTargetWithProfile(GameObject target)
        {
            if (target == null) return;

            var profile = ResolveEquipmentFireProfile(target);

            GameObject hazardGo = new GameObject($"FireHazard_{target.name}");
            hazardGo.transform.SetParent(target.transform, false);
            hazardGo.transform.localPosition = profile.localOffset;

            FireHazard hazard = hazardGo.AddComponent<FireHazard>();
            hazard.Initialize(
                target,
                _scenarioSeverity,
                profile.hazardType,
                profile.hazardCause,
                profile.visualClue,
                profile.thermalReading
            );

            // Mark entity as on fire
            var machineComp = target.GetComponent<MiningMachine>();
            if (machineComp != null) machineComp.IsOnFire = true;

            var elecComp = target.GetComponent<ElectricalEquipment>();
            if (elecComp != null) elecComp.IsOnFire = true;

            _activeFires.Add(hazard);
            OnFireIgnited?.Invoke(hazard);
        }

        private struct FireProfileData
        {
            public FireHazardType hazardType;
            public string hazardCause;
            public string visualClue;
            public string thermalReading;
            public Vector3 localOffset;
        }

        private FireProfileData ResolveEquipmentFireProfile(GameObject target)
        {
            FireProfileData p = new FireProfileData();
            p.localOffset = Vector3.up * 0.45f;

            var machine = target.GetComponent<MiningMachine>();
            var elec = target.GetComponent<ElectricalEquipment>();

            if (machine != null)
            {
                switch (machine.Type)
                {
                    case MachineType.Conveyor:
                        p.hazardType = FireHazardType.MechanicalFriction;
                        p.hazardCause = "Seized Idler Bearing & Coal Dust Friction Ignition";
                        p.visualClue = "Return idler bearing seized solid in housing; rubber belt underside scorched and shedding molten carbon residue.";
                        p.thermalReading = "IR Thermography: 425°C bearing housing heat-soak.";
                        p.localOffset = new Vector3(0f, 0.75f, -0.65f); // At tail roller bearing
                        break;

                    case MachineType.Scooptram:
                        p.hazardType = FireHazardType.VehicleHydraulic;
                        p.hazardCause = "High-Pressure Hydraulic Hose Rupture on Hot Turbocharger";
                        p.visualClue = "Atomized 3,000 PSI hydraulic fluid spraying from cracked hose onto diesel turbo exhaust manifold.";
                        p.thermalReading = "IR Thermography: 540°C turbocharger exhaust housing.";
                        p.localOffset = new Vector3(0f, 0.85f, -0.42f); // At rear engine compartment
                        break;

                    case MachineType.ContinuousMiner:
                        p.hazardType = FireHazardType.VehicleHydraulic;
                        p.hazardCause = "Cutter Motor Overheat & Trailing Cable Mechanical Abrasion";
                        p.visualClue = "Damaged 950V trailing cable jacket abraded against track frame; localized flame at cutter drive gearbox.";
                        p.thermalReading = "IR Thermography: 365°C cutter motor housing.";
                        p.localOffset = new Vector3(0f, 0.85f, 0.65f); // Near front cutter boom
                        break;

                    case MachineType.RoofBolter:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Hydraulic Pump Motor Electrical Short Circuit";
                        p.visualClue = "Charred conduit terminal box; electrical arcing scorched the bolter mast support frame.";
                        p.thermalReading = "IR Thermography: 285°C pump motor enclosure.";
                        p.localOffset = new Vector3(0f, 1.25f, 0.35f);
                        break;

                    case MachineType.MiningDrill:
                        p.hazardType = FireHazardType.MechanicalFriction;
                        p.hazardCause = "Drill Mast Rotation Bearing Overheat";
                        p.visualClue = "Seized rotation head bearing housing; burnt grease residue and heat-discolored steel.";
                        p.thermalReading = "IR Thermography: 340°C rotation head.";
                        p.localOffset = new Vector3(0f, 1.40f, 0.25f);
                        break;

                    case MachineType.Excavator:
                        p.hazardType = FireHazardType.VehicleHydraulic;
                        p.hazardCause = "Hydraulic Excavator Main Boom Line Rupture";
                        p.visualClue = "Pressurized hydraulic fluid spray onto hot engine exhaust manifold; flames spreading across engine bay.";
                        p.thermalReading = "IR Thermography: 410°C hydraulic return manifold.";
                        p.localOffset = new Vector3(0f, 1.15f, -0.10f);
                        break;

                    default:
                        p.hazardType = FireHazardType.StandardEquipment;
                        p.hazardCause = "Mining Machine Overheat";
                        p.visualClue = "Smoke and localized flame emitting from machinery chassis.";
                        p.thermalReading = "IR Thermography: 380°C chassis hotspot.";
                        p.localOffset = Vector3.up * 0.90f;
                        break;
                }
            }
            else if (elec != null)
            {
                switch (elec.Type)
                {
                    case ElectricalType.CableReel:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Trailing Cable Drum Insulation Puncture & Thermal Breakdown";
                        p.visualClue = "Melting neoprene outer sheath wound tightly on drum; continuous inter-layer smoldering and ozone odor.";
                        p.thermalReading = "IR Thermography: 275°C core cable winding.";
                        p.localOffset = Vector3.up * 0.65f;
                        break;

                    case ElectricalType.PortableElectricDrill:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Armature Commutator Brush Flash & Carbon Dust Smolder";
                        p.visualClue = "Vigorous spark emission from drill cooling vents; molten copper spatter inside casing.";
                        p.thermalReading = "IR Thermography: 220°C motor casing.";
                        p.localOffset = Vector3.up * 0.45f;
                        break;

                    case ElectricalType.ControlBox:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Control Relay Contact Welding & Terminal Board Scorching";
                        p.visualClue = "Charred push-button station and fused relay contacts with black soot venting from door seams.";
                        p.thermalReading = "IR Thermography: 250°C internal relay rack.";
                        p.localOffset = Vector3.up * 0.65f;
                        break;

                    case ElectricalType.CableBox:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Loose Connection & Phase-to-Ground Electrical Arcing";
                        p.visualClue = "Charred terminal lugs with arc-pitting on Phase C copper bus-bar; melted PVC insulation pooling at base.";
                        p.thermalReading = "IR Thermography: 310°C localized to Phase C wiring lug.";
                        p.localOffset = Vector3.up * 0.50f;
                        break;

                    case ElectricalType.Transformer:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Dielectric Oil Overheating & High-Voltage Flashover";
                        p.visualClue = "Dielectric oil expansion tank pressure relief ruptured; black soot coating cooling radiators.";
                        p.thermalReading = "IR Thermography: 385°C transformer tank core.";
                        p.localOffset = Vector3.up * 1.05f;
                        break;

                    case ElectricalType.ElectricalMotor:
                        p.hazardType = FireHazardType.MechanicalFriction;
                        p.hazardCause = "Drive Motor Stator Winding Overload & Bearing Seizure";
                        p.visualClue = "Motor end-bell discolored from friction; copper winding insulation blistered with burnt lacquer odor.";
                        p.thermalReading = "IR Thermography: 295°C motor stator casing.";
                        p.localOffset = Vector3.up * 0.65f;
                        break;

                    case ElectricalType.BatteryChargingStation:
                        p.hazardType = FireHazardType.BatteryThermalRunaway;
                        p.hazardCause = "Lithium/Lead-Acid Battery Cell Thermal Runaway";
                        p.visualClue = "Swollen battery casing with ruptured pressure caps; vigorous spark emission and chemical aerosol vapor.";
                        p.thermalReading = "IR Thermography: 360°C battery bank.";
                        p.localOffset = Vector3.up * 0.95f;
                        break;

                    case ElectricalType.ElectricalPanel:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Molded-Case Breaker Internal Phase-to-Ground Arc Flash";
                        p.visualClue = "Enclosure door blown ajar; copper vapor residue and charred breaker casing.";
                        p.thermalReading = "IR Thermography: 325°C main breaker.";
                        p.localOffset = Vector3.up * 1.25f;
                        break;

                    case ElectricalType.Switchboard:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Switchboard Incomer Busbar Short Circuit";
                        p.visualClue = "Heavy soot deposition across all 3 phases; incoming breaker contacts welded together.";
                        p.thermalReading = "IR Thermography: 350°C incoming cell.";
                        p.localOffset = Vector3.up * 1.35f;
                        break;

                    case ElectricalType.PowerUnit:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Solid-State Rectifier Component Thermal Overload";
                        p.visualClue = "Ruptured power capacitors with electrolyte spray inside enclosure.";
                        p.thermalReading = "IR Thermography: 265°C power stack.";
                        p.localOffset = Vector3.up * 0.95f;
                        break;

                    case ElectricalType.VentilationFan:
                        p.hazardType = FireHazardType.MechanicalFriction;
                        p.hazardCause = "Fan Impeller Bearing Seizure & Motor Stall";
                        p.visualClue = "Fan housing scraped by blade tips; stalled motor coils smoking.";
                        p.thermalReading = "IR Thermography: 315°C motor casing.";
                        p.localOffset = Vector3.up * 1.05f;
                        break;

                    default:
                        p.hazardType = FireHazardType.ElectricalArc;
                        p.hazardCause = "Electrical Cable Insulation Breakdown";
                        p.visualClue = "Charred jacket and copper strand melting.";
                        p.thermalReading = "IR Thermography: 270°C.";
                        p.localOffset = Vector3.up * 0.75f;
                        break;
                }
            }
            else
            {
                p.hazardType = FireHazardType.StandardEquipment;
                p.hazardCause = "Equipment Overheat";
                p.visualClue = "Smoke and flame emitting from equipment housing.";
                p.thermalReading = "IR Thermography: 300°C.";
            }

            return p;
        }

        public FireSeverity GetHighestSeverity()
        {
            FireSeverity highest = FireSeverity.Small;
            foreach (var fire in _activeFires)
            {
                if (fire == null || fire.State != FireState.Burning) continue;
                if (fire.Severity > highest)
                    highest = fire.Severity;
            }
            return highest;
        }

        public FireHazard GetClosestFire(Vector3 position)
        {
            FireHazard closest = null;
            float minDist = float.MaxValue;

            foreach (var fire in _activeFires)
            {
                if (fire == null || fire.State != FireState.Burning) continue;
                float dist = Vector3.Distance(position, fire.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = fire;
                }
            }

            return closest;
        }

        public void SetScenarioSeverity(FireSeverity severity)
        {
            _scenarioSeverity = severity;
            SetAllFiresSeverity(severity);
        }

        public void SetAllFiresSeverity(FireSeverity severity)
        {
            _scenarioSeverity = severity;
            _lastNotifiedSeverity = severity;

            float medTime = _spreadSystem != null ? _spreadSystem.SmallToMediumTime : 60.0f;
            float largeTime = _spreadSystem != null ? _spreadSystem.MediumToLargeTime : 60.0f;

            if (severity == FireSeverity.Small)
            {
                _scenarioTimer = 0f;
            }
            else if (severity == FireSeverity.Medium)
            {
                _scenarioTimer = medTime + 0.1f;
            }
            else if (severity == FireSeverity.Large)
            {
                _scenarioTimer = largeTime + 0.1f;
            }

            foreach (var fire in _activeFires)
            {
                if (fire != null && fire.State == FireState.Burning)
                {
                    fire.SetSeverity(severity);
                }
            }

            OnHighestSeverityChanged?.Invoke(severity);
        }

        public void ExtinguishAllFires()
        {
            foreach (var fire in _activeFires)
            {
                if (fire != null)
                {
                    fire.Extinguish(9999f);
                }
            }
            CheckExtinguishCompletion();
        }

        public void ClearAllFires()
        {
            foreach (var fire in _activeFires)
            {
                if (fire != null) Destroy(fire.gameObject);
            }
            _activeFires.Clear();
            _isScenarioActive = false;
            _scenarioTimer = 0f;
        }

        private void CheckExtinguishCompletion()
        {
            if (!_isScenarioActive) return;

            bool allExtinguished = true;
            foreach (var fire in _activeFires)
            {
                if (fire != null && fire.State == FireState.Burning)
                {
                    allExtinguished = false;
                    break;
                }
            }

            if (allExtinguished && _activeFires.Count > 0)
            {
                _isScenarioActive = false;
                Debug.Log("[FireManager] All fires have been EXTINGUISHED!");
                OnAllFiresExtinguished?.Invoke();
            }
        }

        /// <summary>
        /// Ensures fire only ignites on equipment positioned to the side (rock ribs or inbye face)
        /// and strictly maintains clearance from the evacuation path and emergency exit doorway.
        /// </summary>
        private bool IsTargetOnSideAndClearOfExitPath(GameObject target, Vector3 playerPos, Vector3 exitPos)
        {
            if (target == null) return false;

            Vector3 targetPos = target.transform.position;

            // 1. Must not be anywhere near the Emergency Exit doorway or Safe Zone (minimum 2.0m buffer)
            float distToExit = Vector3.Distance(targetPos, exitPos);
            if (distToExit < 2.0f)
                return false;

            // 2. Must not be on or adjacent to the direct evacuation walking corridor (minimum 1.15m lateral clearance)
            float distToPath = DistanceToLineSegment(targetPos, playerPos, exitPos);
            if (distToPath < 1.15f)
                return false;

            // 3. Must be situated on the side (lateral rib offset) or deep inbye at the working face
            var generator = FindFirstObjectByType<ARMiningSimulator.Environment.MiningEnvironmentGenerator>();
            if (generator != null && generator.IsGenerated)
            {
                Vector3 localPos = Quaternion.Inverse(generator.transform.rotation) * (targetPos - generator.transform.position);
                // In local space: X is lateral ribs, Z is tunnel axis (negative = inbye, positive = exit)
                bool isLateralSide = Mathf.Abs(localPos.x) >= 0.40f;
                bool isInbyeFace = localPos.z < -0.1f;
                if (!isLateralSide && !isInbyeFace)
                    return false;
            }

            return true;
        }

        private float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 line = lineEnd - lineStart;
            float len = line.magnitude;
            if (len < 0.001f) return Vector3.Distance(point, lineStart);

            Vector3 dir = line.normalized;
            float proj = Vector3.Dot(point - lineStart, dir);
            proj = Mathf.Clamp(proj, 0f, len);

            Vector3 closestPoint = lineStart + dir * proj;
            return Vector3.Distance(point, closestPoint);
        }
    }
}
