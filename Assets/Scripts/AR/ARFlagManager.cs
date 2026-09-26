using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARMiningSimulator.AR
{
    public class MeasurementFlag
    {
        public int Index { get; set; }
        public Vector3 Position { get; set; }
        public GameObject FlagObject { get; set; }
        public ARAnchor Anchor { get; set; }

        public MeasurementFlag(int index, Vector3 position, GameObject flagObject, ARAnchor anchor)
        {
            Index = index;
            Position = position;
            FlagObject = flagObject;
            Anchor = anchor;
        }
    }

    /// <summary>
    /// Manages persistent 3D numbered surveyor flags with ARAnchor spatial locking.
    /// Flags remain physically locked to real-world room coordinates as the camera moves.
    /// </summary>
    public class ARFlagManager : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private Camera _arCamera;

        [Header("Visual Styling")]
        [SerializeField] private Color _flagPoleColor = new Color(0.9f, 0.9f, 0.95f);
        [SerializeField] private Color _flagHeadColor = new Color(0.95f, 0.25f, 0.2f); // Safety Red
        [SerializeField] private float _poleHeight = 0.25f;
        [SerializeField] private float _poleRadius = 0.02f;

        private readonly List<MeasurementFlag> _flags = new List<MeasurementFlag>();

        public int Count => _flags.Count;
        public IReadOnlyList<MeasurementFlag> Flags => _flags;

        private void Awake()
        {
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
            if (_arCamera == null)
                _arCamera = Camera.main;
        }

        private void Update()
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            // Billboard text labels to face camera & sync anchor coordinates
            foreach (var flag in _flags)
            {
                // Synchronize with ARAnchor updates only for subtle SLAM drift correction (< 0.40m)
                // Strictly preserves the calibrated floor Y level so the flag NEVER sinks!
                if (flag.Anchor != null && flag.Anchor.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
                {
                    Vector3 ancPos = flag.Anchor.transform.position;
                    float horizDrift = Vector2.Distance(new Vector2(ancPos.x, ancPos.z), new Vector2(flag.Position.x, flag.Position.z));
                    if (horizDrift < 0.40f)
                    {
                        Vector3 updated = new Vector3(ancPos.x, flag.Position.y, ancPos.z);
                        flag.Position = updated;
                        if (flag.FlagObject != null)
                        {
                            flag.FlagObject.transform.position = updated;
                        }
                    }
                }

                if (flag.FlagObject != null)
                {
                    Transform labelTr = flag.FlagObject.transform.Find("FlagVisual/LabelBillboard") ?? flag.FlagObject.transform.Find("LabelBillboard");
                    if (labelTr != null)
                    {
                        labelTr.LookAt(_arCamera.transform);
                        labelTr.Rotate(0, 180, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a persistent anchored 3D flag at the specified world position.
        /// Locked directly in AR world coordinates for absolute measurement calibration.
        /// Guaranteed to stay at the exact same level as the floor.
        /// </summary>
        public MeasurementFlag AddFlag(Vector3 position, Pose pose, ARPlane plane)
        {
            int index = _flags.Count + 1;
            ARAnchor anchor = null;

            if (_anchorManager != null && plane != null)
            {
                // Only attach anchor to plane if the flag position is actually near that plane's bounds
                bool isNearPlane = Vector3.Distance(position, plane.transform.position) <= Mathf.Max(plane.size.x, plane.size.y) * 1.5f;
                if (isNearPlane)
                {
                    anchor = _anchorManager.AttachAnchor(plane, pose);
                }
            }

            GameObject flagGo = CreateFlagGameObject(index, position);
            var flag = new MeasurementFlag(index, position, flagGo, anchor);
            _flags.Add(flag);

            Transform visualTr = flagGo.transform.Find("FlagVisual");
            Vector3 visualLocalPos = visualTr != null ? visualTr.localPosition : Vector3.zero;
            Debug.Log($"[ARFlagManager] Placed Corner #{index} - Root: '{flagGo.name}' Pos={position}, VisualLocalOffset={visualLocalPos}, Anchored={anchor != null}");
            return flag;
        }

        private GameObject CreateFlagGameObject(int index, Vector3 position)
        {
            // Root represents the exact AR floor measurement position at floor level
            GameObject cornerPoint = new GameObject($"CornerPoint_{index}");
            cornerPoint.transform.position = position;
            cornerPoint.transform.rotation = Quaternion.identity;

            // Child container for visuals only - allows visual adjustments without affecting measurement coordinates
            GameObject flagVisual = new GameObject("FlagVisual");
            flagVisual.transform.SetParent(cornerPoint.transform, false);
            flagVisual.transform.localPosition = Vector3.zero;
            flagVisual.transform.localRotation = Quaternion.identity;

            // Distance-adaptive scaling: ensures flags placed far away (5m - 20m) remain clearly visible and proud
            float dist = (_arCamera != null) ? Vector3.Distance(_arCamera.transform.position, position) : 1f;
            float flagScale = Mathf.Clamp(1.0f + (dist - 2.0f) * 0.08f, 1.0f, 2.2f);
            flagVisual.transform.localScale = Vector3.one * flagScale;

            // 1. Grounded Base Disc resting atop the floor (thickness 6mm: cylinder height 2 * 0.003m = 0.006m, local Y = 0.003m -> bottom at Y = 0)
            GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisc.name = "BaseDisc";
            baseDisc.transform.SetParent(flagVisual.transform, false);
            baseDisc.transform.localScale = new Vector3(0.14f, 0.003f, 0.14f);
            baseDisc.transform.localPosition = new Vector3(0f, 0.003f, 0f); // Sits from exactly Y=0 to Y=0.006m
            Destroy(baseDisc.GetComponent<Collider>());

            Material baseMat = ARMaterialHelper.CreateUnlitMaterial(new Color(0.2f, 0.25f, 0.3f, 0.95f));
            baseDisc.GetComponent<Renderer>().sharedMaterial = baseMat;

            // 2. Flag Pole (Vertical cylinder, bottom rests at local Y = 0.006m on top of base disc)
            float poleH = 0.35f;
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(flagVisual.transform, false);
            pole.transform.localScale = new Vector3(_poleRadius * 2f, poleH * 0.5f, _poleRadius * 2f);
            pole.transform.localPosition = new Vector3(0f, 0.006f + poleH * 0.5f, 0f);
            Destroy(pole.GetComponent<Collider>());

            Material poleMat = ARMaterialHelper.CreateUnlitMaterial(_flagPoleColor);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            // 3. Flag Head (Red Sphere at top of pole)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(flagVisual.transform, false);
            head.transform.localScale = Vector3.one * 0.08f;
            head.transform.localPosition = new Vector3(0f, 0.006f + poleH + 0.04f, 0f);
            Destroy(head.GetComponent<Collider>());

            Material headMat = ARMaterialHelper.CreateUnlitMaterial(_flagHeadColor);
            head.GetComponent<Renderer>().sharedMaterial = headMat;

            // 4. Floating 3D Text Label ("P{index} 🚩")
            GameObject labelObj = new GameObject("LabelBillboard");
            labelObj.transform.SetParent(flagVisual.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.006f + poleH + 0.12f, 0f);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = $"P{index} 🚩";
            tm.fontSize = 32;
            tm.characterSize = 0.035f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            return cornerPoint;
        }

        /// <summary>
        /// Removes the most recently placed flag (Undo).
        /// </summary>
        public bool RemoveLastFlag()
        {
            if (_flags.Count == 0) return false;

            int lastIdx = _flags.Count - 1;
            MeasurementFlag flag = _flags[lastIdx];

            if (flag.FlagObject != null)
            {
                Destroy(flag.FlagObject);
            }

            if (flag.Anchor != null)
            {
                Destroy(flag.Anchor.gameObject);
            }

            _flags.RemoveAt(lastIdx);
            Debug.Log($"[ARFlagManager] Removed FLAG {flag.Index}. Remaining flags: {_flags.Count}");
            return true;
        }

        /// <summary>
        /// Destroys all flags and anchors (Reset).
        /// </summary>
        public void ClearAllFlags()
        {
            foreach (var flag in _flags)
            {
                if (flag.FlagObject != null)
                    Destroy(flag.FlagObject);
                if (flag.Anchor != null)
                    Destroy(flag.Anchor.gameObject);
            }
            _flags.Clear();
            Debug.Log("[ARFlagManager] Cleared all flags.");
        }

        /// <summary>
        /// Toggles the visibility of all placed flag GameObjects.
        /// Allows hiding flags when the underground mining environment is active.
        /// </summary>
        public void SetFlagsVisible(bool visible)
        {
            foreach (var flag in _flags)
            {
                if (flag.FlagObject != null)
                {
                    flag.FlagObject.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Returns all locked flag positions in sequence.
        /// </summary>
        public List<Vector3> GetPointPositions()
        {
            List<Vector3> pts = new List<Vector3>(_flags.Count);
            foreach (var flag in _flags)
            {
                pts.Add(flag.Position);
            }
            return pts;
        }
    }
}
