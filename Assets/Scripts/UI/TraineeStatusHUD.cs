using UnityEngine;
using ARMiningSimulator.Player;
using ARMiningSimulator.Fire;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 5 Trainee Status HUD.
    /// Displays:
    /// - 100 HP Health Bar (smooth color transitions: Green -> Orange -> Red).
    /// - Reaction Stopwatch (time from fire ignition to detection).
    /// - Target Lock-on Reticle (animating brackets while aiming at fire).
    /// - Damage Vignette / Low Health Heartbeat Pulse.
    /// - Incapacitated / Game Over modal with instant retry.
    /// </summary>
    public class TraineeStatusHUD : MonoBehaviour
    {
        private Texture2D _whiteTexture;
        private GUIStyle _boldHeaderStyle;
        private GUIStyle _subLabelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _dangerBoxStyle;

        private float _displayHealth = 100f;
        private float _healthSmoothVelocity = 0f;

        private void Awake()
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void Update()
        {
            if (TraineeHealth.Instance != null)
            {
                _displayHealth = Mathf.SmoothDamp(_displayHealth, TraineeHealth.Instance.CurrentHealth, ref _healthSmoothVelocity, 0.12f);
            }
        }

        private void OnGUI()
        {
            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage == ARMiningSimulator.Core.SimulationStage.ScanningRoom)
            {
                return;
            }

            InitStyles();

            DrawDamageVignette();
            DrawHealthBar();
            DrawDetectionAndStopwatch();
            DrawTargetingReticle();
            DrawIncapacitatedModal();
        }

        private void InitStyles()
        {
            if (_boldHeaderStyle == null)
            {
                _boldHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
                _boldHeaderStyle.normal.textColor = Color.white;
            }

            if (_subLabelStyle == null)
            {
                _subLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft
                };
                _subLabelStyle.normal.textColor = new Color(0.85f, 0.88f, 0.92f);
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

            if (_dangerBoxStyle == null)
            {
                _dangerBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }

        private void DrawHealthBar()
        {
            float currentHp = TraineeHealth.Instance != null ? _displayHealth : 100f;
            float maxHp = TraineeHealth.Instance != null ? TraineeHealth.Instance.MaxHealth : 100f;
            float healthRatio = Mathf.Clamp01(currentHp / maxHp);

            // Container dimensions
            float boxWidth = 300f;
            float boxHeight = 70f;
            Rect containerRect = new Rect(15, 15, boxWidth, boxHeight);

            // Semi-transparent dark background card
            DrawColorRect(containerRect, new Color(0.08f, 0.10f, 0.14f, 0.88f));

            // Header label
            GUI.color = Color.white;
            string statusIcon = healthRatio > 0.6f ? "💚" : (healthRatio > 0.3f ? "💛" : "❤️");
            GUI.Label(new Rect(25, 20, 200, 24), $"{statusIcon} TRAINEE HEALTH", _boldHeaderStyle);

            string hpText = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)} HP";
            GUI.Label(new Rect(boxWidth - 95, 22, 100, 20), hpText, _subLabelStyle);

            // Bar background slot
            Rect barSlotRect = new Rect(25, 48, boxWidth - 20, 20);
            DrawColorRect(barSlotRect, new Color(0.18f, 0.20f, 0.25f, 0.95f));

            // Bar fill
            Color barColor;
            if (healthRatio > 0.60f)
            {
                barColor = new Color(0.18f, 0.80f, 0.44f); // Emerald Green
            }
            else if (healthRatio > 0.30f)
            {
                barColor = new Color(0.95f, 0.61f, 0.07f); // Warning Orange
            }
            else
            {
                // Flashing red
                float flash = Mathf.PingPong(Time.time * 4f, 0.4f);
                barColor = new Color(0.90f + flash, 0.20f, 0.20f);
            }

            Rect fillRect = new Rect(25, 48, (boxWidth - 20) * healthRatio, 20);
            DrawColorRect(fillRect, barColor);

            // Low health pulse warning text
            if (TraineeHealth.Instance != null && TraineeHealth.Instance.IsLowHealth)
            {
                GUI.color = new Color(1f, 0.25f, 0.25f, 0.95f);
                GUI.Label(new Rect(15, 90, 320, 22), "⚠️ CRITICAL HEAT EXPOSURE — BACK AWAY!", _boldHeaderStyle);
                GUI.color = Color.white;
            }
        }

        private void DrawDetectionAndStopwatch()
        {
            float cardWidth = 320f;
            float cardHeight = 70f;
            Rect cardRect = new Rect(Screen.width - cardWidth - 15, 15, cardWidth, cardHeight);

            DrawColorRect(cardRect, new Color(0.08f, 0.10f, 0.14f, 0.88f));

            float timeElapsed = TraineeDetection.Instance != null ? TraineeDetection.Instance.ReactionTime : 0f;
            string timeStr = FormatStopwatch(timeElapsed);

            GUI.Label(new Rect(Screen.width - cardWidth, 20, 250, 24), $"⏱️ REACTION TIME: {timeStr}", _boldHeaderStyle);

            // Status row
            if (TraineeDetection.Instance != null)
            {
                if (TraineeDetection.Instance.HasDetectedFire)
                {
                    GUI.color = new Color(0.2f, 1f, 0.4f);
                    GUI.Label(new Rect(Screen.width - cardWidth, 48, 300, 22), "✅ FIRE CONFIRMED DETECTED!", _boldHeaderStyle);
                    GUI.color = Color.white;
                }
                else if (TraineeDetection.Instance.DwellProgress > 0f)
                {
                    int pct = Mathf.RoundToInt(TraineeDetection.Instance.DwellProgress * 100f);
                    GUI.color = new Color(1f, 0.85f, 0.2f);
                    GUI.Label(new Rect(Screen.width - cardWidth, 48, 300, 22), $"🎯 TARGETING HAZARD: {pct}%", _boldHeaderStyle);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(0.9f, 0.5f, 0.2f);
                    GUI.Label(new Rect(Screen.width - cardWidth, 48, 300, 22), "⚠️ AIM CAMERA AT FIRE HAZARD", _subLabelStyle);
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawTargetingReticle()
        {
            if (TraineeDetection.Instance == null || TraineeDetection.Instance.HasDetectedFire) return;

            float progress = TraineeDetection.Instance.DwellProgress;
            if (progress <= 0.01f) return;

            // Reticle animates from size 90 down to 45 as it locks on
            float size = Mathf.Lerp(90f, 45f, progress);
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            Color reticleColor = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.2f, 1f, 0.3f), progress);

            // Corner brackets
            float bracketLen = size * 0.35f;
            float lineThick = 3f;

            // Top-Left
            DrawColorRect(new Rect(cx - size, cy - size, bracketLen, lineThick), reticleColor);
            DrawColorRect(new Rect(cx - size, cy - size, lineThick, bracketLen), reticleColor);

            // Top-Right
            DrawColorRect(new Rect(cx + size - bracketLen, cy - size, bracketLen, lineThick), reticleColor);
            DrawColorRect(new Rect(cx + size - lineThick, cy - size, lineThick, bracketLen), reticleColor);

            // Bottom-Left
            DrawColorRect(new Rect(cx - size, cy + size - lineThick, bracketLen, lineThick), reticleColor);
            DrawColorRect(new Rect(cx - size, cy + size - bracketLen, lineThick, bracketLen), reticleColor);

            // Bottom-Right
            DrawColorRect(new Rect(cx + size - bracketLen, cy + size - lineThick, bracketLen, lineThick), reticleColor);
            DrawColorRect(new Rect(cx + size - lineThick, cy + size - bracketLen, lineThick, bracketLen), reticleColor);

            // Central lock progress bar
            Rect progressBg = new Rect(cx - 50, cy + size + 10, 100, 8);
            DrawColorRect(progressBg, new Color(0.1f, 0.1f, 0.15f, 0.8f));
            Rect progressFill = new Rect(cx - 50, cy + size + 10, 100 * progress, 8);
            DrawColorRect(progressFill, reticleColor);
        }

        private void DrawDamageVignette()
        {
            if (TraineeHealth.Instance == null) return;

            float flashAlpha = TraineeHealth.Instance.DamageFlashAlpha;
            if (flashAlpha > 0.01f)
            {
                // Pulsing red border around viewport
                Color vignetteColor = new Color(0.85f, 0.1f, 0.1f, flashAlpha * 0.45f);
                float border = 40f;

                // Top, bottom, left, right borders
                DrawColorRect(new Rect(0, 0, Screen.width, border), vignetteColor);
                DrawColorRect(new Rect(0, Screen.height - border, Screen.width, border), vignetteColor);
                DrawColorRect(new Rect(0, 0, border, Screen.height), vignetteColor);
                DrawColorRect(new Rect(Screen.width - border, 0, border, Screen.height), vignetteColor);
            }
        }

        private void DrawIncapacitatedModal()
        {
            if (TraineeHealth.Instance == null || !TraineeHealth.Instance.IsIncapacitated) return;

            // Fullscreen dark overlay
            DrawColorRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.05f, 0.05f, 0.08f, 0.85f));

            float modalWidth = 460f;
            float modalHeight = 280f;
            float mx = (Screen.width - modalWidth) * 0.5f;
            float my = (Screen.height - modalHeight) * 0.5f;

            // Modal Card
            DrawColorRect(new Rect(mx, my, modalWidth, modalHeight), new Color(0.12f, 0.14f, 0.18f, 0.95f));

            // Header
            GUI.color = new Color(1f, 0.25f, 0.25f);
            var titleStyle = new GUIStyle(_boldHeaderStyle) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(mx, my + 25, modalWidth, 35), "💀 TRAINEE INCAPACITATED", titleStyle);

            GUI.color = Color.white;
            var subStyle = new GUIStyle(_subLabelStyle) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(mx + 20, my + 65, modalWidth - 40, 40),
                $"Primary Cause: {TraineeHealth.Instance.LastDamageCause}\nTrainee succumbed to extreme heat and toxic smoke.", subStyle);

            // Stats box
            Rect statsRect = new Rect(mx + 30, my + 115, modalWidth - 60, 50);
            DrawColorRect(statsRect, new Color(0.08f, 0.09f, 0.12f, 0.8f));
            float timeElapsed = TraineeDetection.Instance != null ? TraineeDetection.Instance.ReactionTime : 0f;
            GUI.Label(new Rect(statsRect.x + 10, statsRect.y + 12, statsRect.width - 20, 25),
                $"Elapsed Time: {timeElapsed:F1}s | Hazard Detected: {(TraineeDetection.Instance != null && TraineeDetection.Instance.HasDetectedFire ? "YES" : "NO")}", subStyle);

            // Retry Button
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUI.Button(new Rect(mx + 40, my + 190, modalWidth - 80, 55), "🔄 RESPAWN & RETRY SCENARIO", _buttonStyle))
            {
                RestartScenario();
            }
            GUI.backgroundColor = Color.white;
        }

        private void RestartScenario()
        {
            if (TraineeHealth.Instance != null)
            {
                TraineeHealth.Instance.ResetHealth();
            }
            if (TraineeDetection.Instance != null)
            {
                TraineeDetection.Instance.ResetDetection();
            }
            if (FireManager.Instance != null)
            {
                FireManager.Instance.ExtinguishAll();
                FireManager.Instance.IgniteRandomFires();
            }
        }

        private void DrawColorRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _whiteTexture);
            GUI.color = oldColor;
        }

        private string FormatStopwatch(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            float secs = seconds % 60f;
            return $"{mins:00}:{secs:04.1f}s";
        }
    }
}
