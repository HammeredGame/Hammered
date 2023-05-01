using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// The title screen is a menu screen that shows options to start a new game or continue.
    /// </summary>
    internal class TitleScreen : AbstractMenuScreen
    {
        public Action ContinueFunc;
        public Action StartNewFunc;

        public override void LoadContent()
        {
            // Set up list of menu items before calling base.LoadContent();

            MenuHeaderText = "HAMMERED";

            MenuItem menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
                Enabled = false
            };
            // Setting Enabled = false when creating a MenuItem doesn't seem to disable its
            // click handler events, so it's commented for now:
            //
            //menuItemContinue.Selected += (s, a) =>
            //{
            //    ContinueFunc?.Invoke();
            //    ExitScreen(alsoUnloadContent: true);
            //};

            MenuItem menuItemStartGame = new()
            {
                Text = "Start New",
                Id = "_menuItemStartNew"
            };
            menuItemStartGame.Selected += (s, a) =>
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

            MenuItems = new List<MenuItem>() { menuItemContinue, menuItemStartGame, menuItemOptions, menuItemCredits, menuItemQuitToDesktop };
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);
        }

    }
}
