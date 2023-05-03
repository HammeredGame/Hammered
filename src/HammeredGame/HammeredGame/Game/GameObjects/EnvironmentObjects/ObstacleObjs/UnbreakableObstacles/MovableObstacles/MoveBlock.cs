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
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects;
using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using BEPUutilities;
using static HammeredGame.Game.GameObjects.Player;
using System.Numerics;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;

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
        public MBState MblockState { get; private set; }
        private BEPUutilities.Vector3 initialMovementVelocity = BEPUutilities.Vector3.Zero;

        public MoveBlock(GameServices services, Model model, Texture2D t, Microsoft.Xna.Framework.Vector3 pos, Microsoft.Xna.Framework.Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                this.Entity.Tag = "MovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = CollisionRule.Defer;
                this.SetStationary();

                // Setting base kinetic friction and higher gravitational force
                // These settings are meant to help the rock stay on the ground 
                // when moving, while still moving as desired along the hammer's direction
                // TODO: these settings may need tweaking
                this.Entity.Material.KineticFriction = 0.5f;
                float downwardForceY = (this.Scale / 2.0f) * (-1000f);
                this.Entity.Gravity = new BEPUutilities.Vector3(0f, downwardForceY, 0f);
                //this.Entity.LinearDamping = 0f;

                this.ActiveSpace.Add(this.Entity);
                
                this.Entity.CollisionInformation.Events.InitialCollisionDetected += this.Events_InitialCollisionDetected;
                this.Entity.CollisionInformation.Events.PairTouching += this.Events_PairTouching;
                
                this.AudioEmitter = new AudioEmitter();
                this.AudioEmitter.Position = this.Position; 
            }

            MblockState = MBState.Stationary;
        }

        private void Events_PairTouching(EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            // Check if collided object is a static mesh (ground/water) or an entity
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                // If colliding with a moving hammer, set the move block to move in the same direction
                if (other.Tag is Hammer && this.MblockState != MBState.Moving)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        Services.GetService<AudioManager>().Play3DSound("Audio/short_roll", false, this.AudioEmitter, 1);
                        this.SetMoving(hammer.Entity.LinearVelocity);
                    }
                }
            }
        }

        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            if (this.MblockState == MBState.InWater)
            {
                // If in water, do nothing with the block - the block is
                // essentially submerged and cannot move anymore

                // TODO: Once the MoveBlock is set to the InWater state,
                // there may need to be some checks that allow the player
                // to walk over the rock to cross the water (similar to the tree maybe?)
                if (other.Tag is Player)
                {
                    var player = other.Tag as Player;

                    if (player.StandingOn != PlayerOnSurfaceState.OnRock)
                    {
                        float maxY = player.Entity.Position.Y;
                        foreach (var contact in pair.Contacts)
                        {
                            BEPUutilities.Vector3 pointOfContact = contact.Contact.Position;
                            maxY = Math.Max(maxY, pointOfContact.Y);
                        }

                        player.StandingOn = PlayerOnSurfaceState.OnRock;
                        player.Entity.Position = new BEPUutilities.Vector3(player.Entity.Position.X, maxY + (this.Entity as Box).HalfHeight, player.Entity.Position.Z);
                    }
                }

                if (other.Tag is MoveBlock)
                {
                    var otherBlock = other.Tag as MoveBlock;

                    float maxY = otherBlock.Entity.Position.Y;
                    foreach (var contact in pair.Contacts)
                    {
                        BEPUutilities.Vector3 pointOfContact = contact.Contact.Position;
                        maxY = Math.Max(maxY, pointOfContact.Y);
                    }

                    otherBlock.Entity.Position = new BEPUutilities.Vector3(otherBlock.Entity.Position.X, maxY + (this.Entity as Box).Height, otherBlock.Entity.Position.Z);
                    otherBlock.SetMoving(otherBlock.initialMovementVelocity);
                }

                return;
            }

            // Check if collided object is a static mesh (ground/water) or an entity
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                // If colliding with a moving hammer, set the move block to move in the same direction
                if (other.Tag is Hammer && this.MblockState != MBState.Moving)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        Services.GetService<AudioManager>().Play3DSound("Audio/short_roll", false, this.AudioEmitter, 1);
                        this.SetMoving(hammer.Entity.LinearVelocity);
                    }
                }
                else if (this.MblockState == MBState.Moving)
                {

                    // Otherwise, the only collisions we care about is if the block is already moving
                    // If so, and the colliding object is the player or another obstacle,
                    // then handle these cases appropriately (if needed, otherwise default behavior
                    // is for the MoveBlock to come to a stop.
                    if (other.Tag is Player)
                    {
                        // A player or blocking obstacle will stop the movable block
                        this.SetStationary();
                    }
                    
                    if (other.Tag is MoveBlock)
                    {
                        // If hitting another stationary MoveBlock, set that one to move in the
                        // same direction as the current moving MoveBlock
                        // TODO: Revisit this implementation, depending on whether the desired behavior is different.
                        // The current implementation makes the current MoveBlock stop in it's tracks,
                        // and the colliding MoveBlock begins moving
                        var otherMoveBlock = other.Tag as MoveBlock;
                        if (otherMoveBlock != null && otherMoveBlock.MblockState == MBState.Stationary)
                        {
                            otherMoveBlock.SetMoving(initialMovementVelocity);
                        }

                    }
                    
                    //if (other.Tag is PressurePlate)
                    //{
                    //    System.Console.WriteLine("MoveBlock hit pressure plate\n");
                    //}
                }
                
            }
            else
            {
                // Handle static meshes, like interaction when hitting water surface
                if (other.Tag is Water)
                {
                    this.SetStationary();
                    MblockState = MBState.InWater;
                    Services.GetService<AudioManager>().Play3DSound("Audio/rock_water", false, this.AudioEmitter, 1);
                }
            }
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            //if (this.mbState == MBState.Moving) this.Entity.LinearVelocity = initialMovementVelocity;

            if (this.MblockState == MBState.Moving)
            {
                var speed = this.Entity.LinearVelocity.Length();
                if (speed <= 0.01f) this.SetStationary();
                else this.SetMoving(initialMovementVelocity);
            }

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
            MblockState = MBState.Moving;
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
            MblockState = MBState.Stationary;
        }
    }
}
