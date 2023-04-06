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
    /// The <c>ImmovableObstacle</c> class is a subclass of unbreakable obstacle that
    /// cannot be broken and will also NOT move around within the game world.
    /// <para />
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> ImmovableObstacle
    /// <para />
    /// </remarks>
    public class ImmovableObstacle : UnbreakableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here

        public ImmovableObstacle(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale) : base(services, model, t, pos, rotation, scale)
        {
        }
    }
}


