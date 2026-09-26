using UnityEngine;
using ARMiningSimulator.Fire;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// UI Controller for Phase 4 Debug Testing.
    /// Provides a collapsible, unobtrusive tactical drawer that will never overlap
    /// or interfere with gameplay HUD, modals, or touch controls.
    /// </summary>
    public class FireTestingUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private FireManager _fireManager;

        [Header("Debug Settings")]
        [SerializeField] private bool _showDebugPanel = false;

        private void Awake()
        {
            if (_fireManager == null)
                _fireManager = FindFirstObjectByType<FireManager>();
        }

        private void OnGUI()
        {
            // Do not draw debug controls over active modals
            if (TacticalUITheme.IsAnyModalOpen()) return;

            // Small collapsible toggle button in safe corner (Top-Right under clock)
            float toggleW = 90f;
            float toggleH = 26f;
            float toggleX = Screen.width - toggleW - 16f;
            float toggleY = 78f;

            Rect toggleRect = new Rect(toggleX, toggleY, toggleW, toggleH);
            Color toggleAccent = _showDebugPanel ? TacticalUITheme.Amber : TacticalUITheme.CyanDim;

            var toggleStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (TacticalUITheme.DrawTacticalButton(toggleRect, _showDebugPanel ? "⚙️ HIDE DEBUG" : "⚙️ DEBUG", toggleAccent, toggleStyle))
            {
                _showDebugPanel = !_showDebugPanel;
            }

            if (!_showDebugPanel) return;

            // Collapsible Tactical Floating Panel
            float panelW = 280f;
            float panelH = 230f;
            float panelX = Screen.width - panelW - 16f;
            float panelY = toggleY + toggleH + 6f;
            Rect panelRect = new Rect(panelX, panelY, panelW, panelH);

            TacticalUITheme.DrawTechCard(panelRect, TacticalUITheme.CardBgSolid, TacticalUITheme.Cyan, TacticalUITheme.Cyan, 3f);

            GUILayout.BeginArea(new Rect(panelX + 12, panelY + 12, panelW - 24, panelH - 24));

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = TacticalUITheme.Cyan }
            };
            GUILayout.Label("🛠️ DRILL DEBUG OVERRIDE", titleStyle);

            FireSeverity severity = _fireManager != null ? _fireManager.GetHighestSeverity() : FireSeverity.Small;
            float timer = _fireManager != null ? _fireManager.ScenarioTimer : 0f;
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.85f, 0.90f, 0.95f) }
            };
            GUILayout.Label($"State: {severity}  |  Time: {timer:F1}s", subStyle);
            GUILayout.Space(6);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (GUILayout.Button("🔥 RE-IGNITE FIRE", btnStyle, GUILayout.Height(32)))
            {
                if (_fireManager != null) _fireManager.StartScenarioFires();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Small (60s)", btnStyle, GUILayout.Height(30)))
            {
                if (_fireManager != null) _fireManager.SetAllFiresSeverity(FireSeverity.Small);
            }
            if (GUILayout.Button("Large (Inferno)", btnStyle, GUILayout.Height(30)))
            {
                if (_fireManager != null) _fireManager.SetAllFiresSeverity(FireSeverity.Large);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            GUI.backgroundColor = TacticalUITheme.Emerald;
            if (GUILayout.Button("🧯 EXTINGUISH ALL FIRES", btnStyle, GUILayout.Height(34)))
            {
                if (_fireManager != null) _fireManager.ExtinguishAllFires();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndArea();
        }
    }
}
