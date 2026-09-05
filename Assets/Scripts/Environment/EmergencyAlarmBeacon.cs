using UnityEngine;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Mounted on top of the Emergency Exit door frame.
    /// When Decision Stage 1 (Alarm & Surface Notification) is activated,
    /// triggers a flashing red warning strobe and dynamic revolving light
    /// to signal general underground evacuation.
    /// </summary>
    public class EmergencyAlarmBeacon : MonoBehaviour
    {
        public static EmergencyAlarmBeacon Instance { get; private set; }

        private Light _beaconLight;
        private GameObject _beaconCap;
        private bool _isAlarmActive = false;
        private float _rotationSpeed = 420f;

        public bool IsAlarmActive => _isAlarmActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildBeaconVisuals();
        }

        private void BuildBeaconVisuals()
        {
            // Mount cylinder
            GameObject beaconMount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beaconMount.name = "AlarmBeacon_Mount";
            beaconMount.transform.SetParent(transform, false);
            beaconMount.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            beaconMount.transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);

            var mountCol = beaconMount.GetComponent<Collider>();
            if (mountCol != null) Destroy(mountCol);

            // Red lens dome
            _beaconCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _beaconCap.name = "AlarmBeacon_Lens";
            _beaconCap.transform.SetParent(beaconMount.transform, false);
            _beaconCap.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            _beaconCap.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);

            var capCol = _beaconCap.GetComponent<Collider>();
            if (capCol != null) Destroy(capCol);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                Material redMat = new Material(shader)
                {
                    color = new Color(0.95f, 0.1f, 0.1f)
                };
                redMat.EnableKeyword("_EMISSION");
                redMat.SetColor("_EmissionColor", new Color(0.6f, 0.05f, 0.05f));
                _beaconCap.GetComponent<Renderer>().sharedMaterial = redMat;
            }

            // Strobe Point Light
            GameObject lightGo = new GameObject("Alarm_PointLight");
            lightGo.transform.SetParent(beaconMount.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            _beaconLight = lightGo.AddComponent<Light>();
            _beaconLight.type = LightType.Point;
            _beaconLight.color = new Color(1.0f, 0.12f, 0.12f);
            _beaconLight.range = 8.0f;
            _beaconLight.intensity = 0f; // Starts silent/off
        }

        public void ActivateAlarm()
        {
            _isAlarmActive = true;
            if (_beaconLight != null) _beaconLight.intensity = 3.5f;
            Debug.Log("[EmergencyAlarmBeacon] 🚨 MINE EVACUATION ALARM ACTIVATED! Strobe is flashing.");
        }

        public void DeactivateAlarm()
        {
            _isAlarmActive = false;
            if (_beaconLight != null) _beaconLight.intensity = 0f;
        }

        private void Update()
        {
            if (!_isAlarmActive) return;

            // Revolving beacon cap
            if (_beaconCap != null)
            {
                _beaconCap.transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.Self);
            }

            // Pulsing strobe light
            if (_beaconLight != null)
            {
                _beaconLight.intensity = 1.5f + Mathf.PingPong(Time.time * 6.5f, 3.5f);
            }
        }
    }
}
