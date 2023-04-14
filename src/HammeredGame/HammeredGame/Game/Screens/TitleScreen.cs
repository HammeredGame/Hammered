using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;

namespace HammeredGame.Game.Screens
{
    internal class TitleScreen : AbstractMenuScreen
    {
        public Action ContinueFunc;
        public Action StartNewFunc;

        public TitleScreen()
        {
            IsPartial = true;
        }

        public override void LoadContent()
        {
            // Set up list of menu items before calling base.LoadContent();

            MenuHeaderText = "HAMMERED";

            MenuItem menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
            };
            menuItemContinue.Selected += (s, a) =>
            {
                ContinueFunc?.Invoke();
                ExitScreen(alsoUnloadContent: true);
            };

            MenuItem menuItemRestartLevel = new()
            {
                Text = "Start New",
                Id = "_menuItemStartNew"
            };
            menuItemRestartLevel.Selected += (s, a) =>
            {
                StartNewFunc?.Invoke();
                ExitScreen(alsoUnloadContent: true);
            };

            MenuItem menuItemOptions = new()
            {
                Text = "Options",
                Id = "_menuItemOptions"
            };

            MenuItem menuItemCredits = new()
            {
                Text = "Credits",
                Id = "_menuItemCredits"
            };

            MenuItem menuItemQuitToDesktop = new()
            {
                Text = "Quit to Desktop",
                Id = "_menuItemQuitToDesktop",
            };
            menuItemQuitToDesktop.Selected += (_, _) => Environment.Exit(0);

            MenuItems = new List<MenuItem>() { menuItemContinue, menuItemRestartLevel, menuItemOptions, menuItemCredits, menuItemQuitToDesktop };
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);
        }

    }
}
