using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Investigation;
using ARMiningSimulator.UI;

namespace ARMiningSimulator.Integration
{
    /// <summary>
    /// Bridges the SURAKSHA-AR HTML dashboard and this existing Unity AR simulation via an
    /// Android custom-URI deep link (surakshaar://launch?scenario=underground-fire&amp;workerId=...).
    ///
    /// This does NOT create a second fire simulation and does NOT alter any existing gameplay,
    /// scoring, AR, fire, decision, or audio system. It only:
    /// 1. Receives and validates the requested scenario ID on app launch/resume.
    /// 2. If the simulation isn't already sitting fresh at its initial stage, reloads the existing
    ///    single scene so a deep-link launch always begins a clean run of the existing Underground
    ///    Fire training (the project has exactly one scene and one scenario — there is nothing to
    ///    "route" to, the whole app already IS the Underground Fire simulation).
    /// 3. Listens for the existing InvestigationManager.OnScorecardReady event (the real, final
    ///    result) and reports it back toward the dashboard using only values that already exist
    ///    in the existing scoring system — nothing here is invented or randomized.
    ///
    /// Self-bootstraps at scene load (same pattern as FireOriginTapDetector) so no manual scene
    /// setup is required.
    /// </summary>
    public class DashboardBridge : MonoBehaviour
    {
        public static DashboardBridge Instance { get; private set; }

        private const string Scheme = "surakshaar";
        private const string SupportedScenario = "underground-fire";

        [Header("Result Return (optional)")]
        [Tooltip("If set, Unity attempts to return to the dashboard via this URL once the scorecard " +
                 "is ready, appending the real result as a ?result=<json> query parameter — typically " +
                 "a file:// path to the dashboard HTML on the device. Leave empty to skip this and " +
                 "just log [SURAKSHA-RESULT] (the result is still fully computed either way).")]
        [SerializeField] private string _dashboardReturnUrl = "";

        [Header("Fallback UI")]
        [Tooltip("How long the 'unknown scenario' fallback message stays on screen, in seconds.")]
        [SerializeField] private float _fallbackMessageDuration = 4f;

        private string _pendingWorkerId = "";
        private string _fallbackMessage = "";
        private float _fallbackMessageUntil = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<DashboardBridge>() != null) return;
            var go = new GameObject("DashboardBridge");
            go.AddComponent<DashboardBridge>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            Application.deepLinkActivated += HandleDeepLink;
            InvestigationManager.OnScorecardReady += HandleScorecardReady;

