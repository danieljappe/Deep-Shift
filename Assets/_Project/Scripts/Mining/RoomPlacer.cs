using System.Collections.Generic;
using UnityEngine;

namespace DeepShift.Mining
{
    public enum RoomType
    {
        SpawnRoom,
        IntercomTerminal,
        HoistTerminal,
        OreChamber,
        EnemyNest,
        GasPocket,
        SupplyCache,
        BlackMarketDeadDrop
    }

    public struct PlacedRoom
    {
        /// <summary>The type of room placed.</summary>
        public RoomType   type;

        /// <summary>Full footprint including 1-cell border ring.</summary>
        public RectInt    bounds;

        /// <summary>Centre of interior (for GO spawning).</summary>
        public Vector2Int centre;

        /// <summary>True if the room is sealed (walls not opened into cave).</summary>
        public bool       isSealed;
    }

    public struct RoomPlacementResult
    {
        public PlacedRoom[] rooms;
        public Vector2Int   spawnCentre;
        public Vector2Int   intercomCentre;
        public Vector2Int   hoistCentre;
        public bool         hasBlackMarket;
    }

    /// <summary>
    /// Pure static utility that stamps typed rooms into a boolean cave map and returns
    /// placement metadata. All rooms are scattered freely through the cave — no zone grid.
    /// </summary>
    public static class RoomPlacer
    {
        private static readonly Dictionary<RoomType, Vector2Int> s_interiorSizes
            = new Dictionary<RoomType, Vector2Int>
        {
            { RoomType.SpawnRoom,           new Vector2Int(3, 3) },
            { RoomType.IntercomTerminal,    new Vector2Int(4, 4) },
            { RoomType.HoistTerminal,       new Vector2Int(4, 4) },
            { RoomType.OreChamber,          new Vector2Int(5, 5) },
            { RoomType.EnemyNest,           new Vector2Int(4, 4) },
            { RoomType.GasPocket,           new Vector2Int(5, 5) },
            { RoomType.SupplyCache,         new Vector2Int(3, 3) },
            { RoomType.BlackMarketDeadDrop, new Vector2Int(3, 3) },
        };

        private static bool IsSealed(RoomType t) =>
            t == RoomType.SupplyCache || t == RoomType.BlackMarketDeadDrop;

        public static RoomPlacementResult PlaceRooms(
            bool[,] caveMap, int gridWidth, int gridHeight, int floorDepth, int seed)
        {
            var rng = new System.Random(seed);

            // ── 1. Spawn room — always at grid centre ────────────────────────────
            Vector2Int spawnCell = new Vector2Int(gridWidth / 2, gridHeight / 2);
            PlacedRoom spawnRoom = MakeRoom(RoomType.SpawnRoom, spawnCell, gridWidth, gridHeight);
            StampInterior(caveMap, spawnRoom.bounds, gridWidth, gridHeight);

            var placedRooms   = new List<PlacedRoom> { spawnRoom };
            var placedCentres = new List<Vector2Int>  { spawnRoom.centre };

            // ── 2. Build the room list to scatter ────────────────────────────────
            // Terminals always included; filler rooms shuffled each seed.
            var toPlace = new List<RoomType>
            {
                RoomType.IntercomTerminal,
                RoomType.HoistTerminal,
                RoomType.EnemyNest,
                RoomType.GasPocket,
                RoomType.SupplyCache,
                RoomType.OreChamber,
            };
            if (rng.NextDouble() < 0.50) toPlace.Add(RoomType.OreChamber);
            if (rng.NextDouble() < 0.20) toPlace.Add(RoomType.BlackMarketDeadDrop);

            Shuffle(toPlace, rng);

            // ── 3. Place each room in a random open cell ─────────────────────────
            // Terminals require 30 tiles from spawn; others just need separation from
            // already-placed rooms.
            bool hasBlackMarket    = false;
            Vector2Int intercomCentre = spawnRoom.centre;
            Vector2Int hoistCentre    = spawnRoom.centre;

            foreach (var rt in toPlace)
            {
                bool isTerminal = rt == RoomType.IntercomTerminal || rt == RoomType.HoistTerminal;
                int  minFromSpawn = isTerminal ? 30 : 5;

                Vector2Int cell = FindOpenCell(
                    caveMap, gridWidth, gridHeight,
                    spawnRoom.centre, minFromSpawn,
                    placedCentres, minSeparation: 6,
                    rng);

                Vector2Int size = s_interiorSizes[rt];
                PlacedRoom room = MakeRoom(rt, cell, gridWidth, gridHeight);

                if (!room.isSealed)
                    StampInterior(caveMap, room.bounds, gridWidth, gridHeight);

                placedRooms.Add(room);
                placedCentres.Add(room.centre);

                if (rt == RoomType.IntercomTerminal)    intercomCentre = room.centre;
                if (rt == RoomType.HoistTerminal)       hoistCentre    = room.centre;
                if (rt == RoomType.BlackMarketDeadDrop) hasBlackMarket = true;
            }

            // ── 4. Carve corridors between all non-sealed rooms ──────────────────
            var nonSealed = new List<PlacedRoom>();
            foreach (var r in placedRooms)
                if (!r.isSealed) nonSealed.Add(r);

            if (nonSealed.Count > 1)
                CarveCorridors(caveMap, gridWidth, gridHeight, nonSealed, placedRooms, rng);

            return new RoomPlacementResult
            {
                rooms          = placedRooms.ToArray(),
                spawnCentre    = spawnRoom.centre,
                intercomCentre = intercomCentre,
                hoistCentre    = hoistCentre,
                hasBlackMarket = hasBlackMarket,
            };
        }

