using Microsoft.Xna.Framework;

namespace HammeredGame.Core
{
    /// <summary>
    /// This is a helper function pulled from the bepuphysics v1 repository to help with conversions
    /// between BEPU math and XNA math. All methods can be used as extension methods as
    /// vector3.ToXNA(), or otherwise directly called via MathConverter.ToXNA(vector3).
    ///
    /// Helps convert between XNA math types and the BEPUphysics replacement math types. A version
    /// of this converter could be created for other platforms to ease the integration of the engine.
    /// </summary>
    public static class MathConverter
    {
        //Vector2
        public static Vector2 ToXNA(this BEPUutilities.Vector2 bepuVector)
        {
            Vector2 toReturn;
            toReturn.X = bepuVector.X;
            toReturn.Y = bepuVector.Y;
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Vector2 bepuVector, out Vector2 xnaVector)
        {
            xnaVector.X = bepuVector.X;
            xnaVector.Y = bepuVector.Y;
        }

        public static BEPUutilities.Vector2 ToBepu(this Vector2 xnaVector)
        {
            BEPUutilities.Vector2 toReturn;
            toReturn.X = xnaVector.X;
            toReturn.Y = xnaVector.Y;
            return toReturn;
        }

        public static void ToBepu(this ref Vector2 xnaVector, out BEPUutilities.Vector2 bepuVector)
        {
            bepuVector.X = xnaVector.X;
            bepuVector.Y = xnaVector.Y;
        }

        //Vector3
        public static Vector3 ToXNA(this BEPUutilities.Vector3 bepuVector)
        {
            Vector3 toReturn;
            toReturn.X = bepuVector.X;
            toReturn.Y = bepuVector.Y;
            toReturn.Z = bepuVector.Z;
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Vector3 bepuVector, out Vector3 xnaVector)
        {
            xnaVector.X = bepuVector.X;
            xnaVector.Y = bepuVector.Y;
            xnaVector.Z = bepuVector.Z;
        }

        public static BEPUutilities.Vector3 ToBepu(this Vector3 xnaVector)
        {
            BEPUutilities.Vector3 toReturn;
            toReturn.X = xnaVector.X;
            toReturn.Y = xnaVector.Y;
            toReturn.Z = xnaVector.Z;
            return toReturn;
        }

        public static void ToBepu(this ref Vector3 xnaVector, out BEPUutilities.Vector3 bepuVector)
        {
            bepuVector.X = xnaVector.X;
            bepuVector.Y = xnaVector.Y;
            bepuVector.Z = xnaVector.Z;
        }

        public static Vector3[] ToXNA(this BEPUutilities.Vector3[] bepuVectors)
        {
            Vector3[] xnaVectors = new Vector3[bepuVectors.Length];
            for (int i = 0; i < bepuVectors.Length; i++)
            {
                bepuVectors[i].ToXNA(out xnaVectors[i]);
            }
            return xnaVectors;

        }

        public static BEPUutilities.Vector3[] ToBepu(this Vector3[] xnaVectors)
        {
            var bepuVectors = new BEPUutilities.Vector3[xnaVectors.Length];
            for (int i = 0; i < xnaVectors.Length; i++)
            {
                xnaVectors[i].ToBepu(out bepuVectors[i]);
            }
            return bepuVectors;

        }

        //Matrix
        public static Matrix ToXNA(this BEPUutilities.Matrix matrix)
        {
            Matrix toReturn;
            matrix.ToXNA(out toReturn);
            return toReturn;
        }

        public static BEPUutilities.Matrix ToBepu(this Matrix matrix)
        {
            BEPUutilities.Matrix toReturn;
            matrix.ToBepu(out toReturn);
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Matrix matrix, out Matrix xnaMatrix)
        {
            xnaMatrix.M11 = matrix.M11;
            xnaMatrix.M12 = matrix.M12;
            xnaMatrix.M13 = matrix.M13;
            xnaMatrix.M14 = matrix.M14;

            xnaMatrix.M21 = matrix.M21;
            xnaMatrix.M22 = matrix.M22;
            xnaMatrix.M23 = matrix.M23;
            xnaMatrix.M24 = matrix.M24;

            xnaMatrix.M31 = matrix.M31;
            xnaMatrix.M32 = matrix.M32;
            xnaMatrix.M33 = matrix.M33;
            xnaMatrix.M34 = matrix.M34;

            xnaMatrix.M41 = matrix.M41;
            xnaMatrix.M42 = matrix.M42;
            xnaMatrix.M43 = matrix.M43;
            xnaMatrix.M44 = matrix.M44;

        }

