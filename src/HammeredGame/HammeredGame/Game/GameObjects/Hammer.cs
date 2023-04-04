﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;
using BEPUphysics;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using Hammered_Physics.Core;

namespace HammeredGame.Game.GameObjects
{
    /// <summary>
    /// The <c>Hammer</c> class defines the properties and interactions specific to the core "Hammer" mechanic of the game.
    /// <para/>
    /// In addition to base <c>GameObject</c> properties, the hammer also has the following properties defined:
    /// - speed of the hammer (how fast it will travel, when called back to the player character -> <code>float hammerSpeed</code>
    /// - the current state of the hammer with respect to the keyboard/gamepad input + context within the scene -> <code>HammerState _hammerState</code>
    ///     -- follow the player character -> <code>HammerState.WithCharacter</code>
    ///     -- hammer is dropped (it will stay in the dropped location until called back to player) -> <code>HammerState.Dropped</code>
    ///     -- hammer is called back and must find its way back to the player  -> <code>HammerState.Enroute</code>
    /// <para/>
    /// An additional variable holding the hammer's position in the previous frame/tick is also provided -> <code>Vector3 oldPos</code>.
    /// This variable, along with the hammer state, helps in determining contextual interactions with certain other objects that may be in the scene.
    /// <example>
    /// Determining the falling direction of a tree or blocking the hammer if an unbreakable obstacle is in the way)
    /// </example>
    /// <para/>
    /// This class also has access to an instance of the <c>Player</c> class, mainly for the purpose of path finding, by keeping track of the position
    /// of the player within the level.
    /// </summary>
    ///
    /// <remark>
    /// TODO: Improved path finding - technical achievement of the game!
    /// </remark>
    public class Hammer : GameObject
    {
        public enum HammerState
        {
            WithCharacter,
            Dropped,
            Enroute
        }

        // Hammer specific variables
        private float hammerSpeed = 10f;
        private HammerState hammerState;

        public Vector3 OldPosition { get; private set; }

        private readonly Input input;
        private readonly Player player;

        public Hammer(Model model, Vector3 pos, float scale, Texture2D t, Space space, Input inp, Player p)
            : base(model, pos, scale, t, space)
        {
            this.input = inp;
            player = p;

            hammerState = HammerState.WithCharacter;

            // Defining the bounding volume entity (currently a box, but this could be
            // defined as a capsule/cylinder/compound/etc. --> see bepuphysics1 repo)
            this.Entity = new Box(MathConverter.Convert(this.player.Position), 1, 3, 1);

            // Adding a tag to the entity, to allow us to potentially filter and
            // view bounding volumes (for debugging)
            this.Entity.Tag = "HammerBounds";

            // Setting the entity's collision information tag to the game object itself.
            // This will help in checking for specific collisions in object-specific 
            // collision handling.
            this.Entity.CollisionInformation.Tag = this;

            // Set hammer to continuous collision detection
            this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;

            // Set the entity's collision rule to 'NoBroadPhase' -->
            // This will ensure that the hammer will not be considered for collision
            // constraint solving while attached to the player character
            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoBroadPhase;

            // Set the entity's local inverse intertia tensor --> this ensures that the 
            // player character doesn't just fall over due to gravity
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

            this.Entity.Material.KineticFriction = 1.0f;

            // Add entity to the level's active physics space
            this.ActiveSpace.Add(this.Entity);
        }

