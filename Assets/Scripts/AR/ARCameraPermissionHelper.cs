using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Ensures that Android Camera Permissions are properly requested and granted at launch.
    /// Without this, Android 11+ devices block camera access, resulting in a black background.
    /// </summary>
    public class ARCameraPermissionHelper : MonoBehaviour
    {
        private static bool _permissionRequested = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitializeOnAppLaunch()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            GameObject helperGo = new GameObject("AR_Camera_Permission_Helper");
            DontDestroyOnLoad(helperGo);
            helperGo.AddComponent<ARCameraPermissionHelper>();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void Awake()
        {
            RequestCameraPermission();
        }

        public static void RequestCameraPermission()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("[ARCameraPermissionHelper] Camera permission not yet authorized. Prompting user dialog...");
                Permission.RequestUserPermission(Permission.Camera);
                _permissionRequested = true;
            }
            else
            {
                Debug.Log("[ARCameraPermissionHelper] Camera permission already granted!");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _permissionRequested)
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Debug.Log("[ARCameraPermissionHelper] User granted camera permission! AR Foundation can now access camera.");
                }
            }
        }

        private void OnGUI()
        {
            // If permission was denied or not granted, display a clear, friendly recovery button on screen
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                float w = Mathf.Min(480f, Screen.width * 0.90f);
                float h = 160f;
                float x = (Screen.width - w) * 0.5f;
                float y = (Screen.height - h) * 0.5f;

                GUI.Box(new Rect(x, y, w, h), GUIContent.none);

                GUILayout.BeginArea(new Rect(x + 12, y + 12, w - 24, h - 24));
                GUILayout.Label("📷 CAMERA PERMISSION REQUIRED", new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.yellow } });
                GUILayout.Space(8);
                GUILayout.Label("The AR Mining Simulator requires camera access to scan your real room and display the virtual mine environment.", new GUIStyle { fontSize = 12, wordWrap = true, normal = { textColor = Color.white } });
                GUILayout.Space(12);

                if (GUILayout.Button("GRANT CAMERA PERMISSION", GUILayout.Height(40)))
                {
                    Permission.RequestUserPermission(Permission.Camera);
                }

                GUILayout.EndArea();
            }
        }
#endif
    }
}
