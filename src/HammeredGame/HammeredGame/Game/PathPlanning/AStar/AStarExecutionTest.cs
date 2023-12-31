﻿using System;
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

        // Graph example taken from https://www.youtube.com/watch?v=ySN5Wnu88nE&t=242s&ab_channel=Computerphile

        Vertex s = new Vertex("S", 0, 10), a = new Vertex("A", inf, 9), b = new Vertex("B", inf, 7),
                c = new Vertex("C", inf, 8), d = new Vertex("D", inf, 8), e = new Vertex("E", inf, 0),
                f = new Vertex("F", inf, 6), g = new Vertex("G", inf, 3), h = new Vertex("H", inf, 6),
                i = new Vertex("I", inf, 4), j = new Vertex("J", inf, 4), k = new Vertex("K", inf, 3),
                l = new Vertex("L", inf, 6);

        HashSet<Vertex> vertices = new HashSet<Vertex>(new[] { s, a, b, c, d, e, f, g, h, i, j, k, l });

        Edge SA = new Edge(a, 7), SB = new Edge(b, 2), SC = new Edge(c, 3);
        List<Edge> sEdges = new List<Edge>(new[] { SA, SB, SC }); s.Edges = sEdges;

        Edge AB = new Edge(b, 3), AD = new Edge(d, 4), AS = new Edge(s, 7);
        List<Edge> aEdges = new List<Edge>(new[] { AB, AD, AS }); a.Edges = aEdges;

        Edge BA = new Edge(a, 3), BD = new Edge(d, 4), BH = new Edge(h, 1), BS = new Edge(s, 2);
        List<Edge> bEdges = new List<Edge>(new[] { BA, BD, BH, BS }); b.Edges = bEdges;

        Edge CL = new Edge(l, 2), CS = new Edge(s, 3);
        List<Edge> cEdges = new List<Edge>(new[] { CL, CS }); c.Edges = cEdges;

        Edge DF = new Edge(f, 5), DB = new Edge(b, 4), DA = new Edge(a, 4);
        List<Edge> dEdges = new List<Edge>(new[] { DF, DB, DA }); d.Edges = dEdges;

        Edge EG = new Edge(g, 2), EK = new Edge(k, 5);
        List<Edge> eEdges = new List<Edge>(new[] { EG, EK }); e.Edges = eEdges;

        Edge FD = new Edge(d, 5), FH = new Edge(h, 3);
        List<Edge> fEdges = new List<Edge>(new[] { FD, FH }); f.Edges = fEdges;

        Edge GH = new Edge(h, 2), GE = new Edge(e, 2);
        List<Edge> gEdges = new List<Edge>(new[] { GH, GE }); g.Edges = gEdges;

        Edge HG = new Edge(g, 2), HF = new Edge(f, 3), HB = new Edge(b, 1);
        List<Edge> hEdges = new List<Edge>(new[] { HG, HF, HB }); h.Edges = hEdges;

        Edge IJ = new Edge(j, 6), IK = new Edge(k, 4), IL = new Edge(l, 4);
        List<Edge> iEdges = new List<Edge>(new[] { IJ, IK, IL }); i.Edges = iEdges;

        Edge JK = new Edge(k, 4), JL = new Edge(l, 4), JI = new Edge(i, 6);
        List<Edge> jEdges = new List<Edge>(new[] { JK, JL, JI }); j.Edges = jEdges;

        Edge KE = new Edge(e, 5), KI = new Edge(i, 4), KJ = new Edge(j, 4);
        List<Edge> kEdges = new List<Edge>(new[] { KE, KI, KJ }); k.Edges = kEdges;

        Edge LI = new Edge(i, 4), LJ = new Edge(j, 4), LC = new Edge(c, 2);
        List<Edge> lEdges = new List<Edge>(new[] { LI, LJ, LC }); l.Edges = lEdges;

        Graph graph = new Graph(vertices);

        Stack<Vertex> result = AStarAlgorithm.GetMinimumPath(s, e, graph);

        while (result.Count > 0)
        {
            Console.WriteLine(result.Pop().ID);
        }
    }
}


