using BEPUphysics;
﻿using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles
{
    /// <summary>
    /// The <c>MovableObstacle</c> class is a subclass of unbreakable obstacle that
    /// cannot be broken, but can still move around within the game world.
    /// <para />
    /// On interaction with the hammer, these obstacles change their state within the world.
    /// This may be a change in properties (position, rotation, etc.) or an animation trigger.
    /// (See the documentation for the subclasses of <c>MovableObstacle</c> for specific interaction
    /// descriptions)
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> MovableObstacle
    /// <para />
    /// </remarks>

    public class MovableObstacle : UnbreakableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here

        public MovableObstacle(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale) : base(services, model, t, pos, rotation, scale)
        {
        }

        public override void Update(GameTime gameTime)
        {
            //this.ComputeBounds();
            base.Update(gameTime);
        }
    }
}
