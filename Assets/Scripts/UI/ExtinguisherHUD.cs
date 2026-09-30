using UnityEngine;
using UnityEngine.InputSystem;
using ARMiningSimulator.Extinguisher;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 8 Extinguisher & Fire Response HUD — Fortnite & Battle Royale Action Style.
    /// Manages:
    /// - Decision Stage 3A: Fire Size Assessment modal (Common Incipient vs Mythic Inferno)
    /// - Decision Stage 3A: Tactical Action Choice modal (Fight Assault vs Defensive Evac)
    /// - Decision Stage 3B: Extinguisher Agent Loadout Selection modal (Rare CO2 vs Epic Dry Chemical vs Hazard Foam)
    /// - Interactive P.A.S.S. Discharge Combat HUD (Ammo/Pressure Gauge, Reticle, 3D Squeeze Trigger)
    /// - Victory Royale Fire Suppression Celebration modal
    /// </summary>
    public class ExtinguisherHUD : MonoBehaviour
    {
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _questionStyle;
        private GUIStyle _optionBtnStyle;
        private GUIStyle _optionTitleStyle;
        private GUIStyle _optionDescStyle;
        private GUIStyle _actionBtnStyle;
        private GUIStyle _triggerBtnStyle;
        private GUIStyle _feedbackStyle;

        // Shared scroll position for the option list on whichever Decision-3 modal is
        // currently open (only one is ever visible at a time, so one field is enough).
        private Vector2 _optionsScrollPos;

        // Squeeze trigger hold tracking. IMGUI only reports MouseDown/MouseDrag on the frame the
        // pointer changes, so a finger held still would read as released. Instead we remember the
        // button rect from OnGUI and poll the live pointer state in Update.
        private Rect _triggerRect;
        private int _triggerDrawnFrame = -1;
        private bool _triggerHeld;

        private void Update()
        {
            if (ExtinguisherController.Instance == null) return;

            // Only honour the trigger while the discharge HUD is actually on screen
            bool triggerVisible = Time.frameCount - _triggerDrawnFrame <= 1;
            bool held = triggerVisible && IsPointerHeldInside(_triggerRect);

            if (held != _triggerHeld)
            {
                _triggerHeld = held;
                ExtinguisherController.Instance.SetExternalTrigger(held);
            }
        }

        private void OnDisable()
        {
            if (_triggerHeld && ExtinguisherController.Instance != null)
                ExtinguisherController.Instance.SetExternalTrigger(false);
            _triggerHeld = false;
        }

        private static bool IsPointerHeldInside(Rect guiRect)
        {
            // Input System reports real device pixels (bottom-left origin), but guiRect is in
            // IMGUI's scaled "virtual" coordinate space (top-left origin) — see
            // TacticalUITheme.BeginScaledGUI. Flip using the real Screen.height, then divide by
            // UIScale to land in the same virtual space the rect was captured in.
            float scale = TacticalUITheme.UIScale;

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (!touch.press.isPressed) continue;
                    Vector2 p = touch.position.ReadValue();
                    Vector2 virtualPos = new Vector2(p.x, Screen.height - p.y) / scale;
                    if (guiRect.Contains(virtualPos)) return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 p = Mouse.current.position.ReadValue();
                Vector2 virtualPos = new Vector2(p.x, Screen.height - p.y) / scale;
                if (guiRect.Contains(virtualPos)) return true;
            }

            return false;
        }

        private void OnGUI()
        {
            if (FireResponseManager.Instance == null) return;

            // Suppress ExtinguisherHUD completely if any investigation or scorecard state is active
            if (ARMiningSimulator.Investigation.InvestigationManager.Instance != null &&
                ARMiningSimulator.Investigation.InvestigationManager.Instance.State != ARMiningSimulator.Investigation.InvestigationState.Inactive)
            {
                return;
            }

            InitStyles();

            TacticalUITheme.BeginScaledGUI();
            try
            {
                FireResponseState state = FireResponseManager.Instance.State;

                if (state == FireResponseState.Stage3A_FireSizeEval)
                {
                    if (FireResponseManager.Instance.IsShowingFeedback)
                    {
                        string nextBtn = FireResponseManager.Instance.IsFeedbackCorrect
                            ? "PROCEED TO TACTICAL ACTION ➔"
                            : "🔄 RE-EVALUATE: SELECT CORRECT FIRE SIZE ➔";
                        DrawFeedbackModal(nextBtn, () => FireResponseManager.Instance.ProceedAfterSizeFeedback(), FireResponseManager.Instance.IsFeedbackCorrect);
                    }
                    else
                    {
                        DrawStage3A_SizeEvalModal();
                    }
                }
                else if (state == FireResponseState.Stage3A_ActionChoice)
                {
                    if (FireResponseManager.Instance.IsShowingFeedback)
                    {
                        string nextBtn = FireResponseManager.Instance.IsFeedbackCorrect
                            ? (FireResponseManager.Instance.IsActuallyBig ? "PROCEED TO INCIDENT REPORT ➔" : "PROCEED TO SELECT EXTINGUISHER AGENT ➔")
                            : (FireResponseManager.Instance.IsActuallyBig ? "🔄 RE-EVALUATE: CHOOSE STAY IN SAFETY ➔" : "🔄 RE-EVALUATE: CHOOSE FIGHT THE FIRE ➔");
                        DrawFeedbackModal(nextBtn, () => FireResponseManager.Instance.ProceedAfterActionFeedback(), FireResponseManager.Instance.IsFeedbackCorrect);
                    }
                    else
                    {
                        DrawStage3A_ActionChoiceModal();
                    }
                }
                else if (state == FireResponseState.Stage3B_AgentSelection)
                {
                    if (FireResponseManager.Instance.IsShowingFeedback)
                    {
                        string nextBtn = FireResponseManager.Instance.IsFeedbackCorrect
                            ? "PROCEED TO P.A.S.S. DISCHARGE ➔"
                            : "🔄 RE-EVALUATE & SELECT PROPER AGENT ➔";
                        DrawFeedbackModal(nextBtn, () => FireResponseManager.Instance.ProceedAfterAgentFeedback(), FireResponseManager.Instance.IsFeedbackCorrect);
                    }
                    else
                    {
                        DrawStage3B_AgentModal();
                    }
                }
                else if (state == FireResponseState.PASS_Discharge || (ExtinguisherController.Instance != null && ExtinguisherController.Instance.IsEquipped))
                {
                    DrawDischargeHUD();
                }
                else if (state == FireResponseState.Extinguished)
                {
                    DrawSuppressionSuccessModal();
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
                    normal = { textColor = new Color(0.80f, 0.90f, 1.0f) }
                };
            }

            if (_questionStyle == null)
            {
                _questionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }

            if (_optionBtnStyle == null)
            {
                _optionBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(18, 18, 10, 10)
                };
            }

            if (_optionTitleStyle == null)
            {
                _optionTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    wordWrap = true,
                    normal = { textColor = Color.white }
                };
            }

            if (_optionDescStyle == null)
            {
                _optionDescStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    normal = { textColor = new Color(0.80f, 0.87f, 0.95f) }
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

            if (_triggerBtnStyle == null)
            {
                _triggerBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
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
        }

        /// <summary>One selectable row for a content-driven option list: label text + click handler.</summary>
        private struct OptionRow
        {
            public string Title;
            public string Description;
            public System.Action OnClick;
            public OptionRow(string title, string description, System.Action onClick)
            {
                Title = title;
                Description = description;
                OnClick = onClick;
            }
        }

        private void DrawStage3A_SizeEvalModal()
        {
            DrawDecisionModal(
                TacticalUITheme.FortniteBlue,
                "DECISION 3: FIRE SIZE EVALUATION",
                "At the Safe Refuge, evaluate the hazard before taking your tactical action:\nIs this hazard a Small (Incipient) or Big (Critical) fire?",
                new[]
                {
                    new OptionRow("🕯️ 1) SMALL INCIPIENT FIRE",
                        "Localized equipment flame; unblocked retreat path; fightable with extinguisher.",
                        () => FireResponseManager.Instance.SubmitFireSizeAssessment(false)),
                    new OptionRow("🔥 2) BIG ESCALATED INFERNO",
                        "Large roaring flames, rolling toxic smoke; unmanageable by portable canister.",
                        () => FireResponseManager.Instance.SubmitFireSizeAssessment(true)),
                });
        }

        private void DrawStage3A_ActionChoiceModal()
        {
            bool isBig = FireResponseManager.Instance.IsActuallyBig;
            string promptText = isBig
                ? "The fire has been assessed. Dense smoke and rapid heat release are present.\nWhat tactical life-safety action should you take?"
                : "The fire has been assessed. Portable fire extinguishers are positioned in this zone.\nWhat tactical life-safety action should you take?";

            DrawDecisionModal(
                TacticalUITheme.FortnitePurple,
                "🧯 TACTICAL ACTION PROTOCOL",
                promptText,
                new[]
                {
                    new OptionRow("⚔️ 1) FIGHT WITH EXTINGUISHER (ATTACK)",
                        "Take portable extinguisher from Safe Area, verify clear retreat route, suppress fire.",
                        () => FireResponseManager.Instance.SubmitTacticalAction(true)),
                    new OptionRow("🛡️ 2) STAY IN REFUGE & EVACUATE MINE (DEFEND)",
                        "Do not attack. Seal refuge chamber, notify surface rescue, await mine rescue team.",
                        () => FireResponseManager.Instance.SubmitTacticalAction(false)),
                },
                headerColor: TacticalUITheme.FortniteGold);
        }

        private void DrawStage3B_AgentModal()
        {
            DrawDecisionModal(
                TacticalUITheme.FortniteGold,
                "SELECT EXTINGUISHING WEAPON AGENT",
                "The equipment fire is ready to be fought. Grab an extinguisher and get ready.",
                new[]
                {
                    new OptionRow("🧯 PICK FIRE EXTINGUISHER",
                        "Take the extinguisher from the Safe Area and prepare for suppression.",
                        () => FireResponseManager.Instance.PickFireExtinguisher()),
                });
        }

        /// <summary>
        /// Shared modal shell for every Decision-3 sub-screen: header card, a question box sized
        /// to its actual text, and a list of two-line option rows each sized to ITS actual text
        /// (title + description), never a guessed constant. If the fully laid-out content would
        /// exceed the available screen height, the option list scrolls instead of clipping —
        /// the header and question stay pinned and always visible.
        /// </summary>
        private void DrawDecisionModal(Color accentColor, string headerText, string questionText, OptionRow[] options, Color? headerColor = null)
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, TacticalUITheme.VW - 32f);
            float innerW = modalWidth - 50f;
            float optionInnerW = innerW - 36f; // minus the option row's own left/right text padding

            // 1. Measure the question box height from its real text.
            float questionH = Mathf.Max(TacticalUITheme.CalcTextHeight(_questionStyle, questionText, innerW - 20f) + 16f, 44f);

            // 2. Measure every option row's real height (title + description, each wrapped).
            float[] rowHeights = new float[options.Length];
            float optionsTotalH = 0f;
            const float rowGap = 12f;
            for (int i = 0; i < options.Length; i++)
            {
                rowHeights[i] = TacticalUITheme.MeasureOptionRowHeight(options[i].Title, options[i].Description, _optionTitleStyle, _optionDescStyle, optionInnerW);
                optionsTotalH += rowHeights[i];
                if (i > 0) optionsTotalH += rowGap;
            }

            const float titleTopPad = 18f;
            const float titleToQuestion = 14f;
            const float questionToOptions = 16f;
            const float bottomPad = 16f;

            // Header can wrap to 2 lines for longer strings ("SELECT EXTINGUISHING WEAPON AGENT"
            // on a narrow screen) — measure it instead of assuming one line, same as the question
            // and option rows, so the question box below it is never pushed into/over the header.
            float headerH = Mathf.Max(TacticalUITheme.CalcTextHeight(_headerStyle, headerText, modalWidth - 40f), 26f);
            float topToQuestion = titleTopPad + headerH + titleToQuestion;

            float naturalContentH = topToQuestion + questionH + questionToOptions + optionsTotalH + bottomPad;
            float maxModalH = TacticalUITheme.VH - 32f;
            float modalHeight = Mathf.Min(naturalContentH, maxModalH);
            bool needsScroll = naturalContentH > maxModalH;

            float mx = (TacticalUITheme.VW - modalWidth) * 0.5f;
            float my = (TacticalUITheme.VH - modalHeight) * 0.5f;

            // Card with no ribbon tag — kept plain per request.
            TacticalUITheme.DrawFortniteCard(new Rect(mx, my, modalWidth, modalHeight), accentColor, TacticalUITheme.FortniteNavyDark);

            var hStyle = headerColor.HasValue ? new GUIStyle(_headerStyle) { normal = { textColor = headerColor.Value } } : _headerStyle;
            GUI.Label(new Rect(mx + 20, my + titleTopPad, modalWidth - 40, headerH), headerText, hStyle);

            Rect promptBox = new Rect(mx + 25, my + topToQuestion, innerW, questionH);
            TacticalUITheme.DrawFortniteCard(promptBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(promptBox.x + 10, promptBox.y + 6, promptBox.width - 20, promptBox.height - 12), questionText, _questionStyle);

            float optionsAreaY = my + topToQuestion + questionH + questionToOptions;
            float optionsAreaH = modalHeight - (topToQuestion + questionH + questionToOptions) - bottomPad;

            if (needsScroll)
            {
                Rect viewRect = new Rect(mx + 25, optionsAreaY, innerW, optionsAreaH);
                Rect contentRect = new Rect(0, 0, innerW - 16f, optionsTotalH);
                _optionsScrollPos = GUI.BeginScrollView(viewRect, _optionsScrollPos, contentRect);

                float y = 0f;
                for (int i = 0; i < options.Length; i++)
                {
                    DrawTwoLineOption(0f, y, contentRect.width, rowHeights[i], options[i].Title, options[i].Description, options[i].OnClick);
                    y += rowHeights[i] + rowGap;
                }

                GUI.EndScrollView();
            }
            else
            {
                float y = optionsAreaY;
                for (int i = 0; i < options.Length; i++)
                {
                    DrawTwoLineOption(mx + 25, y, innerW, rowHeights[i], options[i].Title, options[i].Description, options[i].OnClick);
                    y += rowHeights[i] + rowGap;
                }
            }
        }

        /// <summary>
        /// Draws a wide option button whose title and description are separate label draws
        /// (never crammed into one wrapped button string), so neither can overlap the other.
        /// Height is passed in already measured from the real text by the caller.
        /// </summary>
        private void DrawTwoLineOption(float x, float y, float width, float height, string title, string description, System.Action onClick)
        {
            Rect rect = new Rect(x, y, width, height);
            bool clicked = TacticalUITheme.DrawFortniteButton(rect, "", TacticalUITheme.FortniteBlue, _optionBtnStyle);

            float titleH = TacticalUITheme.CalcTextHeight(_optionTitleStyle, title, rect.width - 36f);
            TacticalUITheme.DrawShadowedLabel(new Rect(rect.x + 18, rect.y + 10, rect.width - 36, titleH), title, _optionTitleStyle);
            TacticalUITheme.DrawShadowedLabel(new Rect(rect.x + 18, rect.y + 10 + titleH + 6f, rect.width - 36, rect.height - (10 + titleH + 6f) - 8f), description, _optionDescStyle);

            if (clicked) onClick?.Invoke();
        }

        private void DrawFeedbackModal(string nextButtonText, System.Action onNext, bool isCorrect)
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, TacticalUITheme.VW - 32f);
            float innerW = modalWidth - 50f;

            const float chromeH = 54f;      // title, above the explanation box
            const float boxToBtn = 20f;
            const float bottomPad = 20f;

            // Explanation box shows a short confirmation only, per request (no detailed rule text).
            string shortFeedback = isCorrect ? "✅ Correct! Well done." : "❌ Incorrect — try again.";
            float explanationH = Mathf.Max(TacticalUITheme.CalcTextHeight(_feedbackStyle, shortFeedback, innerW - 28f) + 24f, 60f);

            // Proceed/re-evaluate button height from its own real label — these strings can be
            // long ("PROCEED TO SELECT EXTINGUISHER AGENT ➔") and must wrap to 2 lines rather
            // than clip, so the button must be tall enough to hold however many lines that takes.
            float btnH = Mathf.Max(TacticalUITheme.CalcTextHeight(_actionBtnStyle, nextButtonText, modalWidth - 60f - 24f) + 24f, 60f);

            float naturalH = chromeH + explanationH + boxToBtn + btnH + bottomPad;
            float modalHeight = Mathf.Min(naturalH, TacticalUITheme.VH - 32f);
            float mx = (TacticalUITheme.VW - modalWidth) * 0.5f;
            float my = (TacticalUITheme.VH - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            Color outcomeColor = isCorrect ? TacticalUITheme.FortniteGreen : TacticalUITheme.FortniteRed;

            // Card with no ribbon tag — kept plain per request.
            TacticalUITheme.DrawFortniteCard(modalRect, outcomeColor, TacticalUITheme.FortniteNavyDark);

            string title = isCorrect ? "🏆 CORRECT SURVIVAL ACTION!" : "⚠️ SAFETY HAZARD LOGGED!";
            var titleStyle = new GUIStyle(_headerStyle) { normal = { textColor = outcomeColor }, fontSize = 20 };
            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 28), title, titleStyle);

            Rect expBox = new Rect(mx + 25, my + chromeH, innerW, explanationH);
            TacticalUITheme.DrawFortniteCard(expBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(expBox.x + 14, expBox.y + 12, expBox.width - 28, expBox.height - 24), shortFeedback, _feedbackStyle);

            // No corner rarity-badge here: it collided with wrapped 2-line button labels, so the
            // full instruction is carried in the label text itself (e.g. "PROCEED TO ...").
            Rect nextBtnRect = new Rect(mx + 30, my + chromeH + explanationH + boxToBtn, modalWidth - 60, btnH);
            if (TacticalUITheme.DrawFortniteButton(nextBtnRect, nextButtonText, outcomeColor, _actionBtnStyle))
            {
                onNext?.Invoke();
            }
        }

        // =========================================================================
        // P.A.S.S. DISCHARGE COMBAT HUD
        // =========================================================================
        private void DrawDischargeHUD()
        {
            if (ExtinguisherController.Instance == null) return;

            // 1. Top Canister Pressure/Ammo Banner (Fortnite Style)
            float bannerW;
            float bx;
            float by;

            if (TacticalUITheme.VW >= 960f)
            {
                bannerW = Mathf.Min(440f, TacticalUITheme.VW - 560f);
                bx = (TacticalUITheme.VW - bannerW) * 0.5f;
                by = 16f;
            }
            else
            {
                bannerW = Mathf.Min(440f, TacticalUITheme.VW - 32f);
                bx = (TacticalUITheme.VW - bannerW) * 0.5f;
                by = TacticalUITheme.HudTopZoneClearY;
            }

            float charge = ExtinguisherController.Instance.RemainingCharge;
            int pct = Mathf.RoundToInt(charge * 100f);
            string pressureText = $"🧯 CANISTER PRESSURE: {pct}% (AIM BASE OF FLAME)";

            // This status line can wrap to 2 lines on a narrow banner — measure it instead of a
            // fixed 20px single-line guess, so the second line is never clipped by the card edge.
            const float topPad = 16f;
            const float labelToBar = 10f;
            const float barH = 14f;
            const float bottomPad = 14f;
            float textH = Mathf.Max(TacticalUITheme.CalcTextHeight(_headerStyle, pressureText, bannerW - 20f), 20f);
            float bannerH = Mathf.Max(topPad + textH + labelToBar + barH + bottomPad, 68f);

            Rect bannerRect = new Rect(bx, by, bannerW, bannerH);
            TacticalUITheme.DrawFortniteCard(bannerRect, TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavy, "/// P.A.S.S. COMBAT HUD ///", TacticalUITheme.FortniteBlue);

            GUI.Label(new Rect(bx + 10, by + topPad, bannerW - 20, textH), pressureText, _headerStyle);

            // Pressure gauge bar
            Rect gaugeBg = new Rect(bx + 16, by + topPad + textH + labelToBar, bannerW - 32, barH);
            Color chargeColor = charge > 0.4f ? TacticalUITheme.FortniteGreen : (charge > 0.15f ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed);
            TacticalUITheme.DrawFortniteBar(gaugeBg, charge, chargeColor, TacticalUITheme.CardSlotBg, 10);

            // 2. Futuristic Reticle Crosshairs (Center Screen)
            float cx = TacticalUITheme.VW * 0.5f;
            float cy = TacticalUITheme.VH * 0.5f;
            bool isDischarging = ExtinguisherController.Instance.IsDischarging;
            Color crossColor = isDischarging ? TacticalUITheme.FortniteGreen : TacticalUITheme.FortniteGold;

            TacticalUITheme.DrawCornerBrackets(new Rect(cx - 26, cy - 26, 52, 52), crossColor, 12f, 3f);
            TacticalUITheme.DrawRect(new Rect(cx - 3, cy - 3, 6, 6), crossColor);

            // Aim status + suppression progress (below reticle)
            var aimed = ExtinguisherController.Instance.AimedFire;
            bool inRange = ExtinguisherController.Instance.IsFireInRange;
            string aimText;
            Color aimColor;
            if (aimed == null)
            {
                aimText = "🎯 AIM AT THE BASE OF THE FIRE";
                aimColor = TacticalUITheme.FortniteAmber;
            }
            else if (!inRange)
            {
                aimText = "⬆ MOVE CLOSER — FIRE OUT OF NOZZLE RANGE";
                aimColor = TacticalUITheme.FortniteAmber;
            }
            else
            {
                aimText = "✅ ON TARGET — HOLD TRIGGER TO SUPPRESS";
                aimColor = TacticalUITheme.FortniteGreen;
            }

            var aimStyle = new GUIStyle(_headerStyle) { fontSize = 14, normal = { textColor = aimColor } };
            GUI.Label(new Rect(cx - 220, cy + 36, 440, 22), aimText, aimStyle);

            if (aimed != null)
            {
                float progress = aimed.ExtinguishProgress;
                float barW = Mathf.Min(300f, TacticalUITheme.VW - 60f);
                Rect barRect = new Rect(cx - barW * 0.5f, cy + 62, barW, 14);
                TacticalUITheme.DrawFortniteBar(barRect, progress, TacticalUITheme.FortniteGreen, TacticalUITheme.CardSlotBg, 10);

                float secondsLeft = (1f - progress) * ExtinguisherController.Instance.SecondsToExtinguish;
                var progStyle = new GUIStyle(_headerStyle) { fontSize = 12, normal = { textColor = TacticalUITheme.FortniteGreen } };
                GUI.Label(new Rect(cx - 220, cy + 78, 440, 18), $"FIRE SUPPRESSION {Mathf.RoundToInt(progress * 100f)}%  ·  ~{secondsLeft:F0}s OF SPRAY LEFT", progStyle);
            }

            // 3. Touch Squeeze Button (Bottom Center)
            float btnW = Mathf.Min(340f, TacticalUITheme.VW - 40f);
            float btnH = 68f;
            Rect triggerRect = new Rect((TacticalUITheme.VW - btnW) * 0.5f, TacticalUITheme.VH - btnH - 24, btnW, btnH);

            // Hold state is polled in Update() from the live touch/mouse state
            _triggerRect = triggerRect;
            _triggerDrawnFrame = Time.frameCount;

            Color trigAccent = isDischarging ? TacticalUITheme.FortniteRed : TacticalUITheme.FortniteBlue;
            string trigLabel = isDischarging ? "🔥 DISCHARGING WEAPON (HOLDING)" : "💥 HOLD TO SQUEEZE TRIGGER";
            TacticalUITheme.DrawFortniteButton(triggerRect, trigLabel, trigAccent, _triggerBtnStyle, isDischarging ? "[FIRING]" : "[DISCHARGE]");
        }

        private void DrawSuppressionSuccessModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(560f, TacticalUITheme.VW - 32f);
            float modalHeight = 340f;
            float mx = (TacticalUITheme.VW - modalWidth) * 0.5f;
            float my = (TacticalUITheme.VH - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            // Card with no ribbon tag — kept plain per request.
            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteGreen, TacticalUITheme.FortniteNavyDark);

            var victoryStyle = new GUIStyle(_headerStyle) { fontSize = 24, normal = { textColor = TacticalUITheme.FortniteGreen } };
            GUI.Label(new Rect(mx, my + 24, modalWidth, 32), "🏆 ALL FIRES SUPPRESSED! 🏆", victoryStyle);

            Rect infoBox = new Rect(mx + 25, my + 68, modalWidth - 50, 130);
            TacticalUITheme.DrawFortniteCard(infoBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);

            // Short confirmation only, per request (no detailed operation summary).
            string summary = "🎉 Great work! All fire has been extinguished.";

            GUI.Label(new Rect(infoBox.x + 14, infoBox.y + 14, infoBox.width - 28, 102), summary, _feedbackStyle);

            // Action Button
            Rect btnRect = new Rect(mx + 35, my + 225, modalWidth - 70, 60);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "🏆 PROCEED TO NEXT ➔", TacticalUITheme.FortniteGreen, _actionBtnStyle))
            {
                if (ARMiningSimulator.Investigation.InvestigationManager.Instance != null)
                {
                    ARMiningSimulator.Investigation.InvestigationManager.Instance.EndSimulationAndShowScorecard();
                }
            }
        }

        private GUIStyle _warningStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = TacticalUITheme.FortniteRed }
            };
        }
    }
}
