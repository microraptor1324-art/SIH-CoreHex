using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Evacuation
{
    /// <summary>
    /// Spawns and animates 3D photo-luminescent green chevrons along the floor
    /// leading from the trainee's position through the Emergency Exit to the Safe Zone.
    /// </summary>
    public class EvacuationPathVisualizer : MonoBehaviour
    {
        [Header("Chevron Visual Settings")]
        [SerializeField] private float _chevronSpacing = 0.65f;
        [SerializeField] private float _chevronScale = 0.28f;
        [SerializeField] private float _rippleSpeed = 6.0f;

        private readonly List<GameObject> _chevrons = new List<GameObject>();
        private Material _chevronMat;
        private bool _isVisible = false;
        private Vector3 _lastExitPos;
        private Vector3 _lastSafeZonePos;

        private void Awake()
        {
            CreateMaterial();
        }

        private void CreateMaterial()
        {
            if (_chevronMat != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                _chevronMat = new Material(shader)
                {
                    color = new Color(0.15f, 0.95f, 0.4f, 0.95f)
                };
                _chevronMat.EnableKeyword("_EMISSION");
                _chevronMat.SetColor("_EmissionColor", new Color(0.2f, 1.2f, 0.5f));
            }
        }

        public void ShowPath(Vector3 startPos, Vector3 exitPos, Vector3 safeZonePos)
        {
            _lastExitPos = exitPos;
            _lastSafeZonePos = safeZonePos;
            _isVisible = true;

            RebuildPath(startPos, exitPos, safeZonePos);
        }

        public void ShowDirectPath(Vector3 startPos, Vector3 targetPos)
        {
            _lastExitPos = targetPos;
            _lastSafeZonePos = targetPos;
            _isVisible = true;

            ClearChevrons();
            CreateMaterial();

            List<Vector3> waypoints = new List<Vector3>
            {
                new Vector3(startPos.x, 0.02f, startPos.z),
                new Vector3(targetPos.x, 0.02f, targetPos.z)
            };
            BuildWaypoints(waypoints);
        }

        public void UpdatePlayerPosition(Vector3 currentPos)
        {
            if (!_isVisible) return;
            RebuildPath(currentPos, _lastExitPos, _lastSafeZonePos);
        }

        public void HidePath()
        {
            _isVisible = false;
            ClearChevrons();
        }

        private void RebuildPath(Vector3 startPos, Vector3 exitPos, Vector3 safeZonePos)
        {
            ClearChevrons();
            CreateMaterial();

            // Waypoints: start -> doorway exit -> safe zone pad
            List<Vector3> waypoints = new List<Vector3>
            {
                new Vector3(startPos.x, 0.02f, startPos.z),
                new Vector3(exitPos.x, 0.02f, exitPos.z),
                new Vector3(safeZonePos.x, 0.02f, safeZonePos.z)
            };

            BuildWaypoints(waypoints);
        }

        private void BuildWaypoints(List<Vector3> waypoints)
        {
            for (int w = 0; w < waypoints.Count - 1; w++)
            {
                Vector3 segStart = waypoints[w];
                Vector3 segEnd = waypoints[w + 1];
                Vector3 dir = segEnd - segStart;
                float segLen = dir.magnitude;

                if (segLen < 0.2f) continue;
                dir.Normalize();

                Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);
                int count = Mathf.Max(1, Mathf.FloorToInt(segLen / _chevronSpacing));

                for (int i = 1; i <= count; i++)
                {
                    float t = (float)i / (count + 1);
                    Vector3 pos = Vector3.Lerp(segStart, segEnd, t);
                    pos.y = 0.02f;

                    GameObject chevron = BuildChevronMesh(pos, rotation);
                    _chevrons.Add(chevron);
                }
            }
        }

        private GameObject BuildChevronMesh(Vector3 pos, Quaternion rot)
        {
            GameObject chevronRoot = new GameObject("PathChevron");
            chevronRoot.transform.SetParent(transform, false);
            chevronRoot.transform.position = pos;
            chevronRoot.transform.rotation = rot;

            // Left wing of chevron
            GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWing.name = "LeftWing";
            leftWing.transform.SetParent(chevronRoot.transform, false);
            leftWing.transform.localScale = new Vector3(_chevronScale * 0.25f, 0.015f, _chevronScale * 0.8f);
            leftWing.transform.localPosition = new Vector3(-_chevronScale * 0.22f, 0, 0);
            leftWing.transform.localRotation = Quaternion.Euler(0, 30f, 0);
            if (_chevronMat != null) leftWing.GetComponent<Renderer>().sharedMaterial = _chevronMat;

            var colL = leftWing.GetComponent<Collider>();
            if (colL != null) Destroy(colL);

            // Right wing of chevron
            GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWing.name = "RightWing";
            rightWing.transform.SetParent(chevronRoot.transform, false);
            rightWing.transform.localScale = new Vector3(_chevronScale * 0.25f, 0.015f, _chevronScale * 0.8f);
            rightWing.transform.localPosition = new Vector3(_chevronScale * 0.22f, 0, 0);
            rightWing.transform.localRotation = Quaternion.Euler(0, -30f, 0);
            if (_chevronMat != null) rightWing.GetComponent<Renderer>().sharedMaterial = _chevronMat;

            var colR = rightWing.GetComponent<Collider>();
            if (colR != null) Destroy(colR);

            return chevronRoot;
        }

        private void Update()
        {
            if (!_isVisible || _chevrons.Count == 0) return;

            // Ripple emission / height along path
            float time = Time.time * _rippleSpeed;
            for (int i = 0; i < _chevrons.Count; i++)
            {
                if (_chevrons[i] == null) continue;
                float wave = Mathf.PingPong(time - i * 0.45f, 1.0f);
                float scaleMod = Mathf.Lerp(0.85f, 1.25f, wave);
                _chevrons[i].transform.localScale = Vector3.one * scaleMod;
            }
        }

        private void ClearChevrons()
        {
            for (int i = 0; i < _chevrons.Count; i++)
            {
                if (_chevrons[i] != null)
                {
                    Destroy(_chevrons[i]);
                }
            }
            _chevrons.Clear();
        }

        private void OnDisable()
        {
            ClearChevrons();
        }

        private void OnDestroy()
        {
            ClearChevrons();
        }
    }
}
