#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using ARMiningSimulator.AR;
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// Editor automation utility for Phase 2: AR Room Scanning & Measurement.
    /// Accessible via Unity menu bar: "AR Training > 5. Setup Phase 2 (Room Measurement Scene)"
    /// </summary>
    public static class ARPhase2SetupUtility
    {
        private const string ScenePath = "Assets/Scenes/ARPhase2_RoomMeasurementScene.unity";
        private const string CornerPinPrefabPath = "Assets/Prefabs/CornerPin_Prefab.prefab";
        private const string PlanePrefabPath = "Assets/Prefabs/ARPlane_Prefab.prefab";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!File.Exists(ScenePath))
                {
                    Debug.Log("[AR Setup] Auto-generating Phase 2 Scene...");
                    EnsureDirectories();
                    ARPhase1SetupUtility.ConfigureURPRenderer();
                    ARPhase1SetupUtility.EnableARCoreLoader();
                    GameObject planePrefab = EnsurePlanePrefab();
                    GameObject pinPrefab = CreateOrUpdateCornerPinPrefab();
                    CreatePhase2Scene(planePrefab, pinPrefab);
                    AddSceneToBuildSettings();
                    Debug.Log("[AR Setup] Phase 2 Scene generated automatically at " + ScenePath);
                }
            };
        }

        [MenuItem("AR Training/5. Setup Phase 2 (Room Measurement Scene)", priority = 50)]
        public static void RunPhase2Setup()
        {
            EnsureDirectories();
            ARPhase1SetupUtility.ConfigureURPRenderer();
            ARPhase1SetupUtility.EnableARCoreLoader();

            GameObject planePrefab = EnsurePlanePrefab();
            GameObject pinPrefab = CreateOrUpdateCornerPinPrefab();
            CreatePhase2Scene(planePrefab, pinPrefab);
            AddSceneToBuildSettings();

            EditorUtility.DisplayDialog(
                "Phase 2 Setup Complete",
                "AR Phase 2 Room Measurement scene and assets created successfully!\n\n" +
                "• Scene: Assets/Scenes/ARPhase2_RoomMeasurementScene.unity\n" +
                "• Corner Pin Prefab: Assets/Prefabs/CornerPin_Prefab.prefab\n" +
                "• Controllers: ARRoomMeasurement, ARRoomScanner, RoomMeasurementUI\n" +
                "• Build Settings: Set as active scene (Index 0)\n\n" +
                "You can now test room measurement in Play Mode or build to your phone!",
                "OK"
            );
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Prefabs"))
                Directory.CreateDirectory("Assets/Prefabs");
            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }

        private static GameObject EnsurePlanePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            if (existing != null) return existing;

            // Generate plane prefab if not found
            GameObject planeGo = new GameObject("ARPlane_Visualizer");
            planeGo.AddComponent<ARPlane>();
            planeGo.AddComponent<ARPlaneMeshVisualizer>();
            planeGo.AddComponent<MeshFilter>();
            MeshRenderer mr = planeGo.AddComponent<MeshRenderer>();
            LineRenderer lr = planeGo.AddComponent<LineRenderer>();
            planeGo.AddComponent<ARPlaneVisualizerHelper>();

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (urpLit != null)
            {
                Material mat = new Material(urpLit);
                mat.name = "ARPlane_Mat";
                mat.color = new Color(0.1f, 0.8f, 0.4f, 0.35f);
                mr.sharedMaterial = mat;

                Material lineMat = new Material(urpLit);
                lineMat.name = "ARPlane_LineMat";
                lineMat.color = new Color(0.2f, 1f, 0.5f, 0.9f);
                lr.sharedMaterial = lineMat;
            }

            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.useWorldSpace = false;
            lr.loop = true;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(planeGo, PlanePrefabPath);
            Object.DestroyImmediate(planeGo);
            return prefab;
        }

        private static GameObject CreateOrUpdateCornerPinPrefab()
        {
            EnsureDirectories();

            GameObject pinRoot = new GameObject("CornerPin");

            // Base disk
            GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisk.name = "Base";
            baseDisk.transform.SetParent(pinRoot.transform, false);
            baseDisk.transform.localScale = new Vector3(0.18f, 0.015f, 0.18f);
            baseDisk.transform.localPosition = new Vector3(0f, 0.0075f, 0f);

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(pinRoot.transform, false);
            pole.transform.localScale = new Vector3(0.035f, 0.18f, 0.035f);
            pole.transform.localPosition = new Vector3(0f, 0.18f, 0f);

            // Glowing surveyor sphere top
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PinHead";
            sphere.transform.SetParent(pinRoot.transform, false);
            sphere.transform.localScale = Vector3.one * 0.12f;
            sphere.transform.localPosition = new Vector3(0f, 0.4f, 0f);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material cyanMat = new Material(shader);
                cyanMat.name = "CornerPin_Mat";
                cyanMat.color = new Color(0.1f, 0.85f, 0.95f); // Cyan surveyor color

                baseDisk.GetComponent<Renderer>().sharedMaterial = cyanMat;
                pole.GetComponent<Renderer>().sharedMaterial = cyanMat;
                sphere.GetComponent<Renderer>().sharedMaterial = cyanMat;
            }

            // Remove colliders so raycasts are uninterrupted
            foreach (var c in pinRoot.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pinRoot, CornerPinPrefabPath);
            Object.DestroyImmediate(pinRoot);
            Debug.Log($"[AR Setup] Created CornerPin prefab at {CornerPinPrefabPath}");
            return prefab;
        }

        private static void CreatePhase2Scene(GameObject planePrefab, GameObject pinPrefab)
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light lightComp = lightGo.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.2f;
            lightComp.color = Color.white;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. AR Session
            GameObject arSessionGo = new GameObject("AR Session");
            arSessionGo.AddComponent<ARSession>();
            arSessionGo.AddComponent<ARInputManager>();

            // 3. XR Origin (XR Rig)
            GameObject xrOriginGo = new GameObject("XR Origin (XR Rig)");
            XROrigin xrOrigin = xrOriginGo.AddComponent<XROrigin>();
            ARPlaneManager planeManager = xrOriginGo.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            if (planePrefab != null)
            {
                planeManager.planePrefab = planePrefab;
            }

            ARRaycastManager raycastManager = xrOriginGo.AddComponent<ARRaycastManager>();

            // 4. Camera Offset & Main Camera
            GameObject cameraOffsetGo = new GameObject("Camera Offset");
            cameraOffsetGo.transform.SetParent(xrOriginGo.transform, false);

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraOffsetGo.transform, false);

            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            cameraGo.AddComponent<ARCameraManager>();
            cameraGo.AddComponent<ARCameraBackground>();
            cameraGo.AddComponent<EditorWebcamBackground>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.Camera = cam;

            // 5. AR Room Measurement & Scanner Components
            var measurement = xrOriginGo.AddComponent<ARRoomMeasurement>();
            var scanner = xrOriginGo.AddComponent<ARRoomScanner>();
            var ui = xrOriginGo.AddComponent<RoomMeasurementUI>();

            // Serialize references
            SerializedObject soMeasurement = new SerializedObject(measurement);
            soMeasurement.FindProperty("_raycastManager").objectReferenceValue = raycastManager;
            soMeasurement.FindProperty("_planeManager").objectReferenceValue = planeManager;
            soMeasurement.FindProperty("_arCamera").objectReferenceValue = cam;
            if (pinPrefab != null)
            {
                soMeasurement.FindProperty("_cornerPinPrefab").objectReferenceValue = pinPrefab;
            }
            soMeasurement.ApplyModifiedProperties();

            SerializedObject soScanner = new SerializedObject(scanner);
            soScanner.FindProperty("_planeManager").objectReferenceValue = planeManager;
            soScanner.FindProperty("_measurement").objectReferenceValue = measurement;
            soScanner.ApplyModifiedProperties();

            SerializedObject soUI = new SerializedObject(ui);
            soUI.FindProperty("_measurement").objectReferenceValue = measurement;
            soUI.FindProperty("_scanner").objectReferenceValue = scanner;
            soUI.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[AR Setup] Saved Phase 2 scene to {ScenePath}");
        }

        private static void AddSceneToBuildSettings()
        {
            var originalScenes = EditorBuildSettings.scenes;
            bool sceneAlreadyInBuild = false;

            foreach (var scene in originalScenes)
            {
                if (scene.path == ScenePath)
                {
                    sceneAlreadyInBuild = true;
                    break;
                }
            }

            if (!sceneAlreadyInBuild)
            {
                var newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(ScenePath, true);
                for (int i = 0; i < originalScenes.Length; i++)
                {
                    newScenes[i + 1] = originalScenes[i];
                }
                EditorBuildSettings.scenes = newScenes;
                Debug.Log($"[AR Setup] Added {ScenePath} to EditorBuildSettings at Index 0.");
            }
        }
    }
}
#endif
