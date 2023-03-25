﻿using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Classes.GameObjects.EnvironmentObjects.ObstacleObjs
{
    public class BreakableObstacle : ObstacleObject
    {
        // Any Obstacle specific variables go here

        public BreakableObstacle(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        public override void hitByHammer(Hammer hammer)
        {
            if (hammer.isEnroute())
                visible = false;
            //HammeredGame.activeLevelObstacles.Remove(this);
        }
    }
}