using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif
using ARMiningSimulator.Fire;
using ARMiningSimulator.Player;

namespace ARMiningSimulator.Extinguisher
{
    /// <summary>
    /// First-person fire extinguisher controller.
    /// Handles:
    /// - Equipping 3D viewmodel canister (CO2, Dry Powder, or Foam).
    /// - Discharging pressurized particle spray cone.
    /// - Raycasting to flame base and applying suppression damage.
    /// - Detecting electrical shock hazard when using conductive agents (Water/Foam) on live panels.
    /// </summary>
    public class ExtinguisherController : MonoBehaviour
    {
        public static ExtinguisherController Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera _traineeCamera;

        // Viewmodel runtime references
        private GameObject _extinguisherViewmodel;
        private ParticleSystem _sprayParticles;
        private Transform _nozzleTip;
        private ExtinguisherConfig _activeConfig;

        // State
        private bool _isEquipped = false;
        private bool _isDischarging = false;
        private float _remainingCharge = 1.0f; // 1.0 (full) to 0.0 (empty)
        private bool _externalTriggerHeld = false; // from on-screen touch UI

        // Events
        public static event Action<ExtinguisherConfig> OnExtinguisherEquipped;
        public static event Action OnDischargeStarted;
        public static event Action OnDischargeStopped;
        public static event Action<float> OnChargeChanged; // 0-1
        public static event Action<FireHazard> OnSingleFireExtinguished;
        public static event Action OnAllActiveFiresExtinguished;

        public bool IsEquipped => _isEquipped;
        public bool IsDischarging => _isDischarging;
        public float RemainingCharge => _remainingCharge;
        public ExtinguisherConfig ActiveConfig => _activeConfig;

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

        public void EquipExtinguisher(ExtinguisherType type)
        {
            Unequip();

            _activeConfig = ExtinguisherConfig.GetConfig(type);
            _remainingCharge = 1.0f;

            if (_traineeCamera == null) _traineeCamera = Camera.main;

            Transform mountParent = _traineeCamera != null ? _traineeCamera.transform : transform;
            var built = ProceduralExtinguisherBuilder.BuildExtinguisher(mountParent, _activeConfig);
            _extinguisherViewmodel = built.root;
            _sprayParticles = built.spray;
            _nozzleTip = built.nozzleTip;

            _isEquipped = true;
            Debug.Log($"[ExtinguisherController] Equipped: {_activeConfig.displayName} ({_activeConfig.colorBandName})");

            OnExtinguisherEquipped?.Invoke(_activeConfig);
            OnChargeChanged?.Invoke(_remainingCharge);
        }

        public void Unequip()
        {
            StopDischarge();

            if (_extinguisherViewmodel != null)
            {
                Destroy(_extinguisherViewmodel);
                _extinguisherViewmodel = null;
            }

            _isEquipped = false;
            _activeConfig = null;
        }

        public void SetExternalTrigger(bool isHeld)
        {
            _externalTriggerHeld = isHeld;
        }

        private void Update()
        {
            if (!_isEquipped) return;

            bool wantsDischarge = CheckInput();

            if (wantsDischarge && _remainingCharge > 0f)
            {
                if (!_isDischarging) StartDischarge();
                ProcessDischarge();
            }
            else
            {
                if (_isDischarging) StopDischarge();
            }
        }

        private bool CheckInput()
        {
            if (_externalTriggerHeld) return true;

#if UNITY_EDITOR
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
#endif
            return false;
        }

        public void StartDischarge()
        {
            if (!_isEquipped || _remainingCharge <= 0f) return;

            _isDischarging = true;

            if (_sprayParticles != null)
            {
                var emission = _sprayParticles.emission;
                emission.enabled = true;
                _sprayParticles.Play();
            }

            OnDischargeStarted?.Invoke();
        }

        public void StopDischarge()
        {
            if (!_isDischarging) return;

            _isDischarging = false;

            if (_sprayParticles != null)
            {
                var emission = _sprayParticles.emission;
                emission.enabled = false;
                _sprayParticles.Stop();
            }

            OnDischargeStopped?.Invoke();
        }

        private void ProcessDischarge()
        {
            // 1. Deplete canister charge
            float duration = _activeConfig != null ? _activeConfig.maxDischargeDuration : 12f;
            _remainingCharge = Mathf.Max(0f, _remainingCharge - Time.deltaTime / duration);
            OnChargeChanged?.Invoke(_remainingCharge);

            if (_remainingCharge <= 0f)
            {
                StopDischarge();
                Debug.LogWarning("[ExtinguisherController] Canister pressure fully depleted!");
                return;
            }

            // 2. Perform conical suppression check
            if (_traineeCamera == null) return;

            Vector3 sprayOrigin = _nozzleTip != null ? _nozzleTip.position : _traineeCamera.transform.position;
            Vector3 sprayDir = _traineeCamera.transform.forward;
            float maxRange = _activeConfig != null ? _activeConfig.effectiveRange : 3.5f;

            // SphereCast along forward aim vector
            RaycastHit[] hits = Physics.SphereCastAll(sprayOrigin, 0.45f, sprayDir, maxRange, ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return;

            float suppressionAmount = (_activeConfig != null ? _activeConfig.suppressionRate : 35f) * Time.deltaTime;

            foreach (var hit in hits)
            {
                FireHazard hazard = hit.collider.GetComponentInParent<FireHazard>();
                if (hazard == null) hazard = hit.collider.GetComponentInChildren<FireHazard>();

                if (hazard != null && hazard.IsIgnited)
                {
                    // Check electrical shock hazard
                    if (_activeConfig != null && _activeConfig.isHazardousOnElectrical)
                    {
                        var elec = hazard.GetComponentInParent<ARMiningSimulator.Environment.ElectricalEquipment>();
                        if (elec != null)
                        {
                            // Electrical electrocution damage!
                            TraineeHealth.Instance?.ApplyDamage(40f * Time.deltaTime, "ELECTROCUTION HAZARD: Conductive Water/Foam on Live Electrical Panel!");
                        }
                    }

                    // Apply suppression
                    hazard.Extinguish(suppressionAmount);

                    // If fire is Big (>60s / 1 minute), it is not extinguishable by portable canister
                    if (!hazard.IsExtinguishable)
                    {
                        TraineeHealth.Instance?.ApplyDamage(15f * Time.deltaTime, "INTENSE RADIANT HEAT: Big fire cannot be fought! Retreat to Safety Area!");
                    }

                    if (!hazard.IsIgnited)
                    {
                        Debug.Log($"[ExtinguisherController] 🔥 Fire suppressed on {hazard.TargetName}!");
                        OnSingleFireExtinguished?.Invoke(hazard);
                        CheckAllFiresCleared();
                    }
                }
            }
        }

        private void CheckAllFiresCleared()
        {
            if (FireManager.Instance == null) return;

            List<FireHazard> active = FireManager.Instance.GetActiveFires();
            bool anyStillBurning = false;

            if (active != null)
            {
                foreach (var f in active)
                {
                    if (f != null && f.IsIgnited)
                    {
                        anyStillBurning = true;
                        break;
                    }
                }
            }

            if (!anyStillBurning)
            {
                Debug.Log("[ExtinguisherController] 🎉 ALL ACTIVE FIRES FULLY SUPPRESSED!");
                OnAllActiveFiresExtinguished?.Invoke();
            }
        }
    }
}
