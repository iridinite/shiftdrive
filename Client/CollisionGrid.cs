/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a grid-based space partitioning system.
    /// </summary>
    internal class CollisionGrid {

        private const int SUBDIVISIONS = 20;
        private readonly List<GameObject>[,] grid;

        public CollisionGrid() {
            // initialize grid
            grid = new List<GameObject>[SUBDIVISIONS, SUBDIVISIONS];
            for (int x = 0; x < SUBDIVISIONS; x++)
                for (int y = 0; y < SUBDIVISIONS; y++)
                    grid[x, y] = new List<GameObject>();
        }

        /// <summary>
        /// Updates the positions of objects in the grid.
        /// </summary>
        public void Update() {
            // iterate through all cells
            for (int x = 0; x < SUBDIVISIONS; x++) {
                for (int y = 0; y < SUBDIVISIONS; y++) {
                    // iterate through each object in this cell
                    for (int i = grid[x, y].Count - 1; i >= 0; i--) {
                        GameObject obj = grid[x, y][i];
                        // remove objects that have been scheduled for deletion
                        if (obj.IsDestroyScheduled()) {
                            grid[x, y].RemoveAt(i);
                            continue;
                        }
                        // recalculate this object's position
                        int cellx, celly;
                        GetCell(obj, out cellx, out celly);
                        // if object left this cell, reinsert it
                        if (cellx == x && celly == y) continue;
                        grid[x, y].RemoveAt(i);
                        Insert(obj, cellx, celly);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the cell coordinates for a <see cref="GameObject"/> and then inserts it into the grid.
        /// </summary>
        /// <param name="obj">The <see cref="GameObject"/> to insert.</param>
        public void Insert(GameObject obj) {
            int cellx, celly;
            GetCell(obj, out cellx, out celly);
            Insert(obj, cellx, celly);
        }

        /// <summary>
        /// Inserts a <see cref="GameObject"/> at the specified cell coordinates.
        /// </summary>
        /// <param name="obj">The <see cref="GameObject"/> to insert.</param>
        /// <param name="x">The x-coordinate in the cell array.</param>
        /// <param name="y">The y-coordinate in the cell array.</param>
        public void Insert(GameObject obj, int x, int y) {
            grid[x, y].Add(obj);
        }

        /// <summary>
        /// Removes the specified <see cref="GameObject"/> from the grid.
        /// </summary>
        public void Remove(GameObject obj) {
            // iterate through all cells
            for (int x = 0; x < SUBDIVISIONS; x++)
                for (int y = 0; y < SUBDIVISIONS; y++)
                    // find objects with matching IDs in each cell
                    for (int i = grid[x, y].Count - 1; i >= 0; i--)
                        if (grid[x, y][i].id == obj.id) grid[x, y].RemoveAt(i);
        }

        /// <summary>
        /// Calculate the cell coordinates for a <see cref="GameObject"/>.
        /// </summary>
        public void GetCell(GameObject obj, out int x, out int y) {
            x = MathHelper.Clamp((int)(obj.position.X / NetServer.MAPSIZE * SUBDIVISIONS), 0, SUBDIVISIONS - 1);
            y = MathHelper.Clamp((int)(obj.position.Y / NetServer.MAPSIZE * SUBDIVISIONS), 0, SUBDIVISIONS - 1);
        }

        /// <summary>
        /// Gets a list of GameObjects near the specified GameObject's cell.
        /// </summary>
        public List<GameObject> Query(GameObject obj) {
            // TODO: This needs improvement. Lists and AddRange are quite slow.
            // combine the lists of all objects in this object's cell, as well as the
            // eight adjacent cells
            List<GameObject> ret = new List<GameObject>(128);
            int x, y;
            GetCell(obj, out x, out y);

            // x - 1
            if (x > 0 && y > 0)
                ret.AddRange(grid[x - 1, y - 1]);
            if (x > 0)
                ret.AddRange(grid[x - 1, y]);
            if (x > 0 && y < SUBDIVISIONS - 1)
                ret.AddRange(grid[x - 1, y + 1]);

            // x
            if (y > 0)
                ret.AddRange(grid[x, y - 1]);
            ret.AddRange(grid[x, y]);
            if (y < SUBDIVISIONS - 1)
                ret.AddRange(grid[x, y + 1]);

            // x + 1
            if (x < SUBDIVISIONS - 1 && y > 0)
                ret.AddRange(grid[x + 1, y - 1]);
            if (x < SUBDIVISIONS - 1)
                ret.AddRange(grid[x + 1, y]);
            if (x < SUBDIVISIONS - 1 && y < SUBDIVISIONS - 1)
                ret.AddRange(grid[x + 1, y + 1]);

            return ret;
        }

    }

}
