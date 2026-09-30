using UnityEngine;
using ARMiningSimulator.Decisions;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 6 Interactive Decision UI — Fortnite Quest & Battle Royale Style.
    /// Features:
    /// - Slanted 3D header banner ("/// TACTICAL SURVIVAL DECISION ///").
    /// - Chunky Fortnite buttons with rarity tiers (Legendary Gold, Epic Purple, Hazardous Red).
    /// - Tactile 3D press-down interaction states.
    /// - Thick segmented rapid-response timer meter with Gold bonus callout.
    /// - High-energy Victory / Hazard feedback modal cards.
    /// </summary>
    public class DecisionUI : MonoBehaviour
    {
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _questionStyle;
        private GUIStyle _optionBtnStyle;
        private GUIStyle _feedbackStyle;
        private GUIStyle _actionBtnStyle;
        private Vector2 _optionsScrollPos;

        private void OnGUI()
        {
            if (DecisionManager.Instance == null) return;

            InitStyles();

            TacticalUITheme.BeginScaledGUI();
            try
            {
                if (DecisionManager.Instance.IsWaitingForSelection)
                {
                    DrawQuestionModal();
                }
                else if (DecisionManager.Instance.IsShowingFeedback)
                {
                    DrawFeedbackModal();
                }
            }
            finally
            {
                TacticalUITheme.EndScaledGUI();
            }
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = TacticalUITheme.FortniteGold }
                };
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.80f, 0.90f, 1.0f) }
                };
            }

            if (_questionStyle == null)
            {
                _questionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }

            if (_optionBtnStyle == null)
            {
                _optionBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(20, 20, 10, 10)
                };
            }

            if (_feedbackStyle == null)
            {
                _feedbackStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    normal = { textColor = new Color(0.92f, 0.95f, 0.98f) }
                };
            }

            if (_actionBtnStyle == null)
            {
                _actionBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }
        }

        private void DrawQuestionModal()
        {
            var q = DecisionManager.Instance.CurrentQuestion;
            if (q == null) return;

            // Fullscreen dark backdrop scrim
            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, TacticalUITheme.VW - 32f);
            float innerW = modalWidth - 50f;
            float chromeInnerW = modalWidth - 40f;

            const float questionToOptions = 26f;
            const float bottomPad = 16f;
            const float optGap = 12f;
            const float titleTopPad = 16f;
            const float titleToQuestion = 16f;

            string bannerTag = !string.IsNullOrEmpty(q.title) ? q.title : "TACTICAL DECISION PROTOCOL";

            // Title can wrap to 2 lines on narrow screens / long titles ("DECISION 2: MINE
            // VENTILATION & SMOKE DIRECTION") — measure it instead of assuming one line, so the
            // question box below is never pushed under it.
            float titleH = Mathf.Max(TacticalUITheme.CalcTextHeight(_headerStyle, bannerTag, chromeInnerW), 26f);
            float chromeH = titleTopPad + titleH + titleToQuestion;

            // Question box sized to its actual text — never a guessed constant.
            float questionH = Mathf.Max(TacticalUITheme.CalcTextHeight(_questionStyle, q.questionText, innerW - 24f) + 16f, 44f);

            // Each option row sized to its actual (possibly wrapped) text, via the button's own padding.
            int optCount = q.options.Count;
            float[] optHeights = new float[optCount];
            float optListH = 0f;
            for (int i = 0; i < optCount; i++)
            {
                string labelText = $"[{i + 1}] {q.options[i].text.ToUpper()}";
                float textH = TacticalUITheme.CalcTextHeight(_optionBtnStyle, labelText, innerW - _optionBtnStyle.padding.horizontal);
                optHeights[i] = Mathf.Max(textH + _optionBtnStyle.padding.vertical, 56f);
                optListH += optHeights[i];
                if (i > 0) optListH += optGap;
            }

            float naturalH = chromeH + questionH + questionToOptions + optListH + bottomPad;
            float maxModalH = TacticalUITheme.VH - 32f;
            float modalHeight = Mathf.Min(naturalH, maxModalH);
            bool needsScroll = naturalH > maxModalH;
            float mx = (TacticalUITheme.VW - modalWidth) * 0.5f;
            float my = (TacticalUITheme.VH - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            // Card with no ribbon tag — kept plain per request.
            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortnitePurple, TacticalUITheme.FortniteNavyDark);

            // Title — height matches the measurement above, so a long wrapped title never bleeds
            // into the question box beneath it.
            GUI.Label(new Rect(mx + 20, my + titleTopPad, chromeInnerW, titleH), bannerTag, _headerStyle);

            // Question prompt box — height matches the measurement above
            Rect questionBox = new Rect(mx + 25, my + chromeH, innerW, questionH);
            TacticalUITheme.DrawFortniteCard(questionBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(questionBox.x + 12, questionBox.y + 8, questionBox.width - 24, questionBox.height - 16), q.questionText, _questionStyle);

            // 3 Chunky 3D Action Choice Buttons (Uniform Color - NO HINTS)
            float optAreaY = my + chromeH + questionH + questionToOptions;
            float optAreaH = modalHeight - (chromeH + questionH + questionToOptions) - bottomPad;

            if (needsScroll)
            {
                Rect viewRect = new Rect(mx + 25, optAreaY, innerW, optAreaH);
                Rect contentRect = new Rect(0, 0, innerW - 16f, optListH);
                _optionsScrollPos = GUI.BeginScrollView(viewRect, _optionsScrollPos, contentRect);

                float sy = 0f;
                for (int i = 0; i < optCount; i++)
                {
                    string labelText = $"[{i + 1}] {q.options[i].text.ToUpper()}";
                    if (TacticalUITheme.DrawFortniteButton(new Rect(0, sy, contentRect.width, optHeights[i]), labelText, TacticalUITheme.FortniteBlue, _optionBtnStyle))
                    {
                        DecisionManager.Instance.SubmitDecision(i);
                    }
                    sy += optHeights[i] + optGap;
                }

                GUI.EndScrollView();
            }
            else
            {
                float optY = optAreaY;
                for (int i = 0; i < optCount; i++)
                {
                    string labelText = $"[{i + 1}] {q.options[i].text.ToUpper()}";
                    if (TacticalUITheme.DrawFortniteButton(new Rect(mx + 25, optY, innerW, optHeights[i]), labelText, TacticalUITheme.FortniteBlue, _optionBtnStyle))
                    {
                        DecisionManager.Instance.SubmitDecision(i);
                    }
                    optY += optHeights[i] + optGap;
                }
            }
        }

        private void DrawFeedbackModal()
        {
            var opt = DecisionManager.Instance.LastSelectedOption;
            if (opt == null) return;

            // Fullscreen dark tactical scrim
            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, TacticalUITheme.VW - 32f);
            float innerW = modalWidth - 50f;

            const float chromeH = 76f;     // title + score line, above the explanation box
            const float boxToBtn = 20f;
            const float btnH = 66f;
            const float bottomPad = 20f;

            // Explanation box shows a short confirmation only, per request (no detailed rule text).
            string shortFeedback = opt.isCorrect ? "✅ Correct! Well done." : "❌ Incorrect — try again.";
            float explanationH = Mathf.Max(TacticalUITheme.CalcTextHeight(_feedbackStyle, shortFeedback, innerW - 28f) + 24f, 60f);
            float naturalH = chromeH + explanationH + boxToBtn + btnH + bottomPad;
            float modalHeight = Mathf.Min(naturalH, TacticalUITheme.VH - 32f);
            float mx = (TacticalUITheme.VW - modalWidth) * 0.5f;
            float my = (TacticalUITheme.VH - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            Color outcomeColor = opt.isCorrect ? TacticalUITheme.FortniteGreen : (opt.scoreModifier > 0 ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed);

            // Card with no ribbon tag — kept plain per request.
            TacticalUITheme.DrawFortniteCard(modalRect, outcomeColor, TacticalUITheme.FortniteNavyDark);

            // Title
            string resultTitle = opt.isCorrect ? "🏆 CORRECT SURVIVAL ACTION!" : (opt.scoreModifier > 0 ? "⚠️ SUB-OPTIMAL PROCEDURE" : "🚨 HAZARDOUS VIOLATION LOGGED");
            var resStyle = new GUIStyle(_headerStyle) { normal = { textColor = outcomeColor }, fontSize = 20 };
            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 28), resultTitle, resStyle);

            // Score Modifier Badge
            string scoreSign = opt.scoreModifier >= 0 ? $"+{opt.scoreModifier}" : $"{opt.scoreModifier}";
            int totalScore = DecisionManager.Instance.TotalScore;
            GUI.Label(new Rect(mx + 20, my + 48, modalWidth - 40, 20), $"SCORE IMPACT: {scoreSign} PTS  |  TOTAL SCORE: {totalScore} PTS", _subHeaderStyle);

            Rect expBox = new Rect(mx + 25, my + chromeH, innerW, explanationH);
            TacticalUITheme.DrawFortniteCard(expBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(expBox.x + 14, expBox.y + 12, expBox.width - 28, expBox.height - 24), shortFeedback, _feedbackStyle);

            // Proceed Action Button
            bool isFinal = DecisionManager.Instance.CurrentStage == DecisionStage.Stage2_Ventilation;
            string nextBtnText;
            if (opt.isCorrect)
            {
                nextBtnText = isFinal
                    ? "PROCEED TO REFUGE EVACUATION ➔"
                    : "PROCEED TO VENTILATION CONTROL ➔";
            }
            else
            {
                nextBtnText = "🔄 RE-EVALUATE: SELECT CORRECT SAFETY ACTION ➔";
            }

            Rect nextBtnRect = new Rect(mx + 30, my + chromeH + explanationH + boxToBtn, modalWidth - 60, btnH);
            if (TacticalUITheme.DrawFortniteButton(nextBtnRect, nextBtnText, outcomeColor, _actionBtnStyle))
            {
                DecisionManager.Instance.ProceedAfterFeedback();
            }
        }
    }
}
