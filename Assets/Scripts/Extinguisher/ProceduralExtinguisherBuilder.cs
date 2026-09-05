using UnityEngine;

namespace ARMiningSimulator.Extinguisher
{
    /// <summary>
    /// Builds a first-person 3D fire extinguisher model from Unity primitives,
    /// complete with pressure gauge, discharge hose, nozzle, and procedural
    /// high-velocity chemical spray particle system.
    /// </summary>
    public static class ProceduralExtinguisherBuilder
    {
        private static Material s_MatRedSteel;
        private static Material s_MatBlackHandle;
        private static Material s_MatBrass;
        private static Material s_MatWhiteSpray;

        private static void InitMaterials()
        {
            if (s_MatRedSteel != null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return;

            s_MatRedSteel = new Material(shader) { color = new Color(0.85f, 0.12f, 0.12f) }; // Fire Engine Red
            s_MatBlackHandle = new Material(shader) { color = new Color(0.12f, 0.12f, 0.14f) };
            s_MatBrass = new Material(shader) { color = new Color(0.78f, 0.65f, 0.22f) };

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? shader;
            s_MatWhiteSpray = new Material(unlitShader)
            {
                color = new Color(0.95f, 0.96f, 0.98f, 0.65f)
            };
        }

        public static (GameObject root, ParticleSystem spray, Transform nozzleTip) BuildExtinguisher(Transform parent, ExtinguisherConfig config)
        {
            InitMaterials();

            GameObject root = new GameObject("Handheld_FireExtinguisher");
            root.transform.SetParent(parent, false);
            // Position in bottom-right first-person viewport
            root.transform.localPosition = new Vector3(0.24f, -0.22f, 0.52f);
            root.transform.localRotation = Quaternion.Euler(12f, -18f, 4f);

            // 1. Red Pressure Tank (Cylinder)
            GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Tank_Body";
            tank.transform.SetParent(root.transform, false);
            tank.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            tank.transform.localPosition = new Vector3(0f, 0f, 0f);
            tank.GetComponent<Renderer>().sharedMaterial = s_MatRedSteel;
            StripCollider(tank);

            // 2. Classification Color Band (Black, Blue, or Cream)
            GameObject colorBand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            colorBand.name = "Color_Band";
            colorBand.transform.SetParent(root.transform, false);
            colorBand.transform.localScale = new Vector3(0.122f, 0.05f, 0.122f);
            colorBand.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material bandMat = new Material(shader) { color = config.bandColor };
                colorBand.GetComponent<Renderer>().sharedMaterial = bandMat;
            }
            StripCollider(colorBand);

            // 3. Top Neck & Valve Assembly
            GameObject valve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            valve.name = "Valve_Assembly";
            valve.transform.SetParent(root.transform, false);
            valve.transform.localScale = new Vector3(0.045f, 0.04f, 0.045f);
            valve.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            valve.GetComponent<Renderer>().sharedMaterial = s_MatBrass;
            StripCollider(valve);

            // 4. Carry Handle & Trigger Lever
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "Carry_Handle";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localScale = new Vector3(0.02f, 0.08f, 0.10f);
            handle.transform.localPosition = new Vector3(0f, 0.26f, -0.04f);
            handle.transform.localRotation = Quaternion.Euler(20f, 0, 0);
            handle.GetComponent<Renderer>().sharedMaterial = s_MatBlackHandle;
            StripCollider(handle);

            // 5. Discharge Hose & Nozzle Horn
            GameObject nozzleTip = new GameObject("Nozzle_Tip");
            nozzleTip.transform.SetParent(root.transform, false);
            nozzleTip.transform.localPosition = new Vector3(0.03f, 0.22f, 0.18f);
            nozzleTip.transform.localRotation = Quaternion.Euler(-8f, 15f, 0);

            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            horn.name = "Nozzle_Horn";
            horn.transform.SetParent(nozzleTip.transform, false);
            horn.transform.localScale = new Vector3(0.035f, 0.06f, 0.035f);
            horn.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            horn.GetComponent<Renderer>().sharedMaterial = s_MatBlackHandle;
            StripCollider(horn);

            // 6. High-Velocity Chemical Spray Particle System
            GameObject sprayGo = new GameObject("Extinguisher_Spray_Particles");
            sprayGo.transform.SetParent(nozzleTip.transform, false);
            sprayGo.transform.localPosition = Vector3.forward * 0.08f;
            sprayGo.transform.localRotation = Quaternion.identity;

            ParticleSystem sprayPS = BuildSprayParticleSystem(sprayGo);

            return (root, sprayPS, nozzleTip.transform);
        }

        private static ParticleSystem BuildSprayParticleSystem(GameObject host)
        {
            ParticleSystem ps = host.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = 11.5f; // Fast pressurized spray
            main.startSize = 0.08f;
            main.startColor = new Color(0.95f, 0.96f, 0.98f, 0.70f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 120f; // Dense vapor/powder cloud
            emission.enabled = false;    // Starts silent

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.02f;

            // Expand as vapor travels outward
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.15f);
            curve.AddKey(0.4f, 0.75f);
            curve.AddKey(1f, 1.35f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // Billowing turbulence
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 1.2f;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (s_MatWhiteSpray != null)
            {
                renderer.sharedMaterial = s_MatWhiteSpray;
            }

            return ps;
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
    }
}
