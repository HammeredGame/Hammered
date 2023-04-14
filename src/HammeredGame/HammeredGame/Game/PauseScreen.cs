using FontStashSharp;
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

        public Action RestartLevelFunc;

        private Texture2D whiteRectangle;

        private int maxWidthMenuUI;

        public PauseScreen()
        {
            IsPartial = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            // todo handle viewport resize by checking if .Height changed in Update()
            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            byte[] skranjiTtfData = System.IO.File.ReadAllBytes("Content/Skranji-Regular.ttf");
            FontSystem skranjiFontSystem = new FontSystem();
            skranjiFontSystem.AddFont(skranjiTtfData);

            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            FontSystem barlowFontSystem = new FontSystem();
            barlowFontSystem.AddFont(barlowTtfData);

            var label1 = new Label
            {
                Text = "PAUSED",
                TextColor = Color.White,
                Font = skranjiFontSystem.GetFont(oneLineHeight * 1.5f),
                Margin = new Thickness(100, oneLineHeight, 0, 0),
            };

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

            VerticalMenu mainMenu = new()
            {
                LabelColor = new Color(255, 255, 255, 198),
                SelectionHoverBackground = new SolidBrush("#00000000"),
                SelectionBackground = new SolidBrush("#00000000"),
                LabelHorizontalAlignment = HorizontalAlignment.Left,
                HoverIndexCanBeNull = false,
                Background = new SolidBrush("#00000000"),
                Border = new SolidBrush("#00000000"),
                Id = "_mainMenu",
                Margin = new Thickness(100, (int)(oneLineHeight * 0.5f)),
                LabelFont = barlowFontSystem.GetFont(oneLineHeight),
            };
            mainMenu.Items.Add(menuItemContinue);
            mainMenu.Items.Add(menuItemRestartLevel);
            mainMenu.Items.Add(menuItemOptions);
            mainMenu.Items.Add(menuItemQuitToTitle);
            mainMenu.HoverIndex = 0;

            var panel = new VerticalStackPanel();
            panel.Widgets.Add(label1);
            panel.Widgets.Add(mainMenu);
            // Add it to the desktop
            desktop = new();
            desktop.Root = panel;

            // Make main menu permanently hold keyboard focus as long as it's the active screen
            // todo: i don't think this works
            desktop.WidgetLosingKeyboardFocus += (s, a) =>
            {
                a.Cancel = HasFocus;
            };

            // Precalculate layout once so we know the width of the text to draw the background for
            desktop.UpdateLayout();

            maxWidthMenuUI = (desktop.Root as VerticalStackPanel).Widgets[1].Bounds.Width;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            whiteRectangle.Dispose();
        }

        public override void Update(GameTime gameTime, bool isBelowAnotherScreen, bool isCoveredByNonPartialScreen)
        {
            // Update screen state and HasFocus (= active state && is on top) so we can use it
            base.Update(gameTime, isBelowAnotherScreen, isCoveredByNonPartialScreen);

            // Do nothing if the screen doesn't have focus
            if (!HasFocus) return;

            Input input = GameServices.GetService<Input>();

            // Update the UI based on input, handling any handlers
            desktop.UpdateInput();

            VerticalStackPanel panel = desktop.Root as VerticalStackPanel;
            VerticalMenu mainMenu = panel.Widgets[1] as VerticalMenu;

            // Back out of pause menu without unloading content
            if (input.ButtonPress(Buttons.B) || input.ButtonPress(Buttons.Start) || input.KeyPress(Keys.Escape))
            {
                ExitScreen(alsoUnloadContent: false);
            }

            // Scroll with left thumbstick on controller
            if (MathF.Abs(input.GamePadState.ThumbSticks.Left.Y) > 0.5)
            {
                mainMenu.HoverIndex = (mainMenu.HoverIndex - MathF.Sign(input.GamePadState.ThumbSticks.Left.Y) + mainMenu.Items.Count) % mainMenu.Items.Count;
            }

            // Controller selection support
            if (input.ButtonPress(Buttons.A))
            {
                ((desktop.Root as VerticalStackPanel)?.Widgets[1] as VerticalMenu)?.OnKeyDown(Keys.Enter);
            }

            // Change text color and show a ">" cursor on the currently active item
            foreach (MenuItem menuItem in mainMenu.Items)
            {
                if (menuItem.Index == mainMenu.HoverIndex && menuItem.Text[0] != '>')
                {
                    menuItem.Text = "> " + menuItem.Text;
                    menuItem.Color = new Color(246, 101, 255);
                }
                else if (menuItem.Index != mainMenu.HoverIndex && menuItem.Text[0] == '>')
                {
                    menuItem.Text = menuItem.Text.Substring(2);
                    menuItem.Color = new Color(255, 255, 255, 198);
                }
            }

            // Update the UI layout based on any changes to it above
            desktop.UpdateLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Nice looking purple color for the background
            Color bgColor = new(75, 43, 58);

            // Draw everything in a batch for speed
            GameServices.GetService<SpriteBatch>().Begin();

            // Draw a rectangle at least the width of the menu and its padding
            GameServices.GetService<SpriteBatch>().Draw(whiteRectangle, Vector2.Zero, null, bgColor, 0f, Vector2.Zero, new Vector2(maxWidthMenuUI, ScreenManager.GraphicsDevice.Viewport.Height), SpriteEffects.None, 0f);

            // Draw a triangle next to the previous rectangle, spanning 300 on the top (TODO; change?)
            VertexPositionColor[] vertices = new VertexPositionColor[6];
            vertices[0] = new VertexPositionColor(new Vector3(maxWidthMenuUI, ScreenManager.GraphicsDevice.Viewport.Height, 0), bgColor);
            vertices[1] = new VertexPositionColor(new Vector3(maxWidthMenuUI, 0, 0), bgColor);
            vertices[2] = new VertexPositionColor(new Vector3(maxWidthMenuUI + 300, 0, 0), bgColor);

            // Set up a basic effect and an orthographic projection to draw the previous triangle
            // vertices onto the screen
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

            // Render the UI
            desktop.RenderVisual();
        }
    }
}
