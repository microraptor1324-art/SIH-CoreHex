using UnityEngine;
using ARMiningSimulator.Extinguisher;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 8 Extinguisher & Fire Response HUD.
    /// Manages:
    /// - Decision Stage 3A: Fire Size Assessment modal
    /// - Decision Stage 3B: Extinguisher Agent Selection modal
    /// - P.A.S.S. interactive discharge controls (canister pressure gauge & squeeze trigger)
    /// - Fire suppression celebration modal
    /// </summary>
    public class ExtinguisherHUD : MonoBehaviour
    {
        private Texture2D _whiteTexture;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _questionStyle;
        private GUIStyle _optionBtnStyle;
        private GUIStyle _actionBtnStyle;
        private GUIStyle _triggerBtnStyle;

        private void Awake()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void OnGUI()
        {
            if (FireResponseManager.Instance == null) return;

            InitStyles();

            FireResponseState state = FireResponseManager.Instance.State;

            if (state == FireResponseState.Stage3A_FireSizeEval)
            {
                if (FireResponseManager.Instance.IsShowingFeedback)
                    DrawFeedbackModal("PROCEED TO EXTINGUISHER SELECTION ➔", () => FireResponseManager.Instance.ProceedToAgentSelection());
                else
                    DrawStage3AModal();
            }
            else if (state == FireResponseState.Stage3B_AgentSelection)
            {
                if (FireResponseManager.Instance.IsShowingFeedback)
                    DrawFeedbackModal("PROCEED TO P.A.S.S. DISCHARGE ➔", () => FireResponseManager.Instance.ProceedToDischarge());
                else
                    DrawStage3BModal();
            }
            else if (state == FireResponseState.PASS_Discharge)
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

            if (_triggerBtnStyle == null)
            {
                _triggerBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _triggerBtnStyle.normal.textColor = Color.white;
            }
        }

        private void DrawStage3AModal()
        {
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.80f));

            float modalWidth = Mathf.Min(560f, Screen.width - 30f);
            float modalHeight = 310f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.12f, 0.16f, 0.96f));

            GUI.Label(new Rect(mx, my + 15, modalWidth, 24), "⚠️ DECISION STAGE 3A: FIRE SIZE ASSESSMENT", _headerStyle);
            GUI.Label(new Rect(mx + 25, my + 50, modalWidth - 50, 50),
                "You have reached the Safe Zone and are observing the fire. Can this fire be attacked with portable extinguishers?", _questionStyle);

            // Option 1: Yes
            GUI.backgroundColor = new Color(0.18f, 0.32f, 0.45f);
            if (GUI.Button(new Rect(mx + 25, my + 115, modalWidth - 50, 60),
                "  🧯 YES — Incipient (Small) fire with clear retreat path.\n     Attack using portable fire extinguisher.", _optionBtnStyle))
            {
                FireResponseManager.Instance.SubmitFireSizeAssessment(true);
            }

            // Option 2: No
            GUI.backgroundColor = new Color(0.40f, 0.20f, 0.22f);
            if (GUI.Button(new Rect(mx + 25, my + 190, modalWidth - 50, 60),
                "  🛑 NO — Fire is Medium or Large with heavy smoke.\n     Maintain distance and summon specialized Mine Rescue.", _optionBtnStyle))
            {
                FireResponseManager.Instance.SubmitFireSizeAssessment(false);
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawStage3BModal()
        {
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.80f));

            float modalWidth = Mathf.Min(560f, Screen.width - 30f);
            float modalHeight = 370f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.12f, 0.16f, 0.96f));

            GUI.Label(new Rect(mx, my + 15, modalWidth, 24), "🧯 DECISION STAGE 3B: SELECT EXTINGUISHER AGENT", _headerStyle);
            GUI.Label(new Rect(mx + 25, my + 46, modalWidth - 50, 44),
                "Identify the burning equipment and select the correct extinguishing agent from the locker:", _questionStyle);

            // 1. CO2
            GUI.backgroundColor = new Color(0.15f, 0.18f, 0.22f);
            if (GUI.Button(new Rect(mx + 25, my + 100, modalWidth - 50, 58),
                "  ⬛ Carbon Dioxide (CO2 - Black Band)\n     Non-conductive, clean gas. Ideal for electrical equipment.", _optionBtnStyle))
            {
                FireResponseManager.Instance.SubmitExtinguisherSelection(ExtinguisherType.CO2);
            }

            // 2. Dry Powder
            GUI.backgroundColor = new Color(0.16f, 0.28f, 0.44f);
            if (GUI.Button(new Rect(mx + 25, my + 170, modalWidth - 50, 58),
                "  🟦 ABC Dry Chemical Powder (Blue Band)\n     Multi-purpose smothering powder. Works on machinery & fuel.", _optionBtnStyle))
            {
                FireResponseManager.Instance.SubmitExtinguisherSelection(ExtinguisherType.DryPowder);
            }

            // 3. Water / Foam
            GUI.backgroundColor = new Color(0.35f, 0.28f, 0.18f);
            if (GUI.Button(new Rect(mx + 25, my + 240, modalWidth - 50, 58),
                "  🟨 AFFF Aqueous Foam (Cream Band)\n     Liquid foam blanket. Smothers fuel. DANGEROUS on electrical!", _optionBtnStyle))
            {
                FireResponseManager.Instance.SubmitExtinguisherSelection(ExtinguisherType.WaterFoam);
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawFeedbackModal(string nextButtonText, System.Action onNext)
        {
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.80f));

            float modalWidth = Mathf.Min(560f, Screen.width - 30f);
            float modalHeight = 330f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.12f, 0.16f, 0.96f));

            GUI.Label(new Rect(mx, my + 20, modalWidth, 26), "📋 Protocol Evaluation", _headerStyle);

            Rect expBox = new Rect(mx + 25, my + 60, modalWidth - 50, 165);
            DrawColorRect(expBox, new Color(0.06f, 0.08f, 0.11f, 0.85f));

            var feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            feedbackStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(expBox.x + 15, expBox.y + 15, expBox.width - 30, expBox.height - 30),
                FireResponseManager.Instance.LastFeedbackText, feedbackStyle);

            GUI.backgroundColor = new Color(0.18f, 0.75f, 0.35f);
            if (GUI.Button(new Rect(mx + 35, my + 245, modalWidth - 70, 55), nextButtonText, _actionBtnStyle))
            {
                onNext?.Invoke();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawDischargeHUD()
        {
            if (ExtinguisherController.Instance == null) return;

            // 1. Top PASS banner
            float bannerW = Mathf.Min(500f, Screen.width - 30f);
            float bx = (Screen.width - bannerW) * 0.5f;
            Rect topBanner = new Rect(bx, 15, bannerW, 60);
            DrawColorRect(topBanner, new Color(0.08f, 0.10f, 0.14f, 0.90f));

            GUI.color = new Color(0.3f, 1f, 0.5f);
            GUI.Label(new Rect(bx, 20, bannerW, 22), "🧯 P.A.S.S.: AIM AT BASE OF FLAME & SQUEEZE", _headerStyle);
            GUI.color = Color.white;

            // Pressure gauge bar
            float charge = ExtinguisherController.Instance.RemainingCharge;
            Rect gaugeBg = new Rect(bx + 20, 48, bannerW - 40, 8);
            DrawColorRect(gaugeBg, new Color(0.2f, 0.22f, 0.28f));
            Color chargeColor = charge > 0.4f ? new Color(0.2f, 0.85f, 0.4f) : new Color(0.9f, 0.25f, 0.2f);
            DrawColorRect(new Rect(gaugeBg.x, gaugeBg.y, gaugeBg.width * charge, 8), chargeColor);

            // 2. Aiming Crosshairs
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            Color crossColor = ExtinguisherController.Instance.IsDischarging ? new Color(0.2f, 1f, 0.4f, 0.8f) : new Color(1f, 1f, 1f, 0.6f);
            DrawColorRect(new Rect(cx - 15, cy - 1, 30, 2), crossColor);
            DrawColorRect(new Rect(cx - 1, cy - 15, 2, 30), crossColor);

            // 3. Touch Squeeze Button (Bottom Center/Right)
            float btnW = 280f;
            float btnH = 65f;
            Rect triggerRect = new Rect((Screen.width - btnW) * 0.5f, Screen.height - btnH - 25, btnW, btnH);

            bool isHeld = Event.current.type == EventType.MouseDown && triggerRect.Contains(Event.current.mousePosition) ||
                         Event.current.type == EventType.MouseDrag && triggerRect.Contains(Event.current.mousePosition);

            // Set external trigger status
            ExtinguisherController.Instance.SetExternalTrigger(isHeld);

            GUI.backgroundColor = ExtinguisherController.Instance.IsDischarging ? new Color(0.85f, 0.2f, 0.2f) : new Color(0.2f, 0.65f, 0.9f);
            GUI.Button(triggerRect, "💥 HOLD TO SQUEEZE", _triggerBtnStyle);
            GUI.backgroundColor = Color.white;
        }

        private void DrawSuppressionSuccessModal()
        {
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.82f));

            float modalWidth = Mathf.Min(500f, Screen.width - 30f);
            float modalHeight = 280f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.14f, 0.18f, 0.96f));

            var successStyle = new GUIStyle(_headerStyle) { fontSize = 22, normal = { textColor = new Color(0.2f, 1f, 0.45f) } };
            GUI.Label(new Rect(mx, my + 25, modalWidth, 32), "🎉 FIRE FULLY SUPPRESSED!", successStyle);

            Rect infoBox = new Rect(mx + 25, my + 70, modalWidth - 50, 110);
            DrawColorRect(infoBox, new Color(0.06f, 0.08f, 0.11f, 0.85f));

            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(infoBox.x + 10, infoBox.y + 10, infoBox.width - 20, 90),
                $"Extinguisher Operation: SUCCESS (+150 PTS)\n" +
                $"Active flames eliminated using the P.A.S.S. technique.\n" +
                "Area secured. Ready to begin root cause investigation.", labelStyle);

            GUI.backgroundColor = new Color(0.18f, 0.75f, 0.35f);
            if (GUI.Button(new Rect(mx + 35, my + 200, modalWidth - 70, 55), "PROCEED TO INVESTIGATION (PHASE 9) ➔", _actionBtnStyle))
            {
                Debug.Log("[ExtinguisherHUD] Completed Phase 8. Ready for Phase 9.");
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
