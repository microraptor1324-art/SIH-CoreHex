# SURAKSHA-AR ↔ Unity Integration

How the SURAKSHA-AR HTML dashboard launches the existing Unity AR mining fire-safety
simulation, and how the real result gets back to the dashboard.

## Correcting two assumptions from the original integration brief

Before implementing, the project was inspected and two things differ from what was assumed:

1. **The dashboard is not at `sih-corehex/Dashboard/index.html`.** The actual file is
   `Assets/dashboard/index (7).html` inside the Unity project. It was left at that path — moving
   it wasn't necessary for the integration to work, and the brief said not to move it without a
   reason.
2. **Only one scenario has a real Unity implementation: Underground Fire.** The dashboard's other
   four scenario cards (Toxic Gas Leak, Machine Isolation, Confined Space, PPE Inspection) are UI
   placeholders with no corresponding Unity scene or logic. This project *is* the Underground Fire
   simulation — there's no scenario router to build, because there's exactly one scenario and one
   scene (`ARPhase10_FinalSimulationScene.unity`, the sole scene in Build Settings).

## Architecture

```
Dashboard (Assets/dashboard/index (7).html)
  → click "Underground Fire" → "LAUNCH AR SIMULATION"
  → window.location.href = "surakshaar://launch?scenario=underground-fire&workerId=..."
  → Android resolves the custom-scheme intent to the existing Unity app
      (Assets/Plugins/Android/AndroidManifest.xml — new intent-filter on the existing
      UnityPlayerActivity, launchMode="singleTask" was already set)
  → Unity: DashboardBridge.cs (Assets/Scripts/Integration/DashboardBridge.cs)
      - reads the URI via Application.absoluteURL (cold start) or
        Application.deepLinkActivated (app already running)
      - validates scenario == "underground-fire"
      - if the sim isn't already fresh at ScanningRoom, reloads the single scene for a clean run
      - the existing flow (room scan → mine gen → decisions → fire response → investigation →
        scorecard) plays out completely unmodified
  → InvestigationManager.OnScorecardReady fires (already existed, unchanged)
  → DashboardBridge converts the real ScoreBreakdown to the dashboard's result shape and,
    if a return URL is configured, calls Application.OpenURL() back to the dashboard with the
    result as a query parameter
  → Dashboard reads ?result=<json> on load, calls the existing
    window.SURAKSHA_AR.reportResult() / handleSimulationResult() — unchanged — and clears the
    URL so a page refresh doesn't reprocess it
```

Nothing about the existing AR camera, plane detection, room measurement, mine generation, fire
system, decisions, scoring, audio, or UI was rewritten. `DashboardBridge` only reads existing
public state (`SimulationGameLoop.CurrentStage`, `InvestigationManager.OnScorecardReady`,
`DecisionManager.History`) and triggers the existing scene reload path.

## Deep-link format

```
surakshaar://launch?scenario=underground-fire&workerId=<id>
```

- `scenario` (required) — must be exactly `underground-fire`. Anything else logs
  `[SURAKSHA-LAUNCH] Unknown scenario: ...` and shows a short on-screen message in Unity instead
  of crashing or silently doing nothing.
- `workerId` (optional) — passed through as-is and echoed back in the result payload. Unity has
  no worker/identity system of its own; it only relays whatever the dashboard sent.

## Result payload

Sent back via `?result=<url-encoded JSON>` on the configured return URL:

```json
{
  "module": "Underground Fire",
  "workerId": "M-1042",
  "score": 78,
  "responseTime": 3.4,
  "mistakes": 1
}
```

| Field | Source | Notes |
|---|---|---|
| `module` | hardcoded `"Underground Fire"` | the only connected scenario |
| `workerId` | echoed from the launch request | no Unity-side identity system exists |
| `score` | `ScoreBreakdown.totalScore` converted to 0–100% | Unity's real max is 1400 (1300 without a machine-ID bonus); the dashboard's `handleSimulationResult` clamps to 0–100, so the raw total is converted before sending |
| `responseTime` | `ScoreBreakdown.detectionTimeSeconds` | real fire-detection reaction time |
| `mistakes` | count of wrong entries in `DecisionManager.Instance.History` | **only covers Decision 1 & 2.** `FireResponseManager` (fire-size eval, tactical action, extinguisher pick) doesn't keep an attempt history, so its wrong attempts aren't counted — see Known Limitations |

`sequenceScore` was intentionally left out of the payload — no such metric exists anywhere in
Unity, and the dashboard's own `handleSimulationResult` already falls back to `score` when it's
absent, so this doesn't need to be invented.

## Files changed / created

**Created**
- `Assets/Scripts/Integration/DashboardBridge.cs` — the deep-link receiver and result reporter
- `SURAKSHA_AR_INTEGRATION.md` — this file

**Modified**
- `Assets/Plugins/Android/AndroidManifest.xml` — added one `intent-filter` (VIEW / DEFAULT /
  BROWSABLE, scheme `surakshaar`, host `launch`) to the existing `UnityPlayerActivity`. No new
  Activity, no other changes.
