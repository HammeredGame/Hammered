using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HammeredGame.Game.PathPlanning.AStar;
using HammeredGame.Game.PathPlanning.AStar.GraphComponents;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;

namespace HammeredGame.Game.PathPlanning.Grid
{
    public class UniformGrid
    {
        private Vector3[, ,] grid;
        private bool[,,] mask;

        private BidirectionalDictionary<Vector3, Vertex> biMap  = new BidirectionalDictionary<Vector3, Vertex>();
        private Graph graph;

        // The following three fields can uniquely define a uniform grid.
        public float sideLength { get; private set; }
        public Vector3 originPoint { get; private set; }
        public Vector3 endPoint { get; private set; }

        /// <remarks>
        /// The current implementations of the constructors suggest that the point characterizing a cell is its "bottom-left" one,
        /// instead of the usual convention, where the point characterizing the cell is that at the (cubic) cell's center.
        /// 
        /// WARNING: There is a possibility that this convention might cause some visual anomalies in coarse grids.
        /// </remarks>

        /// <summary>
        /// Constructs a uniform 3D grid with the first grid cell positioned at the origin O(0,0,0) and expanding towards the positive
        /// direction of each axis (strictly monotonically increasing X, Y and Z).
        /// </summary>
        /// <param name="nrCellsX">Number of cells in the X dimension. Expecting a non-negative integer.</param>
        /// <param name="nrCellsY">Number of cells in the Y dimension. Expecting a non-negative integer.</param>
        /// <param name="nrCellsZ">Number of cells in the Z dimension. Expecting a non-negative integer.</param>
        /// <param name="sideLength">The length of each side of a cell. Cells are considered to be perfect cubes.</param>
        public UniformGrid(int nrCellsX, int nrCellsY, int nrCellsZ, float sideLength)
        {
            originPoint = Vector3.Zero;
            endPoint = new Vector3(nrCellsX * sideLength, nrCellsY * sideLength, nrCellsZ * sideLength);
            this.sideLength = sideLength;

            grid = new Vector3[Math.Max(1, nrCellsX), Math.Max(1, nrCellsY), Math.Max(1, nrCellsZ)];
            mask = new bool[grid.GetLength(0), grid.GetLength(1), grid.GetLength(2)];
            // STEP 1: Define all points in 3D space and map them to corresponding vertices.
            FillBidirectionalMapping(originPoint, this.sideLength);
            // STEP 2: All cells are considered to be free during (this) initialization.
            MarkAllCellsAsFree();
            // STEP 3: Define the connections between the vertices.
            MakeVerticesConnections();

        }

        /// <summary>
        /// Constructs a uniform 3D cube grid with the the first grid cell positioned at the origin O(0,0,0)
        /// and expanding towards the positive of each axis (strictly monotonically increasing X, Y and Z).
        /// </summary>
        /// <param name="nrCellsPerDimension">Number of cells in each of the X, Y and Z dimensions. Expecting a non-negative integer</param>
        /// <param name="sideLength">The length of each side of a cell. Cells are considered to be perfect cubes.</param>
        public UniformGrid(int nrCellsPerDimension, float sideLength) : this(nrCellsPerDimension, nrCellsPerDimension, nrCellsPerDimension, sideLength)
        {
        }

        public UniformGrid(float fullCubeLengthPerDimension, float sideLength) : this((int)Math.Ceiling(fullCubeLengthPerDimension / sideLength), sideLength)
        {
        }

        public UniformGrid(Vector3 topRightAwayPoint, float sideLength) : this(Vector3.Zero, topRightAwayPoint, sideLength)
        {
        }

