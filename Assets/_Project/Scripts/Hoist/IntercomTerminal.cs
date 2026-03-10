using UnityEngine;
using DeepShift.Core;

namespace DeepShift.Hoist
{
    public class IntercomTerminal : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameEventSO _onIntercomActivated;

        private bool _activated;

        /// <summary>Activates the intercom terminal, raising the IntercomActivated event once.</summary>
        public void Interact()
        {
            if (_activated) return;
            _activated = true;
            _onIntercomActivated?.Raise();
            Debug.Log("[IntercomTerminal] Activated.");
        }
    }
}
