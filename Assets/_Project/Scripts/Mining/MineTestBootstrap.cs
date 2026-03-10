// TODO: Replace with procedural generation in Phase 3

using UnityEngine;

namespace DeepShift.Mining
{
    /// <summary>
    /// Prototype-only scene bootstrap. Generates a cellular automata cave, scatters ore tiles
    /// in solid rock cells, and provides the player spawn position. Attach to any GameObject
    /// in the Mine scene alongside a configured <see cref="MineGrid"/>.
    /// </summary>
    public class MineTestBootstrap : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private MineGrid   _mineGrid;
        [SerializeField] private TileDataSO _defaultRockTile; // assign RockEmpty SO
        [SerializeField] private TileDataSO _wallTile;        // assign Wall SO
        [SerializeField] private TileDataSO _floorTile;       // assign Floor SO

        [Header("Ore Scatter")]
        [SerializeField] private TileDataSO[] _oreRockTiles;  // assign ore tile SOs
        [SerializeField][Range(0f, 1f)]
        private float _oreSpawnChance = 0.08f;

        [Header("Cave Generation")]
        [SerializeField][Range(0f, 1f)]
        private float _fillChance    = 0.45f;
        [SerializeField][Range(1, 8)]
        private int   _smoothPasses  = 4;
        [SerializeField] private bool _useRandomSeed = true;
        [SerializeField] private int  _fixedSeed     = 0;
        [SerializeField] private int  _maxAttempts   = 10;

        [Header("Intercom Terminal")]
        [Tooltip("Offset from grid centre to the intercom terminal cell (grid units).")]
        [SerializeField] private Vector2Int _intercomOffset = new Vector2Int(0, -5);

        // ── Private state ──────────────────────────────────────────────────────

        private int _usedSeed;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (!ValidateReferences()) return;

            int w = _mineGrid.Width;
            int h = _mineGrid.Height;

            Vector2Int spawn    = new Vector2Int(w / 2, h / 2);
            Vector2Int intercom = ClampToInterior(spawn + _intercomOffset, w, h);

            // ── Generate cave (retry until intercom is reachable) ──────────────
            bool[,] caveMap  = null;
            bool    connected = false;
            int     attempt   = 0;

            while (!connected && attempt < _maxAttempts)
            {
                int seed  = _useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : _fixedSeed;
                _usedSeed = seed;

                caveMap   = CaveGenerator.Generate(w, h, spawn, intercom, _fillChance, _smoothPasses, seed);
                connected = CaveGenerator.IsConnected(caveMap, w, h, spawn, intercom);

                Debug.Log($"[MineTestBootstrap] Attempt {attempt + 1} — seed: {seed}, connected: {connected}");
                attempt++;
            }

            if (!connected)
                Debug.LogWarning("[MineTestBootstrap] Intercom unreachable after all attempts. " +
                                 "The L-corridor in CaveGenerator.Generate guarantees a path — " +
                                 "check that intercom cell is not on the grid border.");

            // ── Apply cave map to MineGrid ─────────────────────────────────────
            // Fill everything with rock first, then stamp open cells as Floor.
            // Using SetTile (not DestroyTile) avoids raising gameplay events during generation.
            _mineGrid.GenerateGrid(_defaultRockTile);

            for (int x = 1; x < w - 1; x++)
            for (int y = 1; y < h - 1; y++)
            {
                if (!caveMap[x, y])
                    _mineGrid.SetTile(x, y, _floorTile);
            }

            // ── Stamp border cells as Wall ─────────────────────────────────────
            if (_wallTile != null)
            {
                for (int x = 0; x < w; x++)
                {
                    _mineGrid.SetTile(x, 0,     _wallTile);
                    _mineGrid.SetTile(x, h - 1, _wallTile);
                }
                for (int y = 1; y < h - 1; y++)
                {
                    _mineGrid.SetTile(0,     y, _wallTile);
                    _mineGrid.SetTile(w - 1, y, _wallTile);
                }
            }

            // ── Scatter ore in solid rock cells only ───────────────────────────
            if (_oreRockTiles != null && _oreRockTiles.Length > 0)
            {
                // Initialise with the same seed for reproducible ore layouts
                Random.InitState(_usedSeed);

                for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                {
                    if (!caveMap[x, y])                        continue; // skip open space
                    if (IsNearSpawn(x, y, spawn))              continue; // keep spawn area clear
                    if (Random.value >= _oreSpawnChance)       continue;

                    TileDataSO ore = _oreRockTiles[Random.Range(0, _oreRockTiles.Length)];
                    _mineGrid.SetTile(x, y, ore);
                }
            }

            Debug.Log($"[MineTestBootstrap] Mine ready — {w}×{h}, seed: {_usedSeed}, " +
                      $"spawn: {spawn}, intercom: {intercom}");
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the world-space centre of the player spawn room.
        /// <see cref="PlayerController"/> calls this in Start() to position the player.
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            return _mineGrid.GridToWorld(_mineGrid.Width / 2, _mineGrid.Height / 2);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private bool ValidateReferences()
        {
            if (_mineGrid == null)
            {
                Debug.LogError("[MineTestBootstrap] MineGrid reference is missing.");
                return false;
            }
            if (_defaultRockTile == null)
            {
                Debug.LogError("[MineTestBootstrap] defaultRockTile is not assigned.");
                return false;
            }
            if (_floorTile == null)
            {
                Debug.LogError("[MineTestBootstrap] floorTile is not assigned. " +
                               "Assign the Floor TileDataSO so open cave cells are set correctly.");
                return false;
            }
            return true;
        }

        private static Vector2Int ClampToInterior(Vector2Int cell, int width, int height) =>
            new Vector2Int(Mathf.Clamp(cell.x, 1, width  - 2),
                           Mathf.Clamp(cell.y, 1, height - 2));

        private static bool IsNearSpawn(int x, int y, Vector2Int spawn) =>
            Mathf.Abs(x - spawn.x) <= 1 && Mathf.Abs(y - spawn.y) <= 1;
    }
}
