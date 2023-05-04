using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HammeredGame.Graphics
{
    // All lights can be enabled or disabled, have a single color (for simplicity,
    // both the diffuse and specular colours are the same) and have an intensity.
    public abstract class Light
    {
        public Color LightColor = Color.White;
        public float Intensity = 1f;
        protected Light(Color lightColor, float intensity)
        {
            LightColor = lightColor;
            Intensity = intensity;
        }
    }

    // A single sun light can exist per scene, and it is essentially an infinite
    // directional light but will also cast shadows.
    public class SunLight : Light
    {
        public Vector3 Direction;
        public SunLight(Color lightColor, float intensity, Vector3 direction) : base(lightColor, intensity)
        {
            Direction = direction;
        }

        /// <summary>
        /// Create a view matrix, a projection matrix, and the position for the sun, for rendering
        /// depth/shadow maps. It uses the camera frustum taken as input to calculate the
        /// world-space bounds that are visible to the camera, and tries to find the smallest
        /// sunlight frustum that contains those bounds. This unfortunately means that the sunlight
        /// essentially moves when the camera frustum moves as well, which can introduce flickering
        /// shadows -- but is better than not having any shadows outside a preset region on the map.
        /// <para/>
        /// Because it uses the camera frustum to decide how much of the world is visible (and
        /// therefore how big the shadow map should cover in world-space), cameras that have a
        /// larger Field of View or cameras that are zoomed out further will have lower quality
        /// shadows, while cameras up close will get all of the shadow map to a small world section
        /// so it'll be high quality.
        /// </summary>
        /// <param name="cameraFrustum"></param>
        /// <returns></returns>
        public (Matrix, Matrix, Vector3) CreateSunViewProjPosition(BoundingFrustum cameraFrustum)
        {
            // Implementation courtesy of https://github.com/CartBlanche/MonoGame-Samples/blob/master/ShadowMapping/ShadowMapping.cs

            // A transformation matrix that will rotate XYZ so its Forward points to the sun
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero, -Direction, Vector3.Up);

            // Get the corners of the frustum
            Vector3[] frustumCorners = cameraFrustum.GetCorners();

            // Transform the positions of the corners into the direction of the light
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = Vector3.Transform(frustumCorners[i], lightRotation);
            }

            // Find the smallest box around the points
            BoundingBox lightBox = BoundingBox.CreateFromPoints(frustumCorners);

            Vector3 boxSize = lightBox.Max - lightBox.Min;
            Vector3 halfBoxSize = boxSize * 0.5f;

            // The position of the light should be in the center of the back
            // pannel of the box.
            Vector3 lightPosition = lightBox.Min + halfBoxSize;
            lightPosition.Z = lightBox.Min.Z;

            // We need the position back in world coordinates so we transform
            // the light position by the inverse of the lights rotation
            lightPosition = Vector3.Transform(lightPosition, Matrix.Invert(lightRotation));

            // Create the view matrix for the light
            Matrix lightView = Matrix.CreateLookAt(lightPosition, lightPosition - Direction, Vector3.Up);

            // Create the projection matrix for the light
            // The projection is orthographic since we are using a directional light
            Matrix lightProjection = Matrix.CreateOrthographic(boxSize.X, boxSize.Y, -boxSize.Z, boxSize.Z);

            return (lightView, lightProjection, lightPosition);
        }
    }

    // An infinite directional light will affect all objects in the scene purely
    // based on their normal direction.
    public class InfiniteDirectionalLight : Light
    {
        public Vector3 Direction;
        public InfiniteDirectionalLight(Color lightColor, float intensity, Vector3 direction) : base(lightColor, intensity)
        {
            Direction = direction;
        }
    }


    // An ambient light affects all pixels on the screen, should be used with
    // care since it reduces contrast of the final image.
    public class AmbientLight : Light
    {
        public AmbientLight(Color lightColor, float intensity) : base(lightColor, intensity) { }
    }

    // Point lights are not implemented yet.
    public class PointLight : Light
    {
        public Vector3 Position;
        public float Radius;
        public PointLight(Color lightColor, float intensity, Vector3 position, float radius) : base(lightColor, intensity)
        {
            Position = position;
            Radius = radius;
        }
    }

    // Spot lights are not implemented yet.
    public class SpotLight : Light
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float Angle;
        public float FallOff;
        public SpotLight(Color lightColor, float intensity, Vector3 position, Vector3 direction, float angle, float fallOff) : base(lightColor, intensity)
        {
            Position = position;
            Direction = direction;
            Angle = angle;
            FallOff = fallOff;
        }
    }
}
