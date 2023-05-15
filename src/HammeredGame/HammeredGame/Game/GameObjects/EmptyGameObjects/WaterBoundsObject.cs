using BEPUphysics.Entities;
using BEPUphysics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;

namespace HammeredGame.Game.GameObjects.EmptyGameObjects
{
    /// <summary>
    /// The <c>WaterBoundsObject</c> class represents an empty game object that can be used to
    /// block player movement into/across water, unless an environmental bridge is made (eg: tree, rock)
    /// <para />
    /// </summary>
    /// <remarks>
    /// REMINDER (class tree): GameObject -> EmptyGameObject -> WaterBoundsObject
    /// <para />
    /// </remarks>
    public class WaterBoundsObject : EmptyGameObject
    {
        public WaterBoundsObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            this.Entity.Tag = "BoundsObjectBounds";
            this.Entity.CollisionInformation.Tag = this;
            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Defer;

            this.Entity.CollisionInformation.Events.PairTouching += this.Events_PairTouching;
            this.Entity.CollisionInformation.Events.CollisionEnded += this.Events_CollisionEnded;
        }

        private void Events_CollisionEnded(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                if (other.Tag is EmptyGameObject) return;

                // Check if there are no other tree/moveblock objects colliding with the water bounds object.
                // If there are no other collisions, only then should the bounds be active
                int num_collisions = 0;
                foreach (var p in sender.Pairs)
                {
                    if (p.EntityA.Equals(this.Entity))
                    {
                        if (p.EntityB != null)
                        {
                            var otherObj = other.Tag as GameObject;
                            if (otherObj != null)
                            {
                                if (otherObj is EmptyGameObject) continue;
                                if (p.EntityB.CollisionInformation.Tag is not MoveBlock && p.EntityB.CollisionInformation.Tag is not Tree) continue;
                                if (!p.EntityB.Equals(otherObj.Entity))
                                {
                                    num_collisions++;
                                }
                            }
                        }
                    }
                    else if (p.EntityB.Equals(this.Entity))
                    {
                        if (p.EntityA != null)
                        {
                            var otherObj = other.Tag as GameObject;
                            if (otherObj != null)
                            {
                                if (otherObj is EmptyGameObject) continue;
                                if (p.EntityA.CollisionInformation.Tag is not MoveBlock && p.EntityA.CollisionInformation.Tag is not Tree) continue;
                                if (!p.EntityA.Equals(otherObj.Entity))
                                {
                                    num_collisions++;
                                }
                            }
                        }
                    }
                }

                if (num_collisions == 0)
                {
                    this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Defer;
                }
            }
        }

        private void Events_PairTouching(EntityCollidable sender, Collidable other, CollidablePairHandler pair)
        {
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //We hit an entity!
                //if (other.Tag is MoveBlock || other.Tag is Tree)
                //{
                //    this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
                //}
                if (other.Tag is MoveBlock)
                {
                    var moveblock = other.Tag as MoveBlock;
                    if (moveblock.MblockState == MoveBlock.MBState.InWater)
                        this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
                }
                if (other.Tag is Tree)
                {
                    var tree = other.Tag as Tree;
                    if (tree.IsTreeFallen())
                        this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoSolver;
                }
            }
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }

    }
}
