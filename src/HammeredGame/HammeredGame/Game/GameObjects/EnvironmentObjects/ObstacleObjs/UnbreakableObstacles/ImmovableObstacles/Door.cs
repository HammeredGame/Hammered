using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
    /// <summary>
    /// The <c>Door</c> class is an immovable obstacle within the game world, blocking the player's
    /// access to other parts of the map.
    /// <para />
    /// Doors have a <code>keyFound</code> property that indicates whether the player has found
    /// the associated key (that will open the door). If this property has been successfully set
    /// (player has key) and the player interacts with the door, the door will open up and cease
    /// blocking the player's path.
    /// </summary>
    ///
    /// <remarks>
    /// <para />
    /// REMINDER (class tree): GameObject -> EnvironmentObject -> ObstacleObject -> UnbreakableObstacle
    ///                         -> ImmovableObstacle
    /// <para />
    /// The current implementation of the door opening up just sets the door's visible status to false,
    /// resulting in it not being drawn on screen anymore and not considered for any further collisions.
    /// In the future, maybe we should be applying some form of animation, instead of it abruptly
    /// disappearing.
    /// <para />
    /// Temporarily, including a parameter <code>isGoal</code> (for the purposes of the demo
    /// submission). This indicates whether the door is the goal of the level - results in the
    /// "PUZZLE SOLVED" text to appear on the debug console (see Player.cs).
    /// <para />
    /// NOTE: THIS WILL NOT SCALE FOR FUTURE LEVELS!!!
    /// <para />
    /// TODO: remove the isGoal from this class. Potentially, make another class that exclusively
    /// handles the goal state - possibly an empty game object that does not get rendered, still
    /// checks for player collisions, and if the player makes it here with the necessary conditions
    /// satisfied, we trigger a cutscene/load next level/etc.
    /// </remarks>

    public class Door : ImmovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool keyFound;
        private bool isGoal; // TEMPORARY

        public Door(GameServices services, Model model, Texture2D t, Vector3 pos, Quaternion rotation, float scale, bool isGoal = false) : base(services, model, t, pos, rotation, scale)
        {
            keyFound = false;
            this.isGoal = isGoal;
        }

        public void SetKeyFound(bool keyFound)
        {
            this.keyFound = keyFound;
        }

        public override void TouchingPlayer(Player player)
        {
            if (keyFound)
            {
                player.ReachedGoal = isGoal; // TEMPORARY
                this.SetVisible(false);
            }
            base.TouchingPlayer(player);
        }
    }
}
