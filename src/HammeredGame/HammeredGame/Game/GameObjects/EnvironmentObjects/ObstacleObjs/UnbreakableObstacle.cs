using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEPUphysics;
using HammeredGame.Core;
using BEPUphysics.Entities;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs
{
    /// <summary>
    /// The <c>UnbreakableObstacle</c> class handles any properties and interactions common to all
    /// unbreakable obstacles (those that will still remain in the game world after any hammer interaction)
    /// within the game world.
    /// <para />
    ///
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    /// <para />
    /// </remarks>

    public class UnbreakableObstacle : ObstacleObject
    {
        // Any Unbreakable Obstacle specific variables go here

        public UnbreakableObstacle(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, Entity entity) : base(services, model, t, pos, rotation, scale, entity)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        ///// <remarks>
        ///// This function should only be entered with a hammer in the <c>Enroute</c> state,
        ///// i.e if the hammer is in the <c>WithCharacter</c> state and this function is entered,
        ///// something has gone wrong.
        ///// </remarks>
        //public override void TouchingHammer(Hammer hammer)
        //{
        //    // Unbreakable obstacles currently fully block the hammer on it's path back to the player
        //    hammer.SetState(Hammer.HammerState.Dropped);
        //    hammer.Position = hammer.OldPosition;
        //}
    }
}
