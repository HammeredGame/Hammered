using HammeredGame.Game.PathPlanning.AStar;
using HammeredGame.Game.PathPlanning.AStar.GraphComponents;
using Priority_Queue; // https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
using System.Collections.Generic;

namespace HammeredGame.Game.PathPlanning
{
    /// <summary>
    /// Basic ingredients of the A* (pronounced "A star") algorithm.
    /// The A* algorithm is a slight extension of the famous Dijkstra algorithm for shortest path calculation.
    /// 
    /// In a nutshell, what needs to be known for A* are:
    /// Components
    /// ----------
    /// GRAPH
    /// 
    ///     - Vertices: they require a value from some heuristic function, which outputs the (estimated) distance
    ///     of the vertex v from the goal vertex g.
    ///         The choice of the heuristic function does not matter, as long as it is consistent (i.e. the distance function "makes sense").
    ///     - Edges: directional weighted edges.
    ///         Note: Undirectional edges are handled as two directional edges instead.
    /// 
    /// PRIORITY QUEUE:
    ///     The priority queue sorts the vertices to be examined with respect to their "urgency".
    ///     In the A* algorithm, urgency refers to its estimated total (accumulated) distance required to reach the goal vertex g
    ///     from the starting vertex s; this is expressed by the simple function
    ///     urgency(v) = distance travelled from s until v + (estimated) distance required to reach the g from v
    ///     
    ///     Reminder...The term "priority queue" is a BEHAVIOURAL description of the data structure(s) used,
    ///     and not some specific well-known data structure (arrays, hash maps, AVLs)
    ///     
    /// </summary>
    /// 
    public class AStarAlgorithm
    {

