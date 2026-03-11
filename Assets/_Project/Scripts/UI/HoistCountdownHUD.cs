using UnityEngine;
using DeepShift.Core;

namespace DeepShift.UI
{
    /// <summary>
    /// Prototype HUD overlay for the hoist extraction flow. Displays:
    /// <list type="bullet">
    ///   <item>A prominent centre-screen countdown while the hoist is active.</item>
    ///   <item>A timed abort message when the countdown is cancelled.</item>
    ///   <item>A brief "DESCENDING — Floor X" banner on successful extraction.</item>
    /// </list>
    /// Subscribes to <see cref="_onHoistCountdownTick"/> (float), <see cref="_onHoistCancelled"/>,
    /// and <see cref="_onPlayerFloorChanged"/> (int) via the event bus.
    /// Attach to any active GameObject in the Mine scene and wire the three event SO references.
    /// </summary>
    public class HoistCountdownHUD : MonoBehaviour,
        IGameEventListener,
        IGameEventListener<float>,
        IGameEventListener<int>
    {
        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Float _onHoistCountdownTick;
        [SerializeField] private GameEventSO       _onHoistCancelled;
        [SerializeField] private GameEventSO_Int   _onPlayerFloorChanged;

        [Header("Display Durations")]
        [SerializeField] private float _abortFlashDuration    = 3f;
        [SerializeField] private float _descendingDuration    = 3f;

        // ── Private state ──────────────────────────────────────────────────────

        private float _countdownRemaining = -1f; // negative = not active
        private float _abortTimer;
        private float _descendingTimer;
        private int   _descendingFloor;

        // ── Cached GUI styles (built once in Start) ────────────────────────────

        private GUIStyle _countdownStyle;
        private GUIStyle _abortStyle;
        private GUIStyle _descendingStyle;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            _countdownStyle = new GUIStyle
            {
                fontSize  = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _countdownStyle.normal.textColor = new Color(0f, 1f, 0.4f); // green

            _abortStyle = new GUIStyle
            {
                fontSize  = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _abortStyle.normal.textColor = new Color(1f, 0.2f, 0.2f); // red

            _descendingStyle = new GUIStyle
            {
                fontSize  = 36,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _descendingStyle.normal.textColor = new Color(1f, 0.65f, 0.1f); // VEKTRA orange
        }

        private void OnEnable()
        {
            _onHoistCountdownTick?.RegisterListener(this);
            _onHoistCancelled?.RegisterListener(this);
            _onPlayerFloorChanged?.RegisterListener(this);
        }

        private void OnDisable()
        {
            _onHoistCountdownTick?.UnregisterListener(this);
            _onHoistCancelled?.UnregisterListener(this);
            _onPlayerFloorChanged?.UnregisterListener(this);
        }

        private void Update()
        {
            if (_abortTimer > 0f)    _abortTimer    -= Time.deltaTime;
            if (_descendingTimer > 0f) _descendingTimer -= Time.deltaTime;
        }

        // ── IGameEventListener (HoistCancelled) ───────────────────────────────

        /// <summary>Clears the countdown and starts the abort flash timer.</summary>
        public void OnEventRaised()
        {
            _countdownRemaining = -1f;
            _abortTimer = _abortFlashDuration;
        }

        // ── IGameEventListener<float> (HoistCountdownTick) ────────────────────

        /// <summary>Updates the displayed countdown with the latest remaining seconds.</summary>
        public void OnEventRaised(float value) => _countdownRemaining = value;

        // ── IGameEventListener<int> (PlayerFloorChanged) ──────────────────────

        /// <summary>Records the new floor number and begins the descending banner timer.</summary>
        public void OnEventRaised(int value)
        {
            _countdownRemaining = -1f;
            _descendingFloor  = value;
            _descendingTimer  = _descendingDuration;
        }

        // ── OnGUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_countdownStyle == null) return;

            float sw = Screen.width;
            float sh = Screen.height;

            // Countdown — green, centre screen, large
            if (_countdownRemaining > 0f)
            {
                string text = $"HOIST ENGAGED\n{_countdownRemaining:F1}s";
                GUI.Label(new Rect(sw * 0.5f - 300f, sh * 0.3f, 600f, 120f), text, _countdownStyle);
            }

            // Abort flash — red, centre screen
            if (_abortTimer > 0f)
            {
                GUI.Label(
                    new Rect(sw * 0.5f - 400f, sh * 0.45f, 800f, 60f),
                    "HOIST ABORTED — Maintain position at terminal.",
                    _abortStyle);
            }

            // Descending banner — VEKTRA orange, centre screen
            if (_descendingTimer > 0f)
            {
                GUI.Label(
                    new Rect(sw * 0.5f - 300f, sh * 0.5f - 30f, 600f, 80f),
                    $"DESCENDING — Floor {_descendingFloor}",
                    _descendingStyle);
            }
        }
    }
}