        public static void ToBepu(this ref Matrix matrix, out BEPUutilities.Matrix bepuMatrix)
        {
            bepuMatrix.M11 = matrix.M11;
            bepuMatrix.M12 = matrix.M12;
            bepuMatrix.M13 = matrix.M13;
            bepuMatrix.M14 = matrix.M14;

            bepuMatrix.M21 = matrix.M21;
            bepuMatrix.M22 = matrix.M22;
            bepuMatrix.M23 = matrix.M23;
            bepuMatrix.M24 = matrix.M24;

            bepuMatrix.M31 = matrix.M31;
            bepuMatrix.M32 = matrix.M32;
            bepuMatrix.M33 = matrix.M33;
            bepuMatrix.M34 = matrix.M34;

            bepuMatrix.M41 = matrix.M41;
            bepuMatrix.M42 = matrix.M42;
            bepuMatrix.M43 = matrix.M43;
            bepuMatrix.M44 = matrix.M44;

        }

        public static Matrix ToXNA(this BEPUutilities.Matrix3x3 matrix)
        {
            Matrix toReturn;
            matrix.ToXNA(out toReturn);
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Matrix3x3 matrix, out Matrix xnaMatrix)
        {
            xnaMatrix.M11 = matrix.M11;
            xnaMatrix.M12 = matrix.M12;
            xnaMatrix.M13 = matrix.M13;
            xnaMatrix.M14 = 0;

            xnaMatrix.M21 = matrix.M21;
            xnaMatrix.M22 = matrix.M22;
            xnaMatrix.M23 = matrix.M23;
            xnaMatrix.M24 = 0;

            xnaMatrix.M31 = matrix.M31;
            xnaMatrix.M32 = matrix.M32;
            xnaMatrix.M33 = matrix.M33;
            xnaMatrix.M34 = 0;

            xnaMatrix.M41 = 0;
            xnaMatrix.M42 = 0;
            xnaMatrix.M43 = 0;
            xnaMatrix.M44 = 1;
        }

        public static void ToBepu(this ref Matrix matrix, out BEPUutilities.Matrix3x3 bepuMatrix)
        {
            bepuMatrix.M11 = matrix.M11;
            bepuMatrix.M12 = matrix.M12;
            bepuMatrix.M13 = matrix.M13;

            bepuMatrix.M21 = matrix.M21;
            bepuMatrix.M22 = matrix.M22;
            bepuMatrix.M23 = matrix.M23;

            bepuMatrix.M31 = matrix.M31;
            bepuMatrix.M32 = matrix.M32;
            bepuMatrix.M33 = matrix.M33;

        }

        //Quaternion
        public static Quaternion ToXNA(this BEPUutilities.Quaternion quaternion)
        {
            Quaternion toReturn;
            toReturn.X = quaternion.X;
            toReturn.Y = quaternion.Y;
            toReturn.Z = quaternion.Z;
            toReturn.W = quaternion.W;
            return toReturn;
        }

        public static BEPUutilities.Quaternion ToBepu(this Quaternion quaternion)
        {
            BEPUutilities.Quaternion toReturn;
            toReturn.X = quaternion.X;
            toReturn.Y = quaternion.Y;
            toReturn.Z = quaternion.Z;
            toReturn.W = quaternion.W;
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Quaternion bepuQuaternion, out Quaternion quaternion)
        {
            quaternion.X = bepuQuaternion.X;
            quaternion.Y = bepuQuaternion.Y;
            quaternion.Z = bepuQuaternion.Z;
            quaternion.W = bepuQuaternion.W;
        }

        public static void ToBepu(this ref Quaternion quaternion, out BEPUutilities.Quaternion bepuQuaternion)
        {
            bepuQuaternion.X = quaternion.X;
            bepuQuaternion.Y = quaternion.Y;
            bepuQuaternion.Z = quaternion.Z;
            bepuQuaternion.W = quaternion.W;
        }

