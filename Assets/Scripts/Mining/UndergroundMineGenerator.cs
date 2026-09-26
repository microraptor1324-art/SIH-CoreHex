using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.AR;

namespace ARMiningSimulator.Mining
{
    /// <summary>
    /// Generates a realistic underground mining environment inside the AR-measured room boundary.
    /// Inspired by authentic underground coal/hard-rock mine drift tunnels:
    /// - Heavy timber polygonal support sets (posts, caps, knee braces, ceiling stringers)
    /// - Dual steel railway tracks on wooden ties (sleepers) running down the centerline
    /// - Detailed industrial mine ore cart with iron flanged wheels and coal cargo
    /// - Warm hanging incandescent cage work lamps with real-time atmospheric point lights
    /// - Flexible industrial ventilation ducting suspended along the rafters
    /// - Scattered coal and rock rubble heaps along the tunnel shoulders
    /// </summary>
    public class UndergroundMineGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [Tooltip("Spacing between timber support frames along the tunnel length.")]
        [SerializeField] private float _frameSpacing = 2.0f;

        [Tooltip("Minimum tunnel ceiling height inside the room boundary.")]
        [SerializeField] private float _tunnelHeight = 2.7f;

        [Tooltip("Gauge (width) of the mine cart railway tracks.")]
        [SerializeField] private float _trackGauge = 0.80f;

        [Tooltip("Include real-time warm incandescent point lights on hanging lamps.")]
        [SerializeField] private bool _enableWorkLights = true;

        [Tooltip("Include a mine ore cart on the railway tracks.")]
        [SerializeField] private bool _spawnOreCart = true;

        private GameObject _environmentRoot;
        private bool _isGenerated = false;

        public bool IsGenerated => _isGenerated;
        public GameObject EnvironmentRoot => _environmentRoot;

        /// <summary>
        /// Generates the complete underground mining environment strictly inside the measured room box.
        /// </summary>
        public void GenerateEnvironment(RoomData roomData)
        {
            ClearEnvironment();

            if (roomData == null || !roomData.IsValid)
            {
                Debug.LogWarning("[UndergroundMineGenerator] Cannot generate: room data is invalid or unconfirmed.");
                return;
            }

            float length = Mathf.Max(roomData.Length, 2.0f);
            float width = Mathf.Max(roomData.Width, 1.8f);
            Vector3 center = roomData.Center;
            Quaternion rotation = roomData.Rotation;

            _environmentRoot = new GameObject("[Underground_Mining_Environment]");
            _environmentRoot.transform.position = center;
            _environmentRoot.transform.rotation = rotation;
            _environmentRoot.transform.SetParent(transform, true);

            // User requirement: Remove blue shading and flags once the mining environment is generated
            var polyManager = GetComponent<RoomPolygonManager>() ?? FindFirstObjectByType<RoomPolygonManager>();
            if (polyManager != null) polyManager.ClearShadedFloorPolygon();

            var flagManager = GetComponent<ARFlagManager>() ?? FindFirstObjectByType<ARFlagManager>();
            if (flagManager != null) flagManager.SetFlagsVisible(false);

            var lineManager = GetComponent<MeasurementLineManager>() ?? FindFirstObjectByType<MeasurementLineManager>();
            if (lineManager != null) lineManager.SetLinesVisible(false);

            // 1. Build Timber Arch Support Sets (Posts, Caps, Knee Braces, Stringers)
            BuildTimberSupportSets(length, width);

            // 2. Build Mine Cart Railway Tracks (Rails & Wooden Sleepers)
            BuildRailwayTracks(length);

            // 3. Build Mine Ore Cart on Tracks
            if (_spawnOreCart)
            {
                BuildOreCart(length);
            }

            // 4. Build Hanging Caged Work Lamps with Warm Atmospheric Lighting
            if (_enableWorkLights)
            {
                BuildHangingWorkLamps(length, width);
            }

            // 5. Build Industrial Ventilation Duct
            BuildVentilationDuct(length, width);

            // 6. Build Coal and Rock Rubble Piles
            BuildRubblePiles(length, width);

            _isGenerated = true;
            Debug.Log($"[UndergroundMineGenerator] Successfully generated underground mine environment inside room ({length:F2}m x {width:F2}m).");
        }

