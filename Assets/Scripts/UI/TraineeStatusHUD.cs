using UnityEngine;
using ARMiningSimulator.Player;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Extinguisher;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Phase 5 Trainee Status HUD — Fortnite & Battle Royale Action Game Style.
    /// Features:
    /// - Chunky high-contrast Vitals widget (Top-Left) with big bold numbers and segmented health/shield meters.
    /// - Arcade Mission Survival Clock & Hazard Target Lock (Top-Right).
    /// - Fortnite Storm-Alert style Fire Spread Countdown Banner (Top-Center).
    /// - 3D Beveled Loot/Action Pickup Prompt (Bottom-Center).
    /// - Automatic modal conflict suppression (100% overlap-free).
    /// </summary>
    public class TraineeStatusHUD : MonoBehaviour
    {
        private GUIStyle _hudHeaderStyle;
        private GUIStyle _hudValueStyle;
        private GUIStyle _hudLargeNumStyle;
        private GUIStyle _hudStatusStyle;
        private GUIStyle _actionBtnStyle;
        private GUIStyle _modalHeaderStyle;
        private GUIStyle _modalSubStyle;

        private float _displayHealth = 100f;
        private float _healthSmoothVelocity = 0f;

        private bool _hasSeenFire = false;
        private float _healthBarAlpha = 0f;

        public void ResetFireVisibility()
        {
            _hasSeenFire = false;
            _healthBarAlpha = 0f;
        }

        private void OnEnable()
        {
            FireManager.OnFireIgnited += HandleFireIgnited;
        }

        private void OnDisable()
        {
            FireManager.OnFireIgnited -= HandleFireIgnited;
        }

        private void HandleFireIgnited(FireHazard hazard)
        {
            _hasSeenFire = false;
            _healthBarAlpha = 0f;
        }

        private void Update()
        {
            if (TraineeHealth.Instance != null)
            {
                _displayHealth = Mathf.SmoothDamp(_displayHealth, TraineeHealth.Instance.CurrentHealth, ref _healthSmoothVelocity, 0.12f);
            }

            // Check if player points camera at fire hazard to reveal HP bar
            if (!_hasSeenFire)
            {
                if (TraineeDetection.Instance != null)
                {
                    if (TraineeDetection.Instance.CurrentTargetHazard != null ||
                        TraineeDetection.Instance.DwellProgress > 0.01f ||
                        TraineeDetection.Instance.HasDetectedFire)
                    {
                        _hasSeenFire = true;
                    }
                }

                if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                    ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage >= ARMiningSimulator.Core.SimulationStage.FireDetected)
                {
                    _hasSeenFire = true;
                }
            }

            float targetAlpha = _hasSeenFire ? 1.0f : 0.0f;
            _healthBarAlpha = Mathf.MoveTowards(_healthBarAlpha, targetAlpha, Time.deltaTime * 3.5f);
        }

        private void OnGUI()
        {
            bool hasActiveFires = FireManager.Instance != null && FireManager.Instance.HasActiveFires;
            if (!hasActiveFires && ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage == ARMiningSimulator.Core.SimulationStage.ScanningRoom)
            {
                return;
            }

            InitStyles();

            TacticalUITheme.BeginScaledGUI();
            try
            {
                // Always draw full-screen damage vignette when player takes damage
                DrawDamageVignette();

                // Game over / Incapacitated modal has highest priority
                if (TraineeHealth.Instance != null && TraineeHealth.Instance.IsIncapacitated)
                {
                    DrawIncapacitatedModal();
                    return;
                }

                // If a decision or scorecard modal is active, suppress background HUD to avoid any overlap
                if (TacticalUITheme.IsAnyModalOpen())
                {
                    return;
                }

                // Main gameplay HUD elements — positioned in isolated screen zones with guaranteed zero overlap
                DrawHealthWidget();
                DrawExtinguisherPickupPrompt();
                DrawTargetingReticle();
            }
            finally
            {
                TacticalUITheme.EndScaledGUI();
            }
        }

        private void InitStyles()
        {
            if (_hudHeaderStyle == null)
            {
                _hudHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.75f, 0.88f, 1.0f) }
                };
            }

            if (_hudValueStyle == null)
            {
                _hudValueStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = Color.white }
                };
            }

            if (_hudLargeNumStyle == null)
            {
                _hudLargeNumStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.white }
                };
            }

            if (_hudStatusStyle == null)
            {
                _hudStatusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
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

            if (_modalHeaderStyle == null)
            {
                _modalHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = TacticalUITheme.FortniteRed }
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
        }

        // =========================================================================
        // ZONE 1: TOP-LEFT CHUNKY FORTNITE VITALS WIDGET
        // =========================================================================
        private void DrawHealthWidget()
        {
            if (_healthBarAlpha <= 0.01f) return;

            float currentHp = TraineeHealth.Instance != null ? _displayHealth : 100f;
            float maxHp = TraineeHealth.Instance != null ? TraineeHealth.Instance.MaxHealth : 100f;
            float ratio = Mathf.Clamp01(currentHp / maxHp);

            // Wide enough on every portrait width to hold "999 / 100 HP" on one line without
            // the number, unit and status tag competing for the same cramped row.
            float width = Mathf.Min(260f, TacticalUITheme.VW * 0.42f);
            float height = TacticalUITheme.VitalsCardHeight;
            Rect cardRect = new Rect(16f, 16f, width, height);

            Color hpColor;
            string statusTag;
            if (ratio > 0.60f)
            {
                hpColor = TacticalUITheme.FortniteGreen;
                statusTag = "SUIT // NOMINAL";
            }
            else if (ratio > 0.30f)
            {
                hpColor = TacticalUITheme.FortniteAmber;
                statusTag = "SUIT // HIGH TEMPERATURE";
            }
            else
            {
                float flash = Mathf.PingPong(Time.time * 6f, 0.4f);
                hpColor = new Color(1.0f, 0.2f + flash, 0.25f);
                statusTag = "CRITICAL // THERMAL INJURY";
            }

            // Draw chunky Fortnite card plate
            TacticalUITheme.DrawFortniteCard(cardRect, hpColor, TacticalUITheme.FortniteNavy, "➕ HEALTH", hpColor);

            // Row 1: big number, then "/ MAX HP" measured and placed right after it — never a
            // fixed offset — so it can't overlap on narrow cards.
            int hpInt = Mathf.CeilToInt(currentHp);
            string numStr = hpInt.ToString();
            float numW = _hudLargeNumStyle.CalcSize(new GUIContent(numStr)).x;
            GUI.Label(new Rect(cardRect.x + 12f, cardRect.y + 12f, numW + 4f, 26f), numStr, _hudLargeNumStyle);

            _hudValueStyle.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
            float sufX = cardRect.x + 14f + numW;
            float sufW = cardRect.x + cardRect.width - 12f - sufX;
            if (sufW > 30f)
            {
                GUI.Label(new Rect(sufX, cardRect.y + 18f, sufW, 18f), $"/ {Mathf.CeilToInt(maxHp)} HP", _hudValueStyle);
            }

            // Row 2: status tag on its own full-width line — never squeezed beside the number.
            _hudStatusStyle.normal.textColor = hpColor;
            GUI.Label(new Rect(cardRect.x + 12f, cardRect.y + 38f, cardRect.width - 24f, 16f), statusTag, _hudStatusStyle);

            // Row 3: health bar
            Rect barRect = new Rect(cardRect.x + 12f, cardRect.y + 58f, cardRect.width - 24f, 14f);
            TacticalUITheme.DrawFortniteBar(barRect, ratio, hpColor, TacticalUITheme.CardSlotBg, 8);
        }

        // =========================================================================
        // ZONE 4: BOTTOM CONTEXTUAL LOOT PICKUP PROMPT
        // =========================================================================
        private void DrawExtinguisherPickupPrompt()
        {
            if (Environment.WorldFireExtinguisher.Instance == null) return;
            if (Environment.WorldFireExtinguisher.Instance.IsTaken) return;
            if (!Environment.WorldFireExtinguisher.Instance.IsPlayerInRange) return;

            float btnW = Mathf.Min(360f, TacticalUITheme.VW - 40f);
            float btnH = 64f;
            float bx = (TacticalUITheme.VW - btnW) * 0.5f;
            float by = TacticalUITheme.VH - btnH - 30f;
            Rect promptRect = new Rect(bx, by, btnW, btnH);

            if (TacticalUITheme.DrawFortniteButton(promptRect, "🧯 EQUIP EXTINGUISHER (TAP / 'E')", TacticalUITheme.FortniteGold, _actionBtnStyle, "[EPIC LOOT]"))
            {
                Environment.WorldFireExtinguisher.Instance.TakeExtinguisher();
            }
        }

        // =========================================================================
        // ZONE 5: RETICLE & VIGNETTE
        // =========================================================================
        private void DrawTargetingReticle()
        {
            if (TraineeDetection.Instance == null || TraineeDetection.Instance.HasDetectedFire) return;

            float progress = TraineeDetection.Instance.DwellProgress;
            if (progress <= 0.01f) return;

            float size = Mathf.Lerp(85f, 42f, progress);
            float cx = TacticalUITheme.VW * 0.5f;
            float cy = TacticalUITheme.VH * 0.5f;

            Color reticleColor = Color.Lerp(TacticalUITheme.FortniteAmber, TacticalUITheme.FortniteGreen, progress);
            Rect reticleRect = new Rect(cx - size, cy - size, size * 2f, size * 2f);

            TacticalUITheme.DrawCornerBrackets(reticleRect, reticleColor, size * 0.45f, 3f);
            TacticalUITheme.DrawRect(new Rect(cx - 3, cy - 3, 6, 6), reticleColor);

            Rect progressBg = new Rect(cx - 50, cy + size + 10, 100, 8);
            TacticalUITheme.DrawFortniteBar(progressBg, progress, reticleColor, TacticalUITheme.CardSlotBg, 4);
        }

        private void DrawDamageVignette()
        {
            if (TraineeHealth.Instance == null) return;

            float flashAlpha = TraineeHealth.Instance.DamageFlashAlpha;
            if (flashAlpha > 0.01f)
            {
                Color vignetteColor = new Color(TacticalUITheme.FortniteRed.r, TacticalUITheme.FortniteRed.g, TacticalUITheme.FortniteRed.b, flashAlpha * 0.45f);
                float border = 35f;

                TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, border), vignetteColor);
                TacticalUITheme.DrawRect(new Rect(0, TacticalUITheme.VH - border, TacticalUITheme.VW, border), vignetteColor);
                TacticalUITheme.DrawRect(new Rect(0, 0, border, TacticalUITheme.VH), vignetteColor);
                TacticalUITheme.DrawRect(new Rect(TacticalUITheme.VW - border, 0, border, TacticalUITheme.VH), vignetteColor);
            }
        }

        private void DrawIncapacitatedModal()
        {
            TacticalUITheme.DrawRect(new Rect(0, 0, TacticalUITheme.VW, TacticalUITheme.VH), new Color(0.04f, 0.05f, 0.08f, 0.88f));

            float modalWidth = Mathf.Min(500f, TacticalUITheme.VW - 32f);
            float modalHeight = 310f;
            float mx = (TacticalUITheme.VW - modalWidth) * 0.5f;
            float my = (TacticalUITheme.VH - modalHeight) * 0.5f;
            Rect modalRect = new Rect(mx, my, modalWidth, modalHeight);

            TacticalUITheme.DrawFortniteCard(modalRect, TacticalUITheme.FortniteRed, TacticalUITheme.FortniteNavyDark, "/// ELIMINATED // CASUALTY ///", TacticalUITheme.FortniteRed);

            GUI.Label(new Rect(mx, my + 26, modalWidth, 32), "💀 TRAINEE ELIMINATED", _modalHeaderStyle);

            string cause = TraineeHealth.Instance != null ? TraineeHealth.Instance.LastDamageCause : "Fire Hazard";
            GUI.Label(new Rect(mx + 20, my + 68, modalWidth - 40, 42),
                $"Fatal Factor: {cause}\nTrainee succumbed to excessive radiant thermal flux.", _modalSubStyle);

            // Stats slot
            Rect statsRect = new Rect(mx + 24, my + 124, modalWidth - 48, 54);
            TacticalUITheme.DrawFortniteCard(statsRect, TacticalUITheme.BorderSubtle, TacticalUITheme.CardSlotBg);

            float timeElapsed = TraineeDetection.Instance != null ? TraineeDetection.Instance.ReactionTime : 0f;
            string statsText = $"Drill Time: {timeElapsed:F1}s  |  Final Evaluation: GRADE F";
            GUI.Label(new Rect(statsRect.x + 10, statsRect.y + 16, statsRect.width - 20, 24), statsText, _modalSubStyle);

            // Respawn Button
            Rect btnRect = new Rect(mx + 30, my + 215, modalWidth - 60, 60);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "🔄 RETURN TO BATTLE / RESTART", TacticalUITheme.FortniteGreen, _actionBtnStyle, "[RETRY]"))
            {
                RestartScenario();
            }
        }

        private void RestartScenario()
        {
            _hasSeenFire = false;
            _healthBarAlpha = 0f;

            if (TraineeHealth.Instance != null) TraineeHealth.Instance.ResetHealth();
            if (TraineeDetection.Instance != null) TraineeDetection.Instance.ResetDetection();
            if (FireManager.Instance != null)
            {
                FireManager.Instance.ExtinguishAll();
                FireManager.Instance.IgniteRandomFires();
            }
        }

    }
}
