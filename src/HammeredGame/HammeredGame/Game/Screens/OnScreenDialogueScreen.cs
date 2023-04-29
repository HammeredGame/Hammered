using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// This screen is a partial focus-stealing screen that shows dialogues.
    /// </summary>
    internal class OnScreenDialogueScreen : Screen
    {
        private Desktop desktop;
        private Queue<(string, TaskCompletionSource)> dialogueQueue = new();
        private VerticalStackPanel dialoguesPanel;
        private Label dialogueLabel;

        private FontSystem barlowFontSystem;

        // a 1x1 white rectangle that we'll use to render the dialogue background
        private Texture2D whiteRectangle;

        private SpriteBatch spriteBatch;

        public OnScreenDialogueScreen()
        {
            IsPartial = true;
            PassesFocusThrough = false;
        }

        /// <summary>
        /// Show a dialogue.
        /// </summary>
        /// <param name="dialogue"></param>
        public Task ShowDialogueAndWait(string dialogue)
        {
            TaskCompletionSource completionSource = new();
            dialogueQueue.Enqueue((dialogue, completionSource));
            return completionSource.Task;
        }

        /// <summary>
        /// Removes all shown prompts.
        /// </summary>
        public void ClearAllDialogues()
        {
            dialogueQueue.Clear();
        }

        public override void LoadContent()
        {
            base.LoadContent();

            spriteBatch = GameServices.GetService<SpriteBatch>();

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            int tenthPercentageHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            // Load font
            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            barlowFontSystem = new FontSystem();
            barlowFontSystem.AddFont(barlowTtfData);

            // The panel at the bottom to add text to
            dialoguesPanel = new VerticalStackPanel()
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, tenthPercentageHeight),
                Background = new SolidBrush(new Color(75, 43, 58)), // purple background
                Opacity = 0f // begin its life as transparent
            };

            dialogueLabel = new Label
            {
                TextColor = Color.White,
                Padding = new Thickness(50, 20, 50, 0),
                Text = "",
                // minimum 16 font size
                Font = barlowFontSystem.GetFont(MathHelper.Max(tenthPercentageHeight * 0.3f, 16f)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Show a small image with the Interact button in the corner, so players know what to
            // press to move forward
            List<TextureRegion> interactButton = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Interact);
            var imageElement = new Image
            {
                // Since the interact action is discrete, there is only one possible button for it:
                // we show the 0th index
                Renderable = interactButton[0],
                Opacity = 0.5f,
                HorizontalAlignment = HorizontalAlignment.Right,
                // Make sure to set both width and height to prevent any weird stretching issues
                Width = (int)MathHelper.Max(tenthPercentageHeight * 0.3f, 16f),
                Height = (int)MathHelper.Max(tenthPercentageHeight * 0.3f, 16f),
                // Adding some padding on the right and bottom somehow causes the image to become
                // smaller to a nice size (?)
                Padding = new Thickness(0, 0, 10, 10),
                // But we want it a little bit bigger without affecting the rest of the UI, so use a
                // Scale transform
                Scale = new Vector2(2, 2)
            };

            dialoguesPanel.AddChild(dialogueLabel);
            dialoguesPanel.AddChild(imageElement);

            // Add it to the desktop
            desktop = new()
            {
                Root = dialoguesPanel
            };
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            // Dispose all assets that were manually created outside of the ContentManager
            whiteRectangle.Dispose();
            barlowFontSystem.Dispose();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Don't update dialogues if there is something above that is taking focus, like a paused menu
            if (!HasFocus) return;

            desktop.UpdateInput();

            if (dialogueQueue.Count > 0)
            {
                PassesFocusThrough = false;

                // Dequeue the top item if the confirmation action is performed, and call into any
                // callbacks it had
                if (UserAction.Interact.Pressed(GameServices.GetService<Input>()))
                {
                    dialoguesPanel.Opacity = 0f;
                    (_, TaskCompletionSource taskCompletionSource) = dialogueQueue.Dequeue();
                    taskCompletionSource.SetResult();
                }
                else
                {
                    dialoguesPanel.Opacity = 1f;
                    // Otherwise show the top dialogue in the queue without dequeuing it
                    dialogueLabel.Text = dialogueQueue.Peek().Item1;
                }

                desktop.UpdateLayout();
            }
            else
            {
                PassesFocusThrough = true;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Don't draw prompts if there is something above that is taking focus, like a paused menu
            if (!HasFocus) return;

            desktop.RenderVisual();
        }
    }
}
