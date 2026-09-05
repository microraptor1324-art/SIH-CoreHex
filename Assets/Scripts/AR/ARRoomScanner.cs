using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Coordinates AR room scanning workflow and provides step-by-step guidance instructions.
    /// </summary>
    public class ARRoomScanner : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private ARRoomMeasurement _measurement;

        private float _scannedFloorArea = 0f;

        public float ScannedFloorArea => _scannedFloorArea;

        private void Awake()
        {
            if (_planeManager == null)
                _planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (_measurement == null)
                _measurement = FindAnyObjectByType<ARRoomMeasurement>();
        }

        private void Update()
        {
            UpdateScannedFloorArea();
        }

        private void UpdateScannedFloorArea()
        {
            if (_planeManager == null) return;

            float totalArea = 0f;
            foreach (var plane in _planeManager.trackables)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp && plane.trackingState == TrackingState.Tracking)
                {
                    totalArea += plane.size.x * plane.size.y;
                }
            }
            _scannedFloorArea = totalArea;
        }

        /// <summary>
        /// Returns real-time user-facing instruction text based on current AR state.
        /// </summary>
        public string GetGuidanceInstruction()
        {
            if (_measurement == null)
                return "Initializing AR System...";

            return _measurement.StatusMessage;
        }
    }
}
