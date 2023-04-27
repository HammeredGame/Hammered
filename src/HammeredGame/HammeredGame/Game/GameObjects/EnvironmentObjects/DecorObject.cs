using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using HammeredGame.Core;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    /// <summary>
    /// The <c>DecorObject</c> class refers to any decorative element of the environment,
    /// which will not block the player's movement.
    /// </summary>

    /// <remarks>
    /// <para>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="DecorObject "/>
    /// </para>
    /// <para>
    /// TODO: Either we do nothing when colliding with these objects (as it is now), or
    /// we trigger some sort of animation (like moving grass, etc.)
    /// </para>
    /// </remarks>
    class DecorObject : EnvironmentObject
    {
        // Any Interactable specific variables go here

        public DecorObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            // Do nothing (for now)
        }
    }
}
