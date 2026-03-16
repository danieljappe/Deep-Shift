using UnityEngine;
using DeepShift.Core;
using DeepShift.Economy;

namespace DeepShift.UI
{
    /// <summary>
    /// Displays banked ore credits and current debt in the top-right corner.
    /// Subscribes to <see cref="_onOreCreditsChanged"/> and <see cref="_onDebtChanged"/>
    /// to update without polling. Reads the current values from <see cref="EconomyManager"/>
    /// on Start so the display is correct even if events fired before this component enabled.
    /// Attach to any active GameObject in the Mine scene and wire both event references.
    /// </summary>
    public class EconomyHUD : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Int _onOreCreditsChanged;
        [SerializeField] private GameEventSO_Int _onDebtChanged;

        // ── Cached display values ──────────────────────────────────────────────

        private int _credits;
        private int _debt;

        // ── Nested listeners (avoids implementing IGameEventListener<int> twice) ─

        private readonly CreditsListener _creditsListener;
        private readonly DebtListener    _debtListener;

        private class CreditsListener : IGameEventListener<int>
        {
            private readonly EconomyHUD _hud;
            public CreditsListener(EconomyHUD hud) => _hud = hud;
            public void OnEventRaised(int value) => _hud._credits = value;
        }

        private class DebtListener : IGameEventListener<int>
        {
            private readonly EconomyHUD _hud;
            public DebtListener(EconomyHUD hud) => _hud = hud;
            public void OnEventRaised(int value) => _hud._debt = value;
        }

        // ── Styles ────────────────────────────────────────────────────────────

        private GUIStyle _labelStyle;
        private GUIStyle _debtStyle;

        // ── Constructor ───────────────────────────────────────────────────────

        public EconomyHUD()
        {
            _creditsListener = new CreditsListener(this);
            _debtListener    = new DebtListener(this);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            // Initialise from current EconomyManager state in case events already fired
            if (EconomyManager.Instance != null)
            {
                _credits = EconomyManager.Instance.OreCredits;
                _debt    = EconomyManager.Instance.DebtTokens;
            }

            _labelStyle = new GUIStyle
            {
                fontSize  = 33,
                alignment = TextAnchor.UpperRight,
            };
            _labelStyle.normal.textColor = new Color(1f, 0.65f, 0.1f); // VEKTRA orange

            _debtStyle = new GUIStyle
            {
                fontSize  = 33,
                alignment = TextAnchor.UpperRight,
            };
            _debtStyle.normal.textColor = new Color(0.9f, 0.25f, 0.25f); // red
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

            const float PanelWidth = 400f;
            const float LineHeight = 48f;
            const float MarginRight = 45f;
            const float MarginTop   = 45f;

            float x = Screen.width - PanelWidth - MarginRight;
            float y = MarginTop;

            GUI.Label(new Rect(x, y, PanelWidth, LineHeight),
                $"CREDITS  {_credits:N0} ¢", _labelStyle);

            y += LineHeight;

            if (_debt > 0)
                GUI.Label(new Rect(x, y, PanelWidth, LineHeight),
                    $"DEBT  {_debt:N0}", _debtStyle);
        }
    }
}