        public UniformGrid(Vector3 bottomLeftClosePoint, Vector3 topRightAwayPoint, float sideLength)
        {
            if (bottomLeftClosePoint.X > topRightAwayPoint.X)
                throw new ArgumentException(String.Format("First input must be on the left of the second input. Instead, {0} > {1} was provided", bottomLeftClosePoint.X, topRightAwayPoint.X));
            if (bottomLeftClosePoint.Y > topRightAwayPoint.Y)
                throw new ArgumentException(String.Format("First input must be below the second input. Instead, {0} > {1} was provided", bottomLeftClosePoint.Y, topRightAwayPoint.Y));
            if (bottomLeftClosePoint.Z > topRightAwayPoint.Z)
                throw new ArgumentException(String.Format("First input must be closer to origin than the second input. Instead, {0} > {1} was provided", bottomLeftClosePoint.Z, topRightAwayPoint.Z));

            originPoint = bottomLeftClosePoint;
            endPoint = topRightAwayPoint;
            this.sideLength = sideLength;

            int nrCellsX = (int)Math.Ceiling(Math.Abs(bottomLeftClosePoint.X - topRightAwayPoint.X) / sideLength);
            int nrCellsY = (int)Math.Ceiling(Math.Abs(bottomLeftClosePoint.Y - topRightAwayPoint.Y) / sideLength);
            int nrCellsZ = (int)Math.Ceiling(Math.Abs(bottomLeftClosePoint.Z - topRightAwayPoint.Z) / sideLength);

            grid = new Vector3[Math.Max(1, nrCellsX), Math.Max(1, nrCellsY), Math.Max(1, nrCellsZ)];
            mask = new bool[grid.GetLength(0), grid.GetLength(1), grid.GetLength(2)];
            // STEP 1: Define all points in 3D space and map them to corresponding vertices.
            FillBidirectionalMapping(originPoint, this.sideLength);
            // STEP 2: All cells are considered to be free during (this) initialization.
            MarkAllCellsAsFree();
            // STEP 3: Define the connections between the vertices.
            MakeVerticesConnections();
        }

        public int[] GetDimensions() { return new int[3] { grid.GetLength(0), grid.GetLength(1), grid.GetLength(2) }; }

        /// <summary>
        /// This function returns the cell index  [x_index, y_index, z_index] in which the arbitray 3D input point resides in. 
        /// </summary>
        /// <param name="position">A position in 3D (or lower) space.</param>
        /// <returns>
        /// An array of ints (int[3]), where int[0] -> x index, int[1] -> y index, and int[2] -> z index.
        /// The grid is zero-indexed.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The point is supposed to be inside the grid. If it is not, an "ArgumentException" is thrown, with its text message
        /// giving specifics on which dimension of the provided input is out of bounds.
        /// </exception>
        /// <remarks>
        /// The current implementation suggests that the point characterizing a cell is its "bottom-left" one,
        /// instead of the usual convention, where the point characterizing the cell is that at the (cubic) cell's center.
        /// 
        /// WARNING: There is a possibility that this convention might cause some visual anomalies in coarse grids.
        /// </remarks>
        public uint[] GetCellIndex(Vector3 position)
        {
            if (position.X < originPoint.X || position.Y < originPoint.Y || position.Z < originPoint.Z)
                throw new ArgumentException("The provided position is outside the grid.");

            if (position.X < originPoint.X || position.X > originPoint.X + grid.GetLength(0) * sideLength)
                throw new ArgumentException(String.Format("The provided position's X coordinate is outside the grid." +
                    "grid max X = {0}. {1} was provided instead.", originPoint.X + grid.GetLength(0) * sideLength, position.X));

            if (position.Y < originPoint.Y || position.Y > originPoint.Y + grid.GetLength(1) * sideLength)
                throw new ArgumentException(String.Format("The provided position's Y coordinate is outside the grid." +
                    "grid max Y = {0}. {1} was provided instead.", originPoint.Y + grid.GetLength(1) * sideLength, position.Y));

            if (position.X < originPoint.Z || position.Z > originPoint.Z + grid.GetLength(2) * sideLength)
                throw new ArgumentException(String.Format("The provided position's Z coordinate is outside the grid." +
                    "grid max Z = {0}. {1} was provided instead.", originPoint.Z + grid.GetLength(2) * sideLength, position.Z));

            uint xIndex = (uint)Math.Floor((position.X - originPoint.X) / sideLength);
            uint yIndex = (uint)Math.Floor((position.Y - originPoint.Y) / sideLength);
            uint zIndex = (uint)Math.Floor((position.Z - originPoint.Z) / sideLength);

            uint[] index = new uint[3] { xIndex, yIndex, zIndex };

            return index;

        }

        /// <summary>
        /// Sets the availability of a specific cell of the grid. 
        /// </summary>
        /// <param name="index">
        /// A uint[3] [x_index, y_index, z_index]
        /// denoting the index of the cell whose mask value shall be modified.
        /// </param>
        /// <param name="value">The value which will be assigned to the cell mask:
        /// true denotes "free", while false denotes "unavailable".
        /// </param>
        public void MarkCellAs(uint[] index, bool value) { mask[index[0], index[1], index[2]] = value; }

