﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics.Entities;
using BEPUphysics;
using Hammered_Physics.Core;

namespace HammeredGame.Game
{
    /// <summary>
    /// The <c>GameObject</c> class encompasses all the visual components the player is going to encounter during their playthrough.
    /// The specific behaviours (either individual or inter-object) are to be defined in the respective subclasses
    /// (see the "class_skeleton_class_diagram.jpg" for a thorough class tree on the subclasses)
    /// <para />
    /// Any 3D object/shape in a geometric space can be uniquely defined by its:
    /// - (base) geometry -> <code>Model model</code> variable
    /// - the affine transformation applied to it:
    ///     -- translation -> <code>Vector3 position</code> variable
    ///     -- rotation -> <code>Quaternion rotation</code> variable
    ///     -- scaling -> <code>float scale</code>
    /// <para />
    /// In addition, a shape (as defined above) may be cosmetically enhanced with a (.png) texture -> <code>Texture2D tex</code> variable.
    /// <para />
    /// For collision detection, each <c>GameObject</c> has bounding box attached
    /// -> <code>BoundingBox boundingBox</code> variable.
    /// <remark>To be exact the current implementation supports an "Axis-Aligned Bounding Box" or "AABB" for short)</remark>
    /// </summary>
    ///
    /// <remarks>
    /// TODO: Add "class_skeleton_class_diagram.jpg" to game files
    /// </remarks>
    public abstract class GameObject
    {
        // Common variables for any object in the game (will be modified as we develop further)
        public Model Model;

        /// <summary>
        /// Physics Entity that this model follows.
        /// </summary>
        public Entity Entity;

        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;

        public Texture2D Texture;

        // Probably unnecessary now, since we're shifting to using a physics engine
        public BoundingBox BoundingBox { get; private set; }

        // The active level physics space to add and remove entities for physics constraint solving
        public Space ActiveSpace;

        /// <summary>
        ///  The "flag" variable <code>bool visible</code> is used to indicate the state in which the <c>GameObject</c>
        ///  instance is in.
        ///  If its value is true, then the instance will be drawn on the screen (utilizing the <code>DrawModel()</code> function)
        /// </summary>
        protected bool Visible = true;

        private List<(int, float[])> allVertexData;

        protected GameObject(Model model, Vector3 pos, float scale, Texture2D t, Space space)
        {
            this.Model = model;
            this.Position = pos;
            this.Rotation = Quaternion.Identity;
            this.Scale = scale;
            this.Texture = t;

            this.ActiveSpace = space;

            if (this.Model != null)
            {
                // Precalculate the vertex buffer data, since VertextBuffer.GetData is very
                // expensive to perform on every Update. We can find the bounding box of the
                // mesh by applying the transformations to this precalculated vertex data.
                allVertexData = new();
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (ModelMeshPart meshPart in mesh.MeshParts)
                    {
                        int vertexStride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                        int vertexBufferSize = meshPart.NumVertices * vertexStride;

                        int vertexDataSize = vertexBufferSize / sizeof(float);
                        float[] vertexData = new float[vertexDataSize];
                        meshPart.VertexBuffer.GetData(vertexData);
                        allVertexData.Add((vertexStride, vertexData));
                    }
                }
            }

            //this.ComputeBounds();
        }

        public bool IsVisible()
        {
            return this.Visible;
        }

        public void SetVisible(bool vis)
        {
            this.Visible = vis;
        }

        public abstract void Update(GameTime gameTime);

        // get position and rotation of the object - extract the scale, rotation, and translation matrices
        // get world matrix and then call draw model to draw the mesh on screen
        public virtual void Draw(Matrix view, Matrix projection)
        {
            if (this.IsVisible())
                DrawModel(Model, view, projection, Texture);
        }