        // ── Room construction ─────────────────────────────────────────────────────

        private static PlacedRoom MakeRoom(RoomType rt, Vector2Int centre, int gridW, int gridH)
        {
            Vector2Int size = s_interiorSizes[rt];
            int iW = size.x, iH = size.y;
            int ax = Mathf.Clamp(centre.x - iW / 2 - 1, 1, gridW - 2 - (iW + 2));
            int ay = Mathf.Clamp(centre.y - iH / 2 - 1, 1, gridH - 2 - (iH + 2));
            return new PlacedRoom
            {
                type     = rt,
                bounds   = new RectInt(ax, ay, iW + 2, iH + 2),
                centre   = new Vector2Int(ax + 1 + iW / 2, ay + 1 + iH / 2),
                isSealed = IsSealed(rt),
            };
        }

        private static void StampInterior(bool[,] caveMap, RectInt b, int w, int h)
        {
            for (int x = b.xMin + 1; x < b.xMax - 1; x++)
            for (int y = b.yMin + 1; y < b.yMax - 1; y++)
                if (x > 0 && x < w - 1 && y > 0 && y < h - 1)
                    caveMap[x, y] = false;
        }

        // ── Candidate cell search ─────────────────────────────────────────────────

        /// <summary>
        /// Returns a random open cell (caveMap == false) that is at least
        /// <paramref name="minFromSpawn"/> tiles from spawn and at least
        /// <paramref name="minSeparation"/> tiles from every already-placed room centre.
        /// Relaxes both thresholds progressively if no candidates are found.
        /// </summary>
        private static Vector2Int FindOpenCell(
            bool[,] caveMap, int w, int h,
            Vector2Int spawn, int minFromSpawn,
            List<Vector2Int> occupied, int minSeparation,
            System.Random rng)
        {
            for (int spawnDist = minFromSpawn; spawnDist >= 3; spawnDist -= 3)
            for (int sep = minSeparation; sep >= 2; sep -= 2)
            {
                var candidates = new List<Vector2Int>();
                for (int x = 2; x < w - 2; x++)
                for (int y = 2; y < h - 2; y++)
                {
                    if (caveMap[x, y]) continue;
                    var cell = new Vector2Int(x, y);
                    if (Vector2Int.Distance(cell, spawn) < spawnDist) continue;
                    bool tooClose = false;
                    foreach (var oc in occupied)
                        if (Vector2Int.Distance(cell, oc) < sep) { tooClose = true; break; }
                    if (!tooClose) candidates.Add(cell);
                }
                if (candidates.Count > 0)
                {
                    if (spawnDist < minFromSpawn)
                        Debug.LogWarning($"[RoomPlacer] Distance relaxed to {spawnDist}/{sep} (grid too small).");
                    return candidates[rng.Next(candidates.Count)];
                }
            }

            Debug.LogWarning("[RoomPlacer] No suitable cell found — falling back to grid centre.");
            return new Vector2Int(w / 2, h / 2);
        }

