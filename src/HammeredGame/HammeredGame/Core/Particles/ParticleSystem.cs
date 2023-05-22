using System;
using System.Linq;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Graphics;
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
        public Effect Effect;

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

            this.Effect = content.Load<Effect>("Effects/ForwardRendering/MainShading");
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

                // Select a random end size for the particle. (This will select a different end size
                // on each frame for the same particle, which isn't ideal since it's wasted
                // computation but is unnoticeable)
                float randomEndSize = MathHelper.Lerp(settings.MinEndSize, settings.MaxEndSize, (float)random.NextDouble());

                // Interpolate towards it
                particles[i].Size = MathHelper.Lerp(particles[i].Size, randomEndSize, particleAge / particleDuration);
                i = (i + 1) % settings.MaxParticles;
            }
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
                instanceTransforms[instanceTransformIndex] = Matrix.CreateScale(particles[i].Size) * Matrix.CreateTranslation(MathConverter.Convert(particles[i].Entity.Position));

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

                    // Proactively select the main shading technique instead of letting GameRenderer
                    // choose it like for GameObjects, since we won't render shadows for particles
                    // plus we want to use instancing
                    Effect.CurrentTechnique = Effect.Techniques["MainShadingInstanced"];

                    // Load in the shader and set its parameters
                    part.Effect = this.Effect;

                    part.Effect.Parameters["World"]?.SetValue(mesh.ParentBone.Transform);
                    part.Effect.Parameters["View"]?.SetValue(view);
                    part.Effect.Parameters["Projection"]?.SetValue(projection);
                    part.Effect.Parameters["CameraPosition"]?.SetValue(cameraPosition);

                    // Pre-compute the inverse transpose of the world matrix to use in shader
                    Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform));

                    part.Effect.Parameters["WorldInverseTranspose"]?.SetValue(worldInverseTranspose);

                    // Set light parameters
                    part.Effect.Parameters["DirectionalLightColors"]?.SetValue(lights.Directionals.Select(l => l.LightColor.ToVector4()).Append(lights.Sun.LightColor.ToVector4()).ToArray());
                    part.Effect.Parameters["DirectionalLightIntensities"]?.SetValue(lights.Directionals.Select(l => l.Intensity).Append(lights.Sun.Intensity).ToArray());
                    part.Effect.Parameters["DirectionalLightDirections"]?.SetValue(lights.Directionals.Select(l => l.Direction).Append(lights.Sun.Direction).ToArray());
                    part.Effect.Parameters["SunLightIndex"]?.SetValue(lights.Directionals.Count);

                    part.Effect.Parameters["AmbientLightColor"]?.SetValue(lights.Ambient.LightColor.ToVector4());
                    part.Effect.Parameters["AmbientLightIntensity"]?.SetValue(lights.Ambient.Intensity);

                    // Set tints for the diffuse color, ambient color, and specular color. These are
                    // multiplied in the shader by the light color and intensity, as well as each
                    // component's weight.
                    part.Effect.Parameters["MaterialDiffuseColor"]?.SetValue(Color.White.ToVector4());
                    part.Effect.Parameters["MaterialAmbientColor"]?.SetValue(Color.White.ToVector4());
                    part.Effect.Parameters["MaterialHasSpecular"].SetValue(false);
                    // Uncomment if specular; will use Blinn-Phong.
                    // part.Effect.Parameters["MaterialSpecularColor"]?.SetValue(Color.White.ToVector4());
                    // part.Effect.Parameters["MaterialShininess"]?.SetValue(20f);

                    part.Effect.Parameters["ModelTexture"]?.SetValue(settings.Texture);
                    // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                    part.Effect.Parameters["ModelTextureGammaCorrection"]?.SetValue(true);

                    foreach (EffectPass pass in part.Effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        gpu.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, part.StartIndex, part.PrimitiveCount, numberParticles);
                    }
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
            particles[firstInactiveParticle].Entity = new Sphere(MathConverter.Convert(position), 0.1f, 0.01f)
            {
                Position = MathConverter.Convert(position),
                LinearVelocity = MathConverter.Convert(velocity),
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
            particles[firstInactiveParticle].Size = MathHelper.Lerp(settings.MinStartSize, settings.MaxStartSize, (float)random.NextDouble());

            firstInactiveParticle = nextFreeParticle;
        }
    }
}
