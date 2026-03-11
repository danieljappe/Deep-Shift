using UnityEngine;
using DeepShift.Core;
using DeepShift.UI;

namespace DeepShift.Mining
{
    /// <summary>
    /// World-space ore pickup spawned when an ore tile is destroyed.
    /// Auto-collects when the player's collider overlaps the trigger.
    /// Adds the ore to the player's <see cref="PlayerInventory"/>, raises the
    /// <c>OrePickedUp</c> event with the credit value, shows a floating text popup,
    /// then destroys itself.
    /// </summary>
    public class OrePickup : MonoBehaviour
    {
        private OreDataSO      _ore;
        private GameEventSO_Int _onOrePickedUp;

        /// <summary>
        /// Sets the ore data and event channel reference. Called by <see cref="DrillController"/>
        /// immediately after instantiation.
        /// </summary>
        public void Initialize(OreDataSO ore, GameEventSO_Int onOrePickedUp)
        {
            _ore           = ore;
            _onOrePickedUp = onOrePickedUp;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_ore == null) return;

            var inventory = other.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            inventory.AddOre(_ore);
            _onOrePickedUp?.Raise(_ore.creditValue);

            FloatingText.Spawn(transform.position, _ore.displayName);

            Destroy(gameObject);
        }
    }
}
