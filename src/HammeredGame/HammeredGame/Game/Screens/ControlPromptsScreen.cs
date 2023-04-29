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
        private readonly Dictionary<InputType, TextureRegionAtlas> controlsAtlas = new();
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
            if (action is ContinuousUserAction continuousAction)
            {
                var side = continuousAction.GamepadContinuousStickSide;
                var (up, left, down, right) = continuousAction.KeyboardContinuousKeys;

                // Fall back to keyboard and mouse (which we are guaranteed to have loaded in
                // LoadContent()) if the active input atlas hasn't been loaded yet
                InputType inputType = GameServices.GetService<Input>().CurrentlyActiveInput;
                if (!controlsAtlas.ContainsKey(inputType))
                {
                    inputType = InputType.KeyboardMouse;
                }

                switch (inputType)
                {
                    case InputType.Xbox:
                        // for controller, show either XboxSeriesX_Left_Stick or XboxSeriesX_Right_Stick
                        return new List<TextureRegion>() { controlsAtlas[InputType.Xbox][side] };
                    case InputType.PlayStation:
                        return new();
                    case InputType.Switch:
                        return new();
                    case InputType.KeyboardMouse:
                        // todo: for keyboard, create an image with all four keys somehow
                        return new List<TextureRegion>() {
                                    controlsAtlas[InputType.KeyboardMouse][up.ToString()],
                                    controlsAtlas[InputType.KeyboardMouse][left.ToString()],
                                    controlsAtlas[InputType.KeyboardMouse][down.ToString()],
                                    controlsAtlas[InputType.KeyboardMouse][right.ToString()]
                                };
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (action is DiscreteUserAction discreteAction)
            {
                var button = discreteAction.GamepadButton;
                var key = discreteAction.KeyboardKey;

                // Fall back to keyboard and mouse (which we are guaranteed to have loaded in
                // LoadContent()) if the active input atlas hasn't been loaded yet
                InputType inputType = GameServices.GetService<Input>().CurrentlyActiveInput;
                if (!controlsAtlas.ContainsKey(inputType))
                {
                    inputType = InputType.KeyboardMouse;
                }

                switch (inputType)
                {
                    case InputType.Xbox:
                        return new List<TextureRegion>() { controlsAtlas[InputType.Xbox][button.ToString()] };
                    case InputType.PlayStation:
                        return new();
                    case InputType.Switch:
                        return new();
                    case InputType.KeyboardMouse:
                        return new List<TextureRegion>() { controlsAtlas[InputType.KeyboardMouse][key.ToString()] };
                    default:
                        throw new NotSupportedException();
                }
            }

            return new();
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

            // Load the keyboard atlas by default
            const InputType defaultAtlasType = InputType.KeyboardMouse;

            // Myra uses its own asset manager. The default one uses a File stream based
            // implementation that reads from the directory of the currently executing assembly.
            controlsAtlas[defaultAtlasType] = MyraEnvironment.DefaultAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/" + defaultAtlasType.ToString() + ".xmat");

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
                    foreach (TextureRegion image in GetImagesForAction(action))
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

            // Load texture atlas when input type changed. This is IO heavy so do it asynchronously,
            // and only once for any input type.
            if (!controlsAtlas.ContainsKey(GameServices.GetService<Input>().CurrentlyActiveInput)) {
                new Task(() =>
                {
                    InputType activeType = GameServices.GetService<Input>().CurrentlyActiveInput;
                    controlsAtlas[activeType] = MyraEnvironment.DefaultAssetManager.Load<TextureRegionAtlas>("Content/ControlPrompts/" + activeType + ".xmat");
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