        /// <summary>
        /// Destroys all spawned mining models and restores the room to clean AR view.
        /// </summary>
        public void ClearEnvironment()
        {
            if (_environmentRoot != null)
            {
                Destroy(_environmentRoot);
                _environmentRoot = null;
            }
            _isGenerated = false;
        }

        #region Timber Support Sets

        private void BuildTimberSupportSets(float length, float width)
        {
            GameObject setsRoot = new GameObject("TimberSupportSets");
            setsRoot.transform.SetParent(_environmentRoot.transform, false);

            Material timberMat = MiningMaterialFactory.GetTimberMaterial();

            float halfLen = length * 0.5f;
            float margin = 0.4f;
            float usableLen = length - margin * 2f;
            int numSets = Mathf.Max(2, Mathf.RoundToInt(usableLen / _frameSpacing) + 1);
            float actualSpacing = usableLen / (numSets - 1);

            float setWidth = Mathf.Max(1.4f, width - 0.25f);
            float setHeight = Mathf.Clamp(_tunnelHeight, 2.3f, 3.2f);
            float postThick = 0.20f;
            float capThick = 0.22f;

            List<Vector3> setPositions = new List<Vector3>();

            for (int i = 0; i < numSets; i++)
            {
                float z = -halfLen + margin + i * actualSpacing;
                setPositions.Add(new Vector3(0f, 0f, z));

                GameObject setObj = new GameObject($"TimberSet_{i + 1}");
                setObj.transform.SetParent(setsRoot.transform, false);
                setObj.transform.localPosition = new Vector3(0f, 0f, z);

                // Left Leg (Post)
                CreateBeam(setObj.transform, timberMat,
                    new Vector3(-setWidth * 0.5f, setHeight * 0.5f, 0f),
                    new Vector3(postThick, setHeight, postThick),
                    Quaternion.identity, "LeftPost");

                // Right Leg (Post)
                CreateBeam(setObj.transform, timberMat,
                    new Vector3(setWidth * 0.5f, setHeight * 0.5f, 0f),
                    new Vector3(postThick, setHeight, postThick),
                    Quaternion.identity, "RightPost");

                // Top Cap Beam (Header)
                CreateBeam(setObj.transform, timberMat,
                    new Vector3(0f, setHeight, 0f),
                    new Vector3(setWidth + postThick * 1.5f, capThick, capThick),
                    Quaternion.identity, "TopCap");

                // Left Knee Brace (45 degree diagonal corner support)
                float braceLen = 0.75f;
                float braceThick = 0.15f;
                Vector3 leftBracePos = new Vector3(-setWidth * 0.5f + 0.26f, setHeight - 0.26f, 0f);
                CreateBeam(setObj.transform, timberMat, leftBracePos,
                    new Vector3(braceThick, braceLen, braceThick),
                    Quaternion.Euler(0f, 0f, -45f), "LeftKneeBrace");

                // Right Knee Brace (45 degree diagonal corner support)
                Vector3 rightBracePos = new Vector3(setWidth * 0.5f - 0.26f, setHeight - 0.26f, 0f);
                CreateBeam(setObj.transform, timberMat, rightBracePos,
                    new Vector3(braceThick, braceLen, braceThick),
                    Quaternion.Euler(0f, 0f, 45f), "RightKneeBrace");

                // Base Footers (Mud sills)
                CreateBeam(setObj.transform, timberMat,
                    new Vector3(-setWidth * 0.5f, 0.04f, 0f),
                    new Vector3(postThick * 1.4f, 0.08f, postThick * 1.4f),
                    Quaternion.identity, "LeftFooter");

                CreateBeam(setObj.transform, timberMat,
                    new Vector3(setWidth * 0.5f, 0.04f, 0f),
                    new Vector3(postThick * 1.4f, 0.08f, postThick * 1.4f),
                    Quaternion.identity, "RightFooter");
            }

            // Longitudinal Runner Stringers connecting consecutive sets along ceiling
            for (int i = 0; i < numSets - 1; i++)
            {
                float zMid = (setPositions[i].z + setPositions[i + 1].z) * 0.5f;
                float spanLen = setPositions[i + 1].z - setPositions[i].z;
                float stringerThick = 0.14f;

                // Center ceiling stringer
                CreateBeam(setsRoot.transform, timberMat,
                    new Vector3(0f, setHeight + capThick * 0.5f + stringerThick * 0.5f, zMid),
                    new Vector3(stringerThick, stringerThick, spanLen),
                    Quaternion.identity, $"CeilingStringer_Mid_{i}");

                // Left shoulder stringer
                CreateBeam(setsRoot.transform, timberMat,
                    new Vector3(-setWidth * 0.5f + 0.1f, setHeight, zMid),
                    new Vector3(stringerThick, stringerThick, spanLen),
                    Quaternion.identity, $"CeilingStringer_Left_{i}");

                // Right shoulder stringer
                CreateBeam(setsRoot.transform, timberMat,
                    new Vector3(setWidth * 0.5f - 0.1f, setHeight, zMid),
                    new Vector3(stringerThick, stringerThick, spanLen),
                    Quaternion.identity, $"CeilingStringer_Right_{i}");
            }
        }

