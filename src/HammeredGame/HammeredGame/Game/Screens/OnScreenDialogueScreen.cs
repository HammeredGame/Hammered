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
using System.Linq;
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
            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Fonts/Barlow-Medium.ttf");
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

                // If the confirmation action is performed, either show the whole dialogue if it
                // hasn't been shown yet, or dequeue the top item and call into any callbacks it had
                if (UserAction.Confirm.Pressed(GameServices.GetService<Input>()))
                {
                    // The currently shown dialogue matches the top of the queue, so dequeue it
                    if (dialogueLabel.Text == dialogueQueue.Peek().Item1)
                    {
                        (_, TaskCompletionSource taskCompletionSource) = dialogueQueue.Dequeue();
                        taskCompletionSource.SetResult();
                    }
                    // Otherwise, show the whole dialogue and make the user press confirm again to
                    // move on
                    else
                    {
                        dialogueLabel.Text = dialogueQueue.Peek().Item1;
                    }
                }
                else
                {
                    // If the dialogue box is empty, it means there was no dialogue shown in the
                    // previous frame - we animate the opacity and position slightly.
                    if (dialogueLabel.Text == "")
                    {
                        Tweening.Tween(dialoguesPanel, nameof(dialoguesPanel.Opacity), 1f, 100, Easing.Linear, LerpFunctions.Float);
                        Tweening.Tween((f) => dialoguesPanel.Top = (int)f, 10f, 0f, 100, Easing.Quadratic.Out, LerpFunctions.Float);
                    }

                    // Show one character at a time from the top dialogue in the queue without
                    // dequeuing it, unless all text is already shown, in which case we don't update
                    // the text.
                    if (dialogueLabel.Text != dialogueQueue.Peek().Item1)
                    {
                        // Number of matching characters from last frame
                        int matchingCharacters = dialogueLabel.Text.TakeWhile((c, i) =>i < dialogueQueue.Peek().Item1.Length && dialogueQueue.Peek().Item1[i] == c).Count();

                        // Show one more character. This will never be out of bounds because if
                        // matchingCharacters + 1 is out of bounds, then matchingCharacters ==
                        // dialogueQueue.Peek().Item1.Length, which means the whole dialogue is
                        // already shown.
                        dialogueLabel.Text = dialogueQueue.Peek().Item1.Substring(0, matchingCharacters + 1);
                    }

                    // In any case, if a dialogue is shown, update the prompt image just in case the
                    // input type changed Input.Prompts.GetImagesForAction() performs a lookup into
                    // its internal asset store, but will never cause an expensive IO operation (it
                    // will default to keyboard if atlas not found) so we're fine to perform this in
                    // Update(). Updating the Myra UI is probably the more expensive bottleneck if
                    // at all.
                    List<TextureRegion> confirmButton = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Confirm);
                    dialoguePromptImage.Renderable = confirmButton[0];
                }

                desktop.UpdateLayout();
            }
            else
            {
                // There was a dialogue in the previous frame - we animate the opacity and position
                // to fade out.
                if (dialogueLabel.Text != "")
                {
                    Tweening.Tween(dialoguesPanel, nameof(dialoguesPanel.Opacity), 0f, 100, Easing.Linear, LerpFunctions.Float);
                    Tweening.Tween((f) => dialoguesPanel.Top = (int)f, 0f, 10f, 100, Easing.Quadratic.Out, LerpFunctions.Float);
                }

                // Reset the dialogue label to empty text and give back focus to the underlying game.
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
