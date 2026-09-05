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
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// Automation utility for Phase 3: Mining Environment Generation & Equipment Placement.
    /// Accessible via Unity menu bar: "AR Training > 6. Setup Phase 3 (Mining Environment Scene)"
    /// </summary>
    public static class ARPhase3SetupUtility
    {
        private const string ScenePath = "Assets/Scenes/ARPhase3_MiningEnvironmentScene.unity";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!File.Exists(ScenePath))
                {
                    Debug.Log("[AR Setup] Auto-generating Phase 3 Mining Environment Scene...");
                    RunPhase3SetupSilently();
                    Debug.Log("[AR Setup] Phase 3 Scene generated at " + ScenePath);
                }
            };
        }

        [MenuItem("AR Training/6. Setup Phase 3 (Mining Environment Scene)", priority = 60)]
        public static void RunPhase3Setup()
        {
            RunPhase3SetupSilently();

            EditorUtility.DisplayDialog(
                "Phase 3 Setup Complete",
                "AR Phase 3 Mining Environment Scene created successfully!\n\n" +
                "• Scene: Assets/Scenes/ARPhase3_MiningEnvironmentScene.unity\n" +
                "• Generator: MiningEnvironmentGenerator\n" +
                "• Equipment: 5 Mining Machines + 7 Electrical Items\n" +
                "• Safety Features: Emergency Exit Doorway & Safe Zone Pad\n" +
                "• Active Build Scene: Configured (Index 0)\n\n" +
                "Open the scene to test procedural generation in Play Mode or build to Android!",
                "OK"
            );
        }

        public static void RunPhase3SetupSilently()
        {
            EnsureDirectories();
            ARPhase1SetupUtility.ConfigureURPRenderer();
            ARPhase1SetupUtility.EnableARCoreLoader();

            CreatePhase3Scene();
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

        private static void CreatePhase3Scene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Directional Light
            GameObject lightGo = new GameObject("Directional Light");
            Light lightComp = lightGo.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1.3f;
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
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.Camera = cam;

            // 5. Mining Environment Generator & Spawner
            var spawner = xrOriginGo.AddComponent<EquipmentSpawner>();
            var generator = xrOriginGo.AddComponent<MiningEnvironmentGenerator>();
            var ui = xrOriginGo.AddComponent<MiningEnvironmentUI>();

            // Wire serialized fields
            SerializedObject soGen = new SerializedObject(generator);
            soGen.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soGen.FindProperty("_arCamera").objectReferenceValue = cam;
            soGen.ApplyModifiedProperties();

            SerializedObject soUI = new SerializedObject(ui);
            soUI.FindProperty("_generator").objectReferenceValue = generator;
            soUI.FindProperty("_arCamera").objectReferenceValue = cam;
            soUI.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[AR Setup] Saved Phase 3 scene to {ScenePath}");
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
