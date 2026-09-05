using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Generates a procedural flat 3D mesh covering the measured room polygon,
    /// shaded with a semi-transparent AR floor material physically locked to world coordinates.
    /// Supports triangles, rectangles, L-shaped rooms, and arbitrary simple polygons.
    /// </summary>
    public class RoomPolygonManager : MonoBehaviour
    {
        [Header("Shading Visuals")]
        [SerializeField] private Color _shadingColor = new Color(0.0f, 0.40f, 1.0f, 0.50f); // 50% Blue color shade
        [SerializeField] private Color _boundaryGlowColor = new Color(0.1f, 0.6f, 1.0f, 0.95f); // Blue outline

        private GameObject _shadedMeshObj;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _shadingMaterial;

        private void Awake()
        {
            SetupMaterial();
        }

        private void SetupMaterial()
        {
            _shadingMaterial = ARMaterialHelper.GetRoomShadingMaterial(_shadingColor);
        }

        /// <summary>
        /// Builds and renders the shaded floor polygon mesh covering all measured points.
        /// </summary>
        public void CreateShadedFloorPolygon(IList<Vector3> worldPoints)
        {
            ClearShadedFloorPolygon();

            if (worldPoints == null || worldPoints.Count < 3) return;

            if (_shadingMaterial == null)
            {
                SetupMaterial();
            }

            if (_shadingMaterial != null)
            {
                _shadingMaterial.color = _shadingColor;
                if (_shadingMaterial.HasProperty("_BaseColor"))
                    _shadingMaterial.SetColor("_BaseColor", _shadingColor);
            }

            _shadedMeshObj = new GameObject("AR_ShadedRoomFloor");
            _shadedMeshObj.transform.SetParent(transform, false);

            _meshFilter = _shadedMeshObj.AddComponent<MeshFilter>();
            _meshRenderer = _shadedMeshObj.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = _shadingMaterial;

            Mesh mesh = BuildPolygonMesh(worldPoints);
            _meshFilter.sharedMesh = mesh;

            Debug.Log($"[RoomPolygonManager] Generated 50% blue shaded floor mesh with {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles.");
        }

        public void ClearShadedFloorPolygon()
        {
            if (_shadedMeshObj != null)
            {
                Destroy(_shadedMeshObj);
                _shadedMeshObj = null;
            }
        }

        private Mesh BuildPolygonMesh(IList<Vector3> points)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoomFloorMesh";

            int n = points.Count;
            Vector3[] vertices = new Vector3[n];
            Vector2[] uvs = new Vector2[n];

            // Elevate slightly (+0.003m) to prevent z-fighting with the real floor
            for (int i = 0; i < n; i++)
            {
                vertices[i] = points[i] + Vector3.up * 0.003f;
                uvs[i] = new Vector2(points[i].x, points[i].z);
            }

            int[] triangles = TriangulatePolygon(vertices);

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Triangulates a simple 2D polygon projected on X-Z using Ear Clipping.
        /// Handles convex, concave, and irregular room geometries.
        /// </summary>
        private int[] TriangulatePolygon(Vector3[] vertices)
        {
            int n = vertices.Length;
            if (n < 3) return Array.Empty<int>();

            if (n == 3)
            {
                return new int[] { 0, 1, 2 };
            }

            if (n == 4)
            {
                // Double-sided quad: visible regardless of clockwise or CCW surveying
                return new int[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
            }

            // Ear clipping for N >= 5
            List<int> indices = new List<int>(n);
            for (int i = 0; i < n; i++) indices.Add(i);

            List<int> triangles = new List<int>((n - 2) * 3);

            // Determine polygon orientation (signed area)
            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p1 = vertices[i];
                Vector3 p2 = vertices[(i + 1) % n];
                area += (p1.x * p2.z - p2.x * p1.z);
            }

            bool clockwise = area < 0f;

            int iterations = 0;
            int maxIterations = n * 4;

            while (indices.Count > 3 && iterations < maxIterations)
            {
                iterations++;
                bool earFound = false;

                for (int i = 0; i < indices.Count; i++)
                {
                    int prevIdx = indices[(i + indices.Count - 1) % indices.Count];
                    int currIdx = indices[i];
                    int nextIdx = indices[(i + 1) % indices.Count];

                    Vector3 a = vertices[prevIdx];
                    Vector3 b = vertices[currIdx];
                    Vector3 c = vertices[nextIdx];

                    // Check convexity
                    float cross = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
                    if ((clockwise && cross < 0f) || (!clockwise && cross > 0f))
                    {
                        // Check if any other vertex lies inside triangle abc
                        bool hasPointInside = false;
                        for (int j = 0; j < indices.Count; j++)
                        {
                            int testIdx = indices[j];
                            if (testIdx == prevIdx || testIdx == currIdx || testIdx == nextIdx) continue;

                            if (IsPointInTriangle(vertices[testIdx], a, b, c))
                            {
                                hasPointInside = true;
                                break;
                            }
                        }

                        if (!hasPointInside)
                        {
                            // Valid ear found: clip it
                            triangles.Add(prevIdx);
                            triangles.Add(currIdx);
                            triangles.Add(nextIdx);
                            indices.RemoveAt(i);
                            earFound = true;
                            break;
                        }
                    }
                }

                if (!earFound)
                {
                    // Fallback fan triangulation if self-intersecting or complex
                    break;
                }
            }

            if (indices.Count == 3)
            {
                triangles.Add(indices[0]);
                triangles.Add(indices[1]);
                triangles.Add(indices[2]);
            }
            else if (triangles.Count == 0)
            {
                // Fallback fan triangulation from vertex 0
                for (int i = 1; i < n - 1; i++)
                {
                    triangles.Add(0);
                    triangles.Add(i);
                    triangles.Add(i + 1);
                }
            }

            return triangles.ToArray();
        }

        private bool IsPointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);

            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }

        private float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
        }
    }
}
