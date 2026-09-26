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
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
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
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }
        }

        private void DrawQuestionModal()
        {
            var q = DecisionManager.Instance.CurrentQuestion;
            if (q == null) return;

            // Fullscreen dark backdrop scrim
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, Screen.width - 32f);
            float modalHeight = 450f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            // Fortnite Card with slanted ribbon
            string bannerTag = !string.IsNullOrEmpty(q.title) ? q.title : "TACTICAL DECISION PROTOCOL";
            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortnitePurple, TacticalUITheme.FortniteNavyDark, $"/// {bannerTag} ///", TacticalUITheme.FortnitePurple);

            // Title
            GUI.Label(new Rect(mx + 20, my + 16, modalWidth - 40, 26), bannerTag, _headerStyle);

            // Threat / Escalation Warning line
            float fireGrowthLimit = Fire.FireManager.Instance != null ? Fire.FireManager.Instance.FireGrowthTime : 60f;
            float fireRem = Fire.FireManager.Instance != null ? Fire.FireManager.Instance.RemainingSmallTime : fireGrowthLimit;
            bool isSmall = Fire.FireManager.Instance == null || Fire.FireManager.Instance.IsSmallPhase;
            string statusStr = isSmall
                ? $"⚡ FLASH ESCALATION RISK — {fireRem:F1}s BEFORE COMPARTMENT FLASHOVER ⚡"
                : "🚨 CRITICAL INFERNO LEVEL — LIFE HAZARD RISK CRITICAL 🚨";
            _subHeaderStyle.normal.textColor = isSmall ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed;
            GUI.Label(new Rect(mx + 20, my + 44, modalWidth - 40, 20), statusStr, _subHeaderStyle);

            // Rapid response timer bar
            float elapsed = DecisionManager.Instance.StageTimer;
            float timeRatio = Mathf.Clamp01(elapsed / q.timeLimit);
            Rect timerBar = new Rect(mx + 30, my + 68, modalWidth - 60, 10);
            Color barColor = elapsed <= 8f ? TacticalUITheme.FortniteGold : (elapsed <= 14f ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed);
            TacticalUITheme.DrawFortniteBar(timerBar, 1f - timeRatio, barColor, TacticalUITheme.CardSlotBg, 10);

            // Question prompt box
            Rect questionBox = new Rect(mx + 25, my + 86, modalWidth - 50, 60);
            TacticalUITheme.DrawFortniteCard(questionBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(questionBox.x + 12, questionBox.y + 8, questionBox.width - 24, questionBox.height - 16), q.questionText, _questionStyle);

            // 3 Chunky 3D Action Choice Buttons (Uniform Color - NO HINTS)
            float optY = my + 158f;
            float optH = 60f;
            float optGap = 12f;

            for (int i = 0; i < q.options.Count; i++)
            {
                var opt = q.options[i];
                Rect optRect = new Rect(mx + 25, optY + i * (optH + optGap), modalWidth - 50, optH);

                string badge = $"[{i + 1}] ";
                string labelText = $"{badge}{opt.text.ToUpper()}";

                Color optAccent = TacticalUITheme.FortniteBlue;
                string neutralTag = $"[OPTION {i + 1}]";

                if (TacticalUITheme.DrawFortniteButton(optRect, labelText, optAccent, _optionBtnStyle, neutralTag))
                {
                    DecisionManager.Instance.SubmitDecision(i);
                }
            }

            // Quick bonus hint at bottom
            string bonusHint = elapsed <= 8.0f ? "⚡ RAPID-RESPONSE BONUS ACTIVE (+25 BONUS PTS)" : "STANDARD RESPONSE PROTOCOL";
            var hintStyle = new GUIStyle(_subHeaderStyle) { normal = { textColor = elapsed <= 8.0f ? TacticalUITheme.FortniteGold : new Color(0.6f, 0.7f, 0.8f) } };
            GUI.Label(new Rect(mx + 20, my + modalHeight - 26, modalWidth - 40, 20), bonusHint, hintStyle);
        }

        private void DrawFeedbackModal()
        {
            var opt = DecisionManager.Instance.LastSelectedOption;
            if (opt == null) return;

            // Fullscreen dark tactical scrim
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, Screen.width - 32f);
            float modalHeight = 360f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            Color outcomeColor = opt.isCorrect ? TacticalUITheme.FortniteGreen : (opt.scoreModifier > 0 ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed);
            string ribbonTag = opt.isCorrect ? "/// OBJECTIVE VERIFIED ///" : "/// SAFETY VIOLATION ///";

            TacticalUITheme.DrawFortniteCard(modalRect, outcomeColor, TacticalUITheme.FortniteNavyDark, ribbonTag, outcomeColor);

            // Title
            string resultTitle = opt.isCorrect ? "🏆 CORRECT SURVIVAL ACTION!" : (opt.scoreModifier > 0 ? "⚠️ SUB-OPTIMAL PROCEDURE" : "🚨 HAZARDOUS VIOLATION LOGGED");
            var resStyle = new GUIStyle(_headerStyle) { normal = { textColor = outcomeColor }, fontSize = 20 };
            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 28), resultTitle, resStyle);

            // Score Modifier Badge
            string scoreSign = opt.scoreModifier >= 0 ? $"+{opt.scoreModifier}" : $"{opt.scoreModifier}";
            int totalScore = DecisionManager.Instance.TotalScore;
            GUI.Label(new Rect(mx + 20, my + 48, modalWidth - 40, 20), $"SCORE IMPACT: {scoreSign} PTS  |  TOTAL SCORE: {totalScore} PTS", _subHeaderStyle);

            // Explanation box
            Rect expBox = new Rect(mx + 25, my + 76, modalWidth - 50, 160);
            TacticalUITheme.DrawFortniteCard(expBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(expBox.x + 14, expBox.y + 12, expBox.width - 28, expBox.height - 24), opt.explanation, _feedbackStyle);

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

            Rect nextBtnRect = new Rect(mx + 30, my + 265, modalWidth - 60, 60);
            if (TacticalUITheme.DrawFortniteButton(nextBtnRect, nextBtnText, outcomeColor, _actionBtnStyle, opt.isCorrect ? "[CONTINUE]" : "[RETRY]"))
            {
                DecisionManager.Instance.ProceedAfterFeedback();
            }
        }
    }
}
