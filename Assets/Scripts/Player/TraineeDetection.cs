using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Fire;

namespace ARMiningSimulator.Player
{
    /// <summary>
    /// Handles the fire detection phase of the mining safety simulation.
    /// Requirements:
    /// 1. The trainee must locate and aim the phone camera (or Editor camera) at the fire hazard.
    /// 2. Verifies line-of-sight and camera viewport alignment (hazard within center 60% of screen).
    /// 3. Requires looking at the fire steadily for a dwell time (1.2 seconds) to confirm detection.
    /// 4. Records the exact elapsed reaction time from fire ignition to detection.
    /// </summary>
    public class TraineeDetection : MonoBehaviour
    {
        public static TraineeDetection Instance { get; private set; }

        [Header("Camera & Frustum Settings")]
        [SerializeField] private Camera _traineeCamera;
        [SerializeField] private float _maxDetectionDistance = 25f;
        [SerializeField] private float _maxAlignmentAngle = 32f;

        [Header("Detection Timing")]
        [SerializeField] private float _requiredDwellTime = 1.2f;
        [SerializeField] private float _dwellDecayRate = 2.0f;

        // Runtime State
        private bool _hasDetectedFire = false;
        private float _dwellTimer = 0f;
        private float _dwellProgress = 0f;
        private float _reactionTimer = 0f;
        private bool _isTimerRunning = false;
        private FireHazard _currentTargetHazard = null;
        private FireHazard _confirmedHazard = null;

        // Events
        public static event Action<FireHazard, float> OnTargetingProgress;
        public static event Action<FireHazard, float> OnFireDetected;
        public static event Action OnDetectionLost;

        public bool HasDetectedFire => _hasDetectedFire;
        public float DwellProgress => _dwellProgress;
        public float ReactionTime => _reactionTimer;
        public bool IsTimerRunning => _isTimerRunning;
        public FireHazard CurrentTargetHazard => _currentTargetHazard;
        public FireHazard ConfirmedHazard => _confirmedHazard;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_traineeCamera == null)
            {
                _traineeCamera = GetComponent<Camera>();
                if (_traineeCamera == null) _traineeCamera = Camera.main;
            }
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
            StartStopwatch();
        }

        private void Start()
        {
            // Do not run stopwatch until a fire actually ignites
            _reactionTimer = 0f;
            _isTimerRunning = false;
            _hasDetectedFire = false;
            _confirmedHazard = null;
            _dwellTimer = 0f;
            _dwellProgress = 0f;
            _currentTargetHazard = null;
        }

        public void StartStopwatch()
        {
            _reactionTimer = 0f;
            _isTimerRunning = true;
            _hasDetectedFire = false;
            _confirmedHazard = null;
            _dwellTimer = 0f;
            _dwellProgress = 0f;
            _currentTargetHazard = null;
        }

        private void Update()
        {
            if (_traineeCamera == null)
            {
                _traineeCamera = Camera.main;
                if (_traineeCamera == null) return;
            }

            // Advance reaction timer until detection is confirmed
            if (_isTimerRunning && !_hasDetectedFire)
            {
                _reactionTimer += Time.deltaTime;
            }

            if (_hasDetectedFire) return;

            EvaluateFiresInView();
        }

        private void EvaluateFiresInView()
        {
            if (FireManager.Instance == null) return;

            List<FireHazard> activeFires = FireManager.Instance.GetActiveFires();
            if (activeFires == null || activeFires.Count == 0)
            {
                ResetDwell();
                return;
            }

            FireHazard bestTarget = null;
            float bestDistanceToCenter = float.MaxValue;

            Vector3 camPos = _traineeCamera.transform.position;
            Vector3 camForward = _traineeCamera.transform.forward;

            foreach (var hazard in activeFires)
            {
                if (hazard == null || !hazard.IsIgnited) continue;

                // Test point is slightly elevated above equipment base (around flame center)
                Vector3 targetPoint = hazard.transform.position + Vector3.up * 0.45f;
                Vector3 toTarget = targetPoint - camPos;
                float distance = toTarget.magnitude;

                if (distance > _maxDetectionDistance || distance < 0.1f) continue;

                // 1. Angle Check (trainee facing direction)
                float angle = Vector3.Angle(camForward, toTarget);
                if (angle > _maxAlignmentAngle) continue;

                // 2. Viewport Frustum Check (within central 70% of camera screen)
                Vector3 vp = _traineeCamera.WorldToViewportPoint(targetPoint);
                if (vp.z <= 0.1f) continue; // Behind camera
                if (vp.x < 0.15f || vp.x > 0.85f || vp.y < 0.15f || vp.y > 0.85f) continue;

                // 3. Line of Sight Raycast
                if (Physics.Raycast(camPos, toTarget.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Collide))
                {
                    // Check if hit object is part of the hazard or its equipment
                    bool hitHazardOrParent = hit.collider.transform.IsChildOf(hazard.transform) ||
                                            hazard.transform.IsChildOf(hit.collider.transform) ||
                                            (hazard.FireSource != null && (hit.collider.transform.IsChildOf(hazard.FireSource.transform) || hazard.FireSource.transform.IsChildOf(hit.collider.transform))) ||
                                            Vector3.Distance(hit.point, targetPoint) < 1.0f;
                    if (!hitHazardOrParent)
                    {
                        // Blocked by an obstacle
                        continue;
                    }
                }

                // Calculate distance from screen center (0.5, 0.5)
                float distToCenter = Vector2.Distance(new Vector2(vp.x, vp.y), new Vector2(0.5f, 0.5f));
                if (distToCenter < bestDistanceToCenter)
                {
                    bestDistanceToCenter = distToCenter;
                    bestTarget = hazard;
                }
            }

            if (bestTarget != null)
            {
                // Targeting hazard!
                _currentTargetHazard = bestTarget;
                _dwellTimer += Time.deltaTime;
                _dwellProgress = Mathf.Clamp01(_dwellTimer / _requiredDwellTime);

                OnTargetingProgress?.Invoke(_currentTargetHazard, _dwellProgress);

                if (_dwellProgress >= 1.0f)
                {
                    ConfirmDetection(_currentTargetHazard);
                }
            }
            else
            {
                // Not looking at any fire - smoothly decay dwell progress
                if (_dwellTimer > 0f)
                {
                    _dwellTimer = Mathf.Max(0f, _dwellTimer - Time.deltaTime * _dwellDecayRate);
                    _dwellProgress = Mathf.Clamp01(_dwellTimer / _requiredDwellTime);
                    OnTargetingProgress?.Invoke(_currentTargetHazard, _dwellProgress);

                    if (_dwellTimer <= 0f)
                    {
                        _currentTargetHazard = null;
                        OnDetectionLost?.Invoke();
                    }
                }
                else
                {
                    _currentTargetHazard = null;
                }
            }
        }

        private void ConfirmDetection(FireHazard hazard)
        {
            _hasDetectedFire = true;
            _isTimerRunning = false;
            _confirmedHazard = hazard;

            Debug.Log($"[TraineeDetection] 🔥 FIRE CONFIRMED DETECTED! Equipment: {hazard.TargetName} | Reaction Time: {_reactionTimer:F2}s");

            OnFireDetected?.Invoke(hazard, _reactionTimer);
        }

        private void ResetDwell()
        {
            _dwellTimer = 0f;
            _dwellProgress = 0f;
            _currentTargetHazard = null;
        }

        public void ResetDetection()
        {
            StartStopwatch();
        }
    }
}
