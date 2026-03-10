using System.Collections.Generic;
using UnityEngine;

namespace DeepShift.Mining
{
    /// <summary>
    /// Cellular automata cave generator. Produces a boolean map where <c>true</c> = solid rock
    /// and <c>false</c> = open space. Borders are always solid. Designed for use with
    /// <see cref="MineGrid"/> via <see cref="MineTestBootstrap"/>.
    /// </summary>
    public static class CaveGenerator
    {
        /// <summary>
        /// Number of rock neighbours (8-directional) at or above which a cell becomes/stays rock
        /// during a smoothing pass. Out-of-bounds cells count as rock.
        /// </summary>
        private const int BirthThreshold = 5;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a cave map for a grid of size (<paramref name="width"/> × <paramref name="height"/>).
        /// Steps: random fill → clear spawn area → smooth → carve L-corridor → re-clear spawn area.
        /// </summary>
        /// <param name="width">Grid width (columns).</param>
        /// <param name="height">Grid height (rows).</param>
        /// <param name="spawn">Player spawn cell in grid coordinates.</param>
        /// <param name="intercom">Intercom terminal cell in grid coordinates.</param>
        /// <param name="fillChance">Probability [0–1] that a cell starts as rock (recommended 0.45).</param>
        /// <param name="smoothPasses">Number of CA smoothing iterations (recommended 4).</param>
        /// <param name="seed">Deterministic random seed.</param>
        /// <returns>Bool map: <c>true</c> = rock, <c>false</c> = open space.</returns>
        public static bool[,] Generate(
            int        width,
            int        height,
            Vector2Int spawn,
            Vector2Int intercom,
            float      fillChance,
            int        smoothPasses,
            int        seed)
        {
            var rng  = new System.Random(seed);
            var rock = RandomFill(width, height, fillChance, spawn, rng);

            for (int i = 0; i < smoothPasses; i++)
                rock = SmoothPass(rock, width, height);

            CarveCorridorL(rock, spawn, intercom);

            // Re-clear spawn area — smoothing may have partially filled it back in
            ClearArea(rock, spawn, radius: 1);

            return rock;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="target"/> is reachable from <paramref name="start"/>
        /// through open-space cells, using a BFS flood fill over cardinal neighbours.
        /// </summary>
        public static bool IsConnected(
            bool[,]    rock,
            int        width,
            int        height,
            Vector2Int start,
            Vector2Int target)
        {
            if (rock[start.x, start.y] || rock[target.x, target.y]) return false;

            var visited = new bool[width, height];
            var queue   = new Queue<Vector2Int>();

            queue.Enqueue(start);
            visited[start.x, start.y] = true;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell == target) return true;

                foreach (var n in CardinalNeighbours(cell, width, height))
                {
                    if (visited[n.x, n.y] || rock[n.x, n.y]) continue;
                    visited[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }

            return false;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static bool[,] RandomFill(
            int        width,
            int        height,
            float      fillChance,
            Vector2Int spawn,
            System.Random rng)
        {
            var rock = new bool[width, height];

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                // Borders are always solid
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    rock[x, y] = true;
                    continue;
                }

                rock[x, y] = rng.NextDouble() < fillChance;
            }

            // Guarantee a clear spawn room before smoothing
            ClearArea(rock, spawn, radius: 1);

            return rock;
        }

        private static bool[,] SmoothPass(bool[,] rock, int width, int height)
        {
            var next = new bool[width, height];

            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    next[x, y] = true; // borders always stay solid
                    continue;
                }

                int neighbours = RockNeighbourCount(rock, x, y, width, height);
                next[x, y]     = neighbours >= BirthThreshold;
            }

            return next;
        }

        private static int RockNeighbourCount(bool[,] rock, int x, int y, int width, int height)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx, ny = y + dy;

                // Out-of-bounds cells count as rock — thickens cave borders naturally
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    count++;
                else if (rock[nx, ny])
                    count++;
            }
            return count;
        }

        private static void ClearArea(bool[,] rock, Vector2Int centre, int radius)
        {
            int w = rock.GetLength(0), h = rock.GetLength(1);
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int nx = centre.x + dx, ny = centre.y + dy;
                // Never clear the outermost border row/column
                if (nx > 0 && nx < w - 1 && ny > 0 && ny < h - 1)
                    rock[nx, ny] = false;
            }
        }

        /// <summary>
        /// Carves an L-shaped corridor: horizontal from <paramref name="from"/> to the
        /// column of <paramref name="to"/>, then vertical to <paramref name="to"/>.
        /// </summary>
        private static void CarveCorridorL(bool[,] rock, Vector2Int from, Vector2Int to)
        {
            int w = rock.GetLength(0), h = rock.GetLength(1);

            // Horizontal leg: walk from.x → to.x along y = from.y
            int stepX = to.x >= from.x ? 1 : -1;
            for (int x = from.x; x != to.x + stepX; x += stepX)
                CarveCell(rock, x, from.y, w, h);

            // Vertical leg: walk from.y → to.y along x = to.x
            int stepY = to.y >= from.y ? 1 : -1;
            for (int y = from.y; y != to.y + stepY; y += stepY)
                CarveCell(rock, to.x, y, w, h);
        }

        private static void CarveCell(bool[,] rock, int x, int y, int w, int h)
        {
            if (x > 0 && x < w - 1 && y > 0 && y < h - 1)
                rock[x, y] = false;
        }

        private static IEnumerable<Vector2Int> CardinalNeighbours(Vector2Int cell, int width, int height)
        {
            if (cell.x > 0)          yield return new Vector2Int(cell.x - 1, cell.y);
            if (cell.x < width  - 1) yield return new Vector2Int(cell.x + 1, cell.y);
            if (cell.y > 0)          yield return new Vector2Int(cell.x, cell.y - 1);
            if (cell.y < height - 1) yield return new Vector2Int(cell.x, cell.y + 1);
        }
    }
}