        #endregion

        #region Railway Tracks & Sleepers

        private void BuildRailwayTracks(float length)
        {
            GameObject trackRoot = new GameObject("MineRailwayTracks");
            trackRoot.transform.SetParent(_environmentRoot.transform, false);

            Material steelMat = MiningMaterialFactory.GetSteelRailMaterial();
            Material timberMat = MiningMaterialFactory.GetTimberMaterial();

            float halfLen = length * 0.5f;
            float trackLength = length - 0.4f;

            // 1. Dual Steel Rails
            float halfGauge = _trackGauge * 0.5f;
            float railWidth = 0.06f;
            float railHeight = 0.09f;
            float railY = 0.10f; // Sits on top of sleepers (0.06m)

            // Left Steel Rail
            CreateBeam(trackRoot.transform, steelMat,
                new Vector3(-halfGauge, railY, 0f),
                new Vector3(railWidth, railHeight, trackLength),
                Quaternion.identity, "LeftRail");

            // Right Steel Rail
            CreateBeam(trackRoot.transform, steelMat,
                new Vector3(halfGauge, railY, 0f),
                new Vector3(railWidth, railHeight, trackLength),
                Quaternion.identity, "RightRail");

            // 2. Wooden Sleepers (Ties)
            float sleeperSpacing = 0.55f;
            int numSleepers = Mathf.Max(3, Mathf.RoundToInt(trackLength / sleeperSpacing));
            float actualSleeperSpacing = trackLength / numSleepers;
            float sleeperWidth = _trackGauge + 0.45f;
            float sleeperHeight = 0.06f;
            float sleeperLength = 0.16f;

            for (int i = 0; i <= numSleepers; i++)
            {
                float z = -halfLen + 0.25f + i * actualSleeperSpacing;
                CreateBeam(trackRoot.transform, timberMat,
                    new Vector3(0f, sleeperHeight * 0.5f + 0.005f, z),
                    new Vector3(sleeperWidth, sleeperHeight, sleeperLength),
                    Quaternion.identity, $"Sleeper_{i}");
            }
        }

        #endregion

        #region Mine Ore Cart