        /// <summary>
        /// Finds the shortest available path from first input position to the second input by travelling only between a set of provided points.
        /// </summary>
        /// <param name="start">A 3D position in space from where the A* algorithm begins.</param>
        /// <param name="finish">The 3D position which we wish to reach.</param>
        /// <param name="pointsConsidered">
        /// The 3D grid points (i.e. the 3D locations which uniquely identify the cell)
        /// which shall be considered in the search</param>
        /// <returns>
        /// A sequence of 3D positions which consists the shortest path in temporal order.
        /// </returns>
        
        /// <remarks>
        /// 1) HOW IS THE SEQUENCE GENERATED?:
        /// i) the provided starting position is inserted into the list of points
        /// ii) the provided starting position is moved to the closest cell of the grid. This cell will be the start of the
        ///     path planning/routing algorithm --in our case the A* algorithm.
        /// iii) The path planning routine is executed and a sequence of points, starting form the point described in ii)
        ///      and finishing at the grid point in which the real goal point resides in.
        /// iv) the provided finishing/goal position is inserted into the list of points.
        /// </remarks>
        private Vector3[] FindShortestPathAStar(Vector3 start, Vector3 finish, HashSet<Vector3> pointsConsidered)
        {
            uint[] startCellIndex = this.GetCellIndex(start), finishCellIndex = this.GetCellIndex(finish);
            Vector3 startCell = this.grid[startCellIndex[0], startCellIndex[1], startCellIndex[2]];
            Vector3 finishCell = this.grid[finishCellIndex[0], finishCellIndex[1], finishCellIndex[2]];

            HashSet<Vertex> verticesOfGraph = new HashSet<Vertex>();
            // Isolate the vertices which will be taken into consideration for the A* algorithm.
            // Define the heuristic value of each vertex.
            // LEAVE UNCOMMENTED ONLY THE HEURISTIC FUNCTION WHICH WILL BE USED
            foreach (Vector3 point in pointsConsidered)
            {
                Vertex correspondingVertex = biMap.Forward[point];
                verticesOfGraph.Add(correspondingVertex);
                Vector3 differenceVector = point - finishCell;
                //correspondingVertex.HeuristicValue = differenceVector.Length(); // Euclidean distance heuristic function
                //correspondingVertex.HeuristicValue = differenceVector.LengthSquared(); // Squared euclidean distance heuristic function. In hopes of punishing detours.
                correspondingVertex.HeuristicValue = Math.Abs(differenceVector.X) + Math.Abs(differenceVector.Y) + Math.Abs(differenceVector.Z); // Manhattan distance. In hopes of punishing detours even more.

            }

            Stack<Vertex> aResultVertex = AStarAlgorithm.GetMinimumPath(biMap.Forward[startCell], biMap.Forward[finishCell], new Graph(verticesOfGraph));
            // Transform the vertex result (which is independent from any geographical meaning) to a 3D point result.
            int pathLength = aResultVertex.Count();
            Vector3[] shortestPath = new Vector3[1 + pathLength + 1];

            shortestPath[0] = start;
            for (int i = 0; i < pathLength; ++i) { shortestPath[1 + i] = biMap.Reverse[aResultVertex.Pop()]; }
            shortestPath[1 + pathLength] = finish;

            return shortestPath;

        }

        /// <summary>
        /// Finds the shortest available path from first input position to the second input position using an arbitrary mask.
        /// </summary>
        /// <param name="start">A 3D position in space from where the A* algorithm begins</param>
        /// <param name="finish">The 3D position which we wish to reach.</param>
        /// <param name="mask">Flags which indicate which positions can be used during the seach: "true" flags can, while "false" flags cannot.</param>
        /// <returns>A sequence of 3D positions which consists the shortest path in temporal order.</returns>
        /// <exception cref="ArgumentException"></exception>
        public Vector3[] FindShortestPathAStar(Vector3 start, Vector3 finish, bool[, ,] mask)
        {
            if (mask.GetLength(0) != grid.GetLength(0) || mask.GetLength(1) != grid.GetLength(1) || mask.GetLength(2) != grid.GetLength(2))
                throw new ArgumentException("The mask provided is not the same dimensions as the grid");

            HashSet<Vector3> pointsConsidered = new HashSet<Vector3>();

            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int j = 0; j < grid.GetLength(1); ++j)
                {
                    for (int k = 0; k < grid.GetLength(2); ++k)
                    {
                        if (mask[i, j, k]) { pointsConsidered.Add(grid[i, j, k]); }
                    }
                }
            }

