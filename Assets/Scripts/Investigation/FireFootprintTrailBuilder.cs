using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.AR;

namespace ARMiningSimulator.Investigation
{
    /// <summary>
    /// Procedurally builds high-visibility burnt footprints and scorched soot evidence
    /// on the mine drift floor leading from the evacuation path to the origin machine.
    /// Also attaches floating forensic inspection markers to candidate machines.
    /// </summary>
    public static class FireFootprintTrailBuilder
    {
        private static GameObject s_ActiveTrailRoot;
        private static Texture2D s_FootprintTex;
        private static Texture2D s_ScorchDecalTex;
        private static Material s_FootprintMat;
        private static Material s_ScorchMat;

        public static GameObject ActiveTrailRoot => s_ActiveTrailRoot;

        private static void InitTexturesAndMaterials()
        {
            if (s_FootprintTex == null)
            {
                s_FootprintTex = GenerateBootFootprintTexture();
            }

            if (s_ScorchDecalTex == null)
            {
                s_ScorchDecalTex = GenerateScorchDecalTexture();
            }

            if (s_FootprintMat == null)
            {
                s_FootprintMat = ARMaterialHelper.CreateUnlitMaterial(new Color(0.12f, 0.08f, 0.06f, 0.95f), transparent: true);
                s_FootprintMat.name = "Procedural_BurntFootprintMat";
                if (s_FootprintMat.HasProperty("_BaseMap")) s_FootprintMat.SetTexture("_BaseMap", s_FootprintTex);
                if (s_FootprintMat.HasProperty("_MainTex")) s_FootprintMat.SetTexture("_MainTex", s_FootprintTex);
            }

            if (s_ScorchMat == null)
            {
                s_ScorchMat = ARMaterialHelper.CreateUnlitMaterial(new Color(0.08f, 0.06f, 0.04f, 0.90f), transparent: true);
                s_ScorchMat.name = "Procedural_ScorchPuddleMat";
                if (s_ScorchMat.HasProperty("_BaseMap")) s_ScorchMat.SetTexture("_BaseMap", s_ScorchDecalTex);
                if (s_ScorchMat.HasProperty("_MainTex")) s_ScorchMat.SetTexture("_MainTex", s_ScorchDecalTex);
            }
        }

        /// <summary>
        /// Clears any existing evidence trail.
        /// </summary>
        public static void ClearTrail()
        {
            if (s_ActiveTrailRoot != null)
            {
                Object.Destroy(s_ActiveTrailRoot);
                s_ActiveTrailRoot = null;
            }
        }

        /// <summary>
        /// Spawns burnt footprints on the floor leading to the origin machine,
        /// a scorch puddle beneath it, and an in-world forensic evidence marker.
        /// </summary>
        public static GameObject SpawnTrail(GameObject originMachine, Vector3? startPoint = null)
        {
            ClearTrail();
            if (originMachine == null) return null;

            InitTexturesAndMaterials();

            s_ActiveTrailRoot = new GameObject("FireInvestigation_FootprintTrail");

            Vector3 machinePos = originMachine.transform.position;
            machinePos.y = 0.02f;

            // Determine trail starting location (e.g. evacuation walking corridor or player location)
            Vector3 startPos = startPoint.HasValue
                ? startPoint.Value
                : (Camera.main != null ? Camera.main.transform.position : machinePos + Vector3.back * 3.5f);
            startPos.y = 0.02f;

            // 1. Generate trail of burnt footprints leading to machine
            BuildFootprintPath(s_ActiveTrailRoot.transform, startPos, machinePos);

            // 2. Generate large charred scorch decal under the machine
            BuildScorchPuddle(s_ActiveTrailRoot.transform, machinePos);

            // 3. Floating 3D forensic origin beacon above the machine
            BuildEvidenceBeacon(originMachine.transform);

            Debug.Log($"[FireFootprintTrailBuilder] 👣 Burnt footprints spawned leading to '{originMachine.name}' at {machinePos}!");
            return s_ActiveTrailRoot;
        }

