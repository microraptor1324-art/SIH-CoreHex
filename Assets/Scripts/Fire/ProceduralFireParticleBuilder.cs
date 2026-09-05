using UnityEngine;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Programmatically generates lightweight, performant Unity Particle Systems
    /// for Fire and Smoke without relying on third-party or paid assets.
    /// </summary>
    public static class ProceduralFireParticleBuilder
    {
        private static Material s_FireMaterial;
        private static Material s_SmokeMaterial;

        private static void InitMaterials()
        {
            if (s_FireMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                             ?? Shader.Find("Particles/Standard Unlit") 
                             ?? Shader.Find("Standard");

                s_FireMaterial = new Material(shader) { name = "Procedural_FireMat" };
                s_FireMaterial.color = new Color(1.0f, 0.55f, 0.1f, 0.9f); // Flame orange

                s_SmokeMaterial = new Material(shader) { name = "Procedural_SmokeMat" };
                s_SmokeMaterial.color = new Color(0.18f, 0.18f, 0.2f, 0.65f); // Billowing dark smoke
            }
        }

        public static (ParticleSystem fire, ParticleSystem smoke, Light fireLight) BuildFireEffect(Transform parent)
        {
            InitMaterials();

            GameObject effectRoot = new GameObject("FireEffect_Root");
            effectRoot.transform.SetParent(parent, false);
            effectRoot.transform.localPosition = Vector3.zero;

            // 1. FIRE PARTICLE SYSTEM
            GameObject fireGo = new GameObject("FireParticles");
            fireGo.transform.SetParent(effectRoot.transform, false);

            ParticleSystem firePS = fireGo.AddComponent<ParticleSystem>();
            var fireMain = firePS.main;
            fireMain.duration = 1.0f;
            fireMain.loop = true;
            fireMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            fireMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            fireMain.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            fireMain.startColor = new Color(1.0f, 0.45f, 0.05f, 0.95f);
            fireMain.simulationSpace = ParticleSystemSimulationSpace.World;
            fireMain.playOnAwake = true;

            var fireEmission = firePS.emission;
            fireEmission.rateOverTime = 30f;

            var fireShape = firePS.shape;
            fireShape.shapeType = ParticleSystemShapeType.Cone;
            fireShape.angle = 12f;
            fireShape.radius = 0.18f;

            var fireSizeOverLife = firePS.sizeOverLifetime;
            fireSizeOverLife.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0.0f, 0.3f);
            sizeCurve.AddKey(0.3f, 1.0f);
            sizeCurve.AddKey(1.0f, 0.1f);
            fireSizeOverLife.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

            var fireColorOverLife = firePS.colorOverLifetime;
            fireColorOverLife.enabled = true;
            Gradient fireGrad = new Gradient();
            fireGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(new Color(1f, 0.2f, 0f), 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            fireColorOverLife.color = fireGrad;

            var fireRenderer = fireGo.GetComponent<ParticleSystemRenderer>();
            fireRenderer.sharedMaterial = s_FireMaterial;

            // 2. SMOKE PARTICLE SYSTEM
            GameObject smokeGo = new GameObject("SmokeParticles");
            smokeGo.transform.SetParent(effectRoot.transform, false);
            smokeGo.transform.localPosition = new Vector3(0, 0.25f, 0);

            ParticleSystem smokePS = smokeGo.AddComponent<ParticleSystem>();
            var smokeMain = smokePS.main;
            smokeMain.duration = 1.0f;
            smokeMain.loop = true;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
            smokeMain.startColor = new Color(0.2f, 0.2f, 0.22f, 0.6f);
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
            smokeMain.playOnAwake = true;

            var smokeEmission = smokePS.emission;
            smokeEmission.rateOverTime = 20f;

            var smokeShape = smokePS.shape;
            smokeShape.shapeType = ParticleSystemShapeType.Cone;
            smokeShape.angle = 20f;
            smokeShape.radius = 0.2f;

            var smokeSizeOverLife = smokePS.sizeOverLifetime;
            smokeSizeOverLife.enabled = true;
            AnimationCurve smokeCurve = new AnimationCurve();
            smokeCurve.AddKey(0.0f, 0.4f);
            smokeCurve.AddKey(1.0f, 2.2f); // Expands as it billows upward
            smokeSizeOverLife.size = new ParticleSystem.MinMaxCurve(1.0f, smokeCurve);

            var smokeColorOverLife = smokePS.colorOverLifetime;
            smokeColorOverLife.enabled = true;
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.gray, 0.0f), new GradientColorKey(Color.black, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            smokeColorOverLife.color = smokeGrad;

            var smokeRenderer = smokeGo.GetComponent<ParticleSystemRenderer>();
            smokeRenderer.sharedMaterial = s_SmokeMaterial;

            // 3. FLICKERING FLAME POINT LIGHT
            GameObject lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(effectRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.3f, 0);

            Light fireLight = lightGo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1.0f, 0.55f, 0.15f);
            fireLight.intensity = 2.5f;
            fireLight.range = 3.5f;

            lightGo.AddComponent<FireLightFlicker>();

            return (firePS, smokePS, fireLight);
        }
    }

    /// <summary>
    /// Procedural flame light flickering behavior.
    /// </summary>
    public class FireLightFlicker : MonoBehaviour
    {
        private Light _light;
        private float _baseIntensity = 2.5f;
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
            float noise = Mathf.PerlinNoise(_seed, Time.time * 8f);
            _light.intensity = _baseIntensity * (0.75f + noise * 0.5f);
        }
    }
}
