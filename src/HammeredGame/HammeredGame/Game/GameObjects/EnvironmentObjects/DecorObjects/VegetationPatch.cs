using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Core;
using HammeredGame.Graphics;
using HammeredGame.Graphics.ForwardRendering;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.DecorObjects
{
    /// <summary>
    /// The <c>VegetationPatch</c> class refers to  a patch of vegetation.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="DecorObject "/> --> <see cref="VegetationPatch"/>
    /// </para>
    /// </remarks>
    internal class VegetationPatch : DecorObject, IImGui
    {
        private readonly List<Matrix> instanceTransformations = new();

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

        public VegetationPatch(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            Box box = entity as Box ?? throw new Exception("Only boxes supported for vegetation patches");
            RegenerateVegetations(box.Height, box.Width, box.Length);
        }

        private void RegenerateVegetations(float distance, float patchSizeX, float patchSizeZ)
        {
            if (patchSizeX <= 0 || patchSizeZ <= 0 || distance <= 0) return;

            instanceTransformations.Clear();

            // Generate poisson disc sample points that are `distance` apart within the location bounds of `-patchSizeX/2` to `patchSizeX/2` and `-patchSizeZ/2` to `patchSizeZ/2`.

            // We'll use a grid of cells to keep track of the points we've already generated samples around.
            // The grid cell size is `distance / sqrt(2)`, which is the maximum distance between two points
            // in a poisson disc sample. We'll use a 2D array of bools to represent the grid, and initialize
            // all cells to false to indicate that they are empty.
            int gridCellSize = (int)(distance / Math.Sqrt(2));
            if (gridCellSize <= 0) return;
            int gridWidth = (int)MathF.Ceiling(patchSizeX / gridCellSize);
            int gridHeight = (int)MathF.Ceiling(patchSizeZ / gridCellSize);
            Vector2?[,] grid = new Vector2?[gridWidth, gridHeight];

            // We'll also use a list to keep track of the points we've already generated samples around.
            // This is used to ensure that we don't generate samples too close to each other.
            List<Vector2> points = new();

            // We'll start by generating a sample point at the center of the location bounds.
            Vector2 center = new(patchSizeX / 2, patchSizeZ / 2);
            points.Add(center);

            // We'll also add the center point to the grid, so that we can keep track of it.
            grid[(int)(center.X / gridCellSize), (int)(center.Y / gridCellSize)] = center;

            // We'll use a queue to keep track of the points we still need to generate samples around.
            List<Vector2> pointsToProcess = new()
            {
                center
            };

            // We'll keep generating samples until there are no more points to process.
            while (pointsToProcess.Count > 0)
            {
                // Get the next point to process.
                int randomIndex = Random.Shared.Next(0, pointsToProcess.Count);
                Vector2 point = pointsToProcess[randomIndex];
                pointsToProcess.RemoveAt(randomIndex);

                for (int i = 0; i < 30; i++)
                {
                    // Generate a random sample around the point.
                    Vector2 sample = GenerateRandomPointAround(point, distance);

                    // If the sample is within the location bounds, and not too close to any other points,
                    // we'll add it to the list of points and the queue of points to process.
                    if (IsPointInBounds(sample, patchSizeX, patchSizeZ) && !IsInNeighbourhood(sample, grid, distance, gridCellSize))
                    {
                        points.Add(sample);
                        pointsToProcess.Add(sample);
                        grid[(int)(sample.X / gridCellSize), (int)(sample.Y / gridCellSize)] = sample;
                    }
                }
            }

            // Now that we've generated all the points, we'll create a transformation matrix for each
            // point and add it to the list of instance transformations.

            foreach (Vector2 point in points) {
                Vector3 translation = new(point.X - patchSizeX / 2, 0, point.Y - patchSizeZ / 2);
                // We'll use a random rotation around the Y axis for each instance.
                Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, Random.Shared.NextSingle() * MathHelper.TwoPi);

                // For scale, we'll use a random scale factor between 0.5 - 1.0 that gets multiplied
                // by the base scale.
                float scale = (Random.Shared.NextSingle() * 0.5f + 0.5f);

                // We'll also reduce the scale down to zero near the edges
                float distToEdge = Math.Min(Math.Min(point.X, patchSizeX - point.X), Math.Min(point.Y, patchSizeZ - point.Y));
                scale = MathHelper.Lerp(0.3f, scale, Math.Min(distToEdge / 2f, 1f));

                instanceTransformations.Add(Matrix.CreateScale(Scale * scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation));
            }
        }

        private bool IsInNeighbourhood(Vector2 point, Vector2?[,] grid, float distance, int gridCellSize)
        {
            int gridX = (int)(point.X / gridCellSize);
            int gridY = (int)(point.Y / gridCellSize);

            // Check if the point is too close to any other points.
            for (int i = -5; i < 5; i++)
            {
                for (int j = -5; j < 5; j++)
                {
                    if (gridX + i < 0 || gridX + i >= grid.GetLength(0) || gridY + j < 0 || gridY + j >= grid.GetLength(1)) continue;

                    if (grid[gridX + i, gridY + j] is Vector2 gridValue)
                    {
                        if (Vector2.Distance(gridValue, point) < distance)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsPointInBounds(Vector2 point, float sizeX, float sizeZ)
        {
            if (point.X < 0 || point.X >= sizeX || point.Y < 0 || point.Y >= sizeZ)
            {
                return false;
            }
            // rounded rectangle check
            float radius = 10f;
            if (point.X < radius && point.Y < radius && Vector2.Distance(point, new(radius, radius)) > radius) {
                return false;
            }
            if (point.X < radius && point.Y > sizeZ - radius && Vector2.Distance(point, new(radius, sizeZ - radius)) > radius)
            {
                return false;
            }
            if (point.X > sizeX - radius && point.Y < radius && Vector2.Distance(point, new(sizeX - radius, radius)) > radius)
            {
                return false;
            }
            if (point.X > sizeX - radius && point.Y > sizeZ - radius && Vector2.Distance(point, new(sizeX - radius, sizeZ - radius)) > radius)
            {
                return false;
            }
            return true;
        }

        private Vector2 GenerateRandomPointAround(Vector2 point, float distance)
        {
            // Generate a random point in polar coordinates.
            float angle = Random.Shared.NextSingle() * MathHelper.TwoPi;
            float radius = (Random.Shared.NextSingle() * distance) + distance;

            // Convert the polar coordinates to cartesian coordinates.
            float x = point.X + radius * MathF.Cos(angle);
            float y = point.Y + radius * MathF.Sin(angle);

            return new(x, y);
        }

        public override void DrawModel(GameTime gameTime, Model model, Matrix view, Matrix projection, Vector3 cameraPosition, Texture2D tex, SceneLightSetup lights)
        {
            Matrix[] meshTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            // Create an array of instance world-space transform matrices that we'll send to the GPU
            // for instancing with a single mesh. The size of this changes whenever a new particle
            // is added or retired, so it's recreated on each Draw call.
            Matrix[] worldSpaceTransforms = new Matrix[instanceTransformations.Count];

            for (int i = 0; i < instanceTransformations.Count; i++)
            {
                worldSpaceTransforms[i] = instanceTransformations[i] * Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position);
            }

            // If there is nothing to render, stop here to avoid sending zero-buffers to the GPU and
            // causing errors.s
            if (instanceTransformations.Count == 0)
                return;

            // Create a vertex buffer for instance transform matrices, and fill it with the above data.
            var instanceVertexBuffer = new DynamicVertexBuffer(GPU, instanceTransformVertexDeclarations, instanceTransformations.Count, BufferUsage.WriteOnly);
            instanceVertexBuffer.SetData(worldSpaceTransforms, 0, instanceTransformations.Count, SetDataOptions.Discard);

            foreach (ModelMesh mesh in Model.Meshes)
            {
                // Proactively select the main shading technique instead of letting GameRenderer
                // choose it like for GameObjects, since we won't render shadows for particles
                // plus we want to use instancing
                DefaultShadingEffect.CurrentPass = AbstractForwardRenderingEffect.Pass.MainShadingInstanced;

                // Load in the shader and set its parameters
                DefaultShadingEffect.World = mesh.ParentBone.Transform;
                DefaultShadingEffect.View = view;
                DefaultShadingEffect.Projection = projection;
                DefaultShadingEffect.CameraPosition = cameraPosition;

                // Pre-compute the inverse transpose of the world matrix to use in shader
                Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform));

                DefaultShadingEffect.WorldInverseTranspose = worldInverseTranspose;

                DefaultShadingEffect.Lit = true;

                // Set light parameters
                DefaultShadingEffect.DirectionalLightColors = lights.Directionals.Select(l => l.LightColor.ToVector4()).Append(lights.Sun.LightColor.ToVector4()).ToArray();
                DefaultShadingEffect.DirectionalLightIntensities = lights.Directionals.Select(l => l.Intensity).Append(lights.Sun.Intensity).ToArray();
                DefaultShadingEffect.DirectionalLightDirections = lights.Directionals.Select(l => l.Direction).Append(lights.Sun.Direction).ToArray();
                DefaultShadingEffect.SunLightIndex = lights.Directionals.Count;

                DefaultShadingEffect.AmbientLightColor = lights.Ambient.LightColor.ToVector4();
                DefaultShadingEffect.AmbientLightIntensity = lights.Ambient.Intensity;

                // Set tints for the diffuse color, ambient color, and specular color. These are
                // multiplied in the shader by the light color and intensity, as well as each
                // component's weight.
                DefaultShadingEffect.MaterialDiffuseColor = Color.White.ToVector4();
                DefaultShadingEffect.MaterialAmbientColor = Color.White.ToVector4();
                DefaultShadingEffect.MaterialHasSpecular = false;
                // Uncomment if specular; will use Blinn-Phong.
                // effect.MaterialSpecularColor = Color.White.ToVector4();
                // effect.MaterialShininess = 20f;

                DefaultShadingEffect.ModelTexture = Texture;
                // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                DefaultShadingEffect.ModelTextureGammaCorrection = true;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // We usually don't worry about the vertex/index buffers when using
                    // ModelMesh.Draw(), but in this case we want to pass the instance transform
                    // vertex buffer as well, so we explicitly tell the GPU to read from both the
                    // model vertex buffer plus our instanceVertexBuffer.
                    GPU.SetVertexBuffers(
                        new VertexBufferBinding(part.VertexBuffer, part.VertexOffset, 0),
                        new VertexBufferBinding(instanceVertexBuffer, 0, 1));

                    // And also tell it to read from the model index buffer.
                    GPU.Indices = part.IndexBuffer;

                    DefaultShadingEffect.Apply();
                    GPU.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, part.StartIndex, part.PrimitiveCount, instanceTransformations.Count);
                }
            }
        }

        public new void UI()
        {
            Box box = this.Entity as Box;
            if (box == null) return;

            float oldScale = Scale;
            float oldAmount = box.Height;
            float oldSizeX = box.Width;
            float oldSizeZ = box.Length;

            base.UI();

            if (Scale != oldScale || box.Height != oldAmount || box.Width != oldSizeX || box.Length != oldSizeZ)
            {
                RegenerateVegetations(box.Height, box.Width, box.Length);
            }
        }
    }
}
