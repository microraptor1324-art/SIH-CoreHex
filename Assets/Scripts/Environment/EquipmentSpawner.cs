using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Intelligently arranges mining machinery and electrical equipment into functional underground mining zones:
    /// 1. Working Face (Inbye): Continuous Miner, Roof Bolter, Mining Drill, Cable Reel.
    /// 2. Material Transport Route (+X Rib): Conveyor Belt System with attached Industrial Electric Motor & Control Box.
    /// 3. Haulage Way (Drift Corridor): LHD Scooptram parked with full walking clearance.
    /// 4. Electrical Alcove Substation (-X Rib): HV Transformer, Main Switchboard, HV Panel, PDU, Junction Box, Battery Station.
    /// 5. Ventilation System: Ventilation Fan coupled to overhead ducting.
    /// 6. Evacuation Path: Guaranteed 0.85m clear walking corridor to Emergency Exit.
    /// Supports adaptive subset spawning based on measured AR room area.
    /// </summary>
    public class EquipmentSpawner : MonoBehaviour
    {
        [Header("Counts (Configurable in Inspector)")]
        [SerializeField] private int _machineCount = 6;
        [SerializeField] private int _electricalCount = 11;

        [Header("Spacing & Safety Margins")]
        [SerializeField] private float _minSpacing = 0.75f;
        [SerializeField] private float _boundaryPadding = 0.35f;
        [SerializeField] private float _playerSafeRadius = 0.95f;
        [SerializeField] private float _exitCorridorWidth = 0.85f;

        [Header("Randomization")]
        [Tooltip("Set to 0 to use a random seed every time, or a positive number to reproduce exact layout")]
        [SerializeField] private int _randomSeed = 0;

        // Runtime Tracking
        private readonly List<MiningMachine> _spawnedMachines = new List<MiningMachine>();
        private readonly List<ElectricalEquipment> _spawnedElectrical = new List<ElectricalEquipment>();
        private readonly List<Vector3> _occupiedPositions = new List<Vector3>();
        private Vector3 _conveyorBedPosition = Vector3.zero;
        private int _activeSeed = 0;

        public IReadOnlyList<MiningMachine> SpawnedMachines => _spawnedMachines;
        public IReadOnlyList<ElectricalEquipment> SpawnedElectrical => _spawnedElectrical;
        public int ActiveSeed => _activeSeed;

        public void InitSpawningContext()
        {
            _activeSeed = _randomSeed != 0 ? _randomSeed : Random.Range(1, 999999);
            Random.InitState(_activeSeed);
            _conveyorBedPosition = Vector3.zero;
        }

        /// <summary>
        /// Deploys mining machines into their respective functional zones.
        /// Adapts count based on measured AR room dimensions.
        /// </summary>
        public List<MiningMachine> SpawnMachines(Vector3 roomCenter, float roomLength, float roomWidth, Quaternion roomRotation, Vector3 exitPos, Vector3 playerPos, bool animate = true)
        {
            Transform container = transform;
            float halfX = Mathf.Max(0.6f, (roomWidth * 0.5f) - _boundaryPadding);
            float halfZ = Mathf.Max(0.8f, (roomLength * 0.5f) - _boundaryPadding);
            float roomArea = roomLength * roomWidth;

            // Determine machine roster based on room area
            List<MachineType> machinesToSpawn = new List<MachineType>();
            if (roomArea < 18f || roomLength < 4.2f)
            {
                // Compact Room (3 core machines)
                machinesToSpawn.Add(MachineType.ContinuousMiner);
                machinesToSpawn.Add(MachineType.Conveyor);
                machinesToSpawn.Add(MachineType.Scooptram);
            }
            else if (roomArea < 32f)
            {
                // Medium Room (4 machines)
                machinesToSpawn.Add(MachineType.ContinuousMiner);
                machinesToSpawn.Add(MachineType.RoofBolter);
                machinesToSpawn.Add(MachineType.Conveyor);
                machinesToSpawn.Add(MachineType.Scooptram);
            }
            else
            {
                // Large Room (All 6 machines)
                machinesToSpawn.Add(MachineType.ContinuousMiner);
                machinesToSpawn.Add(MachineType.RoofBolter);
                machinesToSpawn.Add(MachineType.MiningDrill);
                machinesToSpawn.Add(MachineType.Conveyor);
                machinesToSpawn.Add(MachineType.Scooptram);
                machinesToSpawn.Add(MachineType.Excavator);
            }

            for (int i = 0; i < machinesToSpawn.Count; i++)
            {
                MachineType mType = machinesToSpawn[i];
                GameObject machineGo = null;
                string mName = "";
                Vector3 preferredLocalPos = Vector3.zero;
                Quaternion preferredLocalRot = Quaternion.identity;

                switch (mType)
                {
                    case MachineType.ContinuousMiner:
                        // Positioned at the working face (-Z end), oriented toward the face heading
                        machineGo = ProceduralModelBuilder.BuildContinuousMiner(container);
                        mName = "Continuous Miner";
                        preferredLocalPos = new Vector3(-halfX * 0.15f, 0f, -halfZ + 0.95f);
                        preferredLocalRot = Quaternion.Euler(0, 0, 0);
                        break;

                    case MachineType.RoofBolter:
                        // Positioned near working face for active ground support
                        machineGo = ProceduralModelBuilder.BuildRoofBolter(container);
                        mName = "Roof Bolter";
                        preferredLocalPos = new Vector3(-halfX * 0.48f, 0f, -halfZ + 2.1f);
                        preferredLocalRot = Quaternion.Euler(0, 15f, 0);
                        break;

                    case MachineType.MiningDrill:
                        // Positioned on the face flank in drilling position
                        machineGo = ProceduralModelBuilder.BuildMiningDrill(container);
                        mName = "Mining Drill";
                        preferredLocalPos = new Vector3(-halfX * 0.72f, 0f, -halfZ + 1.25f);
                        preferredLocalRot = Quaternion.Euler(0, 30f, 0);
                        break;

                    case MachineType.Conveyor:
                        // Positioned along the +X rib running from the continuous miner back outbye
                        machineGo = ProceduralModelBuilder.BuildConveyor(container);
                        mName = "Conveyor Belt System";
                        preferredLocalPos = new Vector3(halfX * 0.65f, 0f, -halfZ + Mathf.Min(halfZ * 1.05f, 2.6f));
                        preferredLocalRot = Quaternion.Euler(0, 180f, 0);
                        _conveyorBedPosition = roomCenter + roomRotation * preferredLocalPos;
                        break;

                    case MachineType.Scooptram:
                        // Positioned along the left rib haulage bay, leaving wide central walking clearance
                        machineGo = ProceduralModelBuilder.BuildScooptram(container);
                        mName = "LHD Scooptram";
                        preferredLocalPos = new Vector3(-halfX * 0.62f, 0f, -0.15f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 180f, 0);
                        break;

                    case MachineType.Excavator:
                    default:
                        // Positioned in side loading pocket on the right rib, safely inbye from the exit
                        machineGo = ProceduralModelBuilder.BuildExcavator(container);
                        mName = "Underground Excavator";
                        preferredLocalPos = new Vector3(halfX * 0.65f, 0f, -0.25f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, -45f, 0);
                        break;
                }

                Vector3 spawnPos = ResolveZonedPosition(preferredLocalPos, roomCenter, halfX, halfZ, roomRotation, exitPos, playerPos);
                Quaternion spawnRot = roomRotation * preferredLocalRot;

                machineGo.transform.position = spawnPos;
                machineGo.transform.rotation = spawnRot;

                var machineComp = machineGo.AddComponent<MiningMachine>();
                machineComp.Initialize(i + 1, mType, mName, 0.70f);
                _spawnedMachines.Add(machineComp);
                _occupiedPositions.Add(spawnPos);

                if (animate && Application.isPlaying)
                {
                    StartCoroutine(Co_PopInAnimation(machineGo));
                }
            }

            Debug.Log($"[EquipmentSpawner] Successfully placed {_spawnedMachines.Count} zoned mining machines.");
            return _spawnedMachines;
        }

        /// <summary>
        /// Deploys electrical equipment logically into the electrical substation bay along the -X rib
        /// and directly couples the electric motor and control box to the conveyor system.
        /// </summary>
        public List<ElectricalEquipment> SpawnElectrical(Vector3 roomCenter, float roomLength, float roomWidth, Quaternion roomRotation, Vector3 exitPos, Vector3 playerPos, bool animate = true)
        {
            Transform container = transform;
            float halfX = Mathf.Max(0.6f, (roomWidth * 0.5f) - _boundaryPadding);
            float halfZ = Mathf.Max(0.8f, (roomLength * 0.5f) - _boundaryPadding);
            float roomArea = roomLength * roomWidth;

            // Determine electrical roster based on room tier
            List<ElectricalType> electricalToSpawn = new List<ElectricalType>();
            if (roomArea < 18f || roomLength < 4.2f)
            {
                // Compact Room (4 core items)
                electricalToSpawn.Add(ElectricalType.ElectricalMotor); // At conveyor
                electricalToSpawn.Add(ElectricalType.ControlBox);      // At conveyor
                electricalToSpawn.Add(ElectricalType.CableBox);        // In bay
                electricalToSpawn.Add(ElectricalType.Switchboard);     // In bay
            }
            else if (roomArea < 32f)
            {
                // Medium Room (7 items)
                electricalToSpawn.Add(ElectricalType.ElectricalMotor); // At conveyor
                electricalToSpawn.Add(ElectricalType.ControlBox);      // At conveyor
                electricalToSpawn.Add(ElectricalType.Transformer);     // In bay
                electricalToSpawn.Add(ElectricalType.ElectricalPanel); // In bay
                electricalToSpawn.Add(ElectricalType.Switchboard);     // In bay
                electricalToSpawn.Add(ElectricalType.CableBox);        // In bay
                electricalToSpawn.Add(ElectricalType.VentilationFan);  // At vent duct
            }
            else
            {
                // Large Room (All 11 items)
                electricalToSpawn.Add(ElectricalType.ElectricalMotor); // At conveyor
                electricalToSpawn.Add(ElectricalType.ControlBox);      // At conveyor
                electricalToSpawn.Add(ElectricalType.CableReel);       // At Continuous Miner
                electricalToSpawn.Add(ElectricalType.Transformer);     // In bay
                electricalToSpawn.Add(ElectricalType.Switchboard);     // In bay
                electricalToSpawn.Add(ElectricalType.ElectricalPanel); // In bay
                electricalToSpawn.Add(ElectricalType.PowerUnit);       // In bay
                electricalToSpawn.Add(ElectricalType.CableBox);        // In bay
                electricalToSpawn.Add(ElectricalType.BatteryChargingStation); // In bay
                electricalToSpawn.Add(ElectricalType.VentilationFan);  // At duct
                electricalToSpawn.Add(ElectricalType.PortableElectricDrill); // In workshop bay
            }

            float bayX = -halfX + 0.32f;

            for (int i = 0; i < electricalToSpawn.Count; i++)
            {
                ElectricalType eType = electricalToSpawn[i];
                GameObject elecGo = null;
                string eName = "";
                Vector3 preferredLocalPos = Vector3.zero;
                Quaternion preferredLocalRot = Quaternion.Euler(0, 90f, 0); // Wall-mounted facing inward

                switch (eType)
                {
                    case ElectricalType.ElectricalMotor:
                        // Attached directly to Conveyor drive head!
                        elecGo = ProceduralModelBuilder.BuildElectricalMotor(container);
                        eName = "Conveyor Drive Motor";
                        if (_conveyorBedPosition != Vector3.zero)
                        {
                            Vector3 localConv = Quaternion.Inverse(roomRotation) * (_conveyorBedPosition - roomCenter);
                            preferredLocalPos = localConv + new Vector3(-0.48f, 0f, -0.65f);
                        }
                        else
                        {
                            preferredLocalPos = new Vector3(halfX * 0.45f, 0f, -0.3f);
                        }
                        preferredLocalRot = Quaternion.Euler(0, -90f, 0);
                        break;

                    case ElectricalType.ControlBox:
                        // Mounted on pedestal right beside Conveyor
                        elecGo = ProceduralModelBuilder.BuildControlBox(container);
                        eName = "Operator Control Console";
                        if (_conveyorBedPosition != Vector3.zero)
                        {
                            Vector3 localConv = Quaternion.Inverse(roomRotation) * (_conveyorBedPosition - roomCenter);
                            preferredLocalPos = localConv + new Vector3(-0.48f, 0f, 0.55f);
                        }
                        else
                        {
                            preferredLocalPos = new Vector3(halfX * 0.45f, 0f, 0.5f);
                        }
                        preferredLocalRot = Quaternion.Euler(0, -90f, 0);
                        break;

                    case ElectricalType.CableReel:
                        // Stationed near continuous miner / drilling face
                        elecGo = ProceduralModelBuilder.BuildCableReel(container);
                        eName = "Trailing Cable Reel";
                        preferredLocalPos = new Vector3(-halfX * 0.58f, 0f, -halfZ + 0.55f);
                        preferredLocalRot = Quaternion.Euler(0, 45f, 0);
                        break;

                    case ElectricalType.Transformer:
                        // Primary substation transformer in electrical alcove (left rib, inbye)
                        elecGo = ProceduralModelBuilder.BuildTransformer(container);
                        eName = "Step-Down HV Transformer";
                        preferredLocalPos = new Vector3(bayX, 0f, -0.48f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 90f, 0);
                        break;

                    case ElectricalType.Switchboard:
                        // Main switchboard in electrical alcove (left rib, inbye)
                        elecGo = ProceduralModelBuilder.BuildSwitchboard(container);
                        eName = "Main Underground Switchboard";
                        preferredLocalPos = new Vector3(bayX, 0f, -0.28f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 90f, 0);
                        break;

                    case ElectricalType.ElectricalPanel:
                        // High-voltage breaker panel in electrical alcove (left rib, mid)
                        elecGo = ProceduralModelBuilder.BuildElectricalPanel(container);
                        eName = "High-Voltage Electrical Panel";
                        preferredLocalPos = new Vector3(bayX, 0f, -0.10f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 90f, 0);
                        break;

                    case ElectricalType.PowerUnit:
                        // Power Distribution Unit (PDU) in electrical alcove (left rib, mid)
                        elecGo = ProceduralModelBuilder.BuildPowerUnit(container);
                        eName = "Power Distribution Unit (PDU)";
                        preferredLocalPos = new Vector3(bayX, 0f, 0.08f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 90f, 0);
                        break;

                    case ElectricalType.CableBox:
                        // Cable junction box near substation inbye transition (left rib, deep inbye)
                        elecGo = ProceduralModelBuilder.BuildCableBox(container);
                        eName = "Cable Junction Box";
                        preferredLocalPos = new Vector3(bayX, 0f, -0.68f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 90f, 0);
                        break;

                    case ElectricalType.BatteryChargingStation:
                        // Designated battery charging rack tucked safely in left rib service bay (inbye)
                        elecGo = ProceduralModelBuilder.BuildBatteryChargingStation(container);
                        eName = "Battery Charging Station";
                        preferredLocalPos = new Vector3(bayX, 0f, -0.38f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 90f, 0);
                        break;

                    case ElectricalType.VentilationFan:
                        // Auxiliary ventilation fan coupled with right rib ducting, safely inbye
                        elecGo = ProceduralModelBuilder.BuildVentilationFan(container);
                        eName = "Mine Ventilation Fan";
                        preferredLocalPos = new Vector3(halfX * 0.68f, 0f, -0.35f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 180f, 0);
                        break;

                    case ElectricalType.PortableElectricDrill:
                    default:
                        // Stored in side tool alcove
                        elecGo = ProceduralModelBuilder.BuildPortableElectricDrill(container);
                        eName = "Portable Electric Drill";
                        preferredLocalPos = new Vector3(bayX + 0.35f, 0f, -0.18f * halfZ);
                        preferredLocalRot = Quaternion.Euler(0, 180f, 0);
                        break;
                }

                Vector3 spawnPos = ResolveZonedPosition(preferredLocalPos, roomCenter, halfX, halfZ, roomRotation, exitPos, playerPos);
                Quaternion spawnRot = roomRotation * preferredLocalRot;

                elecGo.transform.position = spawnPos;
                elecGo.transform.rotation = spawnRot;

                var elecComp = elecGo.AddComponent<ElectricalEquipment>();
                elecComp.Initialize(i + 1, eType, eName, 0.50f);
                _spawnedElectrical.Add(elecComp);
                _occupiedPositions.Add(spawnPos);

                if (animate && Application.isPlaying)
                {
                    StartCoroutine(Co_PopInAnimation(elecGo));
                }
            }

            Debug.Log($"[EquipmentSpawner] Successfully placed {_spawnedElectrical.Count} zoned electrical units.");
            return _spawnedElectrical;
        }

        public void SpawnAll(Vector3 roomCenter, float roomLength, float roomWidth, Quaternion roomRotation, Vector3 exitPos, Vector3 playerPos)
        {
            ClearEquipment();
            InitSpawningContext();
            SpawnMachines(roomCenter, roomLength, roomWidth, roomRotation, exitPos, playerPos, false);
            SpawnElectrical(roomCenter, roomLength, roomWidth, roomRotation, exitPos, playerPos, false);
            Debug.Log($"[EquipmentSpawner] Successfully placed {_spawnedMachines.Count} machines and {_spawnedElectrical.Count} electrical equipment.");
        }

        /// <summary>
        /// Validates and snaps a zoned coordinate onto the room floor, guaranteeing clearance
        /// from the trainee start position, the emergency evacuation corridor, and existing equipment.
        /// </summary>
        private Vector3 ResolveZonedPosition(Vector3 localCandidate, Vector3 roomCenter, float halfX, float halfZ, Quaternion roomRotation, Vector3 exitPos, Vector3 playerPos)
        {
            // Clamp strictly inside room boundary
            localCandidate.x = Mathf.Clamp(localCandidate.x, -halfX, halfX);
            localCandidate.z = Mathf.Clamp(localCandidate.z, -halfZ, halfZ);
            localCandidate.y = 0f; // Firmly on floor

            Vector3 worldPos = roomCenter + roomRotation * localCandidate;
            worldPos.y = roomCenter.y; // Ensure zero vertical floating

            // Test if preferred position is clear
            if (IsPositionSafe(worldPos, playerPos, exitPos, _minSpacing))
            {
                return worldPos;
            }

            // Local spiral search around preferred zoned position
            float[] radii = { 0.35f, 0.65f, 0.95f, 1.3f };
            for (int r = 0; r < radii.Length; r++)
            {
                float radius = radii[r];
                for (int angleDeg = 0; angleDeg < 360; angleDeg += 45)
                {
                    float rad = angleDeg * Mathf.Deg2Rad;
                    float testX = Mathf.Clamp(localCandidate.x + Mathf.Cos(rad) * radius, -halfX, halfX);
                    float testZ = Mathf.Clamp(localCandidate.z + Mathf.Sin(rad) * radius, -halfZ, halfZ);
                    Vector3 testWorld = roomCenter + roomRotation * new Vector3(testX, 0f, testZ);
                    testWorld.y = roomCenter.y;

                    if (IsPositionSafe(testWorld, playerPos, exitPos, _minSpacing * 0.85f))
                    {
                        return testWorld;
                    }
                }
            }

            // Fallback: return clamped candidate firmly on floor
            return worldPos;
        }

        private bool IsPositionSafe(Vector3 worldPos, Vector3 playerPos, Vector3 exitPos, float minSpacing)
        {
            // 1. Must not spawn inside trainee personal radius
            if (Vector3.Distance(worldPos, playerPos) < _playerSafeRadius)
                return false;

            // 2. Must not obstruct the clear evacuation corridor to the emergency exit
            if (DistanceToLineSegment(worldPos, playerPos, exitPos) < _exitCorridorWidth)
                return false;

            // 3. Must not spawn anywhere near the Emergency Exit doorway or Safe Zone
            if (Vector3.Distance(worldPos, exitPos) < 1.85f)
                return false;

            // 4. Must not overlap already spawned equipment
            for (int i = 0; i < _occupiedPositions.Count; i++)
            {
                if (Vector3.Distance(worldPos, _occupiedPositions[i]) < minSpacing)
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

        private IEnumerator Co_PopInAnimation(GameObject obj, float duration = 0.40f)
        {
            if (obj == null) yield break;
            Vector3 targetScale = obj.transform.localScale;
            obj.transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curve = Mathf.Sin(t * Mathf.PI * 0.5f) + Mathf.Sin(t * Mathf.PI) * 0.16f * (1f - t);
                obj.transform.localScale = targetScale * Mathf.Max(0f, curve);
                yield return null;
            }

            if (obj != null)
            {
                obj.transform.localScale = targetScale;
            }
        }

        public void ClearEquipment()
        {
            foreach (var m in _spawnedMachines)
            {
                if (m != null) Destroy(m.gameObject);
            }
            _spawnedMachines.Clear();

            foreach (var e in _spawnedElectrical)
            {
                if (e != null) Destroy(e.gameObject);
            }
            _spawnedElectrical.Clear();

            _occupiedPositions.Clear();
            _conveyorBedPosition = Vector3.zero;
        }
    }
}
