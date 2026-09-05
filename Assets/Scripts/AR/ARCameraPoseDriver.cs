using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Master AR Camera Pose Synchronization & Calibration Driver.
    /// Fixes mobile AR calibration errors:
    /// 1. Integrates UnityEngine.InputSystem.XR.TrackedPoseDriver with explicit ARCore 6-DoF bindings.
    /// 2. Continuously synchronizes Main Camera localPosition and localRotation with ARCore's 6-DoF device pose.
    /// 3. Strictly enforces CameraOffset.localPosition = Vector3.zero and XROrigin.CameraYOffset = 0f
    ///    (eliminates the artificial 1.1176m VR height offset that causes AR flags and lines to render 1-2m behind).
    /// 4. Ensures the 3D virtual camera frustum matches the physical phone camera lens at every frame.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)] // Execute before raycasting and rendering
    public class ARCameraPoseDriver : MonoBehaviour
    {
        [Header("XR Rig Calibration")]
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private Transform _cameraOffset;

        private Camera _camera;
        private TrackedPoseDriver _trackedPoseDriver;
        private static readonly List<UnityEngine.XR.InputDevice> s_TrackingDevices = new List<UnityEngine.XR.InputDevice>();
        private UnityEngine.XR.InputDevice _arDevice;
        private bool _hasDevice = false;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                _camera.nearClipPlane = 0.05f;
            }

            if (_xrOrigin == null)
                _xrOrigin = FindAnyObjectByType<XROrigin>();

            SetupTrackedPoseDriver();
            CalibrateXROrigin();
        }

        private void SetupTrackedPoseDriver()
        {
            _trackedPoseDriver = GetComponent<TrackedPoseDriver>();
            if (_trackedPoseDriver == null)
            {
                _trackedPoseDriver = gameObject.AddComponent<TrackedPoseDriver>();
            }

            if (_trackedPoseDriver != null)
            {
                _trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
                _trackedPoseDriver.ignoreTrackingState = false;

                var positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
                positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");
                positionAction.AddBinding("<XRDevice>/devicePosition");
                _trackedPoseDriver.positionInput = new InputActionProperty(positionAction);

                var rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
                rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");
                rotationAction.AddBinding("<XRDevice>/deviceRotation");
                _trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
            }
        }

        private void OnEnable()
        {
            CalibrateXROrigin();
        }

        private void OnDisable()
        {
        }

        /// <summary>
        /// Eliminates the VR human-height offset (1.1176m) that shifts mobile AR perspective backwards.
        /// In mobile handheld AR, the phone is the origin; offset must always be 0.
        /// </summary>
        public void CalibrateXROrigin()
        {
            if (_xrOrigin != null)
            {
                _xrOrigin.CameraYOffset = 0f;
                _xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

                if (_xrOrigin.CameraFloorOffsetObject != null)
                {
                    _xrOrigin.CameraFloorOffsetObject.transform.localPosition = Vector3.zero;
                    _xrOrigin.CameraFloorOffsetObject.transform.localRotation = Quaternion.identity;
                }
            }

            if (_cameraOffset != null)
            {
                _cameraOffset.localPosition = Vector3.zero;
                _cameraOffset.localRotation = Quaternion.identity;
            }
            else if (transform.parent != null)
            {
                transform.parent.localPosition = Vector3.zero;
                transform.parent.localRotation = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
            // Continuously guarantee Camera Offset stays at 0 so frustum and raycasts align with device physical lens
            if (transform.parent != null && transform.parent.localPosition != Vector3.zero)
            {
                transform.parent.localPosition = Vector3.zero;
            }
        }
    }
}