        private static void BuildFootprintPath(Transform parent, Vector3 from, Vector3 to)
        {
            float dist = Vector3.Distance(from, to);
            if (dist < 0.6f) return;

            Vector3 dir = (to - from).normalized;
            Quaternion forwardRot = Quaternion.LookRotation(dir, Vector3.up);

            // Stride step spacing
            float stepDistance = 0.40f;
            int stepCount = Mathf.Clamp(Mathf.FloorToInt(dist / stepDistance), 3, 24);

            float lateralOffset = 0.12f;

            for (int i = 1; i <= stepCount; i++)
            {
                float t = (float)i / (stepCount + 1);
                Vector3 centerStep = Vector3.Lerp(from, to, t);

                // Alternate left and right boot footsteps
                bool isLeft = (i % 2 == 0);
                Vector3 rightVec = Vector3.Cross(Vector3.up, dir);
                Vector3 stepPos = centerStep + (rightVec * (isLeft ? -lateralOffset : lateralOffset));
                stepPos.y = 0.025f + (i * 0.0005f); // Subtle height sorting

                GameObject printGo = new GameObject($"Footprint_{(isLeft ? "L" : "R")}_{i}");
                printGo.transform.SetParent(parent, true);
                printGo.transform.position = stepPos;
                // Add slight natural toe-out angle
                float toeAngle = isLeft ? -8f : 8f;
                printGo.transform.rotation = forwardRot * Quaternion.Euler(90f, toeAngle, 0f);
                printGo.transform.localScale = new Vector3(0.24f, 0.42f, 1f);

                var mf = printGo.AddComponent<MeshFilter>();
                mf.sharedMesh = GetQuadMesh();

                var mr = printGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = s_FootprintMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        private static void BuildScorchPuddle(Transform parent, Vector3 pos)
        {
            GameObject puddleGo = new GameObject("Origin_ScorchPuddle");
            puddleGo.transform.SetParent(parent, true);
            puddleGo.transform.position = new Vector3(pos.x, 0.02f, pos.z);
            puddleGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            puddleGo.transform.localScale = new Vector3(1.6f, 1.6f, 1f);

            var mf = puddleGo.AddComponent<MeshFilter>();
            mf.sharedMesh = GetQuadMesh();

            var mr = puddleGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = s_ScorchMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static void BuildEvidenceBeacon(Transform machineTransform)
        {
            GameObject beaconGo = new GameObject("Forensic_EvidenceBeacon");
            beaconGo.transform.SetParent(machineTransform, false);
            beaconGo.transform.localPosition = new Vector3(0, 1.45f, 0);

            TextMesh tm = beaconGo.AddComponent<TextMesh>();
            tm.text = "👣 BURNT FOOTPRINTS DETECTED!\n🔍 [INSPECT MACHINE EVIDENCE]";
            tm.fontSize = 24;
            tm.characterSize = 0.035f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1.0f, 0.75f, 0.15f);
            tm.fontStyle = FontStyle.Bold;

            beaconGo.AddComponent<EvidenceBeaconPulsator>();
        }

        private static Mesh s_QuadMesh;
        private static Mesh GetQuadMesh()
        {
            if (s_QuadMesh != null) return s_QuadMesh;

            s_QuadMesh = new Mesh { name = "Procedural_EvidenceQuad" };
            s_QuadMesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3( 0.5f, -0.5f, 0),
                new Vector3(-0.5f,  0.5f, 0),
                new Vector3( 0.5f,  0.5f, 0)
            };
            s_QuadMesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            s_QuadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            s_QuadMesh.RecalculateNormals();
            s_QuadMesh.RecalculateBounds();
            return s_QuadMesh;
        }

        /// <summary>
        /// Generates a realistic charred boot footprint texture with heel, arch, ball, and tread ribs.
        /// </summary>
        private static Texture2D GenerateBootFootprintTexture()
        {
            int w = 64;
            int h = 128;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "Procedural_BootFootprint";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[w * h];
            Vector2 heelCenter = new Vector2(w * 0.5f, h * 0.22f);
            Vector2 ballCenter = new Vector2(w * 0.5f, h * 0.70f);

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    float alpha = 0f;

                    // Heel portion (oval at bottom)
                    if (ny <= 0.38f)
                    {
                        float dx = (x - heelCenter.x) / (w * 0.32f);
                        float dy = (y - heelCenter.y) / (h * 0.16f);
                        float distSq = dx * dx + dy * dy;
                        if (distSq <= 1.0f)
                        {
                            alpha = Mathf.SmoothStep(1.0f, 0.4f, distSq);
                        }
                    }
                    // Sole/ball portion (wider at top)
                    else if (ny >= 0.44f && ny <= 0.95f)
                    {
                        float dx = (x - ballCenter.x) / (w * 0.40f);
                        float dy = (y - ballCenter.y) / (h * 0.26f);
                        float distSq = dx * dx + dy * dy;
                        if (distSq <= 1.0f)
                        {
                            alpha = Mathf.SmoothStep(1.0f, 0.3f, distSq);
                            // Add horizontal tread rib pattern
                            float tread = Mathf.Sin(ny * 60f);
                            if (tread > 0.4f) alpha *= 0.75f;
                        }
                    }
                    // Narrow arch transition
                    else if (ny > 0.38f && ny < 0.44f)
                    {
                        float dx = Mathf.Abs(nx - 0.5f);
                        if (dx < 0.18f)
                        {
                            alpha = 0.55f;
                        }
                    }

                    // Burned charred carbon color with glowing orange ember edge
                    if (alpha > 0.05f)
                    {
                        Color carbon = new Color(0.08f, 0.06f, 0.05f, alpha * 0.92f);
                        // Hot ember rim
                        if (alpha < 0.35f)
                        {
                            carbon = Color.Lerp(carbon, new Color(1.0f, 0.45f, 0.10f, alpha), 0.5f);
                        }
                        pixels[y * w + x] = carbon;
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a charred radial scorch puddle with irregular soot edge falloff.
        /// </summary>
        private static Texture2D GenerateScorchDecalTexture()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Procedural_ScorchPuddle";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = Mathf.Clamp01(dist / maxR);
                    float alpha = Mathf.SmoothStep(0.90f, 0.0f, norm);
                    alpha = Mathf.Pow(alpha, 1.4f);

                    // Add radial noise irregularity
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float noise = Mathf.Sin(angle * 6f) * 0.08f;
                    alpha = Mathf.Clamp01(alpha + noise);

                    Color col = Color.Lerp(new Color(0.05f, 0.04f, 0.03f, alpha * 0.90f), new Color(0.35f, 0.12f, 0.04f, alpha * 0.4f), norm);
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }

    /// <summary>
    /// Smoothly rotates and pulses the floating forensic evidence beacon toward the camera.
    /// </summary>
    public class EvidenceBeaconPulsator : MonoBehaviour
    {
        private Vector3 _basePos;

        private void Start()
        {
            _basePos = transform.position;
        }

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * 2.5f) * 0.08f;
            transform.position = _basePos + new Vector3(0, bob, 0);

            if (Camera.main != null)
            {
                Vector3 toCam = transform.position - Camera.main.transform.position;
                if (toCam.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(toCam);
                }
            }
        }
    }
}
