using UnityEngine;
using UnityEngine.EventSystems;
using ARMiningSimulator.AR;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Professional Mobile AR Measuring HUD for 4-Flag Rectangular Room Surveying.
    /// Features:
    /// - Center-screen precision reticle with dynamic state colors (Green / Amber / Red / Orange).
    /// - Status badge: "FLOOR DETECTED", "POINT AT FLOOR", "HOLD STEADY...", "READY".
    /// - Strict 4-flag action buttons: [ ADD FLAG (1/4) ] -> (2/4) -> (3/4) -> (4/4) -> [ CLOSE ROOM ].
    /// - Disables ADD FLAG once 4 flags are placed.
    /// - Rectangular room analysis summary: "RECTANGULAR ROOM DETECTED", Width, Length, Area.
    /// - [ UNDO LAST FLAG ] and [ RESET ] controls.
    /// - Toggleable Development/Debug Panel showing tracking, raycast, surface, stability, and coordinates.
    /// </summary>
    public class RoomMeasurementUI : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private ARRoomMeasurement _measurement;
        [SerializeField] private ARRoomScanner _scanner;

        [Header("Optional uGUI References (if bound)")]
        [SerializeField] private UnityEngine.UI.Text _lengthText;
        [SerializeField] private UnityEngine.UI.Text _widthText;
        [SerializeField] private UnityEngine.UI.Text _areaText;
        [SerializeField] private UnityEngine.UI.Text _guidanceText;
        [SerializeField] private UnityEngine.UI.Button _addFlagButton;
        [SerializeField] private UnityEngine.UI.Button _closeRoomButton;
        [SerializeField] private UnityEngine.UI.Button _undoButton;
        [SerializeField] private UnityEngine.UI.Button _resetButton;

        // Debug Panel Toggle (Requirement 23)
        private bool _showDebugPanel = true;

        // Input debouncing & feedback
        private float _lastTapTime = 0f;
        private float _invalidTapNoticeTime = -10f;

        private void Awake()
        {
            if (_measurement == null)
                _measurement = FindAnyObjectByType<ARRoomMeasurement>();
            if (_scanner == null)
                _scanner = FindAnyObjectByType<ARRoomScanner>();
        }

        private void Start()
        {
            if (_addFlagButton != null)
                _addFlagButton.onClick.AddListener(OnAddFlagClicked);
            if (_closeRoomButton != null)
                _closeRoomButton.onClick.AddListener(OnCloseRoomClicked);
            if (_undoButton != null)
                _undoButton.onClick.AddListener(OnUndoClicked);
            if (_resetButton != null)
                _resetButton.onClick.AddListener(OnResetClicked);
        }

        private void Update()
        {
            UpdateUGUI();
        }

        private void UpdateUGUI()
        {
            if (_measurement == null) return;

            if (_lengthText != null)
                _lengthText.text = $"Length: {_measurement.Length:F2} m";

            if (_widthText != null)
                _widthText.text = $"Width: {_measurement.Width:F2} m";

            if (_areaText != null)
                _areaText.text = $"Area: {_measurement.Area:F2} m²";

            if (_guidanceText != null)
                _guidanceText.text = _measurement.StatusMessage;
        }

        public void OnAddFlagClicked()
        {
            if (_measurement != null && _measurement.CanAddFlag)
            {
                _measurement.AddFlagAtCrosshair();
            }
        }

        public void OnCloseRoomClicked()
        {
            if (_measurement != null && _measurement.CanCloseRoom)
            {
                _measurement.CloseRoom();
            }
        }

        public void OnUndoClicked()
        {
            if (_measurement != null)
            {
                _measurement.UndoLastFlag();
            }
        }

        public void OnResetClicked()
        {
            if (_measurement != null)
            {
                _measurement.ResetMeasurement();
            }
        }

        public void OnConfirmClicked()
        {
            if (_measurement != null)
            {
                _measurement.ConfirmMeasurement();
            }
        }

        /// <summary>
        /// Responsive OnGUI Mobile Measuring HUD.
        /// </summary>
        private void OnGUI()
        {
            // Only draw OnGUI if uGUI elements are not bound
            if (_lengthText != null && _areaText != null) return;

            // Hide HUD once confirmed or when gameplay simulation starts
            if (_measurement != null && _measurement.IsConfirmed) return;
            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage != ARMiningSimulator.Core.SimulationStage.ScanningRoom)
            {
                return;
            }

            int screenW = Screen.width;
            int screenH = Screen.height;
            int pad = 20;
            int cardW = Mathf.Min(screenW - pad * 2, 720);

            // Screen Tap Handler: Allows user to tap screen to place corner
            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0)
            {
                // Exclude bottom action deck and top status card
                if (e.mousePosition.y > screenH * 0.22f && e.mousePosition.y < screenH * 0.78f)
                {
                    if (_measurement != null && _measurement.FlagCount < 4 && !_measurement.IsRoomClosed)
                    {
                        if (_measurement.CanAddFlag)
                        {
                            // Debounce double-taps (0.35s cooldown)
                            if (Time.unscaledTime - _lastTapTime > 0.35f)
                            {
                                _lastTapTime = Time.unscaledTime;
                                OnAddFlagClicked();
                            }
                        }
                        else
                        {
                            _invalidTapNoticeTime = Time.unscaledTime;
                        }
                    }
                }
            }

            // 1. CENTER SCREEN RETICLE
            DrawCenterReticle(screenW, screenH);

            // 1b. Temporary Invalid Surface Tap Notice
            if (Time.unscaledTime - _invalidTapNoticeTime < 2.2f)
            {
                GUIStyle warningStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(1.0f, 0.35f, 0.35f) }
                };
                GUI.Label(new Rect(screenW * 0.5f - 160, screenH * 0.5f + 65, 320, 36), "Please tap on the detected floor.", warningStyle);
            }

            // 2. TOP STATUS BAR & INSTRUCTION CARD
            DrawTopStatusCard(pad, cardW);

            // 3. BOTTOM ACTION DECK
            DrawBottomActionDeck(pad, screenH, cardW);

            // 4. DEVELOPMENT/DEBUG PANEL (Requirement 23)
            DrawDebugPanel(screenW, screenH);
        }

        private void DrawCenterReticle(int screenW, int screenH)
        {
            float cx = screenW * 0.5f;
            float cy = screenH * 0.5f;
            float reticleSize = 24f;

            bool tracking = _measurement != null && _measurement.IsTrackingStable;
            bool floorHit = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.HasValidFloorHit;
            bool isStable = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.IsPointStable;
            bool hasCalib = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.HasCalibratedFloor;

            Color reticleColor;
            string statusBadge;

            if (!tracking)
            {
                reticleColor = new Color(1.0f, 0.55f, 0.1f, 0.85f); // Orange
                statusBadge = "TRACKING...";
            }
            else if (floorHit)
            {
                if (isStable)
                {
                    reticleColor = new Color(0.15f, 0.95f, 0.45f, 0.95f); // Vivid Green
                    statusBadge = "READY";
                }
                else
                {
                    reticleColor = new Color(0.95f, 0.85f, 0.15f, 0.95f); // Yellow/Amber
                    statusBadge = "HOLD STEADY...";
                }
            }
            else if (hasCalib)
            {
                reticleColor = new Color(0.95f, 0.85f, 0.15f, 0.95f); // Amber
                statusBadge = "POINT AT CORNER";
            }
            else
            {
                reticleColor = new Color(0.95f, 0.25f, 0.25f, 0.75f); // Red
                statusBadge = "POINT AT FLOOR";
            }

            Color oldColor = GUI.color;
            GUI.color = reticleColor;

            // Reticle Crosshairs
            GUI.DrawTexture(new Rect(cx - reticleSize, cy - 2f, reticleSize * 2f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 2f, cy - reticleSize, 4f, reticleSize * 2f), Texture2D.whiteTexture);

            // Central Ring Outline
            float ringRadius = 14f;
            GUI.DrawTexture(new Rect(cx - ringRadius, cy - ringRadius, ringRadius * 2f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - ringRadius, cy + ringRadius - 2f, ringRadius * 2f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - ringRadius, cy - ringRadius, 2f, ringRadius * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + ringRadius - 2f, cy - ringRadius, 2f, ringRadius * 2f), Texture2D.whiteTexture);

            // Distance / Status Badge directly below crosshair
            GUIStyle badgeStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = reticleColor }
            };

            float dist = (_measurement != null && _measurement.RaycastController != null)
                ? _measurement.RaycastController.DistanceFromCamera
                : 0f;

            string badgeText = floorHit ? $"{statusBadge}  ({dist:F2} m)" : statusBadge;
            GUI.Label(new Rect(cx - 110, cy + reticleSize + 14, 220, 32), badgeText, badgeStyle);

            GUI.color = oldColor;
        }

        private void DrawTopStatusCard(int pad, int cardW)
        {
            bool isClosed = _measurement != null && _measurement.IsRoomClosed;
            int topCardH = isClosed ? 230 : 175;
            GUI.Box(new Rect(pad, pad, cardW, topCardH), GUIContent.none);

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 23,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.78f, 0.12f) } // Safety Gold
            };

            GUIStyle statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.92f, 1.0f) }
            };

            GUIStyle guideStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            GUIStyle areaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.2f, 1.0f, 0.5f) } // Green
            };

            GUILayout.BeginArea(new Rect(pad + 14, pad + 10, cardW - 28, topCardH - 18));

            // Row 1: Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("ROOM SCANNER (4 CORNERS)", titleStyle);
            GUILayout.FlexibleSpace();

            bool tracking = _measurement != null && _measurement.IsTrackingStable;
            string trackLabel = tracking ? "Tracking: GOOD" : "Tracking: SCANNING";
            Color trackCol = tracking ? Color.green : Color.yellow;
            GUILayout.Label(trackLabel, new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = trackCol } });
            GUILayout.EndHorizontal();

            // Row 2: Live Info
            string surfaceStr = (_measurement != null && _measurement.RaycastController != null)
                ? _measurement.RaycastController.SurfaceTypeString
                : "NONE";
            int flags = _measurement != null ? _measurement.FlagCount : 0;

            GUILayout.Label($"Surface: {surfaceStr}  |  Corners Measured: {flags} / 4", statusStyle);
            GUILayout.Space(2);

            // Row 3: If closed, show Room Summary
            if (isClosed)
            {
                string statusMsg = _measurement.RectangleAnalysis.StatusMessage;
                Color statusColor = _measurement.IsRectangular ? new Color(0.2f, 1.0f, 0.5f) : new Color(0.95f, 0.85f, 0.2f);
                GUILayout.Label(statusMsg, new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = statusColor } });

                GUILayout.Label($"ROOM AREA: {_measurement.Area:F2} m²", areaStyle);
                GUILayout.Label($"Length: {_measurement.Length:F2} m   |   Width: {_measurement.Width:F2} m", statusStyle);
            }
            else
            {
                string instruction = _measurement != null ? _measurement.StatusMessage : "SCAN THE FLOOR";
                GUILayout.Label(instruction, guideStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawBottomActionDeck(int pad, int screenH, int cardW)
        {
            int btnH = 64;
            int deckH = btnH * 2 + 16;
            int deckY = screenH - deckH - pad;

            GUILayout.BeginArea(new Rect(pad, deckY, cardW, deckH));

            Color origBg = GUI.backgroundColor;
            GUIStyle mainBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };

            bool isClosed = _measurement != null && _measurement.IsRoomClosed;
            int flags = _measurement != null ? _measurement.FlagCount : 0;

            // ==========================================================
            // ROW 1: PRIMARY ACTION BUTTON
            // ==========================================================
            if (isClosed)
            {
                GUI.backgroundColor = new Color(0.18f, 0.95f, 0.42f); // Safety Green
                if (GUILayout.Button($"✅ CONFIRM & START DRILL ({_measurement.Area:F2} m²)", mainBtnStyle, GUILayout.Height(btnH)))
                {
                    OnConfirmClicked();
                }
            }
            else if (flags == 4)
            {
                // EXACTLY 4 FLAGS: Prompt to CLOSE ROOM
                GUI.backgroundColor = new Color(0.1f, 0.6f, 1.0f); // Blue
                if (GUILayout.Button("📐 CLOSE ROOM (50% BLUE SHADE)", mainBtnStyle, GUILayout.Height(btnH)))
                {
                    OnCloseRoomClicked();
                }
            }
            else
            {
                // Adding Flags 1, 2, 3, or 4
                bool canAdd = _measurement != null && _measurement.CanAddFlag;
                string addFlagText = canAdd ? $"🚩 PLACE CORNER {flags + 1} ({flags + 1}/4)" : "🔍 POINT AT FLOOR TO CALIBRATE";
                GUI.backgroundColor = canAdd ? new Color(0.1f, 0.85f, 1.0f) : new Color(0.5f, 0.55f, 0.6f);

                GUI.enabled = canAdd;
                if (GUILayout.Button(addFlagText, mainBtnStyle, GUILayout.Height(btnH)))
                {
                    OnAddFlagClicked();
                }
                GUI.enabled = true;
            }

            GUI.backgroundColor = origBg;
            GUILayout.Space(8);

            // ==========================================================
            // ROW 2: SECONDARY CONTROLS (UNDO, RESET)
            // ==========================================================
            GUILayout.BeginHorizontal();

            GUIStyle subBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };

            // 1. UNDO LAST FLAG
            GUI.enabled = flags > 0;
            if (GUILayout.Button("↩ UNDO LAST FLAG", subBtnStyle, GUILayout.Height(btnH - 12), GUILayout.Width(cardW * 0.48f)))
            {
                OnUndoClicked();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // 2. RESET
            if (GUILayout.Button("🔄 RESET", subBtnStyle, GUILayout.Height(btnH - 12), GUILayout.Width(cardW * 0.48f)))
            {
                OnResetClicked();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Real-time Development/Debug Panel (Requirement 23).
        /// Displays AR Tracking, Floor Calibration, Raycast valid, Surface type, Point stability, Depth status, Hit Position, and Flag count.
        /// </summary>
        private void DrawDebugPanel(int screenW, int screenH)
        {
            // Toggle Button in top-right corner
            int toggleW = 105;
            int toggleH = 34;
            int toggleX = screenW - toggleW - 20;
            int toggleY = 20;

            if (GUI.Button(new Rect(toggleX, toggleY, toggleW, toggleH), _showDebugPanel ? "🐛 HIDE" : "🐛 DEBUG"))
            {
                _showDebugPanel = !_showDebugPanel;
            }

            if (!_showDebugPanel) return;

            int panelW = 290;
            int panelH = 240;
            int panelX = screenW - panelW - 20;
            int panelY = toggleY + toggleH + 6;

            GUI.Box(new Rect(panelX, panelY, panelW, panelH), GUIContent.none);

            GUIStyle dbgHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            GUIStyle dbgLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            GUILayout.BeginArea(new Rect(panelX + 10, panelY + 6, panelW - 20, panelH - 12));

            GUILayout.Label("── AR DEBUG PANEL ──", dbgHeader);

            bool tracking = _measurement != null && _measurement.IsTrackingStable;
            GUILayout.Label($"AR Tracking:  {(tracking ? "GOOD" : "INITIALIZING")}", dbgLabel);

            float calibY = (_measurement != null && _measurement.RaycastController != null) ? _measurement.RaycastController.CalibratedFloorY : 0f;
            bool hasCalib = (_measurement != null && _measurement.RaycastController != null) && _measurement.RaycastController.HasCalibratedFloor;
            GUILayout.Label($"Floor Calib:  {(hasCalib ? $"{calibY:F2} m (LOCKED)" : "SCANNING...")}", dbgLabel);

            bool floorHit = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.HasValidFloorHit;
            GUILayout.Label($"Raycast:      {(floorHit ? "VALID (FLOOR)" : "INVALID")}", dbgLabel);

            string surfaceStr = (_measurement != null && _measurement.RaycastController != null)
                ? _measurement.RaycastController.SurfaceTypeString
                : "NONE";
            GUILayout.Label($"Surface:      {surfaceStr}", dbgLabel);

            bool stable = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.IsPointStable;
            GUILayout.Label($"Point Stable: {(stable ? "YES (READY)" : "NO (HOLD STEADY)")}", dbgLabel);

            bool depth = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.DepthAvailable;
            GUILayout.Label($"Depth:        {(depth ? "AVAILABLE" : "UNAVAILABLE")}", dbgLabel);

            Vector3 hitPos = (_measurement != null && _measurement.RaycastController != null)
                ? _measurement.RaycastController.StabilizedPosition
                : Vector3.zero;
            GUILayout.Label($"Hit Pos: ({hitPos.x:F2}, {hitPos.y:F2}, {hitPos.z:F2})", dbgLabel);

            int flagCount = _measurement != null ? _measurement.FlagCount : 0;
            GUILayout.Label($"Current Flag: {flagCount} / 4", dbgLabel);

            GUILayout.EndArea();
        }
    }
}
