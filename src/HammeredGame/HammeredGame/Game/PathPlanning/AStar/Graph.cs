using HammeredGame.Game.PathPlanning.AStar.GraphComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.PathPlanning.AStar
{
    public class Graph
    {
        public HashSet<Vertex> vertices { get; private set; }

        public Graph(HashSet<Vertex> vertices)
        {
            this.vertices = vertices;
        }
    }
}
