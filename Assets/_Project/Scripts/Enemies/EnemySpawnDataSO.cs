using UnityEngine;

namespace DeepShift.Enemies
{
    /// <summary>
    /// Data asset describing one spawnable enemy type for the threat budget system.
    /// Read-only at runtime — never mutate during a shift.
    /// </summary>
    [CreateAssetMenu(menuName = "DeepShift/Enemies/EnemySpawnData", fileName = "EnemySpawnData")]
    public class EnemySpawnDataSO : ScriptableObject
    {
        /// <summary>The prefab to instantiate when this enemy is selected for spawning.</summary>
        public GameObject prefab;

        /// <summary>Points deducted from the floor's threat budget each time this enemy spawns.</summary>
        public int threatCost = 1;

        /// <summary>Minimum floor depth at which this enemy can appear.</summary>
        public int minFloor = 1;

        /// <summary>Maximum floor depth at which this enemy can appear (inclusive).</summary>
        public int maxFloor = 5;

        /// <summary>
        /// When true the spawner will only place this enemy on an open tile
        /// that is directly adjacent (4-directional) to at least one solid wall tile.
        /// </summary>
        public bool requiresWallPlacement = true;

        /// <summary>
        /// Maximum number of this enemy type per floor.
        /// 0 means unlimited — only the threat budget caps the count.
        /// </summary>
        public int maxPerFloor = 0;
    }
}