            // Cold start: the app was launched fresh by the deep link, so the URI is already
            // available here instead of arriving later via the deepLinkActivated event.
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                HandleDeepLink(Application.absoluteURL);
            }
        }

        private void OnDisable()
        {
            Application.deepLinkActivated -= HandleDeepLink;
            InvestigationManager.OnScorecardReady -= HandleScorecardReady;
        }

        private void HandleDeepLink(string uri)
        {
            Debug.Log($"[SURAKSHA-DEEPLINK] Received URI: {uri}");

            Uri parsed;
            try
            {
                parsed = new Uri(uri);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SURAKSHA-DEEPLINK] Could not parse URI '{uri}': {e.Message}");
                return;
            }

            if (!string.Equals(parsed.Scheme, Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return; // Not one of ours — ignore silently, some other app/link triggered this.
            }

            var query = ParseQuery(parsed.Query);
            query.TryGetValue("scenario", out string scenario);
            query.TryGetValue("workerId", out string workerId);

            if (string.IsNullOrEmpty(scenario) || !string.Equals(scenario, SupportedScenario, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[SURAKSHA-LAUNCH] Unknown scenario: {(string.IsNullOrEmpty(scenario) ? "(none)" : scenario)}");
                _fallbackMessage = $"Unknown scenario requested: {(string.IsNullOrEmpty(scenario) ? "(none)" : scenario)}\nOnly 'underground-fire' is available.";
                _fallbackMessageUntil = Time.time + _fallbackMessageDuration;
                return;
            }

            _pendingWorkerId = workerId ?? "";

            Debug.Log($"[SURAKSHA-LAUNCH] Requested scenario: {scenario}");

            // This launch already came from the external web dashboard picking a scenario, so
            // skip the in-app DashboardScreen (AR Training -> Underground Fire -> Launch) and go
            // straight into the simulation.
            DashboardScreen.Dismiss();

            bool needsReload = ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage != ARMiningSimulator.Core.SimulationStage.ScanningRoom;

            if (needsReload)
            {
                Debug.Log("[SURAKSHA-LAUNCH] Simulation already in progress — restarting for a clean run.");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                Debug.Log("[SURAKSHA-LAUNCH] Starting existing Underground Fire scenario");
            }
        }

        private void HandleScorecardReady(ScoreBreakdown breakdown)
        {
            if (breakdown == null) return;

            // Unity's real score scale is up to 1400 (or 1300 without a machine-ID bonus) — the
            // dashboard expects a 0-100 percentage, so convert rather than sending the raw total.
            int maxPossible = breakdown.machineOriginScore > 0 ? 1400 : 1300;
            int percentScore = maxPossible > 0 ? Mathf.RoundToInt(100f * breakdown.totalScore / maxPossible) : 0;
            percentScore = Mathf.Clamp(percentScore, 0, 100);

            int mistakes = CountDecisionMistakes();

            Debug.Log($"[SURAKSHA-RESULT] Score: {percentScore}% (raw {breakdown.totalScore}/{maxPossible}) | " +
                      $"Detection time: {breakdown.detectionTimeSeconds:F1}s | Decision mistakes: {mistakes}");

            if (string.IsNullOrEmpty(_dashboardReturnUrl))
            {
                Debug.Log("[SURAKSHA-RESULT] No dashboard return URL configured — result logged only.");
                return;
            }

            string json = "{" +
                "\"module\":\"Underground Fire\"," +
                $"\"workerId\":\"{Escape(_pendingWorkerId)}\"," +
                $"\"score\":{percentScore}," +
                $"\"responseTime\":{breakdown.detectionTimeSeconds.ToString(CultureInfo.InvariantCulture)}," +
                $"\"mistakes\":{mistakes}" +
                "}";

            string separator = _dashboardReturnUrl.Contains("?") ? "&" : "?";
            string url = _dashboardReturnUrl + separator + "result=" + Uri.EscapeDataString(json);

            Debug.Log($"[SURAKSHA-RESULT] Returning to dashboard: {url}");
            Application.OpenURL(url);
        }

        /// <summary>
        /// Real, existing data only: counts wrong Decision 1/2 attempts from DecisionManager's own
        /// attempt history. FireResponseManager does not keep an equivalent attempt log, so this
        /// mistake count only covers Decision 1 & 2 — documented as a known limitation.
        /// </summary>
        private int CountDecisionMistakes()
        {
            if (DecisionManager.Instance == null) return 0;
            int count = 0;
            foreach (var record in DecisionManager.Instance.History)
            {
                if (record != null && !record.wasCorrect) count++;
            }
            return count;
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query)) return result;
            if (query.StartsWith("?")) query = query.Substring(1);

            foreach (var pair in query.Split('&'))
            {
                if (string.IsNullOrEmpty(pair)) continue;
                var kv = pair.Split(new[] { '=' }, 2);
                string key = Uri.UnescapeDataString(kv[0]);
                string value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                result[key] = value;
            }
            return result;
        }

        private static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private void OnGUI()
        {
            if (_fallbackMessageUntil < 0f || Time.time > _fallbackMessageUntil) return;

            float w = Mathf.Min(520f, Screen.width - 40f);
            float h = 90f;
            Rect box = new Rect((Screen.width - w) * 0.5f, 30f, w, h);

            GUI.Box(box, GUIContent.none);
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(box.x + 10, box.y + 8, box.width - 20, box.height - 16), _fallbackMessage, style);
        }
    }
}
