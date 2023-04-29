using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// The pause screen is a menu screen that shows Paused stuff. In general, whenever
    /// ExitScreen() is called within PauseScreen, it shouldn't unload its contents. This
    /// is because GameScreen reuses the same pause screen and removes/adds it, to save
    /// on expensive content-loading at runtime.
    /// </summary>
    internal class PauseScreen : AbstractMenuScreen
    {
        public Action ContinueMethod;
        public Action RestartMethod;
        public Action QuitMethod;

        public override void LoadContent()
        {
            // Set up list of menu items before calling base.LoadContent();

            MenuHeaderText = "PAUSED";

            MenuItem menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
            };
            // Keep screen contents loaded since the Pause Screen will be re-added again
            menuItemContinue.Selected += (s, a) =>
            {
                ContinueMethod?.Invoke();
                ExitScreen(alsoUnloadContent: false);
            };

            MenuItem menuItemRestartLevel = new()
            {
                Text = "Restart Level",
                Id = "_menuItemRestartLevel"
            };
            menuItemRestartLevel.Selected += (s, a) =>
            {
                RestartMethod?.Invoke();
                // Keep screen contents loaded since the Pause Screen will be re-added again
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
            menuItemQuitToTitle.Selected += (s, a) =>
            {
                QuitMethod?.Invoke();
                ExitScreen(alsoUnloadContent: true);
            };

            MenuItems = new List<MenuItem>() { menuItemContinue, menuItemRestartLevel, menuItemOptions, menuItemQuitToTitle };
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);

            // Do nothing if the screen doesn't have focus.
            if (!HasFocus) return;

            Input input = GameServices.GetService<Input>();

            // Back out of pause menu without unloading content
            if (UserAction.Pause.Pressed(input) || UserAction.Back.Pressed(input))
            {
                ContinueMethod?.Invoke();
                ExitScreen(alsoUnloadContent: false);
            }
        }
    }
}
