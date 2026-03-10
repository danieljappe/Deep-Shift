using UnityEngine;
using DeepShift.Core;

namespace DeepShift.Hoist
{
    public class HoistTerminal : MonoBehaviour, IInteractable, IGameEventListener
    {
        [SerializeField] private GameEventSO _onIntercomActivated; // subscribe
        [SerializeField] private GameEventSO _onHoistCalled;       // raise

        private bool _unlocked;

        private void OnEnable()  => _onIntercomActivated?.RegisterListener(this);
        private void OnDisable() => _onIntercomActivated?.UnregisterListener(this);

        /// <summary>Called by the event bus when IntercomActivated fires — unlocks the hoist.</summary>
        public void OnEventRaised() { _unlocked = true; }

        /// <summary>
        /// Calls the hoist if the floor intercom has been activated.
        /// Prints a hint if the intercom has not yet been found.
        /// </summary>
        public void Interact()
        {
            if (!_unlocked)
            {
                Debug.Log("[HoistTerminal] HOIST OFFLINE — Locate and activate the floor intercom terminal.");
                return;
            }
            _onHoistCalled?.Raise();
            Debug.Log("[HoistTerminal] Hoist called. Floor transition stub."); // TODO Phase 3
        }
    }
}
