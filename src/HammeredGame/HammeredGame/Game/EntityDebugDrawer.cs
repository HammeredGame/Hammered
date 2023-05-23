using BEPUphysics.Entities;
using HammeredGame.Core;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using BEPUphysics.Entities.Prefabs;

namespace HammeredGame.Game
{
    /// <summary>
    /// Component that draws a model following the position and orientation of a BEPUphysics entity.
    /// </summary>
    public class EntityDebugDrawer
    {
        private readonly BasicEffect basicEffect;
        private readonly Model cubeModel;
        private readonly Model sphereModel;

        /// <summary>
        /// Creates a new EntityModel.
        /// </summary>
        /// <param name="entity">Entity to attach the graphical representation to.</param>
        /// <param name="model">Graphical representation to use for the entity.</param>
        /// <param name="transform">Base transformation to apply to the model before moving to the entity.</param>
        /// <param name="game">Game to which this component will belong.</param>
        public EntityDebugDrawer(GraphicsDevice gpu, ContentManager Content)
        {
            cubeModel = Content.Load<Model>("Meshes/Primitives/unit_cube");
            sphereModel = Content.Load<Model>("Meshes/Primitives/unit_sphere");

            basicEffect = new BasicEffect(gpu);
            basicEffect.EnableDefaultLighting();

            basicEffect.DirectionalLight0.Enabled = true;
            basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
            basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1, 0));
            basicEffect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

            basicEffect.DirectionalLight1.Enabled = true;
            basicEffect.DirectionalLight1.DiffuseColor = Vector3.One * 0.2f;
            basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1, -1, 0));
            basicEffect.DirectionalLight1.SpecularColor = Vector3.One * 0.1f;

            basicEffect.DirectionalLight2.Enabled = true;
            basicEffect.DirectionalLight2.DiffuseColor = Vector3.One * 0.15f;
            basicEffect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
            basicEffect.DirectionalLight2.SpecularColor = Vector3.One * 0.1f;
        }

        public void Draw(Matrix view, Matrix proj, Entity entity)
        {
            BEPUutilities.Matrix scaling;
            Model model;

            // This won't create any graphics for an entity that isn't a box since the model
            // being used is a box. When any of width/height/length are zero, it causes a visual
            // glitch so we don't draw in that case.
            if (entity is Box box && (box.Width > 0 && box.Height > 0 && box.Length > 0))
            {
                scaling = BEPUutilities.Matrix.CreateScale(box.Width, box.Height, box.Length);
                model = cubeModel;
            }
            else if (entity is Sphere sphere && sphere.Radius > 0)
            {
                scaling = BEPUutilities.Matrix.CreateScale(sphere.Radius, sphere.Radius, sphere.Radius);
                model = sphereModel;
            }
            else
            {
                return;
            }

            //Notice that the entity's worldTransform property is being accessed here.
            //This property is returns a rigid transformation representing the orientation
            //and translation of the entity combined.
            //There are a variety of properties available in the entity, try looking around
            //in the list to familiarize yourself with it.
            Matrix worldMatrix = (scaling * entity.CollisionInformation.WorldTransform.Matrix).ToXNA();

            //Collect any bone transformations in the model itself.
            //The default cube model doesn't have any, but this allows the EntityModel to work with more complicated shapes.
            Matrix[] boneTransforms = new Matrix[model.Bones.Count];

            model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    basicEffect.World = boneTransforms[mesh.ParentBone.Index] * worldMatrix;
                    //effect.View = MathConverter.Convert((Game as Hammered_Physics).Camera.ViewMatrix);
                    //effect.Projection = MathConverter.Convert((Game as Hammered_Physics).Camera.ProjectionMatrix);

                    basicEffect.View = view;
                    basicEffect.Projection = proj;
                    part.Effect = basicEffect;
                }
                mesh.Draw();
            }
        }
    }
}
