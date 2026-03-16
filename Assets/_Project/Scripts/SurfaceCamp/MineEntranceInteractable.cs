using UnityEngine;
using DeepShift.Core;

namespace DeepShift.SurfaceCamp
{
    /// <summary>
    /// Place on any GameObject in the SurfaceCamp scene to mark it as the mine entrance.
    /// When the player presses E within interact range, raises <see cref="_onShiftStarted"/>
    /// which causes <see cref="DeepShift.Core.SceneController"/> to load the Mine scene.
    ///
    /// Setup:
    /// 1. Attach to any SurfaceCamp GameObject you want to be the entrance (e.g. a door sprite).
    /// 2. Add a Collider2D on the same GameObject — set isTrigger = false.
    /// 3. Set the GameObject's Layer to "Interactable" (same layer PlayerController watches).
    /// 4. Wire <see cref="_onShiftStarted"/> to ShiftStarted.asset in the Inspector.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MineEntranceInteractable : MonoBehaviour, IInteractable
    {
        [Header("Event Channels — Raise")]
        [SerializeField] private GameEventSO _onShiftStarted;

        /// <summary>Raises ShiftStarted, causing SceneController to load the Mine scene.</summary>
        public void Interact()
        {
            _onShiftStarted?.Raise();
        }
    }
}
