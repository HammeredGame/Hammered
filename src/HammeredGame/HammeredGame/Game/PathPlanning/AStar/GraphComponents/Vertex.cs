using System;
using System.Collections.Generic;

namespace HammeredGame.Game.PathPlanning.AStar.GraphComponents
{
    /// <summary>
    /// The <c>Vertex</c> class represents a single vertex in a graph.
    /// A vertex may optionally have:
    ///     - an ID number (for easier identification in other parts of the code)
    ///     - directional edges, which connect it to other <c>Vertex</c> instances.
    /// </summary>
    public class Vertex : IEquatable<Vertex>
    {
        private static string NextID = "ID00";
        public string ID { get; }
        public double TraveledDistance { get; set; } = double.PositiveInfinity;
        public double HeuristicValue { get; set; } = double.PositiveInfinity;
        /// <value>
        /// The elements "Edges" will be only accessed sequentally during the execution of the A* algorithm.
        /// Therefore a <c>LinkedList</c> data structure will suffice.
        /// </value>
        public List<Edge> Edges { get; set; } = new List<Edge>();

        private static string IncrementNumberIn(string s)
        {
            string result = string.Empty;
            string numberStr = string.Empty;

            int i = s.Length - 1;
            for (; i > 0; i--)
            {
                char c = s[i];

                if (!char.IsDigit(c))
                    break;

                numberStr = c + numberStr;
            }

            int number = int.Parse(numberStr);
            number++;

            result += s.Substring(0, i + 1);
            result += number < 10 ? "0" : "";
            result += number;

            return result;
        }

        public Vertex()
        {
            ID = NextID;
            NextID = IncrementNumberIn(NextID); // Generate next ID.
        }
         

        public Vertex(string id, double traveled, double heuristic) : this()
        {
            ID = id;
            TraveledDistance = traveled;
            HeuristicValue = heuristic;
        }

        public Vertex(string id, double traveled, double heuristic, List<Edge> edges) : this(id, traveled, heuristic)
        {
            Edges = edges;
        }

        public void AddEdge(Vertex target, double weight) { this.Edges.Add(new Edge(target, weight)); }

        public void RemoveEdgeTo(Vertex target)
        {
            for (int i = 0; i <  this.Edges.Count; i++)
            {
                if (this.Edges[i].TargetVertex == target) {  this.Edges.RemoveAt(i); break; }
            }
        }

        /// <summary>
        /// Creates an edge from all vertices to which the current one is connected towards itself.
        /// </summary>
        public void CreateIncidentEdges()
        {
            for (int i = 0; i < this.Edges.Count; ++i)
            {
                // Currently considering symmetric weights.
                // The original plan was for the edges to be bi/undirectional, so it makes sense.
                this.Edges[i].TargetVertex.AddEdge(this, this.Edges[i].Weight);
            }
        }

        /// <summary>
        /// Removes the first reference of this vertex from all incident edges.
        /// </summary>
        public void RemoveIncidentEdges()
        {
            for (int i = 0; i < this.Edges.Count; ++i)
            {
                this.Edges[i].TargetVertex.RemoveEdgeTo(this);
            }
        }

        public bool Equals(Vertex other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
