#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// Automation and configuration utility for Phase 1.
    /// Accessible via the Unity menu bar: "AR Training"
    /// </summary>
    public static class ARPhase1SetupUtility
    {
        private const string ScenePath = "Assets/Scenes/ARPhase1_TestScene.unity";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string PlanePrefabPath = "Assets/Prefabs/ARPlane_Prefab.prefab";
        private const string MarkerPrefabPath = "Assets/Prefabs/ARTestBeacon_Prefab.prefab";
        private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";

        [MenuItem("AR Training/1. Full One-Click Setup (Phase 1)", priority = 10)]
        public static void RunFullSetup()
        {
            EnsureDirectories();
            ConfigureURPRenderer();
            EnableARCoreLoader();
            GameObject planePrefab = CreateOrUpdatePlanePrefab();
            GameObject markerPrefab = CreateOrUpdateMarkerPrefab();
            CreatePhase1Scene(planePrefab, markerPrefab);
            AddSceneToBuildSettings();

            EditorUtility.DisplayDialog(
                "Phase 1 Setup Complete",
                "AR Phase 1 scene and assets have been generated successfully!\n\n" +
                "• Scene: Assets/Scenes/ARPhase1_TestScene.unity\n" +
                "• URP AR Background Renderer Feature: Added\n" +
                "• AR Plane & Marker Prefabs: Created in Assets/Prefabs/\n" +
                "• Scene in Build Settings: Configured (Index 0)\n\n" +
                "Next steps:\n" +
                "1. Verify in Edit > Project Settings > XR Plug-in Management that 'Google ARCore' is checked under Android.\n" +
                "2. Open ARPhase1_TestScene and test!",
                "OK"
            );
        }

        [MenuItem("AR Training/2. Add AR Background Feature to URP Renderer", priority = 20)]
        public static void ConfigureURPRenderer()
        {
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(MobileRendererPath);
            if (rendererData == null)
            {
                Debug.LogWarning($"[AR Setup] Could not find UniversalRendererData at {MobileRendererPath}");
                return;
            }

            // Check if ARBackgroundRendererFeature is already added
            bool hasARBackground = false;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature != null && feature.GetType().Name == "ARBackgroundRendererFeature")
                {
                    hasARBackground = true;
                    break;
                }
            }

            if (!hasARBackground)
            {
                var arBackgroundFeature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                arBackgroundFeature.name = "ARBackgroundRendererFeature";
                AssetDatabase.AddObjectToAsset(arBackgroundFeature, rendererData);
                rendererData.rendererFeatures.Add(arBackgroundFeature);
                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssets();
                Debug.Log("[AR Setup] Added ARBackgroundRendererFeature to Mobile_Renderer.asset");
            }
            else
            {
                Debug.Log("[AR Setup] Mobile_Renderer.asset already has ARBackgroundRendererFeature.");
            }
        }

        [MenuItem("AR Training/3. Enable Google ARCore in XR Management", priority = 30)]
        public static void EnableARCoreLoader()
        {
            try
            {
                Type metadataStoreType = Type.GetType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore, Unity.XR.Management.Editor");
                Type generalSettingsPerTargetType = Type.GetType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget, Unity.XR.Management.Editor");

                if (metadataStoreType != null && generalSettingsPerTargetType != null)
                {
                    MethodInfo methodGetSettings = generalSettingsPerTargetType.GetMethod("XRGeneralSettingsForBuildTarget", BindingFlags.Public | BindingFlags.Static);
                    if (methodGetSettings != null)
                    {
                        var xrSettings = methodGetSettings.Invoke(null, new object[] { BuildTargetGroup.Android });
                        if (xrSettings != null)
                        {
                            PropertyInfo managerProp = xrSettings.GetType().GetProperty("Manager");
                            object manager = managerProp?.GetValue(xrSettings);
                            if (manager != null)
                            {
                                MethodInfo assignMethod = metadataStoreType.GetMethod("AssignLoader", BindingFlags.Public | BindingFlags.Static);
                                if (assignMethod != null)
                                {
                                    assignMethod.Invoke(null, new object[] { manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android });
                                    if (xrSettings is UnityEngine.Object uObj)
                                    {
                                        EditorUtility.SetDirty(uObj);
                                        AssetDatabase.SaveAssets();
                                    }
                                    Debug.Log("[AR Setup] Enabled Google ARCore loader for Android build target.");
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AR Setup] Automatic ARCore loader assignment notification: {ex.Message}. You can manually check 'Google ARCore' in Edit > Project Settings > XR Plug-in Management > Android.");
            }
        }

        [MenuItem("AR Training/4. Generate Phase 1 Test Scene & Prefabs", priority = 40)]
        public static void GenerateSceneAndPrefabs()
        {
            EnsureDirectories();
            GameObject planePrefab = CreateOrUpdatePlanePrefab();
            GameObject markerPrefab = CreateOrUpdateMarkerPrefab();
            CreatePhase1Scene(planePrefab, markerPrefab);
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

        private static GameObject CreateOrUpdatePlanePrefab()
        {
            EnsureDirectories();

            GameObject planeGo = new GameObject("ARPlane_Visualizer");
            planeGo.AddComponent<ARPlane>();
            planeGo.AddComponent<ARPlaneMeshVisualizer>();
            planeGo.AddComponent<MeshFilter>();
            MeshRenderer mr = planeGo.AddComponent<MeshRenderer>();
            LineRenderer lr = planeGo.AddComponent<LineRenderer>();
            planeGo.AddComponent<ARMiningSimulator.AR.ARPlaneVisualizerHelper>();

            // Setup default transparent green material
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
            UnityEngine.Object.DestroyImmediate(planeGo);
            Debug.Log($"[AR Setup] Created AR Plane prefab at {PlanePrefabPath}");
            return prefab;
        }

        private static GameObject CreateOrUpdateMarkerPrefab()
        {
            EnsureDirectories();

            GameObject root = new GameObject("AR_TestBeacon_Marker");

            // Base disk
            GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseDisk.name = "BaseDisk";
            baseDisk.transform.SetParent(root.transform, false);
            baseDisk.transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);
            baseDisk.transform.localPosition = new Vector3(0f, 0.01f, 0f);

            // Center pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.08f, 0.25f, 0.08f);
            pole.transform.localPosition = new Vector3(0f, 0.25f, 0f);

            // Warning Sphere Top
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = "TopBeacon";
            top.transform.SetParent(root.transform, false);
            top.transform.localScale = Vector3.one * 0.15f;
            top.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            // Safety Orange material
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material orangeMat = new Material(shader);
                orangeMat.name = "SafetyMarker_Mat";
                orangeMat.color = new Color(1.0f, 0.45f, 0.0f);
                
                baseDisk.GetComponent<Renderer>().sharedMaterial = orangeMat;
                pole.GetComponent<Renderer>().sharedMaterial = orangeMat;
                top.GetComponent<Renderer>().sharedMaterial = orangeMat;
            }

            // Remove colliders so raycasts hit the plane directly
            foreach (var c in root.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(c);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MarkerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[AR Setup] Created Marker prefab at {MarkerPrefabPath}");
            return prefab;
        }

        private static void CreatePhase1Scene(GameObject planePrefab, GameObject markerPrefab)
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
            ARSession arSession = arSessionGo.AddComponent<ARSession>();
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

            // 4. Camera Offset and Main Camera
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
            cameraGo.AddComponent<ARMiningSimulator.AR.EditorWebcamBackground>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            cameraGo.AddComponent<AudioListener>();

            // Link XR Origin camera references
            xrOrigin.CameraFloorOffsetObject = cameraOffsetGo;
            xrOrigin.Camera = cam;

            // 5. AR Phase 1 Manager Controller
            var phase1Manager = xrOriginGo.AddComponent<ARMiningSimulator.AR.ARPhase1Manager>();
            
            // Connect serialized fields using SerializedObject
            SerializedObject so = new SerializedObject(phase1Manager);
            so.FindProperty("_arSession").objectReferenceValue = arSession;
            so.FindProperty("_planeManager").objectReferenceValue = planeManager;
            so.FindProperty("_raycastManager").objectReferenceValue = raycastManager;
            so.FindProperty("_arCamera").objectReferenceValue = cam;
            if (markerPrefab != null)
            {
                so.FindProperty("_placementMarkerPrefab").objectReferenceValue = markerPrefab;
            }
            so.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(newScene, ScenePath);
            Debug.Log($"[AR Setup] Saved Phase 1 scene to {ScenePath}");
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
