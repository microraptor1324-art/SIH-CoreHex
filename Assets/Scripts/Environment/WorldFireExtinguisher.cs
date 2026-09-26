using System;
using UnityEngine;
using ARMiningSimulator.Extinguisher;
using ARMiningSimulator.Fire;

namespace ARMiningSimulator.Environment
{
    /// <summary>
    /// Interactive physical fire extinguisher stationed in the mine near the Safety Area.
    /// Trainees can approach or tap the extinguisher to take and equip it for suppressing small fires.
    /// </summary>
    public class WorldFireExtinguisher : MonoBehaviour
    {
        public static WorldFireExtinguisher Instance { get; private set; }

        [Header("Interaction Settings")]
        [SerializeField] private float _interactionRadius = 2.5f;
        [SerializeField] private GameObject _canisterVisual;
        [SerializeField] private GameObject _interactionBadge;

        private bool _isTaken = false;
        private bool _isPlayerInRange = false;
        private Camera _traineeCamera;

        public static event Action OnExtinguisherTaken;

        public bool IsTaken => _isTaken;
        public bool IsPlayerInRange => _isPlayerInRange;
        public float InteractionRadius => _interactionRadius;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (_traineeCamera == null)
                _traineeCamera = Camera.main;
        }

        public void SetupVisuals(GameObject canister, GameObject badge)
        {
            _canisterVisual = canister;
            _interactionBadge = badge;
        }

        private void Update()
        {
            if (_isTaken) return;

            if (_traineeCamera == null)
            {
                _traineeCamera = Camera.main;
                if (_traineeCamera == null) return;
            }

            float dist = Vector3.Distance(transform.position, _traineeCamera.transform.position);
            _isPlayerInRange = (dist <= _interactionRadius);

            // Orient 3D badge towards trainee camera
            if (_interactionBadge != null)
            {
                Vector3 lookDir = _interactionBadge.transform.position - _traineeCamera.transform.position;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    _interactionBadge.transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }

            // Also support tap / click raycast on the world extinguisher
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = _traineeCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 6.0f))
                {
                    if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
                    {
                        TakeExtinguisher();
                    }
                }
            }
        }

        /// <summary>
        /// Equips the extinguisher on the trainee and marks this world station as taken.
        /// </summary>
        public bool TakeExtinguisher()
        {
            if (_isTaken) return false;

            _isTaken = true;
            _isPlayerInRange = false;

            // Hide the physical canister model on the station bracket
            if (_canisterVisual != null)
            {
                _canisterVisual.SetActive(false);
            }

            if (_interactionBadge != null)
            {
                var tm = _interactionBadge.GetComponent<TextMesh>();
                if (tm != null) tm.text = "🧯 EXTINGUISHER EQUIPPED\nAPPROACH FIRE & SPRAY FLAMES";
            }

            // Determine best extinguisher type based on burning equipment
            ExtinguisherType agentType = ExtinguisherType.DryPowder;
            if (FireManager.Instance != null)
            {
                var fires = FireManager.Instance.GetActiveFires();
                if (fires != null && fires.Count > 0)
                {
                    var firstFire = fires[0];
                    if (firstFire != null && firstFire.TargetEquipment != null)
                    {
                        if (firstFire.TargetEquipment.GetComponentInParent<ElectricalEquipment>() != null)
                        {
                            agentType = ExtinguisherType.CO2;
                        }
                    }
                }
            }

            if (ExtinguisherController.Instance != null)
            {
                ExtinguisherController.Instance.EquipExtinguisher(agentType);
            }

            Debug.Log($"[WorldFireExtinguisher] 🧯 Trainee took fire extinguisher from Safety Area station! Equipped agent: {agentType}");
            OnExtinguisherTaken?.Invoke();
            return true;
        }

        public void ResetStation()
        {
            _isTaken = false;
            _isPlayerInRange = false;

            if (_canisterVisual != null)
            {
                _canisterVisual.SetActive(true);
            }

            if (_interactionBadge != null)
            {
                var tm = _interactionBadge.GetComponent<TextMesh>();
                if (tm != null) tm.text = "🧯 FIRE EXTINGUISHER\n[ TAP OR APPROACH TO TAKE ]";
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
