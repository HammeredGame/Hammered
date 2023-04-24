using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HammeredGame.Graphics
{
    // All lights can be enabled or disabled, have a single color (for simplicity,
    // both the diffuse and specular colours are the same) and have an intensity.
    public abstract record Light(bool Enabled, Color LightColor, float Intensity);

    // A single sun light can exist per scene, and it is essentially an infinite
    // directional light but will also cast shadows.
    public record SunLight(bool Enabled, Color LightColor, float Intensity, Vector3 Direction) : Light(Enabled, LightColor, Intensity);

    // An infinite directional light will affect all objects in the scene purely
    // based on their normal direction.
    public record InfiniteDirectionalLight(bool Enabled, Color LightColor, float Intensity, Vector3 Direction) : Light(Enabled, LightColor, Intensity);

    // An ambient light affects all pixels on the screen, should be used with
    // care since it reduces contrast of the final image.
    public record AmbientLight(bool Enabled, Color LightColor, float Intensity) : Light(Enabled, LightColor, Intensity);

    // Point lights are not implemented yet.
    public record PointLight(bool Enabled, Color LightColor, Vector3 Position, float Radius, float Intensity) : Light(Enabled, LightColor, Intensity);

    // Spot lights are not implemented yet.
    public record SpotLight(bool Enabled, Color LightColor, Vector3 Position, Vector3 Direction, float Angle, float Falloff, float Intensity) : Light(Enabled, LightColor, Intensity);
}
