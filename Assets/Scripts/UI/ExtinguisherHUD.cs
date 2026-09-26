using UnityEngine;
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
        private GUIStyle _actionBtnStyle;
        private GUIStyle _triggerBtnStyle;
        private GUIStyle _feedbackStyle;

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

            if (_actionBtnStyle == null)
            {
                _actionBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
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

        private void DrawStage3A_SizeEvalModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, Screen.width - 32f);
            float modalHeight = 350f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavyDark, "/// DECISION 3A: FIRE SIZE ASSESSMENT ///", TacticalUITheme.FortniteBlue);

            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 26), "DECISION 3: FIRE SIZE EVALUATION", _headerStyle);

            Rect promptBox = new Rect(mx + 25, my + 48, modalWidth - 50, 48);
            TacticalUITheme.DrawFortniteCard(promptBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(promptBox.x + 10, promptBox.y + 6, promptBox.width - 20, promptBox.height - 12),
                "At the Safe Refuge, evaluate the hazard before taking your tactical action:\nIs this hazard a Small (Incipient) or Big (Critical) fire?", _questionStyle);

            float growthLimit = Fire.FireManager.Instance != null ? Fire.FireManager.Instance.FireGrowthTime : 60f;

            // Option 1: Small Fire
            Rect opt1Rect = new Rect(mx + 25, my + 112, modalWidth - 50, 64);
            string opt1Text = "🕯️ 1) SMALL INCIPIENT FIRE\n    Localized equipment flame; unblocked retreat path; fightable with extinguisher.";
            if (TacticalUITheme.DrawFortniteButton(opt1Rect, opt1Text, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[OPTION 1]"))
            {
                FireResponseManager.Instance.SubmitFireSizeAssessment(false);
            }

            // Option 2: Big Fire
            Rect opt2Rect = new Rect(mx + 25, my + 190, modalWidth - 50, 64);
            string opt2Text = "🔥 2) BIG ESCALATED INFERNO\n    Large roaring flames, rolling toxic smoke; unmanageable by portable canister.";
            if (TacticalUITheme.DrawFortniteButton(opt2Rect, opt2Text, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[OPTION 2]"))
            {
                FireResponseManager.Instance.SubmitFireSizeAssessment(true);
            }
        }

        private void DrawStage3A_ActionChoiceModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, Screen.width - 32f);
            float modalHeight = 350f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            bool isBig = FireResponseManager.Instance.IsActuallyBig;

            Color accentCol = TacticalUITheme.FortnitePurple;
            TacticalUITheme.DrawFortniteCard(modalRect, accentCol, TacticalUITheme.FortniteNavyDark, "/// TACTICAL ACTION CHOICE ///", accentCol);

            string sizeTitle = "🧯 TACTICAL ACTION PROTOCOL";
            _headerStyle.normal.textColor = TacticalUITheme.FortniteGold;
            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 26), sizeTitle, _headerStyle);

            string promptText = isBig
                ? "The fire has been assessed. Dense smoke and rapid heat release are present.\nWhat tactical life-safety action should you take?"
                : "The fire has been assessed. Portable fire extinguishers are positioned in this zone.\nWhat tactical life-safety action should you take?";

            Rect promptBox = new Rect(mx + 25, my + 48, modalWidth - 50, 52);
            TacticalUITheme.DrawFortniteCard(promptBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(promptBox.x + 10, promptBox.y + 6, promptBox.width - 20, promptBox.height - 12), promptText, _questionStyle);

            // Choice 1: Fight
            Rect opt1Rect = new Rect(mx + 25, my + 114, modalWidth - 50, 64);
            string opt1Text = "⚔️ 1) FIGHT WITH EXTINGUISHER (ATTACK)\n    Take portable extinguisher from Safe Area, verify clear retreat route, suppress fire.";
            if (TacticalUITheme.DrawFortniteButton(opt1Rect, opt1Text, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[OPTION 1]"))
            {
                FireResponseManager.Instance.SubmitTacticalAction(true);
            }

            // Choice 2: Stay in safety
            Rect opt2Rect = new Rect(mx + 25, my + 192, modalWidth - 50, 64);
            string opt2Text = "🛡️ 2) STAY IN REFUGE & EVACUATE MINE (DEFEND)\n    Do not attack. Seal refuge chamber, notify surface rescue, await mine rescue team.";
            if (TacticalUITheme.DrawFortniteButton(opt2Rect, opt2Text, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[OPTION 2]"))
            {
                FireResponseManager.Instance.SubmitTacticalAction(false);
            }
        }

        private void DrawStage3B_AgentModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, Screen.width - 32f);
            float modalHeight = 420f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteGold, TacticalUITheme.FortniteNavyDark, "/// DECISION 3B: LOADOUT AGENT SELECTION ///", TacticalUITheme.FortniteGold);

            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 26), "SELECT EXTINGUISHING WEAPON AGENT", _headerStyle);

            Rect promptBox = new Rect(mx + 25, my + 48, modalWidth - 50, 48);
            TacticalUITheme.DrawFortniteCard(promptBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(promptBox.x + 10, promptBox.y + 6, promptBox.width - 20, promptBox.height - 12),
                "Electrical equipment in underground mine is on fire (ENERGIZED CIRCUIT).\nSelect the correct suppression loadout:", _questionStyle);

            float optY = my + 110f;
            float optH = 64f;
            float optGap = 12f;

            // 1) CO2
            Rect btnCO2 = new Rect(mx + 25, optY, modalWidth - 50, optH);
            string co2Desc = "⚡ 1) CO2 CARBON DIOXIDE (BLACK BAND)\n    Displaces oxygen with non-conductive gas blanket. Leaves zero equipment residue.";
            if (TacticalUITheme.DrawFortniteButton(btnCO2, co2Desc, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[AGENT A]"))
            {
                FireResponseManager.Instance.SubmitExtinguisherSelection(ExtinguisherType.CO2);
            }

            // 2) ABC Dry Chemical Powder
            Rect btnDry = new Rect(mx + 25, optY + optH + optGap, modalWidth - 50, optH);
            string dryDesc = "🔵 2) ABC DRY CHEMICAL POWDER (BLUE BAND)\n    Smothers flame with fine non-conductive chemical powder barrier.";
            if (TacticalUITheme.DrawFortniteButton(btnDry, dryDesc, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[AGENT B]"))
            {
                FireResponseManager.Instance.SubmitExtinguisherSelection(ExtinguisherType.DryPowder);
            }

            // 3) Water / Foam
            Rect btnFoam = new Rect(mx + 25, optY + (optH + optGap) * 2, modalWidth - 50, optH);
            string foamDesc = "🔴 3) AFFF FOAM / WATER (RED / CREAM BAND)\n    Suppresses flame by surface aqueous film formation and thermal cooling.";
            if (TacticalUITheme.DrawFortniteButton(btnFoam, foamDesc, TacticalUITheme.FortniteBlue, _optionBtnStyle, "[AGENT C]"))
            {
                FireResponseManager.Instance.SubmitExtinguisherSelection(ExtinguisherType.WaterFoam);
            }
        }

        private void DrawFeedbackModal(string nextButtonText, System.Action onNext, bool isCorrect)
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(580f, Screen.width - 32f);
            float modalHeight = 360f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            Color outcomeColor = isCorrect ? TacticalUITheme.FortniteGreen : TacticalUITheme.FortniteRed;
            string ribbonTag = isCorrect ? "/// TACTICAL ACTION VERIFIED ///" : "/// SAFETY HAZARD LOGGED ///";
            TacticalUITheme.DrawFortniteCard(modalRect, outcomeColor, TacticalUITheme.FortniteNavyDark, ribbonTag, outcomeColor);

            string title = isCorrect ? "🏆 TACTICAL ACTION VERIFIED!" : "⚠️ SAFETY HAZARD LOGGED!";
            var titleStyle = new GUIStyle(_headerStyle) { normal = { textColor = outcomeColor }, fontSize = 20 };
            GUI.Label(new Rect(mx + 20, my + 18, modalWidth - 40, 28), title, titleStyle);

            string feedback = FireResponseManager.Instance.LastFeedbackText;
            Rect expBox = new Rect(mx + 25, my + 54, modalWidth - 50, 180);
            TacticalUITheme.DrawFortniteCard(expBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);
            GUI.Label(new Rect(expBox.x + 14, expBox.y + 12, expBox.width - 28, expBox.height - 24), feedback, _feedbackStyle);

            Rect nextBtnRect = new Rect(mx + 30, my + 265, modalWidth - 60, 60);
            if (TacticalUITheme.DrawFortniteButton(nextBtnRect, nextButtonText, outcomeColor, _actionBtnStyle, isCorrect ? "[PROCEED]" : "[RE-EVALUATE]"))
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

            if (Screen.width >= 960f)
            {
                bannerW = Mathf.Min(440f, Screen.width - 560f);
                bx = (Screen.width - bannerW) * 0.5f;
                by = 16f;
            }
            else
            {
                bannerW = Mathf.Min(440f, Screen.width - 32f);
                bx = (Screen.width - bannerW) * 0.5f;
                by = 86f;
            }

            Rect bannerRect = new Rect(bx, by, bannerW, 68f);
            TacticalUITheme.DrawFortniteCard(bannerRect, TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavy, "/// P.A.S.S. COMBAT HUD ///", TacticalUITheme.FortniteBlue);

            float charge = ExtinguisherController.Instance.RemainingCharge;
            int pct = Mathf.RoundToInt(charge * 100f);
            GUI.Label(new Rect(bx + 10, by + 16, bannerW - 20, 20), $"🧯 CANISTER PRESSURE: {pct}% (AIM BASE OF FLAME)", _headerStyle);

            // Pressure gauge bar
            Rect gaugeBg = new Rect(bx + 16, by + 40, bannerW - 32, 14);
            Color chargeColor = charge > 0.4f ? TacticalUITheme.FortniteGreen : (charge > 0.15f ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed);
            TacticalUITheme.DrawFortniteBar(gaugeBg, charge, chargeColor, TacticalUITheme.CardSlotBg, 10);

            // 2. Futuristic Reticle Crosshairs (Center Screen)
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            bool isDischarging = ExtinguisherController.Instance.IsDischarging;
            Color crossColor = isDischarging ? TacticalUITheme.FortniteGreen : TacticalUITheme.FortniteGold;

            TacticalUITheme.DrawCornerBrackets(new Rect(cx - 26, cy - 26, 52, 52), crossColor, 12f, 3f);
            TacticalUITheme.DrawRect(new Rect(cx - 3, cy - 3, 6, 6), crossColor);

            // 3. Touch Squeeze Button (Bottom Center)
            float btnW = Mathf.Min(340f, Screen.width - 40f);
            float btnH = 68f;
            Rect triggerRect = new Rect((Screen.width - btnW) * 0.5f, Screen.height - btnH - 24, btnW, btnH);

            bool isHeld = Event.current.type == EventType.MouseDown && triggerRect.Contains(Event.current.mousePosition) ||
                          Event.current.type == EventType.MouseDrag && triggerRect.Contains(Event.current.mousePosition);

            ExtinguisherController.Instance.SetExternalTrigger(isHeld);

            Color trigAccent = isDischarging ? TacticalUITheme.FortniteRed : TacticalUITheme.FortniteBlue;
            string trigLabel = isDischarging ? "🔥 DISCHARGING WEAPON (HOLDING)" : "💥 HOLD TO SQUEEZE TRIGGER";
            TacticalUITheme.DrawFortniteButton(triggerRect, trigLabel, trigAccent, _triggerBtnStyle, isDischarging ? "[FIRING]" : "[DISCHARGE]");
        }

        private void DrawSuppressionSuccessModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(560f, Screen.width - 32f);
            float modalHeight = 340f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteGreen, TacticalUITheme.FortniteNavyDark, "/// VICTORY ROYALE ///", TacticalUITheme.FortniteGold);

            var victoryStyle = new GUIStyle(_headerStyle) { fontSize = 24, normal = { textColor = TacticalUITheme.FortniteGreen } };
            GUI.Label(new Rect(mx, my + 24, modalWidth, 32), "🏆 ALL FIRES SUPPRESSED! 🏆", victoryStyle);

            Rect infoBox = new Rect(mx + 25, my + 68, modalWidth - 50, 130);
            TacticalUITheme.DrawFortniteCard(infoBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);

            string summary = "Extinguisher Operation: SUCCESSFUL (+150 BONUS PTS)\n\n" +
                             "All localized equipment fires have been thoroughly suppressed! Active combustion and toxic smoke generation have ceased.\n" +
                             "Drill complete! Review your official performance scorecard and marks below.";

            GUI.Label(new Rect(infoBox.x + 14, infoBox.y + 14, infoBox.width - 28, 102), summary, _feedbackStyle);

            // Action Button
            Rect btnRect = new Rect(mx + 35, my + 225, modalWidth - 70, 60);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "🏆 VIEW FINAL MARKS & SCORECARD ➔", TacticalUITheme.FortniteGreen, _actionBtnStyle, "[SCORECARD]"))
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
