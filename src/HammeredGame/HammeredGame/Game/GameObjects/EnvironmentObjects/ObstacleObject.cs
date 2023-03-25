using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    public class ObstacleObject : EnvironmentObject
    {
        // Any Obstacle specific variables go here

        public ObstacleObject(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        public override void hitByPlayer(Player player)
        {
            player.position = player.oldPos;
        }
    }
}

