using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
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

        public override void hitByPlayer(Player player)
        {
            if (keyFound)
            {
                this.setVisible(false);
            }
            base.hitByPlayer(player);
        }
    }
}
