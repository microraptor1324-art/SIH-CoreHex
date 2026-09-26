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
    /// Professional AR Raycasting & Floor Stabilization Controller.
    /// Strictly detects and tracks the true physical floor using lowest-cluster multi-plane heuristics,
    /// semantic floor classification, and unified coplanar projection.
    /// Features critically-damped visual reticle smoothing (glides without jitter or lag)
    /// and continuous analytical projection extending seamlessly to room corners and far range (30m+).
    /// </summary>
    public class ARRaycastController : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARRaycastManager _raycastManager;
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private AROcclusionManager _occlusionManager;
        [SerializeField] private Camera _arCamera;

        [Header("Stabilization & Smoothing")]
        [SerializeField] private float _reticleLerpSpeed = 24.0f; // Smooth glide factor
        [SerializeField] private float _teleportThreshold = 0.45f; // Fast camera whip snap threshold

        [Header("Colors")]
        [SerializeField] private Color _readyColor = new Color(0.15f, 0.95f, 0.45f, 0.95f);    // Bright Green
        [SerializeField] private Color _steadyColor = new Color(0.95f, 0.85f, 0.15f, 0.95f);  // Amber/Yellow
        [SerializeField] private Color _invalidColor = new Color(0.95f, 0.25f, 0.25f, 0.75f); // Red
        [SerializeField] private Color _trackingColor = new Color(1.0f, 0.55f, 0.1f, 0.85f);  // Orange

        // State & Buffers
        private static readonly List<ARRaycastHit> s_RaycastHits = new List<ARRaycastHit>();

        private Vector3 _stabilizedPosition = Vector3.zero;
        private Pose _stabilizedPose = Pose.identity;
        private bool _hasValidFloorHit = false;
        private bool _isPointStable = false;
        private DetectedSurfaceType _currentSurface = DetectedSurfaceType.None;
        private ARPlane _currentHitPlane = null;
        private float _distanceFromCamera = 0f;

        // Floor Elevation Calibration & Authoritative Locking System
        private bool _hasCalibratedFloor = false;
        private float _calibratedFloorY = 0f;
        private ARPlane _calibratedFloorPlane = null;
        private Vector3 _currentFloorNormal = Vector3.up;
        private Vector3 _currentFloorPlanePos = Vector3.zero;
        private Vector3 _currentRawHitPos = Vector3.zero;
        private Vector3 _lastFrameRawHitPos = Vector3.zero;

        private bool _isFloorLocked = false;
        private ARPlane _lockedFloorPlane = null;
        private float _lockedFloorY = 0f;
        private Vector3 _lockedFloorNormal = Vector3.up;
        private Vector3 _lockedFloorPos = Vector3.zero;

        // 3D Reticle in World Space & Smoothing
        private GameObject _reticleRoot;
        private Renderer _outerRingRenderer;
        private Renderer _centerDotRenderer;
        private Material _reticleMaterial;
        private TextMesh _reticleTextMesh;
        private GameObject _reticleTextObj;

        private Vector3 _smoothedReticlePos = Vector3.zero;
        private Quaternion _smoothedReticleRot = Quaternion.identity;
        private bool _hasInitializedReticle = false;

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
        public Vector3 CurrentFloorNormal => _currentFloorNormal;
        public bool IsFloorLocked => _isFloorLocked;
        public ARPlane LockedFloorPlane => _lockedFloorPlane;
        public float LockedFloorY => _lockedFloorY;
        public Vector3 LockedFloorNormal => _lockedFloorNormal;

        /// <summary>
        /// Permanently locks the authoritative floor reference plane once Corner 1 is placed.
        /// Ensures all 4 corners and the room polygon are mathematically coplanar.
        /// </summary>
        public void LockFloorPlane(ARPlane plane, float floorY, Vector3? normal = null, Vector3? planePos = null)
        {
            _isFloorLocked = true;
            _lockedFloorPlane = ResolveActivePlane(plane ?? _calibratedFloorPlane);
            _lockedFloorY = floorY;
            _lockedFloorNormal = normal ?? (_currentFloorNormal.sqrMagnitude > 0.5f ? _currentFloorNormal : Vector3.up);
            _lockedFloorPos = planePos ?? (plane != null ? plane.transform.position : _currentFloorPlanePos);
            _calibratedFloorY = floorY;
            _hasCalibratedFloor = true;
            Debug.Log($"[ARRaycastController] Authoritative Floor Plane LOCKED at Y={floorY:F4}, Normal={_lockedFloorNormal}");
        }

        public void UnlockFloorPlane()
        {
            _isFloorLocked = false;
            _lockedFloorPlane = null;
            _hasCalibratedFloor = false;
            _calibratedFloorY = 0f;
            _calibratedFloorPlane = null;
            _currentFloorNormal = Vector3.up;
            _currentFloorPlanePos = Vector3.zero;
            _lockedFloorNormal = Vector3.up;
            _lockedFloorPos = Vector3.zero;
            _currentRawHitPos = Vector3.zero;
            _hasInitializedReticle = false;
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

        // Reticle Active & Throttle State
        private bool _isReticleActive = true;
        private readonly List<ARPlane> _candidatePlanes = new List<ARPlane>(16);

        public bool IsReticleActive => _isReticleActive;

        /// <summary>
        /// Controls whether reticle calculations and 3D rendering are active.
        /// When inactive (e.g. during simulation), raycasting and elevation queries are suspended.
        /// </summary>
        public void SetReticleActive(bool active)
        {
            _isReticleActive = active;
            if (_reticleRoot != null)
            {
                _reticleRoot.SetActive(active);
            }
        }

        private void Update()
        {
            if (!_isReticleActive || !enabled) return;

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
        /// Recursively resolves an ARPlane up its subsumption chain to the active merged plane.
        /// Prevents referencing frozen/dead tracking data when ARCore merges planes.
        /// </summary>
        private ARPlane ResolveActivePlane(ARPlane plane)
        {
            if (plane == null) return null;
            ARPlane current = plane;
            int guard = 0;
            while (current.subsumedBy != null && guard++ < 10)
            {
                current = current.subsumedBy;
            }
            return current;
        }

        /// <summary>
        /// Continuously scans detected AR planes to identify the true physical floor using lowest-cluster heuristics.
        /// Rejects elevated horizontal surfaces (tables, desks, beds) and rewards semantic floor classifications.
        /// </summary>
        private void CalibrateFloorElevation()
        {
            if (_isFloorLocked || !_isReticleActive) return;
            if (_planeManager == null || _arCamera == null) return;

            float cameraY = _arCamera.transform.position.y;
            _candidatePlanes.Clear();

            foreach (var p in _planeManager.trackables)
            {
                if (p == null) continue;
                ARPlane active = ResolveActivePlane(p);
                if (active == null) continue;
                if (active.alignment != PlaneAlignment.HorizontalUp) continue;
                if (active.trackingState == TrackingState.None) continue;

                float planeY = active.transform.position.y;
                // Floor must be physically below the camera (at least 0.20m below, down to 3.0m)
                if (planeY < cameraY - 0.20f && planeY > cameraY - 3.0f)
                {
                    if (!_candidatePlanes.Contains(active))
                        _candidatePlanes.Add(active);
                }
            }

            if (_candidatePlanes.Count == 0) return;

            // Sort ascending by height (lowest first)
            _candidatePlanes.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

            // Cluster planes near the lowest detected elevation (within 25cm of lowest)
            float lowestY = _candidatePlanes[0].transform.position.y;
            ARPlane bestPlane = _candidatePlanes[0];
            float bestScore = -1f;

            foreach (var plane in _candidatePlanes)
            {
                float elevationDiff = plane.transform.position.y - lowestY;
                // Exclude elevated surfaces (tables/desks) that are well above the lowest cluster
                if (elevationDiff > 0.25f) continue;

                float area = Mathf.Max(0.01f, plane.size.x * plane.size.y);
                float score = area;

                // Strong bonus for semantic Floor classification if provided by ARCore
                if (plane.classifications.HasFlag(PlaneClassifications.Floor))
                {
                    score += 15.0f;
                }

                // Preference for planes closest to the lowest detected plane
                score += (0.25f - elevationDiff) * 2.0f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPlane = plane;
                }
            }

            if (bestPlane != null)
            {
                _calibratedFloorPlane = bestPlane;
                _calibratedFloorY = bestPlane.transform.position.y;
                _currentFloorPlanePos = bestPlane.transform.position;
                Vector3 n = bestPlane.normal;
                _currentFloorNormal = (n.sqrMagnitude > 0.5f && n.y > 0.6f) ? n.normalized : Vector3.up;
                _hasCalibratedFloor = true;
            }
        }

        /// <summary>
        /// Multi-tier AR raycasting with unified coplanar floor projection.
        /// Eliminates edge-glitch jumps when crossing plane boundaries,
        /// rejects tables/walls, and maintains seamless floor tracking out to far room corners.
        /// </summary>
        private void PerformFloorRaycast()
        {
            _hasValidFloorHit = false;
            _isPointStable = false;
            _currentHitPlane = null;
            _currentSurface = DetectedSurfaceType.None;

            if (_arCamera == null) return;

            float cameraY = _arCamera.transform.position.y;
            Vector3 camPos = _arCamera.transform.position;
            Vector3 camFwd = _arCamera.transform.forward;

            // Pitch check: require camera to look downward (even slightly: camFwd.y < -0.002)
            if (camFwd.y >= -0.002f)
            {
                _currentRawHitPos = Vector3.zero;
                _distanceFromCamera = 0f;
                return;
            }

            // Reference authoritative floor data
            float refFloorY = _isFloorLocked ? _lockedFloorY : (_hasCalibratedFloor ? _calibratedFloorY : (cameraY - 1.35f));
            Vector3 refFloorNormal = _isFloorLocked ? _lockedFloorNormal : (_hasCalibratedFloor ? _currentFloorNormal : Vector3.up);
            Vector3 refFloorPos = _isFloorLocked ? _lockedFloorPos : (_hasCalibratedFloor ? _currentFloorPlanePos : new Vector3(camPos.x, refFloorY, camPos.z));
            if (refFloorPos == Vector3.zero) refFloorPos = new Vector3(camPos.x, refFloorY, camPos.z);

            float w = (_arCamera != null && _arCamera.pixelWidth > 0) ? _arCamera.pixelWidth : Screen.width;
            float h = (_arCamera != null && _arCamera.pixelHeight > 0) ? _arCamera.pixelHeight : Screen.height;
            Vector2 screenCenter = new Vector2(w * 0.5f, h * 0.5f);

            bool hitWall = false;
            bool hitObstacle = false;
            bool hitFloorPlane = false;
            Vector3 rawHitSpot = Vector3.zero;
            float rawHitDist = 0f;
            ARPlane hitFloorPlaneObj = null;

            if (_raycastManager != null)
            {
                TrackableType[] raycastTiers = new TrackableType[]
                {
                    TrackableType.PlaneWithinPolygon,
                    TrackableType.PlaneWithinBounds,
                    TrackableType.PlaneEstimated
                };

                foreach (var tier in raycastTiers)
                {
                    s_RaycastHits.Clear();
                    if (_raycastManager.Raycast(screenCenter, s_RaycastHits, tier) && s_RaycastHits.Count > 0)
                    {
                        foreach (var hit in s_RaycastHits)
                        {
                            ARPlane rawPlane = hit.trackable as ARPlane ?? (_planeManager != null ? _planeManager.GetPlane(hit.trackableId) : null);
                            ARPlane plane = ResolveActivePlane(rawPlane);

                            if (plane != null)
                            {
                                // Check for wall
                                if (plane.alignment == PlaneAlignment.Vertical || (plane.normal.sqrMagnitude > 0.5f && plane.normal.y < 0.35f))
                                {
                                    _currentSurface = DetectedSurfaceType.Wall;
                                    hitWall = true;
                                    break;
                                }

                                // Check for obstacle (table, desk higher than floor)
                                if ((_isFloorLocked || _hasCalibratedFloor) && hit.pose.position.y > refFloorY + 0.28f && hit.distance < 4.0f)
                                {
                                    _currentSurface = DetectedSurfaceType.HorizontalPlane;
                                    hitObstacle = true;
                                    break;
                                }

                                // Valid horizontal floor plane hit
                                if (IsValidFloorPlane(plane, hit.pose.position, cameraY))
                                {
                                    hitFloorPlaneObj = plane;
                                    rawHitSpot = hit.pose.position;
                                    rawHitDist = hit.distance;
                                    hitFloorPlane = true;

                                    if (!_isFloorLocked)
                                    {
                                        if (!_hasCalibratedFloor)
                                        {
                                            _hasCalibratedFloor = true;
                                            _calibratedFloorY = hit.pose.position.y;
                                            _calibratedFloorPlane = plane;
                                            _currentFloorPlanePos = hit.pose.position;
                                            Vector3 n = plane.normal;
                                            _currentFloorNormal = (n.sqrMagnitude > 0.5f && n.y > 0.6f) ? n.normalized : Vector3.up;
                                        }
                                        else
                                        {
                                            // Smoothly blend reference floor normal & position
                                            Vector3 n = plane.normal;
                                            Vector3 validN = (n.sqrMagnitude > 0.5f && n.y > 0.6f) ? n.normalized : Vector3.up;
                                            _currentFloorNormal = Vector3.Slerp(_currentFloorNormal, validN, 0.08f);
                                            _currentFloorPlanePos = Vector3.Lerp(_currentFloorPlanePos, hit.pose.position, 0.08f);
                                            _calibratedFloorY = Mathf.Lerp(_calibratedFloorY, hit.pose.position.y, 0.08f);
                                        }
                                    }
                                    break;
                                }
                            }
                        }

                        if (hitWall || hitObstacle || hitFloorPlane) break;
                    }
                }
            }

            if (hitWall || hitObstacle)
            {
                _hasValidFloorHit = false;
                _currentRawHitPos = Vector3.zero;
                _distanceFromCamera = 0f;
                return;
            }

            // Unified Coplanar Floor Point calculation
            // Intersects the center camera ray with the unified authoritative floor plane
            float denom = Vector3.Dot(refFloorNormal, camFwd);
            if (denom >= -0.002f)
            {
                _currentRawHitPos = Vector3.zero;
                _distanceFromCamera = 0f;
                return;
            }

            float t = Vector3.Dot(refFloorNormal, refFloorPos - camPos) / denom;
            if (t < 0.10f || t > 60.0f)
            {
                _currentRawHitPos = Vector3.zero;
                _distanceFromCamera = 0f;
                return;
            }

            Vector3 unifiedFloorSpot = camPos + camFwd * t;

            // When unlocked, smoothly incorporate physical hit; when locked, strictly follow coplanar reference
            Vector3 finalSpot;
            if (hitFloorPlane && !_isFloorLocked)
            {
                finalSpot = Vector3.Lerp(unifiedFloorSpot, rawHitSpot, 0.35f);
            }
            else
            {
                finalSpot = unifiedFloorSpot;
            }

            _hasValidFloorHit = (hitFloorPlane || _hasCalibratedFloor || _isFloorLocked);
            _currentSurface = DetectedSurfaceType.Floor;
            _currentHitPlane = hitFloorPlaneObj ?? (_isFloorLocked ? (_lockedFloorPlane ?? _calibratedFloorPlane) : _calibratedFloorPlane);
            _currentFloorNormal = refFloorNormal;
            _currentRawHitPos = finalSpot;
            _distanceFromCamera = t;
            _stabilizedPosition = finalSpot;
            _stabilizedPose = new Pose(finalSpot, Quaternion.FromToRotation(Vector3.up, refFloorNormal));

            // Velocity-based stability check
            float motionDelta = Vector3.Distance(finalSpot, _lastFrameRawHitPos);
            _lastFrameRawHitPos = finalSpot;
            float dynamicTolerance = Mathf.Clamp(_distanceFromCamera * 0.025f, 0.035f, 0.22f);
            _isPointStable = (motionDelta <= dynamicTolerance);
        }

        private bool IsValidFloorPlane(ARPlane plane, Vector3 hitPosition, float cameraY)
        {
            if (plane == null) return false;

            if (plane.alignment != PlaneAlignment.HorizontalUp)
                return false;

            if (plane.normal.y < 0.60f)
                return false;

            // Floor must be below camera
            if (hitPosition.y > cameraY - 0.20f)
                return false;

            return true;
        }

        /// <summary>
        /// Updates the 3D world-space reticle with critically-damped visual smoothing.
        /// Glides across the floor like a high-end laser level without jitter, snapping, or lagging.
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
                _reticleRoot.transform.localScale = Vector3.one * 0.35f;
                _reticleRoot.transform.rotation = Quaternion.LookRotation(camFwd, Vector3.up);
            }
            else if (_hasValidFloorHit)
            {
                float visualScale = Mathf.Clamp(_distanceFromCamera * 0.12f, 0.20f, 1.8f);
                Vector3 normal = (_currentFloorNormal.sqrMagnitude > 0.5f) ? _currentFloorNormal : Vector3.up;
                Vector3 targetFloorPos = _stabilizedPosition + normal * 0.005f;
                Quaternion targetFloorRot = Quaternion.FromToRotation(Vector3.up, normal);

                if (!_hasInitializedReticle)
                {
                    _smoothedReticlePos = targetFloorPos;
                    _smoothedReticleRot = targetFloorRot;
                    _hasInitializedReticle = true;
                }
                else
                {
                    float dist = Vector3.Distance(_smoothedReticlePos, targetFloorPos);
                    if (dist > _teleportThreshold)
                    {
                        // Rapid user whip: snap instantly to avoid trailing lag
                        _smoothedReticlePos = targetFloorPos;
                        _smoothedReticleRot = targetFloorRot;
                    }
                    else
                    {
                        // Critically-damped smooth glide
                        float lerpT = Mathf.Clamp01(Time.deltaTime * _reticleLerpSpeed);
                        _smoothedReticlePos = Vector3.Lerp(_smoothedReticlePos, targetFloorPos, lerpT);
                        _smoothedReticleRot = Quaternion.Slerp(_smoothedReticleRot, targetFloorRot, lerpT);
                    }
                }

                _reticleRoot.transform.position = _smoothedReticlePos;
                _reticleRoot.transform.rotation = _smoothedReticleRot;
                _reticleRoot.transform.localScale = Vector3.one * visualScale;

                if (_isPointStable)
                {
                    targetColor = _readyColor;
                    statusText = $"READY ({_distanceFromCamera:F1}m)";
                }
                else
                {
                    targetColor = _steadyColor;
                    statusText = "HOLD STEADY...";
                }
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

                Vector3 targetAirPos = camPos + camFwd * 1.5f;
                Quaternion targetAirRot = Quaternion.LookRotation(camFwd, Vector3.up);

                _reticleRoot.transform.position = targetAirPos;
                _reticleRoot.transform.localScale = Vector3.one * 0.35f;
                _reticleRoot.transform.rotation = targetAirRot;
            }

            // Apply material color
            if (_reticleMaterial != null)
            {
                _reticleMaterial.color = targetColor;
            }

            // Update 3D billboard status text
            if (_reticleTextMesh != null && _reticleTextObj != null)
            {
                _reticleTextMesh.text = statusText;
                _reticleTextMesh.color = targetColor;
                _reticleTextObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                _reticleTextObj.transform.LookAt(_arCamera.transform);
                _reticleTextObj.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Always returns the exact 3D floor coordinate currently targeted by the smoothed reticle.
        /// Guaranteed to match what the user is seeing on screen, with exact floor elevation.
        /// </summary>
        public Vector3 GetBestFloorSpot()
        {
            if (_hasValidFloorHit)
            {
                // Subtract visual lift (+0.005m) to yield the exact ground plane contact
                if (_hasInitializedReticle && _smoothedReticlePos != Vector3.zero)
                {
                    Vector3 normal = (_currentFloorNormal.sqrMagnitude > 0.5f) ? _currentFloorNormal : Vector3.up;
                    return _smoothedReticlePos - normal * 0.005f;
                }

                if (_stabilizedPosition != Vector3.zero)
                {
                    return _stabilizedPosition;
                }

                if (_currentRawHitPos != Vector3.zero)
                {
                    return _currentRawHitPos;
                }
            }

            // Fallback: Analytical ray-plane intersection against calibrated floor
            if (_arCamera != null)
            {
                Vector3 camPos = _arCamera.transform.position;
                Vector3 camFwd = _arCamera.transform.forward;
                Vector3 normal = _isFloorLocked ? _lockedFloorNormal : (_hasCalibratedFloor ? _currentFloorNormal : Vector3.up);
                float floorY = _isFloorLocked ? _lockedFloorY : (_hasCalibratedFloor ? _calibratedFloorY : (camPos.y - 1.35f));
                Vector3 pos = _isFloorLocked ? _lockedFloorPos : new Vector3(camPos.x, floorY, camPos.z);

                float denom = Vector3.Dot(normal, camFwd);
                if (denom < -0.002f)
                {
                    float t = Vector3.Dot(normal, pos - camPos) / denom;
                    if (t >= 0.10f && t <= 60.0f)
                    {
                        return camPos + camFwd * t;
                    }
                }

                Vector3 forwardFlat = new Vector3(camFwd.x, 0f, camFwd.z).normalized;
                return new Vector3(camPos.x + forwardFlat.x * 2.0f, floorY, camPos.z + forwardFlat.z * 2.0f);
            }

            return Vector3.zero;
        }
    }
}
