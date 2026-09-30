using UnityEngine;
using UnityEngine.InputSystem;
using ARMiningSimulator.Environment;

namespace ARMiningSimulator.Investigation
{
    /// <summary>
    /// Fire-origin identification input: while InvestigationManager is in the MachineSelection
    /// state, this listens for a tap (touchscreen) or click (mouse, for Editor testing) and
    /// raycasts from the camera into the scene. If the tap hits a machine or electrical unit
    /// (walking up from whatever collider was hit to its MiningMachine/ElectricalEquipment
    /// parent, so tapping any child mesh of a machine still counts), it's reported to
    /// InvestigationManager.SubmitMachineOriginTap for the correct/incorrect check.
    /// Taps that miss every machine are ignored — only real attempts count.
    /// </summary>
    public class FireOriginTapDetector : MonoBehaviour
    {
        [SerializeField] private Camera _traineeCamera;
        [SerializeField] private float _maxRayDistance = 50f;

        /// <summary>
        /// Self-bootstraps a persistent instance at scene load so this works without needing to be
        /// manually placed on a GameObject in every scene — it always finds Camera.main at runtime.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<FireOriginTapDetector>() != null) return;
            var go = new GameObject("FireOriginTapDetector");
            go.AddComponent<FireOriginTapDetector>();
            DontDestroyOnLoad(go);
        }

        private void Update()
        {
            if (InvestigationManager.Instance == null ||
                InvestigationManager.Instance.State != InvestigationState.MachineSelection)
            {
                return;
            }

            Vector2 screenPos;
            if (!TryGetTap(out screenPos)) return;

            Camera cam = _traineeCamera != null ? _traineeCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance)) return;

            GameObject tapped = ResolveMachineRoot(hit.collider.gameObject);
            if (tapped == null) return; // Tap hit something that isn't a machine — not an attempt.

            InvestigationManager.Instance.SubmitMachineOriginTap(tapped);
        }

        private static bool TryGetTap(out Vector2 screenPos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                return true;
            }

            screenPos = default;
            return false;
        }

        /// <summary>Walks up from the tapped collider to the machine/equipment's root GameObject.</summary>
        private static GameObject ResolveMachineRoot(GameObject hitObject)
        {
            var machine = hitObject.GetComponentInParent<MiningMachine>();
            if (machine != null) return machine.gameObject;

            var electrical = hitObject.GetComponentInParent<ElectricalEquipment>();
            if (electrical != null) return electrical.gameObject;

            return null;
        }
    }
}
