using UnityEngine;

namespace ARMiningSimulator.Environment
{
    public enum MachineType
    {
        MiningDrill,
        Conveyor,
        Excavator,
        ContinuousMiner,
        Scooptram,
        RoofBolter
    }

    /// <summary>
    /// Component attached to spawned mining machines.
    /// Tracks ID, type, bounds, and provides hooks for the fire system and investigation stages.
    /// </summary>
    [SelectionBase]
    public class MiningMachine : MonoBehaviour
    {
        [Header("Machine Info")]
        [SerializeField] private int _machineId;
        [SerializeField] private MachineType _machineType;
        [SerializeField] private string _machineName;
        [SerializeField] private float _footprintRadius = 0.65f;

        [Header("Status")]
        [SerializeField] private bool _isOnFire = false;
        [SerializeField] private bool _isInvestigated = false;

        public int MachineId => _machineId;
        public MachineType Type => _machineType;
        public string MachineName => _machineName;
        public float FootprintRadius => _footprintRadius;
        public bool IsOnFire { get => _isOnFire; set => _isOnFire = value; }
        public bool IsInvestigated { get => _isInvestigated; set => _isInvestigated = value; }

        public void Initialize(int id, MachineType type, string name, float radius)
        {
            _machineId = id;
            _machineType = type;
            _machineName = name;
            _footprintRadius = radius;
            gameObject.name = $"Machine_{id}_{name}";
        }
    }
}