        /// <summary>
        /// Common method to draw 3D models
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="tex"></param>
        public void DrawModel(Model model, Matrix view, Matrix projection, Texture2D tex)
        {
            Matrix world = GetWorldMatrix();

            Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                // Set the effect class for each mesh part in the model
                // This is most likely where we attach shaders to the model/mesh
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = meshTransforms[mesh.ParentBone.Index] * world;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    //effect.LightingEnabled = Keyboard.GetState().IsKeyUp(Keys.L);
                    effect.LightingEnabled = true;

                    //effect.DiffuseColor = new Vector3(0.25f, 0.25f, 0.25f);
                    //effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                    //effect.SpecularPower = 0.1f;
                    effect.AmbientLightColor = new Vector3(0.5f, 0.5f, 0.5f);

                    //effect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
                    //effect.DirectionalLight0.Direction = new Vector3(-1, -1, 0);
                    //effect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

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

                    effect.TextureEnabled = tex != null;
                    effect.Texture = tex;

                    effect.FogEnabled = true;
                    effect.FogStart = 400.0f;
                    effect.FogEnd = 450.0f;
                    effect.FogColor = Color.AliceBlue.ToVector3();
                }

                mesh.Draw();
            }
        }

        /// <summary>
        /// Method to get bounding box for the mesh - for basic collision detection
        /// Probably not going to be necessary for later iterations
        /// (once we bring in an external library to handle collisions)
        /// </summary>
        public void ComputeBounds()
        {
            Matrix world = GetWorldMatrix();
            Matrix[] boneTransforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            int partCount = 0;
            // Get bounding box min/max from each mesh part's vertices
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    // Get mesh transform with respect to world
                    Matrix meshTransform = boneTransforms[mesh.ParentBone.Index] * world;

                    (int vertexStride, float[] vertexData) = allVertexData[partCount];
                    for (int i = 0; i < vertexData.Length; i += vertexStride/ sizeof(float))
                    {
                        //Vector3 vertex = new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]);
                        Vector3 vertex = Vector3.Transform(new Vector3(vertexData[i], vertexData[i + 1], vertexData[i + 2]), meshTransform);
                        min = Vector3.Min(min, vertex);
                        max = Vector3.Max(max, vertex);
                    }
                    partCount++;
                }
            }

            this.BoundingBox = new BoundingBox(min, max);
        }


        /// <summary>
        /// Get the world matrix for the object's current position in the world. Mainly used for drawing.
        /// </summary>
        /// <returns></returns>
        public Matrix GetWorldMatrix()
        {
            Vector3 pos = GetPosition();
            Quaternion rot = GetRotation();

            Matrix rotationMatrix = Matrix.CreateFromQuaternion(rot);
            Matrix translationMatrix = Matrix.CreateTranslation(pos);
            Matrix scaleMatrix = Matrix.CreateScale(Scale);

            // Construct world matrix
            // Be careful! Order matters!
            // The transformations in this framework are applied FROM LEFT TO RIGHT
            // (in contrast with how it is done in mathematical notation).
            ///<example>
            /// Provided the transformation standard affine transformation matrices: Translation (T), Rotation (R) and Scaling (S)
            /// if we wish to apply the transformation: R -> T-> S on a vector
            /// we would express it in mathematical notation as STR,
            /// but as R * T * S in MonoGame (and OpenGL).
            ///</example>

            // World matrix = S -> R -> T
            Matrix world = scaleMatrix * rotationMatrix * translationMatrix;
            return world;
        }

        /// <summary>
        /// Getter function for game object position
        /// </summary>
        /// <returns></returns>
        public virtual Vector3 GetPosition()
        {
            // If an 'Entity' is attached to the game object, return its position.
            // Otherwise, return the mesh/model's position (this is usually only the
            // case for Static Meshes - typically used for static ground models)
            if (this.Entity != null) return MathConverter.Convert(this.Entity.Position);
            else return Position;
        }

        /// <summary>
        /// Getter function for game object rotation
        /// </summary>
        /// <returns></returns>
        public virtual Quaternion GetRotation()
        {
            if (this.Entity != null) return MathConverter.Convert(this.Entity.Orientation);
            else return Rotation;
        }
    }
}
