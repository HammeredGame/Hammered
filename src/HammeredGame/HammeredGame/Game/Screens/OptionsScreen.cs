using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Utility;
using System;
using System.Collections.Generic;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// The pause screen is a menu screen that shows Paused stuff. In general, whenever
    /// ExitScreen() is called within PauseScreen, it shouldn't unload its contents. This
    /// is because GameScreen reuses the same pause screen and removes/adds it, to save
    /// on expensive content-loading at runtime.
    /// </summary>
    internal class OptionsScreen : AbstractMenuScreen
    {
        public override void LoadMenuWidgets()
        {
            base.LoadMenuWidgets();

            MenuHeaderText = "OPTIONS";

            // We use a smaller font size here, 5% of the screen height
            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 20;

            HorizontalStackPanel optionsMusicVolume = CreateSliderOption(
                oneLineHeight,
                "Music Volume",
                (int)(MediaPlayer.Volume * 10f),
                0,
                10,
                (i) =>
                {
                    // todo: write to setting file
                    MediaPlayer.Volume = i / 10f;
                });

            HorizontalStackPanel optionsSFXVolume = CreateSliderOption(
                oneLineHeight,
                "SFX Volume",
                (int)(SoundEffect.MasterVolume * 10f),
                0,
                10,
                (i) =>
                {
                    // todo: write to settings file
                    SoundEffect.MasterVolume = i / 10f;
                });

            HorizontalStackPanel optionsFullScreen = CreateToggleOption(
                oneLineHeight,
                "Full Screen",
                GameServices.GetService<GraphicsDeviceManager>().IsFullScreen,
                "Yes",
                "No",
                (i) =>
                {
                    GameServices.GetService<GraphicsDeviceManager>().IsFullScreen = i;
                    GameServices.GetService<GraphicsDeviceManager>().ApplyChanges();
                });

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

            MenuWidgets = new List<Widget>() { optionsMusicVolume, optionsSFXVolume, optionsFullScreen, menuItemBack };
        }

        /// <summary>
        /// Create a multi-item toggle as a menu item. The label is shown on the left side. The
        /// value will be an integer between min and max inclusive.
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
                        else if (panel.Widgets[1] is Label toggleValue)
                        {
                            // The second item is a label (so it's a text toggle), change its text colour
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
                        else if (panel.Widgets[1] is Label toggleValue)
                        {
                            // The second item is a label (so it's a text toggle), change its text colour
                            toggleValue.TextColor = new Color(255, 255, 255, 198);
                        }
                    }
                }
            }

            Desktop.UpdateLayout();
        }
    }
}
