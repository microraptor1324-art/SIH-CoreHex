using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Configurable fire progression system.
    /// Advances fire severity based on elapsed response time:
    /// 0 - 10s: Small
    /// 10 - 20s: Medium
    /// 20s+: Large
    /// </summary>
    public class FireSpreadSystem : MonoBehaviour
    {
        [Header("Spread Timing Thresholds (Seconds)")]
        [Tooltip("Time before a small fire grows into a medium fire")]
        [SerializeField] private float _smallToMediumTime = 10.0f;

        [Tooltip("Time before a medium fire grows into a dangerous large fire")]
        [SerializeField] private float _mediumToLargeTime = 20.0f;

        [Header("Configuration")]
        [SerializeField] private bool _progressionEnabled = true;

        public static FireSpreadSystem Instance { get; private set; }

        public float SmallToMediumTime { get => _smallToMediumTime; set => _smallToMediumTime = value; }
        public float MediumToLargeTime { get => _mediumToLargeTime; set => _mediumToLargeTime = value; }
        public bool ProgressionEnabled { get => _progressionEnabled; set => _progressionEnabled = value; }
        public float TimeMultiplier { get; set; } = 1.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetSpreadTimers(float smallToMed, float medToLarge)
        {
            _smallToMediumTime = smallToMed;
            _mediumToLargeTime = medToLarge;
        }

        public void UpdateFires(IEnumerable<FireHazard> fires)
        {
            if (!_progressionEnabled || fires == null) return;

            foreach (var fire in fires)
            {
                if (fire == null || fire.State != FireState.Burning) continue;

                float t = fire.ElapsedBurningTime;

                if (t < _smallToMediumTime)
                {
                    if (fire.Severity != FireSeverity.Small)
                        fire.SetSeverity(FireSeverity.Small);
                }
                else if (t < _mediumToLargeTime)
                {
                    if (fire.Severity != FireSeverity.Medium)
                        fire.SetSeverity(FireSeverity.Medium);
                }
                else
                {
                    if (fire.Severity != FireSeverity.Large)
                        fire.SetSeverity(FireSeverity.Large);
                }
            }
        }
    }
}
