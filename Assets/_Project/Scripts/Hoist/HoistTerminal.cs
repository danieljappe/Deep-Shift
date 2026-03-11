using UnityEngine;
using DeepShift.Core;
using DeepShift.Mining;

namespace DeepShift.Hoist
{
    /// <summary>
    /// Hoist terminal that initiates an 8-second extraction countdown once the floor
    /// intercom has been activated. The player must remain within <see cref="_hoistStayRadius"/>
    /// for the full duration — leaving cancels the countdown and raises
    /// <see cref="_onHoistCancelled"/>. On completion raises <see cref="_onHoistExtracted"/>.
    /// Subscribes to <see cref="_onIntercomActivated"/> to unlock itself.
    /// </summary>
    public class HoistTerminal : MonoBehaviour, IInteractable, IGameEventListener
    {
        [Header("Settings")]
        [SerializeField] private float _countdownDuration = 8f;
        [SerializeField] private float _hoistStayRadius   = 1.5f;

        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO _onIntercomActivated;

        [Header("Event Channels — Raise")]
        [SerializeField] private GameEventSO       _onHoistCalled;
        [SerializeField] private GameEventSO_Float _onHoistCountdownTick;
        [SerializeField] private GameEventSO       _onHoistExtracted;
        [SerializeField] private GameEventSO       _onHoistCancelled;

        // ── Private state ──────────────────────────────────────────────────────

        private bool      _unlocked;
        private bool      _countdownActive;
        private float     _countdownRemaining;
        private Transform _playerTransform;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()  => _onIntercomActivated?.RegisterListener(this);
        private void OnDisable() => _onIntercomActivated?.UnregisterListener(this);

        private void Update()
        {
            if (!_countdownActive) return;

            // Cancel if the player has moved out of range
            if (_playerTransform == null ||
                Vector2.Distance(transform.position, _playerTransform.position) > _hoistStayRadius)
            {
                CancelCountdown();
                return;
            }

            _countdownRemaining -= Time.deltaTime;
            _onHoistCountdownTick?.Raise(Mathf.Max(0f, _countdownRemaining));

            if (_countdownRemaining <= 0f)
            {
                _countdownActive = false;
                _onHoistExtracted?.Raise();
                Debug.Log("[HoistTerminal] Extraction complete.");
            }
        }

        // ── IGameEventListener ────────────────────────────────────────────────

        /// <summary>Called by the event bus when IntercomActivated fires — unlocks the hoist.</summary>
        public void OnEventRaised() { _unlocked = true; }

        // ── IInteractable ─────────────────────────────────────────────────────

        /// <summary>
        /// Begins the extraction countdown if the floor intercom has been activated and
        /// no countdown is already in progress. Prints a hint if the intercom has not yet
        /// been found.
        /// </summary>
        public void Interact()
        {
            if (!_unlocked)
            {
                Debug.Log("[HoistTerminal] HOIST OFFLINE — Locate and activate the floor intercom terminal.");
                return;
            }

            if (_countdownActive) return; // already counting down

            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            _playerTransform    = player.transform;
            _countdownActive    = true;
            _countdownRemaining = _countdownDuration;
            _onHoistCalled?.Raise();
            Debug.Log("[HoistTerminal] Hoist countdown started.");
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void CancelCountdown()
        {
            _countdownActive    = false;
            _countdownRemaining = 0f;
            _playerTransform    = null;
            _onHoistCancelled?.Raise();
            Debug.Log("[HoistTerminal] Countdown cancelled — player left range.");
        }
    }
}
