using UnityEngine;
using ARMiningSimulator.AR;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Distinct hazard profiles for realistic underground mining fire occurrences.
    /// </summary>
    public enum FireHazardType
    {
        StandardEquipment = 0,
        ElectricalArc = 1,
        MechanicalFriction = 2,
        VehicleHydraulic = 3,
        BatteryThermalRunaway = 4
    }

    /// <summary>
    /// Bundled return structure containing all components of the procedural fire visual effect.
    /// Supports tuple deconstruction for full backwards compatibility.
    /// </summary>
    public struct FireVisualResult
    {
        public ParticleSystem fire;
        public ParticleSystem smoke;
        public Light fireLight;
        public ParticleSystem sparks;
        public GameObject flameMesh;
        public GameObject hazardMarker;

        public void Deconstruct(out ParticleSystem f, out ParticleSystem s, out Light l, out ParticleSystem sp)
        {
            f = fire;
            s = smoke;
            l = fireLight;
            sp = sparks;
        }

        public void Deconstruct(out ParticleSystem f, out ParticleSystem s, out Light l, out ParticleSystem sp, out GameObject fm, out GameObject hm)
        {
            f = fire;
            s = smoke;
            l = fireLight;
            sp = sparks;
            fm = flameMesh;
            hm = hazardMarker;
        }
    }

    /// <summary>
    /// Programmatically generates vibrant, high-visibility 3D animated flame geometry,
    /// soft-combustion particle systems, atmospheric smoke plumes, and flickering firelight.
    /// Guarantees 100% visibility on all devices (mobile AR & editor) with zero chance of missing shaders.
    /// </summary>
    public static class ProceduralFireParticleBuilder
    {
        private static Material s_FireMaterial;
        private static Material s_SmokeMaterial;
        private static Material s_BlackSmokeMaterial;
        private static Material s_SparksMaterial;
        private static Material s_ChemicalFlameMaterial;
        private static Material s_MeshFlameOuterMat;
        private static Material s_MeshFlameInnerMat;
        private static Texture2D s_SoftParticleTex;

        private static Texture2D GetOrCreateSoftParticleTexture()
        {
            if (s_SoftParticleTex != null) return s_SoftParticleTex;

            int size = 64;
            s_SoftParticleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            s_SoftParticleTex.name = "Procedural_SoftParticleTex";
            s_SoftParticleTex.wrapMode = TextureWrapMode.Clamp;
            s_SoftParticleTex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float norm = Mathf.Clamp01(dist / maxDist);
                    // Smooth hermite falloff with power curve for bright glowing core
                    float alpha = Mathf.SmoothStep(1.0f, 0.0f, norm);
                    alpha = Mathf.Pow(alpha, 1.8f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            s_SoftParticleTex.SetPixels(pixels);
            s_SoftParticleTex.Apply();
            return s_SoftParticleTex;
        }

        private static Material CreateParticleMaterial(string name, Color color)
        {
            Texture2D softTex = GetOrCreateSoftParticleTexture();
            Material mat = ARMaterialHelper.CreateUnlitMaterial(color, transparent: true);
            mat.name = name;
            if (mat.HasProperty("_BaseMap") && softTex != null) mat.SetTexture("_BaseMap", softTex);
            if (mat.HasProperty("_MainTex") && softTex != null) mat.SetTexture("_MainTex", softTex);
            return mat;
        }

        private static void InitMaterials()
        {
            if (s_FireMaterial == null)
            {
                s_FireMaterial = CreateParticleMaterial("Procedural_FireMat", new Color(1.0f, 0.52f, 0.06f, 0.90f));
                s_SmokeMaterial = CreateParticleMaterial("Procedural_SmokeMat", new Color(0.55f, 0.55f, 0.58f, 0.35f));
                s_BlackSmokeMaterial = CreateParticleMaterial("Procedural_BlackSmokeMat", new Color(0.24f, 0.24f, 0.26f, 0.40f));
                s_SparksMaterial = CreateParticleMaterial("Procedural_SparksMat", new Color(1.0f, 0.95f, 0.45f, 1.0f));
                s_ChemicalFlameMaterial = CreateParticleMaterial("Procedural_ChemicalFlameMat", new Color(1.0f, 0.75f, 0.2f, 0.92f));

                s_MeshFlameOuterMat = ARMaterialHelper.CreateUnlitMaterial(new Color(1.0f, 0.45f, 0.02f, 0.90f), transparent: true);
                s_MeshFlameOuterMat.name = "Procedural_MeshFlameOuterMat";

                s_MeshFlameInnerMat = ARMaterialHelper.CreateUnlitMaterial(new Color(1.0f, 0.95f, 0.35f, 0.95f), transparent: true);
                s_MeshFlameInnerMat.name = "Procedural_MeshFlameInnerMat";
            }
        }

        public static FireVisualResult BuildFireEffect(
            Transform parent,
            FireHazardType hazardType = FireHazardType.StandardEquipment,
            string targetName = "")
        {
            InitMaterials();

            GameObject effectRoot = new GameObject($"FireEffect_{hazardType}");
            effectRoot.transform.SetParent(parent, false);
            effectRoot.transform.localPosition = Vector3.zero;

            // =========================================================================
            // 1. GUARANTEED 3D ANIMATED FLAME MESH (Visible on all platforms & renderers)
            // =========================================================================
            GameObject flameMeshObj = Build3DFlameMesh(effectRoot.transform, hazardType);

            // =========================================================================
            // 2. WORLD-SPACE HAZARD INDICATOR (Floating beacon above burning equipment)
            // =========================================================================
            GameObject markerObj = BuildHazardMarker(effectRoot.transform, targetName);

            // =========================================================================
            // 3. FIRE PARTICLE SYSTEM (Soft Glow Combustion)
            // =========================================================================
            GameObject fireGo = new GameObject("FireParticles");
            fireGo.transform.SetParent(effectRoot.transform, false);

            ParticleSystem firePS = fireGo.AddComponent<ParticleSystem>();
            var fireMain = firePS.main;
            fireMain.duration = 1.0f;
            fireMain.loop = true;
            fireMain.maxParticles = 36;
            fireMain.simulationSpace = ParticleSystemSimulationSpace.World;
            fireMain.playOnAwake = true;
            fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.40f, 0.75f);
            fireMain.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            fireMain.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);

            var fireEmission = firePS.emission;
            fireEmission.rateOverTime = 22f;

            var fireShape = firePS.shape;
            fireShape.shapeType = ParticleSystemShapeType.Cone;
            fireShape.angle = 14f;
            fireShape.radius = 0.18f;

            var fireSizeOverLife = firePS.sizeOverLifetime;
            fireSizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0.0f, 0.40f);
            sizeCurve.AddKey(0.35f, 1.0f);
            sizeCurve.AddKey(1.0f, 0.20f);
            fireSizeOverLife.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            var fireColorOverLife = firePS.colorOverLifetime;
            fireColorOverLife.enabled = true;
            Gradient fireGrad = new Gradient();
            fireGrad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0.0f), new GradientColorKey(new Color(1f, 0.35f, 0.02f), 0.6f), new GradientColorKey(new Color(0.7f, 0.1f, 0.0f), 1.0f) },
                new[] { new GradientAlphaKey(0.95f, 0.0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            fireColorOverLife.color = fireGrad;

            var fireRenderer = fireGo.GetComponent<ParticleSystemRenderer>();
            fireRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            fireRenderer.sharedMaterial = hazardType == FireHazardType.BatteryThermalRunaway ? s_ChemicalFlameMaterial : s_FireMaterial;

            // =========================================================================
            // 4. SMOKE PARTICLE SYSTEM (Rising Light Translucent Plume)
            // =========================================================================
            GameObject smokeGo = new GameObject("SmokeParticles");
            smokeGo.transform.SetParent(effectRoot.transform, false);
            smokeGo.transform.localPosition = new Vector3(0, 0.35f, 0);

            ParticleSystem smokePS = smokeGo.AddComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.duration = 1.0f;
            smokeMain.loop = true;
            smokeMain.maxParticles = 24;
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
            smokeMain.playOnAwake = true;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.40f, 0.85f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            smokeMain.startColor = new Color(0.40f, 0.40f, 0.44f, 0.32f);

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 12f;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 18f;
            smokeShape.radius = 0.20f;

            var smokeSizeOverLife = smokePS.sizeOverLifetime;
            smokeSizeOverLife.enabled = true;
            AnimationCurve smokeCurve = new AnimationCurve();
            smokeCurve.AddKey(0.0f, 0.30f);
            smokeCurve.AddKey(1.0f, 1.8f);
            smokeSizeOverLife.size = new ParticleSystem.MinMaxCurve(1.0f, smokeCurve);

            var smokeColorOverLife = smokePS.colorOverLifetime;
            smokeColorOverLife.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.35f, 0.35f, 0.38f), 0.0f), new GradientColorKey(new Color(0.55f, 0.55f, 0.58f), 1.0f) },
                new[] { new GradientAlphaKey(0.32f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            smokeColorOverLife.color = smokeGrad;

            var smokeRenderer = smokeGo.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            smokeRenderer.sharedMaterial = hazardType == FireHazardType.VehicleHydraulic ? s_BlackSmokeMaterial : s_SmokeMaterial;

            // =========================================================================
            // 5. ELECTRICAL SPARKS (For Electrical Arc & Battery Hazards)
            // =========================================================================
            ParticleSystem sparksPS = null;
            if (hazardType == FireHazardType.ElectricalArc || hazardType == FireHazardType.BatteryThermalRunaway)
            {
                GameObject sparksGo = new GameObject("ElectricalSparks");
                sparksGo.transform.SetParent(effectRoot.transform, false);

                sparksPS = sparksGo.AddComponent<ParticleSystem>();
                var spMain = sparksPS.main;
                spMain.duration = 1.0f;
                spMain.loop = true;
                spMain.maxParticles = 28;
                spMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.40f);
                spMain.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.5f);
                spMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
                spMain.startColor = new Color(1.0f, 0.95f, 0.45f, 1.0f);
                spMain.gravityModifier = 1.2f;
                spMain.simulationSpace = ParticleSystemSimulationSpace.World;
                spMain.playOnAwake = true;

                var spEmission = sparksPS.emission;
                spEmission.rateOverTime = 16f;

                var spShape = sparksPS.shape;
                spShape.shapeType = ParticleSystemShapeType.Sphere;
                spShape.radius = 0.20f;

                var spRenderer = sparksGo.GetComponent<ParticleSystemRenderer>();
                spRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                spRenderer.sharedMaterial = s_SparksMaterial;
            }

            // =========================================================================
            // 6. HIGH-VISIBILITY FLICKERING FIRE LIGHT
            // =========================================================================
            GameObject lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(effectRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.45f, 0);

            Light fireLight = lightGo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.range = 5.0f;
            fireLight.intensity = 3.5f;
            fireLight.shadows = LightShadows.None;
            fireLight.color = hazardType == FireHazardType.ElectricalArc ? new Color(0.95f, 0.90f, 1.0f) : new Color(1.0f, 0.50f, 0.12f);

            lightGo.AddComponent<FireLightFlicker>();

            return new FireVisualResult
            {
                fire = firePS,
                smoke = smokePS,
                fireLight = fireLight,
                sparks = sparksPS,
                flameMesh = flameMeshObj,
                hazardMarker = markerObj
            };
        }

        /// <summary>
        /// Constructs stylized 3D flame geometry consisting of intersecting pointed flame tongue quads
        /// and a luminous core. Guaranteed to render cleanly regardless of particle settings or mobile GPU shaders.
        /// </summary>
        private static GameObject Build3DFlameMesh(Transform parent, FireHazardType hazardType)
        {
            GameObject root = new GameObject("3D_FlameMesh");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            // Outer roaring flame tongues
            GameObject outerFlame = CreateFlameQuadGroup("OuterFlameTongues", 0.65f, 0.90f, s_MeshFlameOuterMat);
            outerFlame.transform.SetParent(root.transform, false);

            // Inner bright core
            GameObject innerCore = CreateFlameQuadGroup("InnerFlameCore", 0.42f, 0.60f, s_MeshFlameInnerMat);
            innerCore.transform.SetParent(root.transform, false);

            // Add real-time dynamic flame flickering animator
            root.AddComponent<ProceduralFlameMeshAnimator>();

            return root;
        }

        private static GameObject CreateFlameQuadGroup(string name, float width, float height, Material mat)
        {
            GameObject group = new GameObject(name);
            Mesh mesh = BuildFlameTongueMesh(width, height);

            // Create 3 intersecting cross planes (0 deg, 60 deg, 120 deg)
            float[] angles = { 0f, 60f, 120f };
            for (int i = 0; i < angles.Length; i++)
            {
                GameObject card = new GameObject($"Card_{i}");
                card.transform.SetParent(group.transform, false);
                card.transform.localRotation = Quaternion.Euler(0, angles[i], 0);

                var mf = card.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                var mr = card.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            return group;
        }

        private static Mesh BuildFlameTongueMesh(float width, float height)
        {
            Mesh m = new Mesh { name = "Procedural_FlameTongueMesh" };

            float halfW = width * 0.5f;
            float midH = height * 0.45f;
            float topH = height;

            // 5-point pointed flame silhouette:
            // 0: bottom-left, 1: bottom-right, 2: mid-left, 3: mid-right, 4: top tip
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfW * 0.6f, 0f, 0f),
                new Vector3( halfW * 0.6f, 0f, 0f),
                new Vector3(-halfW, midH, 0f),
                new Vector3( halfW, midH, 0f),
                new Vector3(0f, topH, 0f)
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0.2f, 0.0f),
                new Vector2(0.8f, 0.0f),
                new Vector2(0.0f, 0.5f),
                new Vector2(1.0f, 0.5f),
                new Vector2(0.5f, 1.0f)
            };

            // Double-sided triangles
            int[] triangles = new int[]
            {
                // Front
                0, 2, 1,
                1, 2, 3,
                2, 4, 3,
                // Back
                1, 2, 0,
                3, 2, 1,
                3, 4, 2
            };

            m.vertices = vertices;
            m.uv = uvs;
            m.triangles = triangles;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// Floating world-space incident beacon that alerts the trainee directly to the burning hazard.
        /// </summary>
        private static GameObject BuildHazardMarker(Transform parent, string targetName)
        {
            GameObject marker = new GameObject("HazardBeacon_Tag");
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0, 1.35f, 0);

            TextMesh tm = marker.AddComponent<TextMesh>();
            string cleanName = string.IsNullOrEmpty(targetName) ? "EQUIPMENT" : targetName.Replace("Machine_", "").Replace("Electrical_", "").Replace("(Clone)", "").ToUpper();
            tm.text = $"🔥 FIRE HAZARD!\n{cleanName}";
            tm.fontSize = 24;
            tm.characterSize = 0.038f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1.0f, 0.35f, 0.1f);
            tm.fontStyle = FontStyle.Bold;

            marker.AddComponent<HazardBeaconPulsator>();
            return marker;
        }

        /// <summary>
        /// Tall, slow-rising smoke plume used purely as a findability clue during the fire-origin
        /// identification stage — the flames/embers are already out at that point, but a machine
        /// that caught fire keeps visibly smoldering so the trainee can spot it from across the
        /// room instead of having to search every machine up close. Bigger, taller, and drifts
        /// higher than the in-fire smoke so it reads clearly over other equipment at a distance.
        /// </summary>
        public static ParticleSystem BuildIdentificationSmoke(Transform parent)
        {
            InitMaterials();

            GameObject smokeGo = new GameObject("IdentificationSmoke");
            smokeGo.transform.SetParent(parent, false);
            smokeGo.transform.localPosition = new Vector3(0, 0.4f, 0);

            ParticleSystem ps = smokeGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startColor = new Color(0.30f, 0.30f, 0.32f, 0.55f);

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.22f;

            var sizeOverLife = ps.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0.0f, 0.4f);
            sizeCurve.AddKey(1.0f, 2.6f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.30f, 0.30f, 0.32f), 0f), new GradientColorKey(new Color(0.30f, 0.30f, 0.32f), 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = grad;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.35f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = s_BlackSmokeMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 1;

            return ps;
        }
    }

    /// <summary>
    /// Animates procedural 3D flame mesh with dynamic scale flickering, swaying, and Perlin pulsation.
    /// </summary>
    public class ProceduralFlameMeshAnimator : MonoBehaviour
    {
        private float _baseScale = 1.0f;
        private float _seed;

        private void Awake()
        {
            _baseScale = Mathf.Max(0.1f, transform.localScale.x);
            _seed = Random.value * 100f;
        }

        public void SetBaseScale(float scale)
        {
            _baseScale = Mathf.Max(0.1f, scale);
            transform.localScale = Vector3.one * _baseScale;
        }

        private void Update()
        {
            float t = Time.time * 12f;
            float noiseY = Mathf.PerlinNoise(_seed, t * 0.85f);
            float noiseX = Mathf.PerlinNoise(_seed + 40f, t * 0.95f);

            float scaleY = Mathf.Lerp(0.85f, 1.30f, noiseY);
            float scaleXZ = Mathf.Lerp(0.92f, 1.12f, noiseX);

            transform.localScale = new Vector3(
                _baseScale * scaleXZ,
                _baseScale * scaleY,
                _baseScale * scaleXZ
            );

            // Gentle atmospheric flame tongue sway
            float sway = Mathf.Sin(Time.time * 6f + _seed) * 0.04f;
            transform.localRotation = Quaternion.Euler(sway * 30f, Mathf.Sin(Time.time * 2f) * 15f, -sway * 20f);
        }
    }

    /// <summary>
    /// Soft pulsating animation and camera billboarding for the in-world hazard badge.
    /// </summary>
    public class HazardBeaconPulsator : MonoBehaviour
    {
        private Transform _cam;
        private float _time;

        private void Start()
        {
            if (Camera.main != null) _cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;

            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
            }

            _time += Time.deltaTime * 4.5f;
            float pulse = 1.0f + Mathf.Sin(_time) * 0.08f;
            transform.localScale = Vector3.one * pulse;
        }
    }

    /// <summary>
    /// Procedural flame light flickering behavior.
    /// </summary>
    public class FireLightFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity = 3.5f;
        private float _seed;

        private void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null) _baseIntensity = _light.intensity;
            _seed = Random.value * 100f;
        }

        public void SetBaseIntensity(float intensity)
        {
            _baseIntensity = intensity;
        }

        private void Update()
        {
            if (_light == null) return;
            float noise = Mathf.PerlinNoise(_seed, Time.time * 9f);
            _light.intensity = _baseIntensity * (0.75f + noise * 0.50f);
        }
    }
}
