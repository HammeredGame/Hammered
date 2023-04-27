using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using HammeredGame.Core;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs
{
    /// <summary>
    /// The <c>BreakableObstacle</c> class handles any properties and interactions common to all
    /// Breakable obstacles (those that will not be rendered on screen after interaction with the hammer)
    /// within the game world.
    /// <para />
    ///
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> BreakableObstacle
    /// <para />
    /// </remarks>

    public class BreakableObstacle : ObstacleObject
    {
        // Any Obstacle specific variables go here

        public BreakableObstacle(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            this.Entity.Tag = "BreakableObstacleBounds";
            this.Entity.CollisionInformation.Tag = this;
            this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
            this.ActiveSpace.Add(this.Entity);

            this.Entity.CollisionInformation.Events.InitialCollisionDetected += Events_InitialCollisionDetected; ;
        }

        private void Events_InitialCollisionDetected(BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable sender, BEPUphysics.BroadPhaseEntries.Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
        {
            //This type of event can occur when an entity hits any other object which can be collided with.
            //They aren't always entities; for example, hitting a StaticMesh would trigger this.
            //Entities use EntityCollidables as collision proxies; see if the thing we hit is one.
            var otherEntityInformation = other as EntityCollidable;
            if (otherEntityInformation != null)
            {
                //System.Diagnostics.Debug.WriteLine("OK!");
                if (other.Tag is Hammer)
                {
                    var hammer = other.Tag as Hammer;
                    if (hammer.IsEnroute())
                    {
                        this.Visible = false;
                        this.ActiveSpace.Remove(sender.Entity);
                    }
                }

            }
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }

        //public override void TouchingHammer(Hammer hammer)
        //{
        //    // If the hammer is enroute (i.e in it's destructive state),
        //    // this obstacle's visible boolean will be set to false - which indicates
        //    // that this object should no longer be drawn on screen, as well as not be
        //    // considered for future collisions.
        //    if (hammer.IsEnroute())
        //        Visible = false;
        //    //HammeredGame.activeLevelObstacles.Remove(this);
        //}
    }
}
