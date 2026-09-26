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
    /// Centralized Fortnite & Action-Game Inspired UI Theme.
    /// Provides:
    /// - Bold, high-energy arcade color palette (Fortnite Green, Shield Cyan, Storm Purple, Legendary Gold).
    /// - Chunky 3D beveled buttons with tactile press-down states.
    /// - Slanted quest badges, drop-shadowed containers, and high-impact meters.
    /// - Zero container collision guarantee with automatic modal arbitration.
    /// </summary>
    public static class TacticalUITheme
    {
        // =========================================================================
        // 1. FORTNITE & BATTLE ROYALE ACTION PALETTE
        // =========================================================================
        public static readonly Color FortniteGreen = new Color(0.0f, 1.0f, 0.42f, 1.0f);        // Vivid Health Green
        public static readonly Color FortniteBlue = new Color(0.0f, 0.85f, 1.0f, 1.0f);         // Shield / Radar Cyan
        public static readonly Color FortnitePurple = new Color(0.78f, 0.22f, 1.0f, 1.0f);      // Epic Loot / Storm Alert
        public static readonly Color FortniteGold = new Color(1.0f, 0.80f, 0.0f, 1.0f);         // Legendary Tier / Score
        public static readonly Color FortniteRed = new Color(1.0f, 0.16f, 0.28f, 1.0f);          // Flash Hazard / Damage
        public static readonly Color FortniteAmber = new Color(1.0f, 0.62f, 0.08f, 1.0f);        // Threat Warning
        public static readonly Color FortniteNavy = new Color(0.06f, 0.09f, 0.16f, 0.95f);       // Card Body Fill
        public static readonly Color FortniteNavyLight = new Color(0.12f, 0.18f, 0.28f, 1.0f);   // Subdued Button Fill
        public static readonly Color FortniteNavyDark = new Color(0.04f, 0.06f, 0.11f, 0.98f);   // Modal Solid Fill
        public static readonly Color FortniteDarkShadow = new Color(0.02f, 0.03f, 0.05f, 0.98f); // 3D Drop Shadow
        public static readonly Color FortniteBorder = new Color(0.18f, 0.28f, 0.48f, 0.80f);     // Bevel Edge

        // Legacy compatibility aliases
        public static readonly Color Cyan = FortniteBlue;
        public static readonly Color CyanDim = new Color(0.0f, 0.70f, 0.85f, 0.75f);
        public static readonly Color Emerald = FortniteGreen;
        public static readonly Color EmeraldDim = new Color(0.0f, 0.75f, 0.38f, 0.75f);
        public static readonly Color Amber = FortniteAmber;
        public static readonly Color AmberDim = new Color(0.85f, 0.55f, 0.08f, 0.75f);
        public static readonly Color Crimson = FortniteRed;
        public static readonly Color CrimsonDim = new Color(0.75f, 0.12f, 0.18f, 0.75f);
        public static readonly Color Gold = FortniteGold;
        public static readonly Color CardBg = FortniteNavy;
        public static readonly Color CardBgSolid = FortniteNavyDark;
        public static readonly Color CardSlotBg = new Color(0.03f, 0.05f, 0.09f, 0.90f);
        public static readonly Color BorderSubtle = FortniteBorder;

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
        // 2. MODAL CONFLICT RESOLUTION (Guarantees zero UI overlap)
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
        // 3. FORTNITE-STYLE DRAWING PRIMITIVES
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

        /// <summary>
        /// Draws a bold Fortnite-style card with a 3D drop-shadow, chunky colored border,
        /// and an optional slanted quest/objective header ribbon.
        /// </summary>
        public static void DrawFortniteCard(Rect r, Color borderAccent, Color bgCol, string bannerTag = null, Color bannerBg = default)
        {
            // 1. 3D Drop shadow offset plate (bottom-right 4px)
            DrawRect(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), FortniteDarkShadow);

            // 2. Base card plate
            DrawRect(r, bgCol);

            // 3. Chunky colored border (2.5px)
            DrawOutline(r, borderAccent, 2.5f);

            // 4. Subtle inner highlight on top edge
            DrawRect(new Rect(r.x + 2f, r.y + 2f, r.width - 4f, 2f), new Color(1f, 1f, 1f, 0.20f));

            // 5. Stylized angled top badge ribbon
            if (!string.IsNullOrEmpty(bannerTag))
            {
                if (bannerBg == default) bannerBg = borderAccent;
                float tagW = Mathf.Min(r.width - 24f, 300f);
                float tagH = 24f;
                Rect tagRect = new Rect(r.x + 14f, r.y - 12f, tagW, tagH);

                // Tag shadow & body
                DrawRect(new Rect(tagRect.x + 2f, tagRect.y + 2f, tagRect.width, tagRect.height), FortniteDarkShadow);
                DrawRect(tagRect, bannerBg);
                DrawOutline(tagRect, Color.white, 1.5f);

                // Inverted high-contrast title
                Color old = GUI.color;
                GUI.color = Color.black;
                var tagStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(tagRect, bannerTag.ToUpper(), tagStyle);
                GUI.color = old;
            }
        }

        /// <summary>
        /// Draws a chunky 3D arcade button with an extruded bottom shelf and tactile press displacement.
        /// </summary>
        public static bool DrawFortniteButton(Rect r, string label, Color themeCol, GUIStyle style, string rarityBadge = null, bool isEnabled = true)
        {
            bool isHover = r.Contains(Event.current.mousePosition);
            bool isPressed = isHover && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag);

            float pressOffset = isPressed ? 3f : 0f;
            float shelfHeight = 5f;

            // 1. Dark extruded bottom shelf
            Color shelfCol = new Color(themeCol.r * 0.32f, themeCol.g * 0.32f, themeCol.b * 0.32f, 1.0f);
            DrawRect(new Rect(r.x, r.y + r.height - shelfHeight, r.width, shelfHeight), shelfCol);

            // 2. Displaced button face
            Rect faceRect = new Rect(r.x, r.y + pressOffset, r.width, r.height - (isPressed ? 2f : shelfHeight));

            Color faceBg = isHover
                ? new Color(themeCol.r * 0.30f + 0.08f, themeCol.g * 0.30f + 0.10f, themeCol.b * 0.30f + 0.16f, 0.98f)
                : new Color(0.08f, 0.11f, 0.18f, 0.96f);

            DrawRect(faceRect, faceBg);

            // 3. Thick border
            Color borderCol = isHover ? themeCol : new Color(themeCol.r, themeCol.g, themeCol.b, 0.75f);
            DrawOutline(faceRect, borderCol, isHover ? 2.5f : 1.8f);

            // 4. Top highlight gleam
            DrawRect(new Rect(faceRect.x + 2f, faceRect.y + 1f, faceRect.width - 4f, 2f), new Color(1f, 1f, 1f, isHover ? 0.35f : 0.15f));

            // 5. Left rarity accent tab
            DrawRect(new Rect(faceRect.x, faceRect.y, 6f, faceRect.height), themeCol);

            // 6. Rarity / Score Pill Tag (Top Right)
            if (!string.IsNullOrEmpty(rarityBadge))
            {
                var badgeStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = themeCol }
                };
                GUI.Label(new Rect(faceRect.x + 10f, faceRect.y + 3f, faceRect.width - 20f, 16f), rarityBadge.ToUpper(), badgeStyle);
            }

            // 7. Button text
            Color old = GUI.color;
            GUI.color = Color.white;
            bool clicked = GUI.Button(faceRect, label, style);
            GUI.color = old;

            return clicked;
        }

        /// <summary>
        /// Draws a chunky Fortnite-style health or shield meter with segmented ticks and drop shadow.
        /// </summary>
        public static void DrawFortniteBar(Rect r, float ratio, Color fillCol, Color bgCol, int segments = 10)
        {
            ratio = Mathf.Clamp01(ratio);

            // 1. Drop shadow
            DrawRect(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), FortniteDarkShadow);

            // 2. Dark trench background
            DrawRect(r, bgCol);

            // 3. Filled progress bar
            float fillWidth = (r.width - 4f) * ratio;
            if (fillWidth > 1f)
            {
                Rect fillRect = new Rect(r.x + 2f, r.y + 2f, fillWidth, r.height - 4f);
                DrawRect(fillRect, fillCol);

                // Highlight sheen on top half of bar
                DrawRect(new Rect(fillRect.x, fillRect.y, fillRect.width, fillRect.height * 0.40f), new Color(1f, 1f, 1f, 0.25f));
            }

            // 4. Divider tick marks
            if (segments > 1)
            {
                float segW = r.width / segments;
                for (int i = 1; i < segments; i++)
                {
                    DrawRect(new Rect(r.x + i * segW, r.y, 1.5f, r.height), new Color(0f, 0f, 0f, 0.45f));
                }
            }

            // 5. Chunky outer border
            DrawOutline(r, new Color(fillCol.r, fillCol.g, fillCol.b, 0.85f), 1.8f);
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
