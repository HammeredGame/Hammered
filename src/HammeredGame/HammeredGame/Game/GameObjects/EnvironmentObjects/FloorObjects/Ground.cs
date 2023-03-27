using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Game.GameObjects.EnvironmentObjects;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects
{

    /// <summary>
    /// The <c>Ground</c> class refers to solid ground the character (<see cref="Player"/> may step on.
    /// Free movement of the character along the ground is permitted.
    /// </summary>

    /// <remarks>
    /// REMINDER (class tree): <see cref="GameObject "/> ---> <see cref="EnvironmentObject "/>
    ///                         ---> <see cref="FloorObject "/> ---> <see cref="Ground"/>
    /// </remarks>
    class Ground : FloorObject
    {
        public Ground(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}

