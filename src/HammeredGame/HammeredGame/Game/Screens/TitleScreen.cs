using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Pleasing;
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

        private Texture2D hammerImage;
        protected float HammerImageOpacity = 0f;

        public override void LoadMenuWidgets()
        {
            base.LoadMenuWidgets();

            hammerImage = GameServices.GetService<ContentManager>().Load<Texture2D>("HammerImage");

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

        public override bool UpdateTransitionIn(GameTime gameTime, bool firstFrame)
        {
            bool transitionState = base.UpdateTransitionIn(gameTime, firstFrame);
            if (firstFrame && !transitionState)
            {
                // Add a title-screen specific tween for fading in the hammer image from white
                var fadeTimeline = Tweening.NewTimeline();
                fadeTimeline.AddFloat(this, nameof(HammerImageOpacity))
                    .AddFrame(1200, 1f, Easing.Exponential.Out);
            }
            return transitionState;
        }

        public override void Draw(GameTime gameTime)
        {
            // Draw the hammer image behind, assuming width > height and drawing to a square aligned
            // to the right side of the screen
            int squareSize = ScreenManager.GraphicsDevice.Viewport.Height;
            GameServices.GetService<SpriteBatch>().Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            GameServices.GetService<SpriteBatch>().Draw(
                hammerImage,
                new Rectangle(ScreenManager.GraphicsDevice.Viewport.Width - squareSize, 0, squareSize, squareSize),
                new Color(1f, 1f, 1f, HammerImageOpacity));
            GameServices.GetService<SpriteBatch>().End();

            base.Draw(gameTime);
        }

    }
}
