using UnityEngine;
using DeepShift.Core;
using DeepShift.Economy;

namespace DeepShift.UI
{
    /// <summary>
    /// OnGUI surface camp screen shown between shifts.
    /// Displays banked ore credits, outstanding debt, and a "Begin Shift" button
    /// that raises <see cref="_onShiftStarted"/> to kick off a new mine run.
    ///
    /// Subscribes to <see cref="_onOreCreditsChanged"/> and <see cref="_onDebtChanged"/>
    /// for live economy updates. Reads current values from <see cref="EconomyManager"/>
    /// on Start in case events fired before this scene was loaded.
    ///
    /// Attach to a GameObject in the SurfaceCamp scene. Wire all event SO references
    /// in the Inspector.
    /// </summary>
    public class SurfaceCampUI : MonoBehaviour
    {
        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO_Int _onOreCreditsChanged;
        [SerializeField] private GameEventSO_Int _onDebtChanged;

        [Header("Event Channels — Raise")]
        [SerializeField] private GameEventSO _onShiftStarted;

        // ── Cached economy state ───────────────────────────────────────────────

        private int _credits;
        private int _debt;

        // ── Nested listeners ───────────────────────────────────────────────────

        private readonly CreditsListener _creditsListener;
        private readonly DebtListener    _debtListener;

        private sealed class CreditsListener : IGameEventListener<int>
        {
            private readonly SurfaceCampUI _ui;
            public CreditsListener(SurfaceCampUI ui) => _ui = ui;
            public void OnEventRaised(int value) => _ui._credits = value;
        }

        private sealed class DebtListener : IGameEventListener<int>
        {
            private readonly SurfaceCampUI _ui;
            public DebtListener(SurfaceCampUI ui) => _ui = ui;
            public void OnEventRaised(int value) => _ui._debt = value;
        }

        // ── GUI styles ────────────────────────────────────────────────────────

        private GUIStyle _titleStyle;
        private GUIStyle _creditsStyle;
        private GUIStyle _debtStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;

        // ── Constructor ───────────────────────────────────────────────────────

        public SurfaceCampUI()
        {
            _creditsListener = new CreditsListener(this);
            _debtListener    = new DebtListener(this);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            // Sync immediately from EconomyManager — events from a prior scene may have
            // already fired before this component existed.
            if (EconomyManager.Instance != null)
            {
                _credits = EconomyManager.Instance.OreCredits;
                _debt    = EconomyManager.Instance.DebtTokens;
            }

            _titleStyle = new GUIStyle
            {
                fontSize  = 64,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _titleStyle.normal.textColor = new Color(1f, 0.65f, 0.1f); // VEKTRA orange

            _creditsStyle = new GUIStyle
            {
                fontSize  = 42,
                alignment = TextAnchor.MiddleCenter,
            };
            _creditsStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);

            _debtStyle = new GUIStyle
            {
                fontSize  = 42,
                alignment = TextAnchor.MiddleCenter,
            };
            _debtStyle.normal.textColor = new Color(0.9f, 0.25f, 0.25f);

            _hintStyle = new GUIStyle
            {
                fontSize  = 28,
                alignment = TextAnchor.MiddleCenter,
            };
            _hintStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
        }

        private void OnEnable()
        {
            _onOreCreditsChanged?.RegisterListener(_creditsListener);
            _onDebtChanged?.RegisterListener(_debtListener);
        }

        private void OnDisable()
        {
            _onOreCreditsChanged?.UnregisterListener(_creditsListener);
            _onDebtChanged?.UnregisterListener(_debtListener);
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_titleStyle == null) return;

            if (_buttonStyle == null) BuildButtonStyle();

            float sw = Screen.width;
            float sh = Screen.height;
            float cx = sw * 0.5f;
            float cy = sh * 0.5f;

            // ── Title ──────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx - 600f, cy - 280f, 1200f, 90f),
                "VEKTRA MINING CORP — SURFACE OPERATIONS", _titleStyle);

            // ── Divider hint ───────────────────────────────────────────────────
            GUI.Label(new Rect(cx - 400f, cy - 170f, 800f, 40f),
                "All extracted ore has been processed and credited to your account.",
                _hintStyle);

            // ── Credits ────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx - 400f, cy - 100f, 800f, 60f),
                $"Ore Credits:  {_credits:N0}  ¢", _creditsStyle);

            // ── Debt (only visible when non-zero) ──────────────────────────────
            if (_debt > 0)
                GUI.Label(new Rect(cx - 400f, cy - 30f, 800f, 60f),
                    $"Outstanding Debt:  {_debt:N0}", _debtStyle);

            // ── Net balance line ───────────────────────────────────────────────
            int net = _credits - _debt;
            _hintStyle.normal.textColor = net >= 0
                ? new Color(0.4f, 0.85f, 0.4f)
                : new Color(0.9f, 0.25f, 0.25f);
            GUI.Label(new Rect(cx - 400f, cy + 50f, 800f, 40f),
                $"Net balance:  {net:N0}", _hintStyle);

            // ── Begin Shift button ─────────────────────────────────────────────
            if (GUI.Button(new Rect(cx - 210f, cy + 130f, 420f, 90f), "Begin Shift", _buttonStyle))
                _onShiftStarted?.Raise();

            // ── Placeholder vendor hint ────────────────────────────────────────
            _hintStyle.normal.textColor = new Color(0.35f, 0.35f, 0.35f);
            GUI.Label(new Rect(cx - 400f, cy + 240f, 800f, 40f),
                "[ Vendors, upgrades and contracts — coming soon ]", _hintStyle);
        }

        // Called inside OnGUI so GUI.skin is valid
        private void BuildButtonStyle()
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 36,
                fontStyle = FontStyle.Bold,
            };
            _buttonStyle.normal.textColor = Color.white;
            _buttonStyle.hover.textColor  = new Color(1f, 0.65f, 0.1f);
        }
    }
}
