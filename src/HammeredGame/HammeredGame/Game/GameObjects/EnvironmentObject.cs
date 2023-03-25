using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImMonoGame.Thing;
using ImGuiNET;

namespace HammeredGame.Game.GameObjects
{
    public class EnvironmentObject : GameObject
    {
        public bool isGround = false;
        public EnvironmentObject(Model model, Vector3 pos, float scale, Texture2D t)
            : base (model, pos, scale, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }


        public virtual void hitByHammer(Hammer hammer) { }

        public virtual void notHitByHammer(Hammer hammer) { }

        public virtual void hitByPlayer(Player player) { }

        public virtual void notHitByPlayer(Player player) { }
    }
}
