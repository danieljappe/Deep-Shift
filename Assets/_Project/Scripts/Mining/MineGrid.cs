using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DeepShift.Core;
using DeepShift.Hazards;

namespace DeepShift.Mining
{
    public class MineGrid : MonoBehaviour
    {
        // ── Serialized config ─────────────────────────────────────────────────

        [Header("Grid Dimensions")]
        [SerializeField] private int   _gridWidth  = 20;
        [SerializeField] private int   _gridHeight = 15;
        [SerializeField] private float _tileSize   = 1f;

        [Header("Tilemap")]
        [SerializeField] private Tilemap _tilemap;

        [Header("Event Channels")]
        [SerializeField] private GameEventSO _onTileDestroyed;
        [SerializeField] private GameEventSO _onHazardTriggered;

        // ── Internal state ────────────────────────────────────────────────────

        private TileInstance[,] _grid;

        // One Tile asset per unique Sprite — created once, reused forever
        private readonly Dictionary<Sprite, Tile> _tileCache = new();

        // ── Nested type ───────────────────────────────────────────────────────

        public struct TileInstance
        {
            public TileDataSO data;
            public int        hitsRemaining;
            public bool       isDestroyed;
        }

        // ── Public properties ─────────────────────────────────────────────────

        public int        Width    => _gridWidth;
        public int        Height   => _gridHeight;
        public float      TileSize => _tileSize;

        /// <summary>
        /// Grid coordinate of the most recently destroyed tile.
        /// Updated immediately before <c>TileDestroyed</c> is raised in <see cref="DestroyTile"/>.
        /// Read by <see cref="DeepShift.Enemies.DrillVibrationBroadcaster"/> to broadcast a positioned DrillImpact event.
        /// </summary>
        public Vector2Int LastDrilledPosition { get; private set; }

        // ── Public methods ────────────────────────────────────────────────────

        /// <summary>
        /// Allocates the grid data and fills the Tilemap with the default tile.
        /// The visual Tilemap is populated immediately; call <see cref="RefreshAllWangVisuals"/>
        /// after all <see cref="SetTile"/> overrides to correct Wang bitmask sprites.
        /// </summary>
        public void GenerateGrid(TileDataSO defaultTile)
        {
            ClearGrid();

            _grid = new TileInstance[_gridWidth, _gridHeight];

            // Pass 1: fill all data — must complete before any bitmask is computed,
            // because GetWangBitmask reads neighbour cells that may not be set yet.
            for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                _grid[x, y] = new TileInstance
                {
                    data          = defaultTile,
                    hitsRemaining = defaultTile.drillHitsRequired,
                    isDestroyed   = false,
                };

            // Pass 2: fill tilemap now that all neighbours are initialised.
            for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                _tilemap.SetTile(new Vector3Int(x, y, 0), GetVisualTile(x, y, defaultTile));
        }

        /// <summary>
        /// Returns the <see cref="TileInstance"/> at the given grid coordinates,
        /// or <c>null</c> if the coordinates are out of bounds.
        /// </summary>
        public TileInstance? GetTile(int x, int y)
        {
            if (_grid == null || !InBounds(x, y)) return null;
            return _grid[x, y];
        }

        /// <summary>
        /// Applies one drill hit to the tile at (<paramref name="x"/>, <paramref name="y"/>).
        /// Has no effect on indestructible or already-destroyed tiles.
        /// </summary>
        /// <returns><c>true</c> if the hit destroyed the tile; <c>false</c> otherwise.</returns>
        public bool HitTile(int x, int y)
        {
            if (!InBounds(x, y))                  return false;
            if (_grid[x, y].isDestroyed)           return false;
            if (!_grid[x, y].data.isDestructible)  return false;

            _grid[x, y].hitsRemaining--;

            if (_grid[x, y].hitsRemaining <= 0)
            {
                DestroyTile(x, y);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Immediately destroys the tile at (<paramref name="x"/>, <paramref name="y"/>):
        /// clears its Tilemap cell, raises <see cref="_onTileDestroyed"/>, and checks for adjacent gas.
        /// Ore collection is handled by the <see cref="OrePickup"/> spawned by <see cref="DrillController"/>.
        /// </summary>
        public void DestroyTile(int x, int y)
        {
            if (!InBounds(x, y))         return;
            if (_grid[x, y].isDestroyed) return;

            _grid[x, y].isDestroyed = true;
            _tilemap.SetTile(new Vector3Int(x, y, 0), null);

            LastDrilledPosition = new Vector2Int(x, y);
            _onTileDestroyed?.Raise();
            CheckAdjacentGas(x, y);

            // Refresh Wang sprites on all 8 surrounding tiles — each shares a corner with this cell
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                RefreshWangSprite(x + dx, y + dy);
            }
        }

        private void CheckAdjacentGas(int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (!InBounds(nx, ny)) continue;
                if (_grid[nx, ny].isDestroyed) continue;
                if (_grid[nx, ny].data is GasTileDataSO)
                { _onHazardTriggered?.Raise(); return; }
            }
        }

        /// <summary>
        /// Converts a grid coordinate pair to a world-space <see cref="Vector3"/>,
        /// accounting for this transform's position and <see cref="_tileSize"/>.
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            return transform.position + new Vector3(x * _tileSize, y * _tileSize, 0f);
        }

