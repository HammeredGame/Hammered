using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Linq;
using BEPUphysics.BroadPhaseEntries;
using HammeredGame.Core;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework.Content;
using HammeredGame.Graphics;
using ImMonoGame.Thing;
using ImGuiNET;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects
{
    /// <summary>
    /// The <c>Ground</c> class refers to a water surface the character (<see cref="Player"/> may encounter.
    /// Movement towards or unto it is strictly prohibited.
    /// </summary>
    ///
    ///
    /// <remarks>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="FloorObject "/> ---> see<see cref="Water"/>
    /// </remarks>
    class Water : FloorObject, IImGui
    {
        // Water options
        private Texture2D normalMap0;
        private Texture2D normalMap1;
        private float opacity = 0.6f;
        private bool useBumpMaps = false;

        public Water(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            BEPUutilities.Vector3[] vertices;
            int[] indices;
            ModelDataExtractor.GetVerticesAndIndicesFromModel(this.Model, out vertices, out indices);
            //Give the mesh information to a new StaticMesh.
            var mesh = new StaticMesh(vertices, indices, new BEPUutilities.AffineTransform(new BEPUutilities.Vector3(scale, scale, scale), MathConverter.Convert(this.Rotation), MathConverter.Convert(this.Position)));
            mesh.Tag = this;
            this.ActiveSpace.Add(mesh);

            this.Effect = services.GetService<ContentManager>().Load<Effect>("Effects/ForwardRendering/Water");

            // Normal maps courtesy of royalty free https://www.cadhatch.com/seamless-water-textures
            this.normalMap0 = services.GetService<ContentManager>().Load<Texture2D>("WaterNormalMap0");
            this.normalMap1 = services.GetService<ContentManager>().Load<Texture2D>("WaterNormalMap1");
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }

        /// <summary>
        /// Draw with a water-specific shader
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="tex"></param>
        /// <param name="lights"></param>
        public override void DrawModel(GameTime gameTime, Model model, Matrix view, Matrix projection, Vector3 cameraPosition, Texture2D tex, SceneLightSetup lights)
        {
            Matrix world = GetWorldMatrix();

            Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // Load in the shader and set its parameters
                    part.Effect = this.Effect;

                    part.Effect.Parameters["World"]?.SetValue(mesh.ParentBone.Transform * world);
                    part.Effect.Parameters["View"]?.SetValue(view);
                    part.Effect.Parameters["Projection"]?.SetValue(projection);
                    part.Effect.Parameters["CameraPosition"]?.SetValue(cameraPosition);

                    // Pre-compute the inverse transpose of the world matrix to use in shader
                    Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform * world));

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
                    part.Effect.Parameters["MaterialHasSpecular"]?.SetValue(true);
                    part.Effect.Parameters["MaterialSpecularColor"]?.SetValue(Color.White.ToVector4());
                    part.Effect.Parameters["MaterialShininess"]?.SetValue(10f);

                    part.Effect.Parameters["ModelTexture"]?.SetValue(tex);
                    // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                    part.Effect.Parameters["ModelTextureGammaCorrection"]?.SetValue(true);

                    part.Effect.Parameters["GameTimeSeconds"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
                    part.Effect.Parameters["WaterNormal0"].SetValue(normalMap0);
                    part.Effect.Parameters["WaterNormal1"].SetValue(normalMap1);
                    part.Effect.Parameters["UseBumpMap"].SetValue(useBumpMaps);
                    part.Effect.Parameters["WaterOpacity"].SetValue(opacity);
                }
                mesh.Draw();
            }
        }

        new public void UI()
        {
            base.UI();
            ImGui.DragFloat("Water Opacity", ref opacity, 0.1f, 0f, 1f);
            ImGui.Checkbox("BumpMap", ref useBumpMaps);
        }
    }
}
