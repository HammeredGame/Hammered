using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImMonoGame.Thing;
using ImGuiNET;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game;

namespace HammeredGame.Game.GameObjects.EnvironmentObjects
{
    class FloorObject : EnvironmentObject
    {
        // Any Interactable specific variables go here

        public FloorObject(Model model, Vector3 pos, float scale, Texture2D t) : base(model, pos, scale, t)
        {
            IsGround = true;
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
    }
}
