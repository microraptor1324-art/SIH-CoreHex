using UnityEngine;

namespace ARMiningSimulator.Environment
{
    public enum ElectricalType
    {
        ElectricalPanel,
        ControlBox,
        PowerUnit,
        CableBox,
        Transformer,
        ElectricalMotor,
        Switchboard,
        CableReel,
        PortableElectricDrill,
        BatteryChargingStation,
        VentilationFan
    }

    /// <summary>
    /// Component attached to spawned electrical equipment.
    /// Tracks ID, type, bounds, and provides hooks for the fire system and investigation stages.
    /// </summary>
    [SelectionBase]
    public class ElectricalEquipment : MonoBehaviour
    {
        [Header("Equipment Info")]
        [SerializeField] private int _equipmentId;
        [SerializeField] private ElectricalType _electricalType;
        [SerializeField] private string _equipmentName;
        [SerializeField] private float _footprintRadius = 0.45f;

        [Header("Status")]
        [SerializeField] private bool _isOnFire = false;
        [SerializeField] private bool _isInvestigated = false;

        public int EquipmentId => _equipmentId;
        public ElectricalType Type => _electricalType;
        public string EquipmentName => _equipmentName;
        public float FootprintRadius => _footprintRadius;
        public bool IsOnFire { get => _isOnFire; set => _isOnFire = value; }
        public bool IsInvestigated { get => _isInvestigated; set => _isInvestigated = value; }

        public void Initialize(int id, ElectricalType type, string name, float radius)
        {
            _equipmentId = id;
            _electricalType = type;
            _equipmentName = name;
            _footprintRadius = radius;
            gameObject.name = $"Electrical_{id}_{name}";
        }
    }
}
