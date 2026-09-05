using System;
using UnityEngine;
using ARMiningSimulator.Investigation;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Professional IMGUI user interface for Phase 9:
    /// - Incident Investigation (Visual & Thermal clues, root cause selection, MSHA regulatory review).
    /// - Official MSHA Training Performance Scorecard (Itemized breakdown, Grade A/B/C/F, Next Level unlock).
    /// </summary>
    public class InvestigationAndScoreUI : MonoBehaviour
    {
        public static event Action OnNextLevelRequested;

        private IncidentReport _activeReport;
        private ScoreBreakdown _activeScorecard;
        private bool _isCorrectReview = false;
        private int _reviewPoints = 0;
        private string _reviewExplanation = "";

        // UI Textures
        private Texture2D _panelTex;
        private Texture2D _headerTex;
        private Texture2D _btnTex;
        private Texture2D _btnHoverTex;
        private Texture2D _correctTex;
        private Texture2D _incorrectTex;
        private Texture2D _goldTex;

        // Styles
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _clueStyle;
        private GUIStyle _thermalStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _explanationStyle;
        private GUIStyle _scoreRowStyle;
        private GUIStyle _totalScoreStyle;
        private GUIStyle _gradeBadgeStyle;

        private bool _stylesInitialized = false;
        private Vector2 _scrollPos = Vector2.zero;

        private void OnEnable()
        {
            InvestigationManager.OnInvestigationStarted += HandleInvestigationStarted;
            InvestigationManager.OnRootCauseEvaluated += HandleRootCauseEvaluated;
            InvestigationManager.OnScorecardReady += HandleScorecardReady;
        }

        private void OnDisable()
        {
            InvestigationManager.OnInvestigationStarted -= HandleInvestigationStarted;
            InvestigationManager.OnRootCauseEvaluated -= HandleRootCauseEvaluated;
            InvestigationManager.OnScorecardReady -= HandleScorecardReady;
        }

        private void HandleInvestigationStarted(IncidentReport report)
        {
            _activeReport = report;
        }

        private void HandleRootCauseEvaluated(bool isCorrect, int points, string explanation)
        {
            _isCorrectReview = isCorrect;
            _reviewPoints = points;
            _reviewExplanation = explanation;
        }

        private void HandleScorecardReady(ScoreBreakdown breakdown)
        {
            _activeScorecard = breakdown;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _panelTex = MakeColorTex(new Color(0.08f, 0.10f, 0.13f, 0.95f));
            _headerTex = MakeColorTex(new Color(0.14f, 0.18f, 0.24f, 0.98f));
            _btnTex = MakeColorTex(new Color(0.16f, 0.22f, 0.30f, 0.95f));
            _btnHoverTex = MakeColorTex(new Color(0.24f, 0.36f, 0.50f, 1.0f));
            _correctTex = MakeColorTex(new Color(0.12f, 0.40f, 0.20f, 0.95f));
            _incorrectTex = MakeColorTex(new Color(0.48f, 0.12f, 0.12f, 0.95f));
            _goldTex = MakeColorTex(new Color(0.60f, 0.45f, 0.10f, 0.98f));

            _panelStyle = new GUIStyle
            {
                normal = { background = _panelTex },
                padding = new RectOffset(16, 16, 16, 16)
            };

            _titleStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.85f, 0.30f) }
            };

            _subTitleStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.80f, 0.85f, 0.90f) }
            };

            _clueStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f), background = _headerTex },
                padding = new RectOffset(12, 12, 10, 10)
            };

            _thermalStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(1.0f, 0.45f, 0.25f), background = _headerTex },
                padding = new RectOffset(12, 12, 6, 6)
            };

            _btnStyle = new GUIStyle
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = Color.white, background = _btnTex },
                hover = { textColor = Color.yellow, background = _btnHoverTex },
                padding = new RectOffset(14, 14, 10, 10)
            };

            _explanationStyle = new GUIStyle
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                padding = new RectOffset(12, 12, 10, 10)
            };

            _scoreRowStyle = new GUIStyle
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                normal = { textColor = new Color(0.85f, 0.90f, 0.95f) },
                padding = new RectOffset(4, 4, 3, 3)
            };

            _totalScoreStyle = new GUIStyle
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.90f, 0.20f) }
            };

            _gradeBadgeStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = _goldTex },
                padding = new RectOffset(10, 10, 8, 8)
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

            var mgr = InvestigationManager.Instance;
            if (mgr == null) return;

            // Optional Top-Right quick test button
            DrawTestingBar(mgr);

            switch (mgr.State)
            {
                case InvestigationState.Investigating:
                    DrawInvestigationModal(mgr);
                    break;
                case InvestigationState.RootCauseReviewed:
                    DrawRootCauseReviewModal(mgr);
                    break;
                case InvestigationState.ScorecardReady:
                    DrawScorecardModal(mgr);
                    break;
            }
        }

        private void DrawTestingBar(InvestigationManager mgr)
        {
            if (mgr.State == InvestigationState.Inactive)
            {
                float btnWidth = 160f;
                float btnHeight = 36f;
                Rect rect = new Rect(Screen.width - btnWidth - 16f, 75f, btnWidth, btnHeight);

                if (GUI.Button(rect, "🔍 Investigate Fire"))
                {
                    mgr.StartInvestigation();
                }
            }
        }

        private void DrawInvestigationModal(InvestigationManager mgr)
        {
            if (_activeReport == null) return;

            float w = Mathf.Min(640f, Screen.width * 0.92f);
            float h = Mathf.Min(620f, Screen.height * 0.88f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(x + 16, y + 16, w - 32, h - 32));

            GUILayout.Label("INCIDENT INVESTIGATION: ROOT CAUSE ANALYSIS", _titleStyle);
            GUILayout.Label($"Damaged Unit: {_activeReport.equipmentName} | Mining Safety Protocol", _subTitleStyle);
            GUILayout.Space(12);

            GUILayout.Label("🔬 FORENSIC VISUAL INSPECTION:", EditorLabelHeader());
            GUILayout.Label(_activeReport.visualClue, _clueStyle);
            GUILayout.Space(6);
            GUILayout.Label($"🌡️ THERMAL IMAGING DATA: {_activeReport.thermalReading}", _thermalStyle);
            GUILayout.Space(14);

            GUILayout.Label("Select Primary Root Cause (MSHA 30 CFR Compliant):", EditorLabelHeader());
            GUILayout.Space(6);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(220));
            foreach (var opt in _activeReport.options)
            {
                string btnText = $"• {opt.title}\n  {opt.description}";
                if (GUILayout.Button(btnText, _btnStyle, GUILayout.MinHeight(54)))
                {
                    mgr.SubmitRootCause(opt.causeType);
                }
                GUILayout.Space(6);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawRootCauseReviewModal(InvestigationManager mgr)
        {
            float w = Mathf.Min(580f, Screen.width * 0.90f);
            float h = Mathf.Min(440f, Screen.height * 0.75f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(x + 16, y + 16, w - 32, h - 32));

            string headerTitle = _isCorrectReview ? "✅ ROOT CAUSE CONFIRMED (+200 PTS)" : "⚠️ INCORRECT ROOT CAUSE (+40 PTS)";
            Color headerColor = _isCorrectReview ? new Color(0.2f, 0.9f, 0.4f) : new Color(1.0f, 0.4f, 0.4f);

            var hStyle = new GUIStyle(_titleStyle) { normal = { textColor = headerColor } };
            GUILayout.Label(headerTitle, hStyle);
            GUILayout.Space(12);

            Texture2D bg = _isCorrectReview ? _correctTex : _incorrectTex;
            GUI.Box(new Rect(0, 50, w - 32, 170), GUIContent.none, new GUIStyle { normal = { background = bg } });

            GUILayout.BeginArea(new Rect(8, 56, w - 48, 158));
            GUILayout.Label(_reviewExplanation, _explanationStyle);
            GUILayout.EndArea();

            GUILayout.Space(190);

            if (GUILayout.Button("🏆 VIEW MSHA PERFORMANCE SCORECARD", _btnStyle, GUILayout.Height(50)))
            {
                mgr.FinalizeAndDisplayScorecard();
            }

            GUILayout.EndArea();
        }

        private void DrawScorecardModal(InvestigationManager mgr)
        {
            if (_activeScorecard == null) return;

            float w = Mathf.Min(660f, Screen.width * 0.94f);
            float h = Mathf.Min(680f, Screen.height * 0.92f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;

            GUI.Box(new Rect(x, y, w, h), GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(x + 16, y + 14, w - 32, h - 28));

            GUILayout.Label("OFFICIAL MSHA TRAINING PERFORMANCE SCORECARD", _titleStyle);
            GUILayout.Label("Mine Safety & Health Administration — Underground Emergency Standard", _subTitleStyle);
            GUILayout.Space(8);

            // Grade Badge
            Color gradeColor = GetGradeColor(_activeScorecard.grade);
            _gradeBadgeStyle.normal.textColor = gradeColor;
            GUILayout.Label(_activeScorecard.gradeTitle, _gradeBadgeStyle);
            GUILayout.Space(10);

            // Itemized score breakdown table
            GUILayout.Label("ITEMIZED EVALUATION:", EditorLabelHeader());
            GUILayout.Space(4);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(230));

            DrawScoreRow("🔥 Fire Detection Time:", $"{_activeScorecard.detectionTimeSeconds:F1}s", $"+{_activeScorecard.detectionScore} PTS");
            DrawScoreRow("🚨 Decision 1 (Alarm & Dispatch):", _activeScorecard.decision1Choice, $"{(_activeScorecard.decision1Score >= 0 ? "+" : "")}{_activeScorecard.decision1Score} PTS");
            DrawScoreRow("💨 Decision 2 (Ventilation Control):", _activeScorecard.decision2Choice, $"{(_activeScorecard.decision2Score >= 0 ? "+" : "")}{_activeScorecard.decision2Score} PTS");
            DrawScoreRow("🏃 Evacuation Speed Bonus:", $"{_activeScorecard.evacuationTimeSeconds:F1}s", $"+{_activeScorecard.evacuationBonus} PTS");
            DrawScoreRow("🧯 Decision 3 (Fire Response):", _activeScorecard.extinguisherChoice, $"{(_activeScorecard.decision3Score >= 0 ? "+" : "")}{_activeScorecard.decision3Score} PTS");
            DrawScoreRow("💦 Fire Suppression Complete:", _activeScorecard.wasSuppressed ? "Extinguished" : "Escalated to Rescue", $"+{_activeScorecard.suppressionBonus} PTS");
            DrawScoreRow("🔍 Root Cause Analysis:", _activeScorecard.investigationCorrect ? "Accurate" : "Sub-optimal", $"+{_activeScorecard.investigationScore} PTS");
            DrawScoreRow("❤️ Remaining Trainee Health:", $"{_activeScorecard.remainingHealth:F0}%", $"+{_activeScorecard.healthBonus} PTS");

            GUILayout.EndScrollView();
            GUILayout.Space(6);

            // Total Score
            GUILayout.Label($"TOTAL SCORE: {_activeScorecard.totalScore} / 1000 PTS", _totalScoreStyle);
            GUILayout.Space(4);

            // Summary text
            GUILayout.Label(_activeScorecard.overallSummary, _clueStyle);
            GUILayout.Space(10);

            // Action Buttons
            GUILayout.BeginHorizontal();

            if (_activeScorecard.grade != TraineeGrade.F_Disqualified)
            {
                if (GUILayout.Button("🔓 PROCEED TO LEVEL 2", _btnStyle, GUILayout.Height(44)))
                {
                    OnNextLevelRequested?.Invoke();
                    Debug.Log("[InvestigationAndScoreUI] Level 2 unlocked! Proceeding...");
                }
            }

            if (GUILayout.Button("🔄 RETRY SCENARIO", _btnStyle, GUILayout.Height(44)))
            {
                mgr.RestartScenario();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawScoreRow(string label, string detail, string points)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _scoreRowStyle, GUILayout.Width(220));
            GUILayout.Label(detail, _scoreRowStyle, GUILayout.Width(220));
            GUILayout.Label(points, new GUIStyle(_scoreRowStyle) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight });
            GUILayout.EndHorizontal();
        }

        private Color GetGradeColor(TraineeGrade grade)
        {
            switch (grade)
            {
                case TraineeGrade.A_Exemplary:
                    return new Color(0.25f, 1.0f, 0.45f);
                case TraineeGrade.B_Qualified:
                    return new Color(0.40f, 0.85f, 1.0f);
                case TraineeGrade.C_NeedsRetraining:
                    return new Color(1.0f, 0.80f, 0.20f);
                case TraineeGrade.F_Disqualified:
                default:
                    return new Color(1.0f, 0.30f, 0.30f);
            }
        }

        private GUIStyle EditorLabelHeader()
        {
            return new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.70f, 0.80f, 0.90f) }
            };
        }
    }
}
