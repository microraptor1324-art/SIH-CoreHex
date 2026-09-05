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
using ARMiningSimulator.Environment;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Player;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// Automation utility for Phase 7: Real-Time Evacuation to Safe Zone.
    /// Accessible via Unity menu bar: "AR Training > 10. Setup Phase 7 (Evacuation Scene)"
    /// </summary>
    public static class ARPhase7SetupUtility
    {
        private const string ScenePath = "Assets/Scenes/ARPhase7_EvacuationScene.unity";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!File.Exists(ScenePath))
                {
                    Debug.Log("[AR Setup] Auto-generating Phase 7 Evacuation Scene...");
                    RunPhase7SetupSilently();
                    Debug.Log("[AR Setup] Phase 7 Scene generated at " + ScenePath);
                }
            };
        }

        [MenuItem("AR Training/10. Setup Phase 7 (Evacuation Scene)", priority = 100)]
        public static void RunPhase7Setup()
        {
            RunPhase7SetupSilently();

            EditorUtility.DisplayDialog(
                "Phase 7 Setup Complete",
                "AR Phase 7 Evacuation Scene created successfully!\n\n" +
                "• Scene: Assets/Scenes/ARPhase7_EvacuationScene.unity\n" +
                "• Evacuation Path Visualizer: Animated 3D emissive green floor chevrons\n" +
                "• Evacuation Manager: 30-second countdown timer and safe zone proximity check (1.25m)\n" +
                "• Emergency Alarm Beacon: Flashing red strobe mounted on Emergency Exit\n" +
                "• Evacuation HUD: Real-time distance counter, countdown bar, and arrival celebration modal\n" +
                "• Speed Bonus: Awards up to +150 PTS for prompt evacuation\n" +
                "• Active Build Scene: Configured (Index 0)\n\n" +
                "Open the scene to test in Play Mode or build to your phone!",
                "OK"
            );
        }

        public static void RunPhase7SetupSilently()
        {
            EnsureDirectories();
            ARPhase1SetupUtility.ConfigureURPRenderer();
            ARPhase1SetupUtility.EnableARCoreLoader();

            CreatePhase7Scene();
            AddSceneToBuildSettings();
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Prefabs"))
                Directory.CreateDirectory("Assets/Prefabs");
            if (!Directory.Exists("Assets/Scenes"))
                Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
        }

        private static void CreatePhase7Scene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light lightComp = lightGo.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.1f;
            lightComp.color = new Color(0.95f, 0.92f, 0.88f);
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

            GameObject planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ARPlane_Prefab.prefab");
            if (planePrefab != null) planeManager.planePrefab = planePrefab;

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
            cameraGo.AddComponent<EditorCameraController>();
            var detection = cameraGo.AddComponent<TraineeDetection>();
            var health = cameraGo.AddComponent<TraineeHealth>();

            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.Camera = cam;

            // 5. Environment & Spawner
            var spawner = xrOriginGo.AddComponent<EquipmentSpawner>();
            var generator = xrOriginGo.AddComponent<MiningEnvironmentGenerator>();

            // 6. Fire & Decision Systems
            var spreadSystem = xrOriginGo.AddComponent<FireSpreadSystem>();
            var fireManager = xrOriginGo.AddComponent<FireManager>();
            var decisionManager = xrOriginGo.AddComponent<DecisionManager>();

            // 7. Evacuation Systems
            var pathVisualizer = xrOriginGo.AddComponent<EvacuationPathVisualizer>();
            var evacuationManager = xrOriginGo.AddComponent<EvacuationManager>();

            // 8. HUDs
            var statusHUD = xrOriginGo.AddComponent<TraineeStatusHUD>();
            var decisionUI = xrOriginGo.AddComponent<DecisionUI>();
            var evacuationHUD = xrOriginGo.AddComponent<EvacuationHUD>();
            var fireTestingUI = xrOriginGo.AddComponent<FireTestingUI>();

            // Connect serialized fields
            SerializedObject soGen = new SerializedObject(generator);
            soGen.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soGen.FindProperty("_arCamera").objectReferenceValue = cam;
            soGen.ApplyModifiedProperties();

            SerializedObject soFire = new SerializedObject(fireManager);
            soFire.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soFire.FindProperty("_spreadSystem").objectReferenceValue = spreadSystem;
            soFire.ApplyModifiedProperties();

            SerializedObject soFireUI = new SerializedObject(fireTestingUI);
            soFireUI.FindProperty("_fireManager").objectReferenceValue = fireManager;
            soFireUI.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[AR Setup] Saved Phase 7 scene to {ScenePath}");
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
