using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs
{
    public class UnbreakableObstacle : ObstacleObject
    {
        // Any Unbreakable Obstacle specific variables go here

        public UnbreakableObstacle(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        public override void hitByHammer(Hammer hammer)
        {
            hammer.setEnroute(false);
            hammer.position = hammer.oldPos;
        }
    }
}
