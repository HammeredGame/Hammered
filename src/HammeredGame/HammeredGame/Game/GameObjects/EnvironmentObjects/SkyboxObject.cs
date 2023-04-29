using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using HammeredGame.Core;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework.Content;
using HammeredGame.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    /// <summary>
    /// The <c>Skybox</c> class refers to a unit box rendered using a dedicated shader to make it
    /// appear as a skybox.
    /// </summary>
    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---&gt; <see cref="EnvironmentObject "/>
    /// ---&gt; <see cref="SkyboxObject"/>
    /// </para>
    /// </remarks>
    class SkyboxObject : EnvironmentObject
    {
        private TextureCube cubemap;
        public SkyboxObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            // Although we're accepting any model and texture here, technically only a unit cube and
            // a DDS cube-map texture is supported. Behaviour with other models and textures are undefined.
            // We need a TextureCube and not a Texture2D, so we can't use the XML :(
            Effect = services.GetService<ContentManager>().Load<Effect>("Effects/ForwardRendering/Skybox");
            cubemap = services.GetService<ContentManager>().Load<TextureCube>("Skybox/kloofendal_48d_partly_cloudy_puresky_4k");
        }

        public override void Update(GameTime gameTime, bool screenHasFocus) {}

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

                    part.Effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * world);
                    // Remove the translation part of the view matrix (so that it appears infinitely
                    // far away) by setting the last row and column to the identities.
                    Matrix viewNoTranslation = view;
                    viewNoTranslation.M41 = viewNoTranslation.M42 = viewNoTranslation.M43 = 0f;
                    viewNoTranslation.M14 = viewNoTranslation.M24 = viewNoTranslation.M34 = 0f;
                    viewNoTranslation.M44 = 1f;
                    part.Effect.Parameters["View"].SetValue(viewNoTranslation);
                    part.Effect.Parameters["Projection"].SetValue(projection);

                    // Add sunlight color and direction
                    part.Effect.Parameters["SunLightColor"]?.SetValue(lights.Sun.LightColor.ToVector4());
                    part.Effect.Parameters["SunLightIntensity"]?.SetValue(lights.Sun.Intensity);
                    part.Effect.Parameters["SunLightDirection"]?.SetValue(lights.Sun.Direction);

                    // The skybox texture is an sRGB cube map
                    part.Effect.Parameters["SkyboxTexture"].SetValue(cubemap);
                    part.Effect.Parameters["SkyboxTextureGammaCorrection"].SetValue(true);
                }
                mesh.Draw();
            }
        }
    }
}
