using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Phase 1 AR Test Controller.
    /// Validates AR Session state, AR Plane detection, and AR Raycast surface placement.
    /// Includes both uGUI integration and built-in on-screen diagnostics fallback.
    /// </summary>
    public class ARPhase1Manager : MonoBehaviour
    {
        [Header("AR Foundation Components")]
        [SerializeField] private ARSession _arSession;
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private ARRaycastManager _raycastManager;
        [SerializeField] private Camera _arCamera;

        [Header("Marker Settings")]
        [SerializeField] private GameObject _placementMarkerPrefab;
        [SerializeField] private float _markerScale = 0.25f;

        [Header("Optional UI References (uGUI)")]
        [SerializeField] private UnityEngine.UI.Text _statusText;
        [SerializeField] private UnityEngine.UI.Text _planeCountText;
        [SerializeField] private UnityEngine.UI.Text _raycastInfoText;
        [SerializeField] private UnityEngine.UI.Text _instructionsText;
        [SerializeField] private UnityEngine.UI.Image _statusIndicator;

        // Runtime state
        private GameObject _spawnedMarker;
        private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
        private string _lastHitInfo = "None (Tap on a detected floor plane)";
        private float _lastHitDistance = 0f;
        private int _detectedPlanesCount = 0;
        private ARSessionState _currentState = ARSessionState.None;

        private void Awake()
        {
            // Auto-locate components if not linked in Inspector
            if (_arSession == null)
                _arSession = FindFirstObjectByType<ARSession>();
            if (_planeManager == null)
                _planeManager = FindFirstObjectByType<ARPlaneManager>();
            if (_raycastManager == null)
                _raycastManager = FindFirstObjectByType<ARRaycastManager>();
            if (_arCamera == null)
                _arCamera = Camera.main;
        }

        private void Update()
        {
            UpdateARState();
            HandleTouchOrClickInput();
            UpdateUI();
        }

        private void UpdateARState()
        {
            _currentState = ARSession.state;

            if (_planeManager != null)
            {
                _detectedPlanesCount = _planeManager.trackables.count;
            }
        }

        private void HandleTouchOrClickInput()
        {
            Vector2 screenPosition = Vector2.zero;
            bool inputDetected = false;

            // 1. Touchscreen input (Mobile device)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                inputDetected = true;
            }
            // 2. Mouse input (Editor simulation fallback)
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                inputDetected = true;
            }

            if (!inputDetected) return;

            // Prevent raycasting through UI elements
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                {
                    int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                    if (EventSystem.current.IsPointerOverGameObject(touchId))
                        return;
                }
            }

            PerformRaycast(screenPosition);
        }

        private void PerformRaycast(Vector2 screenPos)
        {
            if (_raycastManager == null) return;

            TrackableType trackableTypes = TrackableType.PlaneWithinPolygon | TrackableType.PlaneWithinBounds;

            if (_raycastManager.Raycast(screenPos, s_Hits, trackableTypes))
            {
                Pose hitPose = s_Hits[0].pose;

                // Create or move marker
                if (_spawnedMarker == null)
                {
                    if (_placementMarkerPrefab != null)
                    {
                        _spawnedMarker = Instantiate(_placementMarkerPrefab, hitPose.position, hitPose.rotation);
                    }
                    else
                    {
                        _spawnedMarker = CreateDefaultMarker();
                        _spawnedMarker.transform.position = hitPose.position;
                        _spawnedMarker.transform.rotation = hitPose.rotation;
                    }
                }
                else
                {
                    _spawnedMarker.transform.position = hitPose.position;
                    _spawnedMarker.transform.rotation = hitPose.rotation;
                    _spawnedMarker.SetActive(true);
                }

                // Calculate distance from AR Camera
                if (_arCamera != null)
                {
                    _lastHitDistance = Vector3.Distance(_arCamera.transform.position, hitPose.position);
                }

                _lastHitInfo = $"Position: ({hitPose.position.x:F2}, {hitPose.position.y:F2}, {hitPose.position.z:F2}) | Dist: {_lastHitDistance:F2}m";
                Debug.Log($"[ARPhase1Manager] Raycast hit floor plane at {hitPose.position}, distance {_lastHitDistance:F2}m");
            }
        }

        private GameObject CreateDefaultMarker()
        {
            // Procedural safety beacon marker (Cylinder base + Cone/Sphere top)
            GameObject root = new GameObject("AR_TestMarker_Beacon");

            // Base disk
            GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisk.name = "BaseDisk";
            baseDisk.transform.SetParent(root.transform, false);
            baseDisk.transform.localScale = new Vector3(_markerScale, 0.02f, _markerScale);
            baseDisk.transform.localPosition = new Vector3(0f, 0.01f, 0f);

            // Center cone/pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(_markerScale * 0.35f, _markerScale * 0.75f, _markerScale * 0.35f);
            pole.transform.localPosition = new Vector3(0f, _markerScale * 0.75f, 0f);

            // Warning Sphere Top
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = "WarningSphere";
            top.transform.SetParent(root.transform, false);
            top.transform.localScale = Vector3.one * (_markerScale * 0.45f);
            top.transform.localPosition = new Vector3(0f, _markerScale * 1.5f, 0f);

            // Apply high-visibility industrial orange material
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material orangeMat = new Material(shader);
                orangeMat.name = "SafetyMarker_Mat";
                orangeMat.color = new Color(1.0f, 0.45f, 0.0f); // Safety Orange
                
                Renderer r1 = baseDisk.GetComponent<Renderer>();
                Renderer r2 = pole.GetComponent<Renderer>();
                Renderer r3 = top.GetComponent<Renderer>();
                if (r1) r1.material = orangeMat;
                if (r2) r2.material = orangeMat;
                if (r3) r3.material = orangeMat;
            }

            // Remove colliders to avoid interference with raycasts
            Collider[] colliders = root.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                Destroy(col);

            return root;
        }

        private void UpdateUI()
        {
            if (_statusText != null)
                _statusText.text = $"AR Status: {_currentState}";

            if (_planeCountText != null)
                _planeCountText.text = $"Floors Detected: {_detectedPlanesCount}";

            if (_raycastInfoText != null)
                _raycastInfoText.text = $"Marker: {_lastHitInfo}";

            if (_instructionsText != null)
            {
                if (_detectedPlanesCount == 0)
                    _instructionsText.text = "Point camera at the floor and pan slowly to detect surfaces...";
                else
                    _instructionsText.text = "Floor detected! Tap anywhere on the floor grid to test AR raycasting.";
            }

            if (_statusIndicator != null)
            {
                _statusIndicator.color = _currentState == ARSessionState.SessionTracking 
                    ? new Color(0.2f, 0.9f, 0.3f) 
                    : new Color(0.9f, 0.6f, 0.1f);
            }
        }

        public void ResetSession()
        {
            if (_arSession != null)
            {
                _arSession.Reset();
                _lastHitInfo = "Session reset. Re-scan floor.";
                if (_spawnedMarker != null)
                    _spawnedMarker.SetActive(false);
            }
        }

        /// <summary>
        /// Fallback OnGUI HUD. Ensures the trainee/tester can ALWAYS see AR status,
        /// plane count, and raycast info directly on screen even if UI Canvas is not set up.
        /// </summary>
        private void OnGUI()
        {
            // Only draw if uGUI text is not linked
            if (_statusText != null && _planeCountText != null) return;

            int pad = 24;
            int width = Mathf.Min(Screen.width - pad * 2, 700);
            int height = 280;

            GUI.Box(new Rect(pad, pad, width, height), GUIContent.none);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            GUIStyle statusStyle = new GUIStyle(bodyStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = _currentState == ARSessionState.SessionTracking ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.7f, 0.2f) }
            };

            GUILayout.BeginArea(new Rect(pad + 15, pad + 15, width - 30, height - 30));
            GUILayout.Label("AR MINING SAFETY — PHASE 1 TEST", titleStyle);
            GUILayout.Space(6);
            GUILayout.Label($"AR Tracking State: {_currentState}", statusStyle);
            GUILayout.Label($"Planes Detected: {_detectedPlanesCount}", bodyStyle);
            GUILayout.Label($"Last Raycast Hit: {_lastHitInfo}", bodyStyle);
            GUILayout.Space(6);

            if (_detectedPlanesCount == 0)
                GUILayout.Label("Point phone at floor and move slowly sideways...", bodyStyle);
            else
                GUILayout.Label("Floor detected! Tap anywhere on the floor grid to place beacon.", bodyStyle);

            GUILayout.Space(6);
            if (GUILayout.Button("Reset AR Session", GUILayout.Height(40)))
            {
                ResetSession();
            }

            GUILayout.EndArea();
        }
    }
}
