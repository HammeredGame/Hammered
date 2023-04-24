using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Graphics
{
    /// <summary>
    /// The SceneLightSetup type ensures that a scene must have one sunlight for drawing shadows,
    /// several directional lights, one ambient light, several point lights and several spotlights.
    /// </summary>
    /// <param name="Sun"></param>
    /// <param name="Directionals"></param>
    /// <param name="Ambient"></param>
    /// <param name="Points"></param>
    /// <param name="Spots"></param>
    public record SceneLightSetup(SunLight Sun, List<InfiniteDirectionalLight> Directionals, AmbientLight Ambient, List<PointLight> Points, List<SpotLight> Spots);
}
