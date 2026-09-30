using UnityEngine;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// UI Controller for Phase 3: Displays environment statistics, equipment counts,
    /// Emergency Exit distance, and interactive layout regeneration controls.
    /// </summary>
    public class MiningEnvironmentUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private MiningEnvironmentGenerator _generator;
        [SerializeField] private Camera _arCamera;

        [Header("Optional uGUI References")]
        [SerializeField] private UnityEngine.UI.Text _infoText;
        [SerializeField] private UnityEngine.UI.Text _equipmentText;
        [SerializeField] private UnityEngine.UI.Text _exitDistanceText;
        [SerializeField] private UnityEngine.UI.Button _regenerateButton;
        [SerializeField] private UnityEngine.UI.Button _startTrainingButton;

        private bool _isTrainingStarted = false;
        private float _trainingBannerTimer = 0f;

        private void Awake()
        {
            if (_generator == null)
                _generator = FindFirstObjectByType<MiningEnvironmentGenerator>();
            if (_arCamera == null)
                _arCamera = Camera.main;
        }

        private void Start()
        {
            if (_regenerateButton != null)
                _regenerateButton.onClick.AddListener(OnRegenerateClicked);
            if (_startTrainingButton != null)
                _startTrainingButton.onClick.AddListener(StartTraining);
        }

        public void OnRegenerateClicked()
        {
            if (_generator != null)
            {
                _generator.Regenerate();
            }
        }

        /// <summary>
        /// Starts the full end-to-end fire safety training drill:
        /// - Ignites an equipment-specific fire scenario (arc fault, hydraulic spray, friction, or battery thermal runaway).
        /// - Resets and starts the trainee reaction time stopwatch.
        /// - Transitions the game loop to the active incident drill.
        /// </summary>
        public void StartTraining()
        {
            _isTrainingStarted = true;
            _trainingBannerTimer = 5.5f;

            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null)
            {
                ARMiningSimulator.Core.SimulationGameLoop.Instance.StartTrainingDrill();
            }
            else
            {
                var gen = _generator != null ? _generator : FindAnyObjectByType<MiningEnvironmentGenerator>();
                if (gen != null && !gen.IsEquipmentSpawned && gen.IsEnvironmentReady)
                {
                    gen.DeployEquipmentNow();
                }

                var fireManager = FindAnyObjectByType<ARMiningSimulator.Fire.FireManager>();
                if (fireManager != null)
                {
                    fireManager.StartScenarioFires();
                }

                if (ARMiningSimulator.Player.TraineeDetection.Instance != null)
                {
                    ARMiningSimulator.Player.TraineeDetection.Instance.StartStopwatch();
                }

                var hud = FindAnyObjectByType<TraineeStatusHUD>();
                if (hud != null)
                {
                    hud.ResetFireVisibility();
                }
            }

            Debug.Log("[MiningEnvironmentUI] 🚨 START TRAINING DRILL TRIGGERED!");
        }

        private void OnGUI()
        {
            TacticalUITheme.BeginScaledGUI();
            try
            {
            DrawOnGUIContent();
            }
            finally
            {
                TacticalUITheme.EndScaledGUI();
            }
        }

        private void DrawOnGUIContent()
        {
            // Sync with global simulation game loop or active fire manager
            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage >= ARMiningSimulator.Core.SimulationStage.FireIgnited)
            {
                _isTrainingStarted = true;
            }

            if (ARMiningSimulator.Fire.FireManager.Instance != null && ARMiningSimulator.Fire.FireManager.Instance.HasActiveFires)
            {
                _isTrainingStarted = true;
            }

            // 1. EMERGENCY TOAST BANNER (Shown for 5 seconds when drill starts)
            if (_trainingBannerTimer > 0f)
            {
                _trainingBannerTimer -= Time.deltaTime;
                int toastW = Mathf.RoundToInt(Mathf.Min(TacticalUITheme.VW - 32, 620));
                int toastH = 48;
                int toastX = Mathf.RoundToInt((TacticalUITheme.VW - toastW) / 2);
                int toastY = 18;

                TacticalUITheme.DrawFortniteCard(new Rect(toastX, toastY, toastW, toastH), TacticalUITheme.FortniteAmber, TacticalUITheme.FortniteNavyDark);
                GUIStyle alertStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1.0f, 0.35f, 0.15f) } // Bright Emergency Orange
                };
                GUI.Label(new Rect(toastX + 8, toastY + 4, toastW - 16, toastH - 8),
                    "🚨 INCIDENT REPORTED! AIM CAMERA AT HAZARDS TO LOCATE FIRE!", alertStyle);
            }

            // Dismiss pre-training configuration cards once drill is underway
            if (_isTrainingStarted) return;

            // Only draw OnGUI if uGUI is not bound
            if (_infoText != null && _equipmentText != null) return;

            // Don't draw the mine-generation cards while still in the room-scanning phase —
            // this was overlapping directly on top of RoomMeasurementUI's status card.
            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage != ARMiningSimulator.Core.SimulationStage.MineActive)
            {
                return;
            }

            int pad = 20;
            int screenW = Mathf.RoundToInt(TacticalUITheme.VW);
            int screenH = Mathf.RoundToInt(TacticalUITheme.VH);
            int cardW = Mathf.Min(screenW - pad * 2, 580);
            int cardX = (screenW - cardW) / 2;

            // 3. BOTTOM ACTIONS (Centered Action Deck)
            bool isReady = _generator != null && _generator.IsEquipmentSpawned;
            int deckH = isReady ? 122 : 70;
            int bottomY = screenH - deckH - pad - 10;

            TacticalUITheme.DrawFortniteCard(new Rect(cardX, bottomY, cardW, deckH), TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavyDark);

            float dInnerX = cardX + 14;
            float dInnerW = cardW - 28;
            float dY = bottomY + 14;

            GUIStyle mainBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUIStyle subBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            if (!isReady)
            {
                // Staged deployment skip button
                if (TacticalUITheme.DrawFortniteButton(new Rect(dInnerX, dY, dInnerW, 46), "⚡ DEPLOY ALL EQUIPMENT NOW", TacticalUITheme.FortniteBlue, mainBtnStyle))
                {
                    if (_generator != null) _generator.DeployEquipmentNow();
                }
            }
            else
            {
                // ROW 1: PRIMARY "START TRAINING" BUTTON
                if (TacticalUITheme.DrawFortniteButton(new Rect(dInnerX, dY, dInnerW, 56), "🚨 START TRAINING DRILL", TacticalUITheme.FortniteGreen, mainBtnStyle))
                {
                    StartTraining();
                }

                dY += 56 + 10;
                float halfW = (dInnerW - 10) * 0.5f;

                // ROW 2: SECONDARY RE-ROLL & RE-SCAN CONTROLS
                if (TacticalUITheme.DrawFortniteButton(new Rect(dInnerX, dY, halfW, 36), "🔀 Re-Roll Layout", TacticalUITheme.FortniteNavyLight, subBtnStyle))
                {
                    OnRegenerateClicked();
                }

                if (TacticalUITheme.DrawFortniteButton(new Rect(dInnerX + halfW + 10, dY, halfW, 36), "🔄 Re-Scan Room", TacticalUITheme.FortniteRed, subBtnStyle))
                {
                    var measurement = FindFirstObjectByType<ARMiningSimulator.AR.ARRoomMeasurement>();
                    if (measurement != null)
                    {
                        measurement.ResetMeasurement();
                    }
                    else if (_generator != null)
                    {
                        _generator.ClearEnvironment();
                    }
                }
            }
        }
    }
}
