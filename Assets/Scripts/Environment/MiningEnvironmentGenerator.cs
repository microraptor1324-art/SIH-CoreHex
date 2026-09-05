using System;
using UnityEngine;
using ARMiningSimulator.AR;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Coordinates the generation of the virtual underground mining training environment.
    /// Builds the virtual floor, corner support pillars, emergency exit, safe zone,
    /// and triggers EquipmentSpawner to populate 5 machines and 7 electrical units.
    /// </summary>
    [RequireComponent(typeof(EquipmentSpawner))]
    public class MiningEnvironmentGenerator : MonoBehaviour
    {
        [Header("Default Fallback Dimensions (when testing without AR scan)")]
        [SerializeField] private float _defaultLength = 5.2f;
        [SerializeField] private float _defaultWidth = 4.2f;
        [SerializeField] private bool _autoGenerateOnStart = false;

        [Header("References")]
        [SerializeField] private EquipmentSpawner _equipmentSpawner;
        [SerializeField] private Camera _arCamera;

        // Runtime Instances
        private GameObject _environmentRoot;
        private GameObject _emergencyExitObj;
        private GameObject _safeZoneObj;
        private GameObject _virtualFloorObj;

        private float _activeLength;
        private float _activeWidth;
        private Vector3 _activeCenter;
        private Quaternion _activeRotation;
        private bool _isGenerated = false;

        public float ActiveLength => _activeLength;
        public float ActiveWidth => _activeWidth;
        public float ActiveArea => _activeLength * _activeWidth;
        public Vector3 EmergencyExitPosition => _emergencyExitObj != null ? _emergencyExitObj.transform.position : Vector3.zero;
        public Vector3 SafeZonePosition => _safeZoneObj != null ? _safeZoneObj.transform.position : Vector3.zero;
        public bool IsGenerated => _isGenerated;
        public EquipmentSpawner Spawner => _equipmentSpawner;

        public static event Action OnEnvironmentGenerated;

        private void Awake()
        {
            if (_equipmentSpawner == null)
                _equipmentSpawner = GetComponent<EquipmentSpawner>();
            if (_arCamera == null)
                _arCamera = Camera.main;
        }

        private void OnEnable()
        {
            RoomData.OnRoomDataUpdated += HandleRoomDataUpdated;
        }

        private void OnDisable()
        {
            RoomData.OnRoomDataUpdated -= HandleRoomDataUpdated;
        }

        private void HandleRoomDataUpdated(RoomData data)
        {
            if (data != null && data.IsConfirmed)
            {
                GenerateEnvironment();
            }
        }

        private void Start()
        {
            if (_autoGenerateOnStart)
            {
                GenerateEnvironment();
            }
        }

        public void GenerateEnvironment()
        {
            ClearEnvironment();

            _environmentRoot = new GameObject("VirtualMiningEnvironment");
            _environmentRoot.transform.SetParent(transform, false);

            // 1. Resolve room dimensions
            if (RoomData.Current != null && RoomData.Current.IsValid && RoomData.Current.IsConfirmed)
            {
                _activeLength = RoomData.Current.Length;
                _activeWidth = RoomData.Current.Width;
                _activeCenter = RoomData.Current.Center;
                _activeRotation = RoomData.Current.Rotation;
                Debug.Log($"[MiningEnvironment] Generating inside confirmed real room: {_activeLength:F2}m x {_activeWidth:F2}m");
            }
            else
            {
                _activeLength = _defaultLength;
                _activeWidth = _defaultWidth;
                _activeCenter = Vector3.zero;
                _activeRotation = Quaternion.identity;
                Debug.Log($"[MiningEnvironment] Generating with default testing dimensions: {_activeLength:F2}m x {_activeWidth:F2}m");
            }

            Vector3 playerPos = _arCamera != null ? _arCamera.transform.position : Vector3.zero;
            playerPos.y = _activeCenter.y; // Flatten to floor level

            // 2. Build virtual floor & boundary curbs
            BuildVirtualFloor();
            BuildPerimeterSupports();

            // 3. Place Emergency Exit and Safe Zone at the far edge
            Vector3 farEdge = _activeCenter + _activeRotation * new Vector3(0, 0, (_activeLength * 0.5f) - 0.25f);
            BuildEmergencyExitAndSafeZone(farEdge);

            // 4. Spawn 5 mining machines and 7 electrical units
            if (_equipmentSpawner != null)
            {
                _equipmentSpawner.SpawnAll(
                    _activeCenter,
                    _activeLength,
                    _activeWidth,
                    _activeRotation,
                    EmergencyExitPosition,
                    playerPos
                );
            }

            _isGenerated = true;
            OnEnvironmentGenerated?.Invoke();
            Debug.Log("[MiningEnvironment] Generation complete!");
        }

        private void BuildVirtualFloor()
        {
            _virtualFloorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _virtualFloorObj.name = "VirtualFloor_Curb";
            _virtualFloorObj.transform.SetParent(_environmentRoot.transform, false);
            _virtualFloorObj.transform.position = _activeCenter - new Vector3(0, 0.02f, 0);
            _virtualFloorObj.transform.rotation = _activeRotation;
            _virtualFloorObj.transform.localScale = new Vector3(_activeWidth, 0.02f, _activeLength);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material floorMat = new Material(shader);
                floorMat.color = new Color(0.13f, 0.14f, 0.16f, 0.85f); // Dark industrial rock/concrete
                _virtualFloorObj.GetComponent<Renderer>().sharedMaterial = floorMat;
            }

            Collider col = _virtualFloorObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private void BuildPerimeterSupports()
        {
            float halfX = _activeWidth * 0.5f;
            float halfZ = _activeLength * 0.5f;

            Vector3[] cornerOffsets = new Vector3[]
            {
                new Vector3(-halfX, 0, -halfZ),
                new Vector3(halfX, 0, -halfZ),
                new Vector3(halfX, 0, halfZ),
                new Vector3(-halfX, 0, halfZ)
            };

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material pillarMat = null;
            if (shader != null)
            {
                pillarMat = new Material(shader) { color = new Color(0.35f, 0.25f, 0.15f) }; // Timber / mine shaft support
            }

            for (int i = 0; i < cornerOffsets.Length; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"MineShaftPillar_{i + 1}";
                pillar.transform.SetParent(_environmentRoot.transform, false);
                pillar.transform.position = _activeCenter + _activeRotation * (cornerOffsets[i] + new Vector3(0, 0.6f, 0));
                pillar.transform.localScale = new Vector3(0.18f, 0.6f, 0.18f);
                if (pillarMat != null) pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;

                Collider c = pillar.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }
        }

        private void BuildEmergencyExitAndSafeZone(Vector3 exitPos)
        {
            // Emergency Exit doorway
            _emergencyExitObj = ProceduralModelBuilder.BuildEmergencyExit(_environmentRoot.transform);
            _emergencyExitObj.transform.position = exitPos;
            _emergencyExitObj.transform.rotation = _activeRotation;
            _emergencyExitObj.AddComponent<EmergencyAlarmBeacon>();

            // Safe Zone pad: placed just through the doorway
            Vector3 safeZonePos = exitPos + _activeRotation * new Vector3(0, 0, 0.65f);
            _safeZoneObj = ProceduralModelBuilder.BuildSafeZone(_environmentRoot.transform);
            _safeZoneObj.transform.position = safeZonePos;
            _safeZoneObj.transform.rotation = _activeRotation;
        }

        public void Regenerate()
        {
            GenerateEnvironment();
        }

        public void ClearEnvironment()
        {
            if (_equipmentSpawner != null)
                _equipmentSpawner.ClearEquipment();

            if (_environmentRoot != null)
            {
                Destroy(_environmentRoot);
                _environmentRoot = null;
            }

            _isGenerated = false;
        }
    }
}
