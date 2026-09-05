using UnityEngine;
using UnityEngine.UI;

namespace ARMiningSimulator.AR
{
    /// <summary>
    /// Editor-only webcam background provider.
    /// When testing in Unity Editor Play Mode on a PC/laptop, AR Foundation cannot run ARCore
    /// (which requires an Android device), leaving the camera background solid black.
    /// 
    /// This component automatically detects your laptop's built-in webcam or USB webcam,
    /// and renders the live video feed behind all 3D mining machinery, equipment, and fire hazards.
    /// 
    /// On Android / iOS builds, this component is completely inactive so ARFoundation (ARCameraBackground)
    /// has 100% control of the mobile device camera.
    /// </summary>
    public class EditorWebcamBackground : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Editor Webcam Settings")]
        [SerializeField] private bool _enableWebcamInEditor = true;
        [SerializeField] private int _requestedWidth = 1280;
        [SerializeField] private int _requestedHeight = 720;
        [SerializeField] private int _requestedFPS = 30;

        private WebCamTexture _webCamTexture;
        private WebCamDevice[] _devices;
        private int _selectedDeviceIndex = 0;
        private Canvas _backgroundCanvas;
        private RawImage _backgroundRawImage;
        private AspectRatioFitter _aspectRatioFitter;
        private bool _isWebcamActive = false;
        private string _statusMessage = "Initializing webcam...";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAttachToMainCameraOnPlay()
        {
            // Auto-attach to Main Camera if not already present
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<EditorWebcamBackground>() == null)
            {
                cam.gameObject.AddComponent<EditorWebcamBackground>();
                Debug.Log("[EditorWebcamBackground] Automatically attached EditorWebcamBackground to Main Camera.");
            }
        }

        private void Start()
        {
            if (!_enableWebcamInEditor) return;
            InitializeWebcam();
        }

        public void InitializeWebcam()
        {
            _devices = WebCamTexture.devices;
            if (_devices == null || _devices.Length == 0)
            {
                _statusMessage = "No PC webcam detected";
                Debug.LogWarning("[EditorWebcamBackground] No webcams detected on this PC. Camera background will remain solid.");
                return;
            }

            StartSelectedWebcam();
        }

        private void StartSelectedWebcam()
        {
            StopWebcam();

            if (_devices == null || _devices.Length == 0) return;

            if (_selectedDeviceIndex < 0 || _selectedDeviceIndex >= _devices.Length)
                _selectedDeviceIndex = 0;

            WebCamDevice device = _devices[_selectedDeviceIndex];
            _statusMessage = $"Active: {device.name}";
            Debug.Log($"[EditorWebcamBackground] Starting PC webcam: {device.name}");

            _webCamTexture = new WebCamTexture(device.name, _requestedWidth, _requestedHeight, _requestedFPS);
            _webCamTexture.Play();

            EnsureBackgroundUI();

            if (_backgroundRawImage != null)
            {
                _backgroundRawImage.texture = _webCamTexture;
            }

            _isWebcamActive = true;
        }

        private void StopWebcam()
        {
            if (_webCamTexture != null)
            {
                if (_webCamTexture.isPlaying)
                {
                    _webCamTexture.Stop();
                }
                Destroy(_webCamTexture);
                _webCamTexture = null;
            }
            _isWebcamActive = false;
        }

        private void EnsureBackgroundUI()
        {
            if (_backgroundCanvas != null) return;

            Camera cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            // Create Canvas behind all 3D scene elements
            GameObject canvasGo = new GameObject("Editor_Webcam_Canvas");
            canvasGo.transform.SetParent(transform, false);

            _backgroundCanvas = canvasGo.AddComponent<Canvas>();
            _backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            _backgroundCanvas.worldCamera = cam;
            // Place near the far clip plane so all 3D mining machinery renders in front
            _backgroundCanvas.planeDistance = cam != null ? Mathf.Max(cam.farClipPlane - 1.0f, 10f) : 90f;
            _backgroundCanvas.sortingOrder = -1000; // Far behind default layers

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // RawImage for rendering webcam texture
            GameObject imageGo = new GameObject("Webcam_Feed");
            imageGo.transform.SetParent(canvasGo.transform, false);

            _backgroundRawImage = imageGo.AddComponent<RawImage>();
            _backgroundRawImage.color = Color.white;

            RectTransform rt = _backgroundRawImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _aspectRatioFitter = imageGo.AddComponent<AspectRatioFitter>();
            _aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _aspectRatioFitter.aspectRatio = 16f / 9f;
        }

        private void Update()
        {
            if (!_isWebcamActive || _webCamTexture == null) return;

            if (_webCamTexture.isPlaying)
            {
                // Update aspect ratio once valid dimensions are available
                if (_aspectRatioFitter != null && _webCamTexture.width > 16 && _webCamTexture.height > 16)
                {
                    _aspectRatioFitter.aspectRatio = (float)_webCamTexture.width / _webCamTexture.height;
                }

                // Adjust for camera orientation / mirroring
                if (_backgroundRawImage != null)
                {
                    float angle = -_webCamTexture.videoRotationAngle;
                    _backgroundRawImage.rectTransform.localEulerAngles = new Vector3(0, 0, angle);

                    if (_webCamTexture.videoVerticallyMirrored)
                    {
                        _backgroundRawImage.uvRect = new Rect(0, 1, 1, -1);
                    }
                    else
                    {
                        _backgroundRawImage.uvRect = new Rect(0, 0, 1, 1);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_devices == null || _devices.Length == 0) return;

            // Small badge in bottom-left showing PC webcam status and toggle
            GUILayout.BeginArea(new Rect(10, Screen.height - 35, 450, 30));
            GUILayout.BeginHorizontal();

            GUI.color = _isWebcamActive ? new Color(0.2f, 1f, 0.2f, 0.9f) : new Color(1f, 0.5f, 0.2f, 0.9f);
            string statusIcon = _isWebcamActive ? "📷 Webcam Active:" : "📷 Webcam Off:";
            GUILayout.Label($"{statusIcon} {_devices[_selectedDeviceIndex].name}", GUILayout.Width(280));
            GUI.color = Color.white;

            if (GUILayout.Button(_isWebcamActive ? "Turn Off" : "Turn On", GUILayout.Width(70), GUILayout.Height(24)))
            {
                if (_isWebcamActive) StopWebcam();
                else StartSelectedWebcam();
            }

            if (_devices.Length > 1)
            {
                if (GUILayout.Button("Switch Cam", GUILayout.Width(80), GUILayout.Height(24)))
                {
                    _selectedDeviceIndex = (_selectedDeviceIndex + 1) % _devices.Length;
                    StartSelectedWebcam();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void OnDisable()
        {
            StopWebcam();
        }

        private void OnDestroy()
        {
            StopWebcam();
            if (_backgroundCanvas != null)
            {
                Destroy(_backgroundCanvas.gameObject);
            }
        }
#else
        // Mobile runtime (Android ARCore): Disable self immediately
        private void Awake()
        {
            Destroy(this);
        }
#endif
    }
}
