using UnityEngine;
using DeepShift.Core;
using DeepShift.Economy;
using DeepShift.Mining;

namespace DeepShift.UI
{
    /// <summary>
    /// Shown when <see cref="_onHoistExtracted"/> fires. Gives the player two choices:
    /// <list type="bullet">
    ///   <item><b>Extract to Surface</b> — banks carried ore as credits, saves, loads SurfaceCamp.</item>
    ///   <item><b>Go Deeper</b> — advances to the next floor without banking.</item>
    /// </list>
    /// Attach to any active GameObject in the Mine scene. Wire <see cref="_onHoistExtracted"/> in the Inspector.
    /// </summary>
    public class HoistChoiceUI : MonoBehaviour, IGameEventListener
    {
        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO _onHoistExtracted;

        // ── State ──────────────────────────────────────────────────────────────

        private bool _visible;

        // ── Cached references (found once on first show) ──────────────────────

        private ExtractionSystem   _extraction;
        private MineTestBootstrap  _bootstrap;

        // ── GUI styles ────────────────────────────────────────────────────────

        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()  => _onHoistExtracted?.RegisterListener(this);
        private void OnDisable() => _onHoistExtracted?.UnregisterListener(this);

        private void Start()
        {
            _headerStyle = new GUIStyle
            {
                fontSize  = 64,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _headerStyle.normal.textColor = new Color(1f, 0.65f, 0.1f);

            _bodyStyle = new GUIStyle
            {
                fontSize  = 36,
                alignment = TextAnchor.MiddleCenter,
            };
            _bodyStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        }

        // ── IGameEventListener (HoistExtracted) ───────────────────────────────

        /// <summary>Shows the choice overlay when the hoist countdown completes.</summary>
        public void OnEventRaised()
        {
            _extraction = FindFirstObjectByType<ExtractionSystem>();
            _bootstrap  = FindFirstObjectByType<MineTestBootstrap>();
            _visible    = true;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_visible) return;
            if (_buttonStyle == null) BuildButtonStyle();

            float sw = Screen.width;
            float sh = Screen.height;
            float cx = sw * 0.5f;
            float cy = sh * 0.5f;

            // Dark overlay
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(cx - 500f, cy - 200f, 1000f, 90f),
                "HOIST READY", _headerStyle);

            GUI.Label(new Rect(cx - 500f, cy - 90f, 1000f, 60f),
                "The hoist cable is locked. What's your call?", _bodyStyle);

            // ── Extract to Surface ─────────────────────────────────────────────
            if (GUI.Button(new Rect(cx - 420f, cy + 20f, 360f, 90f),
                    "Extract to Surface", _buttonStyle))
            {
                _visible = false;
                _extraction?.BankOreAndSave();
                SceneController.Instance?.EnterSurfaceCamp();
            }

            // ── Go Deeper ─────────────────────────────────────────────────────
            if (GUI.Button(new Rect(cx + 60f, cy + 20f, 360f, 90f),
                    "Go Deeper", _buttonStyle))
            {
                _visible = false;
                _bootstrap?.AdvanceToNextFloor();
            }
        }

        private void BuildButtonStyle()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 32,
                fontStyle = FontStyle.Bold,
            };
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor  = new Color(1f, 0.65f, 0.1f);
        }
    }
}
