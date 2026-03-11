using UnityEngine;
using UnityEditor;
using DeepShift.Core;

namespace DeepShift.Editor
{
    public static class EventAssetGenerator
    {
        private const string EventsRoot = "Assets/_Project/ScriptableObjects/Events";

        [MenuItem("DeepShift/Generate Event Assets")]
        public static void GenerateAllEventAssets()
        {
            EnsureFolder(EventsRoot, "Player");
            EnsureFolder(EventsRoot, "Mining");
            EnsureFolder(EventsRoot, "Hoist");
            EnsureFolder(EventsRoot, "Economy");
            EnsureFolder(EventsRoot, "ORION");
            EnsureFolder(EventsRoot, "Shift");
            EnsureFolder(EventsRoot, "Enemies");

            // Player
            Create<GameEventSO>      ("Player", "PlayerDied");
            Create<GameEventSO_Float>("Player", "PlayerHealthChanged");
            Create<GameEventSO_Int>  ("Player", "PlayerFloorChanged");

            // Mining
            Create<GameEventSO_Int>  ("Mining", "OrePickedUp");
            Create<GameEventSO>      ("Mining", "TileDestroyed");
            Create<GameEventSO>      ("Mining", "OreVeinDiscovered");
            Create<GameEventSO>      ("Mining", "HazardTriggered");

            // Hoist
            Create<GameEventSO>      ("Hoist", "HoistCalled");
            Create<GameEventSO_Float>("Hoist", "HoistCountdownTick");
            Create<GameEventSO>      ("Hoist", "HoistExtracted");
            Create<GameEventSO>      ("Hoist", "HoistCancelled");
            Create<GameEventSO>      ("Hoist", "IntercomActivated");

            // Economy
            Create<GameEventSO_Int>  ("Economy", "OreCreditsChanged");
            Create<GameEventSO_Int>  ("Economy", "DebtChanged");
            Create<GameEventSO_Int>  ("Economy", "VektraRepChanged");
            Create<GameEventSO>      ("Economy", "BlackMarketAccessed");

            // ORION
            Create<GameEventSO_String>("ORION", "ORIONDialogueTrigger");
            Create<GameEventSO_Bool>  ("ORION", "ORIONPriorityOverride");

            // Mining (positioned)
            Create<GameEventSO_Vector2Int>("Mining", "DrillImpact");

            // Enemies
            Create<GameEventSO>    ("Enemies", "EnemyAlerted");
            Create<GameEventSO_Int>("Enemies", "EnemyDealDamage");
            Create<GameEventSO>    ("Enemies", "EnemyDied");

            // Shift
            Create<GameEventSO>("Shift", "ShiftStarted");
            Create<GameEventSO>("Shift", "ShiftEnded");
            Create<GameEventSO>("Shift", "FloorGenerated");
            Create<GameEventSO>("Shift", "ContractCompleted");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DeepShift] All event assets generated.");
        }

        private static void Create<T>(string subfolder, string assetName) where T : ScriptableObject
        {
            string path = $"{EventsRoot}/{subfolder}/{assetName}.asset";

            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                Debug.Log($"[DeepShift] Skipped (exists): {assetName}");
                return;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[DeepShift] Created: {subfolder}/{assetName}");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
