using UnityEngine;
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

        [Header("Event Channels")]
        [SerializeField] private GameEventSO_Int _onOrePickedUp;
        [SerializeField] private GameEventSO     _onTileDestroyed;
        [SerializeField] private GameEventSO     _onHazardTriggered;

        // ── Internal state ────────────────────────────────────────────────────

        private TileInstance[,] _grid;

        // ── Nested type ───────────────────────────────────────────────────────

        public struct TileInstance
        {
            public TileDataSO  data;
            public int         hitsRemaining;
            public bool        isDestroyed;
            public GameObject  visualObject;
        }

        // ── Public properties ─────────────────────────────────────────────────

        public int   Width    => _gridWidth;
        public int   Height   => _gridHeight;
        public float TileSize => _tileSize;

        // ── Public methods ────────────────────────────────────────────────────

        /// <summary>
        /// Allocates the grid and spawns one quad GameObject per cell.
        /// Each quad is coloured with <see cref="TileDataSO.debugColor"/> and
        /// positioned using <see cref="_tileSize"/> spacing on the Tiles sorting layer.
        /// Call this once after the scene is ready (e.g. from a ShiftManager).
        /// </summary>
        public void GenerateGrid(TileDataSO defaultTile)
        {
            ClearGrid();

            _grid = new TileInstance[_gridWidth, _gridHeight];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _grid[x, y] = new TileInstance
                    {
                        data          = defaultTile,
                        hitsRemaining = defaultTile.drillHitsRequired,
                        isDestroyed   = false,
                        visualObject  = CreateVisual(x, y, defaultTile),
                    };
                }
            }
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
        /// disables its visual, raises <see cref="_onTileDestroyed"/>, and — if the tile
        /// contained ore — raises <see cref="_onOrePickedUp"/> with the ore's credit value.
        /// </summary>
        public void DestroyTile(int x, int y)
        {
            if (!InBounds(x, y))         return;
            if (_grid[x, y].isDestroyed) return;

            _grid[x, y].isDestroyed = true;

            if (_grid[x, y].visualObject != null)
                _grid[x, y].visualObject.SetActive(false);

            if (_grid[x, y].data.containedOre != null)
                _onOrePickedUp?.Raise(_grid[x, y].data.containedOre.creditValue);

            _onTileDestroyed?.Raise();
            CheckAdjacentGas(x, y);
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
        /// and updates its visual colour immediately.
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

            var go = _grid[x, y].visualObject;
            if (go == null) return;

            go.SetActive(true);
            go.GetComponent<Renderer>().material.color = data.debugColor;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private GameObject CreateVisual(int x, int y, TileDataSO tile)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = $"Tile_{x}_{y}";
            go.transform.SetParent(transform);
            go.transform.position   = GridToWorld(x, y);
            go.transform.localScale = Vector3.one * _tileSize;

            var renderer = go.GetComponent<Renderer>();
            renderer.sortingLayerName = "Tiles";

            // Use a new unlit material per tile so debugColor is always visible
            // regardless of render pipeline. MaterialPropertyBlock on the default
            // URP Lit material does not reliably tint without GPU instancing enabled.
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = tile.debugColor;
            renderer.material = mat;

            // Remove the auto-generated collider — collision is handled at the grid level
            Destroy(go.GetComponent<Collider>());

            return go;
        }

        private void ClearGrid()
        {
            if (_grid == null) return;

            foreach (var tile in _grid)
            {
                if (tile.visualObject != null)
                    Destroy(tile.visualObject);
            }

            _grid = null;
        }

        private bool InBounds(int x, int y) =>
            x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
    }
}
