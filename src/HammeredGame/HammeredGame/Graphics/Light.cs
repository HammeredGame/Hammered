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
