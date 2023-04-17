using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace HammeredGame.Game.GameObjects
{
    /// <summary>
    /// The <c>Player</c> class represents the playable character in the game. TODO: Should the class be renamed to 'Character' instead?
    /// <para />
    /// The character is the main medium through which (whom?) the player can initiate interactions in the game.
    /// The player interacts with the character with the use of the keyboard or a controller.
    /// The actions the player can make with the character are:
    /// - Movement along the 3D space (capability to move along the height dimension is dependent on the environment)
    /// - Change the state of the hammer (for details <see cref="Hammer"/>)
    /// </summary>
    /// <remarks>
    /// Documentation: The <c>Player</c> instance will be mentioned as "character" in the following code.
    /// <para />
    /// REMINDER (class tree): GameObject -> Player
    /// <para />
    /// TODO: Should the class be renamed to 'Character' instead?
    /// <para />
    /// Possible extension:
    /// <c>Hammer</c> instance be attached to a <c>Player</c> instance?
    /// This may allow multi-player capabilities or more complex puzzle-solving in the future
    /// <example>
    /// The character exchanges between different hammers in the same puzzle
    /// </example>
    /// </remarks>

    public class Player : GameObject, IImGui
    {
        // Private variables specific to the player class
        private float baseSpeed = 50f;

        private float baseControllerSpeed = 0.5f;
        private Vector3 player_vel;

        // Last known ground position, used to reset player's position
        // if the player comes into contact with a water object
        private Vector3 lastGroundPosition;

        // TEMPORARY (FOR TESTING)
        public bool OnTree = false;
        public bool ReachedGoal = false;

        private Camera activeCamera;
        
        private List<SoundEffect> player_sfx = new List<SoundEffect>();
        private AudioListener listener;
        private AudioEmitter emitter; 

        // Initialize player class
        public Player(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                // Adding a tag to the entity, to allow us to potentially filter and
                // view bounding volumes (for debugging)
                this.Entity.Tag = "PlayerBounds";

                // Setting the entity's collision information tag to the game object itself.
                // This will help in checking for specific collisions in object-specific
                // collision handling. --> See the Events_DetectingInitialCollision function in
                // this file to see an example of how this might be used
                this.Entity.CollisionInformation.Tag = this;

                // Making the character a continuous object prevents it from flying through
                // walls which would be pretty jarring from a player's perspective.
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;

                // Set the entity's local inverse intertia tensor --> this ensures that the
                // player character doesn't just fall over due to gravity
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

                // Increase the entity's kinetic friction variable --> currently being used to
                // reduce the character sliding along the surface. Also, not setting it too high
                // since higher values make the player get stuck on certain parts of an uneven ground mesh.
                // TODO: May want the flat ground meshes be as even and flat as possible
                // (except ramps/stairs/ladders to reach higher elevations --> these can maybe be
                // handled separately within collision handling <-- more testing needed for these settings)
                this.Entity.Material.KineticFriction = 1.0f;

                // Add the entity to the level's physics space - this ensures that this game object
                // will be considered for collision constraint solving (handled by the physics engine)
                this.ActiveSpace.Add(this.Entity);

                // Initialize the collision handlers based on the associated collision events
                this.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
                this.Entity.CollisionInformation.Events.PairTouching += Events_PairTouching;
                this.Entity.CollisionInformation.Events.ContactCreated += Events_ContactCreated;
                
                player_sfx = Services.GetService<List<SoundEffect>>();
                listener = Services.GetService<AudioListener>();
                emitter = Services.GetService<AudioEmitter>();
            }

            // Initial position should be on/over ground
            this.lastGroundPosition = this.Position;
        }

        private void Events_ContactCreated(EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair, BEPUphysics.CollisionTests.ContactData contact)
        {
            // If the player touches water, return the player to the last
            // known ground position
            // Comment this section out, if testing requires walking on water
            if (other.Tag is Water)
            {
                this.Position = this.lastGroundPosition;
            }
        }

        private void Events_PairTouching(EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            // Make some checks to identify if the last ground position should be updated
            if (other.Tag is Ground)
            {
                // If the player is also touching water, then don't update ground position
                foreach (var contactPair in sender.Pairs)
                {
                    if (contactPair.CollidableA.Tag is Water || contactPair.CollidableB.Tag is Water)
                    {
                        return;
                    }
                }

                // If player isn't falling, update last known ground position
                // Falling is currently being determined via a linear y velocity threshold
                if (Math.Abs(this.Entity.LinearVelocity.Y) < 2.5f)
                    this.lastGroundPosition = this.Position;
            }
        }

        // Collision Handling Event for any initial collisions detected
        private void Events_DetectingInitialCollision(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!

                // If player character collides with hammer, set hammer to with character state
                // This should only happen when hammer is called back
                if (other.Tag is Hammer)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        hammer.SetState(Hammer.HammerState.WithCharacter);
                        otherEntityInformation.Entity.BecomeKinematic();
                        otherEntityInformation.Entity.LinearVelocity = BEPUutilities.Vector3.Zero;
                        otherEntityInformation.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoBroadPhase;
                    }
                }
            }
        }

        /// <summary>
        /// Set the active camera in use, which determines the movement vector for the player.
        /// </summary>
        /// <param name="camera"></param>
        public void SetActiveCamera(Camera camera)
        {
            activeCamera = camera;
        }

        // Update (called every tick)
        public override void Update(GameTime gameTime)
        {
            ///<value>
            /// The variable <c>moveDirty</c> indicates whether there has been any input from the player
            /// with the intent to move the character.
            /// <para />
            /// <remarks> Generally, "dirty flags" are used to indicate that some data has changed </remarks>
            ///</value>
            bool moveDirty = false;

            // Zero out the player velocity vector (to remove the possibility of
            // previous computations accumulating/carrying over)
            player_vel = Vector3.Zero;

            Vector3 forwardDirection;
            if (activeCamera != null) {
                // Get the unit vector (parallel to the y=0 ground plane) in the direction deemed
                // "forward" from the current camera perspective. Calculated by projecting the vector of
                // the current camera position to the player position, onto the ground, and normalising it.
                forwardDirection = Vector3.Normalize(Vector3.Multiply(activeCamera.Target - activeCamera.Position, new Vector3(1, 0, 1)));
            } else
            {
                forwardDirection = Vector3.UnitX;
            }

            // Handling input from keyboard.
            moveDirty = this.KeyboardInput(forwardDirection);

            // Handling input from gamepad.
            if (Services.GetService<Input>().GamePadState.IsConnected)
            {
                moveDirty = moveDirty || GamepadInput(forwardDirection);
            }

            // If there was movement, normalize speed and edit rotation of character model
            // Also account for collisions
            if (moveDirty && this.Entity != null && player_vel != Vector3.Zero)
            {
                BEPUutilities.Vector3 Pos = this.Entity.Position;
                
                //FIX: sound effect itself is too grainy (composed of many smaller sounds), awful when layered
                //player_sfx[0].Play(volume: 0.5f, pitch: 0.1f, pan: 0.0f); 
                

                // Normalize to length 1 regardless of direction, so that diagonals aren't faster than straight
                // Do this only within moveDirty, since otherwise player_vel can be 0 or uninitialised and its unit vector is NaN
                player_vel.Normalize();
                player_vel *= baseSpeed;

                this.Entity.LinearVelocity = MathConverter.Convert(new Vector3(player_vel.X, this.Entity.LinearVelocity.Y, player_vel.Z));

                // At this point, also rotate the player to the direction of movement

                ///<remark>
                /// The "angle" variable and the subsequent "rotation" variable below
                /// currently handle rotations in the xz plane.
                /// There might be a need to decide whether the character rotation should account for slopes
                /// <example>When walking up an inclined piece of land, the character might be facing upwards.</example>
                ///</remark>
                float angle = (float)Math.Atan2(player_vel.X, player_vel.Z);
                this.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.UnitY, angle);
            }
            else
            {
                ///<remark>
                /// Leaving the following code chunk on purpose to remind us of possible bugs.
                /// It resulted in the character managing to move when the keys were released.
                /// Thus, the character could move inside water or execute other "illegal" moves.
                ///</remark>
                // No new keypresses or controller interactions this round, so
                // apply a gradual slowdown to any previous velocity
                //player_vel.X *= 0.5f;
                //player_vel.Z *= 0.5f;

                //position += player_vel;
            }

            //// Mouse based rotation (leaving this here temporarily, probably won't need this)

            #region TEMPORARY_MOUSE_BASED_ROTATION

            //MouseState mouseState = Mouse.GetState();
            //Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

            //// Clamp mouse coordinates to viewport
            //if (mousePos.X < 0) mousePos.X = 0;
            //if (mousePos.Y < 0) mousePos.Y = 0;
            //if (mousePos.X > 1280) mousePos.X = (short)1280;
            //if (mousePos.Y > 720) mousePos.Y = (short)720;

            ////bottom left corener to top right corner
            //Vector3 nearSource = new Vector3((float)mousePos.X, (float)mousePos.Y, 0.0f);
            //Vector3 farSource = new Vector3((float)mousePos.X, (float)mousePos.Y, 1.0f);

            //Vector3 nearPoint = viewport.Unproject(new Vector3(mousePos.X,
            //        mousePos.Y, 0.0f),
            //        projection,
            //        view,
            //        Matrix.Identity);

            //Vector3 farPoint = viewport.Unproject(new Vector3(mousePos.X,
            //        mousePos.Y, 1.0f),
            //        projection,
            //        view,
            //        Matrix.Identity);

            //Ray mouseRay = new Ray(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            //Plane plane = new Plane(Vector3.Up, -8.5f);

            //float denominator = Vector3.Dot(plane.Normal, mouseRay.Direction);
            //float numerator = Vector3.Dot(plane.Normal, mouseRay.Position) + plane.D;
            //float t = -(numerator / denominator);

            //Vector3 lookPosition = (nearPoint + mouseRay.Direction * t);
            //Vector3 lookDirection = lookPosition - _characterPosition;
            //float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);

            ////Vector3 charDirection = _characterPosition - nearPoint;
            //////Ray characterRay = new Ray(nearPoint, charDirection);

            ////Vector3 newDirection = direction - charDirection;
            ////float newAngle = (float)Math.Acos(Vector3.Dot(charDirection, newDirection));

            //_characterRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

            #endregion TEMPORARY_MOUSE_BASED_ROTATION

        }

        private bool KeyboardInput(Vector3 forwardDirectionFromCamera)
        {
            // Adjust player velocity based on input
            // Keyboard input (W - forward, S - back, A - left, D - right)

            bool moveDirty = false;
            Input input = Services.GetService<Input>();

            if (input.KeyDown(Keys.W))
            {
                this.player_vel += forwardDirectionFromCamera;
                moveDirty = true;
            }
            if (input.KeyDown(Keys.S))
            {
                this.player_vel += -forwardDirectionFromCamera;
                moveDirty = true;
            }
            if (input.KeyDown(Keys.A))
            {
                this.player_vel += -Vector3.Cross(forwardDirectionFromCamera, Vector3.Up);
                moveDirty = true;
            }
            if (input.KeyDown(Keys.D))
            {
                this.player_vel += Vector3.Cross(forwardDirectionFromCamera, Vector3.Up);
                moveDirty = true;
            }

            return moveDirty;
        }

        private bool GamepadInput(Vector3 forwardDirectionFromCamera)
        {
            bool moveDirty = false;
            Input input = Services.GetService<Input>();

            float MovePad_LeftRight = input.GamePadState.ThumbSticks.Left.X;
            float MovePad_UpDown = input.GamePadState.ThumbSticks.Left.Y;
            if (MovePad_UpDown < -Input.DEADZONE || MovePad_UpDown > Input.DEADZONE || MovePad_LeftRight < -Input.DEADZONE || MovePad_LeftRight > Input.DEADZONE)
            {
                player_vel = (MovePad_LeftRight * Vector3.Cross(forwardDirectionFromCamera, Vector3.Up) + MovePad_UpDown * forwardDirectionFromCamera) * baseControllerSpeed;
                moveDirty = true;
            }

            return moveDirty;
        }

        new public void UI()
        {
            base.UI();
            ImGui.Separator();
            ImGui.DragFloat("Base Speed", ref baseSpeed, 0.01f);
            ImGui.DragFloat("Base Controller Speed", ref baseControllerSpeed, 0.01f);
        }
    }
}
