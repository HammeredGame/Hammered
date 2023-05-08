using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
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

        public Progress<int> ReportProgress
        {
            get { return new Progress<int>(i => this.Progress = i); }
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

            backgroundColor = new Color(0f, 0f, 0f, 1f);

            int fontSize = MathHelper.Max(ScreenManager.GraphicsDevice.Viewport.Height / 20, 20);

            byte[] skranjiTtfData = System.IO.File.ReadAllBytes("Content/Skranji-Regular.ttf");
            FontSystem skranjiFontSystem = new FontSystem();
            skranjiFontSystem.AddFont(skranjiTtfData);

            text = new Label
            {
                Text = "Loading",
                Wrap = true,
                TextColor = Color.White,
                Font = skranjiFontSystem.GetFont(fontSize),
                Margin = new Thickness(0, 0, 100, 100),
            };

            var panel = new VerticalStackPanel();
            panel.Widgets.Add(text);

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

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            text.Text = Progress.ToString();

            Desktop.UpdateInput();
            Desktop.UpdateLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            spriteBatch.Begin();
            spriteBatch.Draw(whiteRectangle, Vector2.Zero, null, backgroundColor, 0f, Vector2.Zero, new Vector2(ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height), SpriteEffects.None, 0f);
            spriteBatch.End();

            Desktop.RenderVisual();
        }
    }
}