        private void BuildOreCart(float roomLength)
        {
            GameObject cartRoot = new GameObject("MineOreCart");
            cartRoot.transform.SetParent(_environmentRoot.transform, false);

            // Park the cart along the tracks (about 25% along the room)
            float cartZ = Mathf.Clamp(roomLength * 0.20f, -roomLength * 0.35f, roomLength * 0.35f);
            cartRoot.transform.localPosition = new Vector3(0f, 0f, cartZ);

            Material cartMetal = MiningMaterialFactory.GetCartMetalMaterial();
            Material steelMat = MiningMaterialFactory.GetSteelRailMaterial();
            Material coalMat = MiningMaterialFactory.GetCoalOreMaterial();

            float halfGauge = _trackGauge * 0.5f;
            float wheelRadius = 0.15f;
            float axleY = 0.14f;
            float wheelbase = 0.85f;

            // 1. Wheelsets & Axles
            for (int a = 0; a < 2; a++)
            {
                float zAxle = (a == 0) ? -wheelbase * 0.5f : wheelbase * 0.5f;

                // Steel Axle Rod
                CreateBeam(cartRoot.transform, steelMat,
                    new Vector3(0f, axleY, zAxle),
                    new Vector3(_trackGauge + 0.10f, 0.05f, 0.05f),
                    Quaternion.identity, $"Axle_{a}");

                // Left Wheel (Cylinder)
                CreateCylinder(cartRoot.transform, steelMat,
                    new Vector3(-halfGauge, axleY, zAxle),
                    new Vector3(wheelRadius * 2f, 0.06f, wheelRadius * 2f),
                    Quaternion.Euler(0f, 0f, 90f), $"Wheel_L_{a}");

                // Right Wheel (Cylinder)
                CreateCylinder(cartRoot.transform, steelMat,
                    new Vector3(halfGauge, axleY, zAxle),
                    new Vector3(wheelRadius * 2f, 0.06f, wheelRadius * 2f),
                    Quaternion.Euler(0f, 0f, 90f), $"Wheel_R_{a}");
            }

            // 2. Chassis Frame
            float chassisY = axleY + 0.07f;
            CreateBeam(cartRoot.transform, steelMat,
                new Vector3(0f, chassisY, 0f),
                new Vector3(_trackGauge * 0.95f, 0.08f, wheelbase + 0.35f),
                Quaternion.identity, "ChassisFrame");

            // 3. Cart Hopper Body (Iron Bin)
            float bodyY = chassisY + 0.42f;
            float bodyLen = 1.30f;
            float bodyWid = 0.85f;
            float bodyHgt = 0.65f;

            // Bottom base plate
            CreateBeam(cartRoot.transform, cartMetal,
                new Vector3(0f, bodyY - bodyHgt * 0.45f, 0f),
                new Vector3(bodyWid * 0.8f, 0.05f, bodyLen * 0.9f),
                Quaternion.identity, "HopperBase");

            // Front plate
            CreateBeam(cartRoot.transform, cartMetal,
                new Vector3(0f, bodyY, bodyLen * 0.5f),
                new Vector3(bodyWid, bodyHgt, 0.05f),
                Quaternion.identity, "HopperFront");

            // Back plate
            CreateBeam(cartRoot.transform, cartMetal,
                new Vector3(0f, bodyY, -bodyLen * 0.5f),
                new Vector3(bodyWid, bodyHgt, 0.05f),
                Quaternion.identity, "HopperBack");

            // Left side plate
            CreateBeam(cartRoot.transform, cartMetal,
                new Vector3(-bodyWid * 0.5f, bodyY, 0f),
                new Vector3(0.05f, bodyHgt, bodyLen),
                Quaternion.identity, "HopperSideL");

            // Right side plate
            CreateBeam(cartRoot.transform, cartMetal,
                new Vector3(bodyWid * 0.5f, bodyY, 0f),
                new Vector3(0.05f, bodyHgt, bodyLen),
                Quaternion.identity, "HopperSideR");

            // Rim trim
            CreateBeam(cartRoot.transform, steelMat,
                new Vector3(0f, bodyY + bodyHgt * 0.5f, 0f),
                new Vector3(bodyWid + 0.06f, 0.04f, bodyLen + 0.06f),
                Quaternion.identity, "HopperRim");

            // 4. Coal/Ore Cargo Heap inside cart
            GameObject coalHeap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coalHeap.name = "CoalCargoHeap";
            coalHeap.transform.SetParent(cartRoot.transform, false);
            coalHeap.transform.localPosition = new Vector3(0f, bodyY + 0.18f, 0f);
            coalHeap.transform.localScale = new Vector3(bodyWid * 0.85f, 0.42f, bodyLen * 0.85f);
            Destroy(coalHeap.GetComponent<Collider>());
            coalHeap.GetComponent<Renderer>().sharedMaterial = coalMat;
        }

