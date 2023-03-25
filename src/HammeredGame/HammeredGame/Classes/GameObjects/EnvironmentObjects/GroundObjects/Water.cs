using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImMonoGame.Thing;
using ImGuiNET;

namespace HammeredGame.Classes.GameObjects.EnvironmentObjects.GroundObjects
{
    class Water : FloorObject
    {
        public Water(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}