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

        private Dictionary<CancellationToken, HashSet<UserAction>> shownControls = new();
        private HorizontalStackPanel controlsPanel;
        private string inputType; // todo: use enum
        private Dictionary<string, TextureRegionAtlas> controlsAtlas = new();
        private FontSystem barlowFontSystem;

        /// <summary>
        /// Create an image (that you can set in Image.Renderable) for the controls associated with
        /// the specified UserAction. The return value of this depends on the current value of
        /// inputType too.
        /// </summary>
        /// <param name="action">The action to return the image for</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Action was null</exception>
        /// <exception cref="NotSupportedException"></exception>
        public List<TextureRegion> GetImagesForAction(UserAction action)
        {
            switch (action)
            {
                case ContinuousUserAction { GamepadContinuousStickSide: var side, KeyboardContinuousKeys: var keys }:
                    if (inputType == "keyboard")
                    {
                        // todo: for keyboard, create an image with all four keys somehow
                        return new List<TextureRegion>() {
                            controlsAtlas[inputType][keys.Item1.ToString() + "_Key_Dark"],
                            controlsAtlas[inputType][keys.Item2.ToString() + "_Key_Dark"],
                            controlsAtlas[inputType][keys.Item3.ToString() + "_Key_Dark"],
                            controlsAtlas[inputType][keys.Item4.ToString() + "_Key_Dark"]
                        };
                    }
                    // for controller, show either XboxSeriesX_Left_Stick or XboxSeriesX_Right_Stick
                    return new List<TextureRegion>() { controlsAtlas[inputType]["XboxSeriesX_" + side + "_Stick"] };

                case DiscreteUserAction { GamepadButton: var button, KeyboardKey: var key }:
                    // For discrete actions, the key or button enum name is enough
                    if (inputType == "keyboard")
                    {
                        return new List<TextureRegion>() { controlsAtlas["keyboard"][key.ToString() + "_Key_Dark"] };
                    }
                    return new List<TextureRegion>() { controlsAtlas[inputType]["XboxSeriesX_" + button.ToString()] };

                case null:
                    throw new ArgumentNullException();

                default:
                    throw new NotSupportedException();
            }
        }

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

            // Detect upon first launch which type of input method is used if possible
            if (GameServices.GetService<Input>().GamePadState.IsConnected)
            {
                inputType = "xbox";
            }
            else
            {
                inputType = "keyboard";
            }

            // Myra uses its own asset manager. The default one uses a File stream based
            // implementation that reads from the directory of the currently executing assembly.
            controlsAtlas[inputType] = MyraEnvironment.DefaultAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/controls_atlas_" + inputType + ".xmat");

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
                }
                else
                {
                    // Loop over each action to be shown with this cancellation token, and add it to
                    // the horizontal UI
                    foreach (UserAction action in shownControls[token])
                    {
                        // Show all the images (1 or 4 of them) in a horizontal layout
                        var singleControlMultipleImagesLayout = new HorizontalStackPanel
                        {
                            HorizontalAlignment = HorizontalAlignment.Center
                        };
                        foreach (TextureRegion image in GetImagesForAction(action))
                        {
                            var imageElement = new Image
                            {
                                // Choose the image suited for the current input type
                                Renderable = image,
                                Opacity = 0.5f,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Height = tenthPercentageHeight
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
            }

            // Load texture atlas when input type changed. This is IO heavy so do it asynchronously,
            // and only once for any input type. TODO: abstract away the constant strings somehow.
            if (GameServices.GetService<Input>().GamePadState.IsConnected && !controlsAtlas.ContainsKey("xbox"))
            {
                new Task(() =>
                {
                    // Don't set inputType until assets are loaded, since an Update() might use it before
                    controlsAtlas["xbox"] = MyraEnvironment.DefaultAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/controls_atlas_xbox.xmat");
                    inputType = "xbox";
                }).Start();
            }
            else if (!GameServices.GetService<Input>().GamePadState.IsConnected && !controlsAtlas.ContainsKey("keyboard"))
            {
                new Task(() =>
                {
                    // Don't set inputType until assets are loaded, since an Update() might use it before
                    controlsAtlas["keyboard"] = MyraEnvironment.DefaultAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/controls_atlas_keyboard.xmat");
                    inputType = "keyboard";
                }).Start();
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
