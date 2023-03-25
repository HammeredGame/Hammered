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
    class FloorObject : EnvironmentObject, IImGui
    {
        // Any Interactable specific variables go here

        public FloorObject(Model model, Vector3 pos, float scale, Camera cam, Texture2D t)
            : base(model, pos, scale, cam, t)
        {
            isGround = true;
        }

        public override void Update(GameTime gameTime)
        {
            // Do nothing (for now)
        }
        public void UI()
        {
            ImGui.SetNextWindowBgAlpha(0.3f);
            ImGui.Begin("Ground Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing);

            var numericPos = position.ToNumerics();
            ImGui.DragFloat3("Position", ref numericPos);
            position = numericPos;

            ImGui.End();
        }

    }
}
