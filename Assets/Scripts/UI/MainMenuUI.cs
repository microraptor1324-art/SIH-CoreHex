using System;
using UnityEngine;
using ARMiningSimulator.Progression;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Professional IMGUI Scenario & Level Selection UI:
    /// - Displays Level 1, Level 2, and Level 3 cards with lock/unlock status, target objectives, and earned grades.
    /// - Allows trainees and instructors to select training scenarios on demand.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private bool _isOpen = false;

        // Textures
        private Texture2D _panelTex;
        private Texture2D _cardTex;
        private Texture2D _cardHoverTex;
        private Texture2D _lockedCardTex;
        private Texture2D _btnTex;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _cardHoverStyle;
        private GUIStyle _lockedCardStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _cardDescStyle;
        private GUIStyle _gradeStyle;
        private GUIStyle _btnStyle;

        private bool _stylesInitialized = false;

        public bool IsOpen => _isOpen;

        public void ToggleMenu()
        {
            _isOpen = !_isOpen;
        }

        public void OpenMenu()
        {
            _isOpen = true;
        }

        public void CloseMenu()
        {
            _isOpen = false;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _panelTex = MakeColorTex(new Color(0.06f, 0.08f, 0.12f, 0.96f));
            _cardTex = MakeColorTex(new Color(0.12f, 0.16f, 0.22f, 0.95f));
            _cardHoverTex = MakeColorTex(new Color(0.18f, 0.26f, 0.38f, 1.0f));
            _lockedCardTex = MakeColorTex(new Color(0.10f, 0.10f, 0.12f, 0.85f));
            _btnTex = MakeColorTex(new Color(0.20f, 0.28f, 0.38f, 0.95f));

            _panelStyle = new GUIStyle
            {
                normal = { background = _panelTex },
                padding = new RectOffset(20, 20, 16, 16)
            };

            _titleStyle = new GUIStyle
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.85f, 0.30f) }
            };

            _cardStyle = new GUIStyle
            {
                normal = { background = _cardTex },
                padding = new RectOffset(16, 16, 12, 12)
            };

            _cardHoverStyle = new GUIStyle
            {
                normal = { background = _cardHoverTex },
                padding = new RectOffset(16, 16, 12, 12)
            };

            _lockedCardStyle = new GUIStyle
            {
                normal = { background = _lockedCardTex },
                padding = new RectOffset(16, 16, 12, 12)
            };

            _cardTitleStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _cardDescStyle = new GUIStyle
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.80f, 0.85f, 0.90f) }
            };

            _gradeStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.3f, 0.9f, 0.5f) }
            };

            _btnStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = _btnTex }
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeColorTex(Color col)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, col);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();

            DrawTopBarButton();

            if (_isOpen)
            {
                DrawLevelSelectorModal();
            }
        }

        private void DrawTopBarButton()
        {
            float btnW = 150f;
            float btnH = 34f;
            Rect rect = new Rect(16f, 16f, btnW, btnH);

            string label = _isOpen ? "✕ Close Menu" : "📑 Scenarios (Levels)";
            if (GUI.Button(rect, label, _btnStyle))
            {
                ToggleMenu();
            }
        }

        private void DrawLevelSelectorModal()
        {
            float w = Mathf.Min(680f, Screen.width * 0.94f);
            float h = Mathf.Min(620f, Screen.height * 0.90f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(x + 18, y + 16, w - 36, h - 32));

            GUILayout.Label("AR MINING FIRE SAFETY — SCENARIO SELECTOR", _titleStyle);
            GUILayout.Label("Select an MSHA Certified Underground Emergency Drill", new GUIStyle { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.7f, 0.8f, 0.9f) } });
            GUILayout.Space(14);

            var mgr = GameProgressionManager.Instance;
            if (mgr != null)
            {
                for (int i = 1; i <= 3; i++)
                {
                    var cfg = mgr.GetConfigByIndex(i);
                    bool isUnlocked = mgr.IsLevelUnlocked(i);
                    bool isCurrent = (mgr.CurrentLevelIndex == i);
                    string grade = mgr.GetLevelGrade(i);

                    DrawLevelCard(cfg, isUnlocked, isCurrent, grade, mgr);
                    GUILayout.Space(10);
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("🔄 Reset All Progress", _btnStyle, GUILayout.Width(170), GUILayout.Height(36)))
            {
                if (mgr != null) mgr.ResetAllProgression();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Resume Simulation", _btnStyle, GUILayout.Width(170), GUILayout.Height(36)))
            {
                CloseMenu();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawLevelCard(LevelConfig cfg, bool isUnlocked, bool isCurrent, string grade, GameProgressionManager mgr)
        {
            GUIStyle cardStyle = isUnlocked ? _cardStyle : _lockedCardStyle;

            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();

            string statusPrefix = isUnlocked ? (isCurrent ? "▶ [ACTIVE] " : "✓ ") : "🔒 [LOCKED] ";
            Color titleColor = isUnlocked ? (isCurrent ? new Color(1.0f, 0.9f, 0.3f) : Color.white) : new Color(0.6f, 0.6f, 0.6f);

            var tStyle = new GUIStyle(_cardTitleStyle) { normal = { textColor = titleColor } };
            GUILayout.Label(statusPrefix + cfg.levelTitle, tStyle);

            if (isUnlocked && grade != "—")
            {
                GUILayout.Label($"Best: {grade}", _gradeStyle);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label($"{cfg.subtitle} — {cfg.environmentDescription}", _cardDescStyle);
            GUILayout.Label($"Hazard: {cfg.primaryHazardDescription}", new GUIStyle(_cardDescStyle) { normal = { textColor = new Color(1.0f, 0.55f, 0.3f) } });

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Target Score: {cfg.targetScoreToPass}+ PTS | Evac Time: {cfg.evacuationTimeLimit}s", new GUIStyle(_cardDescStyle) { fontStyle = FontStyle.Italic });

            GUILayout.FlexibleSpace();

            if (isUnlocked)
            {
                string btnText = isCurrent ? "REPLAY THIS LEVEL" : "SELECT & START";
                if (GUILayout.Button(btnText, _btnStyle, GUILayout.Width(150), GUILayout.Height(30)))
                {
                    mgr.LoadLevel(cfg.levelIndex);
                    CloseMenu();
                    UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                }
            }
            else
            {
                GUILayout.Label("Pass Previous Level to Unlock", new GUIStyle(_cardDescStyle) { normal = { textColor = new Color(0.7f, 0.4f, 0.4f) } });
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
