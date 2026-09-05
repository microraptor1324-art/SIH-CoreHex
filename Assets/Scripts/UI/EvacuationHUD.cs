using UnityEngine;
using ARMiningSimulator.Evacuation;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 7 Evacuation HUD.
    /// Displays:
    /// - Dynamic top evacuation banner with distance counter and time-remaining bar.
    /// - Success arrival modal with speed bonus score and progression button.
    /// </summary>
    public class EvacuationHUD : MonoBehaviour
    {
        private Texture2D _whiteTexture;
        private GUIStyle _headerStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _modalHeaderStyle;
        private GUIStyle _modalSubStyle;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void OnGUI()
        {
            if (EvacuationManager.Instance == null) return;

            InitStyles();

            if (EvacuationManager.Instance.IsEvacuating)
            {
                DrawEvacuationBanner();
            }
            else if (EvacuationManager.Instance.IsSafeZoneReached)
            {
                DrawSafeZoneArrivalModal();
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
            }

            if (_statusStyle == null)
            {
                _statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter
                };
                _statusStyle.normal.textColor = Color.white;
            }

            if (_modalHeaderStyle == null)
            {
                _modalHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _modalHeaderStyle.normal.textColor = new Color(0.2f, 1.0f, 0.45f);
            }

            if (_modalSubStyle == null)
            {
                _modalSubStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                _modalSubStyle.normal.textColor = new Color(0.88f, 0.92f, 0.96f);
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _buttonStyle.normal.textColor = Color.white;
            }
        }

        private void DrawEvacuationBanner()
        {
            float bannerWidth = Mathf.Min(480f, Screen.width - 40f);
            float bannerHeight = 85f;
            float bx = (Screen.width - bannerWidth) * 0.5f;
            float by = 20f;

            // Translucent dark card with amber warning tint
            DrawColorRect(new Rect(bx, by, bannerWidth, bannerHeight), new Color(0.08f, 0.10f, 0.14f, 0.92f));

            // Pulsing header text
            float pulse = Mathf.PingPong(Time.time * 4f, 0.35f);
            _headerStyle.normal.textColor = new Color(1f, 0.65f + pulse, 0.1f);
            GUI.Label(new Rect(bx, by + 10, bannerWidth, 24), "🏃 EVACUATE TO SAFE ZONE NOW!", _headerStyle);

            // Distance and time status
            float dist = EvacuationManager.Instance.DistanceToSafeZone;
            float timeRem = EvacuationManager.Instance.TimeRemaining;
            GUI.Label(new Rect(bx, by + 36, bannerWidth, 20),
                $"📍 Distance: {dist:F1}m  |  ⏱️ Time Remaining: {timeRem:F1}s", _statusStyle);

            // Countdown progress bar
            float total = EvacuationManager.Instance.CountdownLimit;
            float ratio = Mathf.Clamp01(timeRem / total);
            Rect barBg = new Rect(bx + 25, by + 62, bannerWidth - 50, 8);
            DrawColorRect(barBg, new Color(0.18f, 0.22f, 0.28f));

            Color barColor = ratio > 0.5f ? new Color(0.2f, 0.85f, 0.4f) : (ratio > 0.25f ? new Color(1f, 0.7f, 0.1f) : new Color(1f, 0.25f, 0.25f));
            DrawColorRect(new Rect(barBg.x, barBg.y, barBg.width * ratio, 8), barColor);
        }

        private void DrawSafeZoneArrivalModal()
        {
            // Dim background overlay
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.04f, 0.05f, 0.08f, 0.80f));

            float modalWidth = Mathf.Min(500f, Screen.width - 40f);
            float modalHeight = 310f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            // Main Card
            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.10f, 0.13f, 0.18f, 0.96f));

            // Glowing border header
            GUI.Label(new Rect(mx, my + 25, modalWidth, 32), "🛡️ EVACUATION SUCCESSFUL!", _modalHeaderStyle);

            float timeTaken = EvacuationManager.Instance.EvacuationTimer;
            int bonus = EvacuationManager.Instance.SpeedBonusScore;

            string statsText = $"Evacuation Time: {timeTaken:F1} seconds\n" +
                               $"Speed Rating: {(timeTaken < 10f ? "EXEMPLARY" : (timeTaken < 20f ? "ADEQUATE" : "ACCEPTABLE"))}\n" +
                               $"Evacuation Bonus: +{bonus} PTS\n\n" +
                               "Trainee safely exited the active fire compartment into the designated fresh-air Safe Zone.";

            Rect statBox = new Rect(mx + 25, my + 70, modalWidth - 50, 140);
            DrawColorRect(statBox, new Color(0.06f, 0.08f, 0.12f, 0.85f));
            GUI.Label(new Rect(statBox.x + 10, statBox.y + 12, statBox.width - 20, 116), statsText, _modalSubStyle);

            // Continue button
            GUI.backgroundColor = new Color(0.18f, 0.75f, 0.35f);
            if (GUI.Button(new Rect(mx + 35, my + 230, modalWidth - 70, 52), "CONTINUE TO FIRE RESPONSE (PHASE 8) ➔", _buttonStyle))
            {
                Debug.Log("[EvacuationHUD] Transitioning from Phase 7 (Evacuation) to Phase 8 (Fire Response).");
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
