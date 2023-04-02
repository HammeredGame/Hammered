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
    public class Vertex : IComparable
    {
        public int ID { get; }
        public double TraveledDistance { get; set; } = double.PositiveInfinity;
        public double HeuristicValue { get; set; } = double.PositiveInfinity;
        /// <value>
        /// The elements "Edges" will be only accessed sequentally during the execution of the A* algorithm.
        /// Therefore a <c>LinkedList</c> data structure will suffice.
        /// </value>
        public LinkedList<Edge> Edges { get; set; }

        public Vertex Origin = null;
        public Vertex(int id, double traveled, double heuristic)
        {
            ID = id;
            TraveledDistance = traveled;
            HeuristicValue = heuristic;
        }

        public Vertex(int id, double traveled, double heuristic, LinkedList<Edge> edges) : this(id, traveled, heuristic)
        {
            Edges = edges;
        }

        public int CompareTo(object incomingObject)
        {
            Vertex incomingVertex = incomingObject as Vertex;
            return this.ID.CompareTo(incomingVertex.ID);
        }
    }
}
