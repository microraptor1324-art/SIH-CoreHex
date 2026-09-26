using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARMiningSimulator.AR;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Coordinates the procedural generation of a realistic underground mining section:
    /// - Working face heading (inbye), side rock ribs, steel arch sets, roof bolting.
    /// - Overhead ventilation ducting, wall-routed electrical cable conduits.
    /// - Atmospheric caged industrial mine lighting and MSHA warning signage.
    /// - Unobstructed emergency evacuation corridor to Emergency Exit & Safe Zone.
    /// - Staged equipment generation with animated industrial pop-in.
    /// </summary>
    [RequireComponent(typeof(EquipmentSpawner))]
    public class MiningEnvironmentGenerator : MonoBehaviour
    {
        [Header("Default Fallback Dimensions (when testing without AR scan)")]
        [SerializeField] private float _defaultLength = 6.4f;
        [SerializeField] private float _defaultWidth = 4.8f;
        [SerializeField] private bool _autoGenerateOnStart = false;

        [Header("Staged Generation Settings")]
        [Tooltip("When enabled, generates the physical mine environment first, then spawns machinery and electrical equipment later.")]
        [SerializeField] private bool _delayEquipmentGeneration = true;
        [Tooltip("Delay in seconds after environment creation before machinery begins generating.")]
        [SerializeField] private float _equipmentDelaySeconds = 2.0f;
        [Tooltip("Stagger in seconds between spawning mining machines and electrical equipment.")]
        [SerializeField] private float _electricalStaggerSeconds = 1.0f;

        [Header("References")]
        [SerializeField] private EquipmentSpawner _equipmentSpawner;
        [SerializeField] private Camera _arCamera;
        [SerializeField] private ARAnchorManager _anchorManager;

        // Runtime Instances
        private GameObject _environmentRoot;
        private ARAnchor _environmentAnchor;
        private GameObject _emergencyExitObj;
        private GameObject _safeZoneObj;
        private GameObject _extinguisherStationObj;
        private GameObject _virtualFloorObj;

        private float _activeLength;
        private float _activeWidth;
        private Vector3 _activeCenter;
        private Quaternion _activeRotation;
        private bool _isEnvironmentReady = false;
        private bool _isEquipmentSpawned = false;
        private bool _isGenerated = false;
        private float _remainingEquipmentDelay = 0f;
        private Coroutine _stagedSpawnCoroutine = null;

        public float ActiveLength => _activeLength;
        public float ActiveWidth => _activeWidth;
        public float ActiveArea => _activeLength * _activeWidth;
        public Vector3 EmergencyExitPosition => _emergencyExitObj != null ? _emergencyExitObj.transform.position : Vector3.zero;
        public Vector3 SafeZonePosition => _safeZoneObj != null ? _safeZoneObj.transform.position : Vector3.zero;
        public Vector3 ExtinguisherStationPosition => _extinguisherStationObj != null ? _extinguisherStationObj.transform.position : SafeZonePosition;
        public GameObject ExtinguisherStationObj => _extinguisherStationObj;
        public bool IsEnvironmentReady => _isEnvironmentReady;
        public bool IsEquipmentSpawned => _isEquipmentSpawned;
        public bool IsGenerated => _isGenerated;
        public float RemainingEquipmentDelay => Mathf.Max(0f, _remainingEquipmentDelay);
        public EquipmentSpawner Spawner => _equipmentSpawner;

        public static event Action OnBaseEnvironmentReady;
        public static event Action OnEquipmentSpawned;
        public static event Action OnEnvironmentGenerated;

        private void Awake()
        {
            // Performance & Thermal Throttling Prevention:
            // Lock target frame rate to a smooth 60 FPS and prevent screen sleep
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (_equipmentSpawner == null)
                _equipmentSpawner = GetComponent<EquipmentSpawner>();
            if (_arCamera == null)
                _arCamera = Camera.main;

            if (_anchorManager == null)
            {
                _anchorManager = FindAnyObjectByType<ARAnchorManager>();
                if (_anchorManager == null)
                {
                    var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
                    if (origin != null)
                        _anchorManager = origin.gameObject.AddComponent<ARAnchorManager>();
                    else
                        _anchorManager = gameObject.AddComponent<ARAnchorManager>();
                }
            }
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

        /// <summary>
        /// Generates the base underground mining tunnel section FIRST (rock walls, arches, ducting, exit, safe zone),
        /// and schedules machinery & electrical equipment deployment.
        /// </summary>
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
                Debug.Log($"[MiningEnvironment] Generating underground mine drift inside room: {_activeLength:F2}m x {_activeWidth:F2}m ({ActiveArea:F1}m²)");
            }
            else
            {
                _activeLength = _defaultLength;
                _activeWidth = _defaultWidth;
                _activeCenter = Vector3.zero;
                _activeRotation = Quaternion.identity;
                Debug.Log($"[MiningEnvironment] Generating with default dimensions: {_activeLength:F2}m x {_activeWidth:F2}m");
            }

            // Spatial Anchoring: Lock the entire underground mine section to real-world SLAM feature points
            // This guarantees continuous tracking stability without drift or sudden teleportation
            AttachEnvironmentAnchor(new Pose(_activeCenter, _activeRotation));

            // 2. Build underground floor & rock tunnel infrastructure
            BuildVirtualFloor();
            BuildUndergroundTunnelStructures();

            // 3. Place Emergency Exit and Safe Zone at the outbye edge
            Vector3 farEdge = _activeCenter + _activeRotation * new Vector3(0, 0, (_activeLength * 0.5f) - 0.25f);
            BuildEmergencyExitAndSafeZone(farEdge);

            _isEnvironmentReady = true;
            _isEquipmentSpawned = false;
            OnBaseEnvironmentReady?.Invoke();
            Debug.Log("[MiningEnvironment] Base underground mine section created! Machinery will deploy in sequence.");

            // 4. Staged Equipment Generation: Machines and electrical equipment generate later
            if (_delayEquipmentGeneration && Application.isPlaying)
            {
                _stagedSpawnCoroutine = StartCoroutine(Co_DelayedEquipmentGeneration());
            }
            else
            {
                SpawnEquipmentImmediate();
            }
        }

        /// <summary>
        /// Immediately triggers machinery and electrical generation without waiting for the delay.
        /// </summary>
        public void DeployEquipmentNow()
        {
            if (!_isEnvironmentReady || _isEquipmentSpawned) return;

            if (_stagedSpawnCoroutine != null)
            {
                StopCoroutine(_stagedSpawnCoroutine);
                _stagedSpawnCoroutine = null;
            }
            _remainingEquipmentDelay = 0f;
            SpawnEquipmentImmediate();
        }

        private IEnumerator Co_DelayedEquipmentGeneration()
        {
            _remainingEquipmentDelay = _equipmentDelaySeconds;
            while (_remainingEquipmentDelay > 0f)
            {
                _remainingEquipmentDelay -= Time.deltaTime;
                yield return null;
            }
            _remainingEquipmentDelay = 0f;

            yield return StartCoroutine(Co_SpawnEquipmentSequenced());
        }

        private IEnumerator Co_SpawnEquipmentSequenced()
        {
            Vector3 playerPos = _arCamera != null ? _arCamera.transform.position : Vector3.zero;
            playerPos.y = _activeCenter.y;

            if (_equipmentSpawner != null)
            {
                _equipmentSpawner.ClearEquipment();
                _equipmentSpawner.InitSpawningContext();

                // Step A: First generate production mining machinery in their logical zones
                Debug.Log("[MiningEnvironment] Deploying mining machinery into production zones...");
                _equipmentSpawner.SpawnMachines(
                    _activeCenter,
                    _activeLength,
                    _activeWidth,
                    _activeRotation,
                    EmergencyExitPosition,
                    playerPos,
                    animate: true
                );

                // Step B: Short stagger before electrical systems
                if (_electricalStaggerSeconds > 0f)
                {
                    yield return new WaitForSeconds(_electricalStaggerSeconds);
                }

                // Step C: Then generate electrical substation equipment in the electrical alcove
                Debug.Log("[MiningEnvironment] Deploying electrical equipment into electrical alcove bay...");
                _equipmentSpawner.SpawnElectrical(
                    _activeCenter,
                    _activeLength,
                    _activeWidth,
                    _activeRotation,
                    EmergencyExitPosition,
                    playerPos,
                    animate: true
                );
            }

            _isEquipmentSpawned = true;
            _isGenerated = true;
            _stagedSpawnCoroutine = null;

            OnEquipmentSpawned?.Invoke();
            OnEnvironmentGenerated?.Invoke();
            Debug.Log("[MiningEnvironment] Complete: Underground mine section fully active with zoned equipment and clear evacuation route!");
        }

        private void SpawnEquipmentImmediate()
        {
            Vector3 playerPos = _arCamera != null ? _arCamera.transform.position : Vector3.zero;
            playerPos.y = _activeCenter.y;

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

            _isEquipmentSpawned = true;
            _isGenerated = true;
            OnEquipmentSpawned?.Invoke();
            OnEnvironmentGenerated?.Invoke();
            Debug.Log("[MiningEnvironment] Immediate generation complete!");
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
                floorMat.color = new Color(0.12f, 0.13f, 0.14f, 0.95f); // Dark industrial rock floor
                _virtualFloorObj.GetComponent<Renderer>().sharedMaterial = floorMat;
            }

            Collider col = _virtualFloorObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        /// <summary>
        /// Builds realistic underground tunnel elements:
        /// - Solid rock working face (inbye) & side rib walls
        /// - Structural steel mine arch sets spaced along drift length
        /// - Ceiling roof bolts
        /// - Overhead continuous spiral ventilation ducting
        /// - High-voltage wall cable conduit line
        /// - Industrial caged mine lights
        /// - Safety warning signs & floor evacuation markers
        /// </summary>
        private void BuildUndergroundTunnelStructures()
        {
            float halfW = _activeWidth * 0.5f;
            float halfL = _activeLength * 0.5f;
            float tunnelHeight = 2.4f;

            // 1. SOLID ROCK FACE AT INBYE (-Z)
            Vector3 facePos = _activeCenter + _activeRotation * new Vector3(0, 0, -halfL);
            Vector3 faceSize = new Vector3(_activeWidth, tunnelHeight, 0.35f);
            ProceduralModelBuilder.BuildRockWallSection(_environmentRoot.transform, faceSize, facePos, _activeRotation);

            // 2. LEFT & RIGHT ROCK RIB WALLS (-X and +X)
            Vector3 leftRibPos = _activeCenter + _activeRotation * new Vector3(-halfW, 0, 0);
            Vector3 rightRibPos = _activeCenter + _activeRotation * new Vector3(halfW, 0, 0);
            Vector3 ribSize = new Vector3(0.32f, tunnelHeight, _activeLength);
            ProceduralModelBuilder.BuildRockWallSection(_environmentRoot.transform, ribSize, leftRibPos, _activeRotation);
            ProceduralModelBuilder.BuildRockWallSection(_environmentRoot.transform, ribSize, rightRibPos, _activeRotation);

            // 3. STRUCTURAL STEEL MINE ARCH SETS
            int archCount = Mathf.Max(3, Mathf.RoundToInt(_activeLength / 1.75f));
            for (int i = 0; i < archCount; i++)
            {
                float zOffset = -halfL + 0.4f + (i * (_activeLength - 0.8f) / (archCount - 1));
                Vector3 archPos = _activeCenter + _activeRotation * new Vector3(0, 0, zOffset);
                GameObject arch = ProceduralModelBuilder.BuildMineArchSet(_environmentRoot.transform, _activeWidth - 0.35f, tunnelHeight);
                arch.transform.position = archPos;
                arch.transform.rotation = _activeRotation;

                // Mount industrial mine tunnel light fixture on every alternate arch
                if (i % 2 == 0)
                {
                    Vector3 lightPos = archPos + _activeRotation * new Vector3(0, tunnelHeight - 0.05f, 0);
                    ProceduralModelBuilder.BuildTunnelLightFixture(_environmentRoot.transform, lightPos, _activeRotation);
                }
            }

            // Unified Drift Ambient Illumination (Single soft warm point light optimized for mobile AR)
            GameObject driftLightObj = new GameObject("DriftAmbientFillLight");
            driftLightObj.transform.SetParent(_environmentRoot.transform, false);
            driftLightObj.transform.localPosition = _activeCenter + new Vector3(0, tunnelHeight - 0.25f, 0);
            Light driftLight = driftLightObj.AddComponent<Light>();
            driftLight.type = LightType.Point;
            driftLight.color = new Color(1.0f, 0.94f, 0.82f); // Warm mine drift incandescence
            driftLight.intensity = 1.15f;
            driftLight.range = Mathf.Max(_activeLength, _activeWidth) * 1.35f;
            driftLight.shadows = LightShadows.None; // Maximum mobile AR GPU performance

            // 4. CEILING ROOF BOLT PLATES (Grid pattern along roof)
            int boltRows = Mathf.Max(2, Mathf.RoundToInt(_activeLength / 1.25f));
            for (int r = 0; r < boltRows; r++)
            {
                float z = -halfL + 0.6f + (r * (_activeLength - 1.2f) / (boltRows - 1));
                // Left bolt, center bolt, right bolt
                Vector3 b1 = _activeCenter + _activeRotation * new Vector3(-halfW * 0.5f, tunnelHeight, z);
                Vector3 b2 = _activeCenter + _activeRotation * new Vector3(0, tunnelHeight, z);
                Vector3 b3 = _activeCenter + _activeRotation * new Vector3(halfW * 0.5f, tunnelHeight, z);
                ProceduralModelBuilder.BuildRoofBoltPlate(_environmentRoot.transform, b1);
                ProceduralModelBuilder.BuildRoofBoltPlate(_environmentRoot.transform, b2);
                ProceduralModelBuilder.BuildRoofBoltPlate(_environmentRoot.transform, b3);
            }

            // 5. OVERHEAD SPIRAL VENTILATION DUCTING (Along upper right rib)
            Vector3 ductStart = _activeCenter + _activeRotation * new Vector3(halfW - 0.42f, tunnelHeight - 0.35f, -halfL + 0.4f);
            Vector3 ductEnd = _activeCenter + _activeRotation * new Vector3(halfW - 0.42f, tunnelHeight - 0.35f, halfL - 0.4f);
            ProceduralModelBuilder.BuildVentilationDuctLine(_environmentRoot.transform, ductStart, ductEnd, 0.32f);

            // 6. HIGH-VOLTAGE WALL CABLE CONDUIT LINE (Along left electrical rib)
            Vector3 cableStart = _activeCenter + _activeRotation * new Vector3(-halfW + 0.22f, 0.85f, -halfL + 0.5f);
            Vector3 cableEnd = _activeCenter + _activeRotation * new Vector3(-halfW + 0.22f, 0.85f, halfL - 0.6f);
            ProceduralModelBuilder.BuildCableConduitLine(_environmentRoot.transform, cableStart, cableEnd, 0.055f);

            // 7. INDUSTRIAL WARNING SIGNBOARDS
            // A. Electrical Substation warning on left wall
            Vector3 signElecPos = _activeCenter + _activeRotation * new Vector3(-halfW + 0.18f, 1.45f, -0.2f);
            Quaternion signElecRot = _activeRotation * Quaternion.Euler(0, 90f, 0);
            ProceduralModelBuilder.BuildWarningSign(_environmentRoot.transform, "DANGER", "HIGH VOLTAGE SUBSTATION", signElecPos, signElecRot, new Color(0.95f, 0.18f, 0.18f));

            // B. Conveyor warning on right wall
            Vector3 signConvPos = _activeCenter + _activeRotation * new Vector3(halfW - 0.18f, 1.45f, -0.2f);
            Quaternion signConvRot = _activeRotation * Quaternion.Euler(0, -90f, 0);
            ProceduralModelBuilder.BuildWarningSign(_environmentRoot.transform, "CAUTION", "MOVING CONVEYOR BELT", signConvPos, signConvRot, new Color(0.98f, 0.72f, 0.05f));

            // C. Emergency Escape Route arrow near exit
            Vector3 signExitPos = _activeCenter + _activeRotation * new Vector3(-halfW + 0.18f, 1.45f, halfL - 1.2f);
            ProceduralModelBuilder.BuildWarningSign(_environmentRoot.transform, "ESCAPE ROUTE", "TO EMERGENCY EXIT ->", signExitPos, signElecRot, new Color(0.12f, 0.82f, 0.45f));

            // 8. FLOOR EVACUATION DIRECTIONAL MARKERS
            int markerCount = Mathf.Max(2, Mathf.RoundToInt(_activeLength / 1.8f));
            for (int m = 0; m < markerCount; m++)
            {
                float zM = -halfL + 1.2f + (m * (_activeLength - 2.0f) / Mathf.Max(1, markerCount - 1));
                // Center-right walking lane towards exit
                Vector3 markerPos = _activeCenter + _activeRotation * new Vector3(0.15f, 0.005f, zM);
                ProceduralModelBuilder.BuildFloorEscapeMarker(_environmentRoot.transform, markerPos, _activeRotation);
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

            // Fire Extinguisher Station: placed directly beside the Safety Area / Emergency Exit
            Vector3 extinguisherPos = exitPos + _activeRotation * new Vector3(0.85f, 0, 0.35f);
            Quaternion extinguisherRot = _activeRotation * Quaternion.Euler(0, -90f, 0);
            _extinguisherStationObj = ProceduralModelBuilder.BuildExtinguisherStation(_environmentRoot.transform, extinguisherPos, extinguisherRot);
        }

        public void Regenerate()
        {
            GenerateEnvironment();
        }

        public void ClearEnvironment()
        {
            if (_stagedSpawnCoroutine != null)
            {
                StopCoroutine(_stagedSpawnCoroutine);
                _stagedSpawnCoroutine = null;
            }

            _remainingEquipmentDelay = 0f;
            _isEnvironmentReady = false;
            _isEquipmentSpawned = false;
            _isGenerated = false;

            if (_equipmentSpawner != null)
                _equipmentSpawner.ClearEquipment();

            if (_environmentRoot != null)
            {
                Destroy(_environmentRoot);
                _environmentRoot = null;
            }

            _emergencyExitObj = null;
            _safeZoneObj = null;
            _extinguisherStationObj = null;
            _virtualFloorObj = null;

            if (_environmentAnchor != null)
            {
                if (_anchorManager != null && _anchorManager.enabled && _anchorManager.subsystem != null)
                {
                    try { _anchorManager.TryRemoveAnchor(_environmentAnchor); } catch { }
                }
                else
                {
                    Destroy(_environmentAnchor.gameObject);
                }
                _environmentAnchor = null;
            }
        }

        private async void AttachEnvironmentAnchor(Pose anchorPose)
        {
            if (_anchorManager == null || !_anchorManager.enabled || _anchorManager.subsystem == null || !_anchorManager.subsystem.running)
                return;

            try
            {
                var result = await _anchorManager.TryAddAnchorAsync(anchorPose);
                if (result.status.IsSuccess() && result.value != null && _environmentRoot != null)
                {
                    _environmentAnchor = result.value;
                    _environmentRoot.transform.SetParent(_environmentAnchor.transform, true);
                    Debug.Log("[MiningEnvironment] Successfully anchored virtual mine to ARAnchor via TryAddAnchorAsync.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MiningEnvironment] ARAnchor attachment skipped: {ex.Message}");
            }
        }
    }
}
