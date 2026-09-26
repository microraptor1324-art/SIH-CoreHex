using System;
using UnityEngine;
using ARMiningSimulator.Investigation;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 9 Investigation & Scorecard UI — Fortnite Victory Royale & Match Stats Style.
    /// Features:
    /// - High-energy Victory Royale header banners.
    /// - Itemized score and XP award breakdowns with gold indicators.
    /// - Chunky 3D arcade buttons for Next Level and Replay.
    /// - 100% collision-free layout.
    /// </summary>
    public class InvestigationAndScoreUI : MonoBehaviour
    {
        public static event Action OnNextLevelRequested;

        private IncidentReport _activeReport;
        private ScoreBreakdown _activeScorecard;
        private bool _isCorrectReview = false;
        private int _reviewPoints = 0;
        private string _reviewExplanation = "";

        private GUIStyle _titleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _clueStyle;
        private GUIStyle _thermalStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _explanationStyle;
        private GUIStyle _scoreRowStyle;
        private GUIStyle _totalScoreStyle;
        private GUIStyle _gradeBadgeStyle;

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
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = TacticalUITheme.FortniteGold }
                };
            }

            if (_subTitleStyle == null)
            {
                _subTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.80f, 0.90f, 1.0f) }
                };
            }

            if (_clueStyle == null)
            {
                _clueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    wordWrap = true,
                    normal = { textColor = new Color(0.92f, 0.95f, 0.98f) }
                };
            }

            if (_thermalStyle == null)
            {
                _thermalStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = TacticalUITheme.FortniteRed }
                };
            }

            if (_btnStyle == null)
            {
                _btnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(18, 18, 10, 10)
                };
            }

            if (_explanationStyle == null)
            {
                _explanationStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.92f, 0.95f, 0.98f) }
                };
            }

            if (_scoreRowStyle == null)
            {
                _scoreRowStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white }
                };
            }

            if (_totalScoreStyle == null)
            {
                _totalScoreStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = TacticalUITheme.FortniteGold }
                };
            }

            if (_gradeBadgeStyle == null)
            {
                _gradeBadgeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
            }
        }

        private void OnGUI()
        {
            var mgr = InvestigationManager.Instance;
            if (mgr == null || mgr.State == InvestigationState.Inactive) return;

            InitStyles();

            switch (mgr.State)
            {
                case InvestigationState.MachineSelection:
                    DrawMachineSelectionModal(mgr);
                    break;

                case InvestigationState.MachineInspectionModal:
                    DrawMachineInspectionModal(mgr);
                    break;

                case InvestigationState.Investigating:
                    DrawInvestigationConsole(mgr);
                    break;

                case InvestigationState.RootCauseReviewed:
                    DrawReviewModal(mgr);
                    break;

                case InvestigationState.ScorecardReady:
                    DrawScorecardModal(mgr);
                    break;
            }
        }

        private void DrawMachineSelectionModal(InvestigationManager mgr)
        {
            var report = mgr.CurrentReport;
            if (report == null) return;

            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float w = Mathf.Min(700f, Screen.width - 32f);
            float h = Mathf.Min(580f, Screen.height - 32f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteGold, TacticalUITheme.FortniteNavyDark, "/// FORENSIC INCIDENT INVESTIGATION ///", TacticalUITheme.FortniteGold);

            GUI.Label(new Rect(x + 20, y + 16, w - 40, 26), "WHAT CAUSED THE FIRE? IDENTIFY THE ORIGIN MACHINE", _titleStyle);

            string subtitle = mgr.IsSmallFireFlow
                ? "✅ Fire extinguished! Now investigate the cause — follow the scorched footprint trail to the ignition machine."
                : "🚒 Fire suppressed by emergency team. Follow the burnt footprint trail and identify the origin machine.";
            GUI.Label(new Rect(x + 20, y + 42, w - 40, 20), subtitle, _subTitleStyle);

            float curY = y + 70f;

            // Alert banner if previous selection was incorrect
            if (!string.IsNullOrEmpty(mgr.MachineFeedbackText) && !mgr.IsMachineFeedbackCorrect)
            {
                Rect warnRect = new Rect(x + 24, curY, w - 48, 56);
                TacticalUITheme.DrawFortniteCard(warnRect, TacticalUITheme.FortniteRed, TacticalUITheme.CardSlotBg);
                var warnStyle = new GUIStyle(_explanationStyle) { normal = { textColor = TacticalUITheme.FortniteRed } };
                GUI.Label(new Rect(warnRect.x + 12, warnRect.y + 6, warnRect.width - 24, warnRect.height - 12), mgr.MachineFeedbackText, warnStyle);
                curY += 66f;
            }

            GUI.Label(new Rect(x + 24, curY, w - 48, 22), "Drift Machinery Candidates (Follow In-World Burnt Footprints):", _subTitleStyle);
            curY += 26f;

            var candidates = report.candidateMachines;
            float cardH = 68f;
            float gap = 10f;

            for (int i = 0; i < candidates.Count; i++)
            {
                var cand = candidates[i];
                Rect cardRect = new Rect(x + 24, curY + i * (cardH + gap), w - 48, cardH);

                Color borderCol = cand.hasBeenInspected ? TacticalUITheme.FortniteGreen : TacticalUITheme.BorderSubtle;
                TacticalUITheme.DrawFortniteCard(cardRect, borderCol, TacticalUITheme.CardSlotBg);

                // Machine Title & Status
                string inspectedTag = cand.hasBeenInspected ? " [🔍 INSPECTED]" : " [⚠️ UNINSPECTED]";
                var nameStyle = new GUIStyle(_titleStyle)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = cand.hasBeenInspected ? TacticalUITheme.FortniteGreen : Color.white }
                };
                GUI.Label(new Rect(cardRect.x + 16, cardRect.y + 12, cardRect.width - 280, 22), $"⚙️ {cand.displayName.ToUpper()}{inspectedTag}", nameStyle);

                var hintStyle = new GUIStyle(_subTitleStyle)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.7f, 0.8f, 0.9f) }
                };
                GUI.Label(new Rect(cardRect.x + 16, cardRect.y + 36, cardRect.width - 280, 20),
                    cand.hasBeenInspected ? "Evidence logged: review details or confirm origin." : "Click Inspect to view visual signs and thermography.", hintStyle);

                // Inspect Button
                float btnInspectW = 110f;
                float btnOriginW = 140f;
                float btnInspectX = cardRect.x + cardRect.width - btnInspectW - btnOriginW - 16f;
                float btnOriginX = cardRect.x + cardRect.width - btnOriginW - 10f;
                float btnY = cardRect.y + 11f;
                float btnH = cardH - 22f;

                if (TacticalUITheme.DrawFortniteButton(new Rect(btnInspectX, btnY, btnInspectW, btnH), "🔍 INSPECT", TacticalUITheme.FortniteBlue, _btnStyle, "[LOG]"))
                {
                    mgr.InspectCandidateMachine(cand);
                }

                if (TacticalUITheme.DrawFortniteButton(new Rect(btnOriginX, btnY, btnOriginW, btnH), "🎯 CONFIRM ORIGIN", TacticalUITheme.FortniteGold, _btnStyle, "[SELECT]"))
                {
                    mgr.SubmitMachineOrigin(cand);
                }
            }
        }

        private void DrawMachineInspectionModal(InvestigationManager mgr)
        {
            var cand = mgr.ActiveInspectedMachine;
            if (cand == null) return;

            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float w = Mathf.Min(640f, Screen.width - 32f);
            float h = 460f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavyDark, "/// FORENSIC EQUIPMENT INSPECTION ///", TacticalUITheme.FortniteBlue);

            GUI.Label(new Rect(x + 20, y + 16, w - 40, 26), $"INSPECTING: {cand.displayName.ToUpper()}", _titleStyle);
            GUI.Label(new Rect(x + 20, y + 42, w - 40, 20), "Forensic Field Examination & Sensor Readings", _subTitleStyle);

            float curY = y + 74f;

            // Visual Evidence Card
            Rect clueBox = new Rect(x + 24, curY, w - 48, 100);
            TacticalUITheme.DrawFortniteCard(clueBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(clueBox.x + 14, clueBox.y + 10, clueBox.width - 28, clueBox.height - 20),
                $"🔍 Visual Inspection Log:\n{cand.inspectionClueText}", _clueStyle);
            curY += 114f;

            // Thermal Telemetry Card
            Rect teleBox = new Rect(x + 24, curY, w - 48, 70);
            TacticalUITheme.DrawFortniteCard(teleBox, TacticalUITheme.FortniteRed, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(teleBox.x + 14, teleBox.y + 10, teleBox.width - 28, teleBox.height - 20),
                $"🌡️ Sensor Telemetry:\n{cand.thermalTelemetryText}", _thermalStyle);
            curY += 86f;

            // Action Buttons
            float btnW = (w - 60f) * 0.5f;
            float btnH = 54f;

            Rect backRect = new Rect(x + 24, curY, btnW, btnH);
            if (TacticalUITheme.DrawFortniteButton(backRect, "◀ BACK TO CANDIDATES", TacticalUITheme.FortniteNavyLight, _btnStyle, "[BACK]"))
            {
                mgr.CloseInspectionModal();
            }

            Rect confirmRect = new Rect(x + 24 + btnW + 12f, curY, btnW, btnH);
            if (TacticalUITheme.DrawFortniteButton(confirmRect, "🎯 CONFIRM AS ORIGIN ➔", TacticalUITheme.FortniteGold, _btnStyle, "[CONFIRM]"))
            {
                mgr.SubmitMachineOrigin(cand);
            }
        }

        private void DrawInvestigationConsole(InvestigationManager mgr)
        {
            if (_activeReport == null) return;

            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float w = Mathf.Min(640f, Screen.width - 32f);
            float h = Mathf.Min(560f, Screen.height - 32f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavyDark, "/// FORENSIC INCIDENT INVESTIGATION ///", TacticalUITheme.FortniteBlue);

            GUI.Label(new Rect(x + 20, y + 16, w - 40, 26), "FORENSIC ROOT CAUSE ANALYSIS", _titleStyle);
            GUI.Label(new Rect(x + 20, y + 42, w - 40, 20), $"MSHA Equipment Investigation — {_activeReport.equipmentName}", _subTitleStyle);

            float curY = y + 70f;

            // Clue Card
            Rect clueBox = new Rect(x + 24, curY, w - 48, 70);
            TacticalUITheme.DrawFortniteCard(clueBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(clueBox.x + 14, clueBox.y + 10, clueBox.width - 28, clueBox.height - 20),
                $"🔍 Visual Inspection Log:\n{_activeReport.visualClue}", _clueStyle);
            curY += 82f;

            // Telemetry Card
            Rect teleBox = new Rect(x + 24, curY, w - 48, 44);
            TacticalUITheme.DrawFortniteCard(teleBox, TacticalUITheme.FortniteRed, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(teleBox.x + 14, teleBox.y + 12, teleBox.width - 28, 20),
                $"🌡️ Thermal Signature: {_activeReport.thermalReading}", _thermalStyle);
            curY += 56f;

            // Question prompt
            GUI.Label(new Rect(x + 24, curY, w - 48, 22), "Select the Root Cause of this Underground Fire Incident:", _subTitleStyle);
            curY += 26f;

            // Options List
            float optH = 58f;
            float optGap = 10f;
            for (int i = 0; i < _activeReport.options.Count; i++)
            {
                var opt = _activeReport.options[i];
                Rect optRect = new Rect(x + 24, curY + i * (optH + optGap), w - 48, optH);

                string badge = $"[{i + 1}] ";
                string labelText = $"{badge}{opt.title.ToUpper()}\n    {opt.description}";

                Color optAccent = TacticalUITheme.FortniteBlue;
                string tag = $"[HYPOTHESIS {i + 1}]";
                if (TacticalUITheme.DrawFortniteButton(optRect, labelText, optAccent, _btnStyle, tag))
                {
                    mgr.SubmitRootCause(opt.causeType);
                }
            }
        }

        private void DrawReviewModal(InvestigationManager mgr)
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float w = Mathf.Min(580f, Screen.width - 32f);
            float h = 390f;
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            Color outcomeColor = _isCorrectReview ? TacticalUITheme.FortniteGreen : TacticalUITheme.FortniteAmber;
            string ribbonTag = _isCorrectReview ? "/// ROOT CAUSE VERIFIED ///" : "/// INCOMPLETE FINDING ///";
            TacticalUITheme.DrawFortniteCard(modalRect, outcomeColor, TacticalUITheme.FortniteNavyDark, ribbonTag, outcomeColor);

            string headerTitle = _isCorrectReview ? "🏆 ROOT CAUSE VERIFIED (+200 PTS)" : "⚠️ SUB-OPTIMAL ROOT CAUSE (+40 PTS)";
            var hStyle = new GUIStyle(_titleStyle) { normal = { textColor = outcomeColor }, fontSize = 20 };
            GUI.Label(new Rect(x + 20, y + 20, w - 40, 26), headerTitle, hStyle);
            GUI.Label(new Rect(x + 20, y + 46, w - 40, 20), "MSHA EMERGENCY SAFETY STANDARD REVIEW", _subTitleStyle);

            // Explanation box
            Rect expBox = new Rect(x + 24, y + 74, w - 48, 195);
            TacticalUITheme.DrawFortniteCard(expBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(expBox.x + 16, expBox.y + 14, expBox.width - 32, expBox.height - 28), _reviewExplanation, _explanationStyle);

            // Scorecard Button
            Rect btnRect = new Rect(x + 35, y + 295, w - 70, 60);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "🏆 VIEW PERFORMANCE SCORECARD ➔", outcomeColor, _btnStyle, "[SCORECARD]"))
            {
                mgr.FinalizeAndDisplayScorecard();
            }
        }

        private void DrawScorecardModal(InvestigationManager mgr)
        {
            if (_activeScorecard == null) return;

            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.94f));

            float w = Mathf.Min(680f, Screen.width - 24f);
            float h = Mathf.Min(680f, Screen.height - 24f);
            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            Color gradeColor = GetGradeColor(_activeScorecard.grade);
            TacticalUITheme.DrawFortniteCard(modalRect, gradeColor, TacticalUITheme.FortniteNavyDark, "/// VICTORY ROYALE // MATCH STATS ///", TacticalUITheme.FortniteGold);

            GUI.Label(new Rect(x + 20, y + 16, w - 40, 26), "🏆 OFFICIAL MSHA PERFORMANCE SCORECARD", _titleStyle);
            GUI.Label(new Rect(x + 20, y + 40, w - 40, 18), "Underground Emergency Safety Qualification Standard", _subTitleStyle);

            // Grade Badge Card
            Rect badgeRect = new Rect(x + 25, y + 62, w - 50, 42);
            TacticalUITheme.DrawFortniteCard(badgeRect, gradeColor, TacticalUITheme.CardSlotBg);
            _gradeBadgeStyle.normal.textColor = gradeColor;
            GUI.Label(new Rect(badgeRect.x, badgeRect.y + 7, badgeRect.width, 28), _activeScorecard.gradeTitle, _gradeBadgeStyle);

            // Itemized score table
            bool hasMachineScore = _activeScorecard.machineOriginScore > 0;
            float tableH = hasMachineScore ? 280f : 256f;
            Rect tableRect = new Rect(x + 25, y + 110, w - 50, tableH);
            TacticalUITheme.DrawFortniteCard(tableRect, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);

            float rowY = tableRect.y + 8f;
            float rowH = hasMachineScore ? 26f : 27f;

            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "🔥 Fire Detection Time:", $"{_activeScorecard.detectionTimeSeconds:F1}s", $"+{_activeScorecard.detectionScore} PTS");
            rowY += rowH;
            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "🚨 Decision 1 (Alarm & Dispatch):", _activeScorecard.decision1Choice, $"{(_activeScorecard.decision1Score >= 0 ? "+" : "")}{_activeScorecard.decision1Score} PTS");
            rowY += rowH;
            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "💨 Decision 2 (Ventilation Control):", _activeScorecard.decision2Choice, $"{(_activeScorecard.decision2Score >= 0 ? "+" : "")}{_activeScorecard.decision2Score} PTS");
            rowY += rowH;
            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "🏃 Evacuation Speed Bonus:", $"{_activeScorecard.evacuationTimeSeconds:F1}s", $"+{_activeScorecard.evacuationBonus} PTS");
            rowY += rowH;
            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "🧯 Decision 3 (Fire Response):", _activeScorecard.extinguisherChoice, $"{(_activeScorecard.decision3Score >= 0 ? "+" : "")}{_activeScorecard.decision3Score} PTS");
            rowY += rowH;
            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "💦 Fire Suppression Complete:", _activeScorecard.wasSuppressed ? "Extinguished" : "Escalated to Rescue", $"+{_activeScorecard.suppressionBonus} PTS");
            rowY += rowH;

            if (hasMachineScore)
            {
                DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "👣 Machine Origin Identification:", _activeScorecard.identifiedMachineName, $"+{_activeScorecard.machineOriginScore} PTS");
                rowY += rowH;
            }

            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "🔍 Incident Containment Evaluation:", _activeScorecard.investigationCorrect ? "Suppressed / Verified" : "Sub-optimal", $"+{_activeScorecard.investigationScore} PTS");
            rowY += rowH;
            DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowH), "❤️ Remaining Trainee Health:", $"{_activeScorecard.remainingHealth:F0}%", $"+{_activeScorecard.healthBonus} PTS");

            // Total Score Row
            float totalY = tableRect.y + tableH + 8f;
            Rect totalRect = new Rect(x + 25, totalY, w - 50, 46);
            TacticalUITheme.DrawFortniteCard(totalRect, TacticalUITheme.FortniteGold, TacticalUITheme.CardSlotBg);
            int maxPossible = hasMachineScore ? 1400 : 1300;
            GUI.Label(new Rect(totalRect.x, totalRect.y + 10, totalRect.width, 26), $"TOTAL DRILL SCORE: {_activeScorecard.totalScore} / {maxPossible:N0} PTS", _totalScoreStyle);

            // Summary text box
            float summaryY = totalY + 54f;
            Rect summaryBox = new Rect(x + 25, summaryY, w - 50, 72);
            TacticalUITheme.DrawFortniteCard(summaryBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(summaryBox.x + 14, summaryBox.y + 8, summaryBox.width - 28, summaryBox.height - 16), _activeScorecard.overallSummary, _clueStyle);

            // Action Buttons
            float btnW = (w - 60f) * 0.5f;
            float btnH = 54f;
            float btnY = y + h - btnH - 16f;

            bool canAdvance = _activeScorecard.grade != TraineeGrade.F_Disqualified;

            Rect nextBtnRect = new Rect(x + 25, btnY, canAdvance ? btnW : (w - 50f), btnH);
            if (canAdvance)
            {
                if (TacticalUITheme.DrawFortniteButton(nextBtnRect, "🔓 ADVANCE TO NEXT LEVEL ➔", TacticalUITheme.FortniteGreen, _btnStyle, "[CONTINUE]"))
                {
                    OnNextLevelRequested?.Invoke();
                }

                Rect retryBtnRect = new Rect(x + 25 + btnW + 10f, btnY, btnW, btnH);
                if (TacticalUITheme.DrawFortniteButton(retryBtnRect, "🔄 RETRY DRILL", TacticalUITheme.FortniteBlue, _btnStyle, "[REPLAY]"))
                {
                    mgr.RestartScenario();
                }
            }
            else
            {
                if (TacticalUITheme.DrawFortniteButton(nextBtnRect, "🔄 RETRY DRILL (QUALIFICATION REQUIRED)", TacticalUITheme.FortniteRed, _btnStyle, "[RETRY]"))
                {
                    mgr.RestartScenario();
                }
            }
        }

        private void DrawScoreRow(Rect r, string label, string detail, string points)
        {
            float labelW = r.width * 0.45f;
            float detailW = r.width * 0.35f;
            float ptsW = r.width * 0.20f;

            GUI.Label(new Rect(r.x, r.y, labelW, r.height), label, _scoreRowStyle);
            GUI.Label(new Rect(r.x + labelW, r.y, detailW, r.height), detail, _scoreRowStyle);

            var ptStyle = new GUIStyle(_scoreRowStyle)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = TacticalUITheme.FortniteGold }
            };
            GUI.Label(new Rect(r.x + labelW + detailW, r.y, ptsW, r.height), points, ptStyle);
        }

        private Color GetGradeColor(TraineeGrade grade)
        {
            switch (grade)
            {
                case TraineeGrade.A_Exemplary:
                    return TacticalUITheme.FortniteGreen;
                case TraineeGrade.B_Qualified:
                    return TacticalUITheme.FortniteBlue;
                case TraineeGrade.C_NeedsRetraining:
                    return TacticalUITheme.FortniteAmber;
                case TraineeGrade.F_Disqualified:
                default:
                    return TacticalUITheme.FortniteRed;
            }
        }
    }
}
