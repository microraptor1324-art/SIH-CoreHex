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
        private static Material s_MatCoalRock;
        private static Material s_MatMineArchSteel;
        private static Material s_MatVentDuct;
        private static Material s_MatCableBlack;
        private static Material s_MatSignWhite;

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
                s_MatCoalRock = CreateMat("Mat_CoalRock", new Color(0.11f, 0.12f, 0.13f));
                s_MatMineArchSteel = CreateMat("Mat_MineArchSteel", new Color(0.36f, 0.39f, 0.43f));
                s_MatVentDuct = CreateMat("Mat_VentDuct", new Color(0.92f, 0.85f, 0.15f));
                s_MatCableBlack = CreateMat("Mat_CableBlack", new Color(0.08f, 0.08f, 0.09f));
                s_MatSignWhite = CreateMat("Mat_SignWhite", new Color(0.92f, 0.92f, 0.94f));
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

        public static GameObject BuildContinuousMiner(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("ContinuousMiner");
            root.transform.SetParent(parent, false);

            // Heavy low-profile crawler chassis
            AddBox(root, new Vector3(0.95f, 0.32f, 1.45f), new Vector3(0, 0.22f, 0), s_MatDarkIron);
            // Heavy side track treads
            AddBox(root, new Vector3(0.22f, 0.28f, 1.55f), new Vector3(-0.52f, 0.14f, 0), s_MatDarkIron);
            AddBox(root, new Vector3(0.22f, 0.28f, 1.55f), new Vector3(0.52f, 0.14f, 0), s_MatDarkIron);
            // Protective operator canopy / body
            AddBox(root, new Vector3(0.75f, 0.42f, 0.65f), new Vector3(0, 0.58f, -0.25f), s_MatYellow);
            // Dual boom cutter pivot arms extending forward
            AddBox(root, new Vector3(0.12f, 0.16f, 0.55f), new Vector3(-0.35f, 0.42f, 0.65f), s_MatDarkIron);
            AddBox(root, new Vector3(0.12f, 0.16f, 0.55f), new Vector3(0.35f, 0.42f, 0.65f), s_MatDarkIron);
            // Front rotating cutter drum (horizontal cylinder)
            AddCylinder(root, new Vector3(0.38f, 0.55f, 0.38f), new Vector3(0, 0.45f, 0.95f), s_MatOrange, 0f, 90f);
            // Front gathering pan / scoop plate underneath cutter
            AddBox(root, new Vector3(0.95f, 0.08f, 0.45f), new Vector3(0, 0.10f, 0.72f), s_MatSteel);
            // Rear discharge conveyor boom
            AddBox(root, new Vector3(0.38f, 0.12f, 0.85f), new Vector3(0, 0.42f, -0.92f), s_MatSteel);

            AddLabelTag(root, "CONTINUOUS MINER", 1.35f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildScooptram(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("Scooptram");
            root.transform.SetParent(parent, false);

            // Low-profile articulated mining chassis
            AddBox(root, new Vector3(0.80f, 0.35f, 1.55f), new Vector3(0, 0.32f, 0), s_MatYellow);
            // 4 large mining wheel tires
            AddCylinder(root, new Vector3(0.42f, 0.18f, 0.42f), new Vector3(-0.46f, 0.22f, 0.48f), s_MatDarkIron, 0f, 90f);
            AddCylinder(root, new Vector3(0.42f, 0.18f, 0.42f), new Vector3(0.46f, 0.22f, 0.48f), s_MatDarkIron, 0f, 90f);
            AddCylinder(root, new Vector3(0.42f, 0.18f, 0.42f), new Vector3(-0.46f, 0.22f, -0.48f), s_MatDarkIron, 0f, 90f);
            AddCylinder(root, new Vector3(0.42f, 0.18f, 0.42f), new Vector3(0.46f, 0.22f, -0.48f), s_MatDarkIron, 0f, 90f);
            // Side operator cab & canopy
            AddBox(root, new Vector3(0.35f, 0.45f, 0.55f), new Vector3(-0.22f, 0.65f, 0.05f), s_MatDarkIron);
            // Rear engine compartment
            AddBox(root, new Vector3(0.72f, 0.32f, 0.55f), new Vector3(0, 0.42f, -0.45f), s_MatYellow);
            // Front heavy loader bucket / scoop
            AddBox(root, new Vector3(1.05f, 0.42f, 0.48f), new Vector3(0, 0.28f, 1.05f), s_MatSteel);
            // Hydraulic lift arms
            AddBox(root, new Vector3(0.1f, 0.12f, 0.55f), new Vector3(-0.35f, 0.42f, 0.62f), s_MatOrange);
            AddBox(root, new Vector3(0.1f, 0.12f, 0.55f), new Vector3(0.35f, 0.42f, 0.62f), s_MatOrange);

            AddLabelTag(root, "LHD SCOOPTRAM", 1.25f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildRoofBolter(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("RoofBolter");
            root.transform.SetParent(parent, false);

            // Base chassis
            AddBox(root, new Vector3(0.88f, 0.28f, 1.35f), new Vector3(0, 0.18f, 0), s_MatDarkIron);
            // Crawler tracks
            AddBox(root, new Vector3(0.2f, 0.24f, 1.4f), new Vector3(-0.48f, 0.12f, 0), s_MatDarkIron);
            AddBox(root, new Vector3(0.2f, 0.24f, 1.4f), new Vector3(0.48f, 0.12f, 0), s_MatDarkIron);
            // Operator safety canopy
            AddBox(root, new Vector3(0.68f, 0.52f, 0.55f), new Vector3(0, 0.52f, -0.28f), s_MatYellow);
            // Dual vertical bolting feed masts
            AddBox(root, new Vector3(0.14f, 1.25f, 0.14f), new Vector3(-0.24f, 0.90f, 0.45f), s_MatSteel);
            AddBox(root, new Vector3(0.14f, 1.25f, 0.14f), new Vector3(0.24f, 0.90f, 0.45f), s_MatSteel);
            // Bolter turret drill heads on top
            AddBox(root, new Vector3(0.22f, 0.25f, 0.22f), new Vector3(-0.24f, 1.55f, 0.45f), s_MatOrange);
            AddBox(root, new Vector3(0.22f, 0.25f, 0.22f), new Vector3(0.24f, 1.55f, 0.45f), s_MatOrange);
            // Bolt carrier tray
            AddBox(root, new Vector3(0.35f, 0.25f, 0.12f), new Vector3(0, 0.45f, 0.20f), s_MatSteel);

            AddLabelTag(root, "ROOF BOLTER", 1.85f);
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

        public static GameObject BuildCableReel(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("CableReel");
            root.transform.SetParent(parent, false);

            // Base skid frame
            AddBox(root, new Vector3(0.85f, 0.08f, 0.85f), new Vector3(0, 0.04f, 0), s_MatDarkIron);
            // Dual A-frame reel support uprights
            AddBox(root, new Vector3(0.08f, 0.65f, 0.12f), new Vector3(-0.32f, 0.38f, 0), s_MatTeal);
            AddBox(root, new Vector3(0.08f, 0.65f, 0.12f), new Vector3(0.32f, 0.38f, 0), s_MatTeal);
            // Center axle shaft
            AddCylinder(root, new Vector3(0.08f, 0.38f, 0.08f), new Vector3(0, 0.42f, 0), s_MatSteel, 0f, 90f);
            // Outer reel spool flanges
            AddCylinder(root, new Vector3(0.65f, 0.03f, 0.65f), new Vector3(-0.24f, 0.42f, 0), s_MatOrange, 0f, 90f);
            AddCylinder(root, new Vector3(0.65f, 0.03f, 0.65f), new Vector3(0.24f, 0.42f, 0), s_MatOrange, 0f, 90f);
            // Wound high-voltage cable core
            AddCylinder(root, new Vector3(0.52f, 0.22f, 0.52f), new Vector3(0, 0.42f, 0), s_MatDarkIron, 0f, 90f);
            // Spooling drive motor
            AddBox(root, new Vector3(0.22f, 0.24f, 0.22f), new Vector3(0.38f, 0.40f, 0), s_MatTeal);
            // Fairlead roller guide
            AddBox(root, new Vector3(0.28f, 0.08f, 0.08f), new Vector3(0, 0.20f, 0.38f), s_MatSteel);

            AddLabelTag(root, "CABLE REEL", 0.95f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildPortableElectricDrill(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("PortableElectricDrill");
            root.transform.SetParent(parent, false);

            // Portable mounting stand / leg
            AddBox(root, new Vector3(0.42f, 0.04f, 0.42f), new Vector3(0, 0.02f, 0), s_MatDarkIron);
            AddCylinder(root, new Vector3(0.06f, 0.32f, 0.06f), new Vector3(0, 0.32f, 0), s_MatSteel);
            // Drill motor body
            AddBox(root, new Vector3(0.28f, 0.35f, 0.28f), new Vector3(0, 0.72f, 0), s_MatTeal);
            // Dual handles
            AddCylinder(root, new Vector3(0.04f, 0.26f, 0.04f), new Vector3(0, 0.75f, 0), s_MatDarkIron, 0f, 90f);
            // Chuck & spiral drill steel
            AddCylinder(root, new Vector3(0.05f, 0.28f, 0.05f), new Vector3(0, 1.15f, 0), s_MatSteel);
            // Power cable lead with plug
            AddBox(root, new Vector3(0.06f, 0.06f, 0.18f), new Vector3(0, 0.58f, -0.18f), s_MatOrange);

            AddLabelTag(root, "PORTABLE DRILL", 1.45f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildBatteryChargingStation(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("BatteryChargingStation");
            root.transform.SetParent(parent, false);

            // Charger main cabinet
            AddBox(root, new Vector3(0.72f, 1.05f, 0.38f), new Vector3(0, 0.55f, 0), s_MatTeal);
            // Base skid
            AddBox(root, new Vector3(0.82f, 0.10f, 0.48f), new Vector3(0, 0.05f, 0), s_MatDarkIron);
            // Exhaust louvers / cooling hood
            AddBox(root, new Vector3(0.65f, 0.12f, 0.35f), new Vector3(0, 1.10f, 0), s_MatDarkIron);
            // Digital display monitor
            AddBox(root, new Vector3(0.32f, 0.20f, 0.03f), new Vector3(0, 0.85f, 0.20f), s_MatDarkIron);
            // LED status indicators
            AddCylinder(root, new Vector3(0.04f, 0.02f, 0.04f), new Vector3(-0.08f, 0.98f, 0.20f), s_MatExitGreen, 90f);
            AddCylinder(root, new Vector3(0.04f, 0.02f, 0.04f), new Vector3(0.08f, 0.98f, 0.20f), s_MatRed, 90f);
            // Heavy charging cable holster & connector
            AddCylinder(root, new Vector3(0.08f, 0.18f, 0.08f), new Vector3(0.40f, 0.55f, 0.08f), s_MatOrange);
            // Battery hazard warning sign
            AddBox(root, new Vector3(0.28f, 0.18f, 0.02f), new Vector3(0, 0.45f, 0.20f), s_MatYellow);

            AddLabelTag(root, "BATTERY CHARGER", 1.25f);
            StripColliders(root);
            return root;
        }

        public static GameObject BuildVentilationFan(Transform parent)
        {
            InitMaterials();
            GameObject root = new GameObject("VentilationFan");
            root.transform.SetParent(parent, false);

            // Mounting skid
            AddBox(root, new Vector3(0.68f, 0.08f, 0.78f), new Vector3(0, 0.04f, 0), s_MatDarkIron);
            // Cylindrical duct housing
            AddCylinder(root, new Vector3(0.62f, 0.34f, 0.62f), new Vector3(0, 0.45f, 0), s_MatYellow, 90f);
            // Flange rings front and back
            AddCylinder(root, new Vector3(0.68f, 0.04f, 0.68f), new Vector3(0, 0.45f, -0.32f), s_MatOrange, 90f);
            AddCylinder(root, new Vector3(0.68f, 0.04f, 0.68f), new Vector3(0, 0.45f, 0.32f), s_MatOrange, 90f);
            // Center motor bullet & impeller hub
            AddCylinder(root, new Vector3(0.22f, 0.28f, 0.22f), new Vector3(0, 0.45f, 0), s_MatDarkIron, 90f);
            // Front protective safety grill
            AddBox(root, new Vector3(0.52f, 0.52f, 0.02f), new Vector3(0, 0.45f, -0.34f), s_MatSteel);
            // Electric motor on top
            AddBox(root, new Vector3(0.24f, 0.25f, 0.24f), new Vector3(0, 0.82f, 0), s_MatTeal);

            AddLabelTag(root, "VENTILATION FAN", 1.10f);
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

        public static GameObject BuildExtinguisherStation(Transform parent, Vector3 pos, Quaternion rot)
        {
            InitMaterials();
            GameObject root = new GameObject("FireExtinguisherStation");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = rot;

            // 1. Station Backboard / Mounting Post (Yellow-black industrial hazard post)
            AddBox(root, new Vector3(0.35f, 0.95f, 0.10f), new Vector3(0, 0.475f, 0), s_MatYellow);
            AddBox(root, new Vector3(0.42f, 0.04f, 0.35f), new Vector3(0, 0.02f, 0.08f), s_MatDarkIron);

            // Overhead Red Station Sign
            GameObject signBox = AddBox(root, new Vector3(0.45f, 0.16f, 0.08f), new Vector3(0, 0.95f, 0.02f), s_MatRed);
            AddLabelTag(signBox, "🧯 EXTINGUISHER", 0.04f, Color.white, 22);

            // 2. Extinguisher Canister Model Visuals (Parented to separate sub-object so it can be hidden when taken)
            GameObject canisterVisual = new GameObject("Canister_Visual");
            canisterVisual.transform.SetParent(root.transform, false);
            canisterVisual.transform.localPosition = new Vector3(0, 0.42f, 0.12f);

            // Red steel tank
            AddCylinder(canisterVisual, new Vector3(0.16f, 0.32f, 0.16f), Vector3.zero, s_MatRed);
            // Black color band
            AddCylinder(canisterVisual, new Vector3(0.165f, 0.06f, 0.165f), new Vector3(0, 0.08f, 0), s_MatCableBlack);
            // Top valve
            AddCylinder(canisterVisual, new Vector3(0.06f, 0.05f, 0.06f), new Vector3(0, 0.20f, 0), s_MatSteel);
            // Carry handle & lever
            AddBox(canisterVisual, new Vector3(0.03f, 0.09f, 0.12f), new Vector3(0, 0.23f, -0.04f), s_MatDarkIron);
            // Discharge hose & horn
            AddCylinder(canisterVisual, new Vector3(0.04f, 0.12f, 0.04f), new Vector3(0.09f, 0.10f, 0.06f), s_MatDarkIron);

            // Mounting bracket ring on post
            AddCylinder(root, new Vector3(0.18f, 0.03f, 0.18f), new Vector3(0, 0.42f, 0.12f), s_MatDarkIron);

            // Strip default primitive colliders
            StripColliders(root);

            // 3. Floating 3D Text Badge
            GameObject badgeObj = new GameObject("Extinguisher_Badge");
            badgeObj.transform.SetParent(root.transform, false);
            badgeObj.transform.localPosition = new Vector3(0, 1.25f, 0.05f);
            TextMesh tm = badgeObj.AddComponent<TextMesh>();
            tm.text = "🧯 FIRE EXTINGUISHER\n[ TAP OR APPROACH TO TAKE ]";
            tm.characterSize = 0.035f;
            tm.fontSize = 28;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1.0f, 0.90f, 0.25f);
            tm.fontStyle = FontStyle.Bold;
            badgeObj.AddComponent<BillboardLookAt>();

            // 4. Interaction Trigger Collider
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.2f, 1.6f, 1.2f);
            col.center = new Vector3(0, 0.8f, 0);

            // 5. WorldFireExtinguisher component
            var stationComp = root.AddComponent<WorldFireExtinguisher>();
            stationComp.SetupVisuals(canisterVisual, badgeObj);

            return root;
        }

        #endregion

        #region Underground Tunnel Structure Builders

        public static GameObject BuildMineArchSet(Transform parent, float width, float height)
        {
            InitMaterials();
            GameObject root = new GameObject("MineArchSet");
            root.transform.SetParent(parent, false);

            float halfW = width * 0.5f;
            float legW = 0.14f;
            float legD = 0.14f;

            // Left leg (I-beam post)
            AddBox(root, new Vector3(legW, height, legD), new Vector3(-halfW, height * 0.5f, 0), s_MatMineArchSteel);
            // Left baseplate
            AddBox(root, new Vector3(legW * 1.6f, 0.04f, legD * 1.6f), new Vector3(-halfW, 0.02f, 0), s_MatDarkIron);

            // Right leg (I-beam post)
            AddBox(root, new Vector3(legW, height, legD), new Vector3(halfW, height * 0.5f, 0), s_MatMineArchSteel);
            // Right baseplate
            AddBox(root, new Vector3(legW * 1.6f, 0.04f, legD * 1.6f), new Vector3(halfW, 0.02f, 0), s_MatDarkIron);

            // Overhead horizontal cap / arch crossbar
            AddBox(root, new Vector3(width + legW, legW, legD), new Vector3(0, height, 0), s_MatMineArchSteel);

            // Corner gusset braces
            GameObject leftGusset = AddBox(root, new Vector3(0.08f, 0.35f, 0.08f), new Vector3(-halfW + 0.18f, height - 0.14f, 0), s_MatDarkIron);
            leftGusset.transform.localRotation = Quaternion.Euler(0, 0, -45f);

            GameObject rightGusset = AddBox(root, new Vector3(0.08f, 0.35f, 0.08f), new Vector3(halfW - 0.18f, height - 0.14f, 0), s_MatDarkIron);
            rightGusset.transform.localRotation = Quaternion.Euler(0, 0, 45f);

            StripColliders(root);
            return root;
        }

        public static GameObject BuildRockWallSection(Transform parent, Vector3 size, Vector3 pos, Quaternion rot)
        {
            InitMaterials();
            GameObject root = new GameObject("RockWallSection");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            // Main rock/coal strata block
            AddBox(root, size, new Vector3(0, size.y * 0.5f, 0), s_MatCoalRock);

            // Uneven surface relief / rock protrusion details
            int protrusions = Mathf.Max(2, Mathf.RoundToInt(size.z * 1.5f));
            for (int i = 0; i < protrusions; i++)
            {
                float zRel = ((float)i / protrusions - 0.5f) * size.z * 0.85f;
                float yRel = UnityEngine.Random.Range(0.25f, 0.85f) * size.y;
                float bumpSize = UnityEngine.Random.Range(0.2f, 0.45f);
                Vector3 bump = new Vector3(bumpSize * 0.5f, bumpSize * 0.6f, bumpSize);
                GameObject p = AddBox(root, bump, new Vector3(size.x * 0.35f, yRel, zRel), s_MatRock);
                p.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(-10f, 10f));
            }

            StripColliders(root);
            return root;
        }

        public static GameObject BuildRoofBoltPlate(Transform parent, Vector3 pos)
        {
            InitMaterials();
            GameObject root = new GameObject("RoofBolt");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            // Square steel bearing plate (MSHA 6"x6" plate)
            AddBox(root, new Vector3(0.20f, 0.02f, 0.20f), Vector3.zero, s_MatSteel);
            // Center hexagonal bolt head & washer
            AddCylinder(root, new Vector3(0.06f, 0.035f, 0.06f), new Vector3(0, -0.02f, 0), s_MatDarkIron);

            StripColliders(root);
            return root;
        }

        public static GameObject BuildVentilationDuctLine(Transform parent, Vector3 start, Vector3 end, float diameter = 0.32f)
        {
            InitMaterials();
            GameObject root = new GameObject("VentilationDuctLine");
            root.transform.SetParent(parent, false);

            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.1f) return root;

            root.transform.position = start + delta * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(delta.normalized);

            // Flexible yellow duct segment (cylinder rotated to face forward along Z)
            AddCylinder(root, new Vector3(diameter, length * 0.5f, diameter), Vector3.zero, s_MatVentDuct, 90f);

            // Spiral reinforcement wire rings along the duct
            int rings = Mathf.Max(2, Mathf.RoundToInt(length / 0.5f));
            float halfLen = length * 0.5f;
            for (int i = 0; i <= rings; i++)
            {
                float z = -halfLen + (length * i / rings);
                AddCylinder(root, new Vector3(diameter * 1.05f, 0.02f, diameter * 1.05f), new Vector3(0, 0, z), s_MatDarkIron, 90f);
            }

            // Duct hanger cable from ceiling
            AddCylinder(root, new Vector3(0.015f, 0.25f, 0.015f), new Vector3(0, 0.25f, 0), s_MatSteel);

            StripColliders(root);
            return root;
        }

        public static GameObject BuildCableConduitLine(Transform parent, Vector3 start, Vector3 end, float thickness = 0.05f)
        {
            InitMaterials();
            GameObject root = new GameObject("CableConduitLine");
            root.transform.SetParent(parent, false);

            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.1f) return root;

            root.transform.position = start + delta * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(delta.normalized);

            // Black heavy insulated cable bundle
            AddCylinder(root, new Vector3(thickness, length * 0.5f, thickness), Vector3.zero, s_MatCableBlack, 90f);

            // Wall mounting brackets along cable run
            int brackets = Mathf.Max(2, Mathf.RoundToInt(length / 0.8f));
            float halfLen = length * 0.5f;
            for (int i = 0; i <= brackets; i++)
            {
                float z = -halfLen + (length * i / brackets);
                AddBox(root, new Vector3(0.03f, 0.08f, 0.03f), new Vector3(0, 0.03f, z), s_MatDarkIron);
            }

            StripColliders(root);
            return root;
        }

        public static GameObject BuildTunnelLightFixture(Transform parent, Vector3 pos, Quaternion rot)
        {
            InitMaterials();
            GameObject root = new GameObject("TunnelLightFixture");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            // Industrial ceiling mounting bracket
            AddBox(root, new Vector3(0.12f, 0.08f, 0.12f), Vector3.zero, s_MatDarkIron);

            // Cage fixture body (protective steel mesh cage)
            AddCylinder(root, new Vector3(0.14f, 0.16f, 0.14f), new Vector3(0, -0.12f, 0), s_MatSteel);

            // Luminous globe inside cage (emissive visual glow)
            AddCylinder(root, new Vector3(0.09f, 0.10f, 0.09f), new Vector3(0, -0.12f, 0), s_MatYellow);

            StripColliders(root);
            return root;
        }

        public static GameObject BuildWarningSign(Transform parent, string titleText, string subText, Vector3 pos, Quaternion rot, Color accentColor)
        {
            InitMaterials();
            GameObject root = new GameObject("WarningSign");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            // Border backing plate
            Material borderMat = CreateMat("SignBorderMat", accentColor);
            AddBox(root, new Vector3(0.52f, 0.36f, 0.02f), Vector3.zero, borderMat);

            // Inner white faceplate
            AddBox(root, new Vector3(0.48f, 0.32f, 0.025f), new Vector3(0, 0, 0.005f), s_MatSignWhite);

            // Upper header bar
            AddBox(root, new Vector3(0.46f, 0.08f, 0.028f), new Vector3(0, 0.10f, 0.008f), borderMat);

            // Title Label (e.g. DANGER / CAUTION / ESCAPE)
            AddLabelTag(root, titleText, 0.10f, Color.white, 24);

            // Subtitle text (e.g. HIGH VOLTAGE / CONVEYOR PINCH / EMERGENCY EXIT)
            AddLabelTag(root, subText, -0.04f, Color.black, 18);

            StripColliders(root);
            return root;
        }

        public static GameObject BuildFloorEscapeMarker(Transform parent, Vector3 pos, Quaternion rot)
        {
            InitMaterials();
            GameObject root = new GameObject("FloorEscapeMarker");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            // Luminous green arrow bar base
            AddBox(root, new Vector3(0.12f, 0.006f, 0.40f), new Vector3(0, 0.003f, 0), s_MatExitGreen);

            // Chevron arrow head
            GameObject chevronL = AddBox(root, new Vector3(0.08f, 0.007f, 0.22f), new Vector3(-0.07f, 0.0035f, 0.14f), s_MatExitGreen);
            chevronL.transform.localRotation = Quaternion.Euler(0, -35f, 0);

            GameObject chevronR = AddBox(root, new Vector3(0.08f, 0.007f, 0.22f), new Vector3(0.07f, 0.0035f, 0.14f), s_MatExitGreen);
            chevronR.transform.localRotation = Quaternion.Euler(0, 35f, 0);

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
    /// Uses static camera caching and staggered updates (15 FPS) to eliminate main-thread per-frame overhead.
    /// </summary>
    public class BillboardLookAt : MonoBehaviour
    {
        private static Transform s_CachedCameraTransform;
        private static int s_GlobalCounter = 0;
        private int _instanceOffset;

        private void Awake()
        {
            _instanceOffset = (++s_GlobalCounter) & 3;
        }

        private void Start()
        {
            EnsureCamera();
            UpdateOrientation();
        }

        private static void EnsureCamera()
        {
            if (s_CachedCameraTransform == null && Camera.main != null)
                s_CachedCameraTransform = Camera.main.transform;
        }

        private void LateUpdate()
        {
            // Stagger updates across 4 frames (~15 FPS billboard update) to eliminate CPU lag
            if (((Time.frameCount + _instanceOffset) & 3) != 0) return;

            EnsureCamera();
            UpdateOrientation();
        }

        private void UpdateOrientation()
        {
            if (s_CachedCameraTransform != null)
            {
                transform.LookAt(transform.position + s_CachedCameraTransform.rotation * Vector3.forward,
                                 s_CachedCameraTransform.rotation * Vector3.up);
            }
        }
    }
}
