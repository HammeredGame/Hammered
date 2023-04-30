using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUutilities;

namespace HammeredGame.Game.PathPlanning.TurnSmoothing
{
    internal static class BezierQuadraticSpline
    {
        public static Vector3 MiddleControlPointComputation(Vector3 p0, Vector3 pc, Vector3 p2)
        {
            /* We are provided with three points in 3D space from which we wish the quadratic Bezier spline to pass through.
             * By design, we (arbitrarily) decide that we wish the curve to pass from Pcenter at t = 0.5
             * Hence, we must find the middle control point P1, which satisfies:
             * P(0.5) = Pcenter <=>
             * 0.5^2 P0 + 2 0.5*0.5 P1 + P2 0.5^2 = Pcenter  <=>
             * P1 = 2 Pcenter - 1/2 (P0 + P1)
            */ 
            return 2 * pc - 0.5f * (p0 + p2);
        }

        public static Vector3 QuadraticBezierPosition(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            // Quadratic Bezier curve := P(t) = P0 (1-t)^2 + 2P1(1-t)t + P2t^2
            return p0 * (1 - t) * (1 - t) + 2 * p1 * (1 - t) * t + p2 * t * t;
        }

        public static Vector3 QuadraticBezierVelocity(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            // Quadratic Bezier curve := P(t) = P0 (1-t)^2 + 2P1(1-t)t + P2t^2 === differentiate w.r.t. t ==>
            // P'(t) = -2(1-t)P0 + 2(1-2t)P1 + 2tP2
            return -2 * (1 - t) * p0 + 2 * (1 - 2 * t) * p1 + 2 * p2;
        }
    }
}
