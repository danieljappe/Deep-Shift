using System.Collections.Generic;
using UnityEngine;

namespace DeepShift.Mining
{
    /// <summary>
    /// Tracks the ore the player is currently carrying during a shift.
    /// This is ephemeral run state — <see cref="ClearInventory"/> must be called on death
    /// and on extraction (contents transferred to economy before clearing).
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        private readonly List<OreDataSO> _carriedOre = new();

        /// <summary>Read-only view of all ore currently carried.</summary>
        public IReadOnlyList<OreDataSO> CarriedOre => _carriedOre;

        /// <summary>Number of ore pieces currently carried.</summary>
        public int Count => _carriedOre.Count;

        /// <summary>Adds one piece of ore to the carried inventory.</summary>
        public void AddOre(OreDataSO ore)
        {
            if (ore == null) return;
            _carriedOre.Add(ore);
        }

        /// <summary>
        /// Empties the carried inventory.
        /// TODO: call on player death — all carried ore is lost (run state, not persistent).
        /// TODO: call on successful hoist extraction — after transferring credits to EconomyManager.
        /// </summary>
        public void ClearInventory() => _carriedOre.Clear();
    }
}
