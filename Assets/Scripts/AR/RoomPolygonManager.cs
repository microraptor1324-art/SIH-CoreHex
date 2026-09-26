using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Generates a procedural flat 3D mesh covering the measured room polygon,
    /// shaded with a semi-transparent AR floor material physically locked to world coordinates,
    /// and 4 perimeter vertical boundary walls shaded in 50% blue fading upward to 0% at 5 meters.
    /// Supports triangles, rectangles, L-shaped rooms, and arbitrary simple polygons.
    /// </summary>
    public class RoomPolygonManager : MonoBehaviour
    {
        [Header("Floor Shading Visuals")]
        [SerializeField] private Color _shadingColor = new Color(0.0f, 0.40f, 1.0f, 0.40f); // 40% Blue color shade (10% lighter than 50%)
        [SerializeField] private Color _boundaryGlowColor = new Color(0.1f, 0.6f, 1.0f, 0.95f); // Blue outline

        [Header("Fading Walls Visuals")]
        [SerializeField] private bool _enableFadingWalls = true;
        [SerializeField] private float _wallHeight = 5.0f; // Fades out completely at 5 meters
        [SerializeField] private int _wallVerticalSegments = 16; // Subdivisions for smooth vertical gradient

        private GameObject _shadedMeshObj;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _shadingMaterial;

        private GameObject _wallsMeshObj;
        private MeshFilter _wallsFilter;
        private MeshRenderer _wallsRenderer;
        private Material _wallMaterial;

        public bool EnableFadingWalls
        {
            get => _enableFadingWalls;
            set
            {
                _enableFadingWalls = value;
                if (_wallsMeshObj != null) _wallsMeshObj.SetActive(value);
            }
        }

        public float WallHeight
        {
            get => _wallHeight;
            set => _wallHeight = Mathf.Max(0.5f, value);
        }

        public Color ShadingColor
        {
            get => _shadingColor;
            set
            {
                _shadingColor = value;
                UpdateMaterialColors();
            }
        }

        private void Awake()
        {
            SetupMaterial();
        }

        private void SetupMaterial()
        {
            _shadingMaterial = ARMaterialHelper.GetRoomShadingMaterial(_shadingColor);
            _wallMaterial = ARMaterialHelper.GetWallFadingMaterial(_shadingColor);
        }

        private void UpdateMaterialColors()
        {
            if (_shadingMaterial != null)
            {
                _shadingMaterial.color = _shadingColor;
                if (_shadingMaterial.HasProperty("_BaseColor"))
                    _shadingMaterial.SetColor("_BaseColor", _shadingColor);
            }

            if (_wallMaterial != null)
            {
                _wallMaterial.color = _shadingColor;
                if (_wallMaterial.HasProperty("_BaseColor"))
                    _wallMaterial.SetColor("_BaseColor", _shadingColor);
            }
        }

        /// <summary>
        /// Builds and renders the shaded floor polygon mesh covering all measured points,
        /// plus 4 vertical boundary walls rising 5 meters and fading upward.
        /// </summary>
        public void CreateShadedFloorPolygon(IList<Vector3> worldPoints)
        {
            ClearShadedFloorPolygon();

            if (worldPoints == null || worldPoints.Count < 3) return;

            if (_shadingMaterial == null || _wallMaterial == null)
            {
                SetupMaterial();
            }

            UpdateMaterialColors();

            // 1. Generate Floor Polygon Mesh
            _shadedMeshObj = new GameObject("AR_ShadedRoomFloor");
            _shadedMeshObj.transform.SetParent(transform, false);

            _meshFilter = _shadedMeshObj.AddComponent<MeshFilter>();
            _meshRenderer = _shadedMeshObj.AddComponent<MeshRenderer>();
            _meshRenderer.sharedMaterial = _shadingMaterial;

            Mesh floorMesh = BuildPolygonMesh(worldPoints);
            _meshFilter.sharedMesh = floorMesh;

            // 2. Generate 4 Fading Boundary Walls (fades from 50% blue at base to 0% at 5 meters)
            if (_enableFadingWalls)
            {
                _wallsMeshObj = new GameObject("AR_FadingBoundaryWalls");
                _wallsMeshObj.transform.SetParent(transform, false);

                _wallsFilter = _wallsMeshObj.AddComponent<MeshFilter>();
                _wallsRenderer = _wallsMeshObj.AddComponent<MeshRenderer>();
                _wallsRenderer.sharedMaterial = _wallMaterial;

                Mesh wallsMesh = BuildWallsMesh(worldPoints);
                _wallsFilter.sharedMesh = wallsMesh;

                Debug.Log($"[RoomPolygonManager] Generated {worldPoints.Count} fading boundary walls (height: {_wallHeight:F1}m, vertices: {wallsMesh.vertexCount}, triangles: {wallsMesh.triangles.Length / 3})");
            }

            Debug.Log($"[RoomPolygonManager] Generated 50% blue shaded floor mesh with {floorMesh.vertexCount} vertices and {floorMesh.triangles.Length / 3} triangles.");
        }

        public void ClearShadedFloorPolygon()
        {
            if (_shadedMeshObj != null)
            {
                Destroy(_shadedMeshObj);
                _shadedMeshObj = null;
            }

            if (_wallsMeshObj != null)
            {
                Destroy(_wallsMeshObj);
                _wallsMeshObj = null;
            }
        }

        /// <summary>
        /// Toggles the visibility of both the floor shading mesh and the fading boundary walls.
        /// Allows quickly hiding or showing the blue shading.
        /// </summary>
        public void SetShadingVisible(bool visible)
        {
            if (_shadedMeshObj != null) _shadedMeshObj.SetActive(visible);
            if (_wallsMeshObj != null) _wallsMeshObj.SetActive(visible);
        }

        private Mesh BuildPolygonMesh(IList<Vector3> points)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoomFloorMesh";

            int n = points.Count;
            Vector3[] vertices = new Vector3[n];
            Vector2[] uvs = new Vector2[n];
            Color[] colors = new Color[n];

            // Elevate slightly (+0.004m) to sit cleanly above the physical floor and below laser lines (+0.008m)
            for (int i = 0; i < n; i++)
            {
                vertices[i] = points[i] + Vector3.up * 0.004f;
                uvs[i] = new Vector2(points[i].x, points[i].z);
                colors[i] = _shadingColor;
            }

            int[] triangles = TriangulatePolygon(vertices);

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates vertical boundary wall meshes along all polygon sides.
        /// Each wall starts at floor level with 50% blue opacity and smoothly fades out to 0% at 5 meters height.
        /// Quads are double-sided (both CW and CCW winding) to be visible from both inside and outside the room.
        /// </summary>
        private Mesh BuildWallsMesh(IList<Vector3> points)
        {
            Mesh mesh = new Mesh();
            mesh.name = "RoomFadingWallsMesh";

            int sideCount = points.Count;
            int segments = Mathf.Max(1, _wallVerticalSegments);
            int vertsPerSide = (segments + 1) * 2;
            int totalVerts = sideCount * vertsPerSide;

            // 2 quads per segment (double-sided) = 4 triangles = 12 indices per segment
            int quadsPerSide = segments;
            int trisPerSide = quadsPerSide * 2 * 2; // double-sided (both windings)
            int totalIndices = sideCount * trisPerSide * 3;

            Vector3[] vertices = new Vector3[totalVerts];
            Vector2[] uvs = new Vector2[totalVerts];
            Color[] colors = new Color[totalVerts];
            int[] triangles = new int[totalIndices];

            int vertOffset = 0;
            int triOffset = 0;

            for (int side = 0; side < sideCount; side++)
            {
                Vector3 p0 = points[side];
                Vector3 p1 = points[(side + 1) % sideCount];

                int sideBaseVert = vertOffset;

                // Create vertices row by row from bottom (t = 0) to top (t = 1)
                for (int seg = 0; seg <= segments; seg++)
                {
                    float t = (float)seg / segments; // 0.0 at floor to 1.0 at 5m
                    float yOffset = 0.004f + t * _wallHeight;

                    // Smooth cosine fade curve: alpha = _shadingColor.a at floor, 0.0 at _wallHeight
                    float alphaFade = Mathf.Cos(t * Mathf.PI * 0.5f);
                    Color vertexColor = new Color(_shadingColor.r, _shadingColor.g, _shadingColor.b, _shadingColor.a * alphaFade);

                    Vector3 vLeft = p0 + Vector3.up * yOffset;
                    Vector3 vRight = p1 + Vector3.up * yOffset;

                    vertices[vertOffset] = vLeft;
                    uvs[vertOffset] = new Vector2(0f, t);
                    colors[vertOffset] = vertexColor;
                    vertOffset++;

                    vertices[vertOffset] = vRight;
                    uvs[vertOffset] = new Vector2(1f, t);
                    colors[vertOffset] = vertexColor;
                    vertOffset++;
                }

                // Build double-sided quads between row seg and seg+1
                for (int seg = 0; seg < segments; seg++)
                {
                    int rowBottom = sideBaseVert + seg * 2;
                    int rowTop = sideBaseVert + (seg + 1) * 2;

                    int bl = rowBottom;     // bottom-left
                    int br = rowBottom + 1; // bottom-right
                    int tl = rowTop;        // top-left
                    int tr = rowTop + 1;    // top-right

                    // Face 1: Outward / Front
                    triangles[triOffset++] = bl;
                    triangles[triOffset++] = tl;
                    triangles[triOffset++] = tr;

                    triangles[triOffset++] = bl;
                    triangles[triOffset++] = tr;
                    triangles[triOffset++] = br;

                    // Face 2: Inward / Back (reversed winding so visible from inside the room)
                    triangles[triOffset++] = bl;
                    triangles[triOffset++] = tr;
                    triangles[triOffset++] = tl;

                    triangles[triOffset++] = bl;
                    triangles[triOffset++] = br;
                    triangles[triOffset++] = tr;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
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