        public static Stack<Vertex> GetMinimumPath(Vertex start, Vertex destination, Graph graph)
        {
            // Dynamic sanity check.
            // If the graph does not contain both the starting node s and the destination node g,
            // then the algorithm may return with certainty that there is no path connecting the two.
            // However, since it is most probable that this case may arise due to human error (a.k.a wrong input),
            // instead of returning an empty <c>Stack<Vertex></c>, a null pointer is returned instead.
            if (!graph.vertices.Contains(start) || !graph.vertices.Contains(destination)) return null;

            /// <value>
            /// The <c>Dictionary<Vertex, Vertex> origin</c> data structure is a map M(V) -> V ∪ ∅.
            /// M(v) := which vertex u leads to v in such a way that the total path --which includes u and v--
            ///         from s to g is minimal?
            /// 
            /// Note: the mapping M(v) -> ∅ is only valid for v = start.
            /// </value>

            Dictionary<Vertex, Vertex> origin = new Dictionary<Vertex, Vertex>();
            // Since the path starts from the starting vertex s (variable "start"),
            // there is no prior vertex in any path which would reduce the total travelled distance
            // (only non-negative weights are considered).
            origin.Add(start, null); 

            /// <returns>
            /// The "Stack<Vertex> result" is the output returned to the user (unless invalid input was provided, in which
            /// case "null" is returned).
            /// The top object of the stack includes the first Vertex of the path (s), with each consecutive Vertex being the
            /// next step of the path, until reaching the bottom object, which is the goal vertex (g).
            /// 
            /// With current implementation, two possible outputs:
            /// 1) Path from "start" to "destination" exists: shortest path from "start" s to "destination" g is returned.
            /// 2) Path from "start" to "destination" does NOT exist: an empty stack is returned. Very computationally expensive to check
            /// Note: Scenario 2) is first of all extreme. For less computational resources additional algorithms (connected components checks)
            ///       should be implemented.
            /// </returns>

            // Initializing the output to stack to an empty stack.
            Stack<Vertex> result = new Stack<Vertex>();

            /// <value>
            /// The "HashSet<Vertex> finished" data structure contains all vertices which have been examined by A*.
            /// Due to the nature of the algorithm, once these vertices have been "popped" from the priority queue,
            /// it is impossible for a next reoccurance of it to achieve better results.
            /// This quality of the algorithm allows it to prevent the tree going back-and-forth between nodes already visited needlessly.
            /// </value>

            // Initializing the data structure which keeps track of which vertices have been examined/finalized.
            HashSet<Vertex> finished = new HashSet<Vertex>();

            /// <value>
            /// The "SimplePriorityQueue<Vertex, double> priorityQueue" object serves as the indespensable priority queue of A*.
            /// Items with LOWER (C# convention) objective value (shortest distance) are given priority.
            /// </value>
            /// <remarks>Using the safe version "SimplePriorityQueue" for now, to ensure correct execution. Not fully optimized.</remarks>

            // Sorted set data structure required. It will function as the priority queue of the algorithm.
            SimplePriorityQueue<Vertex, double> priorityQueue = new SimplePriorityQueue<Vertex, double>();

            /// <value>
            /// The "Dictionary<Vertex, double> priorityOfVertex" data structure records the current priority of the vertex in the priority queue.
            /// This data structure is utilized to check whether it is meaningful to update the priority priority or not
            /// (using the "priorirityQueue.UpdatePriority()" function).
            /// </value>
            Dictionary<Vertex, double> priorityOfVertex = new Dictionary<Vertex, double>();

            // Initialize the algorithm to have the starting node as the firts vertex to be examined.
            priorityQueue.Enqueue(start, 0 + start.HeuristicValue);
            priorityOfVertex.Add(start, 0 + start.HeuristicValue);

            while (priorityQueue.Count > 0)
            {
                // Popping the best element of the priority queue
                Vertex topElement = priorityQueue.First;
                priorityQueue.Remove(topElement);
                priorityOfVertex.Remove(topElement);


                // TERMINATION CONDITION HAS BEEN REACHED!
                // Once the destination g is to be examined, the shortest path to it from the starting node has been found.
                // Construct the path by recursively searching through the origins,
                // until the start node s (which has an origin of "null") has been reached.
                if (topElement == destination)
                {
                    while (topElement != null)
                    {
                        result.Push(topElement);
                        topElement = origin[topElement];
                    }

                    return result;
                }



                // Explore all connected vertices connected to "topElement" vertex.
                for (LinkedListNode<Edge> e = topElement.Edges.First; e != null; e = e.Next)
                {
                    Vertex target = e.Value.TargetVertex;

                    // See the explanation of "finished" variable.
                    // In short, the "target" vertex has already been examined by A*.
                    if (finished.Contains(target)) { continue; }


                    double targetPriority = topElement.TraveledDistance + target.HeuristicValue;
                    if (!priorityQueue.Contains(target)) // If "target" vertex has never been in the priority queue, add it.
                    {
                        target.TraveledDistance = topElement.TraveledDistance + e.Value.Weight; // Calculate the total traveled distance 
                        origin.Add(target, topElement); // Making the mapping connection M(target) -> topElement.
                        priorityQueue.Enqueue(target, targetPriority); // Inserting the vertex to the priority queue.
                        priorityOfVertex.Add(target, targetPriority);
                    }
                    else
                    { 

                        // Only update in the case of a better entry.
                        if (targetPriority < priorityOfVertex[target])
                        {
                            // Update vertex "target" information.
                            target.TraveledDistance = topElement.TraveledDistance + e.Value.Weight;
                            origin[target] = topElement;
                            // Update the priority queue entry (by re-inserting it).
                            priorityQueue.UpdatePriority(target, targetPriority);
                            priorityOfVertex[target] = targetPriority;
                        }
                    }
                }

                // Keep track that "topElement" has been popped by the priority queue
                // (read description of "finished" object for more details). 
                finished.Add(topElement);


            }

            // Returns the shortest path to the "destination" vertex OR an empty queue in case that there is no valid path.
            return result;
        }
    }
}
