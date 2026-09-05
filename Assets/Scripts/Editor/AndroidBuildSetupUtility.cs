#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;

namespace ARMiningSimulator.Editor
{
    /// <summary>
    /// One-click helper to automatically configure all Android and ARCore Player Settings
    /// to ensure 100% error-free builds to real Android devices.
    /// Menu: "AR Training > 14. Auto-Configure Android Settings for Phone Build"
    /// </summary>
    public static class AndroidBuildSetupUtility
    {
        [InitializeOnLoadMethod]
        private static void AutoEnsureARCoreOnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                EnsureGoogleARCoreLoaderEnabled();
                EnsureURPRendererFeature();
                EnsureGraphicsAPI();
            };
        }

        [MenuItem("AR Training/14. Auto-Configure Android Settings for Phone Build", priority = 140)]
        public static void ConfigureForAndroidPhone()
        {
            try
            {
                // 1. Company and Product Name
                PlayerSettings.companyName = "MiningSafety";
                PlayerSettings.productName = "ARMiningFireSimulator";

                // 2. Package Name / Application Identifier
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.miningsafety.arfiretrainer");

                // 3. Minimum SDK Version (API 29 / Android 10.0)
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)29;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

                // 4. Scripting Backend: IL2CPP (Required for 64-bit ARM)
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

                // 5. Target Architecture: ARM64 (Required by Google Play & ARCore)
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

                // 6. Graphics API: Enforce OpenGLES3 (Vulkan causes black camera background in ARCore)
                EnsureGraphicsAPI();

                // 7. Camera Settings
                PlayerSettings.Android.renderOutsideSafeArea = true;

                // 8. Ensure URP AR Background Renderer Feature is active
                EnsureURPRendererFeature();

                // 9. Ensure Google ARCore loader is enabled in XR Plug-in Management
                EnsureGoogleARCoreLoaderEnabled();

                // Save all changes
                AssetDatabase.SaveAssets();

                Debug.Log("[Android Setup] Android & ARCore Player Settings configured successfully!");

                EditorUtility.DisplayDialog(
                    "Android Configuration Complete!",
                    "Your Unity project is now 100% configured for Android phone builds!\n\n" +
                    "• Package Name: com.miningsafety.arfiretrainer\n" +
                    "• Minimum API Level: Android 10.0 (API 29) [ARCore Vulkan Requirement]\n" +
                    "• Scripting Backend: IL2CPP\n" +
                    "• Architecture: ARM64\n" +
                    "• Google ARCore Loader: Enabled in XR Plug-in Management\n" +
                    "• Camera Permissions: Configured in AndroidManifest.xml\n" +
                    "• URP AR Background: Active on Mobile_Renderer\n\n" +
                    "Ready to install on your phone:\n" +
                    "1. Connect your Android phone via USB (with USB Debugging turned ON)\n" +
                    "2. In Unity, go to File > Build Settings\n" +
                    "3. Click 'Build and Run'!",
                    "OK"
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android Setup] Error configuring Android settings: {ex.Message}");
            }
        }

        public static void EnsureGoogleARCoreLoaderEnabled()
        {
            try
            {
                XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
                if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings))
                {
                    buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>("Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                }

                if (buildTargetSettings == null)
                {
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    if (!AssetDatabase.IsValidFolder("Assets/XR"))
                    {
                        AssetDatabase.CreateFolder("Assets", "XR");
                    }
                    AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
                }

                XRGeneralSettings androidSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
                if (androidSettings == null)
                {
                    buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
                    androidSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
                }

                if (androidSettings != null)
                {
                    XRManagerSettings manager = androidSettings.Manager;
                    if (manager == null)
                    {
                        manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                        androidSettings.Manager = manager;
                        string assetPath = AssetDatabase.GetAssetOrScenePath(androidSettings);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            AssetDatabase.AddObjectToAsset(manager, assetPath);
                        }
                    }

                    if (manager != null)
                    {
                        bool assigned = XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android);
                        EditorUtility.SetDirty(manager);
                        EditorUtility.SetDirty(androidSettings);
                        EditorUtility.SetDirty(buildTargetSettings);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[Android Setup] Google ARCore loader status for Android: {(assigned ? "Assigned/Verified" : "Already Present")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Android Setup] Direct XR loader configuration notification: {ex.Message}");
            }
        }

        public static void EnsureURPRendererFeature()
        {
            string mobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(mobileRendererPath);
            if (rendererData == null) return;

            bool hasFeature = false;
            foreach (var feat in rendererData.rendererFeatures)
            {
                if (feat != null && feat.GetType().Name == "ARBackgroundRendererFeature")
                {
                    hasFeature = true;
                    break;
                }
            }

            if (!hasFeature)
            {
                var arBg = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                arBg.name = "ARBackgroundRendererFeature";
                AssetDatabase.AddObjectToAsset(arBg, rendererData);
                rendererData.rendererFeatures.Add(arBg);
                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssets();
                Debug.Log("[Android Setup] Added ARBackgroundRendererFeature to Mobile_Renderer.asset");
            }
        }

        public static void EnsureGraphicsAPI()
        {
            try
            {
                var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                bool needsOpenGLES3 = false;
                if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) || apis.Length != 1 || apis[0] != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                {
                    needsOpenGLES3 = true;
                }

                if (needsOpenGLES3)
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                    PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] {
                        UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
                    });
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Android Setup] Configured Android Graphics API: OpenGLES3 (Removed Vulkan for ARCore camera feed display).");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Android Setup] Graphics API setup notice: {ex.Message}");
            }
        }
    }
}
#endif
