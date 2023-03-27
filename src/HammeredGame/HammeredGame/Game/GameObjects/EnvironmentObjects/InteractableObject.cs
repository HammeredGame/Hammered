using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    /// <summary>
    /// The <c>InteractableObject</c> class refers to any element (possibly non-tangible even?) of the environment
    /// which triggers changes in a different <c>EnvironmentObject</c>.
    /// </summary>

    /// <remarks>
    /// TODO: Motivated by the definition provided above, it may be worth considering enriching the constructor of this class
    /// so as to always include another parameter:
    /// the EnvironmentObject which is triggered by the current <c>InteractableObject</c> instance.
    /// </remarks>
    class InteractableObject : EnvironmentObject
    {
        // Any Interactable specific variables go here

        public InteractableObject(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}
