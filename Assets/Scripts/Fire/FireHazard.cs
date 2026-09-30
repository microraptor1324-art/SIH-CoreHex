using System;
using UnityEngine;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Reusable FireHazard component attached to any burning machine or electrical object.
    /// Manages fire state, hazard profile, severity scaling, damage emission, and particle effects.
    /// </summary>
    public class FireHazard : MonoBehaviour
    {
        [Header("Fire Properties")]
        [SerializeField] private GameObject _fireSource;
        [SerializeField] private FireSeverity _severity = FireSeverity.Small;
        [SerializeField] private FireState _state = FireState.Burning;
        [SerializeField] private FireSeverityConfig _config;
        [SerializeField] private FireHazardType _hazardType = FireHazardType.StandardEquipment;

        [Header("Incident Investigation Profile")]
        [SerializeField] private string _hazardCause = "Equipment Overheat";
        [SerializeField] private string _visualClue = "";
        [SerializeField] private string _thermalReading = "";

        [Header("Runtime Metrics")]
        [SerializeField] private float _elapsedBurningTime = 0f;
        [SerializeField] private float _extinguishHealth = 100f;

        // Visual References
        private ParticleSystem _fireParticles;
        private ParticleSystem _smokeParticles;
        private ParticleSystem _sparksParticles;
        private Light _fireLight;
        private FireLightFlicker _lightFlicker;
        private GameObject _flameMesh;
        private GameObject _hazardMarker;

        // Events
        public event Action<FireSeverity> OnSeverityChanged;
        public event Action OnExtinguished;

        public GameObject FireSource => _fireSource;
        public GameObject TargetEquipment => _fireSource;
        public FireSeverity Severity => _severity;
        public FireState State => _state;
        public FireHazardType HazardType => _hazardType;
        public string HazardCause => _hazardCause;
        public string VisualClue => _visualClue;
        public string ThermalReading => _thermalReading;
        public bool IsIgnited => _state == FireState.Burning;
        public string TargetName => _fireSource != null ? _fireSource.name : gameObject.name;
        public float BurnDuration => _elapsedBurningTime;
        public FireSeverityConfig CurrentConfig => _config;
        public float DamageRate => _config != null ? _config.damageRate : 5f;
        public float DamageRadius => _config != null ? _config.damageRadius : 2.0f;
        public bool IsExtinguishable => _config != null ? _config.isExtinguishable : true;
        public float ElapsedBurningTime => _elapsedBurningTime;
        public const float MaxExtinguishHealth = 100f;
        public float ExtinguishProgress => Mathf.Clamp01(1f - _extinguishHealth / MaxExtinguishHealth);

        private void Awake()
        {
            if (_config == null)
            {
                _config = FireSeverityConfig.GetDefault(_severity);
            }

            var col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.55f;
            }
        }

        public void Initialize(
            GameObject source,
            FireSeverity initialSeverity = FireSeverity.Small,
            FireHazardType hazardType = FireHazardType.StandardEquipment,
            string hazardCause = "",
            string visualClue = "",
            string thermalReading = "")
        {
            _fireSource = source;
            _severity = initialSeverity;
            _hazardType = hazardType;
            if (!string.IsNullOrEmpty(hazardCause)) _hazardCause = hazardCause;
            if (!string.IsNullOrEmpty(visualClue)) _visualClue = visualClue;
            if (!string.IsNullOrEmpty(thermalReading)) _thermalReading = thermalReading;

            _state = FireState.Burning;
            float medTime = FireSpreadSystem.Instance != null ? FireSpreadSystem.Instance.SmallToMediumTime : 60f;
            float largeTime = FireSpreadSystem.Instance != null ? FireSpreadSystem.Instance.MediumToLargeTime : 60f;
            if (_severity == FireSeverity.Small) _elapsedBurningTime = 0f;
            else if (_severity == FireSeverity.Medium) _elapsedBurningTime = medTime + 0.1f;
            else if (_severity == FireSeverity.Large) _elapsedBurningTime = largeTime + 0.1f;
            _extinguishHealth = 100f;

            // Generate procedural particle systems tailored to hazard type
            if (_fireParticles == null || _smokeParticles == null)
            {
                var built = ProceduralFireParticleBuilder.BuildFireEffect(transform, _hazardType, _fireSource != null ? _fireSource.name : gameObject.name);
                _fireParticles = built.fire;
                _smokeParticles = built.smoke;
                _fireLight = built.fireLight;
                _sparksParticles = built.sparks;
                _flameMesh = built.flameMesh;
                _hazardMarker = built.hazardMarker;

                if (_fireLight != null)
                    _lightFlicker = _fireLight.GetComponent<FireLightFlicker>();
            }

            ApplySeverityConfig(_severity);
        }

        private void Update()
        {
            if (_state != FireState.Burning) return;

            float mult = FireSpreadSystem.Instance != null ? FireSpreadSystem.Instance.TimeMultiplier : 1.0f;
            _elapsedBurningTime += Time.deltaTime * mult;
        }

        public void SetSeverity(FireSeverity newSeverity)
        {
            if (_severity == newSeverity && _state == FireState.Burning) return;

            _severity = newSeverity;
            float medThreshold = FireSpreadSystem.Instance != null ? FireSpreadSystem.Instance.SmallToMediumTime : 60.0f;
            float largeThreshold = FireSpreadSystem.Instance != null ? FireSpreadSystem.Instance.MediumToLargeTime : 60.0f;

            if (newSeverity == FireSeverity.Small)
            {
                if (_elapsedBurningTime >= medThreshold) _elapsedBurningTime = 0f;
            }
            else if (newSeverity == FireSeverity.Medium)
            {
                if (_elapsedBurningTime < medThreshold || _elapsedBurningTime >= largeThreshold)
                    _elapsedBurningTime = medThreshold + 0.1f;
            }
            else if (newSeverity == FireSeverity.Large)
            {
                if (_elapsedBurningTime < largeThreshold)
                    _elapsedBurningTime = largeThreshold + 0.1f;
            }

            ApplySeverityConfig(newSeverity);
            OnSeverityChanged?.Invoke(_severity);

            Debug.Log($"[FireHazard] Fire on '{(_fireSource != null ? _fireSource.name : name)}' transitioned to {newSeverity} severity at {_elapsedBurningTime:F1}s.");
        }

        private void ApplySeverityConfig(FireSeverity level)
        {
            _config = FireSeverityConfig.GetDefault(level);

            // 1. Update fire particles
            if (_fireParticles != null)
            {
                var emission = _fireParticles.emission;
                emission.rateOverTime = _config.fireEmissionRate;

                var main = _fireParticles.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.20f * _config.particleScale, 0.45f * _config.particleScale);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * Mathf.Sqrt(_config.particleScale), 1.6f * Mathf.Sqrt(_config.particleScale));
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f * Mathf.Sqrt(_config.particleScale), 0.75f * Mathf.Sqrt(_config.particleScale));

                var shape = _fireParticles.shape;
                shape.radius = Mathf.Clamp(0.10f * _config.particleScale, 0.05f, 0.60f);
                shape.angle = level == FireSeverity.Large ? 24f : 12f;
            }

            // 2. Update smoke plume
            if (_smokeParticles != null)
            {
                var emission = _smokeParticles.emission;
                emission.rateOverTime = _config.smokeEmissionRate;

                var main = _smokeParticles.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.25f * _config.particleScale, 0.70f * _config.particleScale);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f * Mathf.Sqrt(_config.particleScale), 1.3f * Mathf.Sqrt(_config.particleScale));
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, level == FireSeverity.Large ? 4.5f : 2.0f);
            }

            // 3. Update sparks & burning embers
            if (_sparksParticles != null)
            {
                var emission = _sparksParticles.emission;
                emission.rateOverTime = level == FireSeverity.Large ? 50f : (level == FireSeverity.Medium ? 20f : 5f);
            }

            // 4. Update firelight glow & flicker
            if (_fireLight != null)
            {
                _fireLight.intensity = _config.lightIntensity;
                _fireLight.range = _config.lightRange;
                if (_lightFlicker != null)
                    _lightFlicker.SetBaseIntensity(_config.lightIntensity);
            }

            // 5. Update 3D flame mesh base scale (using ProceduralFlameMeshAnimator)
            if (_flameMesh != null)
            {
                var animator = _flameMesh.GetComponent<ProceduralFlameMeshAnimator>();
                if (animator != null)
                {
                    animator.SetBaseScale(_config.particleScale);
                }
                else
                {
                    _flameMesh.transform.localScale = Vector3.one * _config.particleScale;
                }
            }

            // 6. Update in-world hazard beacon position & badge text
            if (_hazardMarker != null)
            {
                float beaconHeight = Mathf.Max(0.65f, 0.35f + (_config.particleScale * 0.95f));
                _hazardMarker.transform.localPosition = new Vector3(0, beaconHeight, 0);

                var tm = _hazardMarker.GetComponent<TextMesh>();
                if (tm != null)
                {
                    string cleanName = TargetName.Replace("Machine_", "").Replace("Electrical_", "").Replace("(Clone)", "").ToUpper();
                    if (level == FireSeverity.Small)
                    {
                        tm.text = $"🔥 SMALL INCIPIENT FIRE (FIGHTABLE)\n{cleanName}";
                        tm.color = new Color(1.0f, 0.85f, 0.20f);
                    }
                    else
                    {
                        tm.text = $"🚨 BIG CRITICAL INFERNO!\n{cleanName} (TOO LARGE — STAY IN SAFETY)";
                        tm.color = new Color(1.0f, 0.15f, 0.15f);
                    }
                }
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
            if (_sparksParticles != null) _sparksParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_fireLight != null) _fireLight.enabled = false;
            if (_flameMesh != null) _flameMesh.SetActive(false);
            if (_hazardMarker != null) _hazardMarker.SetActive(false);

            ApplyBurntLook();

            OnExtinguished?.Invoke();
            Debug.Log($"[FireHazard] Fire on '{(_fireSource != null ? _fireSource.name : name)}' successfully EXTINGUISHED!");
        }

        /// <summary>
        /// Gives the machine that caught fire a charred, greyish look once its fire is put out, so
        /// the trainee can visually identify the origin machine during the tap-identification stage.
        /// Darkens every renderer's material color toward sooty grey and flattens shininess for a
        /// matte burnt finish. Accessing Renderer.materials (plural) instantiates a per-renderer
        /// copy the first time it's touched, so this never affects the shared procedural material
        /// still used by every other (unburned) machine.
        /// </summary>
        private void ApplyBurntLook()
        {
            GameObject target = _fireSource != null ? _fireSource : gameObject;
            var renderers = target.GetComponentsInChildren<Renderer>();
            Color burntColor = new Color(0.14f, 0.13f, 0.12f);

            foreach (var rend in renderers)
            {
                var mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i].color = Color.Lerp(mats[i].color, burntColor, 0.85f);
                    if (mats[i].HasProperty("_Metallic")) mats[i].SetFloat("_Metallic", 0f);
                    if (mats[i].HasProperty("_Smoothness")) mats[i].SetFloat("_Smoothness", 0.08f);
                    else if (mats[i].HasProperty("_Glossiness")) mats[i].SetFloat("_Glossiness", 0.08f);
                }
                rend.materials = mats;
            }
        }
    }
}
