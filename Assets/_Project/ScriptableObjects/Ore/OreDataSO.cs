using UnityEngine;
using DeepShift.Mining;

namespace DeepShift.Mining
{
    [CreateAssetMenu(menuName = "DeepShift/Ore/OreData", fileName = "OreData")]
    public class OreDataSO : ScriptableObject
    {
        /// <summary>The ore category this asset represents. Must match the OreType enum.</summary>
        public OreType oreType;

        /// <summary>Human-readable name shown in UI (e.g. "Veritanium").</summary>
        public string displayName;

        /// <summary>Base credit value awarded when this ore is extracted and sold.</summary>
        public int creditValue;

        /// <summary>Representative colour used for prototype tilemap tinting and UI icons.</summary>
        public Color tileColor;
    }
}
