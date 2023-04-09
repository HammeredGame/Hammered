using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.PathPlanning.AStar.GraphComponents
{
    public class Edge
    {
        public double Weight { get; set; } = 0;
        // Consider storing the source vertex as well.
        //Vertex SourceVertex { get; set; };
        public Vertex TargetVertex { get; set; } = null;
        public Edge(Vertex target, double weight)
        {
            TargetVertex = target;
            Weight = weight;
        }
    }
}
