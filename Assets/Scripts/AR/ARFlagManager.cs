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

            // Billboard text labels to face camera
            foreach (var flag in _flags)
            {
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
        /// </summary>
        public MeasurementFlag AddFlag(Vector3 position, Pose pose, ARPlane plane)
        {
            int index = _flags.Count + 1;
            ARAnchor anchor = null;

            if (_anchorManager != null && plane != null)
            {
                anchor = _anchorManager.AttachAnchor(plane, pose);
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
            // Root represents the exact AR floor measurement position
            GameObject cornerPoint = new GameObject($"CornerPoint_{index}");
            cornerPoint.transform.position = position;
            cornerPoint.transform.rotation = Quaternion.identity;

            // Child container for visuals only - allows visual adjustments without affecting measurement coordinates
            GameObject flagVisual = new GameObject("FlagVisual");
            flagVisual.transform.SetParent(cornerPoint.transform, false);
            flagVisual.transform.localPosition = Vector3.zero;
            flagVisual.transform.localRotation = Quaternion.identity;

            // 1. Grounded Base Disc touching the floor (thickness 4mm, local Y = 0.002m, radius 0.06m)
            GameObject baseDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisc.name = "BaseDisc";
            baseDisc.transform.SetParent(flagVisual.transform, false);
            baseDisc.transform.localScale = new Vector3(0.12f, 0.002f, 0.12f);
            baseDisc.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            Destroy(baseDisc.GetComponent<Collider>());

            Material baseMat = ARMaterialHelper.CreateUnlitMaterial(new Color(0.2f, 0.25f, 0.3f, 0.95f));
            baseDisc.GetComponent<Renderer>().sharedMaterial = baseMat;

            // 2. Flag Pole (Vertical cylinder, bottom rests at local Y = 0 on the floor)
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(flagVisual.transform, false);
            pole.transform.localScale = new Vector3(_poleRadius * 2f, _poleHeight * 0.5f, _poleRadius * 2f);
            pole.transform.localPosition = new Vector3(0f, _poleHeight * 0.5f, 0f);
            Destroy(pole.GetComponent<Collider>());

            Material poleMat = ARMaterialHelper.CreateUnlitMaterial(_flagPoleColor);
            pole.GetComponent<Renderer>().sharedMaterial = poleMat;

            // 3. Flag Head (Red Sphere at top of pole)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(flagVisual.transform, false);
            head.transform.localScale = Vector3.one * 0.07f;
            head.transform.localPosition = new Vector3(0f, _poleHeight + 0.035f, 0f);
            Destroy(head.GetComponent<Collider>());

            Material headMat = ARMaterialHelper.CreateUnlitMaterial(_flagHeadColor);
            head.GetComponent<Renderer>().sharedMaterial = headMat;

            // 4. Floating 3D Text Label ("CORNER {index}")
            GameObject labelObj = new GameObject("LabelBillboard");
            labelObj.transform.SetParent(flagVisual.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, _poleHeight + 0.10f, 0f);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = $"P{index} 🚩";
            tm.fontSize = 28;
            tm.characterSize = 0.032f;
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
