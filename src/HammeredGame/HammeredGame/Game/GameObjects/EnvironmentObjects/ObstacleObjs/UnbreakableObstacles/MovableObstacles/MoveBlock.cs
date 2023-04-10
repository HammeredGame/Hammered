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

        public void SetCollisionGroup(CollisionGroup cg)
        {
            this.Entity.CollisionInformation.CollisionRules.Group = cg;
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
                else if (other.Tag is Player || other.Tag is ObstacleObject)
                {
                    // TODO: Revisit hitting another movable block
                    //if (other.Tag is MoveBlock)
                    //{
                    //    var otherMoveBlock = other.Tag as MoveBlock;
                    //    if (otherMoveBlock != null)
                    //    {
                    //        otherMoveBlock.setMoving()
                    //    }
                    //}

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
            else
            {
                // Handle static meshes like interaction when hitting water surface
                if (other.Tag is Water)
                {
                    this.SetStationary();
                    mbState = MBState.InWater;
                }
            }
        }

        private void SetMoving(BEPUutilities.Vector3 velocity)
        {
            this.Entity.BecomeDynamic(1.0f);
            this.Entity.LinearVelocity = velocity;
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
            mbState = MBState.Moving;
        }

        private void SetStationary()
        {
            this.Entity.LinearVelocity = new BEPUutilities.Vector3(0f, -98.1f, 0f);
            this.Entity.BecomeDynamic(10000f);
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
            mbState = MBState.Stationary;
        }
    }
}
