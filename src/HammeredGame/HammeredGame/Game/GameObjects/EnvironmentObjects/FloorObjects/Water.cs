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
using System.Threading.Tasks;
using HammeredGame.Graphics.ForwardRendering;

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
        private float opacity = 0.6f;
        private bool useBumpMaps = false;

        private readonly WaterEffect waterEffect;
        public override AbstractForwardRenderingEffect Effect => waterEffect;

        public Water(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            BEPUutilities.Vector3[] vertices;
            int[] indices;
            ModelDataExtractor.GetVerticesAndIndicesFromModel(this.Model, out vertices, out indices);
            //Give the mesh information to a new StaticMesh.
            var mesh = new StaticMesh(vertices, indices, new BEPUutilities.AffineTransform(new BEPUutilities.Vector3(scale, scale, scale), this.Rotation.ToBepu(), this.Position.ToBepu()));
            mesh.Tag = this;
            this.ActiveSpace.Add(mesh);

            this.waterEffect = new WaterEffect(services.GetService<ContentManager>());
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
                // Load in the shader and set its parameters
                waterEffect.World = mesh.ParentBone.Transform * world;
                waterEffect.View = view;
                waterEffect.Projection = projection;
                waterEffect.CameraPosition = cameraPosition;

                // Pre-compute the inverse transpose of the world matrix to use in shader
                Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform * world));

                waterEffect.WorldInverseTranspose = worldInverseTranspose;

                waterEffect.Lit = true;

                // Set light parameters
                waterEffect.DirectionalLightColors = lights.Directionals.Select(l => l.LightColor.ToVector4()).Append(lights.Sun.LightColor.ToVector4()).ToArray();
                waterEffect.DirectionalLightIntensities = lights.Directionals.Select(l => l.Intensity).Append(lights.Sun.Intensity).ToArray();
                waterEffect.DirectionalLightDirections = lights.Directionals.Select(l => l.Direction).Append(lights.Sun.Direction).ToArray();
                waterEffect.SunLightIndex = lights.Directionals.Count;

                waterEffect.AmbientLightColor = lights.Ambient.LightColor.ToVector4();
                waterEffect.AmbientLightIntensity = lights.Ambient.Intensity;

                // Set tints for the diffuse color, ambient color, and specular color. These are
                // multiplied in the shader by the light color and intensity, as well as each
                // component's weight.
                waterEffect.MaterialDiffuseColor = Color.White.ToVector4();
                waterEffect.MaterialAmbientColor = Color.White.ToVector4();
                waterEffect.MaterialHasSpecular = true;
                waterEffect.MaterialSpecularColor = Color.White.ToVector4();
                waterEffect.MaterialShininess = 10f;

                waterEffect.ModelTexture = tex;
                // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                waterEffect.ModelTextureGammaCorrection = true;

                waterEffect.GameTimeSeconds = (float)gameTime.TotalGameTime.TotalSeconds;
                waterEffect.UseBumpMap = useBumpMaps;
                waterEffect.WaterOpacity = opacity;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = waterEffect.GetEffect();
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