        /// <summary>
        /// Converts a world-space position to the nearest grid coordinate.
        /// Does not clamp — callers should validate with <see cref="GetTile"/> before use.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 local = worldPos - transform.position;
            return new Vector2Int(
                Mathf.RoundToInt(local.x / _tileSize),
                Mathf.RoundToInt(local.y / _tileSize)
            );
        }

        /// <summary>
        /// Replaces the data for the tile at (<paramref name="x"/>, <paramref name="y"/>)
        /// and updates the Tilemap visual immediately.
        /// Resets <see cref="TileInstance.hitsRemaining"/> to the new tile's
        /// <see cref="TileDataSO.drillHitsRequired"/> and clears the destroyed flag.
        /// No events are raised — use this for world generation, not gameplay destruction.
        /// </summary>
        public void SetTile(int x, int y, TileDataSO data)
        {
            if (!InBounds(x, y) || data == null) return;

            _grid[x, y].data          = data;
            _grid[x, y].hitsRemaining = data.drillHitsRequired;
            _grid[x, y].isDestroyed   = false;

            _tilemap.SetTile(new Vector3Int(x, y, 0), GetVisualTile(x, y, data));
        }

        /// <summary>
        /// Refreshes Wang autotile sprites for every non-destroyed tile in the grid that has a
        /// <see cref="TileDataSO.wangSprites"/> array. Call once after all <see cref="SetTile"/>
        /// calls in floor generation have completed.
        /// </summary>
        public void RefreshAllWangVisuals()
        {
            if (_grid == null) return;
            for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                RefreshWangSprite(x, y);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the Tilemap <see cref="Tile"/> asset to display for the cell at (x, y).
        /// Applies the Wang bitmask if <see cref="TileDataSO.wangSprites"/> is populated;
        /// otherwise picks randomly from <see cref="TileDataSO.sprites"/>.
        /// </summary>
        private Tile GetVisualTile(int x, int y, TileDataSO data)
        {
            bool hasWang = data.wangSprites != null && data.wangSprites.Length == 16;
            if (hasWang)
            {
                int idx    = GetWangBitmask(x, y);
                var sprite = data.wangSprites[idx];
                return sprite != null ? GetOrCreateTile(sprite) : null;
            }

            if (data.sprites != null && data.sprites.Length > 0)
                return GetOrCreateTile(PickSprite(data));

            return null;
        }

        /// <summary>
        /// Returns a cached <see cref="Tile"/> asset for the given sprite, creating one on first use.
        /// Total allocation cost: one ScriptableObject per unique sprite across the session.
        /// </summary>
        private Tile GetOrCreateTile(Sprite sprite)
        {
            if (_tileCache.TryGetValue(sprite, out var tile)) return tile;

            tile        = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.flags  = TileFlags.LockColor | TileFlags.LockTransform;
            _tileCache[sprite] = tile;
            return tile;
        }

        // ── Wang autotile helpers ─────────────────────────────────────────────

        /// <summary>
        /// Returns true if the cell at (<paramref name="x"/>, <paramref name="y"/>) is a solid (non-walkable) wall.
        /// Out-of-bounds cells are treated as solid wall so border tiles get correct bitmasks.
        /// </summary>
        private bool IsWallCell(int x, int y)
        {
            if (!InBounds(x, y))          return true;
            if (_grid[x, y].isDestroyed)  return false;
            return !_grid[x, y].data.isWalkable;
        }

        /// <summary>
        /// Computes the 4-bit Wang corner bitmask for the tile at (<paramref name="x"/>, <paramref name="y"/>).
        /// bit 0 (1) = NW corner wall, bit 1 (2) = NE, bit 2 (4) = SE, bit 3 (8) = SW.
        /// A corner is considered wall only when all three cells sharing that corner are solid.
        /// </summary>
        private int GetWangBitmask(int x, int y)
        {
            int mask = 0;
            if (IsWallCell(x-1,y) && IsWallCell(x,y+1) && IsWallCell(x-1,y+1)) mask |= 1; // NW
            if (IsWallCell(x+1,y) && IsWallCell(x,y+1) && IsWallCell(x+1,y+1)) mask |= 2; // NE
            if (IsWallCell(x+1,y) && IsWallCell(x,y-1) && IsWallCell(x+1,y-1)) mask |= 4; // SE
            if (IsWallCell(x-1,y) && IsWallCell(x,y-1) && IsWallCell(x-1,y-1)) mask |= 8; // SW
            return mask;
        }

        /// <summary>
        /// Applies the correct Wang sprite to the Tilemap cell at (<paramref name="x"/>, <paramref name="y"/>)
        /// based on its corner bitmask. No-ops if the tile has no <see cref="TileDataSO.wangSprites"/>.
        /// </summary>
        private void RefreshWangSprite(int x, int y)
        {
            if (!InBounds(x, y)) return;
            if (_grid[x, y].isDestroyed) return;
            if (_grid[x, y].data.wangSprites == null || _grid[x, y].data.wangSprites.Length != 16) return;

            int idx    = GetWangBitmask(x, y);
            var sprite = _grid[x, y].data.wangSprites[idx];
            if (sprite != null)
                _tilemap.SetTile(new Vector3Int(x, y, 0), GetOrCreateTile(sprite));
        }

        private void ClearGrid()
        {
            if (_grid == null) return;
            _tilemap?.ClearAllTiles();
            _grid = null;
        }

        private bool InBounds(int x, int y) =>
            x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;

        private static Sprite PickSprite(TileDataSO tile)
        {
            int baseCount    = Mathf.Clamp(tile.baseCount, 1, tile.sprites.Length);
            bool hasVariants = tile.sprites.Length > baseCount;

            if (!hasVariants || Random.value > tile.variantChance)
                return tile.sprites[Random.Range(0, baseCount)];

            return tile.sprites[Random.Range(baseCount, tile.sprites.Length)];
        }
    }
}
