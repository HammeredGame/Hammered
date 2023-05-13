using HammeredGame.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// The options screen is a menu screen that shows Options/Settings.
    /// </summary>
    internal class OptionsScreen : AbstractMenuScreen
    {
        public override void LoadMenuWidgets()
        {
            base.LoadMenuWidgets();

            MenuHeaderText = "OPTIONS";
            BackgroundOverlayOpacity = 0.3f;

            UserSettings settings = GameServices.GetService<UserSettings>();

            // We use a smaller font size here, 5% of the screen height
            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 20;

            // Music volume slider, which controls the MediaPlayer volume
            HorizontalStackPanel optionsMusicVolume = CreateSliderOption(
                oneLineHeight,
                "Music Volume",
                (int)(settings.MediaVolume * 10f),
                0,
                10,
                (i) =>
                {
                    // Update and save settings, then update the value in the main game class
                    settings.MediaVolume = i / 10f;
                    settings.Save();
                    GameServices.GetService<HammeredGame>().SetMediaVolume(i / 10f);
                });

            // SFX volume slider, which controls the SFX volume
            HorizontalStackPanel optionsSFXVolume = CreateSliderOption(
                oneLineHeight,
                "SFX Volume",
                (int)(settings.SfxVolume * 10f),
                0,
                10,
                (i) =>
                {
                    // Update and save settings, then update the value in the main game class
                    settings.SfxVolume = i / 10f;
                    settings.Save();
                    GameServices.GetService<HammeredGame>().SetSfxVolume(i / 10f);
                });

            // Get the largest possible resolution for this computer. SupportedDisplayModes comes sorted.
            DisplayMode largest = GameServices.GetService<GraphicsDevice>().Adapter.SupportedDisplayModes.Last();

            // Resolution toggle, which cycles through resolutions, filtered by the largest
            // resolution the GPU supports. On retina Macs, these resolutions are the raw high
            // quality resolutions and not the downscaled one MonoGame sees, which means some of the
            // larger ones will inevitably be larger than what can be displayed. This becomes a
            // problem in full screen but as long as its windowed, players can move it around and
            // scale it down so it's fine for now.
            HorizontalStackPanel optionsResolution = CreateMultipleOption(
                oneLineHeight,
                "Resolution",
                settings.Resolution,
                Resolution.AcceptedList.Where(r => r.Width <= largest.Width && r.Height <= largest.Height).ToArray(),
                (i) =>
                {
                    // Update and save settings, then update the value in the main game class
                    settings.Resolution = i;
                    settings.Save();
                    GameServices.GetService<HammeredGame>().SetResolution(i.Width, i.Height);
                });
            // The above resolution toggle is only enabled when it's not full-screen
            optionsResolution.Enabled = !GameServices.GetService<GraphicsDeviceManager>().IsFullScreen;

            // Full screen toggle
            HorizontalStackPanel optionsFullScreen = CreateToggleOption(
                oneLineHeight,
                "Full Screen",
                settings.FullScreen,
                "Yes",
                "No",
                (i) =>
                {
                    // Update and save settings, then update the value in the main game class
                    settings.FullScreen = i;
                    settings.Save();
                    GameServices.GetService<HammeredGame>().SetResolution(settings.Resolution.Width, settings.Resolution.Height, i);
                });

            // Borderless toggle
            HorizontalStackPanel optionsBorderless = CreateToggleOption(
                oneLineHeight,
                "Borderless",
                settings.Borderless,
                "Yes",
                "No",
                (i) =>
                {
                    // Update and save settings, then update the value in the main game class.
                    settings.Borderless = i;
                    settings.Save();
                    GameServices.GetService<HammeredGame>().SetBorderless(i);
                });

            // An option at the bottom to exit
            Label menuItemBack = new()
            {
                Text = "Back",
                Id = "menuItemBack",
                Font = BarlowFont.GetFont(oneLineHeight)
            };
            menuItemBack.TouchUp += (s, a) =>
            {
                ExitScreen(alsoUnloadContent: true);
            };

            MenuWidgets = new List<Widget>() {
                optionsMusicVolume,
                optionsSFXVolume,
                optionsFullScreen,
                optionsResolution,
                optionsBorderless,
                menuItemBack
            };
        }

        /// <summary>
        /// Create a slider toggle as a menu item. The label is shown on the left side.
        /// </summary>
        /// <param name="fontSize">The font size for the text</param>
        /// <param name="label">The label description</param>
        /// <param name="initialValue">The initial value</param>
        /// <param name="min">Minimum allowed value inclusive</param>
        /// <param name="max">Maximum allowed value inclusive</param>
        /// <param name="setter">The callback to actually set the option</param>
        /// <returns>The widget that you can add to MenuWidgets</returns>
        private HorizontalStackPanel CreateSliderOption(int fontSize, string label, int initialValue, int min, int max, Action<int> setter)
        {
            // Create the horizontal layout, left is label, right is the slider
            HorizontalStackPanel optionContainer = new()
            {
                // Arbitrary, but reusing the font size as horizontal spacing gives nice results
                Spacing = fontSize
            };
            Label optionLabel = new()
            {
                Text = label,
                Id = "option" + label,
                Font = BarlowFont.GetFont(fontSize),
                // White by default
                TextColor = new Color(255, 255, 255, 198)
            };

            HorizontalSlider optionSlider = new()
            {
                // Horizontal slider is a slider of floats, and can take any real value between min
                // and max. We therefore add a snapping mechanism later.
                Minimum = min,
                Maximum = max,
                Value = initialValue,
                // Align to the right, in the center vertically with respect to the text on the left
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                // Arbitrary length and height, this might need to change based on resolution somehow
                MinWidth = 300,
                Height = 16,
                // Tag can be used for anything, we'll store int-s. This is not type-checked
                // anywhere (it's all boxed to objects), so be very careful when setting new values
                // to cast to int first, and when retrieving it to cast back to int.
                Tag = initialValue,
                // The background is white, same as the label.
                Background = new SolidBrush(new Color(255, 255, 255, 198))
            };

            // We define a function for snapping the .Value property of the above slider to integer
            // increments, and play the SFX and call the setter function if the rounded value has
            // changed. This is required because HorizontalSlider doesn't have any snapping support
            // and we need to implement it ourselves to prevent users from choosing any float value.
            void snapAndSetValueIfChange()
            {
                // Snap Value to nearest integer in range
                optionSlider.Value = Math.Max(min, Math.Min(max, (int)Math.Round(optionSlider.Value)));

                // We are using the Tag to store (int)s, so compare the previous value and check for
                // a change
                if ((int)optionSlider.Tag != (int)optionSlider.Value)
                {
                    // Set it as the new Tag
                    optionSlider.Tag = (int)optionSlider.Value;

                    // Play the confirmation SFX. This might seem weird because we're changing
                    // values here, but UX-wise, it makes more sense to have the same sounds as when
                    // you click on a boolean toggle, which is the confirm sound.
                    PlaySFX("Audio/UI/selection_confirm", 0.7f);

                    // Call the setter function
                    setter((int)optionSlider.Value);
                }
            }

            // Add a handler to snap when the value is changed by the user with a mouse
            optionSlider.ValueChangedByUser += (s, a) => snapAndSetValueIfChange();

            // On keyboard down (this is only called if the widget has keyboard focus -- this is
            // ensured in the parent class where every change to HoverIndex also changes the focused widget)
            optionContainer.KeyDown += (s, a) =>
            {
                // By default we use the left right keyboard keys regardless of any input type or
                // key remapping, but further customisations can be added on top of this in Update()
                // by simulating the KeyDown event through calling OnKeyDown(Left) or OnKeyDown(Right).
                if (a.Data == Microsoft.Xna.Framework.Input.Keys.Left)
                {
                    optionSlider.Value = Math.Max(min, optionSlider.Value - 1);
                    snapAndSetValueIfChange();
                }
                else if (a.Data == Microsoft.Xna.Framework.Input.Keys.Right)
                {
                    optionSlider.Value = Math.Min(optionSlider.Value + 1, max);
                    snapAndSetValueIfChange();
                }
            };

            // Make sure that the second item stretches the remaining space if possible. This will
            // stretch as far as the widest sibling element, but not as far as the widest siblings
            // of the parent (like the heading/footer).
            optionContainer.Proportions.Add(new Proportion());
            optionContainer.AddChild(optionLabel);
            optionContainer.Proportions.Add(new Proportion { Type = ProportionType.Fill });
            optionContainer.AddChild(optionSlider);
            return optionContainer;
        }

        /// <summary>
        /// Create a boolean toggle as a menu item. The label is shown on the left side.
        /// </summary>
        /// <param name="fontSize">The font size for the text</param>
        /// <param name="label">The label description</param>
        /// <param name="initialValue">The initial value</param>
        /// <param name="yesText">The text to show when the value is true</param>
        /// <param name="noText">The text to show when the value is false</param>
        /// <param name="setter">The callback to actually set the option</param>
        /// <returns>The widget that you can add to MenuWidgets</returns>
        private HorizontalStackPanel CreateToggleOption(int fontSize, string label, bool initialValue, string yesText, string noText, Action<bool> setter)
        {
            // Create the horizontal layout, left is label, right is the slider
            HorizontalStackPanel optionContainer = new();
            Label optionLabel = new()
            {
                Text = label,
                Id = "option" + label,
                Font = BarlowFont.GetFont(fontSize),
                TextColor = new Color(255, 255, 255, 198)
            };

            Label optionValue = new()
            {
                Text = initialValue ? yesText : noText,
                Font = BarlowFont.GetFont(fontSize),
                // Align to the right, in the center vertically with respect to the text on the left
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Right,
                HorizontalAlignment = HorizontalAlignment.Right,
                // Arbitrary length and height, this might need to change based on resolution somehow
                MinWidth = 300,
                TextColor = new Color(255, 255, 255, 198),
                // We use the Tag to store (bool)s here.
                Tag = initialValue
            };

            // On mouse release on this element, invert the value stored in the Tag, update the
            // text, and call the setter.
            optionContainer.TouchUp += (s, a) =>
            {
                optionValue.Tag = !(bool)optionValue.Tag;
                optionValue.Text = (bool)optionValue.Tag ? yesText : noText;
                setter((bool)optionValue.Tag);
            };

            // Make sure that the second item stretches the remaining space if possible. This will
            // stretch as far as the widest sibling element, but not as far as the widest siblings
            // of the parent (like the heading/footer).
            optionContainer.Proportions.Add(new Proportion());
            optionContainer.AddChild(optionLabel);
            optionContainer.Proportions.Add(new Proportion { Type = ProportionType.Fill });
            optionContainer.AddChild(optionValue);
            return optionContainer;
        }

        /// <summary>
        /// Create a toggle for an Enum. The label is shown on the left side.
        /// </summary>
        /// <param name="fontSize">The font size for the text</param>
        /// <param name="label">The label description</param>
        /// <param name="initialValue">The initial value</param>
        /// <param name="setter">The callback to actually set the option</param>
        /// <returns>The widget that you can add to MenuWidgets</returns>
        private HorizontalStackPanel CreateMultipleOption<T>(int fontSize, string label, T initialValue, T[] possibleValues, Action<T> setter) where T : class
        {
            // Create the horizontal layout, left is label, right is the slider
            HorizontalStackPanel optionContainer = new();
            Label optionLabel = new()
            {
                Text = label,
                Id = "option" + label,
                Font = BarlowFont.GetFont(fontSize),
                TextColor = new Color(255, 255, 255, 198)
            };

            Label optionValue = new()
            {
                Text = initialValue.ToString(),
                Font = BarlowFont.GetFont(fontSize),
                // Align to the right, in the center vertically with respect to the text on the left
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Right,
                HorizontalAlignment = HorizontalAlignment.Right,
                TextColor = new Color(255, 255, 255, 198),
                // We use the Tag to store the Enum here.
                Tag = initialValue
            };

            Label optionDecrement = new()
            {
                Text = "< ",
                Font = BarlowFont.GetFont(fontSize),
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Right,
                HorizontalAlignment = HorizontalAlignment.Right,
                Visible = false
            };

            Label optionIncrement = new()
            {
                Text = " >",
                Font = BarlowFont.GetFont(fontSize),
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visible = false
            };

            // Helper function to change the toggle value by some delta, update the stored Tag and
            // Text, and call the setter function.
            void changeValue(int delta)
            {
                // Select the item in the possible value array at index (current index + delta) but
                // providing wrap-arounds for both negative and positive deltas as long as delta >
                // -count. We need to use object.Equals instead of == here so we get value equality
                // for records, and otherwise behaves identical to ==.
                int currentIndex = possibleValues.TakeWhile(v => !v.Equals((T)optionValue.Tag)).Count();
                optionValue.Tag = possibleValues[(currentIndex + delta + possibleValues.Length) % possibleValues.Length];
                optionValue.Text = ((T)optionValue.Tag).ToString();
                setter((T)optionValue.Tag);

                PlaySFX("Audio/UI/selection_confirm", 0.7f);
            }

            // Show decrement and increment buttons on mouse hover
            optionContainer.MouseEntered += (s, a) =>
            {
                optionDecrement.Visible = true;
                optionIncrement.Visible = true;
            };
            optionContainer.MouseLeft += (s, a) =>
            {
                optionDecrement.Visible = false;
                optionIncrement.Visible = false;
            };

            // Set up click handlers on decrement and increment widgets
            optionDecrement.TouchUp += (s, a) =>
            {
                changeValue(-1);
            };
            optionIncrement.TouchUp += (s, a) =>
            {
                changeValue(1);
            };

            // On keyboard down (this is only called if the widget has keyboard focus -- this is
            // ensured in the parent class where every change to HoverIndex also changes the focused widget)
            optionContainer.KeyDown += (s, a) =>
            {
                // By default we use the left right keyboard keys regardless of any input type or
                // key remapping, but further customisations can be added on top of this in Update()
                // by simulating the KeyDown event through calling OnKeyDown(Left) or OnKeyDown(Right).
                // todo: this is a bit janky, we ideally don't want to define any default keys and
                // just have those that are defined in Update() from the key mapping.
                if (a.Data == Microsoft.Xna.Framework.Input.Keys.Left)
                {
                    changeValue(-1);
                }
                else if (a.Data == Microsoft.Xna.Framework.Input.Keys.Right)
                {
                    changeValue(1);
                }
            };

            // Make sure that the second item stretches the remaining space if possible. This will
            // stretch as far as the widest sibling element, but not as far as the widest siblings
            // of the parent (like the heading/footer).
            optionContainer.Proportions.Add(new Proportion());
            optionContainer.AddChild(optionLabel);
            optionContainer.Proportions.Add(Proportion.Fill);
            optionContainer.AddChild(optionDecrement);
            optionContainer.AddChild(optionValue);
            optionContainer.AddChild(optionIncrement);
            return optionContainer;
        }

        /// <summary>
        /// A custom override of MoveHoverTo to prevent hover indices from moving if the user is
        /// pressing either left or right movement keys when we're focused on a slider. This is to
        /// prevent diagonal flicks to trigger a change in hover index when we just wanted to change
        /// some slider value -- in sliders, the horizontal movement takes priority and you need to
        /// really flick straight up or down to switch away.
        /// </summary>
        /// <param name="pos"></param>
        public override void MoveHoverTo(int pos)
        {
            Input input = GameServices.GetService<Input>();

            // Only try to require straight vertical flicking if we're on a slider that might need
            // horizontal flickery
            if (MainMenu.Widgets[HoverIndex] is not HorizontalStackPanel panel ||
                panel.Widgets[0] is not Label ||
                panel.Widgets[1] is not Slider)
            {
                base.MoveHoverTo(pos);
                return;
            }

            // If any of the left/right directions are flicked, cancel the vertical movement
            if (UserAction.MenuItemLeft.Pressed(input) || UserAction.Movement.FlickedLeft(input) ||
                UserAction.MenuItemLeft.Held(input) || UserAction.Movement.HeldLeft(input) ||
                UserAction.MenuItemRight.Pressed(input) || UserAction.Movement.FlickedRight(input) ||
                UserAction.MenuItemRight.Held(input) || UserAction.Movement.HeldRight(input))
            {
                return;
            }

            base.MoveHoverTo(pos);
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);

            // Do nothing if the screen doesn't have focus.
            if (!HasFocus) return;

            Input input = GameServices.GetService<Input>();

            // Back out of pause menu
            if (UserAction.Pause.Pressed(input) || UserAction.Back.Pressed(input))
            {
                ExitScreen(alsoUnloadContent: true);
            }

            // Make left/right movements be simulating a Left/Right arrow key press
            if (UserAction.MenuItemLeft.Pressed(input) || UserAction.Movement.FlickedLeft(input))
            {
                MainMenu.Widgets[HoverIndex].OnKeyDown(Microsoft.Xna.Framework.Input.Keys.Left);
            }
            else if (UserAction.MenuItemRight.Pressed(input) || UserAction.Movement.FlickedRight(input))
            {
                MainMenu.Widgets[HoverIndex].OnKeyDown(Microsoft.Xna.Framework.Input.Keys.Right);
            }

            // Changing the style upon hover for normal menu labels is done within the parent class.
            // But it ignores every widget in the MainMenu that is not a plain Label. In this for
            // loop, we do another pass but for option toggles, which we find heuristically by
            // filtering for HorizontalStackPanel widgets with a Label as the first element.
            for (int index = 0; index < MainMenu.Widgets.Count;  index++)
            {
                if (MainMenu.Widgets[index] is HorizontalStackPanel panel &&
                    panel.Widgets[0] is Label toggleLabel)
                {
                    if (index == HoverIndex)
                    {
                        // This item is currently hovered. For option toggles/sliders, we don't
                        // prepend a > to indicate it's click-able, we just change the colour of the
                        // label to pink and the slider/toggle
                        toggleLabel.TextColor = new Color(246, 101, 255);
                        if (panel.Widgets[1] is HorizontalSlider slider)
                        {
                            // The second item is a slider, change its background to pink
                            slider.Background = new SolidBrush(new Color(246, 101, 255));
                        }
                        else if (panel.Widgets.Count == 4 && panel.Widgets[2] is Label)
                        {
                            // The third item is a label (so it's a multi-toggle), change its text color
                            Label toggleValue = panel.Widgets[2] as Label;
                            toggleValue.TextColor = new Color(246, 101, 255);
                        }
                        else if (panel.Widgets[1] is Label)
                        {
                            // The second item is a label (so it's a text toggle), change its text colour
                            Label toggleValue = panel.Widgets[1] as Label;
                            toggleValue.TextColor = new Color(246, 101, 255);
                        }
                    }
                    else if (index != HoverIndex)
                    {
                        // This item is not hovered
                        toggleLabel.TextColor = new Color(255, 255, 255, 198);
                        if (panel.Widgets[1] is HorizontalSlider slider)
                        {
                            // The second item is a slider, change its background to the default
                            slider.Background = new SolidBrush(new Color(255, 255, 255, 198));
                        }
                        else if (panel.Widgets.Count == 4 && panel.Widgets[2] is Label)
                        {
                            // The third item is a label (so it's a multi-toggle), change its text color
                            Label toggleValue = panel.Widgets[2] as Label;
                            toggleValue.TextColor = new Color(255, 255, 255, 198);
                        }
                        else if (panel.Widgets[1] is Label)
                        {
                            // The second item is a label (so it's a text toggle), change its text colour
                            Label toggleValue = panel.Widgets[1] as Label;
                            toggleValue.TextColor = new Color(255, 255, 255, 198);
                        }
                    }
                }
            }

            Desktop.UpdateLayout();
        }

        public override void UI()
        {
            base.UI();
            ImGui.Separator();
            ImGui.Text($"IsFullScreen: {GameServices.GetService<GraphicsDeviceManager>().IsFullScreen}");
            ImGui.Text($"HardwareModeSwitch: {GameServices.GetService<GraphicsDeviceManager>().HardwareModeSwitch}");
            ImGui.Text($"Selected Resolution: {GameServices.GetService<UserSettings>().Resolution}");
            ImGui.Text($"Max GPU Supported Resolution: {GameServices.GetService<GraphicsDevice>().Adapter.SupportedDisplayModes.Last().Width}x{GameServices.GetService<GraphicsDevice>().Adapter.SupportedDisplayModes.Last().Height}");
            ImGui.Text($"GPU DisplayMode: {GameServices.GetService<GraphicsDevice>().DisplayMode.Width}x{GameServices.GetService<GraphicsDevice>().DisplayMode.Height}");
            ImGui.Text($"GPU CurrentDisplayMode: {GameServices.GetService<GraphicsDevice>().Adapter.CurrentDisplayMode.Width}x{GameServices.GetService<GraphicsDevice>().Adapter.CurrentDisplayMode.Height}");
            ImGui.Text($"GPU BackBuffer: {GameServices.GetService<GraphicsDevice>().PresentationParameters.BackBufferWidth}x{GameServices.GetService<GraphicsDevice>().PresentationParameters.BackBufferHeight}");
            ImGui.Text($"GPU Viewport: {GameServices.GetService<GraphicsDevice>().Viewport.Width}x{GameServices.GetService<GraphicsDevice>().Viewport.Height}");
            ImGui.Text($"Main RenderTarget: {ScreenManager.MainRenderTarget.Width}x{ScreenManager.MainRenderTarget.Height}");
            ImGui.TextWrapped($"GPU SupportedDisplayModes: {string.Join(", ", GameServices.GetService<GraphicsDevice>().Adapter.SupportedDisplayModes.Select(m => m.Width + "x" + m.Height))}");
        }
    }
}
