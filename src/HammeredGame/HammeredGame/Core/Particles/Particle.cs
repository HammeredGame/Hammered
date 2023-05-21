using BEPUphysics.Entities;

namespace HammeredGame.Core.Particles
{
    /// <summary>
    /// A particle represents a single particle that is created through a particle system.
    /// </summary>
    struct Particle
    {
        public Entity Entity;

        // The time (in seconds) at which this particle was created.
        public float Time;

        // The current size of the particle (in world units).
        public float Size;
    }
}
