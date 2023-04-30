using BEPUphysics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
    /// <summary>
    /// The <c>Wall</c> class is an immovable obstacle within the game world, blocking the player's
    /// access to other parts of the map.
    /// <para />
    /// Walls simply act as a world object that blocks player movement.
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> ImmovableObstacle
    /// <para />
    /// </remarks>

    public class Wall : ImmovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here

        public Wall(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            if (this.Entity != null)
            {
                //this.Entity = new Box(MathConverter.Convert(this.Position), 5, 10, 3);
                this.Entity.Tag = "ImmovableObstacleBounds";
                this.Entity.CollisionInformation.Tag = this;
                this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
                this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
                this.Entity.LocalInertiaTensorInverse = new BEPUutilities.Matrix3x3();
                this.ActiveSpace.Add(this.Entity);
            }
        }
    }
}

