using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using HammeredGame.Game.PathPlanning.AStar;
using HammeredGame.Game.PathPlanning.AStar.GraphComponents;
using Microsoft.Xna.Framework;

namespace HammeredGame.Game.PathPlanning.Grid
{
    public class UniformGrid
    {
        public Vector3[,,] grid { get; private set; } // Temporary solution. In general, this is not good practice.
        public bool[,,] mask { get; private set; } // Temporary solution. In general, this is not good practice.

        private BidirectionalDictionary<Vector3, Vertex> biMap  = new BidirectionalDictionary<Vector3, Vertex>();
        private Graph graph;

        // The following three fields can uniquely define a uniform grid.
        public float sideLength { get; private set; }
        public Vector3 originPoint { get; private set; }
        public Vector3 endPoint { get; private set; }


        private HashSet<Vector3> pointsConsidered = new HashSet<Vector3>();
        private HashSet<Vertex> verticesOfGraph = new HashSet<Vertex>(); 

        /// <remarks>
        /// The current implementations of the constructors suggest that the point characterizing a cell is located at its center.
        /// Feel free to change it "corner-coordinated" system by changing the call "GetCellIndex".
        /// 
        /// WARNING: There is a possibility that the "corner-coordinated" convention might cause some visual anomalies in coarse grids.
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
            if (position.X < originPoint.X || position.X > endPoint.X)
                throw new ArgumentException(String.Format("The provided position's X coordinate is outside the grid." +
                    "grid max X = {0}. {1} was provided instead.", endPoint.X, position.X));

            if (position.Y < originPoint.Y || position.Y > endPoint.Y)
                throw new ArgumentException(String.Format("The provided position's Y coordinate is outside the grid." +
                    "grid max Y = {0}. {1} was provided instead.", endPoint.Y, position.Y));

            if (position.Z < originPoint.Z || position.Z > endPoint.Z)
                throw new ArgumentException(String.Format("The provided position's Z coordinate is outside the grid." +
                    "grid max Z = {0}. {1} was provided instead.", endPoint.Z, position.Z));

            //// Corner-coordinated grid cells
            //uint xIndex = (uint)Math.Floor((position.X - originPoint.X) / sideLength);
            //uint yIndex = (uint)Math.Floor((position.Y - originPoint.Y) / sideLength);
            //uint zIndex = (uint)Math.Floor((position.Z - originPoint.Z) / sideLength);
            // Center-coordinated grid cells
            uint xIndex = (uint)Math.Floor((position.X - originPoint.X + sideLength / 2) / sideLength);
            uint yIndex = (uint)Math.Floor((position.Y - originPoint.Y + sideLength / 2) / sideLength);
            uint zIndex = (uint)Math.Floor((position.Z - originPoint.Z + sideLength / 2) / sideLength);



            uint[] index = new uint[3] { xIndex, yIndex, zIndex };

            return index;

        }

        public bool GetCellMark(uint[] cellIndex) { return this.mask[cellIndex[0], cellIndex[1], cellIndex[2]]; }
        public bool GetCellMark(Vector3 position) { return GetCellMark(GetCellIndex(position)); }

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
        public void MarkCellAs(uint[] index, bool value) {
            mask[index[0], index[1], index[2]] = value; // Remove cell from mask.


            Vector3 pointOfInterest = grid[index[0], index[1], index[2]];

            if (value) pointsConsidered.Add(pointOfInterest);
            else pointsConsidered.Remove(pointOfInterest);

            Vertex correspondingVertex = biMap.Forward[pointOfInterest];
            if (value)
            {
                verticesOfGraph.Add(correspondingVertex);
                correspondingVertex.CreateIncidentEdges(); // Connect all neighbouring cells to this one.
            }
            else 
            {
                verticesOfGraph.Remove(correspondingVertex);
                correspondingVertex.RemoveIncidentEdges(); // Disconnect all neighbouring cells from this one.
            }
        }

