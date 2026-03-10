using UnityEngine;
using UnityEditor;
using DeepShift.Core;
using DeepShift.Mining;

namespace DeepShift.Editor
{
    public static class CreateEventAssets
    {
        private const string Root      = "Assets/_Project/ScriptableObjects/Events";
        private const string TilesRoot = "Assets/_Project/ScriptableObjects/Tiles";

        [MenuItem("DeepShift/Create All Event Assets")]
        public static void CreateAll()
        {
            EnsureFolders();

            // Player
            Create<GameEventSO>      ("Player", "PlayerDied");
            Create<GameEventSO_Float>("Player", "PlayerHealthChanged");
            Create<GameEventSO_Int>  ("Player", "PlayerFloorChanged");

            // Mining
            Create<GameEventSO_Int>   ("Mining", "OrePickedUp");
            Create<GameEventSO>       ("Mining", "TileDestroyed");
            Create<GameEventSO_String>("Mining", "OreVeinDiscovered");
            Create<GameEventSO_String>("Mining", "HazardTriggered");

            // Hoist
            Create<GameEventSO>      ("Hoist", "HoistCalled");
            Create<GameEventSO_Float>("Hoist", "HoistCountdownTick");
            Create<GameEventSO>      ("Hoist", "HoistExtracted");
            Create<GameEventSO>      ("Hoist", "HoistCancelled");

            // Economy
            Create<GameEventSO_Int>("Economy", "OreCreditsChanged");
            Create<GameEventSO_Int>("Economy", "DebtChanged");
            Create<GameEventSO_Int>("Economy", "VektraRepChanged");
            Create<GameEventSO>    ("Economy", "BlackMarketAccessed");

            // ORION
            Create<GameEventSO_String>("ORION", "ORIONDialogueTrigger");
            Create<GameEventSO_String>("ORION", "ORIONPriorityOverride");

            // Shift
            Create<GameEventSO>       ("Shift", "ShiftStarted");
            Create<GameEventSO>       ("Shift", "ShiftEnded");
            Create<GameEventSO_String>("Shift", "ContractCompleted");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DeepShift] All event assets created.");
        }

        // ── Tile assets ───────────────────────────────────────────────────────

        [MenuItem("DeepShift/Create All Tile Assets")]
        public static void CreateAllTiles()
        {
            if (!AssetDatabase.IsValidFolder(TilesRoot))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Tiles");

            //              assetName       tileName          destr  hits  risk   hex colour
            CreateTile("RockEmpty",   "Rock",          true,  2, 0.05f, "#3A3A3A");
            CreateTile("RockIron",    "Iron Rock",     true,  2, 0.05f, "#808080");
            CreateTile("RockSilicate","Silicate Rock", true,  2, 0.05f, "#ADD8E6");
            CreateTile("RockVein",    "Vein Rock",     true,  3, 0.10f, "#00A550");
            CreateTile("RockGlacite", "Glacite Rock",  true,  3, 0.10f, "#00FFFF");
            CreateTile("RockVoid",    "Void Rock",     true,  4, 0.15f, "#800080");
            CreateTile("RockCrymite", "Crymite Rock",  true,  4, 0.15f, "#00008B");
            CreateTile("RockEchovein","Echovein Rock", true,  5, 0.20f, "#FFD700");
            CreateTile("Wall",        "Wall",          false, 0, 0.00f, "#111111");
            CreateTile("Floor",       "Floor",         false, 0, 0.00f, "#2A2A2A");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DeepShift] All tile assets created. Assign containedOre references manually in the Inspector.");
        }

        private static void CreateTile(
            string assetName, string tileName,
            bool isDestructible, int drillHits, float collapseRisk, string hex)
        {
            string path = $"{TilesRoot}/{assetName}.asset";

            if (AssetDatabase.LoadAssetAtPath<TileDataSO>(path) != null)
            {
                Debug.Log($"[DeepShift] Skipped (already exists): Tiles/{assetName}");
                return;
            }

            ColorUtility.TryParseHtmlString(hex, out Color color);

            var tile = ScriptableObject.CreateInstance<TileDataSO>();
            tile.name              = assetName;
            tile.tileName          = tileName;
            tile.isDestructible    = isDestructible;
            tile.drillHitsRequired = drillHits;
            tile.containedOre      = null;
            tile.collapseRisk      = collapseRisk;
            tile.debugColor        = color;

            AssetDatabase.CreateAsset(tile, path);
            Debug.Log($"[DeepShift] Created: Tiles/{assetName}.asset");
        }

        // ── Event assets ──────────────────────────────────────────────────────

        private static void Create<T>(string subfolder, string assetName) where T : ScriptableObject
        {
            string path = $"{Root}/{subfolder}/{assetName}.asset";

            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                Debug.Log($"[DeepShift] Skipped (already exists): {subfolder}/{assetName}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[DeepShift] Created: {subfolder}/{assetName}.asset");
        }

        private static void EnsureFolders()
        {
            string[] subfolders = { "Player", "Mining", "Hoist", "Economy", "ORION", "Shift" };
            foreach (string sub in subfolders)
            {
                string path = $"{Root}/{sub}";
                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder(Root, sub);
            }
        }
    }
}
