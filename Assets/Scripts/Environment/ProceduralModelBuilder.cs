using UnityEngine;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Constructs stylized 3D procedural composite industrial models from Unity primitives.
    /// Follows Prototype Asset Rules: uses no paid assets, uses primitives with curated industrial palettes.
    /// </summary>
    public static class ProceduralModelBuilder
    {
        private static Shader s_Shader;
        private static Material s_MatYellow;
        private static Material s_MatOrange;
        private static Material s_MatDarkIron;
        private static Material s_MatSteel;
        private static Material s_MatTeal;
        private static Material s_MatRed;
        private static Material s_MatExitGreen;
        private static Material s_MatRock;

        private static void InitMaterials()
        {
            if (s_Shader == null)
                s_Shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            if (s_MatYellow == null)
            {
                s_MatYellow = CreateMat("Mat_SafetyYellow", new Color(0.98f, 0.76f, 0.05f));
                s_MatOrange = CreateMat("Mat_SafetyOrange", new Color(0.98f, 0.42f, 0.02f));
                s_MatDarkIron = CreateMat("Mat_DarkIron", new Color(0.18f, 0.20f, 0.23f));
                s_MatSteel = CreateMat("Mat_Steel", new Color(0.52f, 0.56f, 0.62f));
                s_MatTeal = CreateMat("Mat_ControlTeal", new Color(0.16f, 0.46f, 0.42f));
                s_MatRed = CreateMat("Mat_WarningRed", new Color(0.88f, 0.18f, 0.22f));
                s_MatExitGreen = CreateMat("Mat_ExitGreen", new Color(0.10f, 0.82f, 0.45f));
                s_MatRock = CreateMat("Mat_GroundRock", new Color(0.12f, 0.13f, 0.15f));
            }
        }

        private static Material CreateMat(string name, Color color)
        {
            Material m = new Material(s_Shader) { name = name, color = color };
            return m;
        }

        #region Mining Machines Builders

        public static GameObject BuildMiningDrill(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("MiningDrill");
            root.transform.SetParent(parent, false);

            // Base chassis
            AddBox(root, new Vector3(0.9f, 0.25f, 1.2f), new Vector3(0, 0.125f, 0), s_MatYellow);
            // Track treads
            AddBox(root, new Vector3(0.2f, 0.22f, 1.3f), new Vector3(-0.45f, 0.11f, 0), s_MatDarkIron);
            AddBox(root, new Vector3(0.2f, 0.22f, 1.3f), new Vector3(0.45f, 0.11f, 0), s_MatDarkIron);
            // Vertical derrick mast
            AddBox(root, new Vector3(0.25f, 1.4f, 0.25f), new Vector3(0, 0.95f, 0.25f), s_MatDarkIron);
            // Drill head motor
            AddBox(root, new Vector3(0.4f, 0.35f, 0.4f), new Vector3(0, 1.15f, 0.25f), s_MatYellow);
            // Drill bit cylinder
            AddCylinder(root, new Vector3(0.12f, 0.55f, 0.12f), new Vector3(0, 0.45f, 0.25f), s_MatSteel);

            AddLabelTag(root, "MINING DRILL", 1.8f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildConveyor(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("Conveyor");
            root.transform.SetParent(parent, false);

            // Angled conveyor bed
            GameObject bed = AddBox(root, new Vector3(0.55f, 0.1f, 1.6f), new Vector3(0, 0.45f, 0), s_MatDarkIron);
            bed.transform.localRotation = Quaternion.Euler(12f, 0, 0);

            // Belt surface
            GameObject belt = AddBox(root, new Vector3(0.48f, 0.04f, 1.55f), new Vector3(0, 0.49f, 0), s_MatSteel);
            belt.transform.localRotation = Quaternion.Euler(12f, 0, 0);

            // End rollers
            AddCylinder(root, new Vector3(0.12f, 0.28f, 0.12f), new Vector3(0, 0.32f, -0.75f), s_MatOrange, 90f);
            AddCylinder(root, new Vector3(0.12f, 0.28f, 0.12f), new Vector3(0, 0.62f, 0.75f), s_MatOrange, 90f);

            // Support legs
            AddCylinder(root, new Vector3(0.06f, 0.18f, 0.06f), new Vector3(-0.25f, 0.18f, -0.4f), s_MatDarkIron);
            AddCylinder(root, new Vector3(0.06f, 0.18f, 0.06f), new Vector3(0.25f, 0.18f, -0.4f), s_MatDarkIron);
            AddCylinder(root, new Vector3(0.06f, 0.35f, 0.06f), new Vector3(-0.25f, 0.35f, 0.4f), s_MatDarkIron);
            AddCylinder(root, new Vector3(0.06f, 0.35f, 0.06f), new Vector3(0.25f, 0.35f, 0.4f), s_MatDarkIron);

            AddLabelTag(root, "CONVEYOR", 1.0f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildRockCrusher(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("RockCrusher");
            root.transform.SetParent(parent, false);

            // Heavy base frame
            AddBox(root, new Vector3(1.1f, 0.3f, 1.1f), new Vector3(0, 0.15f, 0), s_MatDarkIron);
            // Main crushing chamber
            AddBox(root, new Vector3(0.85f, 0.65f, 0.85f), new Vector3(0, 0.625f, 0), s_MatDarkIron);
            // Intake hopper (funnel)
            AddBox(root, new Vector3(0.95f, 0.3f, 0.95f), new Vector3(0, 1.05f, 0), s_MatYellow);
            // Side flywheel
            AddCylinder(root, new Vector3(0.6f, 0.08f, 0.6f), new Vector3(0.48f, 0.65f, 0), s_MatOrange, 0, 90f);
            // Output chute
            AddBox(root, new Vector3(0.4f, 0.15f, 0.5f), new Vector3(0, 0.2f, -0.55f), s_MatSteel);

            AddLabelTag(root, "ROCK CRUSHER", 1.4f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildExcavator(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("Excavator");
            root.transform.SetParent(parent, false);

            // Tracks
            AddBox(root, new Vector3(0.22f, 0.25f, 1.1f), new Vector3(-0.4f, 0.125f, 0), s_MatDarkIron);
            AddBox(root, new Vector3(0.22f, 0.25f, 1.1f), new Vector3(0.4f, 0.125f, 0), s_MatDarkIron);
            // Chassis center
            AddBox(root, new Vector3(0.65f, 0.15f, 0.9f), new Vector3(0, 0.18f, 0), s_MatDarkIron);
            // Operator cab
            AddBox(root, new Vector3(0.7f, 0.55f, 0.75f), new Vector3(0, 0.53f, -0.1f), s_MatYellow);
            // Boom arm base
            AddBox(root, new Vector3(0.18f, 0.65f, 0.18f), new Vector3(0, 0.85f, 0.35f), s_MatDarkIron);
            // Bucket scoop
            AddBox(root, new Vector3(0.45f, 0.3f, 0.35f), new Vector3(0, 0.35f, 0.8f), s_MatSteel);

            AddLabelTag(root, "EXCAVATOR", 1.3f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildGenerator(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("Generator");
            root.transform.SetParent(parent, false);

            // Generator acoustic cabinet
            AddBox(root, new Vector3(1.1f, 0.85f, 0.75f), new Vector3(0, 0.425f, 0), s_MatYellow);
            // Base skid
            AddBox(root, new Vector3(1.2f, 0.1f, 0.85f), new Vector3(0, 0.05f, 0), s_MatDarkIron);
            // Exhaust stack
            AddCylinder(root, new Vector3(0.12f, 0.35f, 0.12f), new Vector3(-0.35f, 1.05f, 0.2f), s_MatSteel);
            // Ventilation louver grill
            AddBox(root, new Vector3(0.7f, 0.45f, 0.04f), new Vector3(0, 0.5f, 0.39f), s_MatDarkIron);

            AddLabelTag(root, "GENERATOR", 1.35f);
            StripColliders(root);
            return root;
        }

        #endregion

        #region Electrical Equipment Builders

        public static GameObject BuildElectricalPanel(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("ElectricalPanel");
            root.transform.SetParent(parent, false);

            // Upright cabinet
            AddBox(root, new Vector3(0.55f, 0.95f, 0.25f), new Vector3(0, 0.475f, 0), s_MatOrange);
            // Pedestal stand
            AddBox(root, new Vector3(0.45f, 0.15f, 0.35f), new Vector3(0, 0.075f, 0), s_MatDarkIron);
            // High voltage indicator plate
            AddBox(root, new Vector3(0.25f, 0.2f, 0.03f), new Vector3(0, 0.65f, 0.14f), s_MatYellow);
            // Indicator lights
            AddCylinder(root, new Vector3(0.04f, 0.03f, 0.04f), new Vector3(-0.12f, 0.82f, 0.14f), s_MatRed, 90f);
            AddCylinder(root, new Vector3(0.04f, 0.03f, 0.04f), new Vector3(0.12f, 0.82f, 0.14f), s_MatExitGreen, 90f);

            AddLabelTag(root, "ELEC PANEL", 1.15f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildControlBox(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("ControlBox");
            root.transform.SetParent(parent, false);

            // Stand
            AddCylinder(root, new Vector3(0.06f, 0.5f, 0.06f), new Vector3(0, 0.25f, 0), s_MatDarkIron);
            AddBox(root, new Vector3(0.35f, 0.04f, 0.35f), new Vector3(0, 0.02f, 0), s_MatDarkIron);
            // Control console
            AddBox(root, new Vector3(0.4f, 0.3f, 0.28f), new Vector3(0, 0.62f, 0), s_MatTeal);
            // Buttons
            AddCylinder(root, new Vector3(0.05f, 0.02f, 0.05f), new Vector3(-0.08f, 0.78f, 0), s_MatExitGreen);
            AddCylinder(root, new Vector3(0.05f, 0.02f, 0.05f), new Vector3(0.08f, 0.78f, 0), s_MatRed);

            AddLabelTag(root, "CONTROL BOX", 0.95f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildPowerUnit(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("PowerUnit");
            root.transform.SetParent(parent, false);

            // Heavy transformer tank
            AddBox(root, new Vector3(0.6f, 0.55f, 0.55f), new Vector3(0, 0.275f, 0), s_MatTeal);
            // Side heat sink ribs
            AddBox(root, new Vector3(0.66f, 0.45f, 0.08f), new Vector3(0, 0.275f, 0.28f), s_MatDarkIron);
            AddBox(root, new Vector3(0.66f, 0.45f, 0.08f), new Vector3(0, 0.275f, -0.28f), s_MatDarkIron);
            // Top insulators
            AddCylinder(root, new Vector3(0.08f, 0.16f, 0.08f), new Vector3(-0.16f, 0.63f, 0), s_MatSteel);
            AddCylinder(root, new Vector3(0.08f, 0.16f, 0.08f), new Vector3(0.16f, 0.63f, 0), s_MatSteel);

            AddLabelTag(root, "POWER UNIT", 0.9f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildCableBox(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("CableBox");
            root.transform.SetParent(parent, false);

            // Low heavy junction box
            AddBox(root, new Vector3(0.5f, 0.22f, 0.4f), new Vector3(0, 0.11f, 0), s_MatDarkIron);
            // Conduit pipes
            AddCylinder(root, new Vector3(0.06f, 0.35f, 0.06f), new Vector3(-0.15f, 0.11f, 0.25f), s_MatSteel, 90f);
            AddCylinder(root, new Vector3(0.06f, 0.35f, 0.06f), new Vector3(0.15f, 0.11f, 0.25f), s_MatSteel, 90f);
            // Warning marker
            AddBox(root, new Vector3(0.2f, 0.02f, 0.15f), new Vector3(0, 0.23f, 0), s_MatOrange);

            AddLabelTag(root, "CABLE BOX", 0.55f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildTransformer(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("Transformer");
            root.transform.SetParent(parent, false);

            // Main tank
            AddBox(root, new Vector3(0.65f, 0.7f, 0.5f), new Vector3(0, 0.35f, 0), s_MatSteel);
            // 3 porcelain insulator bushings on top
            AddCylinder(root, new Vector3(0.07f, 0.22f, 0.07f), new Vector3(-0.2f, 0.81f, 0), s_MatOrange);
            AddCylinder(root, new Vector3(0.07f, 0.22f, 0.07f), new Vector3(0f, 0.81f, 0), s_MatOrange);
            AddCylinder(root, new Vector3(0.07f, 0.22f, 0.07f), new Vector3(0.2f, 0.81f, 0), s_MatOrange);

            AddLabelTag(root, "TRANSFORMER", 1.1f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildElectricalMotor(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("ElectricalMotor");
            root.transform.SetParent(parent, false);

            // Mounting base
            AddBox(root, new Vector3(0.45f, 0.08f, 0.55f), new Vector3(0, 0.04f, 0), s_MatDarkIron);
            // Motor cylinder housing
            AddCylinder(root, new Vector3(0.32f, 0.38f, 0.32f), new Vector3(0, 0.22f, 0), s_MatTeal, 90f);
            // Drive shaft
            AddCylinder(root, new Vector3(0.08f, 0.22f, 0.08f), new Vector3(0, 0.22f, 0.42f), s_MatSteel, 90f);
            // Terminal box on top
            AddBox(root, new Vector3(0.16f, 0.12f, 0.18f), new Vector3(0, 0.42f, 0), s_MatDarkIron);

            AddLabelTag(root, "ELEC MOTOR", 0.75f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildSwitchboard(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("Switchboard");
            root.transform.SetParent(parent, false);

            // Wide cabinet
            AddBox(root, new Vector3(0.85f, 0.9f, 0.25f), new Vector3(0, 0.45f, 0), s_MatTeal);
            // Breaker bank rows
            AddBox(root, new Vector3(0.7f, 0.12f, 0.04f), new Vector3(0, 0.65f, 0.13f), s_MatDarkIron);
            AddBox(root, new Vector3(0.7f, 0.12f, 0.04f), new Vector3(0, 0.45f, 0.13f), s_MatDarkIron);
            AddBox(root, new Vector3(0.7f, 0.12f, 0.04f), new Vector3(0, 0.25f, 0.13f), s_MatDarkIron);
            // Warning bar
            AddBox(root, new Vector3(0.8f, 0.06f, 0.03f), new Vector3(0, 0.82f, 0.13f), s_MatYellow);

            AddLabelTag(root, "SWITCHBOARD", 1.15f);
            StripColliders(root);
            return root;
        }

        #endregion

        #region Emergency Exit & Safe Zone

        public static GameObject BuildEmergencyExit(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("EmergencyExit");
            root.transform.SetParent(parent, false);

            // Frame posts
            AddBox(root, new Vector3(0.12f, 1.8f, 0.12f), new Vector3(-0.45f, 0.9f, 0), s_MatExitGreen);
            AddBox(root, new Vector3(0.12f, 1.8f, 0.12f), new Vector3(0.45f, 0.9f, 0), s_MatExitGreen);
            // Top lintel
            AddBox(root, new Vector3(1.02f, 0.15f, 0.12f), new Vector3(0, 1.8f, 0), s_MatExitGreen);
            // Overhead luminous Exit Sign
            GameObject sign = AddBox(root, new Vector3(0.75f, 0.28f, 0.05f), new Vector3(0, 2.05f, 0), s_MatExitGreen);
            AddLabelTag(sign, "EMERGENCY EXIT 🚪", 0.05f, Color.white, 28);

            // Ground threshold bar
            AddBox(root, new Vector3(1.0f, 0.02f, 0.2f), new Vector3(0, 0.01f, 0), s_MatExitGreen);

            StripColliders(root);
            return root;
        }

        public static GameObject BuildSafeZone(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("SafeZone");
            root.transform.SetParent(parent, false);

            // Ground safety circle / pad
            GameObject pad = AddCylinder(root, new Vector3(1.4f, 0.02f, 1.4f), new Vector3(0, 0.01f, 0), s_MatExitGreen);

            // Beacon marker in center
            AddCylinder(root, new Vector3(0.08f, 0.4f, 0.08f), new Vector3(0, 0.2f, 0), s_MatExitGreen);
            GameObject beaconHead = AddCylinder(root, new Vector3(0.18f, 0.08f, 0.18f), new Vector3(0, 0.45f, 0), s_MatExitGreen);
            AddLabelTag(beaconHead, "SAFE ZONE 🟢", 0.45f, new Color(0.2f, 1f, 0.4f), 26);

            StripColliders(root);
            return root;
        }

        #endregion

        #region Helper Primitives & Utilities

        private static GameObject AddBox(GameObject parent, Vector3 size, Vector3 pos, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent.transform, false);
            go.transform.localScale = size;
            go.transform.localPosition = pos;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject AddCylinder(GameObject parent, Vector3 size, Vector3 pos, Material mat, float rotX = 0f, float rotZ = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent.transform, false);
            go.transform.localScale = size;
            go.transform.localPosition = pos;
            if (rotX != 0f || rotZ != 0f)
                go.transform.localRotation = Quaternion.Euler(rotX, 0f, rotZ);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static void AddLabelTag(GameObject parent, string text, float heightOffset, Color? textColor = null, int fontSize = 20)
        {
            GameObject labelObj = new GameObject("TagText");
            labelObj.transform.SetParent(parent.transform, false);
            labelObj.transform.localPosition = new Vector3(0, heightOffset, 0);

            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = 0.035f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = textColor ?? Color.white;
            tm.fontStyle = FontStyle.Bold;

            // Make it billboard towards main camera
            labelObj.AddComponent<BillboardLookAt>();
        }

        private static void StripColliders(GameObject root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);
        }

        #endregion
    }

    /// <summary>
    /// Helper component to keep in-world 3D labels oriented towards the AR camera.
    /// </summary>
    public class BillboardLookAt : MonoBehaviour
    {
        private Transform _camTransform;

        private void Start()
        {
            if (Camera.main != null)
                _camTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_camTransform == null && Camera.main != null)
                _camTransform = Camera.main.transform;

            if (_camTransform != null)
            {
                transform.LookAt(transform.position + _camTransform.rotation * Vector3.forward,
                                 _camTransform.rotation * Vector3.up);
            }
        }
    }
}