        public void MarkCellAs(Vector3 position, bool value) { MarkCellAs(GetCellIndex(position), value); }

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
        private Vector3[] FindShortestPathAStar(Vector3 start, Vector3 finish, HashSet<Vector3> pointsConsidered, bool smoothPath=true)
        {
            uint[] startCellIndex = this.GetCellIndex(start), finishCellIndex = this.GetCellIndex(finish);
            Vector3 startCell = this.grid[startCellIndex[0], startCellIndex[1], startCellIndex[2]];
            Vector3 finishCell = this.grid[finishCellIndex[0], finishCellIndex[1], finishCellIndex[2]];

            //HashSet<Vertex> verticesOfGraph = new HashSet<Vertex>(); // Is replaced by local variable.
            // Isolate the vertices which will be taken into consideration for the A* algorithm.
            // Define the heuristic value of each vertex.
            // LEAVE UNCOMMENTED ONLY THE HEURISTIC FUNCTION WHICH WILL BE USED
            foreach (Vector3 point in pointsConsidered)
            {
                Vertex correspondingVertex = biMap.Forward[point];
                //verticesOfGraph.Add(correspondingVertex);
                Vector3 differenceVector = point - finishCell;
                //correspondingVertex.HeuristicValue = differenceVector.Length(); // Euclidean distance heuristic function
                //correspondingVertex.HeuristicValue = differenceVector.LengthSquared(); // Squared euclidean distance heuristic function. In hopes of punishing detours.
                correspondingVertex.HeuristicValue = Math.Abs(differenceVector.X) + Math.Abs(differenceVector.Y) + Math.Abs(differenceVector.Z); // Manhattan distance. In hopes of punishing detours even more.

            }
            /// <remarks>
            /// The "foreach" loop requires 2.941 seconds for the fully open map...i.e. most of the computational time...
            /// Update: by adding the private variable "verticesOfGraph", this got reduced to 1.9 seconds. Still too slow...
            /// </remarks>

            // Better have the following as a "while" loop (search for the next available grid until none are possible.)
            Stack<Vertex> aResultVertex = new();
            Vector3[] shortestPath;
            int pathLength;
            try
            {
                aResultVertex = AStarAlgorithm.GetMinimumPath(biMap.Forward[startCell], biMap.Forward[finishCell], new Graph(verticesOfGraph));
                pathLength = aResultVertex.Count();
            }
            catch (System.ArgumentNullException e) // There is no path from the current grid cell towards the destination
            {
                int step;

                // Initializing required data structures.
                step = 0;
                PriorityQueue<uint[], double> availablePositions = new PriorityQueue<uint[], double>();

                // Find closest available neighbouring positions.
                while (availablePositions.Count == 0)
                {
                    // Search the nearest neighbours in the x-z plane.
                    for (int width = -1 * step; width <= 1 * step; width++)
                    {
                        for (int depth = -1 * step; depth <= 1 * step; depth++)
                        {
                            if (Math.Abs(width) == step || Math.Abs(depth) == step) // Care only for the layer that is currently explored.
                            {
                                uint[] neighbourCellIndex = {
                                Convert.ToUInt32(startCellIndex[0] + width),
                                startCellIndex[1],
                                Convert.ToUInt32(startCellIndex[2] + depth)
                            };
                                // WARNING: This will produce errors in cases where there are obstruced obstacles at the boundary
                                // of the grid (out of bounds error).
                                if (this.GetCellMark(neighbourCellIndex))
                                {
                                    // Priority is done according to Manhattan distance
                                    availablePositions.Enqueue(
                                        neighbourCellIndex,
                                        Math.Abs(startCellIndex[0] - neighbourCellIndex[0])
                                        + Math.Abs(startCellIndex[1] - neighbourCellIndex[1])
                                        + Math.Abs(startCellIndex[2] - neighbourCellIndex[2])
                                    );

                                    //// Priority is done according to (squared) Euclidean distance
                                    //availablePositions.Enqueue(
                                    //    neighbourCellIndex,
                                    //    (startCellIndex[0] - neighbourCellIndex[0]) * (startCellIndex[0] - neighbourCellIndex[0])
                                    //    + (startCellIndex[1] - neighbourCellIndex[1]) * (startCellIndex[1] - neighbourCellIndex[1])
                                    //    + (startCellIndex[2] - neighbourCellIndex[2]) * (startCellIndex[2] - neighbourCellIndex[2])
                                    //);
                                }
                            }
                        }
                    }
                    ++step;
                }
                
                if (availablePositions.Count > 0)
                {
                    startCellIndex = availablePositions.Dequeue();
                    startCell = this.grid[startCellIndex[0], startCellIndex[1], startCellIndex[2]];
                }

                // Flush previous data and initialize data structures.
                step = 0;
                availablePositions.Clear();

                // Find closest avaiable destination position
                while (availablePositions.Count == 0)
                {
                    // Search the nearest neighbours in the x-z plane.
                    for (int width = -1 * step; width <= 1 * step; width++)
                    {
                        for (int depth = -1 * step; depth <= 1 * step; depth++)
                        {
                            if (Math.Abs(width) == step || Math.Abs(depth) == step) // Care only for the layer that is currently explored.
                            {
                                uint[] neighbourCellIndex = {
                                Convert.ToUInt32(finishCellIndex[0] + width),
                                finishCellIndex[1],
                                Convert.ToUInt32(finishCellIndex[2] + depth)
                            };
                                // WARNING: This will produce errors in cases where there are obstruced obstacles at the boundary
                                // of the grid (out of bounds error).
                                if (this.GetCellMark(neighbourCellIndex))
                                {
                                    // Priority is done according to Manhattan distance
                                    availablePositions.Enqueue(
                                        neighbourCellIndex,
                                        Math.Abs(finishCellIndex[0] - neighbourCellIndex[0])
                                        + Math.Abs(finishCellIndex[1] - neighbourCellIndex[1])
                                        + Math.Abs(finishCellIndex[2] - neighbourCellIndex[2])
                                    );

                                    //// Priority is done according to (squared) Euclidean distance
                                    //availablePositions.Enqueue(
                                    //    neighbourCellIndex,
                                    //    (finishCellIndex[0] - neighbourCellIndex[0]) * (finishCellIndex[0] - neighbourCellIndex[0])
                                    //    + (finishCellIndex[1] - neighbourCellIndex[1]) * (finishCellIndex[1] - neighbourCellIndex[1])
                                    //    + (finishCellIndex[2] - neighbourCellIndex[2]) * (finishCellIndex[2] - neighbourCellIndex[2])
                                    //);
                                }
                            }
                        }
                    }
                    ++step;
                }

                if (availablePositions.Count > 0)
                {
                    finishCellIndex = availablePositions.Dequeue();
                    finishCell = this.grid[finishCellIndex[0], finishCellIndex[1], finishCellIndex[2]];
                }

                aResultVertex = AStarAlgorithm.GetMinimumPath(biMap.Forward[startCell], biMap.Forward[finishCell], new Graph(verticesOfGraph));

            }
            finally
            {
                pathLength = aResultVertex.Count();
                shortestPath = new Vector3[1 + pathLength + 1];

                shortestPath[0] = start;
                for (int i = 0; i < pathLength; ++i) { shortestPath[1 + i] = biMap.Reverse[aResultVertex.Pop()]; }
                shortestPath[1 + pathLength] = finish;
            }

            return smoothPath ? this.RoughShortestPathSmoothing(shortestPath) : shortestPath;

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


            // In "Debug" mode, just this triple loop requires 1.745 seconds for a fully open grid!
            // Wait too slow for real time!
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
            // this.MakeVerticesConnections(mask); // Comment this line if you consider to be unchanging OR changed somewhere else (the latter being a more realistic scenario).


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
            return this.FindShortestPathAStar(start, finish, this.pointsConsidered);
        }

