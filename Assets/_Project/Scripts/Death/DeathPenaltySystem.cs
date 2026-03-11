using UnityEngine;
using DeepShift.Core;
using DeepShift.Economy;
using DeepShift.Mining;
using DeepShift.UI;

namespace DeepShift.Death
{
    /// <summary>
    /// Responds to <see cref="_onPlayerDied"/> and applies death penalties:
    /// <list type="bullet">
    ///   <item>Removes 50% of the player's carried ore (rounded down).</item>
    ///   <item>Adds the VEKTRA medical revival fee (<see cref="_revivalFee"/>) to <see cref="EconomyManager"/> debt.</item>
    /// </list>
    /// After applying penalties, calls <see cref="DeathScreenUI.Show"/> with the result data.
    /// Attach to any persistent GameObject in the Mine scene. Wire all references in the Inspector.
    /// </summary>
    public class DeathPenaltySystem : MonoBehaviour, IGameEventListener
    {
        [Header("Settings")]
        [SerializeField] private int _revivalFee = 150;

        [Header("References")]
        [SerializeField] private DeathScreenUI _deathScreenUI;

        [Header("Event Channels — Subscribe")]
        [SerializeField] private GameEventSO _onPlayerDied;

        // ── Cached scene reference ─────────────────────────────────────────────

        private PlayerInventory _inventory;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            _inventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void OnEnable()  => _onPlayerDied?.RegisterListener(this);
        private void OnDisable() => _onPlayerDied?.UnregisterListener(this);

        // ── IGameEventListener (PlayerDied) ───────────────────────────────────

        /// <summary>
        /// Applies death penalties when the player dies:
        /// removes 50% of carried ore, adds revival fee debt, then shows the death screen.
        /// </summary>
        public void OnEventRaised()
        {
            int totalOre = _inventory != null ? _inventory.Count : 0;
            int oreLost  = totalOre / 2; // integer division = round down

            // Remove 50% of carried ore
            if (_inventory != null && oreLost > 0)
                _inventory.RemoveOre(oreLost);

            // Add VEKTRA medical revival fee to debt
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.AddDebt(_revivalFee);

            int currentDebt = EconomyManager.Instance != null
                ? EconomyManager.Instance.DebtTokens
                : _revivalFee;

            // TODO: Phase 2 — apply gear durability damage on death per GearDataSO

            Debug.Log($"[DeathPenaltySystem] Ore lost: {oreLost}, debt added: {_revivalFee}, total debt: {currentDebt}");

            _deathScreenUI?.Show(oreLost, currentDebt);
        }
    }
}
