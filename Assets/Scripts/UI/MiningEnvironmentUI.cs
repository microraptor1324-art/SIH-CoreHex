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
                int toastW = Mathf.Min(Screen.width - 32, 620);
                int toastH = 48;
                int toastX = (Screen.width - toastW) / 2;
                int toastY = 18;

                GUI.Box(new Rect(toastX, toastY, toastW, toastH), GUIContent.none);
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

            int pad = 20;
            int screenW = Screen.width;
            int screenH = Screen.height;
            int cardW = Mathf.Min(screenW - pad * 2, 580);
            int cardX = (screenW - cardW) / 2;

            // 2. TOP STATS CARD
            int topH = 195;
            GUI.Box(new Rect(cardX, pad, cardW, topH), GUIContent.none);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.75f, 0.1f) } // Mining Gold
            };

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.85f, 0.9f, 0.95f) }
            };

            GUIStyle readyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.15f, 0.95f, 0.45f) } // Bright Green
            };

            GUILayout.BeginArea(new Rect(cardX + 16, pad + 12, cardW - 32, topH - 24));
            GUILayout.Label("⛏️ UNDERGROUND MINING SECTION", headerStyle);
            GUILayout.Space(2);

            float len = _generator != null ? _generator.ActiveLength : 0f;
            float wid = _generator != null ? _generator.ActiveWidth : 0f;
            float area = _generator != null ? _generator.ActiveArea : 0f;

            GUILayout.Label($"Room Drift: {len:F2} m x {wid:F2} m ({area:F2} m²)", labelStyle);

            if (_generator != null && !_generator.IsEquipmentSpawned)
            {
                GUILayout.Label($"Equipment: Deploying in {_generator.RemainingEquipmentDelay:F1}s ⚙️", subStyle);
            }
            else
            {
                int machines = _generator != null && _generator.Spawner != null ? _generator.Spawner.SpawnedMachines.Count : 6;
                int elec = _generator != null && _generator.Spawner != null ? _generator.Spawner.SpawnedElectrical.Count : 11;
                GUILayout.Label($"Equipment: {machines} Heavy Machines • {elec} Electrical Substation Units", subStyle);
                GUILayout.Label("Status: All equipment zoned & ready for training drill ✅", readyStyle);
            }

            // Emergency exit distance
            if (_generator != null && _arCamera != null)
            {
                float exitDist = Vector3.Distance(_arCamera.transform.position, _generator.EmergencyExitPosition);
                GUILayout.Label($"Emergency Exit Corridor: {exitDist:F2} m away 🚪", subStyle);
            }

            int seed = _generator != null && _generator.Spawner != null ? _generator.Spawner.ActiveSeed : 0;
            GUILayout.Label($"Layout Seed: #{seed}", subStyle);

            GUILayout.EndArea();

            // 3. BOTTOM ACTIONS (Centered Action Deck)
            bool isReady = _generator != null && _generator.IsEquipmentSpawned;
            int deckH = isReady ? 115 : 62;
            int bottomY = screenH - deckH - pad - 6;

            GUI.Box(new Rect(cardX, bottomY, cardW, deckH), GUIContent.none);
            GUILayout.BeginArea(new Rect(cardX + 12, bottomY + 8, cardW - 24, deckH - 16));

            Color orig = GUI.backgroundColor;

            if (!isReady)
            {
                // Staged deployment skip button
                GUI.backgroundColor = new Color(0.15f, 0.65f, 0.95f);
                if (GUILayout.Button("⚡ DEPLOY ALL EQUIPMENT NOW", GUILayout.Height(46)))
                {
                    if (_generator != null) _generator.DeployEquipmentNow();
                }
            }
            else
            {
                // ROW 1: PRIMARY "START TRAINING" BUTTON
                GUI.backgroundColor = new Color(0.12f, 0.85f, 0.42f); // Emerald Safety Green
                GUIStyle startBtnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold
                };
                if (GUILayout.Button("🚨 START TRAINING DRILL", startBtnStyle, GUILayout.Height(54)))
                {
                    StartTraining();
                }

                GUILayout.Space(6);

                // ROW 2: SECONDARY RE-ROLL & RE-SCAN CONTROLS
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.20f, 0.28f, 0.38f);
                if (GUILayout.Button("🔀 Re-Roll Layout", GUILayout.Height(34)))
                {
                    OnRegenerateClicked();
                }

                GUILayout.Space(8);

                GUI.backgroundColor = new Color(0.28f, 0.18f, 0.22f);
                if (GUILayout.Button("🔄 Re-Scan Room", GUILayout.Height(34)))
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

                GUILayout.EndHorizontal();
            }

            GUI.backgroundColor = orig;
            GUILayout.EndArea();
        }
    }
}
