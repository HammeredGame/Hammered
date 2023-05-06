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
        public BoundsObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
            this.Entity.Tag = "BoundsObjectBounds";
            this.Entity.CollisionInformation.Tag = this;
            this.Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }

    }
}