        // Update function (called every tick)
        public override void Update(GameTime gameTime)
        {
            OldPosition = this.Position;

            // Ensure hammer follows/sticks with the player,
            // if hammer has not yet been dropped / if hammer is not being called back
            if (hammerState == HammerState.WithCharacter)
            {
                Position = player.GetPosition();
                this.Entity.Position = MathConverter.Convert(player.GetPosition());
            }

            // Get the input via keyboard or gamepad
            KeyboardInput(); GamePadInput();

            // If hammer is called back (successfully), update its position
            // and handle interactions along the way - ending once the hammer is back with player
            /// <remark>
            /// TODO: this is currently just the hammer's position being updated with very naive collision checking
            /// This is most likely where the path finding should take place - so this will need to change for improved hammer mechanics
            /// </remark>
            if (hammerState != HammerState.WithCharacter)
            {
                if (hammerState == HammerState.Enroute)
                {
                    // Update Hammer's Linear Velocity
                    this.Entity.LinearVelocity = hammerSpeed * MathConverter.Convert(player.GetPosition() - this.GetPosition());

                    //// Update position
                    //Position += hammerSpeed * (player.GetPosition() - Position);

                    //// If position is close enough to player, end its traversal
                    //if ((Position - player.GetPosition()).Length() < 0.5f)
                    //{
                    //    hammerState = HammerState.WithCharacter;
                    //}

                    //this.ComputeBounds();
                }

                //// Check for any collisions along the way
                ////BoundingBox hammerbbox = this.GetBounds();
                //foreach(EnvironmentObject gO in HammeredGame.ActiveLevelObstacles)
                //{
                //    if (gO != null && gO.IsVisible())
                //    {
                //        //BoundingBox objectbbox = gO.GetBounds();
                //        if (this.BoundingBox.Intersects(gO.BoundingBox) && hammerState != HammerState.WithCharacter)
                //        {
                //            gO.TouchingHammer(this);
                //        }
                //        else
                //        {
                //            gO.NotTouchingHammer(this);
                //        }
                //    }
                //}
            }
        }

        public void KeyboardInput()
        {
            // Keyboard input (E - drop hammer, Q - Call back hammer)
            // Hammer Drop Mechanic
            if (hammerState == HammerState.WithCharacter && input.KeyDown(Keys.E))
            {
                //hammerState = HammerState.Dropped;
                DropHammer();
                //this.ComputeBounds();
            }

            // Hammer Call Back Mechanic
            // Call back only possible if hammer has already been dropped
            // Otherwise 'Q' does nothing
            if (hammerState == HammerState.Dropped && input.KeyDown(Keys.Q))
            {
                hammerState = HammerState.Enroute;

                // When hammer is enroute, the physics engine shouldn't solve for 
                // collision constraints with it --> rather we want to manually
                // handle collisions
                this.Entity.BecomeKinematic();
            }
        }

        public void GamePadInput()
        {
            // GamePad Control (A - Hammer drop, B - Hammer call back)
            // Same functionality as with above keyboard check
            if (input.GamePadState.IsConnected)
            {
                if (hammerState == HammerState.WithCharacter && input.ButtonPress(Buttons.A))
                {
                    hammerState = HammerState.Dropped;
                    this.ComputeBounds();
                }
                if (hammerState == HammerState.Dropped && input.ButtonPress(Buttons.B))
                {
                    hammerState = HammerState.Enroute;
                }
            }
        }

        public void DropHammer()
        {
            // Set hammer state to dropped
            hammerState = HammerState.Dropped;

            // Add a lot of mass to the hammer, so it becomes a dynamic entity
            // --> this was to ensure that the hammer interacts properly with
            // pressure plates... However, this may also be the cause of other
            // issues, so this bit of code may need to be tweaked.
            this.Entity.BecomeDynamic(10000);
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();

            // Only gravitational force being applied to the entity, velocity in the other
            // directions are zeroed out --> hammer is dropped, so it shouldn't move
            this.Entity.LinearVelocity = new BEPUutilities.Vector3(0, -98.1f, 0);

            // Normal collisions to ensure the physics engine solves collision constraint with 
            // this entity --> Also, probably a cause for issues
            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
        }

        public bool IsEnroute()
        {
            return hammerState == HammerState.Enroute;
        }

        public void SetState(HammerState newState)
        {
            hammerState = newState;
        }
    }
}
