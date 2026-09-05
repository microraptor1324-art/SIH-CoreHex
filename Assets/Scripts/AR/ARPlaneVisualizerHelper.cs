using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Attached to the AR Plane prefab.
    /// Fixes the "pink randomness" bug by completely suppressing raw plane mesh rendering
    /// so the real floor and floor tiles remain crystal clear.
    /// AR raycasting continues detecting the mathematical floor planes with 100% accuracy.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    public class ARPlaneVisualizerHelper : MonoBehaviour
    {
        private ARPlane _arPlane;
        private MeshRenderer _meshRenderer;
        private ARPlaneMeshVisualizer _meshVisualizer;
        private LineRenderer _lineRenderer;

        [Header("Floor Plane Visibility")]
        [Tooltip("Keep false to prevent pink/opaque plane blobs from occluding the floor.")]
        [SerializeField] private bool _renderFloorMesh = false;
        [SerializeField] private bool _renderPerimeterLine = false;

        private void Awake()
        {
            _arPlane = GetComponent<ARPlane>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshVisualizer = GetComponent<ARPlaneMeshVisualizer>();
            _lineRenderer = GetComponent<LineRenderer>();

            // Immediately suppress raw plane mesh to eliminate pink plane artifacts
            if (!_renderFloorMesh)
            {
                if (_meshRenderer != null) _meshRenderer.enabled = false;
                if (_meshVisualizer != null) _meshVisualizer.enabled = false;
            }

            if (!_renderPerimeterLine)
            {
                if (_lineRenderer != null) _lineRenderer.enabled = false;
            }
        }

        private void Start()
        {
            // Enforce suppression at Start
            if (!_renderFloorMesh)
            {
                if (_meshRenderer != null) _meshRenderer.enabled = false;
                if (_meshVisualizer != null) _meshVisualizer.enabled = false;
            }
            if (!_renderPerimeterLine && _lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (_arPlane == null) return;

            // Never allow missing-shader pink plane meshes to render on the floor
            if (!_renderFloorMesh)
            {
                if (_meshRenderer != null && _meshRenderer.enabled) _meshRenderer.enabled = false;
                if (_meshVisualizer != null && _meshVisualizer.enabled) _meshVisualizer.enabled = false;
            }

            if (!_renderPerimeterLine)
            {
                if (_lineRenderer != null && _lineRenderer.enabled) _lineRenderer.enabled = false;
            }
        }
    }
}
