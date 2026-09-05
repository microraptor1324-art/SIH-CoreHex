using UnityEngine;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// UI Controller for Phase 3: Displays environment statistics, equipment counts,
    /// Emergency Exit distance, and interactive layout regeneration controls.
    /// </summary>
    public class MiningEnvironmentUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private MiningEnvironmentGenerator _generator;
        [SerializeField] private Camera _arCamera;

        [Header("Optional uGUI References")]
        [SerializeField] private UnityEngine.UI.Text _infoText;
        [SerializeField] private UnityEngine.UI.Text _equipmentText;
        [SerializeField] private UnityEngine.UI.Text _exitDistanceText;
        [SerializeField] private UnityEngine.UI.Button _regenerateButton;

        private void Awake()
        {
            if (_generator == null)
                _generator = FindFirstObjectByType<MiningEnvironmentGenerator>();
            if (_arCamera == null)
                _arCamera = Camera.main;
        }

        private void Start()
        {
            if (_regenerateButton != null)
                _regenerateButton.onClick.AddListener(OnRegenerateClicked);
        }

        public void OnRegenerateClicked()
        {
            if (_generator != null)
            {
                _generator.Regenerate();
            }
        }

        private void OnGUI()
        {
            // Only draw OnGUI if uGUI is not bound
            if (_infoText != null && _equipmentText != null) return;

            int pad = 24;
            int screenW = Screen.width;
            int screenH = Screen.height;
            int cardW = Mathf.Min(screenW - pad * 2, 700);

            // 1. TOP STATS CARD
            int topH = 210;
            GUI.Box(new Rect(pad, pad, cardW, topH), GUIContent.none);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.75f, 0.1f) } // Mining Gold
            };

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = new Color(0.85f, 0.9f, 0.95f) }
            };

            GUILayout.BeginArea(new Rect(pad + 16, pad + 12, cardW - 32, topH - 24));
            GUILayout.Label("VIRTUAL MINING ENVIRONMENT", headerStyle);
            GUILayout.Space(4);

            float len = _generator != null ? _generator.ActiveLength : 0f;
            float wid = _generator != null ? _generator.ActiveWidth : 0f;
            float area = _generator != null ? _generator.ActiveArea : 0f;

            GUILayout.Label($"Room Size: {len:F2} m x {wid:F2} m ({area:F2} m²)", labelStyle);

            int machines = _generator != null && _generator.Spawner != null ? _generator.Spawner.SpawnedMachines.Count : 5;
            int elec = _generator != null && _generator.Spawner != null ? _generator.Spawner.SpawnedElectrical.Count : 7;
            GUILayout.Label($"Equipment: {machines} Mining Machines | {elec} Electrical Tools", subStyle);

            // Emergency exit distance
            if (_generator != null && _arCamera != null)
            {
                float exitDist = Vector3.Distance(_arCamera.transform.position, _generator.EmergencyExitPosition);
                GUILayout.Label($"Emergency Exit: {exitDist:F2} m away 🚪", subStyle);
            }

            int seed = _generator != null && _generator.Spawner != null ? _generator.Spawner.ActiveSeed : 0;
            GUILayout.Label($"Scenario Seed: #{seed}", subStyle);

            GUILayout.EndArea();

            // 2. BOTTOM ACTIONS
            int btnH = 65;
            int bottomY = screenH - btnH - pad - 10;

            GUILayout.BeginArea(new Rect(pad, bottomY, cardW, btnH));
            Color orig = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.15f, 0.65f, 0.95f);

            if (GUILayout.Button("🔀 Regenerate Random Equipment Layout", GUILayout.Height(btnH)))
            {
                OnRegenerateClicked();
            }

            GUI.backgroundColor = orig;
            GUILayout.EndArea();
        }
    }
}
