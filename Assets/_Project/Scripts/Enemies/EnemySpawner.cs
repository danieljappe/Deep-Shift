using System.Collections.Generic;
using UnityEngine;
using DeepShift.Mining;

namespace DeepShift.Enemies
{
    /// <summary>
    /// Populates a floor with enemies by spending a threat budget.
    /// Called by <c>MineTestBootstrap</c> after grid and room generation completes.
    ///
    /// Attach to any scene GameObject (e.g. the MineTestBootstrap GameObject).
    /// Assign <see cref="_config"/> in the Inspector.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Config")]
        /// <summary>Threat budget rules and enemy pool.</summary>
        [SerializeField] private ThreatBudgetConfig _config;

        [Header("Spawn Safety")]
        /// <summary>Manhattan tile radius around the player spawn centre that is kept clear of enemies.</summary>
        [SerializeField] private int _spawnClearRadius = 3;

        /// <summary>Manhattan tile radius around the hoist terminal that is kept clear of enemies.</summary>
        [SerializeField] private int _hoistClearRadius = 3;

        // ── Internal tracking ─────────────────────────────────────────────────

        private readonly List<GameObject> _spawnedEnemies = new();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the floor by spending the threat budget for <paramref name="floorNumber"/>.
        /// Instantiates enemy prefabs from <see cref="_config"/> at valid positions on <paramref name="grid"/>.
        /// </summary>
        /// <param name="floorNumber">Current floor depth (1-based).</param>
        /// <param name="grid">The fully-generated mine grid for this floor.</param>
        /// <param name="spawnCentre">Grid coordinate of the player spawn room centre.</param>
        /// <param name="hoistCentre">Grid coordinate of the hoist terminal room centre.</param>
        public void SpawnEnemies(int floorNumber, MineGrid grid, Vector2Int spawnCentre, Vector2Int hoistCentre)
        {
            if (_config == null)
            {
                Debug.LogWarning("[EnemySpawner] No ThreatBudgetConfig assigned — skipping spawn.");
                return;
            }

            if (_config.enemyPool == null || _config.enemyPool.Length == 0)
            {
                Debug.LogWarning("[EnemySpawner] Enemy pool is empty — skipping spawn.");
                return;
            }

            int budget = _config.GetBudget(floorNumber);
            Debug.Log($"[EnemySpawner] Floor {floorNumber} — budget: {budget} pts");

            // Filter pool to entries valid for this floor
            var eligible = new List<EnemySpawnDataSO>();
            foreach (var entry in _config.enemyPool)
            {
                if (entry == null || entry.prefab == null) continue;
                if (floorNumber >= entry.minFloor && floorNumber <= entry.maxFloor)
                    eligible.Add(entry);
            }

            if (eligible.Count == 0)
            {
                Debug.Log("[EnemySpawner] No eligible enemy types for this floor.");
                return;
            }

            // Pre-compute candidate positions for each placement category
            List<Vector2Int> wallAdjacentPositions = BuildWallAdjacentPositions(grid, spawnCentre, hoistCentre);
            List<Vector2Int> openPositions         = BuildOpenPositions(grid, spawnCentre, hoistCentre);

            Shuffle(wallAdjacentPositions);
            Shuffle(openPositions);

            // Track which positions are already taken
            var takenPositions = new HashSet<Vector2Int>();

            // Track spawn counts per entry to enforce maxPerFloor
            var spawnCounts = new Dictionary<EnemySpawnDataSO, int>();
            foreach (var entry in eligible)
                spawnCounts[entry] = 0;

            int totalSpawned = 0;

            while (budget > 0)
            {
                // Build list of types we can still afford and haven't capped
                var affordableTypes = new List<EnemySpawnDataSO>();
                foreach (var entry in eligible)
                {
                    if (entry.threatCost > budget) continue;
                    if (entry.maxPerFloor > 0 && spawnCounts[entry] >= entry.maxPerFloor) continue;
                    affordableTypes.Add(entry);
                }

                if (affordableTypes.Count == 0) break;

                Shuffle(affordableTypes);

                bool placedAny = false;

                foreach (var entry in affordableTypes)
                {
                    List<Vector2Int> candidates = entry.requiresWallPlacement
                        ? wallAdjacentPositions
                        : openPositions;

                    Vector2Int pos;
                    if (!TryPickPosition(candidates, takenPositions, out pos))
                        continue;

                    // Instantiate and track
                    var go = Instantiate(entry.prefab, grid.GridToWorld(pos.x, pos.y), Quaternion.identity);
                    _spawnedEnemies.Add(go);
                    takenPositions.Add(pos);

                    spawnCounts[entry]++;
                    budget -= entry.threatCost;
                    totalSpawned++;
                    placedAny = true;
                    break; // restart outer loop to re-evaluate affordable types
                }

                if (!placedAny) break; // no type could be placed — stop
            }

            Debug.Log($"[EnemySpawner] Spawned {totalSpawned} enemies. Remaining budget: {budget} pts.");
        }

