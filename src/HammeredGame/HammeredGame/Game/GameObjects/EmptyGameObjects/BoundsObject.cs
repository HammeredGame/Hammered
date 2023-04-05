using BEPUphysics.Entities;
using BEPUphysics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EmptyGameObjects
{
    /// <summary>
    /// The <c>BoundsObject</c> class represents an empty game object that can be used to
    /// block player movement within the game world (like the world edges/cliff edges/etc.)
    /// <para />
    /// </summary>
    /// <remarks>
    /// REMINDER (class tree): GameObject -> EmptyGameObject -> BoundsObject
    /// <para />
    /// </remarks>
    public class BoundsObject : EmptyGameObject
    {
        public BoundsObject(Model model, Vector3 pos, float scale, Texture2D t, Space space, Entity entity)
            : base(model, pos, scale, t, space, entity)
        {
            this.Entity.Tag = "BoundsObjectBounds";

            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

    }
}
