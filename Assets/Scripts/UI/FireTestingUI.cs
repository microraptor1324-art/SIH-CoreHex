using UnityEngine;
using ARMiningSimulator.Fire;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// UI Controller for Phase 4: Displays fire severity, spread timer,
    /// and interactive debug controls for testing fire states.
    /// </summary>
    public class FireTestingUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private FireManager _fireManager;

        [Header("Optional uGUI References")]
        [SerializeField] private UnityEngine.UI.Text _fireStatusText;
        [SerializeField] private UnityEngine.UI.Text _severityText;
        [SerializeField] private UnityEngine.UI.Text _timerText;
        [SerializeField] private UnityEngine.UI.Button _igniteButton;
        [SerializeField] private UnityEngine.UI.Button _smallButton;
        [SerializeField] private UnityEngine.UI.Button _largeButton;
        [SerializeField] private UnityEngine.UI.Button _extinguishButton;

        private void Awake()
        {
            if (_fireManager == null)
                _fireManager = FindFirstObjectByType<FireManager>();
        }

        private void Start()
        {
            if (_igniteButton != null) _igniteButton.onClick.AddListener(() => _fireManager.StartScenarioFires());
            if (_smallButton != null) _smallButton.onClick.AddListener(() => _fireManager.SetAllFiresSeverity(FireSeverity.Small));
            if (_largeButton != null) _largeButton.onClick.AddListener(() => _fireManager.SetAllFiresSeverity(FireSeverity.Large));
            if (_extinguishButton != null) _extinguishButton.onClick.AddListener(() => _fireManager.ExtinguishAllFires());
        }

        private void OnGUI()
        {
            // Only draw OnGUI if uGUI is not bound
            if (_fireStatusText != null && _severityText != null) return;

            int pad = 24;
            int screenW = Screen.width;
            int screenH = Screen.height;
            int cardW = Mathf.Min(screenW - pad * 2, 700);

            // 1. TOP FIRE STATUS CARD
            int topH = 220;
            GUI.Box(new Rect(pad, pad, cardW, topH), GUIContent.none);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.35f, 0.1f) } // Fire Orange
            };

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle warningStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };

            GUILayout.BeginArea(new Rect(pad + 16, pad + 12, cardW - 32, topH - 24));
            GUILayout.Label("🔥 FIRE HAZARD SYSTEM", titleStyle);
            GUILayout.Space(4);

            int activeCount = _fireManager != null ? _fireManager.ActiveFires.Count : 0;
            FireSeverity severity = _fireManager != null ? _fireManager.GetHighestSeverity() : FireSeverity.Small;
            float timer = _fireManager != null ? _fireManager.ScenarioTimer : 0f;

            Color severityColor = severity switch
            {
                FireSeverity.Small => new Color(0.98f, 0.75f, 0.1f), // Gold
                FireSeverity.Medium => new Color(1.0f, 0.45f, 0.05f), // Orange
                FireSeverity.Large => new Color(1.0f, 0.18f, 0.22f), // Red
                _ => Color.white
            };

            GUIStyle sevStyle = new GUIStyle(labelStyle) { normal = { textColor = severityColor } };

            GUILayout.Label($"Active Fires: {activeCount} Hazard(s)", labelStyle);
            GUILayout.Label($"Fire Severity: {severity.ToString().ToUpper()} FIRE", sevStyle);
            GUILayout.Label($"Burning Time: {timer:F1}s (0-10s Small | 10-20s Med | 20s+ Large)", warningStyle);

            GUILayout.Space(4);
            string advisory = severity switch
            {
                FireSeverity.Small => "Condition: Localized fire. Low damage rate. Can be fought.",
                FireSeverity.Medium => "Condition: Fire spreading! Smoke increasing. Evacuation advised.",
                FireSeverity.Large => "DANGER: Fire out of control! High heat! DO NOT FIGHT.",
                _ => ""
            };
            GUILayout.Label(advisory, warningStyle);
            GUILayout.EndArea();

            // 2. BOTTOM DEBUG BAR (Section 23)
            int btnH = 55;
            int bottomH = btnH * 2 + 15;
            int bottomY = screenH - bottomH - pad - 10;

            GUILayout.BeginArea(new Rect(pad, bottomY, cardW, bottomH));

            // Row 1: Severity Controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔥 Ignite New Fires", GUILayout.Height(btnH)))
            {
                if (_fireManager != null) _fireManager.StartScenarioFires();
            }

            if (GUILayout.Button("Force Small", GUILayout.Height(btnH), GUILayout.Width(cardW * 0.28f)))
            {
                if (_fireManager != null) _fireManager.SetAllFiresSeverity(FireSeverity.Small);
            }

            if (GUILayout.Button("Force Large", GUILayout.Height(btnH), GUILayout.Width(cardW * 0.28f)))
            {
                if (_fireManager != null) _fireManager.SetAllFiresSeverity(FireSeverity.Large);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Row 2: Extinguish
            Color orig = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.45f);

            if (GUILayout.Button("🧯 Extinguish All Fires", GUILayout.Height(btnH)))
            {
                if (_fireManager != null) _fireManager.ExtinguishAllFires();
            }

            GUI.backgroundColor = orig;
            GUILayout.EndArea();
        }
    }
}
