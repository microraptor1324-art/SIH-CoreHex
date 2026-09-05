using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.AR
{
    public class MeasurementSegment
    {
        public Vector3 StartPoint { get; set; }
        public Vector3 EndPoint { get; set; }
        public float Distance { get; set; }
        public GameObject LineObject { get; set; }
        public GameObject LabelObject { get; set; }

        public MeasurementSegment(Vector3 start, Vector3 end, float distance, GameObject lineObj, GameObject labelObj)
        {
            StartPoint = start;
            EndPoint = end;
            Distance = distance;
            LineObject = lineObj;
            LabelObject = labelObj;
        }
    }

    /// <summary>
    /// Renders 3D world-space laser measurement lines between flags,
    /// dynamic live preview lines, and floating 3D distance badges.
    /// </summary>
    public class MeasurementLineManager : MonoBehaviour
    {
        [Header("Visual Styling")]
        [SerializeField] private Color _lockedLineColor = new Color(0.1f, 0.85f, 1.0f, 0.95f); // Cyan laser
        [SerializeField] private Color _previewLineColor = new Color(1.0f, 0.9f, 0.2f, 0.9f);  // Yellow/gold
        [SerializeField] private Color _closingLineColor = new Color(0.2f, 1.0f, 0.45f, 0.95f); // Safety Green
        [SerializeField] private float _lineWidth = 0.025f;

        [Header("Camera Reference")]
        [SerializeField] private Camera _arCamera;

        private readonly List<MeasurementSegment> _segments = new List<MeasurementSegment>();

        // Live Preview Line & Label
        private LineRenderer _previewLineRenderer;
        private GameObject _previewLabelObj;
        private TextMesh _previewTextMesh;
        private float _liveDistance = 0f;

        // Closing Segment
        private MeasurementSegment _closingSegment = null;

        public float LiveDistance => _liveDistance;
        public int SegmentCount => _segments.Count;
        public IReadOnlyList<MeasurementSegment> Segments => _segments;

        private void Awake()
        {
            if (_arCamera == null)
                _arCamera = Camera.main;

            SetupPreviewLine();
        }

        private void SetupPreviewLine()
        {
            GameObject previewLineObj = new GameObject("LivePreviewLine");
            previewLineObj.transform.SetParent(transform, false);
            _previewLineRenderer = previewLineObj.AddComponent<LineRenderer>();

            Material mat = ARMaterialHelper.CreateUnlitMaterial(_previewLineColor);
            _previewLineRenderer.sharedMaterial = mat;

            _previewLineRenderer.startColor = _previewLineColor;
            _previewLineRenderer.endColor = _previewLineColor;
            _previewLineRenderer.startWidth = _lineWidth;
            _previewLineRenderer.endWidth = _lineWidth;
            _previewLineRenderer.useWorldSpace = true;
            _previewLineRenderer.positionCount = 0;

            // Preview Text Label
            _previewLabelObj = new GameObject("LivePreviewLabel");
            _previewLabelObj.transform.SetParent(transform, false);
            _previewTextMesh = _previewLabelObj.AddComponent<TextMesh>();
            _previewTextMesh.fontSize = 26;
            _previewTextMesh.characterSize = 0.035f;
            _previewTextMesh.anchor = TextAnchor.MiddleCenter;
            _previewTextMesh.alignment = TextAlignment.Center;
            _previewTextMesh.color = Color.white;
            _previewLabelObj.SetActive(false);
        }

        private void Update()
        {
            if (_arCamera == null)
            {
                _arCamera = Camera.main;
                if (_arCamera == null) return;
            }

            // Billboard distance labels towards camera
            foreach (var seg in _segments)
            {
                if (seg.LabelObject != null)
                {
                    seg.LabelObject.transform.LookAt(_arCamera.transform);
                    seg.LabelObject.transform.Rotate(0, 180, 0);
                }
            }

            if (_closingSegment != null && _closingSegment.LabelObject != null)
            {
                _closingSegment.LabelObject.transform.LookAt(_arCamera.transform);
                _closingSegment.LabelObject.transform.Rotate(0, 180, 0);
            }

            if (_previewLabelObj != null && _previewLabelObj.activeSelf)
            {
                _previewLabelObj.transform.LookAt(_arCamera.transform);
                _previewLabelObj.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Adds a permanent locked 3D measurement line between two points with a distance badge.
        public static string FormatDistance(float distanceInMeters)
        {
            if (distanceInMeters < 1.0f)
            {
                return $"{(distanceInMeters * 100f):F1} cm";
            }
            return $"{distanceInMeters:F2} m";
        }

        // Tiny visual-only offset to eliminate z-fighting with the floor mesh without appearing floating in air
        private const float VisualLineFloorOffset = 0.003f;  // 3mm
        private const float VisualLabelFloorOffset = 0.035f; // 3.5cm

        /// <summary>
        /// Adds a permanent locked 3D measurement line between two points with a distance badge.
        /// </summary>
        public MeasurementSegment AddSegment(Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            GameObject lineObj = CreateLineGameObject($"Segment_{_segments.Count + 1}", from, to, _lockedLineColor);
            GameObject labelObj = CreateDistanceLabelGameObject($"Label_{_segments.Count + 1}", (from + to) * 0.5f, FormatDistance(dist));

            var segment = new MeasurementSegment(from, to, dist, lineObj, labelObj);
            _segments.Add(segment);

            Debug.Log($"[MeasurementLineManager] Created segment from {from} to {to}: Dist={FormatDistance(dist)} (Visual line Y offset: +{VisualLineFloorOffset}m)");
            return segment;
        }

        /// <summary>
        /// Updates the temporary dynamic line and distance label connecting the latest flag to current raycast aim.
        /// </summary>
        public void UpdatePreviewLine(Vector3 from, Vector3 to)
        {
            if (_previewLineRenderer == null) return;

            _previewLineRenderer.positionCount = 2;
            _previewLineRenderer.SetPosition(0, from + Vector3.up * VisualLineFloorOffset);
            _previewLineRenderer.SetPosition(1, to + Vector3.up * VisualLineFloorOffset);

            _liveDistance = Vector3.Distance(from, to);

            if (_previewLabelObj != null)
            {
                _previewLabelObj.SetActive(true);
                Vector3 mid = (from + to) * 0.5f + Vector3.up * VisualLabelFloorOffset;
                _previewLabelObj.transform.position = mid;
                if (_previewTextMesh != null)
                {
                    _previewTextMesh.text = FormatDistance(_liveDistance);
                }
            }
        }

        public void HidePreviewLine()
        {
            if (_previewLineRenderer != null)
                _previewLineRenderer.positionCount = 0;
            if (_previewLabelObj != null)
                _previewLabelObj.SetActive(false);
            _liveDistance = 0f;
        }

        /// <summary>
        /// Connects the final point back to the first flag to close the polygon.
        /// </summary>
        public void ConnectClosingSegment(Vector3 lastPoint, Vector3 firstPoint)
        {
            float dist = Vector3.Distance(lastPoint, firstPoint);
            GameObject lineObj = CreateLineGameObject("ClosingSegment", lastPoint, firstPoint, _closingLineColor);
            GameObject labelObj = CreateDistanceLabelGameObject("ClosingLabel", (lastPoint + firstPoint) * 0.5f, FormatDistance(dist));

            _closingSegment = new MeasurementSegment(lastPoint, firstPoint, dist, lineObj, labelObj);
            HidePreviewLine();
        }

        /// <summary>
        /// Removes the most recently placed line segment (Undo).
        /// </summary>
        public bool RemoveLastSegment()
        {
            if (_closingSegment != null)
            {
                if (_closingSegment.LineObject != null) Destroy(_closingSegment.LineObject);
                if (_closingSegment.LabelObject != null) Destroy(_closingSegment.LabelObject);
                _closingSegment = null;
                return true;
            }

            if (_segments.Count == 0) return false;

            int lastIdx = _segments.Count - 1;
            MeasurementSegment seg = _segments[lastIdx];

            if (seg.LineObject != null) Destroy(seg.LineObject);
            if (seg.LabelObject != null) Destroy(seg.LabelObject);

            _segments.RemoveAt(lastIdx);
            return true;
        }

        /// <summary>
        /// Clears all lines and labels (Reset).
        /// </summary>
        public void ClearAllLines()
        {
            foreach (var seg in _segments)
            {
                if (seg.LineObject != null) Destroy(seg.LineObject);
                if (seg.LabelObject != null) Destroy(seg.LabelObject);
            }
            _segments.Clear();

            if (_closingSegment != null)
            {
                if (_closingSegment.LineObject != null) Destroy(_closingSegment.LineObject);
                if (_closingSegment.LabelObject != null) Destroy(_closingSegment.LabelObject);
                _closingSegment = null;
            }

            HidePreviewLine();
        }

        private GameObject CreateLineGameObject(string name, Vector3 start, Vector3 end, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            Material mat = ARMaterialHelper.CreateUnlitMaterial(color);
            lr.sharedMaterial = mat;

            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth;
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, start + Vector3.up * VisualLineFloorOffset);
            lr.SetPosition(1, end + Vector3.up * VisualLineFloorOffset);

            return go;
        }

        private GameObject CreateDistanceLabelGameObject(string name, Vector3 position, string text)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position + Vector3.up * VisualLabelFloorOffset;

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 26;
            tm.characterSize = 0.035f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            return go;
        }
    }
}
