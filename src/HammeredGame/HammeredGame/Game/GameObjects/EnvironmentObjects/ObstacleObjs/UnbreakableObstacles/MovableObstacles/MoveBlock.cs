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
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
                this.SetStationary();
                this.ActiveSpace.Add(this.Entity);
                this.Entity.CollisionInformation.Events.InitialCollisionDetected += this.Events_InitialCollisionDetected;
            }

            mbState = MBState.Stationary;
        }

        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is Hammer)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        this.SetMoving(hammer.Entity.LinearVelocity);
                    }
                }
                else if (this.mbState == MBState.Moving)
                {
                    if (other.Tag is Player || other.Tag is ObstacleObject)
                    {
                        // TODO: Revisit hitting another movable block
                        if (other.Tag is MoveBlock)
                        {
                            var otherMoveBlock = other.Tag as MoveBlock;
                            if (otherMoveBlock != null && otherMoveBlock.mbState == MBState.Stationary)
                            {
                                otherMoveBlock.SetMoving(initialMovementVelocity);
                            }
                        }

                        // Handle Obstacles like fallen tree here
                        if (other.Tag is Tree)
                        {
                            var tree = other.Tag as Tree;
                            // Do nothing if the tree is already fallen
                            // Otherwise, handle it like any other blocking obstacle
                            if (tree != null && tree.IsTreeFallen()) return;
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
                    // TODO: Once the MoveBlock is set to the InWater state,
                    // there needs to be some checks that allow the player
                    // to walk over the rock to cross the water (similar to the tree maybe?)
                }
            }
        }

        // This function sets the MoveBlock object to a moving state, with the provided velocity.
        // The dynamic mass of the block is set to a small value, so as not to send 
        // the player flying on collision
        private void SetMoving(BEPUutilities.Vector3 velocity)
        {
            this.Entity.BecomeDynamic(1.0f);
            this.Entity.LinearVelocity = velocity;
            initialMovementVelocity = velocity;
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
