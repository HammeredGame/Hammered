using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;

namespace HammeredGame.Game.Screens
{
    internal class LoadingScreen : Screen
    {
        protected Desktop Desktop;
        private SpriteBatch spriteBatch;
        private Texture2D whiteRectangle;
        private Color backgroundColor;

        public int Progress;
        private Label text;
        private HorizontalProgressBar progressBar;

        private Effect animatedStripeEffect;

        public Progress<int> ReportProgress
        {
            get { return new Progress<int>(i => this.Progress = i ) ; }
        }

        public LoadingScreen()
        {
            IsPartial = false;
            PassesFocusThrough = false;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            spriteBatch = GameServices.GetService<SpriteBatch>();

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            // Set up the shader for showing a stripe animation
            animatedStripeEffect = GameServices.GetService<ContentManager>().Load<Effect>("Effects/Backgrounds/Stripe");

            animatedStripeEffect.Parameters["Speed"].SetValue(-0.1f);
            animatedStripeEffect.Parameters["Color1"].SetValue(new Color(191, 154, 203).ToVector4());
            animatedStripeEffect.Parameters["Color2"].SetValue(new Color(207, 187, 242).ToVector4());
            // How many pairs of Color1 and Color2 appear on the screen at one time
            animatedStripeEffect.Parameters["Divisions"].SetValue(16.0f);
            // How wider Color2 should be with respect to Color1
            animatedStripeEffect.Parameters["Ratio"].SetValue(3.0f);
            // Aspect ratio required to fix angle calculation since shader only knows screen [0,1] coordinates
            animatedStripeEffect.Parameters["AspectRatio"].SetValue(ScreenManager.GraphicsDevice.Viewport.AspectRatio);
            animatedStripeEffect.Parameters["Angle"].SetValue(0.3f);

            int fontSize = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            byte[] skranjiTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            FontSystem skranjiFontSystem = new FontSystem();
            skranjiFontSystem.AddFont(skranjiTtfData);

            // The center label
            text = new Label
            {
                Text = "Loading...",
                Wrap = true,
                TextColor = Color.White,
                Font = skranjiFontSystem.GetFont(fontSize),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // A progress bar beneath it. The Value will be incremented while progressing.
            progressBar = new()
            {
                Value = 0,
                Top = fontSize / 3,
                // Slightly wider than the text above, added spaces on either side but this is arbitrary
                Width = (int)skranjiFontSystem.GetFont(fontSize).MeasureString(" Loading... ").X,
                HorizontalAlignment = HorizontalAlignment.Center,
                // See through background with signature colour purple as border and filler
                Background = null,
                Border = new SolidBrush(new Color(75, 43, 58)),
                Filler = new SolidBrush(new Color(75, 43, 58)),
                BorderThickness = new Thickness(2)
            };

            var panel = new VerticalStackPanel()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            panel.Widgets.Add(text);
            panel.Widgets.Add(progressBar);

            // Add it to the desktop
            Desktop = new();
            Desktop.Root = panel;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            // Any assets created or loaded without the content manager should be disposed of.
            whiteRectangle.Dispose();
        }

        /// <summary>
        /// Reset the progress of the loading screen back to zero, for when we want to reuse the
        /// same loading screen again later and we don't want a flash of 100% at the start.
        /// </summary>
        public void ResetProgress()
        {
            Progress = 0;
            progressBar.Value = 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            progressBar.Value = Progress;

            Desktop.UpdateInput();
            Desktop.UpdateLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // All of the constant parameters for the stripe animation are pre-set once during
            // LoadContent, so we just update the time parameter here for the animation.
            animatedStripeEffect.Parameters["GameTimeSeconds"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);

            // Draw a black rectangle of screen size by stretching the 1x1 pixel texture and filling
            // the vertices with black. The magic of animated stripes happens entirely within the
            // shader we're using here.
            spriteBatch.Begin(effect: animatedStripeEffect);
            spriteBatch.Draw(whiteRectangle, Vector2.Zero, null, Color.Black, 0f, Vector2.Zero, new Vector2(ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height), SpriteEffects.None, 0f);
            spriteBatch.End();

            Desktop.RenderVisual();
        }
    }
}
