using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HammeredGame.Game.PathPlanning.AStar;
using HammeredGame.Game.PathPlanning.AStar.GraphComponents;
using Microsoft.Xna.Framework;

namespace HammeredGame.Game.PathPlanning.Grid
{
    public class UniformGrid
    {
        private Vector3[, ,] grid;
        private BidirectionalDictionary<Vector3, Vertex> biMap;
        private Graph graph;

        public UniformGrid(int nrCellsX, int nrCellsY, int nrCellsZ, float sideLength)
        {
            grid = new Vector3[nrCellsX, nrCellsY, nrCellsZ];
            // STEP 1: Define all points in 3D space and map them to corresponding vertices.
            FillBidirectionalMapping(sideLength);
            // STEP 2: Define the connections between the vertices.
            MakeVerticesConnections();

        }

        public UniformGrid(int nrCellsPerDimension, float sideLength) : this(nrCellsPerDimension, nrCellsPerDimension, nrCellsPerDimension, sideLength)
        {
        }

        public UniformGrid(float fullCubeLengthPerDimension, float sideLength) : this((int)Math.Ceiling(fullCubeLengthPerDimension / sideLength), sideLength)
        {
        }

        public UniformGrid(Vector3 bottomLeftClosePoint, Vector3 topRightAwayPoint, int nrCellsX, int nrCellsY, int nrCellsZ, float sideLength)
        {
            if (bottomLeftClosePoint.X > topRightAwayPoint.X)
                throw new ArgumentException(String.Format("First input must be on the left of the second input. Instead, {0} > {1} was provided", bottomLeftClosePoint.X, topRightAwayPoint.X));
            if (bottomLeftClosePoint.Y > topRightAwayPoint.Y)
                throw new ArgumentException(String.Format("First input must be below the second input. Instead, {0} > {1} was provided", bottomLeftClosePoint.Y, topRightAwayPoint.Y));
            if (bottomLeftClosePoint.Z > topRightAwayPoint.Z)
                throw new ArgumentException(String.Format("First input must be closer to origin than the second input. Instead, {0} > {1} was provided", bottomLeftClosePoint.Z, topRightAwayPoint.Z));

            grid = new Vector3[nrCellsX, nrCellsY, nrCellsZ];
            // STEP 1: Define all points in 3D space and map them to corresponding vertices.
            FillBidirectionalMapping(sideLength);
            // STEP 2: Define the connections between the vertices.
            MakeVerticesConnections();


        }

        public UniformGrid(Vector3 bottomLeftClosePoint, Vector3 topRightAwayPoint, float sideLength) :
            this(bottomLeftClosePoint, topRightAwayPoint,
                (int)Math.Ceiling(Math.Abs(bottomLeftClosePoint.X - topRightAwayPoint.X) / sideLength),
                (int)Math.Ceiling(Math.Abs(bottomLeftClosePoint.Y - topRightAwayPoint.Y) / sideLength),
                (int)Math.Ceiling(Math.Abs(bottomLeftClosePoint.Z - topRightAwayPoint.Z) / sideLength),
                sideLength
                )
        { 
        }

        public Vector3[] FindShortestPathAStar(Vector3 start, Vector3 finish, HashSet<Vector3> pointsConsidered)
        {
            HashSet<Vertex> verticesOfGraph = new HashSet<Vertex>();
            // Isolate the vertices which will be taken into consideration for the A* algorithm.
            // Define the heuristic value of each vertex.
            // CURRENT HEURISTIC FUNCTION: EUCLIDEAN DISTANCE
            foreach (Vector3 point in pointsConsidered)
            {
                Vertex correspondingVertex = biMap.Forward[point];
                verticesOfGraph.Add(correspondingVertex);
                correspondingVertex.HeuristicValue = (point - finish).Length(); // Euclidean distance heuristic function
            }

            Stack<Vertex> aResultVertex = AStarAlgorithm.GetMinimumPath(biMap.Forward[start], biMap.Forward[finish], new Graph(verticesOfGraph));
            // Transform the vertex result (which is independent from any geographical meaning) to a 3D point result.
            int pathLength = aResultVertex.Count();
            Vector3[] shortestPath = new Vector3[pathLength];
            for (int i = 0; i < pathLength; ++i) { shortestPath[i] = biMap.Reverse[aResultVertex.Pop()]; }

            // shortestPath[0] = starting point, shortestPath[pathLength-1] = finish point.
            return shortestPath;

        }

        public Vector3[] FindShortestPathAStar(Vector3 start, Vector3 finish)
        {
            // Supposes that all points of the grids are taken into consideration in the A* algorithm. 
            HashSet<Vector3> allVectors = new HashSet<Vector3>();

            // Isolate the vertices which will be taken into consideration for the A* algorithm.
            for (int i = 0; i < grid.GetLength(0); ++i)
            {
                for (int j = 0; j < grid.GetLength(1); ++j)
                {
                    for (int k = 0; k < grid.GetLength(2); ++k)
                    {
                        allVectors.Add(grid[i, j, k]);
                    }
                }
            }

            return FindShortestPathAStar(start, finish, allVectors);
        }

        // Assistance function for constructor.
        private void FillBidirectionalMapping(float sideLength)
        // Theoretically, this function can be broken into two pieces: I) fill in the grid entries II) make the mapping
        // However, it was considered to be too much of a waste to iterate through a 3D data structure twice.
        {
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    for (int k = 0; k < grid.GetLength(2); k++)
                    {
                        grid[i, j, k] = new Vector3(i * sideLength, j * sideLength, k * sideLength); // Define 3D point
                        biMap.Add(grid[i, j, k], new Vertex()); // Make 3D mapping.
                    }
                }
            }
        }

        // Assistance function for constructor.
        private void MakeVerticesConnections()
        {
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    for (int k = 0; k < grid.GetLength(2); k++)
                    {
                        // Add all 26 possible adjacent 3D cells connections.
                        Vertex vReference = biMap.Forward[grid[i, j, k]];
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
                                        (horizontal != 0 && vertical != 0 && depth != 0))
                                        vReference.AddEdge(biMap.Forward[grid[leftRight, upDown, deepClose]],
                                                            (grid[i, j, k] - grid[leftRight, upDown, deepClose]).Length());
                                }
                            }
                        }
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
