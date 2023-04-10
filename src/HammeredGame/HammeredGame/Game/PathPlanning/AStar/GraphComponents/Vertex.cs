using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public LinkedList<Edge> Edges { get; set; }

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

        public Vertex(string id, double traveled, double heuristic, LinkedList<Edge> edges) : this(id, traveled, heuristic)
        {
            Edges = edges;
        }

        public void AddEdge(Vertex target, double weight) { this.Edges.AddLast(new Edge(target, weight)); }

        public bool Equals(Vertex other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
