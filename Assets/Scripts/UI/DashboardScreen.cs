using UnityEngine;

namespace ARMiningSimulator.UI
{
    /// <summary>
    /// In-app landing screen shown before the AR simulation starts, so opening the app lands on
    /// a dashboard (AR Training -> Underground Fire -> Launch) instead of dropping straight into
    /// room scanning. Built with the project's existing IMGUI system (no WebView/new dependency).
    /// Self-bootstraps at scene load like DashboardBridge/FireOriginTapDetector.
    /// </summary>
    public class DashboardScreen : MonoBehaviour
    {
        public static bool IsShowing { get; private set; } = true;

        /// <summary>Called by DashboardBridge when a scenario was launched from the external web
        /// dashboard via deep link — that flow already chose a scenario, so this screen is skipped.</summary>
        public static void Dismiss() => IsShowing = false;

        private enum Page { Home, ArTraining, UndergroundFireDetail }
        private Page _page = Page.Home;

        private static readonly string[] PlaceholderScenarios =
        {
            "Toxic Gas Leak", "Machine Isolation", "Confined Space", "PPE Inspection"
        };

        private GUIStyle _titleStyle, _subtitleStyle, _sectionTitleStyle, _bodyStyle,
            _cardTitleStyle, _cardBodyStyle, _btnStyle, _navBtnStyle, _lockedStyle;
        private bool _stylesInit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<DashboardScreen>() != null) return;
            var go = new GameObject("DashboardScreen");
            go.AddComponent<DashboardScreen>();
            DontDestroyOnLoad(go);
        }

        private void InitStyles()
        {
            if (_stylesInit) return;
            _stylesInit = true;

            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = TacticalUITheme.FortniteGold } };
            _subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.6f, 0.63f, 0.6f) } };
            _sectionTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, normal = { textColor = new Color(0.68f, 0.7f, 0.68f) } };
            _cardTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _cardBodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true, normal = { textColor = new Color(0.6f, 0.63f, 0.6f) } };
            _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _navBtnStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _lockedStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.5f, 0.5f, 0.5f) } };
        }

        private void OnGUI()
        {
            if (!IsShowing) return;
            InitStyles();

            TacticalUITheme.BeginScaledGUI();
            try
            {
                DrawContent();
            }
            finally
            {
                TacticalUITheme.EndScaledGUI();
            }
        }

        private void DrawContent()
        {
            float w = TacticalUITheme.VW;
            float h = TacticalUITheme.VH;

            // Opaque backdrop so the raw AR camera feed doesn't show through behind the dashboard.
            TacticalUITheme.DrawRect(new Rect(0, 0, w, h), new Color(0.04f, 0.043f, 0.043f, 1f));

            switch (_page)
            {
                case Page.Home: DrawHome(w, h); break;
                case Page.ArTraining: DrawArTraining(w, h); break;
                case Page.UndergroundFireDetail: DrawUndergroundFireDetail(w, h); break;
            }
        }

        private void DrawHeader(float w, string label)
        {
            TacticalUITheme.DrawShadowedLabel(new Rect(20, 24, w - 40, 32), "SURAKSHA-AR", _titleStyle);
            TacticalUITheme.DrawShadowedLabel(new Rect(20, 56, w - 40, 18), label, _subtitleStyle);
        }

        private void DrawHome(float w, float h)
        {
            DrawHeader(w, "MINE SAFETY COMMAND");

            TacticalUITheme.DrawShadowedLabel(new Rect(20, 96, w - 40, 60),
                "AR-based fire safety training for underground mining crews. Pick a track below to begin.",
                _bodyStyle);

            Rect cardRect = new Rect(20, 168, w - 40, 96);
            TacticalUITheme.DrawFortniteCard(cardRect, TacticalUITheme.FortniteGold, TacticalUITheme.CardBgSolid, "TRAINING");
            TacticalUITheme.DrawShadowedLabel(new Rect(cardRect.x + 18, cardRect.y + 16, cardRect.width - 36, 24), "AR Training", _sectionTitleStyle);
            TacticalUITheme.DrawShadowedLabel(new Rect(cardRect.x + 18, cardRect.y + 42, cardRect.width - 36, 44), "Scenario-based AR simulations, including live fire response drills.", _cardBodyStyle);

            Rect btnRect = new Rect(20, h - 90, w - 40, 56);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "AR TRAINING", TacticalUITheme.FortniteGold, _btnStyle))
            {
                _page = Page.ArTraining;
            }
        }

        private void DrawArTraining(float w, float h)
        {
            DrawBackButton(() => _page = Page.Home);
            TacticalUITheme.DrawShadowedLabel(new Rect(20, 64, w - 40, 26), "AR TRAINING", _sectionTitleStyle);
            TacticalUITheme.DrawShadowedLabel(new Rect(20, 92, w - 40, 34), "Select a scenario to begin.", _bodyStyle);

            float y = 136f;
            const float gap = 12f;

            // Underground Fire is the only scenario with a real Unity simulation behind it.
            Rect ufRect = new Rect(20, y, w - 40, 92f);
            TacticalUITheme.DrawFortniteCard(ufRect, TacticalUITheme.FortniteRed, TacticalUITheme.CardBgSolid, "READY");
            TacticalUITheme.DrawShadowedLabel(new Rect(ufRect.x + 18, ufRect.y + 16, ufRect.width - 36, 22), "Underground Fire", _cardTitleStyle);
            TacticalUITheme.DrawShadowedLabel(new Rect(ufRect.x + 18, ufRect.y + 40, ufRect.width - 36, 44), "Detect, evacuate and suppress a live underground fire.", _cardBodyStyle);
            if (GUI.Button(ufRect, GUIContent.none, GUIStyle.none))
            {
                _page = Page.UndergroundFireDetail;
            }
            y += ufRect.height + gap;

            foreach (var name in PlaceholderScenarios)
            {
                Rect r = new Rect(20, y, w - 40, 60f);
                TacticalUITheme.DrawFortniteCard(r, new Color(0.35f, 0.37f, 0.35f), TacticalUITheme.CardBgSolid);
                TacticalUITheme.DrawShadowedLabel(new Rect(r.x + 18, r.y + 10, r.width - 100, 20), name, _cardTitleStyle);
                TacticalUITheme.DrawShadowedLabel(new Rect(r.x + r.width - 84, r.y + 20, 66, 20), "SOON", _lockedStyle);
                y += r.height + gap;
            }
        }

        private void DrawUndergroundFireDetail(float w, float h)
        {
            DrawBackButton(() => _page = Page.ArTraining);
            TacticalUITheme.DrawShadowedLabel(new Rect(20, 64, w - 40, 26), "UNDERGROUND FIRE", _sectionTitleStyle);

            Rect descRect = new Rect(20, 104, w - 40, 150);
            TacticalUITheme.DrawFortniteCard(descRect, TacticalUITheme.FortniteRed, TacticalUITheme.CardBgSolid, "MODULE");
            TacticalUITheme.DrawShadowedLabel(new Rect(descRect.x + 18, descRect.y + 18, descRect.width - 36, 116),
                "A full AR fire-response drill: scan your room, detect a live fire, make critical decisions, evacuate, and suppress the blaze before it spreads. Your performance is scored at the end.",
                _bodyStyle);

            Rect btnRect = new Rect(20, h - 90, w - 40, 56);
            if (TacticalUITheme.DrawFortniteButton(btnRect, "LAUNCH AR SIMULATION", TacticalUITheme.FortniteRed, _btnStyle))
            {
                Debug.Log("[DashboardScreen] Launching Underground Fire AR simulation");
                Dismiss();
            }
        }

        private void DrawBackButton(System.Action onBack)
        {
            Rect r = new Rect(20, 20, 76, 32);
            if (TacticalUITheme.DrawFortniteButton(r, "< BACK", TacticalUITheme.FortniteBlue, _navBtnStyle))
            {
                onBack();
            }
        }
    }
}
