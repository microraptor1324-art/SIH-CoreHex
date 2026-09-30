using UnityEngine;
using ARMiningSimulator.Decisions;
using ARMiningSimulator.Evacuation;
using ARMiningSimulator.Extinguisher;
using ARMiningSimulator.Fire;
using ARMiningSimulator.Investigation;
using ARMiningSimulator.Player;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// Centralized tactical UI theme. Provides:
    /// - A cohesive dark HUD palette (refined from the original high-neon set).
    /// - Genuinely rounded panels/buttons/bars, built from procedurally generated
    ///   9-sliced corner textures (no imported art — matches project rule).
    /// - Soft layered drop shadows instead of hard offset "3D" blocks.
    /// - Zero container collision guarantee with automatic modal arbitration.
    /// </summary>
    public static class TacticalUITheme
    {
        // =========================================================================
        // 1. PALETTE — cohesive tactical HUD, softened from the original neon set
        // =========================================================================
        public static readonly Color FortniteGreen = new Color(0.20f, 0.88f, 0.52f, 1.0f);
        public static readonly Color FortniteBlue = new Color(0.28f, 0.70f, 1.0f, 1.0f);
        public static readonly Color FortnitePurple = new Color(0.64f, 0.42f, 0.96f, 1.0f);
        public static readonly Color FortniteGold = new Color(1.0f, 0.78f, 0.28f, 1.0f);
        public static readonly Color FortniteRed = new Color(1.0f, 0.34f, 0.40f, 1.0f);
        public static readonly Color FortniteAmber = new Color(1.0f, 0.66f, 0.24f, 1.0f);
        public static readonly Color FortniteNavy = new Color(0.075f, 0.095f, 0.14f, 0.94f);
        public static readonly Color FortniteNavyLight = new Color(0.14f, 0.18f, 0.26f, 1.0f);
        public static readonly Color FortniteNavyDark = new Color(0.045f, 0.055f, 0.085f, 0.97f);
        public static readonly Color FortniteDarkShadow = new Color(0.0f, 0.0f, 0.0f, 0.35f);
        public static readonly Color FortniteBorder = new Color(0.30f, 0.42f, 0.62f, 0.55f);

        // Legacy compatibility aliases
        public static readonly Color Cyan = FortniteBlue;
        public static readonly Color CyanDim = new Color(0.20f, 0.55f, 0.78f, 0.75f);
        public static readonly Color Emerald = FortniteGreen;
        public static readonly Color EmeraldDim = new Color(0.15f, 0.62f, 0.40f, 0.75f);
        public static readonly Color Amber = FortniteAmber;
        public static readonly Color AmberDim = new Color(0.80f, 0.52f, 0.16f, 0.75f);
        public static readonly Color Crimson = FortniteRed;
        public static readonly Color CrimsonDim = new Color(0.72f, 0.22f, 0.26f, 0.75f);
        public static readonly Color Gold = FortniteGold;
        public static readonly Color CardBg = FortniteNavy;
        public static readonly Color CardBgSolid = FortniteNavyDark;
        public static readonly Color CardSlotBg = new Color(0.045f, 0.06f, 0.095f, 0.88f);
        public static readonly Color BorderSubtle = FortniteBorder;

        // =========================================================================
        // 2. SPACING / TYPE SCALE — shared rhythm so every screen lines up the same
        // =========================================================================
        public const float SpaceXS = 6f;
        public const float SpaceSM = 12f;
        public const float SpaceMD = 20f;
        public const float SpaceLG = 32f;
        public const float RadiusCard = 14f;
        public const float RadiusPill = 999f; // fully rounded / capsule

        // =========================================================================
        // DPI-AWARE SCALING
        // Every screen's fixed pixel sizes (fonts, paddings, row heights) were tuned
        // against a ~480px-wide reference. On a real phone's native resolution
        // (often 1080-1440px wide), those same raw pixel values render as tiny,
        // near-illegible text. BeginScaledGUI() scales the whole GUI coordinate
        // system up to compensate, so a "14px" font is always ~14px at a 480px
        // reference regardless of the device's actual pixel density. Callers must
        // use VW/VH instead of Screen.width/Screen.height while scaled, and must
        // call EndScaledGUI() in a finally block so a skipped call can never leave
        // the matrix scaled for some other script's OnGUI later in the same frame.
        // =========================================================================
        private const float ReferenceWidth = 480f;
        private static Matrix4x4 s_savedMatrix;

        public static float UIScale { get; private set; } = 1f;
        public static float VW { get; private set; }
        public static float VH { get; private set; }

        public static void BeginScaledGUI()
        {
            UIScale = Mathf.Clamp(Screen.width / ReferenceWidth, 1f, 2.6f);
            VW = Screen.width / UIScale;
            VH = Screen.height / UIScale;
            s_savedMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(UIScale, UIScale), Vector2.zero);
        }

        public static void EndScaledGUI()
        {
            GUI.matrix = s_savedMatrix;
        }

        private static Texture2D s_WhiteTex;
        public static Texture2D WhiteTexture
        {
            get
            {
                if (s_WhiteTex == null)
                {
                    s_WhiteTex = new Texture2D(1, 1);
                    s_WhiteTex.SetPixel(0, 0, Color.white);
                    s_WhiteTex.Apply();
                }
                return s_WhiteTex;
            }
        }

        // =========================================================================
        // 3. PROCEDURAL ROUNDED-CORNER TEXTURES (9-sliced, generated at runtime)
        // =========================================================================
        // Top-left/top-right HUD vitals cards (health, survival clock) share this height so any
        // top-center banner can position itself safely below both without per-screen guessing.
        public const float VitalsCardHeight = 84f;
        // Y a top-center banner (whose ribbon tag pokes ~11px above its own rect) must start at
        // on portrait screens to clear the vitals row (y=16 + VitalsCardHeight) with a visible gap.
        public const float HudTopZoneClearY = 16f + VitalsCardHeight + 14f + 11f;

        private const int CardTexSize = 48;
        private const int CardTexRadius = 14;
        private const int PillTexSize = 24;
        private const int PillTexRadius = 10;

        private static Texture2D s_CardRoundTex;
        private static Texture2D s_PillRoundTex;
        private static GUIStyle s_CardRoundStyle;
        private static GUIStyle s_PillRoundStyle;

        private static Texture2D BuildRoundedTexture(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = x + 0.5f;
                    float cy = y + 0.5f;
                    float minX = radius, minY = radius, maxX = size - radius, maxY = size - radius;
                    float dx = cx < minX ? minX - cx : (cx > maxX ? cx - maxX : 0f);
                    float dy = cy < minY ? minY - cy : (cy > maxY ? cy - maxY : 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static GUIStyle GetRoundedStyle(bool pill)
        {
            if (pill)
            {
                if (s_PillRoundStyle == null)
                {
                    s_PillRoundTex = BuildRoundedTexture(PillTexSize, PillTexRadius);
                    s_PillRoundStyle = new GUIStyle();
                    s_PillRoundStyle.normal.background = s_PillRoundTex;
                    int b = PillTexSize / 2 - 1;
                    s_PillRoundStyle.border = new RectOffset(b, b, b, b);
                }
                return s_PillRoundStyle;
            }

            if (s_CardRoundStyle == null)
            {
                s_CardRoundTex = BuildRoundedTexture(CardTexSize, CardTexRadius);
                s_CardRoundStyle = new GUIStyle();
                s_CardRoundStyle.normal.background = s_CardRoundTex;
                int b = CardTexSize / 2 - 1;
                s_CardRoundStyle.border = new RectOffset(b, b, b, b);
            }
            return s_CardRoundStyle;
        }

        /// <summary>Draws a genuinely rounded, flat-filled rectangle. Use pill=true for capsule-shaped bars/badges/short buttons.</summary>
        public static void DrawRoundedRect(Rect r, Color tint, bool pill = false)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            Color old = GUI.color;
            GUI.color = tint;
            GUI.Box(r, GUIContent.none, GetRoundedStyle(pill));
            GUI.color = old;
        }

        // =========================================================================
        // 4. MODAL CONFLICT RESOLUTION (Guarantees zero UI overlap)
        // =========================================================================
        public static bool IsAnyModalOpen()
        {
            if (TraineeHealth.Instance != null && TraineeHealth.Instance.IsIncapacitated)
                return true;

            if (DecisionManager.Instance != null &&
                (DecisionManager.Instance.IsWaitingForSelection || DecisionManager.Instance.IsShowingFeedback))
                return true;

            if (EvacuationManager.Instance != null && EvacuationManager.Instance.IsSafeZoneReached &&
                (FireResponseManager.Instance == null || FireResponseManager.Instance.State == FireResponseState.Inactive))
                return true;

            if (FireResponseManager.Instance != null &&
                (FireResponseManager.Instance.State == FireResponseState.Stage3A_FireSizeEval ||
                 FireResponseManager.Instance.State == FireResponseState.Stage3A_ActionChoice ||
                 FireResponseManager.Instance.State == FireResponseState.Stage3B_AgentSelection ||
                 FireResponseManager.Instance.State == FireResponseState.Extinguished))
                return true;

            if (InvestigationManager.Instance != null &&
                InvestigationManager.Instance.State != InvestigationState.Inactive)
                return true;

            return false;
        }

        // =========================================================================
        // 5. FLAT / SHARP DRAWING PRIMITIVES (for reticles, brackets, dividers)
        // =========================================================================
        public static void DrawRect(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, WhiteTexture);
            GUI.color = old;
        }

        public static void DrawOutline(Rect r, Color c, float thickness = 2.0f)
        {
            DrawRect(new Rect(r.x, r.y, r.width, thickness), c);
            DrawRect(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), c);
            DrawRect(new Rect(r.x, r.y, thickness, r.height), c);
            DrawRect(new Rect(r.x + r.width - thickness, r.y, thickness, r.height), c);
        }

        // =========================================================================
        // 6. CARD / PANEL
        // =========================================================================
        /// <summary>
        /// Draws a clean rounded card: soft layered shadow, thin accent border, flat fill,
        /// a faint top sheen for depth, and an optional rounded pill title tag overlapping the top edge.
        /// </summary>
        public static void DrawFortniteCard(Rect r, Color borderAccent, Color bgCol, string bannerTag = null, Color bannerBg = default)
        {
            // 1. Soft layered shadow (blur simulation via decreasing-alpha offsets)
            DrawRoundedRect(new Rect(r.x, r.y + 8f, r.width, r.height), new Color(0f, 0f, 0f, 0.10f));
            DrawRoundedRect(new Rect(r.x, r.y + 4f, r.width, r.height), new Color(0f, 0f, 0f, 0.18f));

            // 2. Thin accent border (drawn as a slightly larger rounded rect behind the fill)
            DrawRoundedRect(r, new Color(borderAccent.r, borderAccent.g, borderAccent.b, 0.55f));
            Rect inner = new Rect(r.x + 1.5f, r.y + 1.5f, r.width - 3f, r.height - 3f);
            DrawRoundedRect(inner, bgCol);

            // 3. Faint top sheen for a touch of depth
            float sheenH = Mathf.Min(inner.height * 0.35f, 46f);
            DrawRoundedRect(new Rect(inner.x + 8f, inner.y + 2f, inner.width - 16f, sheenH), new Color(1f, 1f, 1f, 0.025f));

            // 4. Rounded pill title tag overlapping the top edge — some callers pass a long, fully
            // dynamic tag (e.g. a whole decision title), which wraps to 2+ lines on a narrow
            // screen. Measure it instead of assuming one fixed 22px line, so the pill grows to
            // fit and never spills its own text down into the card's title beneath it.
            if (!string.IsNullOrEmpty(bannerTag))
            {
                if (bannerBg == default) bannerBg = borderAccent;
                float tagW = Mathf.Min(r.width - 32f, 320f);
                string tagText = bannerTag.ToUpper();

                var tagStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = new Color(0.05f, 0.06f, 0.09f) }
                };
                float tagH = Mathf.Max(CalcTextHeight(tagStyle, tagText, tagW - 12f) + 6f, 22f);
                Rect tagRect = new Rect(r.x + 16f, r.y - tagH * 0.5f, tagW, tagH);

                DrawRoundedRect(new Rect(tagRect.x, tagRect.y + 2f, tagRect.width, tagRect.height), new Color(0f, 0f, 0f, 0.28f), true);
                DrawRoundedRect(tagRect, bannerBg, true);
                GUI.Label(tagRect, tagText, tagStyle);
            }
        }

        // =========================================================================
        // 7. BUTTON
        // =========================================================================
        /// <summary>
        /// Draws a clean rounded button with a soft shadow, subtle hover glow, and a gentle
        /// press displacement. No more chunky offset "3D shelf" — flatter, calmer, modern.
        /// </summary>
        public static bool DrawFortniteButton(Rect r, string label, Color themeCol, GUIStyle style, string rarityBadge = null, bool isEnabled = true)
        {
            bool isHover = r.Contains(Event.current.mousePosition);
            bool isPressed = isHover && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag);

            float pressOffset = isPressed ? 2f : 0f;
            Rect faceRect = new Rect(r.x, r.y + pressOffset, r.width, r.height - pressOffset);

            // 1. Soft shadow (skipped while pressed, so the button visually "sinks")
            if (!isPressed)
                DrawRoundedRect(new Rect(faceRect.x, faceRect.y + 3f, faceRect.width, faceRect.height), new Color(0f, 0f, 0f, 0.22f));

            // 2. Border + fill
            Color borderCol = isHover ? themeCol : new Color(themeCol.r, themeCol.g, themeCol.b, 0.50f);
            Color faceBg = isHover
                ? new Color(themeCol.r * 0.22f + 0.05f, themeCol.g * 0.22f + 0.06f, themeCol.b * 0.22f + 0.09f, 0.97f)
                : new Color(0.095f, 0.12f, 0.165f, 0.95f);

            DrawRoundedRect(faceRect, borderCol);
            Rect innerFace = new Rect(faceRect.x + 1.5f, faceRect.y + 1.5f, faceRect.width - 3f, faceRect.height - 3f);
            DrawRoundedRect(innerFace, faceBg);

            // 3. Top sheen
            DrawRoundedRect(new Rect(innerFace.x + 8f, innerFace.y + 1.5f, innerFace.width - 16f, innerFace.height * 0.38f),
                new Color(1f, 1f, 1f, isHover ? 0.05f : 0.025f));

            // 4. Rarity / score pill tag (top-right)
            if (!string.IsNullOrEmpty(rarityBadge))
            {
                var badgeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = new Color(themeCol.r, themeCol.g, themeCol.b, 0.85f) }
                };
                GUI.Label(new Rect(innerFace.x + 10f, innerFace.y + 4f, innerFace.width - 20f, 16f), rarityBadge.ToUpper(), badgeStyle);
            }

            // 5. Label — GUI.Button with a style based on GUI.skin.button still carries Unity's
            // stock button background sprite on every state (only textColor was overridden
            // above), which was rendering on top of our custom chrome and washing everything
            // out to flat gray. Strip every background slot so only our own drawing shows,
            // and render the text with a soft dark shadow so it stays legible over the
            // translucent AR camera feed regardless of what's behind the card.
            var clickStyle = new GUIStyle(style);
            clickStyle.normal.background = null;
            clickStyle.hover.background = null;
            clickStyle.active.background = null;
            clickStyle.focused.background = null;
            clickStyle.onNormal.background = null;
            clickStyle.onHover.background = null;
            clickStyle.onActive.background = null;
            clickStyle.onFocused.background = null;

            bool clicked = GUI.Button(innerFace, GUIContent.none, clickStyle);

            if (!string.IsNullOrEmpty(label))
            {
                var shadowStyle = new GUIStyle(clickStyle) { normal = { textColor = new Color(0f, 0f, 0f, 0.85f) } };
                var textStyle = new GUIStyle(clickStyle) { normal = { textColor = Color.white } };
                GUI.Label(new Rect(innerFace.x + 1.5f, innerFace.y + 1.5f, innerFace.width, innerFace.height), label, shadowStyle);
                GUI.Label(innerFace, label, textStyle);
            }

            return clicked;
        }

        /// <summary>
        /// Draws a label with a soft dark shadow behind it so it stays legible regardless of
        /// what's rendered behind the card (translucent AR camera feed, bright backgrounds, etc.).
        /// Use for any text drawn directly onto a card/button face outside of DrawFortniteButton.
        /// </summary>
        public static void DrawShadowedLabel(Rect r, string text, GUIStyle style)
        {
            if (string.IsNullOrEmpty(text)) return;
            var shadowStyle = new GUIStyle(style) { normal = { textColor = new Color(0f, 0f, 0f, 0.85f) } };
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, shadowStyle);
            GUI.Label(r, text, style);
        }

        // =========================================================================
        // 7b. CONTENT-DRIVEN SIZING
        // IMGUI has no Content Size Fitter — GUIStyle.CalcHeight is the real equivalent:
        // it measures exactly how tall a given string needs to be at a given width, word
        // wrap included. Every dynamically-sized card/row below measures its actual text
        // this way instead of guessing a fixed pixel height, so long strings can never clip.
        // =========================================================================
        public static float CalcTextHeight(GUIStyle style, string text, float width)
        {
            if (style == null || string.IsNullOrEmpty(text) || width <= 0f) return 0f;
            return style.CalcHeight(new GUIContent(text), width);
        }

        /// <summary>
        /// Measures the total height a two-line "title + description" option row needs at
        /// the given inner width, so callers can size the row to its real content instead
        /// of a guessed constant. Mirrors the padding used by DrawTwoLineOption-style helpers.
        /// </summary>
        public static float MeasureOptionRowHeight(string title, string description, GUIStyle titleStyle, GUIStyle descStyle, float innerWidth, float topPad = 10f, float gap = 6f, float bottomPad = 10f)
        {
            float titleH = string.IsNullOrEmpty(title) ? 0f : CalcTextHeight(titleStyle, title, innerWidth);
            float descH = string.IsNullOrEmpty(description) ? 0f : CalcTextHeight(descStyle, description, innerWidth);
            float total = topPad + titleH;
            if (descH > 0f) total += gap + descH;
            total += bottomPad;
            return Mathf.Max(total, 44f); // never smaller than a comfortably tappable row
        }

        // =========================================================================
        // 8. PROGRESS / METER BAR
        // =========================================================================
        /// <summary>Draws a clean rounded/capsule progress bar with a soft shadow and gentle fill sheen.</summary>
        public static void DrawFortniteBar(Rect r, float ratio, Color fillCol, Color bgCol, int segments = 10)
        {
            ratio = Mathf.Clamp01(ratio);

            DrawRoundedRect(new Rect(r.x, r.y + 2f, r.width, r.height), new Color(0f, 0f, 0f, 0.18f), true);
            DrawRoundedRect(r, bgCol, true);

            float fillWidth = (r.width - 4f) * ratio;
            if (fillWidth > 2f)
            {
                Rect fillRect = new Rect(r.x + 2f, r.y + 2f, fillWidth, r.height - 4f);
                DrawRoundedRect(fillRect, fillCol, true);
                DrawRoundedRect(new Rect(fillRect.x, fillRect.y, fillRect.width, fillRect.height * 0.45f), new Color(1f, 1f, 1f, 0.18f), true);
            }
        }

        // =========================================================================
        // LEGACY HELPER COMPATIBILITY
        // =========================================================================
        public static void DrawTechCard(Rect r, Color bg, Color border, Color accentTop, float accentHeight = 3.5f)
        {
            DrawFortniteCard(r, border, bg);
        }

        public static void DrawCornerBrackets(Rect r, Color c, float len = 10f, float thick = 2f)
        {
            len = Mathf.Min(len, Mathf.Min(r.width * 0.4f, r.height * 0.4f));
            DrawRect(new Rect(r.x, r.y, len, thick), c);
            DrawRect(new Rect(r.x, r.y, thick, len), c);
            DrawRect(new Rect(r.x + r.width - len, r.y, len, thick), c);
            DrawRect(new Rect(r.x + r.width - thick, r.y, thick, len), c);
            DrawRect(new Rect(r.x, r.y + r.height - thick, len, thick), c);
            DrawRect(new Rect(r.x, r.y + r.height - len, thick, len), c);
            DrawRect(new Rect(r.x + r.width - len, r.y + r.height - thick, len, thick), c);
            DrawRect(new Rect(r.x + r.width - thick, r.y + r.height - len, thick, len), c);
        }

        public static void DrawSegmentedBar(Rect r, float ratio, int segments, Color fillCol, Color slotCol, float glowAlpha = 0.25f)
        {
            DrawFortniteBar(r, ratio, fillCol, slotCol, segments);
        }

        public static bool DrawTacticalButton(Rect r, string label, Color accentCol, GUIStyle style, bool isEnabled = true)
        {
            return DrawFortniteButton(r, label, accentCol, style, null, isEnabled);
        }
    }
}
