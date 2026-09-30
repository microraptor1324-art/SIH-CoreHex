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

        private ScoreBreakdown _activeScorecard;

        private GUIStyle _titleStyle;
        private GUIStyle _subTitleStyle;
        private GUIStyle _clueStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _scoreRowStyle;
        private GUIStyle _totalScoreStyle;
        private GUIStyle _gradeBadgeStyle;

        private Vector2 _scrollPos = Vector2.zero;

        private void OnEnable()
        {
            InvestigationManager.OnScorecardReady += HandleScorecardReady;
        }

        private void OnDisable()
        {
            InvestigationManager.OnScorecardReady -= HandleScorecardReady;
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

            TacticalUITheme.BeginScaledGUI();
            try
            {
                switch (mgr.State)
                {
                    case InvestigationState.MachineSelection:
                        DrawMachineTapPrompt(mgr);
                        break;

                    case InvestigationState.ScorecardReady:
                        DrawScorecardModal(mgr);
                        break;
                }
            }
            finally
            {
                TacticalUITheme.EndScaledGUI();
            }
        }

        /// <summary>
        /// Fire-origin identification prompt. Deliberately non-blocking (no fullscreen scrim) —
        /// the trainee identifies the machine by tapping it directly in the 3D scene behind this
        /// banner (see FireOriginTapDetector), not through a list UI, so the banner must never
        /// cover the machines themselves.
        /// </summary>
        private void DrawMachineTapPrompt(InvestigationManager mgr)
        {
            float w = Mathf.Min(520f, TacticalUITheme.VW - 32f);
            string prompt = "🔍 Identify the machine that caused the fire.";
            float promptH = Mathf.Max(TacticalUITheme.CalcTextHeight(_titleStyle, prompt, w - 40f), 26f) + 20f;
            float x = (TacticalUITheme.VW - w) * 0.5f;
            float y = TacticalUITheme.HudTopZoneClearY;

            Rect bannerRect = new Rect(x, y, w, promptH);
            TacticalUITheme.DrawFortniteCard(bannerRect, TacticalUITheme.FortniteGold, TacticalUITheme.FortniteNavyDark);
            GUI.Label(new Rect(x + 20, y, w - 40, promptH), prompt, _titleStyle);

            // Brief tap feedback ("Try again." / "You're right!") — fades away a couple seconds
            // after each attempt so it never blocks the view of the machines.
            if (!string.IsNullOrEmpty(mgr.MachineFeedbackText) && mgr.MachineFeedbackAge < 2.5f)
            {
                Color fbColor = mgr.IsMachineFeedbackCorrect ? TacticalUITheme.FortniteGreen : TacticalUITheme.FortniteRed;
                float fbW = Mathf.Min(320f, TacticalUITheme.VW - 32f);
                float fbH = 64f;
                float fbX = (TacticalUITheme.VW - fbW) * 0.5f;
                float fbY = y + promptH + 14f;
                Rect fbRect = new Rect(fbX, fbY, fbW, fbH);
                TacticalUITheme.DrawFortniteCard(fbRect, fbColor, TacticalUITheme.FortniteNavyDark);
                var fbStyle = new GUIStyle(_titleStyle) { normal = { textColor = fbColor }, fontSize = 22 };
                GUI.Label(fbRect, mgr.MachineFeedbackText, fbStyle);
            }
        }

        private Vector2 _scorecardScrollPos;

        /// <summary>One itemized scorecard line: label + detail + points, each independently wrapped.</summary>
        private struct ScoreRowData
        {
            public string Label;
            public string Detail;
            public string Points;
            public ScoreRowData(string label, string detail, string points) { Label = label; Detail = detail; Points = points; }
        }

        private ScoreRowData[] BuildScoreRows(bool hasMachineScore)
        {
            var rows = new ScoreRowData[hasMachineScore ? 9 : 8];
            int i = 0;
            rows[i++] = new ScoreRowData("🔥 Fire Detection Time:", $"{_activeScorecard.detectionTimeSeconds:F1}s", $"+{_activeScorecard.detectionScore} PTS");
            rows[i++] = new ScoreRowData("🚨 Decision 1 (Alarm & Dispatch):", _activeScorecard.decision1Choice, $"{(_activeScorecard.decision1Score >= 0 ? "+" : "")}{_activeScorecard.decision1Score} PTS");
            rows[i++] = new ScoreRowData("💨 Decision 2 (Ventilation Control):", _activeScorecard.decision2Choice, $"{(_activeScorecard.decision2Score >= 0 ? "+" : "")}{_activeScorecard.decision2Score} PTS");
            rows[i++] = new ScoreRowData("🏃 Evacuation Speed Bonus:", $"{_activeScorecard.evacuationTimeSeconds:F1}s", $"+{_activeScorecard.evacuationBonus} PTS");
            rows[i++] = new ScoreRowData("🧯 Decision 3 (Fire Response):", _activeScorecard.extinguisherChoice, $"{(_activeScorecard.decision3Score >= 0 ? "+" : "")}{_activeScorecard.decision3Score} PTS");
            rows[i++] = new ScoreRowData("💦 Fire Suppression Complete:", _activeScorecard.wasSuppressed ? "Extinguished" : "Escalated to Rescue", $"+{_activeScorecard.suppressionBonus} PTS");
            if (hasMachineScore)
            {
                rows[i++] = new ScoreRowData("👣 Machine Origin Identification:", _activeScorecard.identifiedMachineName, $"+{_activeScorecard.machineOriginScore} PTS");
            }
            rows[i++] = new ScoreRowData("🔍 Incident Containment Evaluation:", _activeScorecard.investigationCorrect ? "Suppressed / Verified" : "Sub-optimal", $"+{_activeScorecard.investigationScore} PTS");
            rows[i++] = new ScoreRowData("❤️ Remaining Trainee Health:", $"{_activeScorecard.remainingHealth:F0}%", $"+{_activeScorecard.healthBonus} PTS");
            return rows;
        }

        /// <summary>Measures each row's real height (max of its wrapped label/detail columns) — never a guessed constant.</summary>
        private float[] MeasureScoreRowHeights(ScoreRowData[] rows, float rowWidth, out float labelW, out float detailW, out float ptsW)
        {
            labelW = rowWidth * 0.48f;
            detailW = rowWidth * 0.34f;
            ptsW = rowWidth - labelW - detailW;
            float[] heights = new float[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                float lh = TacticalUITheme.CalcTextHeight(_scoreRowStyle, rows[i].Label, labelW);
                float dh = TacticalUITheme.CalcTextHeight(_scoreRowStyle, rows[i].Detail, detailW);
                heights[i] = Mathf.Max(Mathf.Max(lh, dh), 18f) + 10f;
            }
            return heights;
        }

        private void DrawScorecardModal(InvestigationManager mgr)
        {
            if (_activeScorecard == null) return;

            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.09f, 0.94f));

            float w = Mathf.Min(680f, TacticalUITheme.VW - 24f);
            float innerW = w - 50f;

            bool hasMachineScore = _activeScorecard.machineOriginScore > 0;
            ScoreRowData[] rows = BuildScoreRows(hasMachineScore);
            float[] rowHeights = MeasureScoreRowHeights(rows, innerW - 24f, out _, out _, out _);
            float rowsTotalH = 0f;
            for (int i = 0; i < rowHeights.Length; i++) rowsTotalH += rowHeights[i];
            float tableH = rowsTotalH + 16f;

            // Grade title can wrap ("GRADE A — EXEMPLARY MINE SAFETY OFFICER" on a narrow screen) —
            // measure it instead of a fixed 42px guess, so it never spills into the subtitle above
            // or the score table below.
            float badgeH = Mathf.Max(TacticalUITheme.CalcTextHeight(_gradeBadgeStyle, _activeScorecard.gradeTitle, innerW - 24f) + 14f, 42f);
            const float totalH = 46f;
            float summaryH = Mathf.Max(TacticalUITheme.CalcTextHeight(_clueStyle, _activeScorecard.overallSummary, innerW - 28f) + 16f, 48f);

            // Title/subtitle can wrap ("OFFICIAL MSHA PERFORMANCE SCORECARD" on a narrow screen) —
            // measure both instead of assuming one line, so the body below is never pushed under them.
            string titleText = "🏆 OFFICIAL MSHA PERFORMANCE SCORECARD";
            string subText = "Underground Emergency Safety Qualification Standard";
            float titleH = Mathf.Max(TacticalUITheme.CalcTextHeight(_titleStyle, titleText, w - 40f), 26f);
            float subH = Mathf.Max(TacticalUITheme.CalcTextHeight(_subTitleStyle, subText, w - 40f), 18f);
            const float titleTopPad = 16f;
            const float titleToSub = 4f;
            const float subToBody = 12f;
            float chromeH = titleTopPad + titleH + titleToSub + subH + subToBody;

            const float badgeToTable = 8f;
            const float tableToTotal = 8f;
            const float totalToSummary = 8f;
            const float summaryToButtons = 16f;
            const float bottomPad = 16f;

            // Buttons can wrap to 2 lines ("ADVANCE TO NEXT LEVEL") — measure the tallest one instead
            // of a fixed height, so wrapped text is never clipped/overlapped by the corner badge.
            float btnTextW = (w - 60f) * 0.5f - _btnStyle.padding.horizontal;
            float btnH = Mathf.Max(
                Mathf.Max(
                    TacticalUITheme.CalcTextHeight(_btnStyle, "🔓 ADVANCE TO NEXT LEVEL ➔", btnTextW),
                    TacticalUITheme.CalcTextHeight(_btnStyle, "🔄 RETRY DRILL", btnTextW)),
                TacticalUITheme.CalcTextHeight(_btnStyle, "🔄 RETRY DRILL (QUALIFICATION REQUIRED)", w - 50f - _btnStyle.padding.horizontal))
                + _btnStyle.padding.vertical;
            btnH = Mathf.Max(btnH, 54f);

            float bodyH = badgeH + badgeToTable + tableH + tableToTotal + totalH + totalToSummary + summaryH;
            float naturalH = chromeH + bodyH + summaryToButtons + btnH + bottomPad;
            float maxH = TacticalUITheme.VH - 24f;
            float h = Mathf.Min(naturalH, maxH);
            bool needsScroll = naturalH > maxH;

            float x = (TacticalUITheme.VW - w) * 0.5f;
            float y = (TacticalUITheme.VH - h) * 0.5f;
            Rect modalRect = new Rect(x, y, w, h);

            Color gradeColor = GetGradeColor(_activeScorecard.grade);
            TacticalUITheme.DrawFortniteCard(modalRect, gradeColor, TacticalUITheme.FortniteNavyDark, "/// SCORE ///", TacticalUITheme.FortniteGold);

            GUI.Label(new Rect(x + 20, y + titleTopPad, w - 40, titleH), titleText, _titleStyle);
            GUI.Label(new Rect(x + 20, y + titleTopPad + titleH + titleToSub, w - 40, subH), subText, _subTitleStyle);

            float bodyAreaY = y + chromeH;
            float bodyAreaH = h - chromeH - summaryToButtons - btnH - bottomPad;

            if (needsScroll)
            {
                Rect viewRect = new Rect(x + 25, bodyAreaY, innerW, bodyAreaH);
                Rect contentRect = new Rect(0, 0, innerW - 16f, bodyH);
                _scorecardScrollPos = GUI.BeginScrollView(viewRect, _scorecardScrollPos, contentRect);
                DrawScorecardBody(0f, contentRect.width, gradeColor, hasMachineScore, rows, rowHeights, badgeH, tableH, summaryH);
                GUI.EndScrollView();
            }
            else
            {
                DrawScorecardBody(x + 25, innerW, gradeColor, hasMachineScore, rows, rowHeights, badgeH, tableH, summaryH, bodyAreaY);
            }

            // Action Buttons — always pinned at the bottom, never pushed off-screen
            float btnW = (w - 60f) * 0.5f;
            float btnY = y + h - btnH - bottomPad;

            bool canAdvance = _activeScorecard.grade != TraineeGrade.F_Disqualified;

            Rect nextBtnRect = new Rect(x + 25, btnY, canAdvance ? btnW : (w - 50f), btnH);
            if (canAdvance)
            {
                if (TacticalUITheme.DrawFortniteButton(nextBtnRect, "🔓 ADVANCE TO NEXT LEVEL ➔", TacticalUITheme.FortniteGreen, _btnStyle))
                {
                    OnNextLevelRequested?.Invoke();
                }

                Rect retryBtnRect = new Rect(x + 25 + btnW + 10f, btnY, btnW, btnH);
                if (TacticalUITheme.DrawFortniteButton(retryBtnRect, "🔄 RETRY DRILL", TacticalUITheme.FortniteBlue, _btnStyle))
                {
                    mgr.RestartScenario();
                }
            }
            else
            {
                if (TacticalUITheme.DrawFortniteButton(nextBtnRect, "🔄 RETRY DRILL (QUALIFICATION REQUIRED)", TacticalUITheme.FortniteRed, _btnStyle))
                {
                    mgr.RestartScenario();
                }
            }
        }

        /// <summary>Draws the badge + score table + total + summary block starting at (bx, by).</summary>
        private void DrawScorecardBody(float bx, float bw, Color gradeColor, bool hasMachineScore, ScoreRowData[] rows, float[] rowHeights, float badgeH, float tableH, float summaryH, float by = 0f)
        {
            // Grade Badge Card — height matches the measurement above, so a long grade title
            // ("GRADE A — EXEMPLARY MINE SAFETY OFFICER") that wraps to 2 lines never spills
            // into the subtitle above it or the score table below it.
            Rect badgeRect = new Rect(bx, by, bw, badgeH);
            TacticalUITheme.DrawFortniteCard(badgeRect, gradeColor, TacticalUITheme.CardSlotBg);
            _gradeBadgeStyle.normal.textColor = gradeColor;
            GUI.Label(new Rect(badgeRect.x, badgeRect.y + 7, badgeRect.width, badgeH - 14f), _activeScorecard.gradeTitle, _gradeBadgeStyle);

            // Itemized score table — each row is exactly as tall as its own wrapped text, so a long
            // label/value (e.g. "ABC Dry Chemical Powder Extinguisher") never overlaps the next row.
            Rect tableRect = new Rect(bx, by + badgeH + 8f, bw, tableH);
            TacticalUITheme.DrawFortniteCard(tableRect, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);

            float rowY = tableRect.y + 8f;
            for (int i = 0; i < rows.Length; i++)
            {
                DrawScoreRow(new Rect(tableRect.x + 12, rowY, tableRect.width - 24, rowHeights[i]), rows[i].Label, rows[i].Detail, rows[i].Points);
                rowY += rowHeights[i];
            }

            // Total Score Row
            float totalY = tableRect.y + tableH + 8f;
            Rect totalRect = new Rect(bx, totalY, bw, 46f);
            TacticalUITheme.DrawFortniteCard(totalRect, TacticalUITheme.FortniteGold, TacticalUITheme.CardSlotBg);
            int maxPossible = hasMachineScore ? 1400 : 1300;
            GUI.Label(new Rect(totalRect.x, totalRect.y + 10, totalRect.width, 26), $"TOTAL DRILL SCORE: {_activeScorecard.totalScore} / {maxPossible:N0} PTS", _totalScoreStyle);

            // Summary text box — sized to its real text, never a fixed 72px guess
            float summaryY = totalY + 46f + 8f;
            Rect summaryBox = new Rect(bx, summaryY, bw, summaryH);
            TacticalUITheme.DrawFortniteCard(summaryBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(summaryBox.x + 14, summaryBox.y + 8, summaryBox.width - 28, summaryBox.height - 16), _activeScorecard.overallSummary, _clueStyle);
        }

        private void DrawScoreRow(Rect r, string label, string detail, string points)
        {
            float labelW = r.width * 0.48f;
            float detailW = r.width * 0.34f;
            float ptsW = r.width - labelW - detailW;

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
