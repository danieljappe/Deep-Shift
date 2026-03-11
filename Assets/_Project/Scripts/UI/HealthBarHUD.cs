using UnityEngine;
using DeepShift.Core;

namespace DeepShift.UI
{
    /// <summary>
    /// Draws a simple fill-bar health indicator in the top-left corner of the screen.
    /// Subscribes to <see cref="_onPlayerHealthChanged"/> (float, 0–1) and updates
    /// the bar fill each time the event fires.
    /// Attach to any active GameObject in the Mine scene and wire the event reference.
    /// </summary>
    public class HealthBarHUD : MonoBehaviour, IGameEventListener<float>
    {
        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Float _onPlayerHealthChanged;

        [Header("Layout")]
        [SerializeField] private float _barX      = 20f;
        [SerializeField] private float _barY      = 20f;
        [SerializeField] private float _barWidth  = 200f;
        [SerializeField] private float _barHeight = 20f;

        // ── State ──────────────────────────────────────────────────────────────

        private float    _healthRatio = 1f;
        private GUIStyle _labelStyle;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            _labelStyle = new GUIStyle
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleLeft,
            };
            _labelStyle.normal.textColor = Color.white;
        }

        private void OnEnable()  => _onPlayerHealthChanged?.RegisterListener(this);
        private void OnDisable() => _onPlayerHealthChanged?.UnregisterListener(this);

        // ── IGameEventListener<float> ─────────────────────────────────────────

        /// <summary>Updates the displayed health ratio when PlayerHealthChanged fires.</summary>
        public void OnEventRaised(float value) => _healthRatio = Mathf.Clamp01(value);

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_labelStyle == null) return;

            // Background
            GUI.color = new Color(0.15f, 0.15f, 0.15f);
            GUI.DrawTexture(new Rect(_barX, _barY, _barWidth, _barHeight), Texture2D.whiteTexture);

            // Fill — red at low HP, green at full HP
            GUI.color = Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(0.1f, 0.85f, 0.2f), _healthRatio);
            GUI.DrawTexture(new Rect(_barX, _barY, _barWidth * _healthRatio, _barHeight), Texture2D.whiteTexture);

            // Label
            GUI.color = Color.white;
            int hpPercent = Mathf.RoundToInt(_healthRatio * 100f);
            GUI.Label(new Rect(_barX, _barY + _barHeight + 2f, _barWidth, 18f),
                $"HP  {hpPercent}%", _labelStyle);
        }
    }
}
