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

        public override void LoadMenuWidgets()
        {
            base.LoadMenuWidgets();

            MenuHeaderText = "PAUSED";

            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            Label menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            // Keep screen contents loaded since the Pause Screen will be re-added again
            menuItemContinue.TouchUp += (s, a) =>
            {
                ContinueMethod?.Invoke();
                ExitScreen(alsoUnloadContent: false);
            };

            Label menuItemRestartLevel = new()
            {
                Text = "Restart Level",
                Id = "_menuItemRestartLevel",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemRestartLevel.TouchUp += (s, a) =>
            {
                RestartMethod?.Invoke();
                // Keep screen contents loaded since the Pause Screen will be re-added again
                ExitScreen(alsoUnloadContent: false);
            };

            Label menuItemOptions = new()
            {
                Text = "Options",
                Id = "_menuItemOptions",
                Enabled = false,
                Font = BarlowFont.GetFont(oneLineHeight)
            };

            Label menuItemQuitToTitle = new()
            {
                Text = "Quit to Title",
                Id = "menuItemQuitToTitle",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemQuitToTitle.TouchUp += (s, a) =>
            {
                QuitMethod?.Invoke();
                ExitScreen(alsoUnloadContent: true);
            };

            MenuWidgets = new List<Widget>() { menuItemContinue, menuItemRestartLevel, menuItemOptions, menuItemQuitToTitle };
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
