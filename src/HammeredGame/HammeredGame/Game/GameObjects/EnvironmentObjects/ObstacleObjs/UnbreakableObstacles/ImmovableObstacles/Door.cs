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
    /// </remarks>
    
    public class Door : ImmovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool keyFound;

        public Door(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
            keyFound = false;
        }

        public void SetKeyFound(bool keyFound)
        {
            this.keyFound = keyFound;
        }

        public override void TouchingPlayer(Player player)
        {
            if (keyFound)
            {
                this.SetVisible(false);
            }
            base.TouchingPlayer(player);
        }
    }
}