            // This might prove too expensive. Possible alternative?
            // Possible alternative idea: define a new function, which makes connections per vertex input.
            // This way, the aforementioned function could be called inside the above "for" loop.
            // This may have its own implementation weaknesses and may lead to the software showing unexpected behaviour,
            // e.g. the edges are not updated.
            // The efficiency and maintainability of such an approach needs to be programmed to conclude somewhere.
            this.MakeVerticesConnections(mask);


            return FindShortestPathAStar(start, finish, pointsConsidered);
        }

        /// <summary>
        /// Finds the shortest path available from first input position to the second input position
        /// using the current state (cell availability) of the grid.
        /// </summary>
        /// <param name="start">A 3D position in space from where the path planning begins</param>
        /// <param name="finish">The 3D destination position</param>
        /// <returns>A sequence of 3D positions which consists the shortest path in temporal order.</returns>
        public Vector3[] FindShortestPathAStar(Vector3 start, Vector3 finish) {
            return this.FindShortestPathAStar(start, finish, this.mask);
        }

        // Assistance function for constructor.
        private void FillBidirectionalMapping(Vector3 bottomLeftClosePoint, float sideLength)
        // Theoretically, this function can be broken into two pieces: I) fill in the grid entries II) make the mapping
        // However, it was considered to be too much of a waste to iterate through a 3D data structure twice.
        {
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    for (int k = 0; k < grid.GetLength(2); k++)
                    {
                        grid[i, j, k] = new Vector3(bottomLeftClosePoint.X + i * sideLength,
                                                    bottomLeftClosePoint.Y + j * sideLength,
                                                    bottomLeftClosePoint.Z + k * sideLength); // Define 3D point
                        biMap.Add(grid[i, j, k], new Vertex()); // Make 3D mapping.
                    }
                }
            }
        }

        /// <summary>
        /// Creates the connections of the underlying graph of the grid.
        /// A (directional) edge between two vertices representing cells of the grid is "drawn"
        /// only if the destination of the edge is available (a.k.a its mask value is "true").
        /// </summary>
        /// <param name="mask">
        /// A "bool[][][]" data type whose purpose is to (dis)allow connections between grid cells.
        /// The cell which corresponds to the index of the input will have no incoming edges towards it.
        /// </param>
        /// <exception cref="ArgumentException">The dimensions of the "mask" input must agree with the dimensions of the grid.</exception>
        /// 
        /// <remarks>
        /// An orthogonal parallelepiped may be adjacent with up to 26 other othogonal parallelepipeds.
        /// Since a cube is an orthogonal parallelpiped, the above statement is true for cubes.
        /// </remarks>
        private void MakeVerticesConnections(bool[, ,] mask)
        {
            if (mask.GetLength(0) != grid.GetLength(0) || mask.GetLength(1) != grid.GetLength(1) || mask.GetLength(2) != grid.GetLength(2))
                throw new ArgumentException("The mask provided is not the same dimensions as the grid");

            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    for (int k = 0; k < grid.GetLength(2); k++)
                    {
                        // Add all 26 possible adjacent 3D cells connections.
                        Vertex vReference = biMap.Forward[grid[i, j, k]];
                        // "Flushing" any possible remnants of a previous call.
                        vReference.Edges.Clear();
                        for (int horizontal = -1; horizontal <= 1; ++horizontal)
                        {
                            for (int vertical = -1; vertical <= 1; ++vertical)
                            {
                                for (int depth = -1; depth <= 1; ++depth)
                                {
                                    int leftRight = i + horizontal, upDown = j + vertical, deepClose = k + depth;
                                    if (leftRight >= 0 && leftRight < grid.GetLength(0) &&
                                        upDown >= 0 && upDown < grid.GetLength(1) &&
                                        deepClose >= 0 && deepClose < grid.GetLength(2) &&
                                        (horizontal != 0 || vertical != 0 || depth != 0))
                                        if (mask[leftRight, upDown, deepClose])
                                            vReference.AddEdge(biMap.Forward[grid[leftRight, upDown, deepClose]],
                                                                (grid[i, j, k] - grid[leftRight, upDown, deepClose]).Length());
                                }
                            }
                        }
                    }
                }
            }
        }


        // Assistance function for constructor.
        private void MakeVerticesConnections() { MakeVerticesConnections(this.mask); }

        // The default value of boolean variables in C# is "false".
        public void MarkAllCellsAsOccupied() { this.mask = new bool[grid.GetLength(0), grid.GetLength(1), grid.GetLength(2)]; }

        // Assistance function for constructors.
        public void MarkAllCellsAsFree()
        {
            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int j = 0; j < grid.GetLength(1); ++j)
                {
                    for (int k = 0; k < grid.GetLength(2); ++k)
                    {
                        mask[i, j, k] = true;
                    }
                }
            }

        }







        // Supporting class: <c>BidirectionalDictionary</c>

        public class BidirectionalDictionary<TForwardKey, TReverseKey> : IEnumerable<KeyValuePair<TForwardKey, TReverseKey>>
        {
            public Indexer<TForwardKey, TReverseKey> Forward { get; private set; } = new Indexer<TForwardKey, TReverseKey>();
            public Indexer<TReverseKey, TForwardKey> Reverse { get; private set; } = new Indexer<TReverseKey, TForwardKey>();

            const string DuplicateKeyErrorMessage = "";

            public BidirectionalDictionary()
            {
            }
            public BidirectionalDictionary(IDictionary<TForwardKey, TReverseKey> oneWayMap)
            {
                Forward = new Indexer<TForwardKey, TReverseKey>(oneWayMap);
                Dictionary<TReverseKey, TForwardKey> reversedOneWayMap = oneWayMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                Reverse = new Indexer<TReverseKey, TForwardKey>(reversedOneWayMap);
            }

            public void Add(TForwardKey t1, TReverseKey t2)
            {
                if (Forward.ContainsKey(t1)) throw new ArgumentException(DuplicateKeyErrorMessage, nameof(t1));
                if (Reverse.ContainsKey(t2)) throw new ArgumentException(DuplicateKeyErrorMessage, nameof(t2));
                Forward.Add(t1, t2); Reverse.Add(t2, t1);
            }

            public bool Remove(TForwardKey forwardKey)
            {
                if (Forward.ContainsKey(forwardKey) == false) return false;
                TReverseKey reverseKey = Forward[forwardKey];
                bool success;
                if (Forward.Remove(forwardKey))
                {
                    if (Reverse.Remove(reverseKey)) { success = true; }
                    else
                    {
                        Forward.Add(forwardKey, reverseKey);
                        success = false;
                    }
                }
                else { success = false; }

                return success;
            }

            public int Count() { return Forward.Count(); }

            IEnumerator<KeyValuePair<TForwardKey, TReverseKey>> IEnumerable<KeyValuePair<TForwardKey, TReverseKey>>.GetEnumerator()
            {
                return Forward.GetEnumerator();
            }

            public IEnumerator GetEnumerator()
            {
                return Forward.GetEnumerator();
            }

            /// <summary>
            /// Publically read-only lookup to prevent inconsistent state between forward and reverse map lookups
            /// </summary>
            /// <typeparam name="Key"></typeparam>
            /// <typeparam name="Value"></typeparam>
            public class Indexer<Key, Value> : IEnumerable<KeyValuePair<Key, Value>>
            {
                private IDictionary<Key, Value> dictionary;

                public Indexer() { dictionary = new Dictionary<Key, Value>(); }
                public Indexer(IDictionary<Key, Value> dictionary) { this.dictionary = dictionary; }
                public Value this[Key index] { get { return dictionary[index]; } }

                public static implicit operator Dictionary<Key, Value>(Indexer<Key, Value> indexer)
                {
                    return new Dictionary<Key, Value>(indexer.dictionary);
                }

                internal void Add(Key key, Value value) { dictionary.Add(key, value); }

                internal bool Remove(Key key) { return dictionary.Remove(key); }

                internal int Count() { return dictionary.Count; }

                public bool ContainsKey(Key key) { return dictionary.ContainsKey(key); }

                public IEnumerable<Key> Keys { get { return dictionary.Keys; } }

                public IEnumerable<Value> Values { get { return dictionary.Values; } }

                /// <summary>
                /// Deep copy lookup as a dictionary
                /// </summary>
                /// <returns></returns>
                public Dictionary<Key, Value> ToDictionary() { return new Dictionary<Key, Value>(dictionary); }

                public IEnumerator<KeyValuePair<Key, Value>> GetEnumerator() { return dictionary.GetEnumerator(); }

                IEnumerator IEnumerable.GetEnumerator() { return dictionary.GetEnumerator(); }
            }

        }

    }
}
