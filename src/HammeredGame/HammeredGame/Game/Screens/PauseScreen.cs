using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;

namespace HammeredGame.Game.Screens
{
    internal class PauseScreen : AbstractMenuScreen
    {

        public Action RestartLevelFunc;

        public PauseScreen()
        {
            IsPartial = true;
        }

        public override void LoadContent()
        {
            // Set up list of menu items before calling base.LoadContent();

            MenuHeaderText = "PAUSED";

            MenuItem menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
            };
            menuItemContinue.Selected += (s, a) => ExitScreen(alsoUnloadContent: false);

            MenuItem menuItemRestartLevel = new()
            {
                Text = "Restart Level",
                Id = "_menuItemRestartLevel"
            };
            menuItemRestartLevel.Selected += (s, a) =>
            {
                RestartLevelFunc?.Invoke();
                ExitScreen(alsoUnloadContent: false);
            };

            MenuItem menuItemOptions = new()
            {
                Text = "Options",
                Id = "_menuItemOptions"
            };

            MenuItem menuItemQuitToTitle = new()
            {
                Text = "Quit to Title",
                Id = "menuItemQuitToTitle",
            };
            menuItemQuitToTitle.Selected += (s, a) => Environment.Exit(0); // todo quit to title & unload stuff

            MenuItems = new List<MenuItem>() { menuItemContinue, menuItemRestartLevel, menuItemOptions, menuItemQuitToTitle };
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);
        }

    }
}
