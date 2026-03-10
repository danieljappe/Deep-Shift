using UnityEngine;
using UnityEditor;
using DeepShift.Mining;
using DeepShift.Hazards;

namespace DeepShift.Editor
{
    public static class TileOreAssetGenerator
    {
        private const string OreRoot  = "Assets/_Project/ScriptableObjects/Ore";
        private const string TileRoot = "Assets/_Project/ScriptableObjects/Tiles";

        [MenuItem("DeepShift/Generate Ore & Tile Assets")]
        public static void GenerateAll()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects", "Ore");
            EnsureFolder("Assets/_Project/ScriptableObjects", "Tiles");

            GenerateOreAssets();
            GenerateTileAssets();
            GenerateSpecialTileAssets();
            UpdateTileWalkable("Floor.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DeepShift] Ore & Tile assets generated.");
        }

        // ── Ore ───────────────────────────────────────────────────────────────

        private static void GenerateOreAssets()
        {
            CreateOre(OreType.Iron,             "Iron",              5,   new Color(0.50f, 0.50f, 0.50f));
            CreateOre(OreType.Silicate,         "Silicate",          8,   new Color(0.68f, 0.85f, 0.90f));
            CreateOre(OreType.Veritanium,       "Veritanium",        25,  new Color(0.13f, 0.70f, 0.13f));
            CreateOre(OreType.Glacite,          "Glacite",           35,  new Color(0.00f, 1.00f, 1.00f));
            CreateOre(OreType.Voidstone,        "Voidstone",         80,  new Color(0.50f, 0.00f, 0.50f));
            CreateOre(OreType.Crymite,          "Crymite",           100, new Color(0.00f, 0.00f, 0.55f));
            CreateOre(OreType.EchoveinCrystal,  "EchoveinCrystal",   300, new Color(1.00f, 1.00f, 0.00f));
        }

        private static void CreateOre(OreType type, string assetName, int creditValue, Color color)
        {
            string path = $"{OreRoot}/{assetName}.asset";
            if (Exists<OreDataSO>(path)) return;

            var ore = ScriptableObject.CreateInstance<OreDataSO>();
            ore.oreType     = type;
            ore.displayName = assetName;
            ore.creditValue = creditValue;
            ore.tileColor   = color;

            AssetDatabase.CreateAsset(ore, path);
            Debug.Log($"[DeepShift] Created ore: {assetName}");
        }

        // ── Tiles ─────────────────────────────────────────────────────────────

        private static void GenerateTileAssets()
        {
            // Load ore assets for cross-reference
            var iron      = Load<OreDataSO>($"{OreRoot}/Iron.asset");
            var silicate  = Load<OreDataSO>($"{OreRoot}/Silicate.asset");
            var veri      = Load<OreDataSO>($"{OreRoot}/Veritanium.asset");
            var glacite   = Load<OreDataSO>($"{OreRoot}/Glacite.asset");
            var voidstone = Load<OreDataSO>($"{OreRoot}/Voidstone.asset");
            var crymite   = Load<OreDataSO>($"{OreRoot}/Crymite.asset");
            var echovein  = Load<OreDataSO>($"{OreRoot}/EchoveinCrystal.asset");

            //            name              destr  hits  ore        risk   debugColor
            CreateTile("RockEmpty",    true,  2, null,      0.05f, new Color(0.30f, 0.30f, 0.30f));
            CreateTile("RockIron",     true,  2, iron,      0.05f, new Color(0.50f, 0.50f, 0.50f));
            CreateTile("RockSilicate", true,  2, silicate,  0.05f, new Color(0.68f, 0.85f, 0.90f));
            CreateTile("RockVein",     true,  3, veri,      0.10f, new Color(0.13f, 0.70f, 0.13f));
            CreateTile("RockGlacite",  true,  3, glacite,   0.10f, new Color(0.00f, 1.00f, 1.00f));
            CreateTile("RockVoid",     true,  4, voidstone, 0.15f, new Color(0.50f, 0.00f, 0.50f));
            CreateTile("RockCrymite",  true,  4, crymite,   0.15f, new Color(0.00f, 0.00f, 0.55f));
            CreateTile("RockEchovein", true,  5, echovein,  0.20f, new Color(1.00f, 1.00f, 0.00f));
            CreateTile("Wall",         false, 0, null,      0.00f, new Color(0.00f, 0.00f, 0.00f));
            CreateTile("Floor",        false, 0, null,      0.00f, new Color(0.21f, 0.21f, 0.21f));
        }

        private static void CreateTile(
            string assetName, bool destructible, int hits,
            OreDataSO ore, float collapseRisk, Color color)
        {
            string path = $"{TileRoot}/{assetName}.asset";
            if (Exists<TileDataSO>(path)) return;

            var tile = ScriptableObject.CreateInstance<TileDataSO>();
            tile.tileName         = assetName;
            tile.isDestructible   = destructible;
            tile.drillHitsRequired = hits;
            tile.containedOre     = ore;
            tile.collapseRisk     = collapseRisk;
            tile.debugColor       = color;

            AssetDatabase.CreateAsset(tile, path);
            Debug.Log($"[DeepShift] Created tile: {assetName}");
        }

        // ── Special tiles ─────────────────────────────────────────────────────

        private static void GenerateSpecialTileAssets()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects", "Tiles");

            // GasPocket
            string gasPath = $"{TileRoot}/GasPocket.asset";
            if (!Exists<GasTileDataSO>(gasPath))
            {
                var gas = ScriptableObject.CreateInstance<GasTileDataSO>();
                gas.tileName          = "GasPocket";
                gas.isDestructible    = false;
                gas.drillHitsRequired = 0;
                gas.isWalkable        = true;
                gas.debugColor        = new Color(0.55f, 0.75f, 0.10f);
                gas.gasDamage         = 10;
                gas.gasCheckRadius    = 1;
                AssetDatabase.CreateAsset(gas, gasPath);
                Debug.Log("[DeepShift] Created tile: GasPocket");
            }

            // FalseWall
            string fwPath = $"{TileRoot}/FalseWall.asset";
            if (!Exists<FalseWallTileDataSO>(fwPath))
            {
                var fw = ScriptableObject.CreateInstance<FalseWallTileDataSO>();
                fw.tileName          = "FalseWall";
                fw.isDestructible    = true;
                fw.drillHitsRequired = 2;
                fw.debugColor        = new Color(0.30f, 0.30f, 0.30f);
                AssetDatabase.CreateAsset(fw, fwPath);
                Debug.Log("[DeepShift] Created tile: FalseWall");
            }
        }

        private static void UpdateTileWalkable(string assetFileName)
        {
            string path  = $"{TileRoot}/{assetFileName}";
            var    tile  = AssetDatabase.LoadAssetAtPath<TileDataSO>(path);
            if (tile == null) { Debug.LogWarning($"[DeepShift] UpdateTileWalkable: could not load {path}"); return; }
            if (tile.isWalkable) return;
            tile.isWalkable = true;
            EditorUtility.SetDirty(tile);
            Debug.Log($"[DeepShift] Updated isWalkable on {assetFileName}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static bool Exists<T>(string path) where T : Object
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                Debug.Log($"[DeepShift] Skipped (exists): {path}");
                return true;
            }
            return false;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning($"[DeepShift] Could not load: {path}");
            return asset;
        }
    }
}
