using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using BEPUphysics.PositionUpdating;
using HammeredGame.Core;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using BEPUphysics.CollisionTests;
using HammeredGame.Graphics;
using Microsoft.Xna.Framework.Content;
using ImGuiNET;
using ImMonoGame.Thing;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
    /// <summary>
    /// The <c>Laser</c> class is an immovable obstacle within the game world, blocking the player's
    /// access to other parts of the map.
    /// <para />
    /// In addition to base <c>GameObject</c> properties, the laser also has the following properties defined:
    /// - the current state of the laser -> <code>LaserState laserState</code>
    ///     -- fully blocks both player and hammer -> <code>LaserState.FullBlocking</code>
    ///     -- blocks hammer, but not the player -> <code>LaserState.HammerBlocking</code>
    ///     -- blocks player, but not the hammer -> <code>LaserState.PlayerBlocking</code>
    ///
    /// - The default length of the laser -> <code>float laserDefaultLength</code>
    ///     -- This is the default start length of the laser (at the start, and the value to return
    ///         the entity height to when unobstructed)
    ///
    /// - The default scale of the laser -> <code>float laserDefaultScale</code>
    ///     -- This is the default starting scale of the laser (at the start, and the value to return
    ///        the draw scale to when unobstructed)
    ///
    /// - The current scale of the laser -> <code>float laserScale</code>
    ///     -- This gets modified as obstacles collide with the laser
    ///
    /// <para/>
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> ImmovableObstacle
    /// <para />
    ///
    /// TODO: Currently, the code only handles the default state of the laser (full blocking). Additionally, it does
    /// not handle any rotating laser functionality.
    ///
    /// </remarks>

    public class Laser : ImmovableObstacle, IImGui
    {
        // Any Unbreakable Obstacle specific variables go here
        public enum LaserState
        {
            FullBlocking,
            HammerBlocking,
            PlayerBlocking
        }

        // Default variables (should ideally only be modified at level/scene setup)
        private float laserDefaultLength;
        private float laserDefaultScale;

        // Dynamic variables
        private LaserState laserState;
        private float laserScale;

        //timedelay for laser looping
        TimeSpan timeDelay = TimeSpan.Zero;
        private bool orientToCamera = true;

        private Texture2D laserTexture;
        private Texture2D laserMaskTexture;

        // Laser configuration which hopefully won't need to change after release, but can be
        // changed during debug through the UI to find better values
        private float laserIntensity = 4f;
        private Vector2 laserSpeed = new(-2f, 0f);

        public Laser(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                this.Entity.Tag = "ImmovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Defer;
                this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, 0, (this.Entity as Box).HalfLength);
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
                this.ActiveSpace.Add(this.Entity);

                this.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
                this.Entity.CollisionInformation.Events.PairRemoved += Events_PairRemoved;
            }

            // Set the default state / variables
            this.laserState = LaserState.PlayerBlocking;
            this.laserDefaultLength = (this.Entity as Box).Length;
            this.laserDefaultScale = this.Scale;
            this.laserScale = this.Scale;

            // We use a custom shader for the Laser which uses a 2D plane mesh and rolling textures
            // and color intensities above 1 to simulate the laser.
            this.Effect = services.GetService<ContentManager>().Load<Effect>("Effects/ForwardRendering/Laser");
            this.laserTexture = services.GetService<ContentManager>().Load<Texture2D>("LaserTexture");
            this.laserMaskTexture = services.GetService<ContentManager>().Load<Texture2D>("LaserMask");

            this.AudioEmitter = new AudioEmitter();
            this.AudioEmitter.Position = this.Position;


            //Services.GetService<AudioManager>().Play3DSound("Audio/new_laser", true, this.AudioEmitter, laserScale/10);

        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            double time = gameTime.TotalGameTime.TotalSeconds;
            timeDelay -= gameTime.ElapsedGameTime;
            if (timeDelay < TimeSpan.Zero)
            {
                Services.GetService<AudioManager>().Play3DSound("Audio/buzz", false, this.AudioEmitter, laserScale / laserDefaultScale);
                timeDelay += TimeSpan.FromSeconds(1.15f);
                //Services.GetService<AudioManager>().Play3DSound("Audio/retro", false, this.AudioEmitter, laserScale/laserDefaultScale);
                //timeDelay += TimeSpan.FromSeconds(2f);
            }


        }

        private void Events_PairRemoved(EntityCollidable sender, BroadPhaseEntry other)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is ObstacleObject)
                {
                    if (other.Tag is Laser) return;
                    // find all valid contact pairs between obstacles
                    // if there are no more such valid pairs, then reset the laser to default length

                    List<BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler> validPairs = new();
                    foreach (var pair in sender.Pairs)
                    {
                        if (pair.EntityA == null || pair.EntityB == null)
                            continue;

                        if (pair.EntityA.CollisionInformation.Tag is ObstacleObject && pair.EntityB.CollisionInformation.Tag is ObstacleObject)
                        {
                            validPairs.Add(pair);
                        }
                    }

                    if (validPairs.Count <= 0)
                    {
                        this.ReturnToDefaultLength();
                    }
                }
            }
        }

        private void Events_DetectingInitialCollision(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is ObstacleObject)
                {
                    if (other.Tag is Laser) return;
                    // Go through the pair contact points to find the closest contact point
                    // from the start of the laser - to calculate the re-scaling factor
                    // of the laser
                    float minScale = 1000f;
                    foreach (var contact in pair.Contacts)
                    {
                        BEPUutilities.Vector3 pointOfContact = contact.Contact.Position;
                        float dist = (pointOfContact - MathConverter.Convert(this.Position)).Length();
                        float scale = (this.laserDefaultScale * dist) / this.laserDefaultLength;
                        minScale = Math.Min(minScale, scale);
                    }

                    if (minScale < this.laserScale) this.SetLaserDynamicScale(minScale);

                    (this.Entity as Box).Length *= this.laserScale / this.laserDefaultScale;
                    this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, 0, (this.Entity as Box).HalfLength);

                }
            }
        }

        public void SetLaserState(LaserState state)
        {
            this.laserState = state;
        }

        public void SetLaserAngle(Quaternion angle)
        {
            this.Rotation = angle;
        }

        public void SetLaserDefaultScale(float scale)
        {
            this.SetLaserDynamicScale(scale);
            //(this.Entity as Box).Length *= this.laserScale / this.laserDefaultScale;
            this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, 0, (this.Entity as Box).HalfLength);
            this.laserDefaultScale = scale;
            this.laserDefaultLength = (this.Entity as Box).Length;
        }

        // For setting the scale by collision
        private void SetLaserDynamicScale(float scale)
        {
            this.laserScale = scale;
        }

        // For overriding the dynamic scale regardless of collision
        public void SetLaserScale(float scale)
        {
            SetLaserDynamicScale(scale);
            (this.Entity as Box).Length *= this.laserScale / this.laserDefaultScale;
            this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, 0, (this.Entity as Box).HalfLength);
        }

        public void ReturnToDefaultLength()
        {
            this.laserScale = this.laserDefaultScale;
            (this.Entity as Box).Length = this.laserDefaultLength;
            this.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0, 0, (this.Entity as Box).HalfLength);
        }

        /// <summary>
        /// Override of the GameObject DrawModel, customized for the laser.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="tex"></param>
        /// <param name="lights"></param>
        public override void DrawModel(GameTime gameTime, Model model, Matrix view, Matrix projection, Vector3 cameraPosition, Texture2D tex, SceneLightSetup lights)
        {
            // Pass the camera position to the world matrix so it calculates a billboard orientation
            Matrix world = GetWorldMatrix(cameraPosition);

            Matrix[] meshTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(meshTransforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    // We're using a Laser-specific shader here. Editing these parameters should go
                    // side-in-side with the shader file, since they're very tightly coupled.
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
                    part.Effect.Parameters["MaterialHasSpecular"]?.SetValue(false);

                    part.Effect.Parameters["LaserMaterial"].SetValue(tex);
                    // invert the gamma correction, assuming the texture is srgb and not linear (usually it is)
                    part.Effect.Parameters["LaserMaterialGammaCorrection"].SetValue(true);

                    // Use inverse-gamma-correction for the alpha masks too, since the fade to
                    // transparency is done in sRGB, and if incorrectly treated as linear, the edges
                    // will jump from fully transparent to immediately very visible.
                    part.Effect.Parameters["LaserTexture"]?.SetValue(laserTexture);
                    part.Effect.Parameters["LaserTextureGammaCorrection"]?.SetValue(true);

                    part.Effect.Parameters["LaserMask"].SetValue(laserMaskTexture);
                    part.Effect.Parameters["LaserMaskGammaCorrection"]?.SetValue(true);

                    part.Effect.Parameters["GameTimeSeconds"]?.SetValue((float)gameTime.TotalGameTime.TotalSeconds);
                    part.Effect.Parameters["LaserIntensity"]?.SetValue(laserIntensity);
                    part.Effect.Parameters["LaserSpeed"]?.SetValue(laserSpeed);
                }
                mesh.Draw();
            }
        }

        /// <summary>
        /// Custom world-matrix calculation that involves moving the orientation pivot to the start
        /// of the laser, and making it face the camera in a cylindrical billboard fashion.
        /// </summary>
        /// <param name="cameraPosition"></param>
        /// <returns></returns>
        public Matrix GetWorldMatrix(Vector3 cameraPosition)
        {
            // This rotation determines the direction of the laser
            Matrix rotationMatrix = Matrix.CreateFromQuaternion(Rotation);

            // For translation, include the model's origin offset so that the collision body
            // position matches with the rendered model
            Matrix translationMatrix = Matrix.CreateTranslation(Position + EntityModelOffset);

            // The laser's pointing direction is in the forward Z axis
            Matrix scaleMatrix = Matrix.CreateScale(this.Scale, this.Scale, this.laserScale);

            // Translation matrix to be applied pre-scaling to move the pivot to the beginning of
            // the laser. This assumes that the laser model is a unit plane of 1m by 1m (where 1
            // meter is 10 game units when the FBX is imported at 0.1 scale)
            Matrix translateOriginToEdgeMatrix = Matrix.CreateTranslation(new Vector3(0, 0, 5f));

            // We use a technique called Arbitrary Axis Billboard (or Axis-aligned Billboard) which
            // basically means that we'll lock the rotation of the object in one axis (here, the
            // laser beam direction), and make it try its best to face the camera. This means that
            // unless you view the plane from extreme angles (which we can control by the camera),
            // it'll always look 3D.
            // Reference: (section 6) https://web.archive.org/web/20150227185952/https://nehe.gamedev.net/article/billboarding_how_to/18011/
            //
            // We want to orient the plane (which points up in positive Y), so that its forward Z
            // axis aligns with the new forward Z as a result of the laser rotation:
            Matrix billboard = Matrix.Identity;
            billboard.Forward = rotationMatrix.Forward;

            // We want to temporarily assign the plane's Up (its flat side) to face the camera. This
            // won't be at 90 degree angles compared to the Forward vector, so we'll correct this soon.
            billboard.Up = -Vector3.Normalize(Position + EntityModelOffset - cameraPosition);

            // Use the laser direction and the direction to the camera to calculate the Right
            // vector, which we can then use to re-calculate the Up vector, so that all vectors are
            // perpendicular to each other and the Forward one is locked.
            billboard.Right = Vector3.Normalize(Vector3.Cross(billboard.Forward, billboard.Up));
            billboard.Up = Vector3.Normalize(Vector3.Cross(billboard.Right, billboard.Forward));

            // Construct world matrix, where calculation is applied onto vectors from left to right
            // in the order written in code

            if (orientToCamera)
            {
                // When using billboards, move the pivot point first, perform scaling (including
                // laser elongation), billboarding (in place of rotation), then move back the pivot
                // and also move it to world coordinates.
                return translateOriginToEdgeMatrix * scaleMatrix * billboard * Matrix.Invert(translateOriginToEdgeMatrix) * translationMatrix;
            }
            else
            {
                // When not using billboards, do the same as above but with the rotation matrix
                // instead of billboard matrix.
                return translateOriginToEdgeMatrix * scaleMatrix * rotationMatrix * Matrix.Invert(translateOriginToEdgeMatrix) * translationMatrix;
            }
        }

        new public void UI()
        {
            base.UI();
            ImGui.Separator();
            ImGui.DragFloat("Laser Length", ref laserScale, 0.01f);
            ImGui.DragFloat("Laser Default Length", ref laserDefaultLength, 0.01f);
            ImGui.DragFloat("Laser Default Scale", ref laserDefaultScale, 0.01f);
            ImGui.Checkbox("Toggle billboard effect", ref orientToCamera);
            ImGui.DragFloat("Laser Intensity", ref laserIntensity, 0.1f, 0f);
            System.Numerics.Vector2 copy = laserSpeed.ToNumerics();
            ImGui.DragFloat2("Laser XY Speed", ref copy, 0.1f);
            laserSpeed = copy;
        }

    }
}
