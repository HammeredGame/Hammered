using Aether.Animation;
using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Core;
using HammeredGame.Graphics;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Audio;
using HammeredGame.Graphics.ForwardRendering;

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
        private readonly DefaultShadingEffect defaultShadingEffect;

        public virtual AbstractForwardRenderingEffect Effect => defaultShadingEffect;

        protected GraphicsDevice GPU;

        public Texture2D Texture;
        private Vector3 position;

        protected Scene CurrentScene { get; private set; }

        public AudioEmitter AudioEmitter;

        // Use the private position vector only if we don't have a physics entity attached.
        // Otherwise, we delegate the position property entirely to the physics body position and
        // never use our own private value.
        public Vector3 Position
        {
            get { if (Entity != null) { return Entity.Position.ToXNA(); } else { return position; } }
            set { if (Entity != null) { Entity.Position = value.ToBepu(); } position = value; }
        }

        private Quaternion rotation;

        // Use the private rotation quaternion only if we don't have a physics entity attached.
        // Otherwise, we delegate the rotation property entirely to the physics body orientation and
        // never use our own private value.
        public Quaternion Rotation
        {
            get { if (Entity != null) { return Entity.Orientation.ToXNA(); } else { return rotation; } }
            set { if (Entity != null) { Entity.Orientation = value.ToBepu(); } rotation = value; }
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
            this.defaultShadingEffect = new DefaultShadingEffect(services.GetService<ContentManager>());
        }

        public void SetCurrentScene(Scene currentScene)
        {
            this.CurrentScene = currentScene;
        }

        public abstract void Update(GameTime gameTime, bool screenHasFocus);

        public virtual void Draw(GameTime gameTime, Matrix view, Matrix projection, Vector3 cameraPosition, SceneLightSetup lights)
        {
            if (Visible)
                DrawModel(gameTime, Model, view, projection, cameraPosition, Texture, lights);
        }

        /// <summary>
        /// Common method to draw 3D models
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="tex"></param>
        /// <param name="lights"></param>
        public virtual void DrawModel(GameTime gameTime, Model model, Matrix view, Matrix projection, Vector3 cameraPosition, Texture2D tex, SceneLightSetup lights)
        {
            Matrix world = GetWorldMatrix();

            Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                defaultShadingEffect.World = meshTransforms[mesh.ParentBone.Index] * world;
                defaultShadingEffect.View = view;
                defaultShadingEffect.Projection = projection;
                defaultShadingEffect.CameraPosition = cameraPosition;

                // Pre-compute the inverse transpose of the world matrix to use in shader
                Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform * world));

                defaultShadingEffect.WorldInverseTranspose = worldInverseTranspose;
                defaultShadingEffect.Lit = true;

                // Set light parameters
                defaultShadingEffect.DirectionalLightColors = lights.Directionals.Select(l => l.LightColor.ToVector4()).Append(lights.Sun.LightColor.ToVector4()).ToArray();
                defaultShadingEffect.DirectionalLightIntensities = lights.Directionals.Select(l => l.Intensity).Append(lights.Sun.Intensity).ToArray();
                defaultShadingEffect.DirectionalLightDirections = lights.Directionals.Select(l => l.Direction).Append(lights.Sun.Direction).ToArray();
                defaultShadingEffect.SunLightIndex = lights.Directionals.Count;

                defaultShadingEffect.AmbientLightColor = lights.Ambient.LightColor.ToVector4();
                defaultShadingEffect.AmbientLightIntensity = lights.Ambient.Intensity;

                // Set tints for the diffuse color, ambient color, and specular color. These are
                // multiplied in the shader by the light color and intensity, as well as each
                // component's weight.
                defaultShadingEffect.MaterialDiffuseColor = Color.White.ToVector4();
                defaultShadingEffect.MaterialAmbientColor = Color.White.ToVector4();
                defaultShadingEffect.MaterialHasSpecular = false;
                // Uncomment if specular; will use Blinn-Phong.
                // Effect.MaterialSpecularColor = Color.White.ToVector4();
                // Effect.MaterialShininess = 20f;

                defaultShadingEffect.ModelTexture = tex;
                // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                defaultShadingEffect.ModelTextureGammaCorrection = true;

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = defaultShadingEffect.GetEffect();
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
            Matrix translationMatrix = Matrix.CreateTranslation(Position + Vector3.Transform(EntityModelOffset, rotationMatrix));
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
            if (rot.X != 0f || rot.Y != 0f || rot.Z != 0f || rot.W != 0)
            {
                Rotation = Quaternion.Normalize(new Quaternion(rot));
            }

            ImGui.DragFloat("Scale", ref Scale, 0.01f);
            ImGui.TextWrapped("* Changing scale only changes the rendering scale and not the collision entity scale.");

            ImGui.NewLine();

            ImGui.Text($"Collision body: {Entity?.GetType()?.Name ?? "None"}");
            if (Entity != null)
            {
                System.Numerics.Vector3 modelOffset = EntityModelOffset.ToNumerics();
                ImGui.DragFloat3("Origin offset (between Graphic & Physics)", ref modelOffset);
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
