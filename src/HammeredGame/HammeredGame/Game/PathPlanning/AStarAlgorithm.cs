using HammeredGame.Game.PathPlanning.GraphComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.PathPlanning
{
    public class AStarAlgorithm
    {
        class PriorityQueueEntry : IComparable
        {
            public Vertex Node;
            public double Priority;

            public PriorityQueueEntry(Vertex node, double priority)
            {
                Node = node;
                Priority = priority;
            }

            public static bool operator <(PriorityQueueEntry lhs, PriorityQueueEntry rhs)
            {
                return lhs.Priority < rhs.Priority;
            }
            public static bool operator >(PriorityQueueEntry lhs, PriorityQueueEntry rhs)
            {
                return lhs.Priority > rhs.Priority;
            }
            public bool Equals(PriorityQueueEntry other)
            {
                return this.Node == other.Node;
            }
            public int CompareTo(object incomingObject)
            {
                PriorityQueueEntry incomingPQE = incomingObject as PriorityQueueEntry;

                return this.Node.CompareTo(incomingPQE.Node);
            }
        }

        public Stack<Vertex> getMinimumPath(Vertex start, Vertex destination, Graph graph)
        {
            // Dynamic sanity check.
            if (!graph.vertices.Contains(start) || !graph.vertices.Contains(destination)) return null;

            // Sanity check.
            start.Origin = null;

            // Initializing the output to stack to an empty stack.
            Stack<Vertex> result = new Stack<Vertex>();
            // Initializing the data structure which keeps track of which vertices have been examined/finalized.
            HashSet<Vertex> finished = new HashSet<Vertex>();

            // Sorted set data structure required. It will function as the priority queue of the algorithm.
            SortedSet<PriorityQueueEntry> priorityQueue = new SortedSet<PriorityQueueEntry>();

            // Initialize the algorithm to have the starting node as the firts vertex to be examined.
            priorityQueue.Add(new PriorityQueueEntry(start, start.HeuristicValue));

            while (priorityQueue.Count > 0)
            {
                // Popping the best element of the priority queue
                Vertex topElement = priorityQueue.Min().Node;
                priorityQueue.Remove(priorityQueue.Min());

                //Console.WriteLine(topElement.ID);


                // TERMINATION CONDITION HAS BEEN REACHED!
                // Once the destination is to be examined, the shortest path to it from the starting node has been found.
                // Construct the path by recursively searching to the origins, until the start node (which has an origin of "null" has been reached).
                if (topElement == destination)
                {
                    while (topElement != null)
                    {
                        result.Push(topElement);
                        topElement = topElement.Origin;
                    }

                    return result;
                }

                // Add or update each connected vertex to the priority queue. 
                for (LinkedListNode<Edge> e = topElement.Edges.First; e != null; e = e.Next)
                {
                    Vertex target = e.Value.TargetVertex;
                    if (finished.Contains(target)) { continue; }

                    PriorityQueueEntry potentialInsertion = new PriorityQueueEntry(target, topElement.TraveledDistance + target.HeuristicValue);
                    if (!priorityQueue.Contains(potentialInsertion))
                    {
                        target.TraveledDistance = topElement.TraveledDistance + e.Value.Weight;
                        target.Origin = topElement;
                        priorityQueue.Add(potentialInsertion);
                    }
                    else
                    {
                        PriorityQueueEntry oldEntry;
                        priorityQueue.TryGetValue(potentialInsertion, out oldEntry);
                        // Only update in the case of a better entry.
                        if (potentialInsertion < oldEntry)
                        {
                            priorityQueue.Remove(oldEntry);

                            target.TraveledDistance = topElement.TraveledDistance + e.Value.Weight;
                            target.Origin = topElement;
                            priorityQueue.Add(potentialInsertion);
                        }
                    }
                }

                finished.Add(topElement);


            }

            return result;
        }
    }
}
