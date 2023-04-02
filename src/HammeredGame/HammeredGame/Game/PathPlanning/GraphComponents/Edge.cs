using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.PathPlanning.Graph
{
    public class Edge
    {
        double Weight { get; set; } = 0;
        // Consider storing the source vertex as well.
        //Vertex SourceVertex { get; set; };
        Vertex TargetVertex { get; set; } = null;
        public Edge(double weight, Vertex target) 
        {
            Weight = weight;
            TargetVertex = target;
        }
    }
}
