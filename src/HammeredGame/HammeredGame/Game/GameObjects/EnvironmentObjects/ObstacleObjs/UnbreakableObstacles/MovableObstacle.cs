﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles
{
    public class MovableObstacle : UnbreakableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here

        public MovableObstacle(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            this.computeBounds();
            base.Update(gameTime);
        }
    }
}