        // ── Corridor carving ──────────────────────────────────────────────────────

        private static void CarveCorridors(
            bool[,] caveMap, int w, int h,
            List<PlacedRoom> nonSealed, List<PlacedRoom> allRooms,
            System.Random rng)
        {
            var connected = new HashSet<Vector2Int>();
            connected.Add(nonSealed[0].centre);

            var remaining = new List<PlacedRoom>(nonSealed);
            remaining.RemoveAt(0);

            var sealedBounds = new List<RectInt>();
            foreach (var r in allRooms)
                if (r.isSealed) sealedBounds.Add(r.bounds);

            while (remaining.Count > 0)
            {
                PlacedRoom best     = remaining[0];
                Vector2Int bestFrom = Vector2Int.zero;
                float      bestDist = float.MaxValue;

                foreach (var candidate in remaining)
                foreach (var cc in connected)
                {
                    float d = Vector2Int.Distance(candidate.centre, cc);
                    if (d < bestDist) { bestDist = d; best = candidate; bestFrom = cc; }
                }

                CarveCorridor(caveMap, w, h, bestFrom, best.centre, sealedBounds, rng);
                connected.Add(best.centre);
                remaining.Remove(best);
            }
        }

        private static void CarveCorridor(
            bool[,] caveMap, int w, int h,
            Vector2Int from, Vector2Int to,
            List<RectInt> sealedBounds, System.Random rng)
        {
            int minX = Mathf.Min(from.x, to.x), maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y), maxY = Mathf.Max(from.y, to.y);

            var wp1 = new Vector2Int(
                minX == maxX ? from.x : rng.Next(minX, maxX + 1),
                minY == maxY ? from.y : rng.Next(minY, maxY + 1));
            var wp2 = new Vector2Int(
                minX == maxX ? to.x   : rng.Next(minX, maxX + 1),
                minY == maxY ? to.y   : rng.Next(minY, maxY + 1));

            CarveLine(caveMap, w, h, from, wp1, sealedBounds);
            CarveLine(caveMap, w, h, wp1,  wp2, sealedBounds);
            CarveLine(caveMap, w, h, wp2,  to,  sealedBounds);
        }

        private static void CarveLine(
            bool[,] caveMap, int w, int h,
            Vector2Int from, Vector2Int to,
            List<RectInt> sealedBounds)
        {
            int x = from.x, y = from.y;
            int dx = to.x > x ? 1 : to.x < x ? -1 : 0;
            int dy = to.y > y ? 1 : to.y < y ? -1 : 0;

            while (x != to.x)
            {
                if (IsSafeCarve(x, y, w, h, sealedBounds)) caveMap[x, y] = false;
                x += dx;
            }
            while (y != to.y)
            {
                if (IsSafeCarve(x, y, w, h, sealedBounds)) caveMap[x, y] = false;
                y += dy;
            }
            if (IsSafeCarve(to.x, to.y, w, h, sealedBounds)) caveMap[to.x, to.y] = false;
        }

        private static bool IsSafeCarve(int x, int y, int w, int h, List<RectInt> sealedBounds)
        {
            if (x == 0 || x == w - 1 || y == 0 || y == h - 1) return false;
            foreach (var b in sealedBounds)
            {
                bool onBorder = x >= b.xMin && x < b.xMax &&
                                y >= b.yMin && y < b.yMax &&
                                (x == b.xMin || x == b.xMax - 1 ||
                                 y == b.yMin || y == b.yMax - 1);
                if (onBorder) return false;
            }
            return true;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
