﻿using Aether.Animation;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Core;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Net.Mime;
using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using HammeredGame.Game.Screens;

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
    /// For collision detection, each <c>GameObject</c> is attached to a physics Space. All
    /// non-terrain objects also have an Entity representation (a physics bounding shape) that
    /// handles collisions and physics-based behaviour.
    /// </summary>
    ///
    /// <remarks>
    /// TODO: Add "class_skeleton_class_diagram.jpg" to game files
    /// </remarks>
    public abstract class GameObject : IImGui
    {
        // Common variables for any object in the game
        public Model Model;

        // Load in shader
        public Effect Effect;
        protected GraphicsDevice GPU;

        public Texture2D Texture;
        private Vector3 position;

        protected Scene CurrentScene { get; private set; }

        // Use the private position vector only if we don't have a physics entity attached.
        // Otherwise, we delegate the position property entirely to the physics body position and
        // never use our own private value.
        public Vector3 Position
        {
            get { if (Entity != null) { return MathConverter.Convert(Entity.Position); } else { return position; } }
            set { if (Entity != null) { Entity.Position = MathConverter.Convert(value); } position = value; }
        }

        private Quaternion rotation;

        // Use the private rotation quaternion only if we don't have a physics entity attached.
        // Otherwise, we delegate the rotation property entirely to the physics body orientation and
        // never use our own private value.
        public Quaternion Rotation
        {
            get { if (Entity != null) { return MathConverter.Convert(Entity.Orientation); } else { return rotation; } }
            set { if (Entity != null) { Entity.Orientation = MathConverter.Convert(value); } rotation = value; }
        }

        public float Scale;

        /// <summary>
        /// Physics Entity that this model follows. Could be null for e.g. terrain.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// A model may have its origin somewhere other than its center of mass. In this case, the
        /// graphic display and the physics body calculations will not match. This vector is used to
        /// shift the model drawing so it matches the physics body.
        /// </summary>
        public Vector3 EntityModelOffset = Vector3.Zero;

        protected GameServices Services;

        // The active level physics space to add and remove entities for physics constraint solving
        protected Space ActiveSpace;

        /// <summary>
        ///  The "flag" variable <code>bool visible</code> is used to indicate the state in which the <c>GameObject</c>
        ///  instance is in.
        ///  If its value is true, then the instance will be drawn on the screen (utilizing the <code>DrawModel()</code> function)
        /// </summary>
        public bool Visible = true;

        private List<(int, float[])> allVertexData;

        protected GameObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity)
        {
            this.Entity = entity;
            this.Services = services;
            this.Model = model;
            this.Position = pos;
            this.Rotation = rotation;
            this.Scale = scale;
            this.Texture = t;

            this.ActiveSpace = services.GetService<Space>();
            this.GPU = services.GetService<GraphicsDevice>();

            // Load in Shader
            this.Effect = services.GetService<ContentManager>().Load<Effect>("Effects/basic");
            //var shaderPath = Path.Combine(Directory.GetCurrentDirectory(), "..\\..\\..\\Content\\Effects\\basic.mgfx");
            //this.Effect = new Effect(GPU, File.ReadAllBytes(shaderPath));

            if (this.Model != null && model.GetAnimations() == null)
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
        }

        public void SetCurrentScene(Scene currentScene)
        {
            this.CurrentScene = currentScene;
        }

        public abstract void Update(GameTime gameTime, bool screenHasFocus);

        // get position and rotation of the object - extract the scale, rotation, and translation matrices
        // get world matrix and then call draw model to draw the mesh on screen
        public virtual void Draw(Matrix view, Matrix projection, Light l)
        {
            if (Visible)
                DrawModel(Model, view, projection, Texture, l);
        }

        /// <summary>
        /// Common method to draw 3D models
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="tex"></param>
        public void DrawModel(Model model, Matrix view, Matrix projection, Texture2D tex, Light l)
        {
            DrawModeShader(model, view, projection, tex);
            //DrawModeBasic(model, view, projection, tex, l);
        }

        /// <summary>
        /// Common method to draw 3D models
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="tex"></param>
        public void DrawModeShader(Model model, Matrix view, Matrix projection, Texture2D tex)
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
                    part.Effect.Parameters["ViewVector"]?.SetValue(new Vector3(1.0f, -1.0f, 0.0f));

                    // Pre-compute the inverse transpose of the world matrix to use in shader
                    Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform * world));

                    part.Effect.Parameters["WorldInverseTranspose"]?.SetValue(worldInverseTranspose);

                    // Set light parameters
                    part.Effect.Parameters["AmbientColor"]?.SetValue(Color.White.ToVector4());
                    part.Effect.Parameters["AmbientIntensity"]?.SetValue(0.01f);
                    part.Effect.Parameters["DiffuseIntensity"]?.SetValue(1f);
                    part.Effect.Parameters["DiffuseLightDirection"]?.SetValue(Vector3.Normalize(new Vector3(1, 1, 1)));

                    // This sets all models to have a 1.0 diffuse, which means essentially it'll
                    // behave as the identity when multiplied with the texture color within the
                    // shader. Models with no texture will have 0 as the texture color, so when
                    // multiplied, the final color will be black.
                    part.Effect.Parameters["DiffuseColor"]?.SetValue(Color.White.ToVector4());

                    // Set material properties
                    part.Effect.Parameters["Shininess"]?.SetValue(0.01f);
                    part.Effect.Parameters["SpecularColor"]?.SetValue(Color.White.ToVector4());
                    part.Effect.Parameters["SpecularIntensity"]?.SetValue(0.1f);

                    part.Effect.Parameters["ModelTexture"]?.SetValue(tex);
                }
                mesh.Draw();
            }
        }

        /// <summary>
        /// Common method to draw 3D models
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="tex"></param>
        public void DrawModeBasic(Model model, Matrix view, Matrix projection, Texture2D tex, Light l)
        {
            Matrix world = GetWorldMatrix();

            Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                // Set the effect class for each mesh part in the model
                // This is most likely where we attach shaders to the model/mesh
                foreach (Effect effect in mesh.Effects)
                {
                    if(effect.GetType() == typeof(BasicEffect))
                    {
                        BasicEffect _effect = (BasicEffect) effect;
                        _effect.World = meshTransforms[mesh.ParentBone.Index] * world;
                        _effect.View = view;
                        _effect.Projection = projection;

                        _effect.EnableDefaultLighting();
                        //effect.LightingEnabled = Keyboard.GetState().IsKeyUp(Keys.L);
                        _effect.LightingEnabled = true;

                        //effect.DiffuseColor = new Vector3(0.25f, 0.25f, 0.25f);
                        //effect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                        //effect.SpecularPower = 0.1f;
                        _effect.AmbientLightColor = new Vector3(0.5f, 0.5f, 0.5f);

                        //effect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
                        //effect.DirectionalLight0.Direction = new Vector3(-1, -1, 0);
                        //effect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

                        _effect.DirectionalLight0.Enabled = true;
                        _effect.DirectionalLight0.DiffuseColor = Vector3.One * 0.7f;
                        _effect.DirectionalLight0.Direction = Vector3.Normalize(-l.Direction);
                        _effect.DirectionalLight0.SpecularColor = Vector3.One * 0.2f;

                        _effect.DirectionalLight1.Enabled = true;
                        _effect.DirectionalLight1.DiffuseColor = Vector3.One * 0.2f;
                        _effect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1, -1, 0));
                        _effect.DirectionalLight1.SpecularColor = Vector3.One * 0.1f;

                        _effect.DirectionalLight2.Enabled = true;
                        _effect.DirectionalLight2.DiffuseColor = Vector3.One * 0.15f;
                        _effect.DirectionalLight2.Direction = Vector3.Normalize(new Vector3(-1, -1, -1));
                        _effect.DirectionalLight2.SpecularColor = Vector3.One * 0.1f;

                        _effect.TextureEnabled = tex != null;
                        _effect.Texture = tex;

                        _effect.FogEnabled = true;
                        _effect.FogStart = 400.0f;
                        _effect.FogEnd = 450.0f;
                        _effect.FogColor = Color.AliceBlue.ToVector3();
                    }
                }

                mesh.Draw();
            }
        }

        /// <summary>
        /// Get the world matrix for the object's current position in the world. Mainly used for drawing.
        /// </summary>
        /// <returns></returns>
        public virtual Matrix GetWorldMatrix()
        {
            Matrix rotationMatrix = Matrix.CreateFromQuaternion(Rotation);
            // For translation, include the model's origin offset so that the collision body
            // position matches with the rendered model
            Matrix translationMatrix = Matrix.CreateTranslation(Position + EntityModelOffset);
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
            return scaleMatrix * rotationMatrix * translationMatrix;
        }

        /// <summary>
        /// A default game object property UI, shown in the debug UI for editing the basic position,
        /// rotation, scale, and any physics entity properties at runtime.
        /// </summary>
        public void UI()
        {
            ImGui.Text($"Texture: {Texture?.ToString() ?? "None"}");

            // ImGui accepts only system.numerics.vectorX and not MonoGame VectorX, so
            // we need to temporarily convert.
            System.Numerics.Vector3 pos = Position.ToNumerics();
            ImGui.DragFloat3("Position", ref pos, 10f);
            Position = pos;

            System.Numerics.Vector4 rot = Rotation.ToVector4().ToNumerics();
            ImGui.DragFloat4("Rotation", ref rot, 0.01f, -1.0f, 1.0f);
            Rotation = Quaternion.Normalize(new Quaternion(rot));

            ImGui.DragFloat("Scale", ref Scale, 0.01f);
            ImGui.TextWrapped("* Changing scale only changes the rendering scale and not the collision entity scale.");

            ImGui.NewLine();

            ImGui.Text($"Collision body: {Entity?.GetType()?.Name ?? "None"}");
            if (Entity != null)
            {
                System.Numerics.Vector3 modelOffset = EntityModelOffset.ToNumerics();
                ImGui.DragFloat3("Origin offset (between Graphic & Physics)", ref modelOffset, 0.01f);
                EntityModelOffset = modelOffset;

                // Setting IgnoreShapeChanges makes sure that editing body properties don't randomly
                // start causing the body to tip over or lose its center of mass. We're disobeying
                // the laws of physics by editing these, so it makes sense to disable the change triggers.
                Entity.IgnoreShapeChanges = true;
                // Display some entity-specific parameters
                if (Entity is Box box)
                {
                    System.Numerics.Vector3 whl = new(box.Width, box.Height, box.Length);
                    ImGui.DragFloat3("Box W,H,L", ref whl, 0.1f, 0.1f, float.MaxValue);
                    box.Width = whl.X;
                    box.Height = whl.Y;
                    box.Length = whl.Z;
                }
                else if (Entity is Sphere sph)
                {
                    float radius = sph.Radius;
                    ImGui.DragFloat("Sphere R", ref radius, 0.1f, 0.1f, float.MaxValue);
                    sph.Radius = radius;
                }
                Entity.IgnoreShapeChanges = false;
            }
        }
    }
}
