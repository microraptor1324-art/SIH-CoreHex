using System;
using UnityEngine;
using ARMiningSimulator.AR;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Environment;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Extinguisher;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Investigation;
using ARMiningSimulator.Player;
using ARMiningSimulator.Progression;
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Core
{
    public enum SimulationStage
    {
        ScanningRoom = 0,
        MineActive = 1,
        FireIgnited = 2,
        FireDetected = 3,
        DecisionTesting = 4,
        Evacuating = 5,
        FireResponse = 6,
        Investigation = 7,
        Scorecard = 8,
        LevelComplete = 9
    }

    /// <summary>
    /// Master orchestrator for the full end-to-end training simulation cycle:
    /// SCAN ROOM -> MEASURE -> VIRTUAL MINE -> RANDOM FIRE -> DETECT FIRE ->
    /// DECISION TESTS -> EVACUATION -> FIRE RESPONSE -> INVESTIGATION -> SCORECARD -> NEXT LEVEL
    /// </summary>
    public class SimulationGameLoop : MonoBehaviour
    {
        public static SimulationGameLoop Instance { get; private set; }

        [SerializeField] private SimulationStage _currentStage = SimulationStage.ScanningRoom;
        [SerializeField] private bool _autoAdvanceLevelOnPass = true;

        public static event Action<SimulationStage> OnSimulationStageChanged;

        public SimulationStage CurrentStage => _currentStage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            MiningEnvironmentGenerator.OnEnvironmentGenerated += HandleEnvironmentGenerated;
            FireManager.OnFireIgnited += HandleFireIgnited;
            TraineeDetection.OnFireDetected += HandleFireDetected;
            DecisionManager.OnDecisionsCompleted += HandleDecisionsCompleted;
            EvacuationManager.OnSafeZoneReached += HandleSafeZoneReached;
            FireResponseManager.OnFireResponseCompleted += HandleFireResponseCompleted;
            InvestigationManager.OnInvestigationStarted += HandleInvestigationStarted;
            InvestigationManager.OnScorecardReady += HandleScorecardReady;
            InvestigationAndScoreUI.OnNextLevelRequested += HandleNextLevelRequested;
        }

        private void OnDisable()
        {
            MiningEnvironmentGenerator.OnEnvironmentGenerated -= HandleEnvironmentGenerated;
            FireManager.OnFireIgnited -= HandleFireIgnited;
            TraineeDetection.OnFireDetected -= HandleFireDetected;
            DecisionManager.OnDecisionsCompleted -= HandleDecisionsCompleted;
            EvacuationManager.OnSafeZoneReached -= HandleSafeZoneReached;
            FireResponseManager.OnFireResponseCompleted -= HandleFireResponseCompleted;
            InvestigationManager.OnInvestigationStarted -= HandleInvestigationStarted;
            InvestigationManager.OnScorecardReady -= HandleScorecardReady;
            InvestigationAndScoreUI.OnNextLevelRequested -= HandleNextLevelRequested;
        }

        public void SetStage(SimulationStage newStage)
        {
            _currentStage = newStage;
            Debug.Log($"[SimulationGameLoop] 🏁 Stage Transition -> {_currentStage}");
            OnSimulationStageChanged?.Invoke(_currentStage);
        }

        /// <summary>
        /// Starts the full end-to-end fire safety training drill:
        /// Ensures equipment is deployed, starts scenario fire on a random machine/tool,
        /// resets reaction stopwatch and HUD visibility, and transitions the loop to FireIgnited.
        /// </summary>
        public void StartTrainingDrill()
        {
            Debug.Log("[SimulationGameLoop] 🚨 START TRAINING DRILL INVOKED!");

            // Ensure equipment is deployed if generator is still waiting on delay
            var gen = FindFirstObjectByType<MiningEnvironmentGenerator>();
            if (gen != null && !gen.IsEquipmentSpawned && gen.IsEnvironmentReady)
            {
                gen.DeployEquipmentNow();
            }

            var fireManager = FireManager.Instance ?? FindFirstObjectByType<FireManager>();
            if (fireManager != null)
            {
                fireManager.StartScenarioFires();
            }

            if (TraineeDetection.Instance != null)
            {
                TraineeDetection.Instance.StartStopwatch();
            }

            var hud = FindFirstObjectByType<TraineeStatusHUD>();
            if (hud != null)
            {
                hud.ResetFireVisibility();
            }

            SetStage(SimulationStage.FireIgnited);
        }

        private void HandleEnvironmentGenerated()
        {
            SetStage(SimulationStage.MineActive);
        }

        private void HandleFireIgnited(FireHazard hazard)
        {
            SetStage(SimulationStage.FireIgnited);
        }

        private void HandleFireDetected(FireHazard hazard, float reactionTime)
        {
            SetStage(SimulationStage.FireDetected);
            // Decision testing automatically begins when fire is detected
            SetStage(SimulationStage.DecisionTesting);
        }

        private void HandleDecisionsCompleted()
        {
            SetStage(SimulationStage.Evacuating);
        }

        private void HandleSafeZoneReached(float duration, int bonus)
        {
            SetStage(SimulationStage.FireResponse);
        }

        private void HandleFireResponseCompleted()
        {
            // Transitioning to incident investigation
        }

        private void HandleInvestigationStarted(IncidentReport report)
        {
            SetStage(SimulationStage.Investigation);
        }

        private void HandleScorecardReady(ScoreBreakdown breakdown)
        {
            SetStage(SimulationStage.Scorecard);

            if (GameProgressionManager.Instance != null && breakdown != null)
            {
                int currentLvl = GameProgressionManager.Instance.CurrentLevelIndex;
                GameProgressionManager.Instance.SaveLevelGrade(currentLvl, breakdown.gradeTitle);

                if (breakdown.grade != TraineeGrade.F_Disqualified)
                {
                    int nextLvl = currentLvl + 1;
                    if (nextLvl <= 3)
                    {
                        GameProgressionManager.Instance.UnlockLevel(nextLvl);
                    }
                }
            }
        }

        private void HandleNextLevelRequested()
        {
            SetStage(SimulationStage.LevelComplete);
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.AdvanceToNextLevel();
            }

            // Reload scene to restart loop with new level config
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
