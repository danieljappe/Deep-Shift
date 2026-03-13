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

        /// <summary>True if the player can walk through this tile (e.g. Floor, GasPocket).</summary>
        public bool isWalkable;

        /// <summary>Solid colour used to tint this tile during prototyping before final sprites are in place.</summary>
        public Color debugColor;

        /// <summary>
        /// Sprite variants for this tile. Index 0 is the base sprite; indices 1+ are rare variants.
        /// If empty, falls back to debugColor on a plain quad.
        /// </summary>
        public Sprite[] sprites;

        /// <summary>
        /// Number of sprites (from index 0) treated as common base tiles.
        /// The remaining sprites are rare variants chosen only when a variant roll succeeds.
        /// </summary>
        public int baseCount = 1;

        /// <summary>
        /// Probability (0–1) that a variant sprite (index baseCount+) is chosen instead of a base sprite.
        /// 0 = always base; 0.1 = 10% chance of a variant. Ignored if sprites has only base entries.
        /// </summary>
        [Range(0f, 1f)]
        public float variantChance = 0.15f;

        /// <summary>
        /// 16-sprite Wang autotile set, indexed by a 4-bit corner bitmask (NW=bit0, NE=bit1, SE=bit2, SW=bit3).
        /// When Length == 16, MineGrid uses neighbour-aware sprite selection instead of random PickSprite.
        /// Leave empty for tiles that use the random <see cref="sprites"/> pool (e.g. floor tiles).
        /// </summary>
        public Sprite[] wangSprites;

        /// <summary>
        /// Sorting order used by the SpriteRenderer. Lower values render behind higher values.
        /// Set floor tiles to -1, wall/ore tiles to 0.
        /// </summary>
        public int sortingOrder = 0;
    }
}