        #endregion

        #region Hanging Caged Work Lamps

        private void BuildHangingWorkLamps(float length, float width)
        {
            GameObject lightsRoot = new GameObject("AtmosphericWorkLights");
            lightsRoot.transform.SetParent(_environmentRoot.transform, false);

            Material steelMat = MiningMaterialFactory.GetSteelRailMaterial();
            Material glowMat = MiningMaterialFactory.GetLampGlowMaterial();

            float halfLen = length * 0.5f;
            float margin = 0.6f;
            float usableLen = length - margin * 2f;
            int numLamps = Mathf.Max(2, Mathf.RoundToInt(usableLen / (_frameSpacing * 1.5f)) + 1);
            float spacing = usableLen / (numLamps - 1);
            float setHeight = Mathf.Clamp(_tunnelHeight, 2.3f, 3.2f);

            for (int i = 0; i < numLamps; i++)
            {
                float z = -halfLen + margin + i * spacing;
                GameObject lampObj = new GameObject($"MineWorkLamp_{i + 1}");
                lampObj.transform.SetParent(lightsRoot.transform, false);
                lampObj.transform.localPosition = new Vector3(0f, 0f, z);

                // Hanging Cord / Chain
                float cordLen = 0.40f;
                float cordY = setHeight - cordLen * 0.5f;
                CreateBeam(lampObj.transform, steelMat,
                    new Vector3(0f, cordY, 0f),
                    new Vector3(0.02f, cordLen, 0.02f),
                    Quaternion.identity, "LampCord");

                // Metal Reflector Hood
                float hoodY = setHeight - cordLen;
                CreateCylinder(lampObj.transform, steelMat,
                    new Vector3(0f, hoodY, 0f),
                    new Vector3(0.24f, 0.05f, 0.24f),
                    Quaternion.identity, "ReflectorHood");

                // Glowing Bulb
                float bulbY = hoodY - 0.08f;
                GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "GlowingBulb";
                bulb.transform.SetParent(lampObj.transform, false);
                bulb.transform.localPosition = new Vector3(0f, bulbY, 0f);
                bulb.transform.localScale = Vector3.one * 0.12f;
                Destroy(bulb.GetComponent<Collider>());
                bulb.GetComponent<Renderer>().sharedMaterial = glowMat;

                // Protective Wire Cage Ring
                CreateCylinder(lampObj.transform, steelMat,
                    new Vector3(0f, bulbY - 0.04f, 0f),
                    new Vector3(0.18f, 0.10f, 0.18f),
                    Quaternion.identity, "WireCage");

                // Real-Time Atmospheric Point Light
                GameObject lightGo = new GameObject("WarmMineLight");
                lightGo.transform.SetParent(lampObj.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, bulbY - 0.05f, 0f);

                Light pointLight = lightGo.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.color = new Color(1.0f, 0.76f, 0.42f); // Warm incandescent amber (2800K)
                pointLight.intensity = 2.5f;
                pointLight.range = 5.5f;
                pointLight.shadows = LightShadows.Soft;
            }
        }

        #endregion

        #region Ventilation Ducting

