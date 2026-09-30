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

        // Debug Panel Toggle (Requirement 23) - Clean default (hidden)
        private bool _showDebugPanel = false;

        // Cached styles for clean, modern aesthetic
        private GUIStyle _topTitleStyle;
        private GUIStyle _instructionStyle;
        private GUIStyle _subMetaStyle;
        private GUIStyle _areaResultStyle;
        private GUIStyle _mainBtnStyle;
        private GUIStyle _subBtnStyle;
        private GUIStyle _confirmedActiveStyle;
        private GUIStyle _pendingStyle;
        private GUIStyle _overlayBtnStyle;
        private GUIStyle _dbgToggleBtnStyle;
        private bool _stylesInitialized = false;

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

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _topTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(1.0f, 0.82f, 0.20f) } // Safety Gold
            };

            _instructionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            _subMetaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.78f, 0.84f, 0.92f) }
            };

            _areaResultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.22f, 1.0f, 0.52f) } // Vivid Emerald
            };

            _mainBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _subBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _confirmedActiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.22f, 1.0f, 0.52f) }
            };

            _pendingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.98f, 0.82f, 0.20f) }
            };

            _overlayBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _dbgToggleBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.85f, 0.9f) }
            };

            _stylesInitialized = true;
        }

        /// <summary>Rounded, soft-shadowed card panel — routes through the shared tactical theme.</summary>
        private void DrawPanelBox(Rect r)
        {
            TacticalUITheme.DrawFortniteCard(r, TacticalUITheme.FortniteBlue, TacticalUITheme.FortniteNavyDark);
        }

        /// <summary>Rounded capsule status pill, e.g. the reticle status readout.</summary>
        private void DrawPillBadge(Rect r, Color textColor, string text)
        {
            TacticalUITheme.DrawRoundedRect(new Rect(r.x, r.y + 2f, r.width, r.height), new Color(0f, 0f, 0f, 0.20f), true);
            TacticalUITheme.DrawRoundedRect(r, TacticalUITheme.FortniteNavyDark, true);

            GUIStyle badgeLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor }
            };
            GUI.Label(r, text, badgeLabel);
        }

        /// <summary>
        /// Clean, responsive, and beautifully aligned Mobile AR Measuring HUD.
        /// </summary>
        private void OnGUI()
        {
            // Only draw OnGUI if uGUI elements are not bound
            if (_lengthText != null && _areaText != null) return;

            InitStyles();

            TacticalUITheme.BeginScaledGUI();
            try
            {
                DrawOnGUIContent();
            }
            finally
            {
                TacticalUITheme.EndScaledGUI();
            }
        }

        private void DrawOnGUIContent()
        {
            if (DashboardScreen.IsShowing) return;

            int screenW = Mathf.RoundToInt(TacticalUITheme.VW);
            int screenH = Mathf.RoundToInt(TacticalUITheme.VH);

            // When measurement is confirmed and mine environment is active, show clean minimal re-scan bar
            if (_measurement != null && _measurement.IsConfirmed)
            {
                DrawConfirmedOverlay(screenW, screenH);
                return;
            }

            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage != ARMiningSimulator.Core.SimulationStage.ScanningRoom)
            {
                return;
            }

            // Screen Tap Handler: Allows user to tap screen to place corner
            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0)
            {
                // Exclude bottom action deck and top status card
                if (e.mousePosition.y > 130f && e.mousePosition.y < screenH - 150f)
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
                float noticeW = 320f;
                float noticeH = 34f;
                Rect noticeRect = new Rect((screenW - noticeW) * 0.5f, screenH * 0.5f + 65f, noticeW, noticeH);
                DrawPillBadge(noticeRect, new Color(1.0f, 0.40f, 0.40f), "Please point at detected floor first");
            }

            // 2. TOP STATUS BAR & INSTRUCTION CARD (Centered & Sleek)
            DrawTopStatusCard(screenW);

            // 3. BOTTOM ACTION DECK (Place Flag, Undo, Reset in Center Bottom)
            DrawBottomActionDeck(screenW, screenH);

            // 4. DEVELOPMENT/DEBUG PANEL (Hidden by default, subtle toggle in top right)
            DrawDebugPanel(screenW, screenH);
        }

        private void DrawCenterReticle(int screenW, int screenH)
        {
            float cx = screenW * 0.5f;
            float cy = screenH * 0.5f;
            float reticleSize = 22f;

            bool tracking = _measurement != null && _measurement.IsTrackingStable;
            bool floorHit = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.HasValidFloorHit;
            bool isStable = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.IsPointStable;
            bool hasCalib = _measurement != null && _measurement.RaycastController != null && _measurement.RaycastController.HasCalibratedFloor;

            Color reticleColor;
            string statusBadge;

            if (!tracking)
            {
                reticleColor = new Color(1.0f, 0.55f, 0.1f, 0.90f); // Orange
                statusBadge = "INITIALIZING...";
            }
            else if (floorHit)
            {
                if (isStable)
                {
                    reticleColor = new Color(0.15f, 0.95f, 0.50f, 0.95f); // Vivid Emerald Green
                    statusBadge = "FLOOR READY";
                }
                else
                {
                    reticleColor = new Color(0.98f, 0.82f, 0.15f, 0.95f); // Amber
                    statusBadge = "HOLD STEADY...";
                }
            }
            else if (hasCalib)
            {
                reticleColor = new Color(0.95f, 0.80f, 0.20f, 0.90f);
                statusBadge = "AIM AT CORNER";
            }
            else
            {
                reticleColor = new Color(0.95f, 0.30f, 0.30f, 0.80f); // Soft Red
                statusBadge = "POINT AT FLOOR";
            }

            Color oldColor = GUI.color;
            GUI.color = reticleColor;

            // Reticle Crosshairs (thin high-precision lines)
            GUI.DrawTexture(new Rect(cx - reticleSize, cy - 1.5f, reticleSize * 2f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - 1.5f, cy - reticleSize, 3f, reticleSize * 2f), Texture2D.whiteTexture);

            // Central Ring Outline
            float ringRadius = 12f;
            GUI.DrawTexture(new Rect(cx - ringRadius, cy - ringRadius, ringRadius * 2f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - ringRadius, cy + ringRadius - 2f, ringRadius * 2f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - ringRadius, cy - ringRadius, 2f, ringRadius * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + ringRadius - 2f, cy - ringRadius, 2f, ringRadius * 2f), Texture2D.whiteTexture);

            GUI.color = oldColor;

            // Badge directly below crosshair with dark frosted backdrop
            float dist = (_measurement != null && _measurement.RaycastController != null)
                ? _measurement.RaycastController.DistanceFromCamera
                : 0f;

            string badgeText = floorHit ? $"{statusBadge}  •  {dist:F2} m" : statusBadge;
            float badgeW = 210f;
            float badgeH = 28f;
            Rect badgeRect = new Rect(cx - (badgeW * 0.5f), cy + reticleSize + 12f, badgeW, badgeH);

            DrawPillBadge(badgeRect, reticleColor, badgeText);
        }

        private void DrawTopStatusCard(int screenW)
        {
            bool isClosed = _measurement != null && _measurement.IsRoomClosed;
            float cardW = Mathf.Min(screenW - 32f, 520f);
            float cardX = (screenW - cardW) * 0.5f;
            float cardH = isClosed ? 120f : 100f;
            float cardY = 20f;

            DrawPanelBox(new Rect(cardX, cardY, cardW, cardH));

            GUILayout.BeginArea(new Rect(cardX + 16, cardY + 10, cardW - 32, cardH - 18));

            // Row 1: Title + Live Status Pill
            GUILayout.BeginHorizontal();
            GUILayout.Label("AR ROOM SURVEY", _topTitleStyle);
            GUILayout.FlexibleSpace();

            bool tracking = _measurement != null && _measurement.IsTrackingStable;
            string trackLabel = tracking ? "● TRACKING ACTIVE" : "◌ SCANNING FLOOR";
            Color trackCol = tracking ? new Color(0.20f, 0.95f, 0.50f) : new Color(0.98f, 0.78f, 0.15f);
            GUILayout.Label(trackLabel, new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, normal = { textColor = trackCol } });
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            // Row 2: Dynamic Guide Instruction or Room Summary
            if (isClosed)
            {
                GUILayout.Label($"ROOM AREA: {_measurement.Area:F2} m²  ({_measurement.Length:F2}m × {_measurement.Width:F2}m)", _areaResultStyle);
                string rectMsg = _measurement.IsRectangular ? "✓ Standard Rectangular Boundary Verified" : "⚠️ Non-Standard Rectangular Shape";
                Color rectCol = _measurement.IsRectangular ? new Color(0.25f, 0.95f, 0.55f) : new Color(0.95f, 0.82f, 0.20f);
                GUILayout.Label(rectMsg, new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Normal, normal = { textColor = rectCol } });
            }
            else
            {
                int flags = _measurement != null ? _measurement.FlagCount : 0;
                string instruction = flags switch
                {
                    0 => "Point reticle at first corner & tap Place Flag",
                    1 => "Walk to 2nd corner along wall & tap Place Flag",
                    2 => "Walk to 3rd corner & tap Place Flag",
                    3 => "Walk to 4th corner & tap Place Flag",
                    4 => "All 4 corners set! Tap Close Room below",
                    _ => "Scan floor to begin survey"
                };
                GUILayout.Label(instruction, _instructionStyle);

                // Row 3: Corner Progress Tracker (● ● ○ ○)
                string p1 = flags >= 1 ? "●" : "○";
                string p2 = flags >= 2 ? "●" : "○";
                string p3 = flags >= 3 ? "●" : "○";
                string p4 = flags >= 4 ? "●" : "○";

                string surfaceStr = (_measurement != null && _measurement.RaycastController != null)
                    ? _measurement.RaycastController.SurfaceTypeString
                    : "DETECTING";

                GUILayout.Label($"Corners:  {p1} {p2} {p3} {p4}  ({flags}/4)   |   Surface: {surfaceStr}", _subMetaStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawBottomActionDeck(int screenW, int screenH)
        {
            float deckW = Mathf.Min(screenW - 32f, 500f);
            float deckX = (screenW - deckW) * 0.5f;

            float subBtnH = 46f;
            float spacing = 12f;
            float pad = 16f;

            bool isClosed = _measurement != null && _measurement.IsRoomClosed;
            int flags = _measurement != null ? _measurement.FlagCount : 0;

            // Decide which main-button state applies first, so its (possibly long, e.g. the area
            // readout) label can be measured before the deck is laid out — never a fixed 56px guess.
            string mainBtnText;
            Color mainBtnCol;
            bool mainBtnEnabled = true;
            System.Action mainBtnAction;
            if (isClosed)
            {
                mainBtnText = $"⛏️ GENERATE MINE ENVIRONMENT ({_measurement.Area:F2} m²)";
                mainBtnCol = TacticalUITheme.FortniteGreen;
                mainBtnAction = OnConfirmClicked;
            }
            else if (flags == 4)
            {
                mainBtnText = "📐 CLOSE ROOM & SURVEY AREA";
                mainBtnCol = TacticalUITheme.FortniteBlue;
                mainBtnAction = OnCloseRoomClicked;
            }
            else
            {
                bool canAdd = _measurement != null && _measurement.CanAddFlag;
                mainBtnText = canAdd
                    ? $"🚩 PLACE FLAG  ({flags + 1}/4)"
                    : "🔍 POINT AT FLOOR TO CALIBRATE";
                mainBtnCol = canAdd ? TacticalUITheme.FortniteBlue : new Color(0.40f, 0.45f, 0.52f);
                mainBtnEnabled = canAdd;
                mainBtnAction = OnAddFlagClicked;
            }

            float innerWForMeasure = deckW - pad * 2f;
            float btnH = Mathf.Max(TacticalUITheme.CalcTextHeight(_mainBtnStyle, mainBtnText, innerWForMeasure - _mainBtnStyle.padding.horizontal) + _mainBtnStyle.padding.vertical, 56f);

            float deckH = pad * 2f + btnH + spacing + subBtnH;
            float deckY = screenH - deckH - 24f;

            DrawPanelBox(new Rect(deckX, deckY, deckW, deckH));

            float innerX = deckX + pad;
            float innerW = deckW - pad * 2f;
            float y = deckY + pad;

            // ==========================================================
            // ROW 1: PRIMARY ACTION BUTTON (PLACE FLAG / CLOSE / GENERATE)
            // ==========================================================
            Rect mainBtnRect = new Rect(innerX, y, innerW, btnH);

            GUI.enabled = mainBtnEnabled;
            if (TacticalUITheme.DrawFortniteButton(mainBtnRect, mainBtnText, mainBtnCol, _mainBtnStyle) && mainBtnEnabled)
            {
                mainBtnAction?.Invoke();
            }
            GUI.enabled = true;

            y += btnH + spacing;

            // ==========================================================
            // ROW 2: CENTERED SECONDARY CONTROLS (UNDO, RESET)
            // ==========================================================
            float halfW = (innerW - spacing) * 0.5f;

            bool canUndo = flags > 0 && !isClosed;
            Rect undoRect = new Rect(innerX, y, halfW, subBtnH);
            GUI.enabled = canUndo;
            if (TacticalUITheme.DrawFortniteButton(undoRect, "↩ UNDO", TacticalUITheme.FortniteNavyLight, _subBtnStyle) && canUndo)
            {
                OnUndoClicked();
            }
            GUI.enabled = true;

            Rect resetRect = new Rect(innerX + halfW + spacing, y, halfW, subBtnH);
            if (TacticalUITheme.DrawFortniteButton(resetRect, "🔄 RESET", TacticalUITheme.FortniteRed, _subBtnStyle))
            {
                OnResetClicked();
            }
        }

        private void DrawConfirmedOverlay(int screenW, int screenH)
        {
            // If training is already underway, suppress measurement overlay so trainee has an unobstructed screen
            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null &&
                ARMiningSimulator.Core.SimulationGameLoop.Instance.CurrentStage >= ARMiningSimulator.Core.SimulationStage.FireIgnited)
            {
                return;
            }

            if (ARMiningSimulator.Fire.FireManager.Instance != null && ARMiningSimulator.Fire.FireManager.Instance.HasActiveFires)
            {
                return;
            }

            var envGen = _measurement != null
                ? _measurement.GetComponent<ARMiningSimulator.Environment.MiningEnvironmentGenerator>() ?? FindFirstObjectByType<ARMiningSimulator.Environment.MiningEnvironmentGenerator>()
                : null;

            bool isEquipmentPending = envGen != null && !envGen.IsEquipmentSpawned && envGen.IsEnvironmentReady;
            int barW = Mathf.Min(screenW - 32, isEquipmentPending ? 640 : 540);
            int barH = 52;
            int barX = (screenW - barW) / 2;
            int barY = 18;

            DrawPanelBox(new Rect(barX, barY, barW, barH));

            float area = _measurement != null ? _measurement.Area : 0f;
            float rescanW = 110f;
            float rescanRectX = barX + barW - 16f - rescanW;

            if (isEquipmentPending)
            {
                GUI.Label(new Rect(barX + 18, barY, barW - 40 - rescanW - 130f - 16f, barH), $"⛏️ MINE BUILT  •  EQUIPMENT IN {envGen.RemainingEquipmentDelay:F1}s", _pendingStyle);

                Rect deployRect = new Rect(rescanRectX - 130f - 8f, barY + 8f, 130f, 36f);
                if (TacticalUITheme.DrawFortniteButton(deployRect, "⚡ DEPLOY NOW", TacticalUITheme.FortniteBlue, _overlayBtnStyle))
                {
                    envGen.DeployEquipmentNow();
                }
            }
            else
            {
                int machines = envGen != null && envGen.Spawner != null ? envGen.Spawner.SpawnedMachines.Count : 6;
                int elec = envGen != null && envGen.Spawner != null ? envGen.Spawner.SpawnedElectrical.Count : 11;
                string statusText = envGen != null && envGen.IsEquipmentSpawned
                    ? $"⛏️ MINE & EQUIPMENT ACTIVE ({machines}M | {elec}E)"
                    : $"⛏️ UNDERGROUND MINE ({area:F2} m²)";
                GUI.Label(new Rect(barX + 18, barY, barW - 36 - rescanW, barH), statusText, _confirmedActiveStyle);
            }

            Rect rescanRect = new Rect(rescanRectX, barY + 8f, rescanW, 36f);
            if (TacticalUITheme.DrawFortniteButton(rescanRect, "🔄 RE-SCAN", TacticalUITheme.FortniteNavyLight, _overlayBtnStyle))
            {
                OnResetClicked();
            }

            // If MiningEnvironmentUI is NOT present in the scene, draw the center-bottom Start Training Deck here
            var miningUI = FindFirstObjectByType<MiningEnvironmentUI>();
            if (miningUI == null && envGen != null && envGen.IsEquipmentSpawned)
            {
                int deckW = Mathf.Min(screenW - 40, 520);
                int deckH = 105;
                int deckX = (screenW - deckW) / 2;
                int deckY = screenH - deckH - 24;

                DrawPanelBox(new Rect(deckX, deckY, deckW, deckH));

                float innerX2 = deckX + 14f;
                float innerW2 = deckW - 28f;
                float y2 = deckY + 12f;

                if (TacticalUITheme.DrawFortniteButton(new Rect(innerX2, y2, innerW2, 50f), "🚨 START TRAINING DRILL", TacticalUITheme.FortniteGreen, _mainBtnStyle))
                {
                    StartTrainingFromMeasurementUI();
                }

                y2 += 50f + 8f;
                float half2 = (innerW2 - 8f) * 0.5f;

                if (TacticalUITheme.DrawFortniteButton(new Rect(innerX2, y2, half2, 32f), "🔀 RE-ROLL LAYOUT", TacticalUITheme.FortniteNavyLight, _subBtnStyle))
                {
                    if (envGen != null) envGen.Regenerate();
                }

                if (TacticalUITheme.DrawFortniteButton(new Rect(innerX2 + half2 + 8f, y2, half2, 32f), "🔄 RE-SCAN ROOM", TacticalUITheme.FortniteRed, _subBtnStyle))
                {
                    OnResetClicked();
                }
            }
        }

        private void StartTrainingFromMeasurementUI()
        {
            if (ARMiningSimulator.Core.SimulationGameLoop.Instance != null)
            {
                ARMiningSimulator.Core.SimulationGameLoop.Instance.StartTrainingDrill();
            }
            else
            {
                var envGen = _measurement != null
                    ? _measurement.GetComponent<ARMiningSimulator.Environment.MiningEnvironmentGenerator>() ?? FindAnyObjectByType<ARMiningSimulator.Environment.MiningEnvironmentGenerator>()
                    : FindAnyObjectByType<ARMiningSimulator.Environment.MiningEnvironmentGenerator>();
                if (envGen != null && !envGen.IsEquipmentSpawned && envGen.IsEnvironmentReady)
                {
                    envGen.DeployEquipmentNow();
                }

                var fireManager = FindAnyObjectByType<ARMiningSimulator.Fire.FireManager>();
                if (fireManager != null)
                {
                    fireManager.StartScenarioFires();
                }

                if (ARMiningSimulator.Player.TraineeDetection.Instance != null)
                {
                    ARMiningSimulator.Player.TraineeDetection.Instance.StartStopwatch();
                }

                var hud = FindAnyObjectByType<TraineeStatusHUD>();
                if (hud != null)
                {
                    hud.ResetFireVisibility();
                }
            }

            Debug.Log("[RoomMeasurementUI] 🚨 START TRAINING DRILL TRIGGERED!");
        }

        /// <summary>
        /// Real-time Development/Debug Panel (Requirement 23).
        /// Hidden by default for a clean simulation UI. Toggleable via subtle icon in top right.
        /// </summary>
        private void DrawDebugPanel(int screenW, int screenH)
        {
            // Subtle, small toggle button in top-right corner
            int toggleW = 76;
            int toggleH = 28;
            int toggleX = screenW - toggleW - 16;
            int toggleY = 18;

            GUI.backgroundColor = new Color(0.15f, 0.20f, 0.28f, 0.80f);
            if (GUI.Button(new Rect(toggleX, toggleY, toggleW, toggleH), _showDebugPanel ? "✕ DBG" : "⚙ DBG", _dbgToggleBtnStyle))
            {
                _showDebugPanel = !_showDebugPanel;
            }
            GUI.backgroundColor = Color.white;

            if (!_showDebugPanel) return;

            int panelW = 280;
            int panelH = 220;
            int panelX = screenW - panelW - 16;
            int panelY = toggleY + toggleH + 6;

            DrawPanelBox(new Rect(panelX, panelY, panelW, panelH));

            GUIStyle dbgHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.82f, 0.20f) }
            };

            GUIStyle dbgLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.85f, 0.90f, 0.95f) }
            };

            GUILayout.BeginArea(new Rect(panelX + 12, panelY + 8, panelW - 24, panelH - 16));

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
