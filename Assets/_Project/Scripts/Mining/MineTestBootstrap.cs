// TODO: Replace with procedural generation in Phase 3

using System.Collections.Generic;
using UnityEngine;
using DeepShift.Core;
using DeepShift.Hazards;
using DeepShift.Enemies;
using DeepShift.Weapons;

namespace DeepShift.Mining
{
    /// <summary>
    /// Prototype-only scene bootstrap. Generates a cellular automata cave, runs the room
    /// placement pass, stamps all tile overrides, and provides the player spawn position.
    /// Attach to any GameObject in the Mine scene alongside a configured <see cref="MineGrid"/>.
    /// Subscribes to <c>HoistExtracted</c> to trigger in-place floor regeneration without
    /// reloading the scene.
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

        [Header("Room Placement")]
        [SerializeField] private int                 _floorDepth        = 1;
        [SerializeField] private GasTileDataSO       _gasTile;
        [SerializeField] private FalseWallTileDataSO _falseWallTile;
        [SerializeField] private TileDataSO[]        _oreChamberOreTiles; // depth-matched in Inspector

        [Header("Terminal Prefabs")]
        [SerializeField] private GameObject _intercomTerminalPrefab;
        [SerializeField] private GameObject _hoistTerminalPrefab;
        [SerializeField] private GameObject _enemyNestMarkerPrefab;
        [SerializeField] private GameObject _supplyCacheMarkerPrefab;
        [SerializeField] private GameObject _blackMarketMarkerPrefab;

        [Header("Enemy Spawning")]
        /// <summary>Spawns enemies after each floor is generated using the threat budget system.</summary>
        [SerializeField] private EnemySpawner _enemySpawner;

        [Header("Event Channels — Raise")]
        [SerializeField] private GameEventSO_Int _onPlayerFloorChanged;

        // ── Private state ──────────────────────────────────────────────────────

        private int                 _usedSeed;
        private RoomPlacementResult _placementResult;

