﻿using BEPUphysics.Entities;
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
    public class GridDebugDrawer
    {
        Model model;
        public Vector3 Position;
        /// <summary>
        /// Base transformation to apply to the model.
        /// </summary>
        public Matrix Transform;
        Matrix[] boneTransforms;


        /// <summary>
        /// Creates a new EntityModel.
        /// </summary>
        /// <param name="entity">Entity to attach the graphical representation to.</param>
        /// <param name="model">Graphical representation to use for the entity.</param>
        /// <param name="transform">Base transformation to apply to the model before moving to the entity.</param>
        /// <param name="game">Game to which this component will belong.</param>
        public GridDebugDrawer(Model model, Vector3 pos, Matrix transform)
        {
            this.model = model;
            this.Position = pos;
            this.Transform = transform;

            //Collect any bone transformations in the model itself.
            //The default cube model doesn't have any, but this allows the EntityModel to work with more complicated shapes.
            boneTransforms = new Matrix[model.Bones.Count];
        }

        public void Draw(GameTime gameTime, GraphicsDevice gpu, Matrix view, Matrix proj)
        {
            //Notice that the entity's worldTransform property is being accessed here.
            //This property is returns a rigid transformation representing the orientation
            //and translation of the entity combined.
            //There are a variety of properties available in the entity, try looking around
            //in the list to familiarize yourself with it.
            Matrix translationMatrix = Matrix.CreateTranslation(Position);
            Matrix worldMatrix = Transform * translationMatrix;


            model.CopyAbsoluteBoneTransformsTo(boneTransforms);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    BasicEffect effect = new BasicEffect(gpu);
                    effect.EnableDefaultLighting();

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

                    part.Effect = effect;
                }
                mesh.Draw();
            }
        }
    }
}
