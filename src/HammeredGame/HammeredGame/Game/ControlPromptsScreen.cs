using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using Myra.Assets;
using Myra;
using Myra.Graphics2D.TextureAtlases;
using System.Threading;

namespace HammeredGame.Game
{
    internal class ControlPromptsScreen : Screen
    {
        private Desktop desktop;

        private Dictionary<CancellationToken, VerticalStackPanel> shownControls = new();
        private HorizontalStackPanel controlsPanel;
        TextureRegionAtlas controlsAtlas;
        FontSystem barlowFontSystem;

        public ControlPromptsScreen()
        {
            IsPartial = true;
            PassesFocusThrough = true;
        }

        public void ShowPromptsFor(List<string> controls, CancellationToken stopToken)
        {
            foreach (string control in controls)
            {
                var image = new Image
                {
                    Renderable = controlsAtlas[control]
                };

                var label = new Label
                {
                    Text = "test",
                    Font = barlowFontSystem.GetFont(30)
                };

                var singleControlLayout = new VerticalStackPanel();
                singleControlLayout.AddChild(image);
                singleControlLayout.AddChild(label);

                System.Diagnostics.Debug.WriteLine("called here" + control);
                controlsPanel.AddChild(singleControlLayout);
                shownControls.Add(stopToken, singleControlLayout);
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // Myra uses its own asset manager. The default one uses a File stream based
            // implementation that reads from the directory of the currently executing assembly.
            controlsAtlas = MyraEnvironment.DefaultAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/controls_atlas_xbox.xmat");

            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            barlowFontSystem = new FontSystem();
            barlowFontSystem.AddFont(barlowTtfData);

            controlsPanel = new HorizontalStackPanel();

            // Add it to the desktop
            desktop = new();
            desktop.Root = controlsPanel;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            desktop.UpdateInput();
            // Remove any control that should no longer be shown
            // TODO: check if this causes an exception by modifying the dictionary while looping
            foreach (CancellationToken token in shownControls.Keys)
            {
                if (token.IsCancellationRequested)
                {
                    controlsPanel.RemoveChild(shownControls[token]);
                    shownControls.Remove(token);
                }
            }
            desktop.UpdateLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            desktop.RenderVisual();
        }
    }
}
