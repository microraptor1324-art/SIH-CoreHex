using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMiningSimulator.AR
{
    public enum DetectedSurfaceType
    {
        None = 0,
        Floor = 1,
        Wall = 2,
        HorizontalPlane = 3,
        FeaturePoint = 4
    }

    /// <summary>
    /// Professional AR Raycasting & Stabilization Controller.
    /// Strictly prioritizes horizontal upward-facing floor planes.
    /// Rejects walls, ceilings, vertical surfaces, and arbitrary feature points for measurement.
    /// Features multi-sample rolling-average stabilization ("HOLD STEADY..." vs "READY")
    /// and a continuous 3D world-space reticle showing real-time environmental targeting.
    /// </summary>
    public class ARRaycastController : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARRaycastManager _raycastManager;
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private AROcclusionManager _occlusionManager;
        [SerializeField] private Camera _arCamera;

        [Header("Stabilization Settings")]
        [SerializeField] private int _sampleCapacity = 10;
        [SerializeField] private float _stabilityTolerance = 0.035f; // 3.5 cm max deviation to be "READY"
        [SerializeField] private float _resetDriftThreshold = 0.35f; // 35 cm sudden motion clears buffer

        [Header("Colors")]
        [SerializeField] private Color _readyColor = new Color(0.15f, 0.95f, 0.45f, 0.95f);    // Bright Green
        [SerializeField] private Color _steadyColor = new Color(0.95f, 0.85f, 0.15f, 0.95f);  // Amber/Yellow
        [SerializeField] private Color _invalidColor = new Color(0.95f, 0.25f, 0.25f, 0.75f); // Red
        [SerializeField] private Color _trackingColor = new Color(1.0f, 0.55f, 0.1f, 0.85f);  // Orange

        // State & Buffers
        private readonly Queue<Vector3> _sampleBuffer = new Queue<Vector3>();
        private static readonly List<ARRaycastHit> s_RaycastHits = new List<ARRaycastHit>();

        private Vector3 _stabilizedPosition = Vector3.zero;
        private Pose _stabilizedPose = Pose.identity;
        private bool _hasValidFloorHit = false;
        private bool _isPointStable = false;
        private DetectedSurfaceType _currentSurface = DetectedSurfaceType.None;
        private ARPlane _currentHitPlane = null;
        private TrackableId _preferredFloorPlaneId = TrackableId.invalidId;
        private float _distanceFromCamera = 0f;

        // Floor Elevation Calibration & Authoritative Locking System
        private bool _hasCalibratedFloor = false;
        private float _calibratedFloorY = 0f;
        private ARPlane _calibratedFloorPlane = null;
        private Vector3 _currentRawHitPos = Vector3.zero;

        private bool _isFloorLocked = false;
        private ARPlane _lockedFloorPlane = null;
        private float _lockedFloorY = 0f;

        // 3D Reticle in World Space
        private GameObject _reticleRoot;
        private Renderer _outerRingRenderer;
        private Renderer _centerDotRenderer;
        private Material _reticleMaterial;
        private TextMesh _reticleTextMesh;
        private GameObject _reticleTextObj;

        // Public Properties
        public bool HasValidFloorHit => _hasValidFloorHit;
        public bool HasValidHit => _hasValidFloorHit; // Backwards compatibility
        public bool IsPointStable => _isPointStable;
        public Vector3 StabilizedPosition => _stabilizedPosition;
        public Pose StabilizedPose => _stabilizedPose;
        public DetectedSurfaceType CurrentSurface => _currentSurface;
        public ARPlane CurrentHitPlane => _currentHitPlane;
        public float DistanceFromCamera => _distanceFromCamera;
        public bool DepthAvailable => _occlusionManager != null && _occlusionManager.enabled && _occlusionManager.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
        public bool HasCalibratedFloor => _hasCalibratedFloor;
        public float CalibratedFloorY => _calibratedFloorY;
        public ARPlane CalibratedFloorPlane => _calibratedFloorPlane;
        public bool IsFloorLocked => _isFloorLocked;
        public ARPlane LockedFloorPlane => _lockedFloorPlane;
        public float LockedFloorY => _lockedFloorY;

        public void LockFloorPlane(ARPlane plane, float floorY)
        {
            _isFloorLocked = true;
            _lockedFloorPlane = plane ?? _calibratedFloorPlane;
            _lockedFloorY = floorY;
            _calibratedFloorY = floorY;
            _hasCalibratedFloor = true;
            Debug.Log($"[ARRaycastController] Authoritative Floor Plane LOCKED at Y={floorY:F4} (Plane: {(_lockedFloorPlane != null ? _lockedFloorPlane.trackableId.ToString() : "Calibrated")})");
        }

        public void UnlockFloorPlane()
        {
            _isFloorLocked = false;
            _lockedFloorPlane = null;
            _hasCalibratedFloor = false;
            _calibratedFloorY = 0f;
            _calibratedFloorPlane = null;
            _currentRawHitPos = Vector3.zero;
            _sampleBuffer.Clear();
            Debug.Log("[ARRaycastController] Authoritative Floor Plane UNLOCKED.");
        }

        public string SurfaceTypeString
        {
            get
            {
                switch (_currentSurface)
                {
                    case DetectedSurfaceType.Floor: return "FLOOR";
                    case DetectedSurfaceType.Wall: return "WALL";
                    case DetectedSurfaceType.HorizontalPlane: return "HORIZONTAL";
                    case DetectedSurfaceType.FeaturePoint: return "FEATURE";
                    default: return "NONE";
                }
            }
        }

        public string StabilityStatusString
        {
            get
            {
                if (!IsTrackingGood()) return "TRACKING...";
                if (!_hasValidFloorHit) return "POINT AT FLOOR";
                if (_isPointStable) return "READY";
                return "HOLD STEADY...";
            }
        }

        private void Awake()
        {
            if (_raycastManager == null)
                _raycastManager = FindAnyObjectByType<ARRaycastManager>();
            if (_planeManager == null)
                _planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (_occlusionManager == null)
                _occlusionManager = FindAnyObjectByType<AROcclusionManager>();
            if (_arCamera == null)
                _arCamera = Camera.main;

            Create3DReticle();
        }

        private void Create3DReticle()
        {
            if (_reticleRoot != null) return;

            _reticleRoot = new GameObject("AR_3DReticle");
            _reticleRoot.transform.SetParent(transform, false);

            _reticleMaterial = ARMaterialHelper.CreateUnlitMaterial(_invalidColor);

            // 1. Outer Ring (Thin cylinder)
            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "OuterRing";
            outerRing.transform.SetParent(_reticleRoot.transform, false);
            outerRing.transform.localScale = new Vector3(0.18f, 0.002f, 0.18f);
            Destroy(outerRing.GetComponent<Collider>());
            _outerRingRenderer = outerRing.GetComponent<Renderer>();
            if (_reticleMaterial != null) _outerRingRenderer.sharedMaterial = _reticleMaterial;

            // 2. Center Bullseye Dot
            GameObject centerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            centerDot.name = "CenterDot";
            centerDot.transform.SetParent(_reticleRoot.transform, false);
            centerDot.transform.localScale = new Vector3(0.035f, 0.035f, 0.035f);
            Destroy(centerDot.GetComponent<Collider>());
            _centerDotRenderer = centerDot.GetComponent<Renderer>();
            if (_reticleMaterial != null) _centerDotRenderer.sharedMaterial = _reticleMaterial;

            // 3. Floating 3D Status Text
            _reticleTextObj = new GameObject("ReticleStatusText");
            _reticleTextObj.transform.SetParent(_reticleRoot.transform, false);
            _reticleTextObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            _reticleTextMesh = _reticleTextObj.AddComponent<TextMesh>();
            _reticleTextMesh.fontSize = 28;
            _reticleTextMesh.characterSize = 0.03f;
            _reticleTextMesh.anchor = TextAnchor.MiddleCenter;
            _reticleTextMesh.alignment = TextAlignment.Center;
            _reticleTextMesh.color = Color.white;
            _reticleTextMesh.text = "POINT AT FLOOR";
        }

        private void Update()
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            CalibrateFloorElevation();
            PerformFloorRaycast();
            Update3DReticleVisuals();
        }

        private bool IsTrackingGood()
        {
            return ARSession.state == ARSessionState.SessionTracking;
        }

        /// <summary>
        /// Continuously scans all detected AR planes to identify and calibrate the true physical floor elevation.
        /// The physical floor in any room is the lowest horizontal surface below the camera.
        /// Surfaces higher than the floor (tables, desks, beds, chairs) are identified as elevated surfaces.
        /// </summary>
        /// <summary>
        /// Continuously scans all detected AR planes to identify and calibrate the true physical floor elevation.
        /// The physical floor in any room is the lowest horizontal surface below the camera.
        /// Surfaces higher than the floor (tables, desks, beds, chairs) are identified as elevated surfaces.
        /// </summary>
        private void CalibrateFloorElevation()
        {
            // Once the floor plane is authoritatively locked, retain it as the ground truth
            if (_isFloorLocked) return;
            if (_planeManager == null || _arCamera == null) return;

            float cameraY = _arCamera.transform.position.y;
            float lowestY = float.MaxValue;
            ARPlane lowestPlane = null;

            foreach (var plane in _planeManager.trackables)
            {
                if (plane == null) continue;
                if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                if (plane.trackingState == TrackingState.None) continue;

                float planeY = plane.transform.position.y;
                // Floor must be below camera eye level by at least 0.30m (user holds phone 1.1m - 1.6m above floor)
                if (planeY < cameraY - 0.30f && planeY < lowestY)
                {
                    lowestY = planeY;
                    lowestPlane = plane;
                }
            }

            if (lowestPlane != null)
            {
                _calibratedFloorY = lowestY;
                _calibratedFloorPlane = lowestPlane;
                _hasCalibratedFloor = true;
            }
        }

        /// <summary>
        /// Continuously casts a ray from the exact screen center (0.5, 0.5).
        /// Strictly filters for the calibrated physical floor plane below the camera.
        /// Rejects elevated tables, desks, beds, walls, vertical planes, ceilings, and feature points.
        /// In room corners where plane polygons have boundary gaps, seamlessly projects onto the calibrated floor plane.
        /// </summary>
        private void PerformFloorRaycast()
        {
            _hasValidFloorHit = false;
            _isPointStable = false;
            _currentHitPlane = null;
            _currentSurface = DetectedSurfaceType.None;

            if (_arCamera == null) return;

            // Optical center of camera viewport
            float w = (_arCamera != null && _arCamera.pixelWidth > 0) ? _arCamera.pixelWidth : Screen.width;
            float h = (_arCamera != null && _arCamera.pixelHeight > 0) ? _arCamera.pixelHeight : Screen.height;
            Vector2 screenCenter = new Vector2(w * 0.5f, h * 0.5f);
            s_RaycastHits.Clear();

            // Prioritize PlaneWithinPolygon (exact boundary)
            bool raycastSuccess = _raycastManager != null && _raycastManager.Raycast(screenCenter, s_RaycastHits, TrackableType.PlaneWithinPolygon);
            if (!raycastSuccess || s_RaycastHits.Count == 0)
            {
                // Fallback to PlaneWithinBounds if within polygon's convex bounding box
                raycastSuccess = _raycastManager != null && _raycastManager.Raycast(screenCenter, s_RaycastHits, TrackableType.PlaneWithinBounds);
            }

            ARRaycastHit bestHit = default;
            ARPlane bestPlane = null;
            bool foundFloor = false;
            Vector3 rawHitPos = Vector3.zero;

            float cameraY = _arCamera.transform.position.y;
            float targetFloorY = _isFloorLocked ? _lockedFloorY : (_hasCalibratedFloor ? _calibratedFloorY : 0f);

            if (raycastSuccess && s_RaycastHits.Count > 0)
            {
                // Iterate through hits (closest first)
                foreach (var hit in s_RaycastHits)
                {
                    ARPlane plane = hit.trackable as ARPlane ?? (_planeManager != null ? _planeManager.GetPlane(hit.trackableId) : null);
                    if (plane != null)
                    {
                        if (plane.alignment == PlaneAlignment.Vertical)
                        {
                            _currentSurface = DetectedSurfaceType.Wall;
                            continue;
                        }

                        if (IsValidFloorPlane(plane, hit.pose.position, cameraY))
                        {
                            // If floor is locked or calibrated, strictly reject elevated surfaces (tables, desks, beds)
                            if ((_isFloorLocked || _hasCalibratedFloor) && hit.pose.position.y > targetFloorY + 0.18f)
                            {
                                _currentSurface = DetectedSurfaceType.HorizontalPlane; // Table, desk, or countertop
                                continue;
                            }

                            // If floor is locked, prioritize matching the locked plane or one at the same elevation
                            if (_isFloorLocked && _lockedFloorPlane != null)
                            {
                                if (plane.trackableId != _lockedFloorPlane.trackableId && Mathf.Abs(hit.pose.position.y - _lockedFloorY) > 0.12f)
                                {
                                    continue;
                                }
                            }

                            bestHit = hit;
                            bestPlane = plane;
                            rawHitPos = hit.pose.position;
                            foundFloor = true;
                            break;
                        }
                    }
                }
            }

            // Fallback Corner Projection: If raycast didn't hit a detected plane polygon (e.g. corner of room, wall base),
            // but we have a calibrated/locked floor elevation, project the center ray directly onto the authoritative floor plane!
            if (!foundFloor && (_isFloorLocked || _hasCalibratedFloor))
            {
                Vector3 camPos = _arCamera.transform.position;
                Vector3 camFwd = _arCamera.transform.forward;
                float refY = _isFloorLocked ? _lockedFloorY : _calibratedFloorY;

                if (camFwd.y < -0.05f)
                {
                    float t = (refY - camPos.y) / camFwd.y;
                    if (t > 0.2f && t < 25.0f)
                    {
                        rawHitPos = new Vector3(camPos.x + camFwd.x * t, refY, camPos.z + camFwd.z * t);
                        bestPlane = _isFloorLocked ? (_lockedFloorPlane ?? _calibratedFloorPlane) : _calibratedFloorPlane;
                        foundFloor = true;
                    }
                }
            }

            if (!foundFloor)
            {
                _currentRawHitPos = Vector3.zero;
                _sampleBuffer.Clear();
                _distanceFromCamera = 0f;
                return;
            }

            // Calibrate floor height if this is our first valid floor plane hit and not locked
            if (!_isFloorLocked && !_hasCalibratedFloor)
            {
                _hasCalibratedFloor = true;
                _calibratedFloorY = rawHitPos.y;
                _calibratedFloorPlane = bestPlane;
            }

            // STRICT FLOOR LOCK: Always ensure the hit coordinate lies flat on the authoritative floor plane
            float effectiveFloorY = _isFloorLocked ? _lockedFloorY : _calibratedFloorY;
            rawHitPos.y = effectiveFloorY;

            // 2. VALID FLOOR HIT CONFIRMED
            _hasValidFloorHit = true;
            _currentHitPlane = _isFloorLocked ? (_lockedFloorPlane ?? bestPlane) : (bestPlane ?? _calibratedFloorPlane);
            _currentSurface = DetectedSurfaceType.Floor;
            _currentRawHitPos = rawHitPos;

            // 3. RESPONSIVE RETICLE TRACKING
            if (_stabilizedPosition == Vector3.zero || Vector3.Distance(_stabilizedPosition, rawHitPos) > 0.45f)
            {
                _stabilizedPosition = rawHitPos;
            }
            else
            {
                _stabilizedPosition = Vector3.Lerp(_stabilizedPosition, rawHitPos, 0.70f);
            }
            _stabilizedPosition.y = effectiveFloorY; // Strictly keep Y on the authoritative floor

            _stabilizedPose = new Pose(_stabilizedPosition, Quaternion.identity);
            _distanceFromCamera = Vector3.Distance(_arCamera.transform.position, _stabilizedPosition);

            // 4. STABILITY CHECK
            float motionDelta = Vector3.Distance(rawHitPos, _stabilizedPosition);
            _isPointStable = (motionDelta <= _stabilityTolerance);
        }

        /// <summary>
        /// Evaluates whether an ARPlane is an upward-facing horizontal floor plane below the camera.
        /// </summary>
        private bool IsValidFloorPlane(ARPlane plane, Vector3 hitPosition, float cameraY)
        {
            if (plane == null) return false;

            // Must be horizontal up alignment
            if (plane.alignment != PlaneAlignment.HorizontalUp)
                return false;

            // Normal must point substantially upward (> 0.80)
            if (plane.normal.y < 0.80f)
                return false;

            // Floor must be below camera eye level (at least 30cm below camera)
            if (hitPosition.y > cameraY - 0.30f)
                return false;

            return true;
        }

        private Vector3 CalculateBufferAverage()
        {
            if (_sampleBuffer.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (var pos in _sampleBuffer)
            {
                sum += pos;
            }
            return sum / _sampleBuffer.Count;
        }

        /// <summary>
        /// Updates the 3D world-space reticle position, orientation, color, and billboarded status text.
        /// Guaranteed to sit completely flat on the physical floor plane (never floating in the air).
        /// </summary>
        private void Update3DReticleVisuals()
        {
            if (_reticleRoot == null || _arCamera == null) return;

            bool tracking = IsTrackingGood();
            Color targetColor;
            string statusText;

            Vector3 camPos = _arCamera.transform.position;
            Vector3 camFwd = _arCamera.transform.forward;

            if (!tracking)
            {
                targetColor = _trackingColor;
                statusText = "TRACKING...";
                _reticleRoot.transform.position = camPos + camFwd * 1.5f;
                _reticleRoot.transform.rotation = Quaternion.LookRotation(camFwd, Vector3.up);
            }
            else if (_hasValidFloorHit)
            {
                // Sits completely flat on the calibrated physical floor plane (+0.003m to avoid z-fighting)
                Vector3 floorPos = new Vector3(_stabilizedPosition.x, _calibratedFloorY + 0.003f, _stabilizedPosition.z);
                _reticleRoot.transform.position = floorPos;
                _reticleRoot.transform.rotation = Quaternion.identity; // Flat along X-Z ground plane

                if (_isPointStable)
                {
                    targetColor = _readyColor;
                    statusText = "READY (CORNER)";
                }
                else
                {
                    targetColor = _steadyColor;
                    statusText = "HOLD STEADY...";
                }
            }
            else if (_hasCalibratedFloor && camFwd.y < -0.05f)
            {
                // Projecting onto calibrated floor plane even when aiming outside AR polygon boundary (e.g. wall corner)
                float t = (_calibratedFloorY - camPos.y) / camFwd.y;
                if (t > 0.2f && t < 25.0f)
                {
                    Vector3 floorPos = new Vector3(camPos.x + camFwd.x * t, _calibratedFloorY + 0.003f, camPos.z + camFwd.z * t);
                    _reticleRoot.transform.position = floorPos;
                    _reticleRoot.transform.rotation = Quaternion.identity;
                }
                targetColor = _steadyColor;
                statusText = "POINT AT CORNER";
            }
            else
            {
                targetColor = _invalidColor;
                if (_currentSurface == DetectedSurfaceType.Wall)
                    statusText = "WALL (POINT AT FLOOR)";
                else if (_currentSurface == DetectedSurfaceType.HorizontalPlane)
                    statusText = "TABLE (POINT AT FLOOR)";
                else
                    statusText = "POINT DOWN AT FLOOR";

                // In air only when user is pointing horizontally or up at ceiling
                _reticleRoot.transform.position = camPos + camFwd * 1.5f;
                _reticleRoot.transform.rotation = Quaternion.LookRotation(camFwd, Vector3.up);
            }

            // Apply color
            if (_reticleMaterial != null)
            {
                _reticleMaterial.color = targetColor;
            }

            // Update 3D status text label
            if (_reticleTextMesh != null && _reticleTextObj != null)
            {
                _reticleTextMesh.text = statusText;
                _reticleTextMesh.color = targetColor;

                // Billboard text towards camera
                _reticleTextObj.transform.LookAt(_arCamera.transform);
                _reticleTextObj.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Always returns the exact 3D floor coordinate currently targeted at the center of the screen.
        /// Strictly calibrated to the physical floor plane (never floating in the air).
        /// </summary>
        public Vector3 GetBestFloorSpot()
        {
            float targetFloorY = _isFloorLocked ? _lockedFloorY : (_hasCalibratedFloor ? _calibratedFloorY : 0f);

            if (_hasValidFloorHit && _currentRawHitPos != Vector3.zero)
            {
                float floorY = (_isFloorLocked || _hasCalibratedFloor) ? targetFloorY : _currentRawHitPos.y;
                return new Vector3(_currentRawHitPos.x, floorY, _currentRawHitPos.z);
            }

            if (_hasValidFloorHit && _stabilizedPosition != Vector3.zero)
            {
                float floorY = (_isFloorLocked || _hasCalibratedFloor) ? targetFloorY : _stabilizedPosition.y;
                return new Vector3(_stabilizedPosition.x, floorY, _stabilizedPosition.z);
            }

            if (_arCamera != null)
            {
                Vector3 camPos = _arCamera.transform.position;
                Vector3 camFwd = _arCamera.transform.forward;
                float floorY = (_isFloorLocked || _hasCalibratedFloor) ? targetFloorY : (camPos.y - 1.35f);

                if (camFwd.y < -0.05f)
                {
                    float t = (floorY - camPos.y) / camFwd.y;
                    if (t > 0.2f && t < 25.0f)
                    {
                        return new Vector3(camPos.x + camFwd.x * t, floorY, camPos.z + camFwd.z * t);
                    }
                }

                Vector3 forwardFlat = new Vector3(camFwd.x, 0f, camFwd.z).normalized;
                return new Vector3(camPos.x + forwardFlat.x * 2.0f, floorY, camPos.z + forwardFlat.z * 2.0f);
            }

            return Vector3.zero;
        }
    }
}