        //Ray
        public static BEPUutilities.Ray ToBepu(this Ray ray)
        {
            BEPUutilities.Ray toReturn;
            ray.Position.ToBepu(out toReturn.Position);
            ray.Direction.ToBepu(out toReturn.Direction);
            return toReturn;
        }

        public static void ToBepu(this ref Ray ray, out BEPUutilities.Ray bepuRay)
        {
            ray.Position.ToBepu(out bepuRay.Position);
            ray.Direction.ToBepu(out bepuRay.Direction);
        }

        public static Ray ToXNA(this BEPUutilities.Ray ray)
        {
            Ray toReturn;
            ray.Position.ToXNA(out toReturn.Position);
            ray.Direction.ToXNA(out toReturn.Direction);
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Ray ray, out Ray xnaRay)
        {
            ray.Position.ToXNA(out xnaRay.Position);
            ray.Direction.ToXNA(out xnaRay.Direction);
        }

        //BoundingBox
        public static BoundingBox ToXNA(this BEPUutilities.BoundingBox boundingBox)
        {
            BoundingBox toReturn;
            boundingBox.Min.ToXNA(out toReturn.Min);
            boundingBox.Max.ToXNA(out toReturn.Max);
            return toReturn;
        }

        public static BEPUutilities.BoundingBox ToBepu(this BoundingBox boundingBox)
        {
            BEPUutilities.BoundingBox toReturn;
            boundingBox.Min.ToBepu(out toReturn.Min);
            boundingBox.Max.ToBepu(out toReturn.Max);
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.BoundingBox boundingBox, out BoundingBox xnaBoundingBox)
        {
            boundingBox.Min.ToXNA(out xnaBoundingBox.Min);
            boundingBox.Max.ToXNA(out xnaBoundingBox.Max);
        }

        public static void ToBepu(this ref BoundingBox boundingBox, out BEPUutilities.BoundingBox bepuBoundingBox)
        {
            boundingBox.Min.ToBepu(out bepuBoundingBox.Min);
            boundingBox.Max.ToBepu(out bepuBoundingBox.Max);
        }

        //BoundingSphere
        public static BoundingSphere ToXNA(this BEPUutilities.BoundingSphere boundingSphere)
        {
            BoundingSphere toReturn;
            boundingSphere.Center.ToXNA(out toReturn.Center);
            toReturn.Radius = boundingSphere.Radius;
            return toReturn;
        }

        public static BEPUutilities.BoundingSphere ToBepu(this BoundingSphere boundingSphere)
        {
            BEPUutilities.BoundingSphere toReturn;
            boundingSphere.Center.ToBepu(out toReturn.Center);
            toReturn.Radius = boundingSphere.Radius;
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.BoundingSphere boundingSphere, out BoundingSphere xnaBoundingSphere)
        {
            boundingSphere.Center.ToXNA(out xnaBoundingSphere.Center);
            xnaBoundingSphere.Radius = boundingSphere.Radius;
        }

        public static void ToBepu(this ref BoundingSphere boundingSphere, out BEPUutilities.BoundingSphere bepuBoundingSphere)
        {
            boundingSphere.Center.ToBepu(out bepuBoundingSphere.Center);
            bepuBoundingSphere.Radius = boundingSphere.Radius;
        }

        //Plane
        public static Plane ToXNA(this BEPUutilities.Plane plane)
        {
            Plane toReturn;
            plane.Normal.ToXNA(out toReturn.Normal);
            toReturn.D = plane.D;
            return toReturn;
        }

        public static BEPUutilities.Plane ToBepu(this Plane plane)
        {
            BEPUutilities.Plane toReturn;
            plane.Normal.ToBepu(out toReturn.Normal);
            toReturn.D = plane.D;
            return toReturn;
        }

        public static void ToXNA(this ref BEPUutilities.Plane plane, out Plane xnaPlane)
        {
            plane.Normal.ToXNA(out xnaPlane.Normal);
            xnaPlane.D = plane.D;
        }

        public static void ToBepu(this ref Plane plane, out BEPUutilities.Plane bepuPlane)
        {
            plane.Normal.ToBepu(out bepuPlane.Normal);
            bepuPlane.D = plane.D;
        }
    }
}
