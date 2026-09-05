using System;
using System.Collections.Generic;
using UnityEngine;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.Fire
{
    /// <summary>
    /// Central manager for random fire generation, fire queries, and scenario progression.
    /// Selects 1-2 mining machines and 1-2 electrical objects to catch fire.
    /// </summary>
    [RequireComponent(typeof(FireSpreadSystem))]
    public class FireManager : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private EquipmentSpawner _equipmentSpawner;
        [SerializeField] private FireSpreadSystem _spreadSystem;

        [Header("Fire Generation Settings")]
        [SerializeField] private bool _autoStartOnEnvironmentReady = true;
        [SerializeField] private float _delayBeforeIgnition = 1.5f;

        // Active Fires
        private readonly List<FireHazard> _activeFires = new List<FireHazard>();
        private bool _isScenarioActive = false;
        private float _scenarioTimer = 0f;

        // Events
        public event Action<List<FireHazard>> OnFiresStarted;
        public event Action<FireSeverity> OnHighestSeverityChanged;
        public event Action OnAllFiresExtinguished;
        public static event Action<FireHazard> OnFireIgnited;

        public static FireManager Instance { get; private set; }

        public IReadOnlyList<FireHazard> ActiveFires => _activeFires;
        public List<FireHazard> GetActiveFires() => new List<FireHazard>(_activeFires);
        public bool HasActiveFires => _activeFires.Count > 0;
        public float ScenarioTimer => _scenarioTimer;
        public FireSpreadSystem SpreadSystem => _spreadSystem;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_spreadSystem == null)
                _spreadSystem = GetComponent<FireSpreadSystem>();
            if (_equipmentSpawner == null)
                _equipmentSpawner = FindFirstObjectByType<EquipmentSpawner>();
        }

        public void ExtinguishAll() => ExtinguishAllFires();
        public void IgniteRandomFires() => StartScenarioFires();

        private void Start()
        {
            if (_autoStartOnEnvironmentReady)
            {
                Invoke(nameof(StartScenarioFires), _delayBeforeIgnition);
            }
        }

        private void Update()
        {
            if (!_isScenarioActive) return;

            _scenarioTimer += Time.deltaTime;

            if (_spreadSystem != null && _activeFires.Count > 0)
            {
                _spreadSystem.UpdateFires(_activeFires);
            }

            CheckExtinguishCompletion();
        }

        /// <summary>
        /// Randomly selects 1-2 mining machines and 1-2 electrical equipment to ignite.
        /// </summary>
        public void StartScenarioFires()
        {
            ClearAllFires();

            if (_equipmentSpawner == null)
                _equipmentSpawner = FindFirstObjectByType<EquipmentSpawner>();

            if (_equipmentSpawner == null)
            {
                Debug.LogWarning("[FireManager] Cannot start fires: EquipmentSpawner not found.");
                return;
            }

            var machines = new List<MiningMachine>(_equipmentSpawner.SpawnedMachines);
            var electrical = new List<ElectricalEquipment>(_equipmentSpawner.SpawnedElectrical);

            if (machines.Count == 0 && electrical.Count == 0)
            {
                Debug.LogWarning("[FireManager] No equipment spawned yet. Retrying in 1.5 seconds...");
                Invoke(nameof(StartScenarioFires), 1.5f);
                return;
            }

            // Shuffle machines
            ShuffleList(machines);
            ShuffleList(electrical);

            // Random selection: 1 or 2 mining machines, 1 or 2 electrical equipment
            int machineFireCount = Mathf.Min(UnityEngine.Random.Range(1, 3), machines.Count);
            int electricalFireCount = Mathf.Min(UnityEngine.Random.Range(1, 3), electrical.Count);

            // Ignite selected machines
            for (int i = 0; i < machineFireCount; i++)
            {
                IgniteObject(machines[i].gameObject);
                machines[i].IsOnFire = true;
            }

            // Ignite selected electrical equipment
            for (int i = 0; i < electricalFireCount; i++)
            {
                IgniteObject(electrical[i].gameObject);
                electrical[i].IsOnFire = true;
            }

            _isScenarioActive = true;
            _scenarioTimer = 0f;

            Debug.Log($"[FireManager] Scenario fires ignited! Selected {machineFireCount} machines and {electricalFireCount} electrical items. Total fires: {_activeFires.Count}");
            OnFiresStarted?.Invoke(_activeFires);
        }

        private void IgniteObject(GameObject target)
        {
            if (target == null) return;

            GameObject hazardGo = new GameObject($"FireHazard_{target.name}");
            hazardGo.transform.SetParent(target.transform, false);
            hazardGo.transform.localPosition = Vector3.up * 0.4f; // Place flame above equipment

            FireHazard hazard = hazardGo.AddComponent<FireHazard>();
            hazard.Initialize(target, FireSeverity.Small);

            _activeFires.Add(hazard);
            OnFireIgnited?.Invoke(hazard);
        }

        public FireSeverity GetHighestSeverity()
        {
            FireSeverity highest = FireSeverity.Small;
            foreach (var fire in _activeFires)
            {
                if (fire == null || fire.State != FireState.Burning) continue;
                if (fire.Severity > highest)
                    highest = fire.Severity;
            }
            return highest;
        }

        public FireHazard GetClosestFire(Vector3 position)
        {
            FireHazard closest = null;
            float minDist = float.MaxValue;

            foreach (var fire in _activeFires)
            {
                if (fire == null || fire.State != FireState.Burning) continue;
                float dist = Vector3.Distance(position, fire.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = fire;
                }
            }

            return closest;
        }

        public void SetAllFiresSeverity(FireSeverity severity)
        {
            foreach (var fire in _activeFires)
            {
                if (fire != null && fire.State == FireState.Burning)
                {
                    fire.SetSeverity(severity);
                }
            }
        }

        public void ExtinguishAllFires()
        {
            foreach (var fire in _activeFires)
            {
                if (fire != null)
                {
                    fire.Extinguish(9999f);
                }
            }
            CheckExtinguishCompletion();
        }

        public void ClearAllFires()
        {
            foreach (var fire in _activeFires)
            {
                if (fire != null) Destroy(fire.gameObject);
            }
            _activeFires.Clear();
            _isScenarioActive = false;
            _scenarioTimer = 0f;
        }

        private void CheckExtinguishCompletion()
        {
            if (!_isScenarioActive) return;

            bool allExtinguished = true;
            foreach (var fire in _activeFires)
            {
                if (fire != null && fire.State == FireState.Burning)
                {
                    allExtinguished = false;
                    break;
                }
            }

            if (allExtinguished && _activeFires.Count > 0)
            {
                _isScenarioActive = false;
                Debug.Log("[FireManager] All fires have been EXTINGUISHED!");
                OnAllFiresExtinguished?.Invoke();
            }
        }

        private void ShuffleList<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
