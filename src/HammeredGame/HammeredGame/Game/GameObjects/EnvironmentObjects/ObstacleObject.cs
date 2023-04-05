using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HammeredGame.Core;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    /// <summary>
    /// The <c>ObstacleObject</c> class handles any properties and interactions common to the various
    /// obstacles that will populate the game world.
    /// <para />
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject
    /// <para />
    /// </remarks>

    public class ObstacleObject : EnvironmentObject
    {
        // Any Obstacle specific variables go here

        public ObstacleObject(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale) : base(services, model, t, pos, rotation, scale)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }

        // Base functionality for handling obstacle collisions with the player within
        // Default behaviour for any obstacle will be to block the player movement.
        // However, other subclasses of <c>ObstacleObject</c> can have additional/modified functionality.
        public override void TouchingPlayer(Player player)
        {
            player.Position = player.PreviousPosition;
        }
    }
}

