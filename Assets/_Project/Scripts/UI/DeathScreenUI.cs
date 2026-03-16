using UnityEngine;
using DeepShift.Core;

namespace DeepShift.UI
{
    /// <summary>
    /// Full-screen death overlay shown when the player dies.
    /// Displays the shift termination summary (ore lost, revival fee, current debt)
    /// and a "Return to Surface" button that raises <see cref="_onShiftEnded"/>,
    /// causing <see cref="DeepShift.Core.SceneController"/> to load the SurfaceCamp scene.
    /// Run state (ore, HP) is handled by the scene reload and system resets on scene entry.
    /// Attach to any active GameObject in the Mine scene. Wire <see cref="_onShiftEnded"/> in the Inspector.
    /// </summary>
    public class DeathScreenUI : MonoBehaviour
    {
        [Header("Event Channels — Raise")]
        [SerializeField] private GameEventSO _onShiftEnded;

        // ── State ──────────────────────────────────────────────────────────────

        private bool _isVisible;
        private int  _oreLostDisplay;
        private int  _debtDisplay;
        private int  _revivalFee;

        // ── Cached GUI styles ──────────────────────────────────────────────────

        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start() => BuildStyles();

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Makes the death screen visible with the given penalty data.
        /// Called by <see cref="DeepShift.Death.DeathPenaltySystem"/> after applying penalties.
        /// </summary>
        public void Show(int oreLost, int currentDebt)
        {
            _oreLostDisplay = oreLost;
            _debtDisplay    = currentDebt;
            _revivalFee     = currentDebt; // store to display the fee line (debt may have been higher before)
            _isVisible      = true;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_isVisible) return;
            if (_headerStyle == null) BuildStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            // Full-screen dark overlay
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float cx = sw * 0.5f;
            float cy = sh * 0.5f;

            // ── "SHIFT TERMINATED" header ──────────────────────────────────────
            GUI.Label(new Rect(cx - 900f, cy - 405f, 1800f, 180f),
                "SHIFT TERMINATED", _headerStyle);

            // ── Penalty lines ──────────────────────────────────────────────────
            GUI.Label(new Rect(cx - 720f, cy - 157f, 1440f, 81f),
                $"Ore lost:  {_oreLostDisplay} units", _bodyStyle);

            GUI.Label(new Rect(cx - 720f, cy - 54f, 1440f, 81f),
                $"VEKTRA MEDICAL REVIVAL FEE: {_revivalFee} credits added to your account.",
                _bodyStyle);

            GUI.Label(new Rect(cx - 720f, cy + 49f, 1440f, 81f),
                $"Current total debt:  {_debtDisplay} credits", _bodyStyle);

            // ── Begin New Shift button ─────────────────────────────────────────
            // _buttonStyle uses GUI.skin and must be built inside OnGUI on first use
            if (_buttonStyle == null) BuildButtonStyle();
            if (GUI.Button(new Rect(cx - 247f, cy + 225f, 495f, 117f),
                    "Return to Surface", _buttonStyle))
                ReturnToSurface();
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void ReturnToSurface()
        {
            _isVisible = false;

            // SceneController listens to ShiftComplete and loads the SurfaceCamp scene.
            // The Mine scene is destroyed on load — run state (HP, inventory) resets automatically.
            _onShiftEnded?.Raise();
        }

        private void BuildStyles()
        {
            _headerStyle = new GUIStyle
            {
                fontSize  = 117,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _headerStyle.normal.textColor = new Color(1f, 0.15f, 0.15f); // danger red

            _bodyStyle = new GUIStyle
            {
                fontSize  = 49,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
            };
            _bodyStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f); // light grey
        }

        // Called inside OnGUI — GUI.skin is only valid there
        private void BuildButtonStyle()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 40,
                fontStyle = FontStyle.Bold,
            };
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor  = new Color(1f, 0.65f, 0.1f); // VEKTRA orange on hover
        }
    }
}
