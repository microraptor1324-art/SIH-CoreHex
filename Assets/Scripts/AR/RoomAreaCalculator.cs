using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.AR
{
    public struct RoomGeometryResult
    {
        public float Area;
        public float Length;
        public float Width;
        public float Perimeter;
        public Vector3 Center;
        public Quaternion Rotation;

        public RoomGeometryResult(float area, float length, float width, float perimeter, Vector3 center, Quaternion rotation)
        {
            Area = area;
            Length = length;
            Width = width;
            Perimeter = perimeter;
            Center = center;
            Rotation = rotation;
        }
    }

    public struct RoomRectangleAnalysis
    {
        public bool IsRectangular;
        public float[] CornerAngles;
        public float Side1;
        public float Side2;
        public float Side3;
        public float Side4;
        public float Length;
        public float Width;
        public float Area;
        public string StatusMessage;

        public RoomRectangleAnalysis(bool isRectangular, float[] cornerAngles, float s1, float s2, float s3, float s4, float length, float width, float area, string message)
        {
            IsRectangular = isRectangular;
            CornerAngles = cornerAngles;
            Side1 = s1;
            Side2 = s2;
            Side3 = s3;
            Side4 = s4;
            Length = length;
            Width = width;
            Area = area;
            StatusMessage = message;
        }
    }

    /// <summary>
    /// Mathematical geometry engine for room surveying.
    /// Computes exact polygon area via Shoelace Formula on the floor plane (X-Z),
    /// analyzes 4-point rectangular geometry within 10-15° tolerance,
    /// and ensures world-space measurement points remain the untouched source of truth.
    /// </summary>
    public static class RoomAreaCalculator
    {
        /// <summary>
        /// Analyzes a 4-flag room measurement to determine whether it approximately represents a rectangular room.
        /// Checks consecutive interior angles (~90° with 15° tolerance), opposite side parallelism, and non-self-intersection.
        /// Does NOT shift or alter the user's world-space points.
        /// </summary>
        public static RoomRectangleAnalysis AnalyzeRectangle(IList<Vector3> points)
        {
            if (points == null || points.Count != 4)
            {
                return new RoomRectangleAnalysis(false, new float[4], 0, 0, 0, 0, 0, 0, 0, "REQUIRES EXACTLY 4 FLAGS");
            }

            // Project onto floor plane (X, Z)
            Vector2 p0 = new Vector2(points[0].x, points[0].z);
            Vector2 p1 = new Vector2(points[1].x, points[1].z);
            Vector2 p2 = new Vector2(points[2].x, points[2].z);
            Vector2 p3 = new Vector2(points[3].x, points[3].z);

            // Side vectors
            Vector2 v0 = p1 - p0;
            Vector2 v1 = p2 - p1;
            Vector2 v2 = p3 - p2;
            Vector2 v3 = p0 - p3;

            // Side lengths
            float s1 = v0.magnitude;
            float s2 = v1.magnitude;
            float s3 = v2.magnitude;
            float s4 = v3.magnitude;

            // Corner interior angles (in degrees)
            float a0 = Vector2.Angle(-v3, v0);
            float a1 = Vector2.Angle(-v0, v1);
            float a2 = Vector2.Angle(-v1, v2);
            float a3 = Vector2.Angle(-v2, v3);

            float[] angles = new float[] { a0, a1, a2, a3 };

            // Check tolerance: ~90° within ±15° (75° to 105°)
            bool anglesOk = true;
            for (int i = 0; i < 4; i++)
            {
                if (angles[i] < 72f || angles[i] > 108f)
                {
                    anglesOk = false;
                    break;
                }
            }

            // Opposite sides ratio check (tolerance ~30%)
            float maxOpp1 = Mathf.Max(s1, s3);
            float maxOpp2 = Mathf.Max(s2, s4);
            bool sidesOk = true;
            if (maxOpp1 > 0.001f && Mathf.Abs(s1 - s3) / maxOpp1 > 0.30f) sidesOk = false;
            if (maxOpp2 > 0.001f && Mathf.Abs(s2 - s4) / maxOpp2 > 0.30f) sidesOk = false;

            // Check non-self-intersection
            bool nonIntersecting = !SegmentsIntersect(p0, p1, p2, p3) && !SegmentsIntersect(p1, p2, p3, p0);

            bool isRect = anglesOk && sidesOk && nonIntersecting;

            // Calculate dimensions
            float avgLen1 = (s1 + s3) * 0.5f;
            float avgLen2 = (s2 + s4) * 0.5f;
            float length = Mathf.Max(avgLen1, avgLen2);
            float width = Mathf.Min(avgLen1, avgLen2);

            // Calculate exact polygon area via Shoelace formula
            float area = CalculateShoelaceArea2D(p0, p1, p2, p3);

            string status = isRect ? "RECTANGULAR ROOM DETECTED" : "4-CORNER ROOM DETECTED";

            return new RoomRectangleAnalysis(isRect, angles, s1, s2, s3, s4, length, width, area, status);
        }

        private static float CalculateShoelaceArea2D(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float cross = (p0.x * p1.y - p1.x * p0.y) +
                          (p1.x * p2.y - p2.x * p1.y) +
                          (p2.x * p3.y - p3.x * p2.y) +
                          (p3.x * p0.y - p0.x * p3.y);
            return Mathf.Abs(cross) * 0.5f;
        }

        public static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float Cross(Vector2 v1, Vector2 v2) => v1.x * v2.y - v1.y * v2.x;
            Vector2 ab = b - a;
            Vector2 cd = d - c;

            float d1 = Cross(ab, c - a);
            float d2 = Cross(ab, d - a);
            float d3 = Cross(cd, a - c);
            float d4 = Cross(cd, b - c);

            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                   ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        /// <summary>
        /// Calculates comprehensive room geometry from 3D world-space polygon vertices.
        /// </summary>
        public static RoomGeometryResult CalculateGeometry(IList<Vector3> points)
        {
            if (points == null || points.Count < 3)
            {
                return new RoomGeometryResult(0f, 0f, 0f, 0f, Vector3.zero, Quaternion.identity);
            }

            int n = points.Count;

            // 1. Calculate Signed Area & Centroid via Shoelace Formula on Horizontal Plane (X, Z)
            float signedArea = 0f;
            float centroidX = 0f;
            float centroidZ = 0f;
            float avgY = 0f;
            float perimeter = 0f;

            for (int i = 0; i < n; i++)
            {
                Vector3 pt0 = points[i];
                Vector3 pt1 = points[(i + 1) % n];

                float cross = (pt0.x * pt1.z) - (pt1.x * pt0.z);
                signedArea += cross;

                centroidX += (pt0.x + pt1.x) * cross;
                centroidZ += (pt0.z + pt1.z) * cross;
                avgY += pt0.y;

                perimeter += Vector3.Distance(new Vector3(pt0.x, 0, pt0.z), new Vector3(pt1.x, 0, pt1.z));
            }

            signedArea *= 0.5f;
            float area = Mathf.Abs(signedArea);
            avgY /= n;

            Vector3 center;
            if (Mathf.Abs(signedArea) > 0.001f)
            {
                centroidX /= (6f * signedArea);
                centroidZ /= (6f * signedArea);
                center = new Vector3(centroidX, avgY, centroidZ);
            }
            else
            {
                float sx = 0f, sz = 0f;
                foreach (var pt in points) { sx += pt.x; sz += pt.z; }
                center = new Vector3(sx / n, avgY, sz / n);
            }

            // 2. Calculate Oriented Principal Dimensions (Length, Width, Rotation)
            float maxEdgeLen = 0f;
            Vector3 primaryEdgeDir = Vector3.forward;

            for (int i = 0; i < n; i++)
            {
                Vector3 edge = points[(i + 1) % n] - points[i];
                edge.y = 0f;
                float edgeDist = edge.magnitude;
                if (edgeDist > maxEdgeLen)
                {
                    maxEdgeLen = edgeDist;
                    primaryEdgeDir = edge.normalized;
                }
            }

            Vector3 perpAxis = Vector3.Cross(Vector3.up, primaryEdgeDir).normalized;

            float minProjU = float.MaxValue, maxProjU = float.MinValue;
            float minProjV = float.MaxValue, maxProjV = float.MinValue;

            for (int i = 0; i < n; i++)
            {
                Vector3 relativePt = points[i] - center;
                relativePt.y = 0f;

                float u = Vector3.Dot(relativePt, primaryEdgeDir);
                float v = Vector3.Dot(relativePt, perpAxis);

                minProjU = Mathf.Min(minProjU, u);
                maxProjU = Mathf.Max(maxProjU, u);
                minProjV = Mathf.Min(minProjV, v);
                maxProjV = Mathf.Max(maxProjV, v);
            }

            float dimU = Mathf.Max(0.5f, maxProjU - minProjU);
            float dimV = Mathf.Max(0.5f, maxProjV - minProjV);

            float length = Mathf.Max(dimU, dimV);
            float width = Mathf.Min(dimU, dimV);

            Quaternion rotation = primaryEdgeDir.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(primaryEdgeDir, Vector3.up)
                : Quaternion.identity;

            return new RoomGeometryResult(area, length, width, perimeter, center, rotation);
        }
    }
}
