using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUutilities;

namespace HammeredGame.Game.PathPlanning.TurnSmoothing
{
    internal static class HermiteCubicSpline
    {
        // Hermite Cubic Spline POSITION section



        private static Vector3 HermiteCubicSplinePositionNoPrecompute(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t)
        {
            // p(t) = (2(p0 - p1) + p0'+ p1')t^3 + (-3(p0 - p1) - 2p0' - p1')t^2 + p0't + p0
            Vector3 t3Term = (2 * (p0 - p1) + m0 + m1); // (2(p0 - p1) + p0'+ p1')t^3
            Vector3 t2Term = (-3 * (p0 - p1) - 2 * m0 - m1);
            Vector3 t1Term = m0;

            return t3Term * t * t * t + t2Term * t * t + t1Term * t + p0;
        }

        private static Vector3 HermiteCubicSplinePositionWithPrecompute(Vector3 t0Term, Vector3 t1Term, Vector3 t2Term, Vector3 t3Term, float t)
        {
            return t0Term + t1Term * t + t2Term * t * t + t3Term * t*t*t;
        }

        public static Vector3 HermiteCubicSplinePosition(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float t, bool precomputed=true)
        {
            Vector3 result = precomputed ?
                HermiteCubicSplinePositionWithPrecompute(v1, v2, v3, v4, t) :
                HermiteCubicSplinePositionNoPrecompute(v1, v2, v3, v4, t);
            return result;
        }

        public static Vector3[] HermiteCubicSplinePositionCoefficients(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            Vector3[] coefficients = new Vector3[] {
                HermiteCubicSplinePositionZeroDegreeCoefficient(p0, p1, m0, m1),
                HermiteCubicSplinePositionFirstDegreeCoefficient(p0, p1, m0, m1),
                HermiteCubicSplinePositionSecondDegreeCoefficient(p0, p1, m0, m1),
                HermiteCubicSplinePositionThirdDegreeCoefficient(p0, p1, m0, m1)
            };

            return coefficients;
        }

        private static Vector3 HermiteCubicSplinePositionThirdDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            return (2 * (p0 - p1) + m0 + m1);
        }

        private static Vector3 HermiteCubicSplinePositionSecondDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            return (-3 * (p0 - p1) - 2 * m0 - m1);
        }
        private static Vector3 HermiteCubicSplinePositionFirstDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            return m1;
        }

        private static Vector3 HermiteCubicSplinePositionZeroDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            return p0;
        }





        // Hermite Cubic Spline VELOCITY section



        private static Vector3 HermiteCubicSplineVelocityNoPrecompute(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t)
        {
            // p'(t) = p0' + 2(- 3(p0 - p1) - 2p0' - p1')t + 3(2(p0 - p1) + p0'+ p1')t^2
            Vector3 t2Term = 3 * (2 * (p0 - p1) + m0 + m1);
            Vector3 t1Term = 2 * (-3 * (p0 - p1) - 2 * m0 - m1);
            return m0 + t1Term * t + t2Term * t * t;
        }

        private static Vector3 HermiteCubicSplineVelocityWithPrecompute(Vector3 t0Term, Vector3 t1Term, Vector3 t2Term, float t)
        {
            return t0Term + t1Term * t + t2Term * t*t;
        }

        public static Vector3 HermiteCubicSplineVelocity(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float t, bool precomputed = true)
        {
            Vector3 result = precomputed ?
                HermiteCubicSplineVelocityWithPrecompute(v1, v2, v3, t) :
                HermiteCubicSplineVelocityNoPrecompute(v1, v2, v3, v4, t);
            return result;
        }


        public static Vector3[] HermiteCubicSplineVelocityCoefficients(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            Vector3[] coefficients = new Vector3[] {
                HermiteCubicSplineVelocityZeroDegreeCoefficient(p0, p1, m0, m1),
                HermiteCubicSplineVelocityFirstDegreeCoefficient(p0, p1, m0, m1),
                HermiteCubicSplineVelocitySecondDegreeCoefficient(p0, p1, m0, m1),
            };

            return coefficients;
        }

        private static Vector3 HermiteCubicSplineVelocityZeroDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            // p'(t) = p0' + ...
            return m0;
        }

        private static Vector3 HermiteCubicSplineVelocityFirstDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            // p'(t) = ... + 2(- 3(p0 - p1) - 2p0' - p1')t + ...
            return 2 * ( -3 * (p0 - p1) - 2 * m0 - m1);
        }

        private static Vector3 HermiteCubicSplineVelocitySecondDegreeCoefficient(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1)
        {
            // p'(t) = ... + 3(2(p0 - p1) + p0'+ p1')t^2
            return 3 * (2 * (p0 - p1) + m0 + m1);
        }


    }
}
