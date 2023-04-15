using HammeredGame.Game.PathPlanning.AStar.GraphComponents;
using System.Collections.Generic;

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
