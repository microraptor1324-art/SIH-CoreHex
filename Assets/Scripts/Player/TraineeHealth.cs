using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.Player
{
    /// <summary>
    /// Manages the trainee's health, damage accumulation, and environmental survival.
    /// Requirements:
    /// 1. Starts with 100 HP.
    /// 2. Proximity-based heat damage from active fires (scales inversely with distance).
    /// 3. Ambient toxic smoke inhalation damage if large fires persist without evacuation.
    /// 4. Triggers damage feedback events (visual red vignette, low health warning).
    /// 5. Triggers Incapacitated (fail state) if HP reaches 0.
    /// </summary>
    public class TraineeHealth : MonoBehaviour
    {
        public static TraineeHealth Instance { get; private set; }

        [Header("Health Settings")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth = 100f;
        [SerializeField] private float _lowHealthThreshold = 35f;

        [Header("Ambient Hazard Settings")]
        [SerializeField] private float _ambientSmokeDamageRate = 1.5f;
        [SerializeField] private float _largeFireSmokeDelay = 25f;

        [Header("References")]
        [SerializeField] private Camera _traineeCamera;

        // Runtime State
        private bool _isIncapacitated = false;
        private float _lastDamageTime = -999f;
        private float _damageFlashTimer = 0f;
        private string _lastDamageCause = "Fire Hazard";

        // Events
        public static event Action<float, float> OnHealthChanged; // current, max
        public static event Action<float, string> OnDamageTaken;  // damageAmount, source
        public static event Action<string> OnTraineeIncapacitated; // reason
        public static event Action OnHealthDepleted;
        public static event Action OnTraineeRevived;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float HealthPercent => Mathf.Clamp01(_currentHealth / _maxHealth);
        public bool IsIncapacitated => _isIncapacitated;
        public bool IsLowHealth => _currentHealth < _lowHealthThreshold && !_isIncapacitated;
        public float DamageFlashAlpha => Mathf.Clamp01(_damageFlashTimer / 0.35f);
        public string LastDamageCause => _lastDamageCause;

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

            _currentHealth = _maxHealth;
        }

        private void Update()
        {
            if (_isIncapacitated) return;

            if (_traineeCamera == null)
            {
                _traineeCamera = Camera.main;
                if (_traineeCamera == null) return;
            }

            // Decay visual damage flash
            if (_damageFlashTimer > 0f)
            {
                _damageFlashTimer -= Time.deltaTime;
            }

            EvaluateFireDamage();
        }

        private void EvaluateFireDamage()
        {
            if (FireManager.Instance == null) return;

            List<FireHazard> activeFires = FireManager.Instance.GetActiveFires();
            if (activeFires == null || activeFires.Count == 0) return;

            Vector3 traineePos = _traineeCamera.transform.position;
            float totalDamageThisFrame = 0f;
            string primaryCause = "";
            float maxDamageFromSource = 0f;

            // 1. Proximity Heat Damage from each fire
            foreach (var hazard in activeFires)
            {
                if (hazard == null || !hazard.IsIgnited) continue;

                float dist = Vector3.Distance(traineePos, hazard.transform.position);
                FireSeverityConfig config = hazard.CurrentConfig;
                float radius = config != null ? config.damageRadius : 2.5f;
                float baseRate = config != null ? config.damageRate : 10f;

                if (dist < radius)
                {
                    // Closer = more severe damage (quadratic falloff)
                    float closeness = 1.0f - Mathf.Clamp01(dist / radius);
                    float damageAmount = baseRate * (closeness * closeness) * Time.deltaTime;
                    totalDamageThisFrame += damageAmount;

                    if (damageAmount > maxDamageFromSource)
                    {
                        maxDamageFromSource = damageAmount;
                        primaryCause = $"{hazard.Severity} Heat Exposure from {hazard.TargetName}";
                    }
                }

                // 2. Ambient Smoke Inhalation (if large fires rage unchecked)
                if (hazard.Severity == FireSeverity.Large && hazard.BurnDuration > _largeFireSmokeDelay)
                {
                    // Check if player is already inside safe zone
                    bool inSafeZone = IsPlayerInSafeZone(traineePos);
                    if (!inSafeZone)
                    {
                        float smokeDamage = _ambientSmokeDamageRate * Time.deltaTime;
                        totalDamageThisFrame += smokeDamage;

                        if (smokeDamage > maxDamageFromSource)
                        {
                            maxDamageFromSource = smokeDamage;
                            primaryCause = "Toxic Mine Smoke Inhalation";
                        }
                    }
                }
            }

            if (totalDamageThisFrame > 0.001f)
            {
                ApplyDamage(totalDamageThisFrame, primaryCause);
            }
        }

        private bool IsPlayerInSafeZone(Vector3 playerPos)
        {
            // If MiningEnvironmentGenerator exists, check distance to SafeZone
            var env = FindFirstObjectByType<MiningEnvironmentGenerator>();
            if (env != null && env.IsGenerated)
            {
                float distToExit = Vector3.Distance(playerPos, env.EmergencyExitPosition);
                if (distToExit < 2.0f) return true; // Inside safe evacuation zone
            }
            return false;
        }

        public void ApplyDamage(float damageAmount, string cause)
        {
            if (_isIncapacitated) return;

            _currentHealth -= damageAmount;
            _damageFlashTimer = 0.35f;
            _lastDamageTime = Time.time;
            _lastDamageCause = string.IsNullOrEmpty(cause) ? "Fire Hazard" : cause;

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                _isIncapacitated = true;
                Debug.LogWarning($"[TraineeHealth] 💀 TRAINEE INCAPACITATED! Reason: {_lastDamageCause}");
                OnTraineeIncapacitated?.Invoke(_lastDamageCause);
                OnHealthDepleted?.Invoke();
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnDamageTaken?.Invoke(damageAmount, _lastDamageCause);
        }

        public void Heal(float amount)
        {
            if (_isIncapacitated) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            _isIncapacitated = false;
            _damageFlashTimer = 0f;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            OnTraineeRevived?.Invoke();
            Debug.Log("[TraineeHealth] Trainee health fully restored to 100 HP.");
        }
    }
}
