using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Progression
{
    public enum MineSectionType
    {
        HaulageDrift = 0,
        ElectricalSubstation = 1,
        ActiveCuttingFace = 2
    }

    [System.Serializable]
    public class LevelConfig
    {
        public int levelIndex; // 1, 2, 3
        public string levelTitle;
        public string subtitle;
        public MineSectionType sectionType;
        public string environmentDescription;
        public string primaryHazardDescription;

        [Header("Fire Dynamics")]
        public float smallToMediumTime;
        public float mediumToLargeTime;
        public float initialIgnitionDelay;
        public bool forceElectricalFire;

        [Header("Environmental Hazards")]
        public float ambientDamageRate;
        public float evacuationTimeLimit;
        public int targetScoreToPass;

        [Header("Equipment Spawning")]
        public int machineCount;
        public int electricalCount;
    }

    /// <summary>
    /// Manages player progression, level unlocking, persistent high scores,
    /// and dynamically configures the simulation parameters for each training scenario.
    /// </summary>
    public class GameProgressionManager : MonoBehaviour
    {
        public static GameProgressionManager Instance { get; private set; }

        private const string PrefKey_HighestUnlocked = "AR_MineSim_HighestUnlockedLevel";
        private const string PrefKey_Level1Grade = "AR_MineSim_Level1_Grade";
        private const string PrefKey_Level2Grade = "AR_MineSim_Level2_Grade";
        private const string PrefKey_Level3Grade = "AR_MineSim_Level3_Grade";

        [SerializeField] private int _currentLevelIndex = 1;

        private List<LevelConfig> _levels = new List<LevelConfig>();

        public static event Action<LevelConfig> OnLevelLoaded;
        public static event Action<int> OnLevelUnlocked;

        public int CurrentLevelIndex => _currentLevelIndex;
        public int HighestUnlockedLevel => PlayerPrefs.GetInt(PrefKey_HighestUnlocked, 1);
        public List<LevelConfig> Levels => _levels;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeLevelDefinitions();
        }

        private void InitializeLevelDefinitions()
        {
            _levels.Clear();

            // Level 1: Haulage Drift Belt Conveyor Fire
            _levels.Add(new LevelConfig
            {
                levelIndex = 1,
                levelTitle = "Level 1: Haulage Drift Belt Fire",
                subtitle = "Incipient Class A/B Conveyor Fire",
                sectionType = MineSectionType.HaulageDrift,
                environmentDescription = "Main coal haulage drift with heavy belt conveyors and transfer chutes.",
                primaryHazardDescription = "Seized idler roller bearing generating intense friction on rubber belt. Combustible coal float dust present.",
                smallToMediumTime = 12f,
                mediumToLargeTime = 22f,
                initialIgnitionDelay = 2.0f,
                forceElectricalFire = false,
                ambientDamageRate = 1.0f,
                evacuationTimeLimit = 35f,
                targetScoreToPass = 600,
                machineCount = 3,
                electricalCount = 2
            });

            // Level 2: High-Voltage Electrical Substation
            _levels.Add(new LevelConfig
            {
                levelIndex = 2,
                levelTitle = "Level 2: High-Voltage Substation",
                subtitle = "Energized 480V Electrical Arc Flash",
                sectionType = MineSectionType.ElectricalSubstation,
                environmentDescription = "Underground power distribution station with step-down transformers and switchgear.",
                primaryHazardDescription = "Phase ground fault with arcing in 480V panel. FATAL SHOCK HAZARD: Conductive water/foam prohibited!",
                smallToMediumTime = 8f,
                mediumToLargeTime = 16f,
                initialIgnitionDelay = 1.5f,
                forceElectricalFire = true,
                ambientDamageRate = 2.0f,
                evacuationTimeLimit = 30f,
                targetScoreToPass = 700,
                machineCount = 2,
                electricalCount = 4
            });

            // Level 3: Continuous Miner Section & Gas Inflow
            _levels.Add(new LevelConfig
            {
                levelIndex = 3,
                levelTitle = "Level 3: Deep Cutting Face & Gas Hazard",
                subtitle = "Rapid Flash Fire with Methane Release",
                sectionType = MineSectionType.ActiveCuttingFace,
                environmentDescription = "Deep continuous miner development entry with active ventilation face.",
                primaryHazardDescription = "Frictional ignition of cutter bits against pyritic rock, igniting methane bleed. Rapid toxic smoke build-up.",
                smallToMediumTime = 6f,
                mediumToLargeTime = 12f,
                initialIgnitionDelay = 1.0f,
                forceElectricalFire = false,
                ambientDamageRate = 3.2f,
                evacuationTimeLimit = 25f,
                targetScoreToPass = 800,
                machineCount = 4,
                electricalCount = 3
            });
        }

        public LevelConfig GetCurrentLevelConfig()
        {
            return GetConfigByIndex(_currentLevelIndex);
        }

        public LevelConfig GetConfigByIndex(int index)
        {
            if (_levels == null || _levels.Count == 0) InitializeLevelDefinitions();
            int clamped = Mathf.Clamp(index - 1, 0, _levels.Count - 1);
            return _levels[clamped];
        }

        public bool IsLevelUnlocked(int index)
        {
            if (index <= 1) return true;
            return HighestUnlockedLevel >= index;
        }

        public void UnlockLevel(int index)
        {
            int currentHighest = HighestUnlockedLevel;
            if (index > currentHighest)
            {
                PlayerPrefs.SetInt(PrefKey_HighestUnlocked, index);
                PlayerPrefs.Save();
                Debug.Log($"[GameProgressionManager] 🔓 CONGRATULATIONS! Level {index} UNLOCKED!");
                OnLevelUnlocked?.Invoke(index);
            }
        }

        public void SaveLevelGrade(int levelIndex, string grade)
        {
            string key = levelIndex switch
            {
                1 => PrefKey_Level1Grade,
                2 => PrefKey_Level2Grade,
                3 => PrefKey_Level3Grade,
                _ => ""
            };

            if (!string.IsNullOrEmpty(key))
            {
                PlayerPrefs.SetString(key, grade);
                PlayerPrefs.Save();
            }
        }

        public string GetLevelGrade(int levelIndex)
        {
            string key = levelIndex switch
            {
                1 => PrefKey_Level1Grade,
                2 => PrefKey_Level2Grade,
                3 => PrefKey_Level3Grade,
                _ => ""
            };

            return !string.IsNullOrEmpty(key) ? PlayerPrefs.GetString(key, "—") : "—";
        }

        public void LoadLevel(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
            var config = GetCurrentLevelConfig();

            Debug.Log($"[GameProgressionManager] 🔄 Applying Level {levelIndex}: {config.levelTitle}");

            // Apply fire spread system parameters
            if (ARMiningSimulator.Fire.FireSpreadSystem.Instance != null)
            {
                ARMiningSimulator.Fire.FireSpreadSystem.Instance.SetSpreadTimers(config.smallToMediumTime, config.mediumToLargeTime);
            }

            // Apply evacuation countdown
            if (ARMiningSimulator.Evacuation.EvacuationManager.Instance != null)
            {
                ARMiningSimulator.Evacuation.EvacuationManager.Instance.SetCountdownLimit(config.evacuationTimeLimit);
            }

            OnLevelLoaded?.Invoke(config);
        }

        public void AdvanceToNextLevel()
        {
            int nextIndex = _currentLevelIndex + 1;
            if (nextIndex <= 3)
            {
                UnlockLevel(nextIndex);
                LoadLevel(nextIndex);
            }
            else
            {
                Debug.Log("[GameProgressionManager] 🏆 ALL LEVELS COMPLETED! Certification Master achieved.");
                LoadLevel(1);
            }
        }

        public void ResetAllProgression()
        {
            PlayerPrefs.DeleteKey(PrefKey_HighestUnlocked);
            PlayerPrefs.DeleteKey(PrefKey_Level1Grade);
            PlayerPrefs.DeleteKey(PrefKey_Level2Grade);
            PlayerPrefs.DeleteKey(PrefKey_Level3Grade);
            PlayerPrefs.Save();
            _currentLevelIndex = 1;
            Debug.Log("[GameProgressionManager] Progression reset to Level 1.");
        }
    }
}
