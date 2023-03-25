using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles
{
    public class Door : ImmovableObstacle
    {
        // Any Unbreakable Obstacle specific variables go here
        private bool _keyFound;

        public Door(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
            _keyFound = false;
        }

        public void setKeyFound(bool keyFound)
        {
            _keyFound = keyFound;
        }

        public override void hitByPlayer(Player player)
        {
            if (_keyFound)
            {
                this.setVisible(false);
            }
            base.hitByPlayer(player);
        }
    }
}
