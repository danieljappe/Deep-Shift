// TODO: Replace with procedural generation in Phase 3

using UnityEngine;

namespace DeepShift.Mining
{
    /// <summary>
    /// Prototype-only scene bootstrap. Generates a mine grid, scatters ore tiles,
    /// and clears a spawn room for the player. Attach to any GameObject in the Mine scene
    /// alongside a configured <see cref="MineGrid"/>.
    /// </summary>
    public class MineTestBootstrap : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private MineGrid    _mineGrid;
        [SerializeField] private TileDataSO  _defaultRockTile;  // assign RockEmpty SO
        [SerializeField] private TileDataSO  _wallTile;         // assign Wall SO

        [Header("Ore Scatter")]
        [SerializeField] private TileDataSO[] _oreRockTiles;    // assign ore tile SOs
        [SerializeField][Range(0f, 1f)]
        private float _oreSpawnChance = 0.08f;

        private void Start()
        {
            if (_mineGrid == null)
            {
                Debug.LogError("[MineTestBootstrap] MineGrid reference is missing.");
                return;
            }

            if (_defaultRockTile == null)
            {
                Debug.LogError("[MineTestBootstrap] defaultRockTile is not assigned.");
                return;
            }

            int w = _mineGrid.Width;
            int h = _mineGrid.Height;

            // ── 1. Fill grid with default rock ────────────────────────────────
            _mineGrid.GenerateGrid(_defaultRockTile);

            // ── 2. Stamp border cells as Wall ─────────────────────────────────
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

            // ── 3. Scatter ore tiles in the interior ──────────────────────────
            if (_oreRockTiles != null && _oreRockTiles.Length > 0)
            {
                int centerX = w / 2;
                int centerY = h / 2;

                for (int x = 1; x < w - 1; x++)
                {
                    for (int y = 1; y < h - 1; y++)
                    {
                        if (IsSpawnRoom(x, y, centerX, centerY)) continue;
                        if (Random.value < _oreSpawnChance)
                        {
                            TileDataSO oreTile = _oreRockTiles[Random.Range(0, _oreRockTiles.Length)];
                            _mineGrid.SetTile(x, y, oreTile);
                        }
                    }
                }
            }

            // ── 4. Clear 3×3 spawn room at grid centre ────────────────────────
            {
                int centerX = w / 2;
                int centerY = h / 2;

                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    _mineGrid.DestroyTile(centerX + dx, centerY + dy);
            }

            Debug.Log($"[MineTestBootstrap] Mine grid ready — {w}x{h}");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the world-space centre of the cleared 3×3 spawn room.
        /// PlayerController calls this in Start() to position the player correctly.
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            int cx = _mineGrid.Width  / 2;
            int cy = _mineGrid.Height / 2;
            return _mineGrid.GridToWorld(cx, cy);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsSpawnRoom(int x, int y, int centerX, int centerY) =>
            Mathf.Abs(x - centerX) <= 1 && Mathf.Abs(y - centerY) <= 1;
    }
}
