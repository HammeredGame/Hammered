using Microsoft.Xna.Framework;
using System;


namespace HammeredGame.Game.PathPlanning.Grid
{
    class HelloWorld
    {
        /// <summary>
        /// The following code produces the following test case
        ///
        /// 2| S |   |   |   |
        /// 1|   | X | X |   |
        /// 0|   |   | X | G |
        ///    0   1   2   3
        ///    
        /// where:
        /// - S := starting cell
        /// - G := goal cell
        /// - X := unavailable cells (e.g. the three X together consist an object in the world.
        /// 
        /// The only available path is "starting position " -> S(0, 2) -- R --> (1, 2) -- R --> (2,2) -- RD --> (3, 1) -- D --> (3,0)
        /// 
        /// </summary>
        static void Main()
        {
            UniformGrid test = new UniformGrid(4, 3, 0, 1.0f);
            test.MarkCellAs(new uint[3] { 1, 1, 0 }, false); test.MarkCellAs(new uint[3] { 2, 0, 0 }, false); test.MarkCellAs(new uint[3] { 2, 1, 0 }, false);
            Vector3[] testPath = test.FindShortestPathAStar(new Vector3(0.1f, 2.3f, 0.0f), new Vector3(3.6f, 0.3f, 0.0f));
            for (int i = 0; i < testPath.GetLength(0); ++i)
            {
                Console.WriteLine(testPath[i]);
            }

        }
    }
}
