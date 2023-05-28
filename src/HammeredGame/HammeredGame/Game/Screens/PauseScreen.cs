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
        public Action OnExit;

        private readonly GameScreen parentGameScreen;

        public PauseScreen(GameScreen parentGameScreen)
        {
            this.parentGameScreen = parentGameScreen;
        }

        public override void LoadMenuWidgets()
        {
            base.LoadMenuWidgets();

            MenuHeaderText = "PAUSED";

            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 12;

            Label menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            // Keep screen contents loaded since the Pause Screen will be re-added again
            menuItemContinue.TouchUp += (s, a) =>
            {
                OnExit?.Invoke();
                ExitScreen(alsoUnloadContent: false);
            };

            Label menuItemRestartCheckpoint = new()
            {
                Text = "Restart Checkpoint",
                Id = "_menuItemRestartCheckpoint",
                Font = BarlowFont.GetFont(oneLineHeight),
                Enabled = parentGameScreen.CurrentScene?.CheckpointManager.CheckpointExists() == true
            };
            menuItemRestartCheckpoint.TouchUp += (s, a) =>
            {
                parentGameScreen.CurrentScene.CheckpointManager.ApplyLastCheckpoint();

                OnExit?.Invoke();
                // Keep screen contents loaded since the Pause Screen will be re-added again
                ExitScreen(alsoUnloadContent: false);
            };

            Label menuItemRestartLevel = new()
            {
                Text = "Restart Level",
                Id = "_menuItemRestartLevel",
                Font = BarlowFont.GetFont(oneLineHeight),
                Enabled = parentGameScreen.CurrentScene != null
            };
            menuItemRestartLevel.TouchUp += (s, a) =>
            {
                parentGameScreen.CurrentScene.CheckpointManager.ResetAllCheckpoints();
                parentGameScreen.InitializeLevel(parentGameScreen.CurrentScene.GetType().FullName);

                OnExit?.Invoke();
                // Keep screen contents loaded since the Pause Screen will be re-added again
                ExitScreen(alsoUnloadContent: false);
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

            Label menuItemQuitToTitle = new()
            {
                Text = "Quit to Title",
                Id = "menuItemQuitToTitle",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemQuitToTitle.TouchUp += (s, a) =>
            {
                // Specifies the callback function when Quit To Title is called. We also need to
                // specify the Restart Level callback, but this is done just before each time the
                // screen is added to the manager, since we need the name of the currently active level.

                // Save the last scene
                GameServices.GetService<UserSettings>().LastSaveScene = parentGameScreen.CurrentScene.GetType().FullName;
                GameServices.GetService<UserSettings>().Save();

                // Ask the main game class to recreate the title screen, since it needs to
                // assign handlers that we don't have access to
                GameServices.GetService<HammeredGame>().InitTitleScreen();
                OnExit?.Invoke();
                ExitScreen(alsoUnloadContent: true);
                parentGameScreen.ExitScreen(true);
            };

            MenuWidgets = new List<Widget>() { menuItemContinue, menuItemRestartCheckpoint, menuItemRestartLevel, menuItemOptions, menuItemQuitToTitle };
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);

            // Update the menu item enabled states depending on the current scene state
            Desktop.GetWidgetByID("_menuItemRestartCheckpoint").Enabled = parentGameScreen.CurrentScene?.CheckpointManager.CheckpointExists() == true;
            Desktop.GetWidgetByID("_menuItemRestartLevel").Enabled = parentGameScreen.CurrentScene != null;

            // Do nothing more if the screen doesn't have focus.
            if (!HasFocus) return;

            Input input = GameServices.GetService<Input>();

            // Back out of pause menu without unloading content
            if (UserAction.Pause.Pressed(input) || UserAction.Back.Pressed(input))
            {
                OnExit?.Invoke();
                ExitScreen(alsoUnloadContent: false);
            }
        }
    }
}
