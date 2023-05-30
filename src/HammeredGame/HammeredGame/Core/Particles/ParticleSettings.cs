using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Core.Particles
{
    /// <summary>
    /// Settings class describes all the tweakable options used to control the appearance of a
    /// particle system.
    /// </summary>
    public class ParticleSettings
    {
        // The texture and model to apply to all particles.
        public Texture2D Texture;
        public Model Model;

        // Maximum number of particles that can be displayed at one time. If AddParticle is called
        // on the particle system when it is already at full capacity, it will be ignored.
        public int MaxParticles = 100;

        // How long these particles will last.
        public TimeSpan Duration = TimeSpan.FromSeconds(1);

        // If greater than zero, some particles will last a shorter time than others.
        public float DurationRandomness = 0;

        // Controls how much particles are influenced by the velocity of the object which created
        // them. You can see this in action with the explosion effect, where the flames continue to
        // move in the same direction as the source projectile. The projectile trail particles, on
        // the other hand, set this value very low so they are less affected by the velocity of the projectile.
        public float EmitterVelocitySensitivity = 1;

        // Range of values controlling how much X and Z axis velocity to give each particle. Values
        // for individual particles are randomly chosen from somewhere between these limits.
        public float MinHorizontalVelocity = 0;
        public float MaxHorizontalVelocity = 0;

        // Range of values controlling how much Y axis velocity to give each particle.
        // Values for individual particles are randomly chosen from somewhere between
        // these limits.
        public float MinVerticalVelocity = 0;
        public float MaxVerticalVelocity = 0;

        // Controls how the particle velocity will change over their lifetime. If set to 1,
        // particles will keep going at the same speed as when they were created. If set to 0,
        // particles will come to a complete stop right before they die. Values greater than 1 make
        // the particles speed up over time.
        public float EndVelocity = 1;

        // Range of values controlling the particle color and alpha. Values for individual particles
        // are randomly chosen from somewhere between these limits.
        public Color MinColor = Color.White;
        public Color MaxColor = Color.White;

        // Range of values controlling how big the particles are when first created. Values for
        // individual particles are randomly chosen from somewhere between these limits.
        public float MinStartSize = 100;
        public float MaxStartSize = 100;

        // Range of values controlling how big particles become at the end of their life. Values for
        // individual particles are randomly chosen from somewhere between these limits.
        public float MinEndSize = 100;
        public float MaxEndSize = 100;

        public Quaternion MinStartRotation = Quaternion.Identity;
        public Quaternion MaxStartRotation = Quaternion.Identity;

        // Whether to ignore collision responses for particles when interacting with the surrounding environment.
        public bool IgnoreCollisionResponses = false;

        // Gravity to apply to the particles.
        public BEPUutilities.Vector3 Gravity = BEPUutilities.Vector3.Zero;

        // Whether particles are affected by light. If this is false, all lights will be ignored and
        // objects will be rendered flat.
        public bool AffectedByLight = true;
    }
}