        // Assistance function for constructor.
        private void FillBidirectionalMapping(Vector3 bottomLeftClosePoint, float sideLength)
        // Theoretically, this function can be broken into two pieces: I) fill in the grid entries II) make the mapping
        // However, it was considered to be too much of a waste to iterate through a 3D data structure twice.
        {

            // Sequential implementation.
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

            // IDEA: For efficiency, parallelize the execution of the mapping, as the procedure needs not be sequential.
            // Programming NOTE: Currently, only the i-th index is parallelized, because parallelizing in itself has an overhead.
            // Experiment results: The overhead of the data structure required (ConcurrentDictionary) actually leads to slower times than sequential.
            //Parallel.For(0, grid.GetLength(0), i =>
            //{
            //    for (int j = 0; j < grid.GetLength(1); j++)
            //    {
            //        for (int k = 0; k < grid.GetLength(2); k++)
            //        {
            //            grid[i, j, k] = new Vector3(bottomLeftClosePoint.X + i * sideLength,
            //                                        bottomLeftClosePoint.Y + j * sideLength,
            //                                        bottomLeftClosePoint.Z + k * sideLength); // Define 3D point
            //            biMap.Add(grid[i, j, k], new Vertex()); // Make 3D mapping.
            //        }
            //    }
            //});
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
            // TODO: Make a faster implementation, where only the cells included in the <c>this.pointsConsidered</c> variable
            // are considered for connections. Could number reduce loops signigicantly.

            // Dynamic sanity check.
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
                                    // This "if" statement reduces the number of adjacent 3D cell connections:
                                    // <= 1 is 6 neighbours: up (U), down (D), left (L), right (R), forward (F), back (B). Produces much unnatural paths.
                                    // <= 2 is 18 neighbours
                                    // <= [3, +inf) is all 26 neighbours.
                                    if (Math.Abs(horizontal) + Math.Abs(vertical) + Math.Abs(depth) <= 3)
                                    {
                                        int leftRight = i + horizontal, upDown = j + vertical, deepClose = k + depth;
                                        if (leftRight >= 0 && leftRight < grid.GetLength(0) &&
                                            upDown >= 0 && upDown < grid.GetLength(1) &&
                                            deepClose >= 0 && deepClose < grid.GetLength(2) &&
                                            (horizontal != 0 || vertical != 0 || depth != 0))
                                            if (mask[leftRight, upDown, deepClose])
                                                if (vertical != 1)
                                                    vReference.AddEdge(biMap.Forward[grid[leftRight, upDown, deepClose]],
                                                                        (grid[i, j, k] - grid[leftRight, upDown, deepClose]).Length());
                                                else
                                                    vReference.AddEdge(biMap.Forward[grid[leftRight, upDown, deepClose]],
                                                                        (grid[i, j, k] - grid[leftRight, upDown, deepClose]).Length() * 1000);
                                        // Game convention: avoiding obstacles by going upwards should be avoided.
                                        // In order to fulfill this going up is heavily punished.
                                        // WARNING: this is supposed to be a temporary solution;
                                        //          the final software should disallow avoiding vertically entirely.
                                    }
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
        public void MarkAllCellsAsOccupied() { 
            this.mask = new bool[grid.GetLength(0), grid.GetLength(1), grid.GetLength(2)];
            this.pointsConsidered.Clear();
            this.verticesOfGraph.Clear();
        }

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
                        pointsConsidered.Add(grid[i, j, k]);
                        verticesOfGraph.Add(biMap.Forward[grid[i, j, k]]);
                    }
                }
            }

        }

        // Below shall follow functions whose purpose is to smoothen the trajectory of the path.


        /// <remarks>
        /// Before a path planner can search for a path it must find the graph nodes closest to the start and destination positions,
        /// and these will not always be the ones that give a natural-looking path. The solution is to post-process paths to "smooth"
        /// out the unwanted kinks.
        /// </remarks>

        // ============================== Post-processing functions smoothing START ==============================

        /// <summary>
        /// A fast algorithm which smoothens out the shortest path computed by the A* algorithm --according to the underlying
        /// graph-- by taking into consideration the real space's geometric information, which is store inside the grid.
        /// (i.e. availability or not of cells of the grid).
        /// <param name="ShortestPathResult"/>The positions in 3D space which constitute the shortest path according to the A* algorithm.</param>
        /// <returns>A smooth(er) path in 3D space.
        /// The index of the resulting array denotes the temporal visit of the positions,
        /// i.e. 0-th position is the first position visited, then the agent visits the 1st position, then the 2nd
        /// and so on and so forth.</returns>
        /// </summary>
        public Vector3[] RoughShortestPathSmoothing(Vector3[] ShortestPathResult)
        {
            /* Algorithm outline
             * -----------------
             * 1. Grab the source position of edge E1.
             * 2. Grab the destination position of edge E2.
             * 3. If the agent **can** move between those two positions unobstructed by the world geometry:
             *  3a. assign the destination of E1 to that of E2
             *  3b. remove E2 from the path.
             *  3c. reassign E2 to the new edge following E1.
             * NOTE FOR A MORE ADVANCED IMPLEMENTATION:
             * This should not be a simple line-of-sight test (as is being done here) as an entity’s size must be taken into consideration
             * -—it must be able to move between the two positions without bumping into any walls.)
             * 4. If the agent cannot move unobstructed between the two positions:
             *  4a. assign E2 to E1 and
             *  4b. advance E2.
             * 5. Repeat steps until the destination of E2 is equal to the destination of the path.
            */
			
			// Note:
			// The fact that this still remains a shortest path is guaranteed by the triangle inequality.

            // Edge case.
            if (ShortestPathResult.Length <= 2) { return ShortestPathResult.ToArray(); }

            LinkedList<Vector3> finalPositions = new LinkedList<Vector3>();
            finalPositions.AddLast(ShortestPathResult[0]); // Initialization

            int behindIndex = 0, intermediateIndex = 1, frontIndex = 2; // behind+intermediate = E1, intermediate+front = E2 

            while (frontIndex < ShortestPathResult.Length)
            {
                Vector3 linearSegment = ShortestPathResult[frontIndex] - ShortestPathResult[behindIndex];
                double linearSegmentLength = linearSegment.Length(); linearSegment.Normalize(); // Now "linearSegment" is a direction.

                bool linearPathIsUnobstructed = true;
                // Sample the linear segment connecting "behind" position and "front" position.
                for (int i = 0; i < Math.Ceiling(linearSegmentLength / this.sideLength); i++)
                {
                    Vector3 samplePoint = ShortestPathResult[behindIndex] + i * sideLength * linearSegment;
                    if (!this.GetCellMark(samplePoint)) { linearPathIsUnobstructed = false; break; }
                }

                if (linearPathIsUnobstructed)
                {
                    // Prepare to check whether there is an available straight path between the current "behind node"
                    // and the next "front node".
                    intermediateIndex = frontIndex; // 3a. assign the destination of E1 to that of E2.
                    frontIndex += 1; // 3c.reassign E2 to the new edge following E1.
                }
                else
                {
                    finalPositions.AddLast(ShortestPathResult[intermediateIndex]);

                    behindIndex = intermediateIndex; intermediateIndex = frontIndex; // 4a.assign E2 to E1
                    frontIndex += 1; // 4b.advance E2
                }
            }
            finalPositions.AddLast(ShortestPathResult.Last()); // Termination

            Vector3[] result = finalPositions.ToArray();

            return result;
        }

        // ============================== Post-processing functions smoothing FINSH ==============================







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

                public Indexer() {
                    dictionary = new Dictionary<Key, Value>();
                    //dictionary = new ConcurrentDictionary<Key, Value>(); // The overhead actually leads to slower times than sequential.
                }
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
