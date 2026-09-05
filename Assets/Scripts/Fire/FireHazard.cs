using System;
using UnityEngine;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Reusable FireHazard component attached to any burning machine or electrical object.
    /// Manages fire state, severity scaling, damage emission, and particle effects.
    /// </summary>
    public class FireHazard : MonoBehaviour
    {
        [Header("Fire Properties")]
        [SerializeField] private GameObject _fireSource;
        [SerializeField] private FireSeverity _severity = FireSeverity.Small;
        [SerializeField] private FireState _state = FireState.Burning;
        [SerializeField] private FireSeverityConfig _config;

        [Header("Runtime Metrics")]
        [SerializeField] private float _elapsedBurningTime = 0f;
        [SerializeField] private float _extinguishHealth = 100f;

        // Visual References
        private ParticleSystem _fireParticles;
        private ParticleSystem _smokeParticles;
        private Light _fireLight;
        private FireLightFlicker _lightFlicker;

        // Events
        public event Action<FireSeverity> OnSeverityChanged;
        public event Action OnExtinguished;

        public GameObject FireSource => _fireSource;
        public FireSeverity Severity => _severity;
        public FireState State => _state;
        public bool IsIgnited => _state == FireState.Burning;
        public string TargetName => _fireSource != null ? _fireSource.name : gameObject.name;
        public float BurnDuration => _elapsedBurningTime;
        public FireSeverityConfig CurrentConfig => _config;
        public float DamageRate => _config != null ? _config.damageRate : 5f;
        public float DamageRadius => _config != null ? _config.damageRadius : 2.0f;
        public bool IsExtinguishable => _config != null ? _config.isExtinguishable : true;
        public float ElapsedBurningTime => _elapsedBurningTime;

        private void Awake()
        {
            if (_config == null)
            {
                _config = FireSeverityConfig.GetDefault(_severity);
            }
        }

        public void Initialize(GameObject source, FireSeverity initialSeverity = FireSeverity.Small)
        {
            _fireSource = source;
            _severity = initialSeverity;
            _state = FireState.Burning;
            _elapsedBurningTime = 0f;
            _extinguishHealth = 100f;

            // Generate procedural particle systems if not already present
            if (_fireParticles == null || _smokeParticles == null)
            {
                var built = ProceduralFireParticleBuilder.BuildFireEffect(transform);
                _fireParticles = built.fire;
                _smokeParticles = built.smoke;
                _fireLight = built.fireLight;
                if (_fireLight != null)
                    _lightFlicker = _fireLight.GetComponent<FireLightFlicker>();
            }

            ApplySeverityConfig(_severity);
        }

        private void Update()
        {
            if (_state != FireState.Burning) return;

            _elapsedBurningTime += Time.deltaTime;
        }

        public void SetSeverity(FireSeverity newSeverity)
        {
            if (_severity == newSeverity && _state == FireState.Burning) return;

            _severity = newSeverity;
            ApplySeverityConfig(newSeverity);
            OnSeverityChanged?.Invoke(_severity);

            Debug.Log($"[FireHazard] Fire on '{(_fireSource != null ? _fireSource.name : name)}' transitioned to {newSeverity} severity.");
        }

        private void ApplySeverityConfig(FireSeverity level)
        {
            _config = FireSeverityConfig.GetDefault(level);

            // Update particle emission and scaling
            if (_fireParticles != null)
            {
                var emission = _fireParticles.emission;
                emission.rateOverTime = _config.fireEmissionRate;

                var main = _fireParticles.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.25f * _config.particleScale, 0.5f * _config.particleScale);
            }

            if (_smokeParticles != null)
            {
                var emission = _smokeParticles.emission;
                emission.rateOverTime = _config.smokeEmissionRate;

                var main = _smokeParticles.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.35f * _config.particleScale, 0.75f * _config.particleScale);
            }

            if (_fireLight != null)
            {
                _fireLight.intensity = _config.lightIntensity;
                _fireLight.range = _config.lightRange;
                if (_lightFlicker != null)
                    _lightFlicker.SetBaseIntensity(_config.lightIntensity);
            }
        }

        /// <summary>
        /// Extinguishes the fire by applying extinguisher agent (Phase 8).
        /// Only Small and Medium fires can be fought.
        /// </summary>
        public bool Extinguish(float amount)
        {
            if (!IsExtinguishable || _state == FireState.Extinguished)
                return false;

            _extinguishHealth -= amount;

            if (_extinguishHealth <= 0f)
            {
                CompleteExtinguish();
                return true;
            }

            return false;
        }

        private void CompleteExtinguish()
        {
            _state = FireState.Extinguished;

            if (_fireParticles != null) _fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_smokeParticles != null) _smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_fireLight != null) _fireLight.enabled = false;

            OnExtinguished?.Invoke();
            Debug.Log($"[FireHazard] Fire on '{(_fireSource != null ? _fireSource.name : name)}' successfully EXTINGUISHED!");
        }
    }
}
