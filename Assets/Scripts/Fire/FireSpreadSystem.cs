using System.Collections.Generic;
using UnityEngine;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Configurable fire progression system.
    /// Advances fire severity based on elapsed response time:
    /// 0 - 60s: Small incipient fire (Can be fought with extinguisher near safety area).
    /// 60s+: Big critical inferno (Grows into large fire; player must stay in safety).
    /// </summary>
    public class FireSpreadSystem : MonoBehaviour
    {
        [Header("Spread Timing Thresholds (Seconds)")]
        [Tooltip("Time before a small fire starts to grow and becomes a big fire (strictly 60s / 1 minute minimum)")]
        [SerializeField] private float _smallToMediumTime = 60.0f;

        [Tooltip("Time before reaching maximum inferno (strictly 60s minimum)")]
        [SerializeField] private float _mediumToLargeTime = 60.0f;

        [Header("Configuration")]
        [SerializeField] private bool _progressionEnabled = true;

        public static FireSpreadSystem Instance { get; private set; }

        public float SmallToMediumTime
        {
            get => Mathf.Max(60.0f, _smallToMediumTime);
            set => _smallToMediumTime = Mathf.Max(60.0f, value);
        }

        public float MediumToLargeTime
        {
            get => Mathf.Max(60.0f, _mediumToLargeTime);
            set => _mediumToLargeTime = Mathf.Max(60.0f, value);
        }

        public float FireGrowthTime => SmallToMediumTime;
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

            // Strictly enforce 60s (1 full minute) so that any legacy serialized scene values (e.g. 10s) are overridden
            _smallToMediumTime = Mathf.Max(60.0f, _smallToMediumTime);
            _mediumToLargeTime = Mathf.Max(60.0f, _mediumToLargeTime);
            TimeMultiplier = 1.0f;
        }

        public void SetSpreadTimers(float smallToMed, float medToLarge)
        {
            _smallToMediumTime = Mathf.Max(60.0f, smallToMed);
            _mediumToLargeTime = Mathf.Max(60.0f, medToLarge);
        }

        public void UpdateFires(IEnumerable<FireHazard> fires)
        {
            if (!_progressionEnabled || fires == null) return;

            float growthThreshold = SmallToMediumTime; // Guaranteed >= 60.0s (1 full minute)

            foreach (var fire in fires)
            {
                if (fire == null || fire.State != FireState.Burning) continue;

                // Fire must strictly stay small for at least 1 full minute (60 seconds)
                // Check both the scenario timer and the individual fire's elapsed burning time
                float scenarioTime = FireManager.Instance != null ? FireManager.Instance.ScenarioTimer : fire.ElapsedBurningTime;

                if (scenarioTime < growthThreshold || fire.ElapsedBurningTime < growthThreshold)
                {
                    if (fire.Severity != FireSeverity.Small)
                        fire.SetSeverity(FireSeverity.Small);
                }
                else
                {
                    // After 60 seconds (1 minute), fire starts to grow and becomes a Big fire
                    if (fire.Severity != FireSeverity.Large)
                        fire.SetSeverity(FireSeverity.Large);
                }
            }
        }
    }
}
