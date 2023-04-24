using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace HammeredGame.Game
{
    public abstract record Light(bool Enabled, Color LightColor, float Intensity);
    public record SunLight(bool Enabled, Color LightColor, float Intensity, Vector3 Direction) : Light(Enabled, LightColor, Intensity);

    public record InfiniteDirectionalLight(bool Enabled, Color LightColor, float Intensity, Vector3 Direction) : Light(Enabled, LightColor, Intensity);

    public record AmbientLight(bool Enabled, Color LightColor, float Intensity) : Light(Enabled, LightColor, Intensity);

    public record PointLight(bool Enabled, Color LightColor, Vector3 Position, float Radius, float Intensity) : Light(Enabled, LightColor, Intensity);

    public record SpotLight(bool Enabled, Color LightColor, Vector3 Position, Vector3 Direction, float Angle, float Falloff, float Intensity) : Light(Enabled, LightColor, Intensity);

    public record SceneLightSetup(SunLight Sun, List<InfiniteDirectionalLight> Directionals, AmbientLight Ambient, List<PointLight> Points, List<SpotLight> Spots);
}
