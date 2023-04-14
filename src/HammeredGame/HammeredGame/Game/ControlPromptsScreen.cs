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

namespace HammeredGame.Game
{
    /// <summary>
    /// This screen is a partial non-focus-stealing screen that shows screen overlays for input
    /// prompts. ShowPromptsFor() decides what prompts to show.
    /// </summary>
    internal class ControlPromptsScreen : Screen
    {
        private Desktop desktop;

        private Dictionary<CancellationToken, List<string>> shownControls = new();
        private HorizontalStackPanel controlsPanel;
        private string inputType; // todo: use enum
        private Dictionary<string, TextureRegionAtlas> controlsAtlas = new();
        private FontSystem barlowFontSystem;

        private Dictionary<string, Dictionary<string, string>> controlImageMapping = new()
        {
            {
                // TODO: abstract possible input actions with an enum instead of string
                "Move", new()
                {
                    { "xbox", "XboxSeriesX_Left_Stick" },
                    { "keyboard", "W_Key_Dark" }
                }
            },
            {
                "Summon Hammer", new()
                {
                    { "xbox", "XboxSeriesX_A" },
                    { "keyboard", "Space_Key_Dark" }
                }
            }
        };

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
        public void ShowPromptsFor(List<string> actions, CancellationToken stopToken)
        {
            shownControls[stopToken] = actions;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            int tenthPercentageHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            // Detect upon first launch which type of input method is used if possible
            if (GameServices.GetService<Input>().GamePadState.IsConnected)
            {
                inputType = "xbox";
            } else
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
                Margin = new Thickness(0, 0, 0, tenthPercentageHeight)
            };

            // Add it to the desktop
            desktop = new();
            desktop.Root = controlsPanel;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

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
                    // Loop over each action to be shown with this cancellation token, and add it to the UI
                    foreach (string action in shownControls[token])
                    {
                        var image = new Image
                        {
                            // Choose the image suited for the current input type
                            Renderable = controlsAtlas[inputType][controlImageMapping[action][inputType]],
                            Opacity = 0.5f,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            MaxHeight = tenthPercentageHeight
                        };

                        var label = new Label
                        {
                            // Dark purple
                            TextColor = new(75, 43, 58),
                            Text = action,
                            Font = barlowFontSystem.GetFont(tenthPercentageHeight * 0.5f),
                            HorizontalAlignment = HorizontalAlignment.Center
                        };

                        var singleControlLayout = new VerticalStackPanel()
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(100, 0)
                        };
                        singleControlLayout.AddChild(image);
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
            } else if (!controlsAtlas.ContainsKey("keyboard"))
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

            desktop.RenderVisual();
        }
    }
}
