using BEPUphysics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics.Entities;
using BEPUphysics.PositionUpdating;

namespace HammeredGame.Game.GameObjects
{
    /// <summary>
    /// The <c>EmptyGameObject</c> class encompasses all game objects that will not have a visible mesh, but will
    /// still be involved in certain interactions (event triggers/world bounds/etc.)
    /// <para />
    /// </summary>
    /// <remarks>
    /// REMINDER (class tree): GameObject -> EmptyGameObject
    /// <para />
    /// </remarks>
    public class EmptyGameObject : GameObject
    {
        public EmptyGameObject(Model model, Vector3 pos, float scale, Texture2D t, Space space, Entity entity)
            : base(model, pos, scale, t, space)
        {
            // these game objects should not be visible
            this.SetVisible(false);

            this.Entity = entity;
            this.Entity.PositionUpdateMode = PositionUpdateMode.Continuous;
            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.NoBroadPhase;

            this.ActiveSpace.Add(this.Entity);
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

    }
}
