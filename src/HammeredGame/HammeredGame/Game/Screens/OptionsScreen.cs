using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Myra.Graphics2D.UI;
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

            HorizontalStackPanel optionsMusicVolume = CreateToggleOption(
                oneLineHeight,
                "Music Volume",
                (int)(MediaPlayer.Volume * 10f),
                0,
                10,
                (i) => i.ToString(),
                (i) =>
                {
                    // todo: write to setting file
                    MediaPlayer.Volume = i / 10f;
                });

            HorizontalStackPanel optionsSFXVolume = CreateToggleOption(
                oneLineHeight,
                "SFX Volume",
                (int)(SoundEffect.MasterVolume * 10f),
                0,
                10,
                (i) => i.ToString(),
                (i) =>
                {
                    // todo: write to settings file
                    SoundEffect.MasterVolume = i / 10f;
                });

            HorizontalStackPanel optionsFullScreen = CreateToggleOption(
                oneLineHeight,
                "Full Screen",
                GameServices.GetService<GraphicsDeviceManager>().IsFullScreen ? 1 : 0,
                min: 0,
                1,
                (i) => i == 1 ? "Yes" : "No",
                (i) =>
                {
                    GameServices.GetService<GraphicsDeviceManager>().IsFullScreen = i == 1;
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
        /// <param name="label">The label description</param>
        /// <param name="initialValue">The initial value</param>
        /// <param name="min">Minimum allowed value inclusive</param>
        /// <param name="max">Maximum allowed value inclusive</param>
        /// <param name="setter">The callback to actually set the option</param>
        /// <returns>The widget that you can add to MenuWidgets</returns>
        private HorizontalStackPanel CreateToggleOption(int fontSize, string label, int initialValue, int min, int max, Func<int, string> displayer, Action<int> setter)
        {
            // Create the horizontal layout, left is label, right is the value
            HorizontalStackPanel optionContainer = new()
            {
                //Width = 500
            };
            Label optionLabel = new()
            {
                Text = label,
                Id = "option" + label,
                Font = BarlowFont.GetFont(fontSize)
            };
            Label optionToggle = new()
            {
                // Right align the value as far as we can in the parent container by stretching
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Right,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = displayer(initialValue),
                Font = BarlowFont.GetFont(fontSize)
            };
            // Store the actual non-display machine readable value within the Tag property, which is
            // free for us to use
            optionToggle.Tag = initialValue;

            // On click, we increment the value
            optionContainer.TouchUp += (s, a) =>
            {
                // Use the machine readable Tag (instead of parsing the Text inside the Label) to
                // increment the value by one within the [min, max] range, wrapping around if necessary.
                optionToggle.Tag = ((int)optionToggle.Tag + 1 - min) % (max - min + 1) + min;

                // Update the visual text and call the setter function
                optionToggle.Text = displayer((int)optionToggle.Tag);
                setter((int)optionToggle.Tag);
            };

            // On keyboard down (this is only called if the widget has keyboard focus -- this is
            // ensured in the parent class where every change to HoverIndex also changes the focused widget)
            optionContainer.KeyDown += (s, a) =>
            {
                // By default we use the left right keyboard keys regardless of any input type or
                // key remapping, but further customisations can be added on top of this in Update()
                // by simulating the KeyDown event through calling OnKeyDown(Left) or OnKeyDown(Right).
                if (a.Data == Microsoft.Xna.Framework.Input.Keys.Left)
                {
                    // Same procedure as the click event, although since it is a decrement, we add
                    // (max - min + 1) to make sure we don't go into negatives.
                    optionToggle.Tag = ((int)optionToggle.Tag - 1 - min + (max - min + 1)) % (max - min + 1) + min;
                    optionToggle.Text = displayer((int)optionToggle.Tag);
                    setter((int)optionToggle.Tag);
                }
                else if (a.Data == Microsoft.Xna.Framework.Input.Keys.Right)
                {
                    optionToggle.Tag = ((int)optionToggle.Tag + 1 - min + (max - min + 1)) % (max - min + 1) + min;
                    optionToggle.Text = displayer((int)optionToggle.Tag);
                    setter((int)optionToggle.Tag);
                }
            };

            // Make sure that the second item stretches the remaining space if possible. This will
            // stretch as far as the widest sibling element, but not as far as the widest siblings
            // of the parent (like the heading/footer).
            optionContainer.Proportions.Add(new Proportion());
            optionContainer.AddChild(optionLabel);
            optionContainer.Proportions.Add(new Proportion { Type = ProportionType.Fill });
            optionContainer.AddChild(optionToggle);
            return optionContainer;
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);

            // Do nothing if the screen doesn't have focus.
            if (!HasFocus) return;

            Input input = GameServices.GetService<Input>();

            // Back out of pause menu without unloading content
            if (UserAction.Pause.Pressed(input) || UserAction.Back.Pressed(input))
            {
                ExitScreen(alsoUnloadContent: true);
            }

            // Normal label hovering is updated within the parent class. In this for loop, we do a
            // similar thing but for option toggles, which we find heuristically by filtering for
            // HorizontalStackPanel widgets with a Label as the first and second elements.
            for (int index = 0; index < MainMenu.Widgets.Count; index++)
            {
                Widget menuItem = MainMenu.Widgets[index];

                if (menuItem is HorizontalStackPanel toggle &&
                    toggle.Widgets[0] is Label toggleLabel &&
                    toggle.Widgets[1] is Label)
                {
                    if (index == HoverIndex && toggleLabel.Text[0] != '>')
                    {
                        // This is an item that's hovered but wasn't hovered before
                        PlaySFX("Audio/UI/selection_change", 0.7f);
                        toggleLabel.Text = "> " + toggleLabel.Text;
                        toggleLabel.TextColor = new Color(246, 101, 255);
                    }
                    else if (index != HoverIndex && toggleLabel.Text[0] == '>')
                    {
                        // This is an item that was hovered but now isn't
                        toggleLabel.Text = toggleLabel.Text.Substring(2);
                        toggleLabel.TextColor = new Color(255, 255, 255, 198);
                    }
                }
            }
        }
    }
}
