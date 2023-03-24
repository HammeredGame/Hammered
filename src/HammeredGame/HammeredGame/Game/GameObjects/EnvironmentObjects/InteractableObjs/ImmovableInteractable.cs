using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs
{
    public class ImmovableInteractable : ObstacleObject
    {
        // Any Unbreakable Obstacle specific variables go here

        public ImmovableInteractable(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        public override void Draw(Matrix view, Matrix projection)
        {
            if (visible)
                base.Draw(view, projection);
        }
    }
}
