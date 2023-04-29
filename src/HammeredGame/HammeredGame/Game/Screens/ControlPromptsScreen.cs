using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static HammeredGame.Game.UserAction;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// This screen is a partial non-focus-stealing screen that shows screen overlays for input
    /// prompts. ShowPromptsFor() decides what prompts to show.
    /// </summary>
    internal class ControlPromptsScreen : Screen
    {
        private Desktop desktop;

        private readonly Dictionary<CancellationToken, HashSet<UserAction>> shownControls = new();
        private HorizontalStackPanel controlsPanel;
        private FontSystem barlowFontSystem;

        public ControlPromptsScreen()
        {
            IsPartial = true;
            PassesFocusThrough = true;
        }

        /// <summary>
        /// Show input prompts for a list of actions. Provide a cancellation token to stop showing it.
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="stopToken"></param>
        public void ShowPromptsFor(List<UserAction> actions, CancellationToken stopToken)
        {
            if (shownControls.ContainsKey(stopToken))
            {
                shownControls[stopToken].UnionWith(actions);
            }
            else
            {
                shownControls.Add(stopToken, new HashSet<UserAction>(actions));
            }
        }

        /// <summary>
        /// Removes all shown prompts.
        /// </summary>
        public void ClearAllPrompts()
        {
            shownControls.Clear();
        }

        public override void LoadContent()
        {
            base.LoadContent();

            int tenthPercentageHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            // Load font
            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            barlowFontSystem = new FontSystem();
            barlowFontSystem.AddFont(barlowTtfData);

            // The horizontal layout at the bottom to add the various simultaneous prompts
            controlsPanel = new HorizontalStackPanel()
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, (int)(tenthPercentageHeight * 1.5f))
            };

            // Add it to the desktop
            desktop = new();
            desktop.Root = controlsPanel;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();
            barlowFontSystem.Dispose();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Don't update prompts if there is something above that is taking focus, like a paused menu
            if (!HasFocus) return;

            int tenthPercentageHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            desktop.UpdateInput();
            controlsPanel.Widgets.Clear();

            // Create the layout for currently actively shown keys, for the active input type
            foreach (CancellationToken token in shownControls.Keys)
            {
                // If a cancellation is requested, remove it
                if (token.IsCancellationRequested)
                {
                    shownControls.Remove(token);
                    continue;
                }


                // Loop over each action to be shown with this cancellation token, and add it to
                // the horizontal UI
                foreach (UserAction action in shownControls[token])
                {
                    // Show all the images (1 or 4 of them) in a horizontal layout
                    var singleControlMultipleImagesLayout = new HorizontalStackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    foreach (TextureRegion image in GameServices.GetService<Input>().Prompts.GetImagesForAction(action))
                    {
                        var imageElement = new Image
                        {
                            // Choose the image suited for the current input type
                            Renderable = image,
                            Opacity = 0.5f,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Height = tenthPercentageHeight,
                            Width = tenthPercentageHeight
                        };
                        singleControlMultipleImagesLayout.AddChild(imageElement);
                    }
                    // We also want to show a label for the action name
                    var label = new Label
                    {
                        // Dark purple
                        TextColor = new(75, 43, 58),
                        Text = action.Name,
                        Font = barlowFontSystem.GetFont(tenthPercentageHeight * 0.3f),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    // Layout is such that the horizontal image list is above the label
                    var singleControlLayout = new VerticalStackPanel()
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(100, 0)
                    };
                    singleControlLayout.AddChild(singleControlMultipleImagesLayout);
                    singleControlLayout.AddChild(label);

                    controlsPanel.AddChild(singleControlLayout);
                }
            }

            desktop.UpdateLayout();
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
