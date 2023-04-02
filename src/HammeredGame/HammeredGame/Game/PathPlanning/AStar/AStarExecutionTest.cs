using System;
using System.Collections.Generic;
using HammeredGame.Game.PathPlanning;
using HammeredGame.Game.PathPlanning.AStar;
using HammeredGame.Game.PathPlanning.AStar.GraphComponents;


class HelloWorld
{
    static void Main()
    {
        Console.WriteLine("Hello World");

        double inf = double.PositiveInfinity;

        Vertex s = new Vertex("S", 0, 10), a = new Vertex("A", inf, 9), b = new Vertex("B", inf, 7),
                c = new Vertex("C", inf, 8), d = new Vertex("D", inf, 8), e = new Vertex("E", inf, 0),
                f = new Vertex("F", inf, 6), g = new Vertex("G", inf, 3), h = new Vertex("H", inf, 6),
                i = new Vertex("I", inf, 4), j = new Vertex("J", inf, 4), k = new Vertex("K", inf, 3),
                l = new Vertex("L", inf, 6);

        HashSet<Vertex> vertices = new HashSet<Vertex>(new[] { s, a, b, c, d, e, f, g, h, i, j, k, l });

        Edge SA = new Edge(7, a), SB = new Edge(2, b), SC = new Edge(3, c);
        LinkedList<Edge> sEdges = new LinkedList<Edge>(new[] { SA, SB, SC }); s.Edges = sEdges;

        Edge AB = new Edge(3, b), AD = new Edge(4, d), AS = new Edge(7, s);
        LinkedList<Edge> aEdges = new LinkedList<Edge>(new[] { AB, AD, AS }); a.Edges = aEdges;

        Edge BA = new Edge(3, a), BD = new Edge(4, d), BH = new Edge(1, h), BS = new Edge(2, s);
        LinkedList<Edge> bEdges = new LinkedList<Edge>(new[] { BA, BD, BH, BS }); b.Edges = bEdges;

        Edge CL = new Edge(2, l), CS = new Edge(3, s);
        LinkedList<Edge> cEdges = new LinkedList<Edge>(new[] { CL, CS }); c.Edges = cEdges;

        Edge DF = new Edge(5, f), DB = new Edge(4, b), DA = new Edge(4, a);
        LinkedList<Edge> dEdges = new LinkedList<Edge>(new[] { DF, DB, DA }); d.Edges = dEdges;

        Edge EG = new Edge(2, g), EK = new Edge(5, k);
        LinkedList<Edge> eEdges = new LinkedList<Edge>(new[] { EG, EK }); e.Edges = eEdges;

        Edge FD = new Edge(5, d), FH = new Edge(3, h);
        LinkedList<Edge> fEdges = new LinkedList<Edge>(new[] { FD, FH }); f.Edges = fEdges;

        Edge GH = new Edge(2, h), GE = new Edge(2, e);
        LinkedList<Edge> gEdges = new LinkedList<Edge>(new[] { GH, GE }); g.Edges = gEdges;

        Edge HG = new Edge(2, g), HF = new Edge(3, f), HB = new Edge(1, b);
        LinkedList<Edge> hEdges = new LinkedList<Edge>(new[] { HG, HF, HB }); h.Edges = hEdges;

        Edge IJ = new Edge(6, j), IK = new Edge(4, k), IL = new Edge(4, l);
        LinkedList<Edge> iEdges = new LinkedList<Edge>(new[] { IJ, IK, IL }); i.Edges = iEdges;

        Edge JK = new Edge(4, k), JL = new Edge(4, l), JI = new Edge(6, i);
        LinkedList<Edge> jEdges = new LinkedList<Edge>(new[] { JK, JL, JI }); j.Edges = jEdges;

        Edge KE = new Edge(5, e), KI = new Edge(4, i), KJ = new Edge(4, j);
        LinkedList<Edge> kEdges = new LinkedList<Edge>(new[] { KE, KI, KJ }); k.Edges = kEdges;

        Edge LI = new Edge(4, i), LJ = new Edge(4, j), LC = new Edge(2, c);
        LinkedList<Edge> lEdges = new LinkedList<Edge>(new[] { LI, LJ, LC }); l.Edges = lEdges;

        Graph graph = new Graph(vertices);

        AStarAlgorithm test = new AStarAlgorithm();
        Stack<Vertex> result = test.getMinimumPath(s, e, graph);

        while (result.Count > 0)
        {
            Console.WriteLine(result.Pop().ID);
        }
    }
}


