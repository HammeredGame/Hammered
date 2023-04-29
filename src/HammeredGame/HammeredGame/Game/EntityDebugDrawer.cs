using BEPUphysics.Entities;
using HammeredGame.Core;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game
{
    /// <summary>
    /// Component that draws a model following the position and orientation of a BEPUphysics entity.
    /// </summary>
    public class EntityDebugDrawer
    {
        /// <summary>
        /// Entity that this model follows.
        /// </summary>
        readonly Entity entity;
        readonly Model model;
        /// <summary>
        /// Base transformation to apply to the model.
        /// </summary>
        readonly BEPUutilities.Matrix transform;
        readonly Matrix[] boneTransforms;


        /// <summary>
        /// Creates a new EntityModel.
        /// </summary>
        /// <param name="entity">Entity to attach the graphical representation to.</param>
        /// <param name="model">Graphical representation to use for the entity.</param>
        /// <param name="transform">Base transformation to apply to the model before moving to the entity.</param>
        /// <param name="game">Game to which this component will belong.</param>
        public EntityDebugDrawer(Entity entity, Model model, BEPUutilities.Matrix transform)
        {
            this.entity = entity;
            this.model = model;
            this.transform = transform;

            //Collect any bone transformations in the model itself.
            //The default cube model doesn't have any, but this allows the EntityModel to work with more complicated shapes.
            boneTransforms = new Matrix[model.Bones.Count];
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    if (effect is BasicEffect basic)
                    {
                        basic.EnableDefaultLighting();
                    }
                }
            }
        }

        public void Draw(GameTime gameTime, Matrix view, Matrix proj)
        {
            //Notice that the entity's worldTransform property is being accessed here.
            //This property is returns a rigid transformation representing the orientation
            //and translation of the entity combined.
            //There are a variety of properties available in the entity, try looking around
            //in the list to familiarize yourself with it.
            Matrix worldMatrix = MathConverter.Convert(transform * entity.CollisionInformation.WorldTransform.Matrix);


            model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = boneTransforms[mesh.ParentBone.Index] * worldMatrix;
                    //effect.View = MathConverter.Convert((Game as Hammered_Physics).Camera.ViewMatrix);
                    //effect.Projection = MathConverter.Convert((Game as Hammered_Physics).Camera.ProjectionMatrix);

                    effect.View = view;
                    effect.Projection = proj;

                    effect.DirectionalLight0.Enabled = true;
                    effect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
                    effect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1, 0));
                    effect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

                    effect.DirectionalLight1.Enabled = true;
                    effect.DirectionalLight1.DiffuseColor = Vector3.One * 0.2f;
                    effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1, -1, 0));
                    effect.DirectionalLight1.SpecularColor = Vector3.One * 0.1f;

                    effect.DirectionalLight2.Enabled = true;
                    effect.DirectionalLight2.DiffuseColor = Vector3.One * 0.15f;
                    effect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                    effect.DirectionalLight2.SpecularColor = Vector3.One * 0.1f;
                }
                mesh.Draw();
            }
        }
    }
}
