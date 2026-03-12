using UnityEngine;
using UnityEngine.InputSystem;
using DeepShift.Core;
using DeepShift.Mining;
using DeepShift.Weapons;

namespace DeepShift.Player
{
    /// <summary>
    /// Manages the 2-slot hotbar (1 = Drill, 2 = Bolt Pistol).
    /// Enables the active slot's component and disables the inactive one,
    /// which in turn enables/disables that slot's InputAction bindings.
    /// Attach to the same GameObject as <see cref="DrillController"/> and
    /// <see cref="RangedWeaponController"/>.
    /// </summary>
    public class HotbarController : MonoBehaviour
    {
        [Header("Weapon Components")]
        [SerializeField] private DrillController       _drill;
        [SerializeField] private RangedWeaponController _ranged;

        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Int _onWeaponSlotChanged;

        // ── Private state ──────────────────────────────────────────────────────

        private InputAction _slot1Action;
        private InputAction _slot2Action;
        private int         _activeSlot = -1; // force initial activation in OnEnable

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _slot1Action = new InputAction("Slot1", InputActionType.Button);
            _slot1Action.AddBinding("<Keyboard>/1");

            _slot2Action = new InputAction("Slot2", InputActionType.Button);
            _slot2Action.AddBinding("<Keyboard>/2");
        }

        private void OnEnable()
        {
            _slot1Action.Enable();
            _slot2Action.Enable();
            ActivateSlot(0);
        }

        private void OnDisable()
        {
            _slot1Action.Disable();
            _slot2Action.Disable();
        }

        private void Update()
        {
            if (_slot1Action.WasPressedThisFrame()) ActivateSlot(0);
            if (_slot2Action.WasPressedThisFrame()) ActivateSlot(1);
        }

        // ── Slot activation ───────────────────────────────────────────────────

        /// <summary>
        /// Switches to <paramref name="slot"/> (0 = Drill, 1 = Bolt Pistol).
        /// Enables the newly active component and disables the other, which
        /// automatically propagates to each component's InputAction bindings.
        /// </summary>
        private void ActivateSlot(int slot)
        {
            if (slot == _activeSlot) return;

            _activeSlot = slot;

            if (_drill  != null) _drill.enabled  = (slot == 0);
            if (_ranged != null) _ranged.enabled = (slot == 1);

            _onWeaponSlotChanged?.Raise(slot);
        }
    }
}
