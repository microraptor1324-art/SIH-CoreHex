using System;
using UnityEngine;
using ARMiningSimulator.Environment;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Player;

namespace ARMiningSimulator.Evacuation
{
    /// <summary>
    /// Coordinates the real-time underground mine evacuation phase:
    /// - Activates 3D animated path arrows to Emergency Exit and Safe Zone.
    /// - Tracks remaining evacuation time (30s window before critical smoke levels).
    /// - Detects trainee physical arrival on the green Safe Zone pad (radius <= 1.25m).
    /// - Halts health damage upon arrival and awards speed bonus scores.
    /// </summary>
    public class EvacuationManager : MonoBehaviour
    {
        public static EvacuationManager Instance { get; private set; }

        [Header("Evacuation Settings")]
        [SerializeField] private float _countdownLimit = 30f;
        [SerializeField] private float _safeZoneRadius = 1.25f;

        [Header("References")]
        [SerializeField] private EvacuationPathVisualizer _pathVisualizer;
        [SerializeField] private Camera _traineeCamera;
        [SerializeField] private MiningEnvironmentGenerator _environmentGenerator;

        // Runtime State
        private bool _isEvacuating = false;
        private bool _isSafeZoneReached = false;
        private float _evacuationTimer = 0f;
        private float _currentDistanceToSafeZone = 999f;
        private int _speedBonusScore = 0;

        private Vector3 _exitPosition;
        private Vector3 _safeZonePosition;

        // Events
        public static event Action OnEvacuationStarted;
        public static event Action<float, int> OnSafeZoneReached; // duration, bonusScore
        public static event Action OnEvacuationFailed;

        public bool IsEvacuating => _isEvacuating;
        public bool IsSafeZoneReached => _isSafeZoneReached;
        public float EvacuationTimer => _evacuationTimer;
        public float TimeRemaining => Mathf.Max(0f, _countdownLimit - _evacuationTimer);
        public float CountdownLimit => _countdownLimit;
        public float DistanceToSafeZone => _currentDistanceToSafeZone;
        public int SpeedBonusScore => _speedBonusScore;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_pathVisualizer == null)
                _pathVisualizer = GetComponent<EvacuationPathVisualizer>();
            if (_environmentGenerator == null)
                _environmentGenerator = FindFirstObjectByType<MiningEnvironmentGenerator>();
        }

        public void SetCountdownLimit(float seconds)
        {
            _countdownLimit = Mathf.Max(5f, seconds);
        }

        private void OnEnable()
        {
            DecisionManager.OnDecisionsCompleted += HandleDecisionsCompleted;
        }

        private void OnDisable()
        {
            DecisionManager.OnDecisionsCompleted -= HandleDecisionsCompleted;
        }

        private void HandleDecisionsCompleted()
        {
            // Begin evacuation immediately once decisions conclude
            StartEvacuation();
        }

        public void StartEvacuation()
        {
            ResolveTargetPositions();

            if (_traineeCamera == null)
                _traineeCamera = Camera.main;

            Vector3 startPos = _traineeCamera != null ? _traineeCamera.transform.position : Vector3.zero;

            _isEvacuating = true;
            _isSafeZoneReached = false;
            _evacuationTimer = 0f;
            _speedBonusScore = 0;

            if (_pathVisualizer != null)
            {
                _pathVisualizer.ShowPath(startPos, _exitPosition, _safeZonePosition);
            }

            Debug.Log($"[EvacuationManager] 🏃 EVACUATION INITIATED! Target Safe Zone: {_safeZonePosition} | Limit: {_countdownLimit}s");
            OnEvacuationStarted?.Invoke();
        }

        private void ResolveTargetPositions()
        {
            if (_environmentGenerator == null)
                _environmentGenerator = FindFirstObjectByType<MiningEnvironmentGenerator>();

            if (_environmentGenerator != null && _environmentGenerator.IsGenerated)
            {
                _exitPosition = _environmentGenerator.EmergencyExitPosition;
                // Safe zone is located 0.65m past the exit doorway threshold
                _safeZonePosition = _exitPosition + Vector3.forward * 0.65f;

                // If SafeZone object exists, use its exact position
                var safeZoneGo = GameObject.Find("SafeZone");
                if (safeZoneGo != null)
                {
                    _safeZonePosition = safeZoneGo.transform.position;
                }
            }
            else
            {
                // Fallback default coordinates
                _exitPosition = new Vector3(0, 0, 2.2f);
                _safeZonePosition = new Vector3(0, 0, 2.85f);
            }
        }

        private void Update()
        {
            if (!_isEvacuating || _isSafeZoneReached) return;

            if (_traineeCamera == null)
            {
                _traineeCamera = Camera.main;
                if (_traineeCamera == null) return;
            }

            _evacuationTimer += Time.deltaTime;

            Vector3 playerPos = _traineeCamera.transform.position;
            // Measure 2D ground distance ignoring vertical head height
            Vector2 player2D = new Vector2(playerPos.x, playerPos.z);
            Vector2 safeZone2D = new Vector2(_safeZonePosition.x, _safeZonePosition.z);
            _currentDistanceToSafeZone = Vector2.Distance(player2D, safeZone2D);

            // Update path chevrons periodically
            if (_pathVisualizer != null && Time.frameCount % 5 == 0)
            {
                _pathVisualizer.UpdatePlayerPosition(playerPos);
            }

            // Check arrival inside Safe Zone radius
            if (_currentDistanceToSafeZone <= _safeZoneRadius)
            {
                CompleteEvacuation();
            }
            // Check timeout
            else if (_evacuationTimer >= _countdownLimit)
            {
                FailEvacuation();
            }
        }

        private void CompleteEvacuation()
        {
            _isSafeZoneReached = true;
            _isEvacuating = false;

            // Speed bonus calculation
            if (_evacuationTimer <= 10f)
                _speedBonusScore = 150;
            else if (_evacuationTimer <= 20f)
                _speedBonusScore = 100;
            else
                _speedBonusScore = 50;

            if (_pathVisualizer != null)
                _pathVisualizer.HidePath();

            // Fully restore health upon reaching safety
            if (TraineeHealth.Instance != null)
            {
                TraineeHealth.Instance.ResetHealth();
            }

            Debug.Log($"[EvacuationManager] 🛡️ SAFE ZONE REACHED! Time: {_evacuationTimer:F2}s | Speed Bonus: +{_speedBonusScore} PTS");
            OnSafeZoneReached?.Invoke(_evacuationTimer, _speedBonusScore);
        }

        private void FailEvacuation()
        {
            _isEvacuating = false;

            if (_pathVisualizer != null)
                _pathVisualizer.HidePath();

            Debug.LogWarning("[EvacuationManager] 💀 Evacuation timed out! Trainee overcome by toxic smoke.");

            if (TraineeHealth.Instance != null)
            {
                TraineeHealth.Instance.ApplyDamage(999f, "Evacuation Timeout — Toxic Gas Accumulation");
            }

            OnEvacuationFailed?.Invoke();
        }

        public void ResetEvacuation()
        {
            _isEvacuating = false;
            _isSafeZoneReached = false;
            _evacuationTimer = 0f;
            _speedBonusScore = 0;

            if (_pathVisualizer != null)
                _pathVisualizer.HidePath();
        }
    }
}
