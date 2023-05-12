using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Pleasing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammeredGame.Game.Screens
{
    internal class AbstractMenuScreen : Screen
    {
        protected Desktop Desktop;

        private Texture2D whiteRectangle;

        private Color menuBackgroundColor = new(75, 43, 58);

        // The maximum (default) width of the menu to animate to. This is the minimum width that
        // safely encompasses all the inner menu text.
        private int menuWidthMax;

        private int trianglwWidthMax = 300;

        public float BackgroundOverlayOpacity = 0f;

        // These values are going to be animated. It represents the menu width going from 0 to
        // menuWidthMax, the width of the triangle next to it going from 0 to triangleWidthMax, and
        // the opacity of the background darkening, going from 0 to BackgroundOverlayOpacity. The
        // tween library requires the properties to be accessible from the target class (which in
        // our case are the inherited menu classes), which means these have to be marked no less
        // accessible than protected.
        protected float MenuWidthCurrent;

        protected float MenuTriangleWidth;

        protected float MenuOverlayOpacityCurrent;

        // Store the game time when the last input for moving up/down was pressed. This is used to
        // limit the scrolling speed when an input is held for long.
        private TimeSpan lastContinuousInput = TimeSpan.Zero;

        protected TweenTimeline TransitionAnimationTimeline;

        public AbstractMenuScreen()
        {
            IsPartial = true;
        }

        protected string MenuHeaderText = "";

        // The items to display in the menu under the heading, together with the currently hovered index.
        protected List<Widget> MenuWidgets;

        protected int HoverIndex = 0;

        protected VerticalStackPanel MainMenu;
        private VerticalStackPanel menuContainer;

        private Image okPromptImage;

        private AudioEmitter audioEmitter = new();

        // Fonts: Skranji for stylized heading, Barlow for legible body.
        protected FontSystem SkranjiFont = new();

        protected FontSystem BarlowFont = new();

        /// <summary>
        /// Convenience function to play a sound effect from the default position (is it world center?)
        /// </summary>
        /// <param name="sfxName"></param>
        /// <param name="volume"></param>
        protected void PlaySFX(string sfxName, float volume)
        {
            GameServices.GetService<AudioManager>().Play3DSound(sfxName, false, audioEmitter, volume);
        }

        /// <summary>
        /// Load the fonts and the basic content for all menu screens. This is sealed to prevent
        /// inherited classes from overriding it -- they should override LoadMenuWidgets().
        /// </summary>
        public override sealed void LoadContent()
        {
            base.LoadContent();

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            byte[] skranjiTtfData = System.IO.File.ReadAllBytes("Content/Skranji-Regular.ttf");
            SkranjiFont.AddFont(skranjiTtfData);

            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            BarlowFont.AddFont(barlowTtfData);

            LoadMenuWidgets();

            SetupMenu();
        }

        /// <summary>
        /// Perform any creation of menu widgets here and add them to MenuItems. Also load any
        /// required assets for the particular menu screen. Can be called more than once, when the
        /// game resolution changes and the UI needs to be reconstructed.
        /// </summary>
        public virtual void LoadMenuWidgets()
        { }

        /// <summary>
        /// Using the widgets added to MenuItems in LoadMenuWidgets, construct the main menu with a
        /// heading and a footer. Can be called more than once, when the game resolution changes and
        /// the UI needs to be reconstructed.
        /// </summary>
        private void SetupMenu()
        {
            var oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            // Set up the header with display font
            var header = new Label
            {
                Text = MenuHeaderText,
                TextColor = Color.White,
                Font = SkranjiFont.GetFont(oneLineHeight * 1.5f),
                Margin = new Thickness(100, oneLineHeight, 0, 0),
            };

            // Set up the main menu body which stretches vertically to fill the content between the
            // header and footer (this is specified later).
            MainMenu = new VerticalStackPanel()
            {
                // VerticalStackPanel has a default HorizonalAlignment of Stretch, making it stretch
                // the whole viewport width. We need to set it to Left so it shrinks down on the
                // left side to fit its contents.
                HorizontalAlignment = HorizontalAlignment.Left,
                // The menu itself should be transparent (00 in alpha)
                Background = new SolidBrush("#00000000"),
                Border = new SolidBrush("#00000000"),
                Id = "_mainMenu",
                Margin = new Thickness(100, (int)(oneLineHeight * 0.5f))
            };

            // Add each of the menu widgets to the main menu body.
            for (int i = 0; i < MenuWidgets.Count; i++)
            {
                var menuItem = MenuWidgets[i];

                // On mouse enter, we set the hover index to this index. To capture the value of i
                // at this point in the loop for use in the delegate function (lambda), we need to
                // explicitly copy it to a different variable. If we use i as-is, it will always
                // contain the last index value because the lambda is evaluated at a later point.
                var capturedIndex = i;
                menuItem.MouseEntered += (s, a) =>
                {
                    MoveHoverTo(capturedIndex);
                };

                // We play the sound effect on mouse click press
                menuItem.TouchDown += (s, a) => PlaySFX("Audio/UI/selection_confirm", 1);

                MainMenu.Widgets.Add(menuItem);
            }

            // Create a horizontal panel that's displayed at the very bottom of the screen for an
            // input prompt saying "Press this for OK"
            float okPromptHeight = oneLineHeight * 0.4f;
            okPromptImage = new Image
            {
                // Default with an image here, but it will be updated on every frame to account for
                // the currently active input type
                Renderable = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Confirm)[0],
                // Make sure both Height and Width are set to avoid weird stretching - Myra doesn't
                // respect an image's original aspect ratio.
                Height = (int)okPromptHeight,
                Width = (int)okPromptHeight
            };

            // Set up the footer
            var footer = new HorizontalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                // Align the left edge with the same padding as menu above, and have a not-so-big
                // padding on the bottom side
                Margin = new Thickness(100, 0, 0, (int)okPromptHeight * 2),
                // Any higher than 0.4 feels like it steals too much attention from the menu
                Opacity = 0.4f,
                Widgets =
                {
                    // The labels use the same font size as the image so that it looks inline
                    new Label
                    {
                        Text = "Press",
                        TextColor = Color.White,
                        Font = SkranjiFont.GetFont(okPromptHeight),
                    },
                    okPromptImage,
                    new Label
                    {
                        Text = "to confirm",
                        TextColor = Color.White,
                        Font = SkranjiFont.GetFont(okPromptHeight),
                    }
                }
            };

            // This is the main item stack for the menu, containing the header, the item list, and
            // the footer.
            menuContainer = new VerticalStackPanel
            {
                // It'll span the whole height
                Height = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight,
                // Background being purple
                Background = new SolidBrush(menuBackgroundColor),
                // If anything inside is larger than the bounds (like the menu contents while we're
                // transitioning), clip and don't show them outside bounds
                ClipToBounds = true
            };

            // Set up proportions so that the second item (the menu item list) will stretch and fill
            // available space, while others will use the least amount of space it can.
            menuContainer.Proportions.Add(new Proportion());
            menuContainer.Proportions.Add(new Proportion { Type = ProportionType.Fill });

            menuContainer.Widgets.Add(header);
            menuContainer.Widgets.Add(MainMenu);
            menuContainer.Widgets.Add(footer);

            // Set up the overall UI that has the menu as its root
            Desktop = new()
            {
                Root = menuContainer
            };

            // Set the currently focused widget (nothing visual, but for receiving the KeyDown
            // events) to the one with the hover index.
            Desktop.FocusedKeyboardWidget = MenuWidgets[HoverIndex];

            // Select first non-disabled menu item. In the case where everything is disabled and the
            // first expression returns MenuItems.Count, we want to avoid an index-out-of-bounds, so
            // take the minimum with the largest allowed index
            MoveHoverTo(Math.Min(MenuWidgets.TakeWhile(i => !i.Enabled).Count(), MenuWidgets.Count - 1));

            // Whenever a widget gets new keyboard focus, we should play a sound
            Desktop.WidgetGotKeyboardFocus += (s, a) =>
            {
                PlaySFX("Audio/UI/selection_change", 0.7f);
            };

            // Pre-calculate layout once, so we know the width of the text to draw the background for
            Desktop.UpdateLayout();

            // Store the max width of the menu (that the transition process will tween to)
            menuWidthMax = Math.Max(header.Bounds.Width, MainMenu.Bounds.Width);
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            // Any assets created or loaded without the content manager should be disposed of.
            whiteRectangle.Dispose();
        }

        /// <summary>
        /// A custom override handling resolution changes, which reconstructs the menu UI based on
        /// the new resolution. An alternative implementation (less expensive, but more
        /// implementation effort) would be to change all fonts in the existing UI to a new size.
        /// But UI widgets can be nested, text might have font size coefficients (e.g. 1.5 *
        /// oneLineHeight) so it's easier to just re-create the whole UI, replace the Desktop, and
        /// register new event handlers.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public override void SetResolution(int width, int height)
        {
            // Re-build all menu widgets, they probably depend on resolution for font sizes
            LoadMenuWidgets();
            SetupMenu();

            // Set the current menu width to be the newly calculated width. This is something that
            // would normally be done during the inward transition, but since resolution changes
            // don't trigger one, we do it manually.
            MenuWidthCurrent = menuWidthMax;
            menuContainer.Width = menuWidthMax;
            Desktop.UpdateLayout();
        }

        /// <summary>
        /// The incoming transition for menus is just to slide in from the side.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="firstFrame"></param>
        /// <returns></returns>
        public override bool UpdateTransitionIn(GameTime gameTime, bool firstFrame)
        {
            if (firstFrame)
            {
                // First frame of enter transition: we'll reset the menu and triangle widths down to
                // zero, and set up an animation timeline to tween them up to the pre-calculated
                // menu width. We use a floating point proxy for the current menu width instead of
                // tweening the integer property itself, so that we get nice smoothing and rounding.
                menuContainer.Width = 0;
                MenuTriangleWidth = 0f;
                MenuWidthCurrent = 0f;
                MenuOverlayOpacityCurrent = 0f;
                Desktop.UpdateLayout();

                TransitionAnimationTimeline = Tweening.NewTimeline();
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuWidthCurrent))
                    .AddFrame(200, menuWidthMax, Easing.Exponential.Out);
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuTriangleWidth))
                    .AddFrame(200, trianglwWidthMax, Easing.Exponential.Out);
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuOverlayOpacityCurrent))
                    .AddFrame(200, BackgroundOverlayOpacity, Easing.Exponential.Out);

                PlaySFX("Audio/lohi_whoosh", 1f);
                return false;
            }
            // Set the width on the Myra menu UI because of the above-mentioned int-float proxy.
            menuContainer.Width = (int)MenuWidthCurrent;
            Desktop.UpdateLayout();

            // Return true and indicate the transition is finished only after animation is done
            return TransitionAnimationTimeline.State == TweenState.Stopped;
        }

        /// <summary>
        /// The outgoing transition for menus is just to slide out to the side.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="firstFrame"></param>
        /// <returns></returns>
        public override bool UpdateTransitionOut(GameTime gameTime, bool firstFrame)
        {
            if (firstFrame)
            {
                // First frame of exit transition: we'll set up an animation timeline for tweening
                // the triangle width and the menu width down to zero. We need to tween the triangle
                // width too, because otherwise the transition will end with a triangle visible next
                // to the zero-width menu, and it's very abrupt to just disappear.
                TransitionAnimationTimeline = Tweening.NewTimeline();
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuWidthCurrent))
                    .AddFrame(200, 0, Easing.Exponential.Out);
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuTriangleWidth))
                    .AddFrame(200, 0f, Easing.Exponential.Out);
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuOverlayOpacityCurrent))
                    .AddFrame(200, 0f, Easing.Exponential.Out);

                PlaySFX("Audio/lohi_whoosh", 1f);
                return false;
            }
            // Set the width on the Myra menu UI because we use a int-float proxy for smoothing
            // tweens (see the explanation in the inward transition).
            menuContainer.Width = (int)MenuWidthCurrent;
            Desktop.UpdateLayout();

            // Return true and indicate the transition is finished only after animation is done
            return TransitionAnimationTimeline.State == TweenState.Stopped;
        }

        /// <summary>
        /// Move the hovered pointer to an absolute index. This function will wrap around at the top
        /// and bottom as long as the supplied <paramref name="pos"/> is within the range of
        /// [-WidgetCount, inf]. This function should be used instead of directly manipulating
        /// HoverIndex, since we also set Desktop.FocusedKeyboardWidget in this function which you
        /// might forget to do otherwise.
        /// </summary>
        /// <param name="pos"></param>
        public virtual void MoveHoverTo(int pos)
        {
            HoverIndex = (pos + MainMenu.Widgets.Count) % MainMenu.Widgets.Count;

            // Set the currently focused widget (nothing visual, but for receiving the KeyDown
            // events, required for e.g. slider control via keys/buttons) to the one with the hover index.
            Desktop.FocusedKeyboardWidget = MainMenu.Widgets[HoverIndex];
        }

        /// <summary>
        /// Move the hovered pointer by some delta. Positive delta is downwards, negative is
        /// upwards. This function will properly wrap around at the top and bottom, and also skip
        /// any disabled items as long as there is at least one enabled item.
        /// </summary>
        /// <param name="delta"></param>
        protected virtual void MoveHoverBy(int delta)
        {
            // Counter to make sure we only check once for each item and don't perform an endless
            // loop looking for an enabled item within an all-disabled list
            int i = 0;
            int originalHoverIndex = HoverIndex;

            // Modify the hover index by the delta
            do
            {
                MoveHoverTo(originalHoverIndex + delta + Math.Sign(delta) * i);
                i++;
            }
            // As long as the new menu item isn't disabled, and we haven't looped back around. We
            // allow one whole loop before stopping (<= instead of <) to make sure we don't end up
            // setting HoverIndex to the one before the current position.
            while (!MainMenu.Widgets[HoverIndex].Enabled && i <= MainMenu.Widgets.Count);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Do nothing if the screen doesn't have focus yet (e.g. mid-transition or hidden)
            if (!HasFocus) return;

            Input input = GameServices.GetService<Input>();

            // Register Myra inputs only if the main game is active. Ideally we'd do this
            // conditional in HammeredGame's Update method (where we do the same for the game
            // Input), but since it has to be done for each UI Desktop, we do it here and use the
            // GameServices container to grab the main game instance.
            if (GameServices.GetService<HammeredGame>().IsActive)
            {
                // Update the UI based on input, handling any handlers
                Desktop.UpdateInput();
            }

            // Allow selection with keyboard or controller instead of just mouse. We simulate the
            // mouse click down and up events.
            if (UserAction.Confirm.Pressed(input) || UserAction.Interact.Pressed(input))
            {
                MainMenu.Widgets[HoverIndex].OnTouchDown();
                MainMenu.Widgets[HoverIndex].OnTouchUp();
            }

            // If the keybind for menu-item-up/down is pressed once, shift the index. If it is held,
            // then we want to slowly go through each item, so we use a cooldown timer that is reset
            // whenever any of the actions are handled.
            TimeSpan scrollCooldown = TimeSpan.FromMilliseconds(500);
            if (UserAction.MenuItemUp.Pressed(input) || UserAction.Movement.FlickedUp(input))
            {
                MoveHoverBy(-1);
                lastContinuousInput = gameTime.TotalGameTime;
            }
            else if ((UserAction.MenuItemUp.Held(input) || UserAction.Movement.HeldUp(input)) &&
                gameTime.TotalGameTime > lastContinuousInput + scrollCooldown)
            {
                MoveHoverBy(-1);
                lastContinuousInput = gameTime.TotalGameTime;
            }
            else if (UserAction.MenuItemDown.Pressed(input) || UserAction.Movement.FlickedDown(input))
            {
                MoveHoverBy(1);
                lastContinuousInput = gameTime.TotalGameTime;
            }
            else if ((UserAction.MenuItemDown.Held(input) ||  UserAction.Movement.HeldDown(input)) &&
                gameTime.TotalGameTime > lastContinuousInput + scrollCooldown)
            {
                MoveHoverBy(1);
                lastContinuousInput = gameTime.TotalGameTime;
            }

            // Change text color and show a ">" cursor on the currently active item
            for (int index = 0; index < MainMenu.Widgets.Count; index++)
            {
                Widget menuItem = MainMenu.Widgets[index];

                if (menuItem is Label label)
                {
                    if (index == HoverIndex && label.Text[0] != '>')
                    {
                        // This is an item that's hovered but wasn't hovered before
                        label.Text = "> " + label.Text;
                        label.TextColor = new Color(246, 101, 255);
                        label.Left = (-1) * (int)label.Font.MeasureString("> ").X;
                    }
                    else if (index != HoverIndex && label.Text[0] == '>')
                    {
                        // This is an item that was hovered but now isn't
                        label.Text = label.Text.Substring(2);
                        label.TextColor = new Color(255, 255, 255, 198);
                        label.Left = 0;
                    }
                }
            }

            // Update the OK input prompt based on the currently active input type - this is safe to
            // perform synchronously since it will only query loaded images and will not cause file IO.
            okPromptImage.Renderable = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Confirm)[0];

            // Update the UI layout based on any changes to it above
            Desktop.UpdateLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Draw a dark overlay for the whole screen covering the background
            GameServices.GetService<SpriteBatch>().Begin();
            GameServices.GetService<SpriteBatch>().Draw(
                whiteRectangle,
                new Rectangle(0, 0, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height),
                null,
                new Color(0, 0, 0, MenuOverlayOpacityCurrent));
            GameServices.GetService<SpriteBatch>().End();

            // Draw a triangle beside where the menu would be drawn, by manually specifying the
            // three coloured vertices, with 0,0 being the top left corner.
            VertexPositionColor[] vertices = new VertexPositionColor[6];
            // Bottom
            vertices[0] = new VertexPositionColor(
                new Vector3(menuContainer.Width ?? 0, ScreenManager.GraphicsDevice.Viewport.Height, 0),
                menuBackgroundColor);
            // Top left
            vertices[1] = new VertexPositionColor(
                new Vector3(menuContainer.Width ?? 0, 0, 0),
                menuBackgroundColor);
            // Top right
            vertices[2] = new VertexPositionColor(
                new Vector3((menuContainer.Width ?? 0) + MenuTriangleWidth, 0, 0),
                menuBackgroundColor);

            // Set up a basic effect and an orthographic projection to draw the previous triangle
            // vertices onto the screen. Maybe there's a simpler way to draw these primitives?
            BasicEffect basicEffect = new(ScreenManager.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
                0,
                ScreenManager.GraphicsDevice.Viewport.Width,
                ScreenManager.GraphicsDevice.Viewport.Height,
                0,
                0,
                1);

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                ScreenManager.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 1);
            }

            // Render the UI
            Desktop.RenderVisual();
        }
    }
}
