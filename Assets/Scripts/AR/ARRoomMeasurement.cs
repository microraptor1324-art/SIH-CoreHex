using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMiningSimulator.AR
{
    public enum MeasurementSystemState
    {
        ScanningFloor = 0,       // Waiting for horizontal floor plane detection
        ReadyForFlag1 = 1,       // Floor detected and stable. Ready to place Flag 1
        MeasuringSide1 = 2,      // Flag 1 placed. Live line to reticle. Aim at Corner 2
        MeasuringSide2 = 3,      // Flag 2 placed. Live line to reticle. Aim at Corner 3
        MeasuringSide3 = 4,      // Flag 3 placed. Live line to reticle. Aim at Corner 4
        FourCornersMeasured = 5, // Exactly 4 flags placed! [ ADD FLAG ] disabled. Ready to [ CLOSE ROOM ]
        RoomClosed = 6,          // Final side connected, shaded floor mesh generated, rectangle analyzed
        Confirmed = 7            // Confirmed and exported to RoomData
    }

    /// <summary>
    /// Authoritative Master AR Room Measurement System.
    /// Strictly limits measurement to a normal rectangular room using EXACTLY 4 FLAGS.
    /// Follows the real AR measuring app principle:
    /// SCAN FLOOR -> POINT AT CORNER -> ADD FLAG (1/4) -> (2/4) -> (3/4) -> (4/4) -> CLOSE ROOM -> SHADE FLOOR -> RECTANGULAR ANALYSIS.
    /// </summary>
    [RequireComponent(typeof(ARRaycastController))]
    [RequireComponent(typeof(ARFlagManager))]
    [RequireComponent(typeof(MeasurementLineManager))]
    [RequireComponent(typeof(RoomPolygonManager))]
    public class ARRoomMeasurement : MonoBehaviour
    {
        [Header("Subsystem Components")]
        [SerializeField] private ARRaycastController _raycastController;
        [SerializeField] private ARFlagManager _flagManager;
        [SerializeField] private MeasurementLineManager _lineManager;
        [SerializeField] private RoomPolygonManager _polygonManager;

        [Header("AR Foundation References")]
        [SerializeField] private ARSession _arSession;
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private AROcclusionManager _occlusionManager;
        [SerializeField] private Camera _arCamera;

        // Measurement State
        private MeasurementSystemState _state = MeasurementSystemState.ScanningFloor;
        private RoomGeometryResult _geometryResult;
        private RoomRectangleAnalysis _rectAnalysis;
        private bool _isRoomClosed = false;
        private bool _isConfirmed = false;
        private string _statusMessage = "Scan the floor";

        [Header("Debug Settings")]
        [SerializeField] private bool _verboseDebugLogs = true;

        // Events
        public event Action<float, float, float> OnDimensionsChanged;
        public event Action<RoomData> OnMeasurementConfirmed;
        public event Action<MeasurementSystemState> OnStateChanged;

        // Public Properties
        public MeasurementSystemState State => _state;
        public float Length => _geometryResult.Length;
        public float Width => _geometryResult.Width;
        public float Area => _geometryResult.Area;
        public float Perimeter => _geometryResult.Perimeter;
        public Vector3 Center => _geometryResult.Center;
        public Quaternion Rotation => _geometryResult.Rotation;
        public RoomRectangleAnalysis RectangleAnalysis => _rectAnalysis;
        public bool IsRectangular => _rectAnalysis.IsRectangular;
        public int FlagCount => _flagManager != null ? _flagManager.Count : 0;
        public bool IsRoomClosed => _isRoomClosed;
        public bool IsConfirmed => _isConfirmed;
        public bool HasValidMeasurement => _isRoomClosed && _geometryResult.Area > 0.5f;

        // Validation Properties
        public bool IsTrackingStable => _arSession != null && ARSession.state == ARSessionState.SessionTracking;
        public bool CanAddFlag => FlagCount < 4 && !_isRoomClosed && (_raycastController != null && (_raycastController.HasValidFloorHit || _raycastController.HasCalibratedFloor));
        public bool CanCloseRoom => FlagCount == 4 && !_isRoomClosed;

        // Subsystems Passthrough
        public ARRaycastController RaycastController => _raycastController;
        public ARFlagManager FlagManager => _flagManager;
        public MeasurementLineManager LineManager => _lineManager;
        public string StatusMessage => _statusMessage;

        private void Awake()
        {
            EnsureSubsystems();
        }

        private void EnsureSubsystems()
        {
            if (_raycastController == null)
                _raycastController = GetComponent<ARRaycastController>() ?? gameObject.AddComponent<ARRaycastController>();
            if (_flagManager == null)
                _flagManager = GetComponent<ARFlagManager>() ?? gameObject.AddComponent<ARFlagManager>();
            if (_lineManager == null)
                _lineManager = GetComponent<MeasurementLineManager>() ?? gameObject.AddComponent<MeasurementLineManager>();
            if (_polygonManager == null)
                _polygonManager = GetComponent<RoomPolygonManager>() ?? gameObject.AddComponent<RoomPolygonManager>();

            if (_arSession == null)
                _arSession = FindAnyObjectByType<ARSession>();
            if (_planeManager == null)
                _planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (_arCamera == null)
                _arCamera = Camera.main;

            if (_planeManager != null)
            {
                // Prioritize Horizontal plane detection for floor scanning
                _planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            }

            if (_occlusionManager == null && _arCamera != null)
            {
                _occlusionManager = _arCamera.GetComponent<AROcclusionManager>() ?? _arCamera.gameObject.AddComponent<AROcclusionManager>();
                _occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            }

            if (_arCamera != null)
            {
                var poseDriver = _arCamera.GetComponent<ARCameraPoseDriver>() ?? _arCamera.gameObject.AddComponent<ARCameraPoseDriver>();
                poseDriver.CalibrateXROrigin();
            }

            var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null)
            {
                origin.CameraYOffset = 0f;
                origin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device;
                if (origin.CameraFloorOffsetObject != null)
                {
                    origin.CameraFloorOffsetObject.transform.localPosition = Vector3.zero;
                    origin.CameraFloorOffsetObject.transform.localRotation = Quaternion.identity;
                }
            }
        }

        private void Update()
        {
            UpdateSystemState();
            UpdateLivePreviewLines();
        }

        private void UpdateSystemState()
        {
            if (_state == MeasurementSystemState.Confirmed) return;

            if (!IsTrackingStable)
            {
                _statusMessage = "Tracking initializing...";
                return;
            }

            int planeCount = _planeManager != null ? _planeManager.trackables.count : 0;

            if (_state == MeasurementSystemState.ScanningFloor)
            {
                if (planeCount == 0 || !_raycastController.HasValidFloorHit)
                {
                    _statusMessage = "Scan the floor";
                }
                else
                {
                    SetState(MeasurementSystemState.ReadyForFlag1);
                }
            }
            else if (_state == MeasurementSystemState.ReadyForFlag1)
            {
                _statusMessage = "Tap to place corner 1";
            }
            else if (_state == MeasurementSystemState.MeasuringSide1)
            {
                _statusMessage = "Tap to place corner 2";
            }
            else if (_state == MeasurementSystemState.MeasuringSide2)
            {
                _statusMessage = "Tap to place corner 3";
            }
            else if (_state == MeasurementSystemState.MeasuringSide3)
            {
                _statusMessage = "Tap to place corner 4";
            }
            else if (_state == MeasurementSystemState.FourCornersMeasured || _state == MeasurementSystemState.RoomClosed)
            {
                _statusMessage = "Room measured";
            }
        }

        private void UpdateLivePreviewLines()
        {
            if (_isRoomClosed)
            {
                _lineManager.HidePreviewLine();
                return;
            }

            int count = _flagManager.Count;

            // When flags 1, 2, or 3 are placed, trace live line from latest flag to current reticle
            if (count >= 1 && count <= 3)
            {
                Vector3 aimPos = _raycastController != null ? _raycastController.GetBestFloorSpot() : Vector3.zero;
                if (aimPos != Vector3.zero)
                {
                    Vector3 lastPos = _flagManager.Flags[count - 1].Position;
                    _lineManager.UpdatePreviewLine(lastPos, aimPos);
                }
                else
                {
                    _lineManager.HidePreviewLine();
                }
            }
            else
            {
                _lineManager.HidePreviewLine();
            }
        }

        /// <summary>
        /// Places a persistent numbered 3D flag at the current floor spot.
        /// Adds a flag on that spot every time "ADD FLAG" is pressed.
        /// Traces a line from one point to another, and automatically closes the room with 50% blue shade when 4 points are placed.
        /// </summary>
        public bool AddFlagAtCrosshair()
        {
            if (_flagManager.Count >= 4)
            {
                Debug.LogWarning("[ARRoomMeasurement] Exactly 4 flags allowed for rectangular room measurement.");
                return false;
            }

            if (_isRoomClosed)
            {
                Debug.LogWarning("[ARRoomMeasurement] Room is already closed.");
                return false;
            }

            if (_raycastController == null || (!_raycastController.HasValidFloorHit && !_raycastController.HasCalibratedFloor))
            {
                Debug.LogWarning("[ARRoomMeasurement] Cannot add flag: point at the floor first.");
                return false;
            }

            Vector3 hitPos = _raycastController.GetBestFloorSpot();
            if (hitPos == Vector3.zero)
            {
                Debug.LogWarning("[ARRoomMeasurement] Floor position could not be determined.");
                return false;
            }

            // Strictly lock the flag's vertical elevation to the authoritative/calibrated floor plane
            float floorY = _raycastController.IsFloorLocked ? _raycastController.LockedFloorY :
                          (_raycastController.HasCalibratedFloor ? _raycastController.CalibratedFloorY : hitPos.y);
            hitPos = new Vector3(hitPos.x, floorY, hitPos.z);

            Pose hitPose = new Pose(hitPos, Quaternion.identity);
            ARPlane hitPlane = _raycastController.CurrentHitPlane ?? _raycastController.CalibratedFloorPlane;

            // Lock authoritative floor plane upon confirming Corner 1
            if (_flagManager.Count == 0 && _raycastController != null)
            {
                _raycastController.LockFloorPlane(hitPlane, floorY);
            }

            // 1. Add persistent anchored 3D surveyor flag
            MeasurementFlag newFlag = _flagManager.AddFlag(hitPos, hitPose, hitPlane);
            int newCount = _flagManager.Count;

            // 2. Connect 3D measurement line from previous flag
            if (newCount >= 2)
            {
                Vector3 prevPos = _flagManager.Flags[newCount - 2].Position;
                _lineManager.AddSegment(prevPos, newFlag.Position);
            }

            if (_verboseDebugLogs)
            {
                Debug.Log($"[ARRoomMeasurement] Placed Corner #{newCount} / 4:" +
                          $"\n  - Position: {hitPos}" +
                          $"\n  - Floor Y: {floorY:F4}" +
                          $"\n  - Hit Plane: {(hitPlane != null ? hitPlane.trackableId.ToString() : "Calibrated")}" +
                          $"\n  - Plane Alignment: {(hitPlane != null ? hitPlane.alignment.ToString() : "HorizontalUp")}");
            }

            // 3. User requirement: when all 4 points are made, apply 50% blue color shade over closed area
            if (newCount == 4)
            {
                CloseRoom();
            }
            else
            {
                // Advance state machine
                if (newCount == 1)
                    SetState(MeasurementSystemState.MeasuringSide1);
                else if (newCount == 2)
                    SetState(MeasurementSystemState.MeasuringSide2);
                else if (newCount == 3)
                    SetState(MeasurementSystemState.MeasuringSide3);
            }

            return true;
        }

        /// <summary>
        /// Connects FLAG 4 back to FLAG 1, shades the measured floor with 50% blue semi-transparent material,
        /// verifies rectangular room geometry, and computes exact area.
        /// </summary>
        public bool CloseRoom()
        {
            if (_flagManager.Count != 4)
            {
                Debug.LogWarning($"[ARRoomMeasurement] Need exactly 4 flags to close rectangular room (current: {_flagManager.Count}).");
                return false;
            }

            if (_isRoomClosed) return true;

            // 1. Retrieve the 4 world-space corner points and ensure clean perimeter order (no crossing diagonals)
            List<Vector3> points = _flagManager.GetPointPositions();
            points = EnsureQuadrilateralPerimeter(points);

            // 2. Connect Flag 4 back to Flag 1 if not already connected
            if (_lineManager.SegmentCount < 4)
            {
                Vector3 flag4Pos = points[3];
                Vector3 flag1Pos = points[0];
                _lineManager.ConnectClosingSegment(flag4Pos, flag1Pos);
            }

            // 3. Generate procedural flat shaded floor mesh covering the 4 points with 50% blue shade
            _polygonManager.CreateShadedFloorPolygon(points);

            // 4. Analyze rectangular room geometry and compute area
            _rectAnalysis = RoomAreaCalculator.AnalyzeRectangle(points);
            _geometryResult = RoomAreaCalculator.CalculateGeometry(points);
            _isRoomClosed = true;

            SetState(MeasurementSystemState.RoomClosed);
            OnDimensionsChanged?.Invoke(_geometryResult.Length, _geometryResult.Width, _geometryResult.Area);

            if (_verboseDebugLogs)
            {
                Debug.Log($"[ARRoomMeasurement] Room Closed! Rectangular: {_rectAnalysis.IsRectangular}, Area: {_geometryResult.Area:F2} m², Length: {_geometryResult.Length:F2} m, Width: {_geometryResult.Width:F2} m, Perimeter: {_geometryResult.Perimeter:F2} m");
            }
            return true;
        }

        /// <summary>
        /// Ensures the 4 corner points connect along the room's outer perimeter rather than crossing diagonals through the center.
        /// Preserves the user's selected point positions while ordering connections cleanly.
        /// </summary>
        private List<Vector3> EnsureQuadrilateralPerimeter(List<Vector3> pts)
        {
            if (pts == null || pts.Count != 4) return pts;

            Vector2 p0 = new Vector2(pts[0].x, pts[0].z);
            Vector2 p1 = new Vector2(pts[1].x, pts[1].z);
            Vector2 p2 = new Vector2(pts[2].x, pts[2].z);
            Vector2 p3 = new Vector2(pts[3].x, pts[3].z);

            // Check if diagonals/edges cross through the center
            bool crosses = RoomAreaCalculator.SegmentsIntersect(p0, p1, p2, p3) ||
                           RoomAreaCalculator.SegmentsIntersect(p1, p2, p3, p0);

            if (!crosses) return pts; // Already a clean perimeter

            if (_verboseDebugLogs)
                Debug.Log("[ARRoomMeasurement] Crossed diagonals detected. Reordering perimeter to avoid crossing through center.");

            // Calculate 2D centroid
            Vector2 c = (p0 + p1 + p2 + p3) * 0.25f;

            // Preserve P0 as first point, and sort remaining 3 points by angle around centroid
            Vector3 pt0 = pts[0];
            float baseAngle = Mathf.Atan2(p0.y - c.y, p0.x - c.x);

            List<Vector3> sorted = new List<Vector3>(pts);
            sorted.Sort((a, b) =>
            {
                if (a == pt0) return -1;
                if (b == pt0) return 1;

                float angA = Mathf.Atan2(a.z - c.y, a.x - c.x) - baseAngle;
                while (angA < 0) angA += Mathf.PI * 2f;

                float angB = Mathf.Atan2(b.z - c.y, b.x - c.x) - baseAngle;
                while (angB < 0) angB += Mathf.PI * 2f;

                return angA.CompareTo(angB);
            });

            // Re-render lines to follow uncrossed perimeter P1 -> P2 -> P3 -> P4 -> P1
            _lineManager.ClearAllLines();
            for (int i = 0; i < 3; i++)
            {
                _lineManager.AddSegment(sorted[i], sorted[i + 1]);
            }
            _lineManager.ConnectClosingSegment(sorted[3], sorted[0]);

            return sorted;
        }

        /// <summary>
        /// Removes the most recently placed flag, its line segment, and measurement.
        /// </summary>
        public void UndoLastFlag()
        {
            if (_isRoomClosed)
            {
                // Re-open room and remove Flag 4 so user can re-measure corner 4
                _isRoomClosed = false;
                _polygonManager.ClearShadedFloorPolygon();
                _lineManager.RemoveLastSegment(); // Removes closing segment P4 -> P1
                _flagManager.RemoveLastFlag();    // Removes Flag 4
                _lineManager.RemoveLastSegment(); // Removes segment P3 -> P4
                SetState(MeasurementSystemState.MeasuringSide3);
                return;
            }

            int count = _flagManager.Count;
            if (count > 0)
            {
                _flagManager.RemoveLastFlag();
                _lineManager.RemoveLastSegment();

                int remaining = _flagManager.Count;
                if (remaining == 0)
                {
                    if (_raycastController != null) _raycastController.UnlockFloorPlane();
                    SetState(MeasurementSystemState.ReadyForFlag1);
                }
                else if (remaining == 1)
                    SetState(MeasurementSystemState.MeasuringSide1);
                else if (remaining == 2)
                    SetState(MeasurementSystemState.MeasuringSide2);
                else if (remaining == 3)
                    SetState(MeasurementSystemState.MeasuringSide3);
            }
        }

        /// <summary>
        /// Completely clears all flags, AR anchors, line segments, distance labels, and floor shaded mesh.
        /// </summary>
        public void ResetMeasurement()
        {
            _flagManager.ClearAllFlags();
            _lineManager.ClearAllLines();
            _polygonManager.ClearShadedFloorPolygon();
            if (_raycastController != null)
            {
                _raycastController.UnlockFloorPlane();
            }

            _isRoomClosed = false;
            _isConfirmed = false;
            _geometryResult = new RoomGeometryResult(0f, 0f, 0f, 0f, Vector3.zero, Quaternion.identity);
            _rectAnalysis = default;

            RoomData.Reset();
            SetState(MeasurementSystemState.ScanningFloor);
            OnDimensionsChanged?.Invoke(0f, 0f, 0f);

            Debug.Log("[ARRoomMeasurement] Reset measurement system to initial scanning.");
        }

        /// <summary>
        /// Confirms measurement and exports RoomData to spawn downstream SIH mining scenario.
        /// </summary>
        public bool ConfirmMeasurement()
        {
            if (!HasValidMeasurement)
            {
                Debug.LogWarning("[ARRoomMeasurement] Cannot confirm measurement: room must be closed with valid 4 flags.");
                return false;
            }

            _isConfirmed = true;
            SetState(MeasurementSystemState.Confirmed);

            var roomData = new RoomData(
                _geometryResult.Length,
                _geometryResult.Width,
                _geometryResult.Center,
                _geometryResult.Rotation,
                _flagManager.GetPointPositions().ToArray(),
                confirmed: true
            );

            RoomData.SetCurrent(roomData);
            OnMeasurementConfirmed?.Invoke(roomData);

            Debug.Log($"[ARRoomMeasurement] Room measurement confirmed! Area={roomData.Area:F2}m², Length={roomData.Length:F2}m, Width={roomData.Width:F2}m");
            return true;
        }

        private void SetState(MeasurementSystemState newState)
        {
            _state = newState;
            OnStateChanged?.Invoke(_state);
        }
    }
}
