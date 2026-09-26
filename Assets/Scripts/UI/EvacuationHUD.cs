using UnityEngine;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Extinguisher;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 7 Evacuation HUD — Fortnite Compass & Navigation Style.
    /// Features:
    /// - Dynamic top-screen battle compass navigation banner with distance and turn angles.
    /// - Chunky segmented countdown bar with danger flash.
    /// - Fortnite Victory Royale style Safe Area arrival celebration card.
    /// - Collision-free positioning aligned with the Fortnite UI system.
    /// </summary>
    public class EvacuationHUD : MonoBehaviour
    {
        private GUIStyle _bannerHeaderStyle;
        private GUIStyle _compassPromptStyle;
        private GUIStyle _metricsStyle;
        private GUIStyle _modalHeaderStyle;
        private GUIStyle _modalSubStyle;
        private GUIStyle _actionBtnStyle;

        private void OnGUI()
        {
            if (EvacuationManager.Instance == null) return;

            InitStyles();

            // Suppress evacuation banner if a decision or scorecard modal is open
            if (TacticalUITheme.IsAnyModalOpen() && !EvacuationManager.Instance.IsSafeZoneReached)
            {
                return;
            }

            if (EvacuationManager.Instance.IsEvacuating)
            {
                DrawEvacuationBanner();
            }
            else if (EvacuationManager.Instance.IsSafeZoneReached)
            {
                // Only show Safe Area arrival modal if Stage 3 has not yet begun
                if (FireResponseManager.Instance == null ||
                    FireResponseManager.Instance.State == FireResponseState.Inactive)
                {
                    DrawSafeZoneArrivalModal();
                }
            }
        }

        private void InitStyles()
        {
            if (_bannerHeaderStyle == null)
            {
                _bannerHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = TacticalUITheme.FortniteGreen }
                };
            }

            if (_compassPromptStyle == null)
            {
                _compassPromptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = TacticalUITheme.FortniteGold }
                };
            }

            if (_metricsStyle == null)
            {
                _metricsStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.85f, 0.92f, 1.0f) }
                };
            }

            if (_modalHeaderStyle == null)
            {
                _modalHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = TacticalUITheme.FortniteGreen }
                };
            }

            if (_modalSubStyle == null)
            {
                _modalSubStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.88f, 0.92f, 0.98f) }
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

        private void DrawEvacuationBanner()
        {
            float bannerWidth;
            float bx;
            float by;

            if (Screen.width >= 960f)
            {
                bannerWidth = Mathf.Min(450f, Screen.width - 560f);
                bx = (Screen.width - bannerWidth) * 0.5f;
                by = 16f;
            }
            else
            {
                bannerWidth = Mathf.Min(450f, Screen.width - 32f);
                bx = (Screen.width - bannerWidth) * 0.5f;
                by = 86f;
            }

            float bannerHeight = 94f;
            Rect bannerRect = new Rect(bx, by, bannerWidth, bannerHeight);

            TacticalUITheme.DrawFortniteCard(bannerRect, TacticalUITheme.FortniteGreen, TacticalUITheme.FortniteNavy, "/// ESCAPE ROUTE ACTIVE ///", TacticalUITheme.FortniteGreen);

            // Compute turn angle & live direction to Safe Zone
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            string directionPrompt = "🟢 FOLLOW GREEN ARROWS TO EMERGENCY REFUGE";
            if (cam != null)
            {
                Vector3 toSafe = EvacuationManager.Instance.SafeZonePosition - cam.position;
                toSafe.y = 0;
                Vector3 fwd = cam.forward;
                fwd.y = 0;
                if (toSafe.sqrMagnitude > 0.05f && fwd.sqrMagnitude > 0.05f)
                {
                    float angle = Vector3.SignedAngle(fwd, toSafe, Vector3.up);
                    if (Mathf.Abs(angle) <= 25f)
                        directionPrompt = "⬆️ STRAIGHT AHEAD — SPRINT TO SAFE REFUGE";
                    else if (angle < -25f && angle >= -115f)
                        directionPrompt = $"⬅️ TURN LEFT ({Mathf.Abs(angle):F0}°) TOWARDS SAFE REFUGE";
                    else if (angle > 25f && angle <= 115f)
                        directionPrompt = $"➡️ TURN RIGHT ({angle:F0}°) TOWARDS SAFE REFUGE";
                    else
                        directionPrompt = "🔄 TURN AROUND — REFUGE IS BEHIND YOU";
                }
            }

            GUI.Label(new Rect(bx + 10, by + 16, bannerWidth - 20, 22), directionPrompt, _compassPromptStyle);

            // Distance & countdown metrics
            float dist = EvacuationManager.Instance.DistanceToSafeZone;
            float timeRem = EvacuationManager.Instance.TimeRemaining;
            float total = EvacuationManager.Instance.CountdownLimit;
            float ratio = Mathf.Clamp01(timeRem / total);

            GUI.Label(new Rect(bx + 10, by + 40, bannerWidth - 20, 20),
                $"📍 DISTANCE: {dist:F1}m  |  ⏱️ EVAC TIME: {timeRem:F1}s", _metricsStyle);

            // Chunky segmented gauge bar
            Rect barRect = new Rect(bx + 16, by + 64, bannerWidth - 32, 14);
            Color barColor = ratio > 0.5f ? TacticalUITheme.FortniteGreen : (ratio > 0.25f ? TacticalUITheme.FortniteAmber : TacticalUITheme.FortniteRed);
            TacticalUITheme.DrawFortniteBar(barRect, ratio, barColor, TacticalUITheme.CardSlotBg, 10);
        }

        private void DrawSafeZoneArrivalModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.09f, 0.90f));

            float modalWidth = Mathf.Min(560f, Screen.width - 32f);
            float modalHeight = 350f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteGreen, TacticalUITheme.FortniteNavyDark, "/// REFUGE REACHED ///", TacticalUITheme.FortniteGold);

            GUI.Label(new Rect(mx, my + 22, modalWidth, 32), "🏆 SAFE ZONE REACHED! 🏆", _modalHeaderStyle);

            float timeTaken = EvacuationManager.Instance.EvacuationTimer;
            int bonus = EvacuationManager.Instance.SpeedBonusScore;
            string rating = timeTaken < 10f ? "EXEMPLARY (MAX SPEED BONUS)" : (timeTaken < 20f ? "QUALIFIED" : "ACCEPTABLE");

            Rect statBox = new Rect(mx + 25, my + 64, modalWidth - 50, 150);
            TacticalUITheme.DrawFortniteCard(statBox, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);

            string statsText = $"Drill Evacuation Time: {timeTaken:F1}s\n" +
                               $"Speed Rating: {rating}\n" +
                               $"Speed Bonus Earned: +{bonus} PTS\n\n" +
                               "Trainee has successfully evacuated the active fire compartment into the designated fresh-air Safe Zone.";

            GUI.Label(new Rect(statBox.x + 14, statBox.y + 14, statBox.width - 28, 122), statsText, _modalSubStyle);

            // Action Button
            Rect btnRect = new Rect(mx + 35, my + 235, modalWidth - 70, 60);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "ASSESS FIRE SIZE (DECISION 3) ➔", TacticalUITheme.FortniteGreen, _actionBtnStyle, "[CONTINUE]"))
            {
                if (FireResponseManager.Instance != null)
                {
                    FireResponseManager.Instance.StartStage3A();
                }
            }
        }
    }
}
