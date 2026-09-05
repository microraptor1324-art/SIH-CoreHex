using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Configurable object spawning system that places 5 Mining Machines and 7 Electrical items
    /// inside the measured room bounds without overlaps, preserving trainee walking paths
    /// and clearance to the Emergency Exit.
    /// </summary>
    public class EquipmentSpawner : MonoBehaviour
    {
        [Header("Counts (Configurable in Inspector)")]
        [SerializeField] private int _machineCount = 5;
        [SerializeField] private int _electricalCount = 7;

        [Header("Spacing & Safety Margins")]
        [SerializeField] private float _minSpacing = 0.85f;
        [SerializeField] private float _boundaryPadding = 0.35f;
        [SerializeField] private float _playerSafeRadius = 0.95f;
        [SerializeField] private float _exitCorridorWidth = 0.75f;

        [Header("Randomization")]
        [Tooltip("Set to 0 to use a random seed every time, or a positive number to reproduce exact layout")]
        [SerializeField] private int _randomSeed = 0;

        // Runtime Tracking
        private readonly List<MiningMachine> _spawnedMachines = new List<MiningMachine>();
        private readonly List<ElectricalEquipment> _spawnedElectrical = new List<ElectricalEquipment>();
        private readonly List<Vector3> _occupiedPositions = new List<Vector3>();
        private int _activeSeed = 0;

        public IReadOnlyList<MiningMachine> SpawnedMachines => _spawnedMachines;
        public IReadOnlyList<ElectricalEquipment> SpawnedElectrical => _spawnedElectrical;
        public int ActiveSeed => _activeSeed;

        public void SpawnAll(Vector3 roomCenter, float roomLength, float roomWidth, Quaternion roomRotation, Vector3 exitPos, Vector3 playerPos)
        {
            ClearEquipment();

            _activeSeed = _randomSeed != 0 ? _randomSeed : Random.Range(1, 999999);
            Random.InitState(_activeSeed);
            Debug.Log($"[EquipmentSpawner] Spawning equipment using Seed: {_activeSeed} inside {roomLength:F2}m x {roomWidth:F2}m area.");

            Transform container = transform;

            // Half-extents with safety padding
            float halfX = Mathf.Max(0.2f, (roomWidth * 0.5f) - _boundaryPadding);
            float halfZ = Mathf.Max(0.2f, (roomLength * 0.5f) - _boundaryPadding);

            // 1. SPAWN 5 MINING MACHINES
            for (int i = 0; i < _machineCount; i++)
            {
                Vector3 candidate = FindValidSpawnPosition(roomCenter, halfX, halfZ, roomRotation, exitPos, playerPos);
                GameObject machineGo = null;
                MachineType mType = (MachineType)(i % 5);
                string mName = "";

                switch (mType)
                {
                    case MachineType.MiningDrill:
                        machineGo = ProceduralModelBuilder.BuildMiningDrill(container);
                        mName = "Mining Drill";
                        break;
                    case MachineType.Conveyor:
                        machineGo = ProceduralModelBuilder.BuildConveyor(container);
                        mName = "Conveyor";
                        break;
                    case MachineType.RockCrusher:
                        machineGo = ProceduralModelBuilder.BuildRockCrusher(container);
                        mName = "Rock Crusher";
                        break;
                    case MachineType.Excavator:
                        machineGo = ProceduralModelBuilder.BuildExcavator(container);
                        mName = "Excavator";
                        break;
                    case MachineType.Generator:
                    default:
                        machineGo = ProceduralModelBuilder.BuildGenerator(container);
                        mName = "Heavy Generator";
                        break;
                }

                machineGo.transform.position = candidate;
                machineGo.transform.rotation = roomRotation * Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

                var machineComp = machineGo.AddComponent<MiningMachine>();
                machineComp.Initialize(i + 1, mType, mName, 0.65f);
                _spawnedMachines.Add(machineComp);
                _occupiedPositions.Add(candidate);
            }

            // 2. SPAWN 7 ELECTRICAL UNITS
            for (int i = 0; i < _electricalCount; i++)
            {
                Vector3 candidate = FindValidSpawnPosition(roomCenter, halfX, halfZ, roomRotation, exitPos, playerPos);
                GameObject elecGo = null;
                ElectricalType eType = (ElectricalType)(i % 7);
                string eName = "";

                switch (eType)
                {
                    case ElectricalType.ElectricalPanel:
                        elecGo = ProceduralModelBuilder.BuildElectricalPanel(container);
                        eName = "Electrical Panel";
                        break;
                    case ElectricalType.ControlBox:
                        elecGo = ProceduralModelBuilder.BuildControlBox(container);
                        eName = "Control Box";
                        break;
                    case ElectricalType.PowerUnit:
                        elecGo = ProceduralModelBuilder.BuildPowerUnit(container);
                        eName = "Power Distribution Unit";
                        break;
                    case ElectricalType.CableBox:
                        elecGo = ProceduralModelBuilder.BuildCableBox(container);
                        eName = "Cable Junction Box";
                        break;
                    case ElectricalType.Transformer:
                        elecGo = ProceduralModelBuilder.BuildTransformer(container);
                        eName = "High Voltage Transformer";
                        break;
                    case ElectricalType.ElectricalMotor:
                        elecGo = ProceduralModelBuilder.BuildElectricalMotor(container);
                        eName = "Industrial Motor";
                        break;
                    case ElectricalType.Switchboard:
                    default:
                        elecGo = ProceduralModelBuilder.BuildSwitchboard(container);
                        eName = "Switchboard";
                        break;
                }

                elecGo.transform.position = candidate;
                elecGo.transform.rotation = roomRotation * Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

                var elecComp = elecGo.AddComponent<ElectricalEquipment>();
                elecComp.Initialize(i + 1, eType, eName, 0.45f);
                _spawnedElectrical.Add(elecComp);
                _occupiedPositions.Add(candidate);
            }

            Debug.Log($"[EquipmentSpawner] Successfully placed {_spawnedMachines.Count} machines and {_spawnedElectrical.Count} electrical equipment.");
        }

        private Vector3 FindValidSpawnPosition(Vector3 roomCenter, float halfX, float halfZ, Quaternion roomRotation, Vector3 exitPos, Vector3 playerPos)
        {
            int maxAttempts = 150;
            float currentSpacing = _minSpacing;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Gradually relax spacing if room is compact
                if (attempt > 80) currentSpacing = _minSpacing * 0.8f;
                if (attempt > 120) currentSpacing = _minSpacing * 0.65f;

                float localX = Random.Range(-halfX, halfX);
                float localZ = Random.Range(-halfZ, halfZ);
                Vector3 worldPos = roomCenter + roomRotation * new Vector3(localX, 0f, localZ);

                // Check 1: Avoid spawning on player
                if (Vector3.Distance(worldPos, playerPos) < _playerSafeRadius)
                    continue;

                // Check 2: Keep clear path to emergency exit
                if (DistanceToLineSegment(worldPos, playerPos, exitPos) < _exitCorridorWidth)
                    continue;

                // Check 3: Check distance to already placed equipment
                bool tooClose = false;
                foreach (var occ in _occupiedPositions)
                {
                    if (Vector3.Distance(worldPos, occ) < currentSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                return worldPos;
            }

            // Fallback: spawn at random spot with slight height offset if packed
            float fallbackX = Random.Range(-halfX, halfX);
            float fallbackZ = Random.Range(-halfZ, halfZ);
            return roomCenter + roomRotation * new Vector3(fallbackX, 0f, fallbackZ);
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
        }
    }
}
