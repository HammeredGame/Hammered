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
        public bool Continuable;
        public Action ContinueFunc;
        public Action StartNewFunc;
        public Action ToggleDebugUIFunc;

        public override void LoadMenuWidgets()
        {
            base.LoadMenuWidgets();

            MenuHeaderText = "HAMMERED";

            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            Label menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
                Enabled = false,
                Font = BarlowFont.GetFont(oneLineHeight)
            };

            if (Continuable)
            {
                menuItemContinue.Enabled = true;
                menuItemContinue.TouchUp += (s, a) =>
                {
                    ContinueFunc?.Invoke();
                    ExitScreen(alsoUnloadContent: true);
                };
            }

            Label menuItemStartGame = new()
            {
                Text = "Start New",
                Id = "_menuItemStartNew",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemStartGame.TouchUp += (s, a) =>
            {
                StartNewFunc?.Invoke();
                ExitScreen(alsoUnloadContent: true);
            };

            Label menuItemOptions = new()
            {
                Text = "Options",
                Id = "_menuItemOptions",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemOptions.TouchUp += (s, a) =>
            {
                ScreenManager.AddScreen(new OptionsScreen());
            };

            Label menuItemToggleDebugUI = new()
            {
                Text = "Toggle Debug UI",
                Id = "_menuItemToggleDebugUI",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemToggleDebugUI.TouchUp += (s, a) =>
            {
                ToggleDebugUIFunc?.Invoke();
            };

            Label menuItemQuitToDesktop = new()
            {
                Text = "Quit to Desktop",
                Id = "_menuItemQuitToDesktop",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemQuitToDesktop.TouchUp += (_, _) => Environment.Exit(0);

            MenuWidgets = new List<Widget>() { menuItemContinue, menuItemStartGame, menuItemOptions, menuItemToggleDebugUI, menuItemQuitToDesktop };
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);
        }

    }
}