- `Assets/dashboard/index (7).html` — module → scenario-ID map (`Underground Fire` →
  `underground-fire`), a real `launchUnityScenario()` that attempts the deep link with a
  desktop-safe fallback, an incoming-result consumer that feeds `?result=` into the existing
  `handleSimulationResult`, and updated copy in two integration-note boxes that were describing
  the old "not yet connected" state. `window.SURAKSHA_AR`, `reportResult`, localStorage
  persistence, branding, layout, Worker/Admin modes, and every other existing feature are
  untouched.

## Desktop behavior

Opening the dashboard on a Windows laptop still works exactly as before. Clicking "Launch AR
Simulation" attempts the deep link, and if the page doesn't lose focus within 1.5s (nothing
handled the custom scheme — no Android app to catch it), a toast says *"Open this dashboard on
the Android device to launch the AR simulation."* Nothing is faked; no simulated result is ever
generated on desktop.

## How to test

**TEST 1–3 (existing simulation, unaffected):** Open Unity, press Play on
`ARPhase10_FinalSimulationScene`. Everything should behave exactly as before this change —
`DashboardBridge` only acts on an actual `surakshaar://` URI; it's inert otherwise.

**TEST 4–6 (dashboard, desktop):** Open `Assets/dashboard/index (7).html` in a browser. Go to AR
Training → click Underground Fire → Launch AR Simulation. You should see the "open on Android"
toast within ~1.5s (confirms the deep-link attempt fires and the desktop fallback works).

**TEST 7–14 (Android, requires a phone):** see below — this is the part that genuinely cannot be
verified without a physical device.

### Phone test steps

1. Build & Run the Unity project to your Android phone (package `com.miningsafety.arfiretrainer`,
   unchanged).
2. Copy `Assets/dashboard/index (7).html` onto the phone (e.g. `Downloads/`) and open it in the
   phone's browser.
3. AR Training → Underground Fire → Launch AR Simulation.
4. Android should prompt to open it with the AR app (or open directly if it's the only handler).
5. Confirm in `adb logcat` (or Unity's on-device log if you have one) that you see:
   - `[SURAKSHA-DEEPLINK] Received URI: surakshaar://launch?scenario=underground-fire&workerId=M-1042`
   - `[SURAKSHA-LAUNCH] Requested scenario: underground-fire`
   - `[SURAKSHA-LAUNCH] Starting existing Underground Fire scenario`
6. Play through the existing simulation normally (room scan → decisions → fire response →
   investigation → scorecard).
7. Confirm `[SURAKSHA-RESULT] Score: ...` appears in the log once the scorecard is shown.

### The one piece that needs your manual configuration

By default `DashboardBridge._dashboardReturnUrl` is **empty**, so step 7 above logs the result but
doesn't try to send it anywhere — this is a safe no-op, not a bug. To actually get the result back
onto the dashboard page automatically, set that Inspector field (on the auto-created
`DashboardBridge` GameObject, or add the field's value via script before build) to the exact
on-device path of the dashboard file, e.g.:

```
file:///storage/emulated/0/Download/index (7).html
```

Then `Application.OpenURL` will reopen that exact page with `?result=...` appended, and the page's
own JS (already wired) will pick it up. **This exact path depends on where you put the file on
your phone — test it once manually and adjust the field to match.**

## Known limitations

- **Mistake count only covers Decision 1 & 2.** `FireResponseManager` doesn't keep a per-attempt
  history the way `DecisionManager` does, so wrong fire-size/tactical-action/extinguisher
  attempts aren't reflected in `mistakes`. Adding that would mean adding a history list to
  `FireResponseManager` — a small, safe addition, but out of scope for this integration pass
  since it touches scoring code that was just fixed for a separate bug.
- **The Unity → dashboard return trip relies on `file://` navigation**, which only works
  reliably if the dashboard HTML sits at a stable, known path on the device (see above). There is
  no way to have a plain local HTML page register as a handler for the *return* leg the way an
  installed app can — this is a genuine Android/browser platform constraint, not an oversight.
  If this turns out to be unreliable on your phone, the safe fallback is: Unity still logs the
  full result to `[SURAKSHA-RESULT]`, so nothing is lost — you'd just need to check the log or
  paste the value into the dashboard by hand for the demo.
- **Only Underground Fire is connected.** The other four scenario cards are left as dashboard-only
  placeholders; selecting them shows "not yet connected" rather than attempting a deep link Unity
  doesn't understand.

## Adding a future scenario

1. Build the scenario in Unity (its own manager/flow, following the existing pattern).
2. Add a case for it in `DashboardBridge.HandleDeepLink` (currently only checks for
   `"underground-fire"`).
3. Add it to the dashboard's `SCENARIO_MAP` in `Assets/dashboard/index (7).html`.

No Android manifest changes are needed for additional scenarios — the same `surakshaar://launch`
intent-filter handles any `scenario=` value; only the query parameter changes.