        /// <summary>
        /// Destroys all enemies that were spawned by the last <see cref="SpawnEnemies"/> call.
        /// Called by <c>MineTestBootstrap.CleanupFloorObjects()</c> before floor regeneration.
        /// </summary>
        public void ClearSpawnedEnemies()
        {
            foreach (var go in _spawnedEnemies)
            {
                if (go != null) Destroy(go);
            }
            _spawnedEnemies.Clear();
        }

        // ── Position building ─────────────────────────────────────────────────

        /// <summary>
        /// Returns all open (walkable, non-destroyed) tiles that are directly adjacent
        /// (4-directional) to at least one solid tile, excluding safety radii.
        /// </summary>
        private List<Vector2Int> BuildWallAdjacentPositions(
            MineGrid grid, Vector2Int spawnCentre, Vector2Int hoistCentre)
        {
            var result = new List<Vector2Int>();

            for (int x = 1; x < grid.Width - 1; x++)
            for (int y = 1; y < grid.Height - 1; y++)
            {
                if (!IsOpenTile(grid, x, y))        continue;
                if (IsExcluded(x, y, spawnCentre, hoistCentre)) continue;
                if (!HasSolidNeighbour(grid, x, y)) continue;

                result.Add(new Vector2Int(x, y));
            }

            return result;
        }

        /// <summary>
        /// Returns all open (walkable, non-destroyed) tiles excluding safety radii.
        /// Used for enemies that do not require wall placement.
        /// </summary>
        private List<Vector2Int> BuildOpenPositions(
            MineGrid grid, Vector2Int spawnCentre, Vector2Int hoistCentre)
        {
            var result = new List<Vector2Int>();

            for (int x = 1; x < grid.Width - 1; x++)
            for (int y = 1; y < grid.Height - 1; y++)
            {
                if (!IsOpenTile(grid, x, y))        continue;
                if (IsExcluded(x, y, spawnCentre, hoistCentre)) continue;

                result.Add(new Vector2Int(x, y));
            }

            return result;
        }

        // ── Placement helpers ─────────────────────────────────────────────────

        private static bool TryPickPosition(
            List<Vector2Int> candidates,
            HashSet<Vector2Int> taken,
            out Vector2Int result)
        {
            foreach (var pos in candidates)
            {
                if (!taken.Contains(pos))
                {
                    result = pos;
                    return true;
                }
            }

            result = Vector2Int.zero;
            return false;
        }

        private bool IsExcluded(int x, int y, Vector2Int spawnCentre, Vector2Int hoistCentre)
        {
            int distSpawn = Mathf.Abs(x - spawnCentre.x) + Mathf.Abs(y - spawnCentre.y);
            if (distSpawn <= _spawnClearRadius) return true;

            // TODO: remove this guard once hoistCentre is always valid (non-zero)
            if (hoistCentre != Vector2Int.zero)
            {
                int distHoist = Mathf.Abs(x - hoistCentre.x) + Mathf.Abs(y - hoistCentre.y);
                if (distHoist <= _hoistClearRadius) return true;
            }

            return false;
        }

        private static bool IsOpenTile(MineGrid grid, int x, int y)
        {
            MineGrid.TileInstance? tile = grid.GetTile(x, y);
            if (tile == null)                  return false;
            if (tile.Value.isDestroyed)         return false;
            if (tile.Value.data == null)        return false;
            return tile.Value.data.isWalkable;
        }

        private static bool IsSolidTile(MineGrid grid, int x, int y)
        {
            MineGrid.TileInstance? tile = grid.GetTile(x, y);
            if (tile == null)          return true;  // out-of-bounds counts as solid
            if (tile.Value.isDestroyed) return false;
            if (tile.Value.data == null) return false;
            return !tile.Value.data.isWalkable;
        }

        private static bool HasSolidNeighbour(MineGrid grid, int x, int y)
        {
            return IsSolidTile(grid, x + 1, y) ||
                   IsSolidTile(grid, x - 1, y) ||
                   IsSolidTile(grid, x,     y + 1) ||
                   IsSolidTile(grid, x,     y - 1);
        }

        // ── Utility ───────────────────────────────────────────────────────────

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
