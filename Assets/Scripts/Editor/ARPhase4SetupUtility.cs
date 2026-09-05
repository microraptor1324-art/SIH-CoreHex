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
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// Automation utility for Phase 4: Random Fire Generation, Fire Hazard & Spread System.
    /// Accessible via Unity menu bar: "AR Training > 7. Setup Phase 4 (Fire System Scene)"
    /// </summary>
    public static class ARPhase4SetupUtility
    {
        private const string ScenePath = "Assets/Scenes/ARPhase4_FireSystemScene.unity";

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!File.Exists(ScenePath))
                {
                    Debug.Log("[AR Setup] Auto-generating Phase 4 Fire System Scene...");
                    RunPhase4SetupSilently();
                    Debug.Log("[AR Setup] Phase 4 Scene generated at " + ScenePath);
                }
            };
        }

        [MenuItem("AR Training/7. Setup Phase 4 (Fire System Scene)", priority = 70)]
        public static void RunPhase4Setup()
        {
            RunPhase4SetupSilently();

            EditorUtility.DisplayDialog(
                "Phase 4 Setup Complete",
                "AR Phase 4 Fire System Scene created successfully!\n\n" +
                "• Scene: Assets/Scenes/ARPhase4_FireSystemScene.unity\n" +
                "• Components: FireManager, FireSpreadSystem, FireHazard, FireTestingUI\n" +
                "• Random Ignition: 1-2 Mining Machines + 1-2 Electrical Units\n" +
                "• Progression: Small (0-10s) -> Medium (10-20s) -> Large (20s+)\n" +
                "• Active Build Scene: Configured (Index 0)\n\n" +
                "Open the scene to test in Play Mode or build to your phone!",
                "OK"
            );
        }

        public static void RunPhase4SetupSilently()
        {
            EnsureDirectories();
            ARPhase1SetupUtility.ConfigureURPRenderer();
            ARPhase1SetupUtility.EnableARCoreLoader();

            CreatePhase4Scene();
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

        private static void CreatePhase4Scene()
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
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.Camera = cam;

            // 5. Environment & Spawner
            var spawner = xrOriginGo.AddComponent<EquipmentSpawner>();
            var generator = xrOriginGo.AddComponent<MiningEnvironmentGenerator>();

            // 6. Fire Systems
            var spreadSystem = xrOriginGo.AddComponent<FireSpreadSystem>();
            var fireManager = xrOriginGo.AddComponent<FireManager>();
            var fireUI = xrOriginGo.AddComponent<FireTestingUI>();

            // Serialize connections
            SerializedObject soGen = new SerializedObject(generator);
            soGen.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soGen.FindProperty("_arCamera").objectReferenceValue = cam;
            soGen.ApplyModifiedProperties();

            SerializedObject soFire = new SerializedObject(fireManager);
            soFire.FindProperty("_equipmentSpawner").objectReferenceValue = spawner;
            soFire.FindProperty("_spreadSystem").objectReferenceValue = spreadSystem;
            soFire.ApplyModifiedProperties();

            SerializedObject soUI = new SerializedObject(fireUI);
            soUI.FindProperty("_fireManager").objectReferenceValue = fireManager;
            soUI.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[AR Setup] Saved Phase 4 scene to {ScenePath}");
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
