using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs
{
    /// <summary>
    /// The <c>BreakableObstacle</c> class handles any properties and interactions common to all
    /// Breakable obstacles (those that will not be rendered on screen after interaction with the hammer)
    /// within the game world.
    /// <para />
    ///
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> BreakableObstacle
    /// <para />
    /// </remarks>

    public class BreakableObstacle : ObstacleObject
    {
        // Any Obstacle specific variables go here

        public BreakableObstacle(GameServices services, Model model, Vector3 pos, float scale, Texture2D t) : base(services, model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        public override void TouchingHammer(Hammer hammer)
        {
            // If the hammer is enroute (i.e in it's destructive state),
            // this obstacle's visible boolean will be set to false - which indicates
            // that this object should no longer be drawn on screen, as well as not be
            // considered for future collisions.
            if (hammer.IsEnroute())
                Visible = false;
            //HammeredGame.activeLevelObstacles.Remove(this);
        }
    }
}
