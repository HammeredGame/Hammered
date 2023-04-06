﻿using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using Hammered_Physics.Core;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
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
        private float baseSpeed = 20f;

        private float baseControllerSpeed = 0.5f;
        private Vector3 player_vel;

        public Vector3 PreviousPosition;

        // TEMPORARY (FOR TESTING)
        public bool OnTree = false;
        public bool ReachedGoal = false;

        private Camera activeCamera;

        // Initialize player class
        public Player(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale) : base(services, model, t, pos, rotation, scale)
        {
            // Defining the bounding volume entity (currently a box, but this could be
            // defined as a capsule/cylinder/compound/etc. --> see bepuphysics1 repo)
            this.Entity = new Box(MathConverter.Convert(Position), 2, 6, 2, 50);

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
            this.Entity.Material.KineticFriction = 1.5f;

            // Add the entity to the level's physics space - this ensures that this game object
            // will be considered for collision constraint solving (handled by the physics engine)
            this.ActiveSpace.Add(this.Entity);

            // Initialize the collision handlers based on the associated collision events
            this.Entity.CollisionInformation.Events.DetectingInitialCollision += Events_DetectingInitialCollision;
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

            // After checking for inputs, if the player velocity vector is still a zero vector,
            // simply return and don't do anything else, since there is no movement
            if (player_vel.Equals(Vector3.Zero)) return;

            // If there was movement, normalize speed and edit rotation of character model
            // Also account for collisions
            if (moveDirty)
            {
                BEPUutilities.Vector3 Pos = this.Entity.Position;
                this.PreviousPosition = MathConverter.Convert(Pos);

                // Normalize to length 1 regardless of direction, so that diagonals aren't faster than straight
                // Do this only within moveDirty, since otherwise player_vel can be 0 or uninitialised and its unit vector is NaN
                player_vel.Normalize();
                player_vel *= baseSpeed;

                Pos += MathConverter.Convert(player_vel);
                this.Entity.LinearVelocity = MathConverter.Convert(new Vector3(player_vel.X, this.Entity.LinearVelocity.Y, player_vel.Z));

                // At this point, also rotate the player to the direction of movement
                BEPUutilities.Vector3 lookDirection = Pos - MathConverter.Convert(this.PreviousPosition);
                lookDirection.Normalize(); // Normalizing for good measure.
                ///<remark>
                /// The "angle" variable and the subsequent "rotation" variable below
                /// currently handle rotations in the xz plane.
                /// There might be a need to decide whether the character rotation should account for slopes
                /// <example>When walking up an inclined piece of land, the character might be facing upwards.</example>
                ///</remark>
                float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);
                this.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(BEPUutilities.Vector3.UnitY, angle);



                //// Set the player's old position (as of previous tick)
                //// Memorizing this state has multiple uses, including:
                //// a) velocity direction computation (current - old)
                //// b) reverting to the previous position in case of an unwanted state (e.g. character cannot enter water)
                //this.PreviousPosition = this.Position;

                //// Normalize to length 1 regardless of direction, so that diagonals aren't faster than straight
                //// Do this only within moveDirty, since otherwise player_vel can be 0 or uninitialised and its unit vector is NaN
                //player_vel.Normalize();
                //player_vel *= baseSpeed;

                //Position += player_vel;

                //// At this point, also rotate the player to the direction of movement
                //Vector3 lookDirection = Position - PreviousPosition; lookDirection.Normalize(); // Normalizing for good measure.
                /////<remark>
                ///// The "angle" variable and the subsequent "rotation" variable below
                ///// currently handle rotations in the xz plane.
                ///// There might be a need to decide whether the character rotation should account for slopes
                ///// <example>When walking up an inclined piece of land, the character might be facing upwards.</example>
                /////</remark>
                //float angle = (float)Math.Atan2(lookDirection.X, lookDirection.Z);
                //Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);

                //// The bounding box of the character when they move (translated and/or rotated AABB)
                //// is recomputed inside the "foreach" loop

                //this.OnTree = false;
                //// Obstacle collision detection - will be modified/removed later
                //foreach (EnvironmentObject gO in HammeredGame.ActiveLevelObstacles)
                //{
                //    // Very very basic collision detection
                //    // Check for collisions by checking for bounding box intersections
                //    if (gO != null && gO.IsVisible())
                //    {
                //        // We only care for the bounding box of the character if there *is* an obstacle in the scene.
                //        // Otherwise it is wasted computational time.
                //        this.ComputeBounds();

                //        // If the player intersects with another game object
                //        // trigger the hitByPlayer function of that gameobject
                //        if (this.BoundingBox.Intersects(gO.BoundingBox))
                //        {
                //            gO.TouchingPlayer(this);
                //            // TEMPORARY: if the player is not on tree
                //            // and intersects with water (onGround returns if the player has hit a groundobject,
                //            // currently water is the only ground object being considered for collisions),
                //            // then set player back to old position
                //            // There might be a better solution to this
                //            if (gO.IsGround && !this.OnTree)
                //            {
                //                //System.Diagnostics.Debug.WriteLine(this.PreviousPosition + " -> " + this.Position);
                //                this.Position = this.PreviousPosition;
                //                //this.position = Vector3.Zero;
                //            }
                //        }
                //        else
                //        {
                //            gO.NotTouchingPlayer(this);
                //        }

                //        if (gO is Tree t)
                //        {
                //            if (t.isPlayerOn()) this.OnTree = true;
                //        }
                //    }
                //}
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

            ///<remarks>
            /// This is a temporary measure which works for the rectangular map included in the tutorial level of the functional minimum.
            /// To capture map boundaries in an arbitrarily shaped world (e.g. island),
            /// the need to integrate an (external) physics library for precise bounding checks arises.
            ///
            /// TODO: Integrate an (external) physics library in the project.
            /// TODO: Remember to change the clamping values to match the final tutorial level that will be constructed.
            ///</remarks>
            //this.Position = Vector3.Clamp(this.Position, new Vector3(-60f, -60f, -60f), new Vector3(60f, 60f, 60f));
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

        public void UI()
        {
            ImGui.DragFloat("Base Speed", ref baseSpeed, 0.01f);
            ImGui.DragFloat("Base Controller Speed", ref baseControllerSpeed, 0.01f);
        }
    }
}