        private void BuildVentilationDuct(float length, float width)
        {
            GameObject ductRoot = new GameObject("VentilationDuct");
            ductRoot.transform.SetParent(_environmentRoot.transform, false);

            Material ductMat = MiningMaterialFactory.GetVentDuctMaterial();
            Material steelMat = MiningMaterialFactory.GetSteelRailMaterial();

            float setWidth = Mathf.Max(1.4f, width - 0.25f);
            float setHeight = Mathf.Clamp(_tunnelHeight, 2.3f, 3.2f);
            float ductX = -setWidth * 0.5f + 0.35f; // Along upper left rafter
            float ductY = setHeight - 0.25f;
            float ductDia = 0.36f;
            float ductLength = length - 0.2f;

            // Main Corrugated Air Pipe
            CreateCylinder(ductRoot.transform, ductMat,
                new Vector3(ductX, ductY, 0f),
                new Vector3(ductDia, ductLength * 0.5f, ductDia),
                Quaternion.Euler(90f, 0f, 0f), "MainAirDuct");

            // Suspension brackets
            int numBrackets = Mathf.Max(2, Mathf.RoundToInt(length / _frameSpacing));
            float bracketSpacing = length / numBrackets;
            for (int i = 0; i <= numBrackets; i++)
            {
                float z = -length * 0.5f + 0.3f + i * bracketSpacing;
                CreateBeam(ductRoot.transform, steelMat,
                    new Vector3(ductX, ductY + ductDia * 0.5f + 0.06f, z),
                    new Vector3(0.04f, 0.14f, 0.04f),
                    Quaternion.identity, $"Hanger_{i}");
            }
        }

        #endregion

        #region Rubble Piles

        private void BuildRubblePiles(float length, float width)
        {
            GameObject rubbleRoot = new GameObject("RubblePiles");
            rubbleRoot.transform.SetParent(_environmentRoot.transform, false);

            Material coalMat = MiningMaterialFactory.GetCoalOreMaterial();

            float halfLen = length * 0.5f;
            float halfWid = width * 0.5f;

            // Clustered rock/coal piles in tunnel shoulders/corners
            Vector3[] pileCenters = new Vector3[]
            {
                new Vector3(-halfWid + 0.45f, 0f, -halfLen + 0.6f),
                new Vector3(halfWid - 0.45f, 0f, -halfLen + 0.9f),
                new Vector3(-halfWid + 0.40f, 0f, halfLen * 0.1f),
                new Vector3(halfWid - 0.40f, 0f, halfLen * 0.4f),
                new Vector3(-halfWid + 0.50f, 0f, halfLen - 0.7f),
                new Vector3(halfWid - 0.50f, 0f, halfLen - 0.5f),
            };

            int seed = 12345;
            for (int p = 0; p < pileCenters.Length; p++)
            {
                Vector3 center = pileCenters[p];
                int chunks = 4 + (p % 3);

                for (int c = 0; c < chunks; c++)
                {
                    seed += 37;
                    float offsetX = ((seed % 100) / 100f - 0.5f) * 0.35f;
                    float offsetZ = (((seed * 3) % 100) / 100f - 0.5f) * 0.35f;
                    float scale = 0.12f + ((seed * 7) % 100) / 100f * 0.18f;

                    GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chunk.name = $"RubbleChunk_{p}_{c}";
                    chunk.transform.SetParent(rubbleRoot.transform, false);
                    chunk.transform.localPosition = new Vector3(center.x + offsetX, scale * 0.45f, center.z + offsetZ);
                    chunk.transform.localRotation = Quaternion.Euler((seed * 11) % 360, (seed * 13) % 360, (seed * 17) % 360);
                    chunk.transform.localScale = new Vector3(scale, scale * 0.7f, scale * 1.1f);
                    Destroy(chunk.GetComponent<Collider>());
                    chunk.GetComponent<Renderer>().sharedMaterial = coalMat;
                }
            }
        }

        #endregion

        #region Primitive Helpers

        private static GameObject CreateBeam(Transform parent, Material mat, Vector3 localPos, Vector3 scale, Quaternion localRot, string name)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private static GameObject CreateCylinder(Transform parent, Material mat, Vector3 localPos, Vector3 scale, Quaternion localRot, string name)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            Destroy(go.GetComponent<Collider>());
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        #endregion
    }
}
