using UnityEngine;

namespace DeepShift.Mining
{
    [CreateAssetMenu(menuName = "DeepShift/Tiles/TileData", fileName = "TileData")]
    public class TileDataSO : ScriptableObject
    {
        /// <summary>Internal identifier for this tile type (e.g. "RockIron", "Wall").</summary>
        public string tileName;

        /// <summary>Whether a player's drill can break this tile.</summary>
        public bool isDestructible;

        /// <summary>Number of drill hits required to fully destroy this tile. Zero for indestructible tiles.</summary>
        public int drillHitsRequired;

        /// <summary>The ore yielded when this tile is destroyed. Null means the tile contains no ore.</summary>
        public OreDataSO containedOre;

        /// <summary>
        /// Probability (0–1) that destroying this tile triggers a structural collapse event.
        /// 0 = never collapses; 1 = always collapses.
        /// </summary>
        [Range(0f, 1f)]
        public float collapseRisk;

        /// <summary>Solid colour used to tint this tile during prototyping before final sprites are in place.</summary>
        public Color debugColor;
    }
}