        /// <summary>Tracks terminal/marker GameObjects spawned per floor so they can be cleared on regeneration.</summary>
        private readonly List<GameObject> _spawnedTerminals = new();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (!ValidateReferences()) return;
            DoFloorTransition();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the world-space centre of the player spawn room.
        /// <see cref="PlayerController"/> calls this in Start() to position the player.
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            return _mineGrid.GridToWorld(_placementResult.spawnCentre.x, _placementResult.spawnCentre.y);
        }

        /// <summary>
        /// Clears all floor-scoped objects, increments floor depth, and regenerates the cave.
        /// Called by <see cref="DeepShift.UI.HoistChoiceUI"/> when the player chooses to go deeper.
        /// </summary>
        public void AdvanceToNextFloor()
        {
            CleanupFloorObjects();
            _floorDepth++;
            DoFloorTransition();
        }

        /// <summary>Destroys all floor-scoped GameObjects: enemies, spawned terminals, ore pickups, and floating text.</summary>
        private void CleanupFloorObjects()
        {
            _enemySpawner?.ClearSpawnedEnemies();

            foreach (var go in _spawnedTerminals)
                if (go != null) Destroy(go);
            _spawnedTerminals.Clear();

            foreach (var pickup in FindObjectsByType<OrePickup>(FindObjectsSortMode.None))
                Destroy(pickup.gameObject);

            foreach (var proj in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                Destroy(proj.gameObject);

            foreach (var text in FindObjectsByType<DeepShift.UI.FloatingText>(FindObjectsSortMode.None))
                Destroy(text.gameObject);
        }

        /// <summary>Generates the floor, teleports the player, and raises <see cref="_onPlayerFloorChanged"/>.</summary>
        private void DoFloorTransition()
        {
            GenerateFloor();

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                player.TeleportTo(GetSpawnPosition());

            _onPlayerFloorChanged?.Raise(_floorDepth);
            Debug.Log($"[MineTestBootstrap] Floor {_floorDepth} ready.");
        }

        /// <summary>
        /// Runs CA cave generation, room placement, tile stamping, and terminal spawning.
        /// Updates <see cref="_placementResult"/> with the new layout.
        /// </summary>
        private void GenerateFloor()
        {
            var _sw = System.Diagnostics.Stopwatch.StartNew();
            int w = _mineGrid.Width;
            int h = _mineGrid.Height;

            // ── Generate cave ──────────────────────────────────────────────────
            bool[,] caveMap   = null;
            bool    connected = false;
            int     attempt   = 0;

            Vector2Int tempSpawn    = new Vector2Int(w / 2, h / 2);
            Vector2Int tempIntercom = ClampToInterior(tempSpawn + new Vector2Int(0, -5), w, h);

            while (!connected && attempt < _maxAttempts)
            {
                int seed  = _useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : _fixedSeed;
                _usedSeed = seed;

                caveMap   = CaveGenerator.Generate(w, h, tempSpawn, tempIntercom, _fillChance, _smoothPasses, seed);
                connected = CaveGenerator.IsConnected(caveMap, w, h, tempSpawn, tempIntercom);

                Debug.Log($"[MineTestBootstrap] Attempt {attempt + 1} — seed: {seed}, connected: {connected}");
                attempt++;
            }

            if (!connected)
                Debug.LogWarning("[MineTestBootstrap] Temporary intercom unreachable after all attempts.");

            // ── Room placement pass (operates on caveMap only) ─────────────────
            _placementResult = RoomPlacer.PlaceRooms(caveMap, w, h, _floorDepth, _usedSeed);

            Debug.Log($"[Perf] CaveGen+Rooms: {_sw.ElapsedMilliseconds}ms");
            _sw.Restart();

            // ── Apply cave map to MineGrid ─────────────────────────────────────
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

            // ── Sealed room enforcement ────────────────────────────────────────
            foreach (var room in _placementResult.rooms)
            {
                if (!room.isSealed) continue;
                var b = room.bounds;
                for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                {
                    bool onBorder = (x == b.xMin || x == b.xMax - 1 ||
                                     y == b.yMin || y == b.yMax - 1);
                    if (onBorder)
                        _mineGrid.SetTile(x, y, _defaultRockTile);
                    else
                        _mineGrid.SetTile(x, y, _floorTile);
                }
            }

            // ── Gas tile override ──────────────────────────────────────────────
            if (_gasTile != null)
            {
                foreach (var room in _placementResult.rooms)
                {
                    if (room.type != RoomType.GasPocket) continue;
                    var b = room.bounds;
                    for (int x = b.xMin + 1; x < b.xMax - 1; x++)
                    for (int y = b.yMin + 1; y < b.yMax - 1; y++)
                        _mineGrid.SetTile(x, y, _gasTile);
                }
            }

            // ── False wall cell on BlackMarketDeadDrop ─────────────────────────
            if (_falseWallTile != null)
            {
                foreach (var room in _placementResult.rooms)
                {
                    if (room.type != RoomType.BlackMarketDeadDrop) continue;
                    Vector2Int cell = PickBorderCellNearestGridCentre(room.bounds, w, h);
                    _mineGrid.SetTile(cell.x, cell.y, _falseWallTile);
                    break;
                }
            }

            // ── OreChamber ore density ─────────────────────────────────────────
            if (_oreChamberOreTiles != null && _oreChamberOreTiles.Length > 0)
            {
                Random.InitState(_usedSeed ^ 0x7A3F);
                foreach (var room in _placementResult.rooms)
                {
                    if (room.type != RoomType.OreChamber) continue;
                    var b = room.bounds;
                    for (int x = b.xMin + 1; x < b.xMax - 1; x++)
                    for (int y = b.yMin + 1; y < b.yMax - 1; y++)
                    {
                        if (Random.value < 0.60f)
                            _mineGrid.SetTile(x, y, _oreChamberOreTiles[Random.Range(0, _oreChamberOreTiles.Length)]);
                    }
                }
            }

            // ── Scatter ore in solid rock cells only ───────────────────────────
            if (_oreRockTiles != null && _oreRockTiles.Length > 0)
            {
                Random.InitState(_usedSeed);

                for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                {
                    if (!caveMap[x, y])                                        continue; // skip open space
                    if (IsNearSpawn(x, y, _placementResult.spawnCentre))       continue; // keep spawn clear
                    if (Random.value >= _oreSpawnChance)                        continue;

                    TileDataSO ore = _oreRockTiles[Random.Range(0, _oreRockTiles.Length)];
                    _mineGrid.SetTile(x, y, ore);
                }
            }

            // ── Spawn terminal GameObjects ─────────────────────────────────────
            foreach (var room in _placementResult.rooms)
            {
                switch (room.type)
                {
                    case RoomType.IntercomTerminal:
                        SpawnTerminal(_intercomTerminalPrefab, room.centre);
                        break;
                    case RoomType.HoistTerminal:
                        SpawnTerminal(_hoistTerminalPrefab, room.centre);
                        break;
                    case RoomType.EnemyNest:
                        SpawnTerminal(_enemyNestMarkerPrefab, room.centre);
                        break;
                    case RoomType.SupplyCache:
                        SpawnTerminal(_supplyCacheMarkerPrefab, room.centre);
                        break;
                    case RoomType.BlackMarketDeadDrop:
                        SpawnTerminal(_blackMarketMarkerPrefab, room.centre);
                        break;
                }
            }

            Debug.Log($"[MineTestBootstrap] Floor {_floorDepth} ready — {w}×{h}, seed: {_usedSeed}, " +
                      $"spawn: {_placementResult.spawnCentre}, " +
                      $"intercom: {_placementResult.intercomCentre}, " +
                      $"hoist: {_placementResult.hoistCentre}");

            Debug.Log($"[Perf] Grid+SetTile: {_sw.ElapsedMilliseconds}ms");
            _sw.Restart();

            // ── Wang autotile pass — must run after all SetTile calls ──────────
            _mineGrid.RefreshAllWangVisuals();
            Debug.Log($"[Perf] WangRefresh: {_sw.ElapsedMilliseconds}ms");
            _sw.Restart();

            // ── Enemy spawning ─────────────────────────────────────────────────
            _enemySpawner?.SpawnEnemies(
                _floorDepth,
                _mineGrid,
                _placementResult.spawnCentre,
                _placementResult.hoistCentre);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private void SpawnTerminal(GameObject prefab, Vector2Int cell)
        {
            if (prefab == null) return;
            var go = Instantiate(prefab, _mineGrid.GridToWorld(cell.x, cell.y), Quaternion.identity);
            _spawnedTerminals.Add(go);
        }

        private static Vector2Int PickBorderCellNearestGridCentre(RectInt bounds, int gridW, int gridH)
        {
            Vector2Int gridCentre = new Vector2Int(gridW / 2, gridH / 2);
            Vector2Int best       = new Vector2Int(bounds.xMin, bounds.yMin);
            float      bestDist   = float.MaxValue;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                bool onBorder = (x == bounds.xMin || x == bounds.xMax - 1 ||
                                 y == bounds.yMin || y == bounds.yMax - 1);
                if (!onBorder) continue;
                float d = Vector2Int.Distance(new Vector2Int(x, y), gridCentre);
                if (d < bestDist) { bestDist = d; best = new Vector2Int(x, y); }
            }
            return best;
        }

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
                Debug.LogError("[MineTestBootstrap] floorTile is not assigned.");
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
