using UnityEngine;
using DeepShift.Core;
using DeepShift.Mining;

namespace DeepShift.Enemies
{
    /// <summary>
    /// Translates the payload-less <c>TileDestroyed</c> event into a positioned
    /// <see cref="GameEventSO_Vector2Int"/> DrillImpact event that enemy scripts can subscribe to.
    ///
    /// Place this component on the same GameObject as <see cref="MineGrid"/>.
    /// Wire <c>_onTileDestroyed</c> to the TileDestroyed SO and <c>_onDrillImpact</c>
    /// to the DrillImpact SO in the Inspector.
    /// </summary>
    public class DrillVibrationBroadcaster : MonoBehaviour, IGameEventListener
    {
        [Header("Event Channels")]
        [SerializeField] private GameEventSO _onTileDestroyed;
        [SerializeField] private GameEventSO_Vector2Int _onDrillImpact;

        [Header("References")]
        [SerializeField] private MineGrid _mineGrid;

        private void OnEnable()  => _onTileDestroyed?.RegisterListener(this);
        private void OnDisable() => _onTileDestroyed?.UnregisterListener(this);

        /// <summary>
        /// Called by the event bus when a tile is destroyed.
        /// Reads the last drilled position from <see cref="MineGrid"/> and raises DrillImpact.
        /// </summary>
        public void OnEventRaised()
        {
            if (_mineGrid == null || _onDrillImpact == null) return;
            _onDrillImpact.Raise(_mineGrid.LastDrilledPosition);
        }
    }
}
