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
using ARMiningSimulator.Core;
using ARMiningSimulator.Environment;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Player;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Extinguisher;
using ARMiningSimulator.Investigation;
using ARMiningSimulator.Progression;
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// Master Automation Utility for Phase 10: Final Simulation Scene & Multi-Level System.
    /// Accessible via Unity menu bar: "AR Training > 13. Setup Phase 10 (Final Complete Simulator)"
    /// </summary>
    public static class ARPhase10SetupUtility
    {
        private const string ScenePath = "Assets/Scenes/ARPhase10_FinalSimulationScene.unity";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!File.Exists(ScenePath))
                {
                    Debug.Log("[AR Setup] Auto-generating Phase 10 Master Simulation Scene...");
                    RunPhase10SetupSilently();
                    Debug.Log("[AR Setup] Phase 10 Master Scene generated at " + ScenePath);
                }
            };
        }

        [MenuItem("AR Training/13. Setup Phase 10 (Final Complete Simulator)", priority = 130)]
        public static void RunPhase10Setup()
        {
            RunPhase10SetupSilently();

            EditorUtility.DisplayDialog(
                "Phase 10 Setup Complete",
                "AR Mining Fire Safety Simulator — COMPLETE FINAL INTEGRATION READY!\n\n" +
                "• Master Scene: Assets/Scenes/ARPhase10_FinalSimulationScene.unity\n" +
                "• Complete Game Loop: Scan -> Measure -> Mine Room -> Equipment -> Fire -> Detect -> Decisions 1 & 2 -> Evacuate -> Extinguish -> Investigate -> Score -> Unlock Next Level\n" +
                "• 3 Progressive Levels:\n" +
                "   1. Haulage Drift Belt Conveyor Fire (Incipient Dust & Oil)\n" +
                "   2. High-Voltage Substation (Arc Flash & Electrocution Hazard)\n" +
                "   3. Continuous Miner Section (Explosive Gas & Rapid Spread)\n" +
                "• Scenario Selector Menu: Choose levels, inspect high scores, retry drills\n" +
                "• Dual Mode: Full Android ARCore phone support + Laptop Webcam Play Mode\n" +
                "• Active Build Scene: Configured (Index 0)\n\n" +
                "Hit Play in Unity Editor to experience the full simulator!",
                "OK"
            );
        }

        public static void RunPhase10SetupSilently()
        {
            EnsureDirectories();
            ARPhase1SetupUtility.ConfigureURPRenderer();
            ARPhase1SetupUtility.EnableARCoreLoader();

            CreatePhase10Scene();
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

        private static void CreatePhase10Scene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light lightComp = lightGo.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.15f;
            lightComp.color = new Color(0.96f, 0.93f, 0.89f);
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
            ARAnchorManager anchorManager = xrOriginGo.AddComponent<ARAnchorManager>();

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
            var occlusionMgr = cameraGo.AddComponent<AROcclusionManager>();
            occlusionMgr.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;

            cameraGo.AddComponent<EditorWebcamBackground>();
            cameraGo.AddComponent<EditorCameraController>();
            var detection = cameraGo.AddComponent<TraineeDetection>();
            var health = cameraGo.AddComponent<TraineeHealth>();
            var extinguisherCtrl = cameraGo.AddComponent<ExtinguisherController>();

            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.Camera = cam;
            xrOrigin.CameraYOffset = 0f;
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

            cameraOffsetGo.transform.localPosition = Vector3.zero;
            cameraOffsetGo.transform.localRotation = Quaternion.identity;

            cameraGo.AddComponent<ARCameraPoseDriver>();

            // 5. Room Measurement & Scanner Components (Refactored Subsystems)
            var raycastController = xrOriginGo.AddComponent<ARRaycastController>();
            var flagManager = xrOriginGo.AddComponent<ARFlagManager>();
            var lineManager = xrOriginGo.AddComponent<MeasurementLineManager>();
            var polygonManager = xrOriginGo.AddComponent<RoomPolygonManager>();
            var measurement = xrOriginGo.AddComponent<ARRoomMeasurement>();
            var scanner = xrOriginGo.AddComponent<ARRoomScanner>();
            var roomUI = xrOriginGo.AddComponent<RoomMeasurementUI>();

            SerializedObject soMeasurement = new SerializedObject(measurement);
            soMeasurement.FindProperty("_raycastController").objectReferenceValue = raycastController;
            soMeasurement.FindProperty("_flagManager").objectReferenceValue = flagManager;
            soMeasurement.FindProperty("_lineManager").objectReferenceValue = lineManager;
            soMeasurement.FindProperty("_polygonManager").objectReferenceValue = polygonManager;
            soMeasurement.FindProperty("_planeManager").objectReferenceValue = planeManager;
            soMeasurement.FindProperty("_arCamera").objectReferenceValue = cam;
            var sessionProp = soMeasurement.FindProperty("_arSession");
            if (sessionProp != null) sessionProp.objectReferenceValue = arSessionGo.GetComponent<ARSession>();
            var occlProp = soMeasurement.FindProperty("_occlusionManager");
            if (occlProp != null) occlProp.objectReferenceValue = occlusionMgr;
            soMeasurement.ApplyModifiedProperties();

            SerializedObject soFlag = new SerializedObject(flagManager);
            soFlag.FindProperty("_anchorManager").objectReferenceValue = anchorManager;
            soFlag.FindProperty("_arCamera").objectReferenceValue = cam;
            soFlag.ApplyModifiedProperties();

            SerializedObject soRay = new SerializedObject(raycastController);
            soRay.FindProperty("_raycastManager").objectReferenceValue = raycastManager;
            soRay.FindProperty("_planeManager").objectReferenceValue = planeManager;
            soRay.FindProperty("_arCamera").objectReferenceValue = cam;
            soRay.ApplyModifiedProperties();

            SerializedObject soScanner = new SerializedObject(scanner);
            soScanner.FindProperty("_planeManager").objectReferenceValue = planeManager;
            soScanner.FindProperty("_measurement").objectReferenceValue = measurement;
            soScanner.ApplyModifiedProperties();

            // 6. Environment & Spawner
            var spawner = xrOriginGo.AddComponent<EquipmentSpawner>();
            var generator = xrOriginGo.AddComponent<MiningEnvironmentGenerator>();

            // 7. Fire & Decision Systems
            var spreadSystem = xrOriginGo.AddComponent<FireSpreadSystem>();
            var fireManager = xrOriginGo.AddComponent<FireManager>();
            var decisionManager = xrOriginGo.AddComponent<DecisionManager>();

            // 8. Evacuation Systems
            var pathVisualizer = xrOriginGo.AddComponent<EvacuationPathVisualizer>();
            var evacuationManager = xrOriginGo.AddComponent<EvacuationManager>();

            // 9. Fire Response System
            var fireResponseManager = xrOriginGo.AddComponent<FireResponseManager>();

            // 10. Incident Investigation System
            var investigationManager = xrOriginGo.AddComponent<InvestigationManager>();

            // 11. Progression & Master Loop (Phase 10)
            var progressionManager = xrOriginGo.AddComponent<GameProgressionManager>();
            var gameLoop = xrOriginGo.AddComponent<SimulationGameLoop>();

            // 12. HUDs
            var statusHUD = xrOriginGo.AddComponent<TraineeStatusHUD>();
            var decisionUI = xrOriginGo.AddComponent<DecisionUI>();
            var evacuationHUD = xrOriginGo.AddComponent<EvacuationHUD>();
            var extinguisherHUD = xrOriginGo.AddComponent<ExtinguisherHUD>();
            var investigationUI = xrOriginGo.AddComponent<InvestigationAndScoreUI>();
            var mainMenuUI = xrOriginGo.AddComponent<MainMenuUI>();

            // Connect serialized fields
            SerializedObject soGen = new SerializedObject(generator);
            soGen.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soGen.FindProperty("_arCamera").objectReferenceValue = cam;
            var autoGenProp = soGen.FindProperty("_autoGenerateOnStart");
            if (autoGenProp != null) autoGenProp.boolValue = false;
            soGen.ApplyModifiedProperties();

            SerializedObject soFire = new SerializedObject(fireManager);
            soFire.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soFire.FindProperty("_spreadSystem").objectReferenceValue = spreadSystem;
            soFire.ApplyModifiedProperties();

            SerializedObject soExting = new SerializedObject(extinguisherCtrl);
            var camProp = soExting.FindProperty("_traineeCamera");
            if (camProp != null) camProp.objectReferenceValue = cam;
            soExting.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[AR Setup] Saved Phase 10 scene to {ScenePath}");
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
