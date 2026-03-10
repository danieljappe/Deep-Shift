using UnityEngine;
using DeepShift.Mining;

namespace DeepShift.Hazards
{
    [CreateAssetMenu(menuName = "DeepShift/Tiles/GasTile")]
    public class GasTileDataSO : TileDataSO
    {
        /// <summary>Damage applied to the player per gas tick when standing in or adjacent to this tile.</summary>
        public int gasDamage = 10;

        /// <summary>Radius (in grid cells) checked around a destroyed tile for adjacent gas.</summary>
        public int gasCheckRadius = 1;
    }
}
