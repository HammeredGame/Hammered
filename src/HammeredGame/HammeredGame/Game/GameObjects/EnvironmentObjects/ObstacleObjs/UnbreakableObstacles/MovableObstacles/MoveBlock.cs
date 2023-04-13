﻿using BEPUphysics.Entities.Prefabs;
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
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects;
using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles
{
    /// <summary>
    /// The <c>MoveBlock</c> class is a movable obstacle within the game world, contextually
    /// reacting to the hammer and player interactions. Specifically, this can be used to 
    /// represent any movable blocks/rocks in the world.
    /// <para />
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> MovableObstacle -> MoveBlock
    /// <para />
    /// </remarks>

    public class MoveBlock : MovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        public enum MBState
        {
            Stationary,
            Moving,
            InWater
        }
        private MBState mbState;
        private BEPUutilities.Vector3 initialMovementVelocity = BEPUutilities.Vector3.Zero;

        public MoveBlock(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                this.Entity.Tag = "MovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.Defer;
                this.SetStationary();
                this.Entity.Material.KineticFriction = 1.5f;
                this.ActiveSpace.Add(this.Entity);
                this.Entity.CollisionInformation.Events.InitialCollisionDetected += this.Events_InitialCollisionDetected;
            }

            mbState = MBState.Stationary;
        }

        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            if (this.mbState == MBState.InWater) 
            {
                // If in water, do nothing with the block - the block is
                // essentially submerged and cannot move anymore

                // TODO: Once the MoveBlock is set to the InWater state,
                // there may need to be some checks that allow the player
                // to walk over the rock to cross the water (similar to the tree maybe?)
                if (other.Tag is Player)
                {
                    var player = other.Tag as Player;
                    player.Entity.Position = new BEPUutilities.Vector3(player.Entity.Position.X, player.Entity.Position.Y + (this.Entity as Box).Width + 1.0f, player.Entity.Position.Z);
                }

                return;
            }

            // Check if collided object is a static mesh (ground/water) or an entity
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                // If colliding with a moving hammer, set the move block to move in the same direction
                if (other.Tag is Hammer && this.mbState != MBState.Moving)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        this.SetMoving(hammer.Entity.LinearVelocity * 0.5f);
                    }
                }
                else if (this.mbState == MBState.Moving)
                {
                    // Otherwise, the only collisions we care about is if the block is already moving
                    // If so, and the colliding object is the player or another obstacle,
                    // then handle these cases appropriately (if needed, otherwise default behavior
                    // is for the MoveBlock to come to a stop.
                    if (other.Tag is Player || other.Tag is ObstacleObject)
                    {
                        // If hitting another stationary MoveBlock, set that one to move in the
                        // same direction as the current moving MoveBlock
                        // TODO: Revisit this implementation, depending on whether the desired behavior is different.
                        // The current implementation makes the current MoveBlock stop in it's tracks,
                        // and the colliding MoveBlock begins moving
                        if (other.Tag is MoveBlock)
                        {
                            var otherMoveBlock = other.Tag as MoveBlock;
                            if (otherMoveBlock != null && otherMoveBlock.mbState == MBState.Stationary)
                            {
                                otherMoveBlock.SetMoving(initialMovementVelocity);
                            }
                        }
                        else if (other.Tag is Laser)
                        {
                            // (maybe) TEMPORARY: lasers shall not make the block stationary for now
                            return;
                        }

                        // A player or blocking obstacle will stop the movable block
                        this.SetStationary();
                    }
                }
            }
            else
            {
                // Handle static meshes, like interaction when hitting water surface
                if (other.Tag is Water)
                {
                    this.SetStationary();
                    mbState = MBState.InWater;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            //if (this.mbState == MBState.Moving) this.Entity.LinearVelocity = initialMovementVelocity;

            //base.Update(gameTime);
        }

        // This function sets the MoveBlock object to a moving state, with the provided velocity.
        // The dynamic mass of the block is set to a small value, so as not to send 
        // the player flying on collision
        private void SetMoving(BEPUutilities.Vector3 velocity)
        {
            this.Entity.BecomeDynamic(1.0f);
            BEPUutilities.Vector3 move_vel = new BEPUutilities.Vector3(velocity.X, this.Entity.LinearVelocity.Y, velocity.Z);
            this.Entity.LinearVelocity = move_vel;
            initialMovementVelocity = move_vel;
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
            mbState = MBState.Moving;
        }

        // On initialization, and on valid collisions with other objects,
        // the block needs to be set to a stationary state.
        // When stationary, the block's mass is set to a really high value,
        // to not allow the player to manually move the block around 
        // (i.e. the hammer must be used to move these blocks around)
        private void SetStationary()
        {
            //this.Entity.LinearVelocity = new BEPUutilities.Vector3(0f, -98.1f, 0f);
            this.Entity.LinearVelocity = BEPUutilities.Vector3.Zero;
            this.Entity.BecomeDynamic(10000f);
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
            mbState = MBState.Stationary;
        }
    }
}
