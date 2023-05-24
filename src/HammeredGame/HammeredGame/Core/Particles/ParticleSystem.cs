using System;
using System.Linq;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Graphics;
using HammeredGame.Graphics.ForwardRendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Core.Particles
{
    /// <summary>
    /// ParticleSystem is a class for simple CPU-side particle systems. This means that performance
    /// is likely to be suboptimal for large numbers of particles, although a few dozen shouldn't be
    /// a problem. Performing particle update logic within the GPU would be better (and it would be
    /// even better if all particles were rendered with one call to Draw with instances geometry),
    /// but understanding vertex/index buffer complications (especially with Models) was too
    /// difficult, and also we can reuse bepuphysics if we're on the CPU to do updates and collisions.
    /// <para/>
    /// A lot of the code is based on the Particle3D sample from the Monogame-Samples repository
    /// originally created by Microsoft and now maintained by CartBlanche (https://github.com/CartBlanche/MonoGame-Samples/tree/master/Particle3DSample).
    /// </summary>
    public class ParticleSystem
    {
        // Settings class controls the appearance and animation of this particle system.
        private readonly ParticleSettings settings;

        // An array of particles, treated as a circular queue.
        private readonly Particle[] particles;

        private int firstActiveParticle;
        private int firstInactiveParticle;

        // Store the current time, in seconds.
        private float currentTime;

        // The main shading effect
        private readonly DefaultShadingEffect effect;

        // The physics space
        private readonly Space activeSpace;
        private readonly GraphicsDevice gpu;

        // Shared random number generator.
        static readonly Random random = new();

        // We'll use hardware instancing to render the particles efficiently using one Draw call for
        // all particles combined. For this, we need to pass an extra vertex buffer containing
        // world-space transformation matrices for each particle. We declare the format for that
        // buffer here since it's constant. It is four Vector4s, representing a 4x4 matrix. In the
        // shader side, this can be accessed as a BLENDWEIGHT input to the vertex shader.
        private readonly VertexDeclaration instanceTransformVertexDeclarations = new(
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3));

        public ParticleSystem(ParticleSettings settings, GraphicsDevice gpu, ContentManager content, Space space)
        {
            this.settings = settings;
            this.gpu = gpu;
            this.activeSpace = space;

            // Allocate an array of structs to hold the particles.
            particles = new Particle[settings.MaxParticles];

            this.effect = new DefaultShadingEffect(content);
        }

        /// <summary>
        /// Update the particle system, getting rid of any old particles and updating any active ones.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            RetireActiveParticles();
            UpdateActiveParticles();

            // If we let our timer go on increasing for ever, it would eventually run out of
            // floating point precision, at which point the particles would render incorrectly. An
            // easy way to prevent this is to notice that the time value doesn't matter when no
            // particles are being drawn, so we can reset it back to zero any time the active queue
            // is empty.
            if (firstActiveParticle == firstInactiveParticle)
                currentTime = 0;
        }

        /// <summary>
        /// Removes old particles from the physics space and updates the pointer indices so that the
        /// active particle portion no longer contains retired particles.
        /// </summary>
        private void RetireActiveParticles()
        {
            float particleDuration = (float)settings.Duration.TotalSeconds;

            // Go through the active particles from oldest to newest (assumes that all particles
            // have the same duration), and retire particles until we hit one that's still young.
            while (firstActiveParticle != firstInactiveParticle)
            {
                // Is this particle old enough to retire?
                float particleAge = currentTime - particles[firstActiveParticle].Time;

                if (particleAge < particleDuration)
                    break;

                // Remove from physics space
                activeSpace.Remove(particles[firstActiveParticle].Entity);

                // Move the first active particle pointer within the circular buffer.
                firstActiveParticle++;

                if (firstActiveParticle >= settings.MaxParticles)
                    firstActiveParticle = 0;
            }
        }

        /// <summary>
        /// Updates all active particles' animations and other visual properties throughout its
        /// lifetime. Physics updates are handled through bepuphysics.
        /// </summary>
        private void UpdateActiveParticles()
        {
            float particleDuration = (float)settings.Duration.TotalSeconds;

            for (int i = firstActiveParticle; i != firstInactiveParticle;)
            {
                float particleAge = currentTime - particles[i].Time;

                // Interpolate a size between start and end sizes
                particles[i].Size = MathHelper.Lerp(particles[i].StartSize, particles[i].EndSize, particleAge / particleDuration);
                i = (i + 1) % settings.MaxParticles;
            }
        }

        /// <summary>
        /// Copy any shadow map related parameters from the parent object to the particles, since
        /// they get only assigned to the object's main effect and not to any particle systems (in
        /// GameRenderer). The parameters set by this function are ignored anyway if we're still in
        /// the shadow map generation pass or if the particles are set to "unlit", so it is safe to
        /// call this function on every Draw() call.
        /// </summary>
        /// <param name="parentObjectEffect"></param>
        public void CopyShadowMapParametersFrom(AbstractForwardRenderingEffect parentObjectEffect)
        {
            effect.SunDepthTexture = parentObjectEffect.SunDepthTexture;
            effect.SunView = parentObjectEffect.SunView;
            effect.SunProjection = parentObjectEffect.SunProjection;
            effect.ShadowMapDepthBias = parentObjectEffect.ShadowMapDepthBias;
            effect.ShadowMapNormalOffset = parentObjectEffect.ShadowMapNormalOffset;
        }

        /// <summary>
        /// Draws the particle system.
        /// </summary>
        public void Draw(GameTime gameTime, Matrix view, Matrix projection, Vector3 cameraPosition, SceneLightSetup lights)
        {
            Matrix[] meshTransforms = new Matrix[settings.Model.Bones.Count];
            settings.Model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            // Find the number of active particles, accounting for any wrap-around
            int numberParticles = (firstInactiveParticle + settings.MaxParticles - firstActiveParticle) % settings.MaxParticles;

            // Create an array of instance world-space transform matrices that we'll send to the GPU
            // for instancing with a single mesh. The size of this changes whenever a new particle
            // is added or retired, so it's recreated on each Draw call.
            Matrix[] instanceTransforms = new Matrix[numberParticles];

            // Go through the active particles and populate the instance transform array
            int instanceTransformIndex = 0;
            for (int i = firstActiveParticle; i != firstInactiveParticle;)
            {
                // We create the world matrix using the scale and world-space translation
                instanceTransforms[instanceTransformIndex] = Matrix.CreateScale(particles[i].Size) * Matrix.CreateTranslation(particles[i].Entity.Position.ToXNA());

                i = (i + 1) % settings.MaxParticles;
                instanceTransformIndex++;
            }

            // If there is nothing to render, stop here to avoid sending zero-buffers to the GPU and
            // causing errors.s
            if (instanceTransforms.Length == 0)
                return;

            // Create a vertex buffer for instance transform matrices, and fill it with the above data.
            var instanceVertexBuffer = new DynamicVertexBuffer(gpu, instanceTransformVertexDeclarations, numberParticles, BufferUsage.WriteOnly);
            instanceVertexBuffer.SetData(instanceTransforms, 0, numberParticles, SetDataOptions.Discard);

            foreach (ModelMesh mesh in settings.Model.Meshes)
            {
                // Proactively select the main shading technique instead of letting GameRenderer
                // choose it like for GameObjects, since we won't render shadows for particles
                // plus we want to use instancing
                effect.CurrentPass = AbstractForwardRenderingEffect.Pass.MainShadingInstanced;

                // Load in the shader and set its parameters
                effect.World = mesh.ParentBone.Transform;
                effect.View = view;
                effect.Projection = projection;
                effect.CameraPosition = cameraPosition;

                // Pre-compute the inverse transpose of the world matrix to use in shader
                Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform));

                effect.WorldInverseTranspose = worldInverseTranspose;

                effect.Lit = settings.AffectedByLight;

                // Set light parameters
                effect.DirectionalLightColors = lights.Directionals.Select(l => l.LightColor.ToVector4()).Append(lights.Sun.LightColor.ToVector4()).ToArray();
                effect.DirectionalLightIntensities = lights.Directionals.Select(l => l.Intensity).Append(lights.Sun.Intensity).ToArray();
                effect.DirectionalLightDirections = lights.Directionals.Select(l => l.Direction).Append(lights.Sun.Direction).ToArray();
                effect.SunLightIndex = lights.Directionals.Count;

                effect.AmbientLightColor = lights.Ambient.LightColor.ToVector4();
                effect.AmbientLightIntensity = lights.Ambient.Intensity;

                // Set tints for the diffuse color, ambient color, and specular color. These are
                // multiplied in the shader by the light color and intensity, as well as each
                // component's weight.
                effect.MaterialDiffuseColor = Color.White.ToVector4();
                effect.MaterialAmbientColor = Color.White.ToVector4();
                effect.MaterialHasSpecular = false;
                // Uncomment if specular; will use Blinn-Phong.
                // effect.MaterialSpecularColor = Color.White.ToVector4();
                // effect.MaterialShininess = 20f;

                effect.ModelTexture = settings.Texture;
                // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                effect.ModelTextureGammaCorrection = true;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // We usually don't worry about the vertex/index buffers when using
                    // ModelMesh.Draw(), but in this case we want to pass the instance transform
                    // vertex buffer as well, so we explicitly tell the GPU to read from both the
                    // model vertex buffer plus our instanceVertexBuffer.
                    gpu.SetVertexBuffers(
                        new VertexBufferBinding(part.VertexBuffer, part.VertexOffset, 0),
                        new VertexBufferBinding(instanceVertexBuffer, 0, 1));

                    // And also tell it to read from the model index buffer.
                    gpu.Indices = part.IndexBuffer;

                    effect.Apply();
                    gpu.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, part.StartIndex, part.PrimitiveCount, numberParticles);
                }
            }
        }

        /// <summary>
        /// Add a new particle to the particle system.
        /// </summary>
        /// <param name="position">The world-space position to spawn the particle at</param>
        /// <param name="velocity">
        /// The velocity of the parent object, or otherwise the initial particle velocity. Any
        /// randomness to velocity will be applied on top of this.
        /// </param>
        public void AddParticle(Vector3 position, Vector3 velocity)
        {
            // Figure out where in the circular queue to allocate the new particle.
            int nextFreeParticle = firstInactiveParticle + 1;

            if (nextFreeParticle >= settings.MaxParticles)
                nextFreeParticle = 0;

            // If there are no free particles, we just have to give up.
            if (nextFreeParticle == firstActiveParticle)
                return;

            // Adjust the input velocity based on how much
            // this particle system wants to be affected by it.
            velocity *= settings.EmitterVelocitySensitivity;

            // Add in some random amount of horizontal velocity.
            float horizontalVelocity = MathHelper.Lerp(settings.MinHorizontalVelocity,
                            settings.MaxHorizontalVelocity,
                            (float)random.NextDouble());

            double horizontalAngle = random.NextDouble() * MathHelper.TwoPi;

            velocity.X += horizontalVelocity * (float)Math.Cos(horizontalAngle);
            velocity.Z += horizontalVelocity * (float)Math.Sin(horizontalAngle);

            // Add in some random amount of vertical velocity.
            velocity.Y += MathHelper.Lerp(settings.MinVerticalVelocity,
                    settings.MaxVerticalVelocity,
                    (float)random.NextDouble());

            // Set up a small sphere as the physics entity (it could've been anything), and give it
            // the position and velocity as specified. We give it a very small but explicit mass to
            // make it a dynamic object (so it doesn't push around things like having infinite mass).
            particles[firstInactiveParticle].Entity = new Sphere(position.ToBepu(), 0.1f, 0.01f)
            {
                Position = position.ToBepu(),
                LinearVelocity = velocity.ToBepu(),
                Tag = "Particle"
            };

            // Pass the particle to the collision information so we can pattern match and ignore
            // particle collisions from certain objects.
            particles[firstInactiveParticle].Entity.CollisionInformation.Tag = particles[firstInactiveParticle];

            // If we should ignore physics collision responses, set the solver to NoSolver. This
            // will still trigger collision events, it just won't solve constraints to e.g. bounce back.
            if (settings.IgnoreCollisionResponses)
            {
                particles[firstInactiveParticle].Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
            }

            // Set the gravity of the particles
            particles[firstInactiveParticle].Entity.Gravity = settings.Gravity;

            // Add the physics entity to the physics space
            activeSpace.Add(particles[firstInactiveParticle].Entity);

            particles[firstInactiveParticle].Time = currentTime;

            // Select a random starting size
            particles[firstInactiveParticle].StartSize = MathHelper.Lerp(settings.MinStartSize, settings.MaxStartSize, (float)random.NextDouble());
            particles[firstInactiveParticle].EndSize = MathHelper.Lerp(settings.MinEndSize, settings.MaxEndSize, (float)random.NextDouble());
            particles[firstInactiveParticle].Size = particles[firstInactiveParticle].StartSize;

            firstInactiveParticle = nextFreeParticle;
        }
    }
}
