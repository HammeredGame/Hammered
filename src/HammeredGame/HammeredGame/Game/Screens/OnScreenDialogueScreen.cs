using FontStashSharp;
using HammeredGame.Core;
using HammeredGame.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Pleasing;
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
        private Image dialoguePromptImage;

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
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, (int)(tenthPercentageHeight * 0.5f)),
                Opacity = 0f // begin its life as transparent
            };

            dialogueLabel = new Label
            {
                TextColor = Color.White,
                Padding = new Thickness(50, 20, 50, 20),
                Text = "",
                // minimum 16 font size
                Font = barlowFontSystem.GetFont(MathHelper.Max(tenthPercentageHeight * 0.4f, 16f)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidBrush(new Color(75, 43, 58)), // purple background
            };

            // Show a small image with the Confirm button in the corner, so players know what to
            // press to move forward. This image can be overriden in Update() based on the currently
            // active input type too.
            List<TextureRegion> confirmButton = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Confirm);
            dialoguePromptImage = new Image
            {
                // Since the confirm action is discrete, there is only one possible button for it:
                // we show the 0th index
                Renderable = confirmButton[0],
                Opacity = 0.7f,
                HorizontalAlignment = HorizontalAlignment.Center,
                // Make sure to set both width and height to prevent any weird stretching issues
                Width = (int)MathHelper.Max(tenthPercentageHeight * 0.6f, 24f),
                Height = (int)MathHelper.Max(tenthPercentageHeight * 0.6f, 24f)
            };

            dialoguesPanel.AddChild(dialogueLabel);
            dialoguesPanel.AddChild(dialoguePromptImage);

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
                if (UserAction.Confirm.Pressed(GameServices.GetService<Input>()))
                {
                    (_, TaskCompletionSource taskCompletionSource) = dialogueQueue.Dequeue();
                    taskCompletionSource.SetResult();
                }
                else
                {
                    if (dialogueLabel.Text == "")
                    {
                        Tweening.Tween(dialoguesPanel, nameof(dialoguesPanel.Opacity), 1f, 100, Easing.Linear, LerpFunctions.Float);
                        Tweening.Tween((f) => dialoguesPanel.Top = (int)f, 10f, 0f, 100, Easing.Quadratic.Out, LerpFunctions.Float);
                    }
                    // Otherwise show the top dialogue in the queue without dequeuing it
                    dialogueLabel.Text = dialogueQueue.Peek().Item1;

                    // And update the prompt image just in case the input type changed
                    // Input.Prompts.GetImagesForAction() performs a lookup into its internal asset
                    // store, but will never cause an expensive IO operation (it will default to
                    // keyboard if atlas not found) so we're fine to perform this in Update().
                    // Updating the Myra UI is probably the more expensive bottleneck if at all.
                    List<TextureRegion> confirmButton = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Confirm);
                    dialoguePromptImage.Renderable = confirmButton[0];
                }

                desktop.UpdateLayout();
            }
            else
            {
                if (dialogueLabel.Text != "")
                {
                    Tweening.Tween(dialoguesPanel, nameof(dialoguesPanel.Opacity), 0f, 100, Easing.Linear, LerpFunctions.Float);
                    Tweening.Tween((f) => dialoguesPanel.Top = (int)f, 0f, 10f, 100, Easing.Quadratic.Out, LerpFunctions.Float);
                }
                dialogueLabel.Text = "";
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
