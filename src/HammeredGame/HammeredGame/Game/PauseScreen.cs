using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;

namespace HammeredGame.Game
{
    internal class PauseScreen : Screen
    {
        private Desktop desktop;

        private Action restartLevelFunc;

        Texture2D whiteRectangle;

        public PauseScreen(Action restartLevelFunc) {
            IsPartial = true;
            this.restartLevelFunc = restartLevelFunc;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            var label1 = new Label
            {
                Text = "Hammered",
                TextColor = Color.LightBlue,
            };

            MenuItem menuItemContinue = new()
            {
                Text = "Continue",
                Id = "menuItemContinue"
            };
            menuItemContinue.Selected += (s, a) => ExitScreen();

            MenuItem menuItemRestartLevel = new()
            {
                Text = "Restart Level",
                Id = "_menuItemRestartLevel"
            };
            menuItemRestartLevel.Selected += (s, a) =>
            {
                restartLevelFunc.Invoke();
                ExitScreen();
            };

            MenuItem menuItemOptions = new()
            {
                Text = "Options",
                Id = "_menuItemOptions"
            };

            MenuItem menuItemQuitToTitle = new()
            {
                Text = "Quit to Title",
                Id = "menuItemQuitToTitle"
            };
            menuItemQuitToTitle.Selected += (s, a) => Environment.Exit(0); // todo quit to title & unload stuff

            VerticalMenu mainMenu = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                LabelColor = Color.Indigo,
                SelectionHoverBackground = new SolidBrush("#808000FF"),
                SelectionBackground = new SolidBrush("#FFA500FF"),
                LabelHorizontalAlignment = HorizontalAlignment.Center,
                HoverIndexCanBeNull = false,
                Background = new SolidBrush("#00000000"),
                Border = new SolidBrush("#00000000"),
                Id = "_mainMenu",
                Padding = new Thickness(100)
            };
            mainMenu.Items.Add(menuItemContinue);
            mainMenu.Items.Add(menuItemRestartLevel);
            mainMenu.Items.Add(menuItemOptions);
            mainMenu.Items.Add(menuItemQuitToTitle);
            mainMenu.HoverIndex = 0;

            var grid = new Grid
            {
                RowSpacing = 8,
                ColumnSpacing = 8
            };
            grid.Widgets.Add(label1);
            grid.Widgets.Add(mainMenu);
            // Add it to the desktop
            desktop = new();
            desktop.Root = grid;

            // Make main menu permanently hold keyboard focus as long as it's the active screen
            // todo: i don't think this works
            desktop.WidgetLosingKeyboardFocus += (s, a) =>
            {
                a.Cancel = HasFocus;
            };
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            whiteRectangle.Dispose();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            Input input = GameServices.GetService<Input>();
            if (!otherScreenHasFocus && (input.ButtonPress(Buttons.B) || input.ButtonPress(Buttons.Start) || input.KeyPress(Keys.Escape)))
            {
                ExitScreen();
            }

            if (!otherScreenHasFocus && (MathF.Abs(input.GamePadState.ThumbSticks.Left.Y) > 0.5))
            {
                Grid grid = desktop.Root as Grid;
                VerticalMenu mainMenu = grid.Widgets[1] as VerticalMenu;
                mainMenu.HoverIndex = (mainMenu.HoverIndex - MathF.Sign(input.GamePadState.ThumbSticks.Left.Y) + mainMenu.Items.Count) % mainMenu.Items.Count;
            }

            if (!otherScreenHasFocus && input.ButtonPress(Buttons.A))
            {
                ((desktop.Root as Grid)?.Widgets[1] as VerticalMenu)?.OnKeyDown(Keys.Enter);
            }

        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            Color bgColor = new(75, 43, 58);

            GameServices.GetService<SpriteBatch>().Begin();

            int maxWidthMenuUI = (desktop.Root as Grid).Widgets[1].Bounds.Width;

            GameServices.GetService<SpriteBatch>().Draw(whiteRectangle, Vector2.Zero, null, bgColor, 0f, Vector2.Zero, new Vector2(maxWidthMenuUI, ScreenManager.GraphicsDevice.Viewport.Height), SpriteEffects.None, 0f);

            VertexPositionColor[] vertices = new VertexPositionColor[6];
            vertices[0] = new VertexPositionColor(new Vector3(maxWidthMenuUI, ScreenManager.GraphicsDevice.Viewport.Height, 0), bgColor);
            vertices[1] = new VertexPositionColor(new Vector3(maxWidthMenuUI, 0, 0), bgColor);
            vertices[2] = new VertexPositionColor(new Vector3(maxWidthMenuUI + 300, 0, 0), bgColor);

            BasicEffect basicEffect = new BasicEffect(ScreenManager.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height, 0, 0, 1);

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                ScreenManager.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, vertices, 0, 1);
            }
            GameServices.GetService<SpriteBatch>().End();

            desktop.Render();
        }
    }
}
