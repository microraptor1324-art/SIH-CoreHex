using UnityEngine;
using ARMiningSimulator.Decisions;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 6 Interactive Decision Test UI.
    /// Presents Stage 1 (Alarm & Surface Notification) and Stage 2 (Ventilation Control).
    /// Features:
    /// - High contrast dark glassmorphism card styling.
    /// - Response time bar with quick-thinking bonus indicator.
    /// - Large touch-friendly option buttons (>= 50px).
    /// - Educational feedback panel citing official MSHA/ISO mining safety standards.
    /// - Total training score tracker.
    /// </summary>
    public class DecisionUI : MonoBehaviour
    {
        private Texture2D _whiteTexture;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _questionStyle;
        private GUIStyle _optionBtnStyle;
        private GUIStyle _feedbackStyle;
        private GUIStyle _actionBtnStyle;

        private void Awake()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void OnGUI()
        {
            if (DecisionManager.Instance == null) return;

            InitStyles();

            if (DecisionManager.Instance.IsWaitingForSelection)
            {
                DrawQuestionModal();
            }
            else if (DecisionManager.Instance.IsShowingFeedback)
            {
                DrawFeedbackModal();
            }
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _headerStyle.normal.textColor = new Color(1.0f, 0.85f, 0.2f);
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _subHeaderStyle.normal.textColor = new Color(0.7f, 0.85f, 1.0f);
            }

            if (_questionStyle == null)
            {
                _questionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                _questionStyle.normal.textColor = Color.white;
            }

            if (_optionBtnStyle == null)
            {
                _optionBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true
                };
                _optionBtnStyle.normal.textColor = Color.white;
            }

            if (_feedbackStyle == null)
            {
                _feedbackStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true
                };
                _feedbackStyle.normal.textColor = new Color(0.9f, 0.92f, 0.95f);
            }

            if (_actionBtnStyle == null)
            {
                _actionBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _actionBtnStyle.normal.textColor = Color.white;
            }
        }

        private void DrawQuestionModal()
        {
            var q = DecisionManager.Instance.CurrentQuestion;
            if (q == null) return;

            // Dim background overlay
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.75f));

            float modalWidth = Mathf.Min(560f, Screen.width - 30f);
            float optionCount = q.options.Count;
            float modalHeight = 180f + optionCount * 60f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            // Main Modal Card
            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.12f, 0.16f, 0.95f));

            // Stage Title
            string stageTag = DecisionManager.Instance.CurrentStage == DecisionStage.Stage1_Alarm
                ? "⚠️ DECISION STAGE 1 OF 2: IMMEDIATE ACTION"
                : "⚠️ DECISION STAGE 2 OF 2: VENTILATION CONTROL";
            GUI.Label(new Rect(mx, my + 15, modalWidth, 24), stageTag, _headerStyle);

            // Timer bar
            float elapsed = DecisionManager.Instance.StageTimer;
            float timeRatio = Mathf.Clamp01(elapsed / q.timeLimit);
            Rect timerBar = new Rect(mx + 30, my + 44, modalWidth - 60, 6);
            DrawColorRect(timerBar, new Color(0.2f, 0.22f, 0.28f));
            Color timerColor = elapsed < 8f ? new Color(0.2f, 0.8f, 0.4f) : (elapsed < 14f ? new Color(0.9f, 0.7f, 0.1f) : new Color(0.9f, 0.2f, 0.2f));
            DrawColorRect(new Rect(timerBar.x, timerBar.y, timerBar.width * (1f - timeRatio), 6), timerColor);

            // Question Text
            GUI.Label(new Rect(mx + 25, my + 60, modalWidth - 50, 50), q.questionText, _questionStyle);

            // Options List
            float optY = my + 120f;
            for (int i = 0; i < q.options.Count; i++)
            {
                var opt = q.options[i];
                Rect btnRect = new Rect(mx + 25, optY, modalWidth - 50, 52);

                GUI.backgroundColor = new Color(0.18f, 0.22f, 0.30f);
                if (GUI.Button(btnRect, $"  {opt.text}", _optionBtnStyle))
                {
                    DecisionManager.Instance.SubmitDecision(i);
                }
                GUI.backgroundColor = Color.white;

                optY += 60f;
            }
        }

        private void DrawFeedbackModal()
        {
            var opt = DecisionManager.Instance.LastSelectedOption;
            if (opt == null) return;

            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.80f));

            float modalWidth = Mathf.Min(560f, Screen.width - 30f);
            float modalHeight = 360f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.12f, 0.16f, 0.96f));

            // Result Banner
            string bannerText = opt.isCorrect ? "✅ CORRECT SAFETY PROTOCOL" : "❌ SAFETY PROTOCOL BREACH";
            Color bannerColor = opt.isCorrect ? new Color(0.15f, 0.85f, 0.45f) : new Color(0.95f, 0.25f, 0.25f);

            GUI.color = bannerColor;
            var resultStyle = new GUIStyle(_headerStyle) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(mx, my + 20, modalWidth, 30), bannerText, resultStyle);
            GUI.color = Color.white;

            // Score Modifier Badge
            string scoreSign = opt.scoreModifier >= 0 ? $"+{opt.scoreModifier}" : $"{opt.scoreModifier}";
            GUI.Label(new Rect(mx, my + 52, modalWidth, 22), $"Score Impact: {scoreSign} PTS (Total Score: {DecisionManager.Instance.TotalScore})", _subHeaderStyle);

            // Explanation Box
            Rect expBoxRect = new Rect(mx + 25, my + 85, modalWidth - 50, 180);
            DrawColorRect(expBoxRect, new Color(0.06f, 0.08f, 0.11f, 0.85f));

            GUI.Label(new Rect(expBoxRect.x + 15, expBoxRect.y + 12, expBoxRect.width - 30, 24), "📋 Standard Mining Protocol (MSHA / ISO):", _subHeaderStyle);
            GUI.Label(new Rect(expBoxRect.x + 15, expBoxRect.y + 40, expBoxRect.width - 30, 130), opt.explanation, _feedbackStyle);

            // Continue Button
            string nextBtnText = DecisionManager.Instance.CurrentStage == DecisionStage.Stage1_Alarm
                ? "PROCEED TO STAGE 2: VENTILATION ➔"
                : "PROCEED TO EVACUATION ➔";

            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.35f);
            if (GUI.Button(new Rect(mx + 35, my + 285, modalWidth - 70, 52), nextBtnText, _actionBtnStyle))
            {
                DecisionManager.Instance.ProceedAfterFeedback();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawColorRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = old;
        }
    }
}
