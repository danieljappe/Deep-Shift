using UnityEngine;
using DeepShift.Core;
using DeepShift.Economy;

namespace DeepShift.UI
{
    /// <summary>
    /// Compact corner HUD for the SurfaceCamp scene.
    /// Shows banked ore credits and outstanding debt in the top-left corner.
    ///
    /// "Begin Shift" is handled by <see cref="DeepShift.SurfaceCamp.MineEntranceInteractable"/>
    /// — the player walks up to the mine entrance and presses E.
    ///
    /// Attach to any active GameObject in the SurfaceCamp scene.
    /// Wire <see cref="_onOreCreditsChanged"/> and <see cref="_onDebtChanged"/> in the Inspector.
    /// </summary>
    public class SurfaceCampUI : MonoBehaviour
    {
        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO_Int _onOreCreditsChanged;
        [SerializeField] private GameEventSO_Int _onDebtChanged;

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

        private GUIStyle _labelStyle;
        private GUIStyle _debtStyle;
        private GUIStyle _hintStyle;

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

            _labelStyle = new GUIStyle
            {
                fontSize  = 28,
                fontStyle = FontStyle.Bold,
            };
            _labelStyle.normal.textColor = new Color(1f, 0.75f, 0.2f); // VEKTRA orange

            _debtStyle = new GUIStyle
            {
                fontSize  = 28,
                fontStyle = FontStyle.Bold,
            };
            _debtStyle.normal.textColor = new Color(0.9f, 0.25f, 0.25f); // red

            _hintStyle = new GUIStyle
            {
                fontSize  = 22,
            };
            _hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
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
            if (_labelStyle == null) return;

            const float pad = 16f;
            const float w   = 320f;
            const float h   = 34f;

            GUI.Label(new Rect(pad, pad,         w, h), $"Credits:  {_credits:N0} ¢", _labelStyle);

            if (_debt > 0)
                GUI.Label(new Rect(pad, pad + h, w, h), $"Debt:  {_debt:N0}", _debtStyle);

            // Interaction hint — rendered near the bottom of the screen
            GUI.Label(new Rect(pad, Screen.height - 50f, 400f, 30f),
                "Approach the mine entrance and press [E] to begin a shift.", _hintStyle);
        }
    }
}
