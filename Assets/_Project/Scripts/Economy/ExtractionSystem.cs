using UnityEngine;
using DeepShift.Core;
using DeepShift.Mining;

namespace DeepShift.Economy
{
    /// <summary>
    /// Converts the player's carried ore into banked credits and saves the economy.
    /// Called explicitly by <see cref="DeepShift.UI.HoistChoiceUI"/> when the player
    /// chooses "Extract to Surface" — not event-driven, so the call order is guaranteed.
    /// Attach to any GameObject in the Mine scene.
    /// </summary>
    public class ExtractionSystem : MonoBehaviour
    {
        private PlayerInventory _inventory;

        private void Start()
        {
            _inventory = FindFirstObjectByType<PlayerInventory>();
        }

        /// <summary>
        /// Sums the credit value of all carried ore, adds it to <see cref="EconomyManager"/>,
        /// clears the player's inventory, and saves the economy to disk.
        /// Call this before transitioning to the SurfaceCamp scene.
        /// </summary>
        public void BankOreAndSave()
        {
            if (_inventory != null)
            {
                int totalCredits = 0;
                foreach (var ore in _inventory.CarriedOre)
                    if (ore != null) totalCredits += ore.creditValue;

                if (totalCredits > 0)
                    EconomyManager.Instance?.AddOreCredits(totalCredits);

                _inventory.ClearInventory();
            }

            EconomyManager.Instance?.SaveToJson();
        }
    }
}
