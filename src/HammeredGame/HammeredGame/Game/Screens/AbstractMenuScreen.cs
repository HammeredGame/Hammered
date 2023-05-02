using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        // The maximum (default) width of the menu to animate to. This is the minimum width that
        // safely encompasses all the inner menu text.
        private int menuWidthMax;

        // These values are going to be animated. The tween library requires the properties to be
        // accessible from the target class (which in our case are the inherited menu classes),
        // which means these have to be marked no less accessible than protected.
        protected float MenuWidthCurrent;

        protected float MenuTriangleWidth;

        // Store the time since the last input for moving up or down in the menu
        private TimeSpan lastContinuousInput = TimeSpan.Zero;

        protected TweenTimeline TransitionAnimationTimeline;

        public AbstractMenuScreen()
        {
            IsPartial = true;
        }

        protected string MenuHeaderText = "";

        protected List<MenuItem> MenuItems;
        private VerticalMenu mainMenu;
        private VerticalStackPanel menuContainer;

        private Image okPromptImage;

        public override void LoadContent()
        {
            base.LoadContent();

            // todo: handle viewport resize by checking if .Height changed in Update()
            int oneLineHeight = ScreenManager.GraphicsDevice.Viewport.Height / 10;

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            byte[] skranjiTtfData = System.IO.File.ReadAllBytes("Content/Skranji-Regular.ttf");
            FontSystem skranjiFontSystem = new FontSystem();
            skranjiFontSystem.AddFont(skranjiTtfData);

            byte[] barlowTtfData = System.IO.File.ReadAllBytes("Content/Barlow-Medium.ttf");
            FontSystem barlowFontSystem = new FontSystem();
            barlowFontSystem.AddFont(barlowTtfData);

            var label1 = new Label
            {
                Text = MenuHeaderText,
                TextColor = Color.White,
                Font = skranjiFontSystem.GetFont(oneLineHeight * 1.5f),
                Margin = new Thickness(100, oneLineHeight, 0, 0),
            };

            mainMenu = new VerticalMenu()
            {
                LabelColor = new Color(255, 255, 255, 198),
                SelectionHoverBackground = new SolidBrush("#00000000"),
                SelectionBackground = new SolidBrush("#00000000"),
                LabelHorizontalAlignment = HorizontalAlignment.Left,
                // We mark it so that the HoverIndex (the selection) will never be null, i.e. there
                // is always going to be a selection even from the get-go. Unfortunately, this flag
                // is not implemented correctly in Myra and HoverIndex will become null when we
                // click on a menu item, regardless of the value here. This means that when you
                // click on a menu item and the menu hides, and you show the menu again, there is
                // nothing hovered. That's a problem, but fortunately it's not a big one because if
                // we use Menu.MoveHover() when we need to move indices (instead of manually
                // changing Menu.HoverIndex), Myra will properly re-hover items even if there was
                // nothing hovered.
                HoverIndexCanBeNull = false,
                Background = new SolidBrush("#00000000"),
                Border = new SolidBrush("#00000000"),
                Id = "_mainMenu",
                Margin = new Thickness(100, (int)(oneLineHeight * 0.5f)),
                LabelFont = barlowFontSystem.GetFont(oneLineHeight),
            };
            foreach (MenuItem menuItem in MenuItems)
            {
                // MenuItem.Click() resets the hover index to null despite HoverIndexCanBeNull being
                // false (due to a buggy Myra implementation). Since the .Selected event fires after
                // this, we can work around it by resetting the HoverIndex to the clicked index in a
                // click handler.
                menuItem.Selected += (s, a) =>
                {
                    mainMenu.HoverIndex = (s as MenuItem).Index;
                };

                mainMenu.Items.Add(menuItem);
            }

            // Select first non-disabled menu item. In the case where everything is disabled and the
            // first expression returns MenuItems.Count, we want to avoid an index-out-of-bounds, so
            // take the minimum with the largest allowed index
            mainMenu.HoverIndex = Math.Min(MenuItems.TakeWhile(i => !i.Enabled).Count(), MenuItems.Count - 1);

            // Create a horizontal panel that's displayed at the very bottom of the screen for an
            // input prompt saying "Press this for OK"
            float okPromptHeight = oneLineHeight * 0.4f;
            okPromptImage = new Image
            {
                // Default with an image here, but it will be updated on every frame to account for
                // the currently active input type
                Renderable = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Confirm)[0],
                // Make sure both Height and Width are set to avoid weird stretching
                Height = (int)okPromptHeight,
                Width = (int)okPromptHeight
            };
            var controlPrompts = new HorizontalStackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                // Align the left edge with the same padding as menu above, and have a medium
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
                        Font = skranjiFontSystem.GetFont(okPromptHeight),
                    },
                    okPromptImage,
                    new Label
                    {
                        Text = "to confirm",
                        TextColor = Color.White,
                        Font = skranjiFontSystem.GetFont(okPromptHeight),
                    }
                }
            };

            // This is the main item stack for the menu, containing the heading, the item list, and
            // some other things at the bottom.
            menuContainer = new VerticalStackPanel
            {
                // It'll span the whole height
                Height = ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight,
                // Background being purple
                Background = new SolidBrush(new Color(75, 43, 58)),
                // If anything inside is larger than the bounds (like text while transitioning),
                // clip and don't show them outside bounds
                ClipToBounds = true
            };
            // Set up proportions so that the second item (the menu item list) will stretch and fill
            // available space, while others will use the least amount of space it can.
            menuContainer.Proportions.Add(new Proportion());
            menuContainer.Proportions.Add(new Proportion { Type = ProportionType.Fill });
            menuContainer.Widgets.Add(label1);
            menuContainer.Widgets.Add(mainMenu);
            menuContainer.Widgets.Add(controlPrompts);

            Desktop = new()
            {
                Root = menuContainer
            };

            // Make main menu permanently hold keyboard focus as long as it's the active screen, by
            // canceling the lose-keyboard-focus event when necessary. This doesn't seem to be
            // implemented in Myra (c.f. HoverIndexCanBeNull above), but it's safer than nothing.
            Desktop.WidgetLosingKeyboardFocus += (s, a) =>
            {
                a.Cancel = HasFocus;
            };

            // Pre-calculate layout once, so we know the width of the text to draw the background for
            Desktop.UpdateLayout();

            // Store the max width of the menu (that the transition process will tween to)
            menuWidthMax = menuContainer.Widgets[1].Bounds.Width;
            menuContainer.Width = 0;
            MenuWidthCurrent = 0f;
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            // Any assets created or loaded without the content manager should be disposed of.
            whiteRectangle.Dispose();
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
                // menu width.
                MenuTriangleWidth = 0f;
                MenuWidthCurrent = 0f;
                TransitionAnimationTimeline = Tweening.NewTimeline();
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuWidthCurrent))
                    .AddFrame(200, menuWidthMax, Easing.Exponential.Out);
                TransitionAnimationTimeline.AddFloat(this, nameof(MenuTriangleWidth))
                    .AddFrame(200, 300f, Easing.Exponential.Out);
                return false;
            }
            // Set the width on the Myra menu UI.
            menuContainer.Width = (int)MenuWidthCurrent;

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
                return false;
            }
            // Set the width on the Myra menu UI.
            menuContainer.Width = (int)MenuWidthCurrent;

            // Return true and indicate the transition is finished only after animation is done
            return TransitionAnimationTimeline.State == TweenState.Stopped;
        }

        public override void Update(GameTime gameTime)
        {
            // Update screen state and HasFocus so we can use it
            base.Update(gameTime);

            // Do nothing if the screen doesn't have focus
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

            // Allow selection with keyboard or controller instead of just mouse
            if (UserAction.Confirm.Pressed(input) || UserAction.Interact.Pressed(input))
            {
                mainMenu.OnKeyDown(Keys.Enter);
            }

            // If the keybind for menu-item-up/down is pressed once, shift the index. If it is held,
            // then we want to slowly go through each item, so we use a cooldown timer that is reset
            // whenever any of the actions are handled.
            TimeSpan scrollCooldown = TimeSpan.FromMilliseconds(500);
            if (UserAction.MenuItemUp.Pressed(input))
            {
                // Make sure we use MoveHover and not manually add or remove one from
                // Menu.HoverIndex, because the latter will not work if HoverIndex is null (which
                // can happen after an item is clicked). MoveHover() accounts for this case automatically.
                mainMenu.MoveHover(-1);
                lastContinuousInput = gameTime.TotalGameTime;
            }
            else if (UserAction.MenuItemUp.Held(input) && gameTime.TotalGameTime > lastContinuousInput + scrollCooldown)
            {
                mainMenu.MoveHover(-1);
                lastContinuousInput = gameTime.TotalGameTime;
            }
            else if (UserAction.MenuItemDown.Pressed(input))
            {
                mainMenu.MoveHover(1);
                lastContinuousInput = gameTime.TotalGameTime;
            }
            else if (UserAction.MenuItemDown.Held(input) && gameTime.TotalGameTime > lastContinuousInput + scrollCooldown)
            {
                mainMenu.MoveHover(1);
                lastContinuousInput = gameTime.TotalGameTime;
            }

            // Change text color and show a ">" cursor on the currently active item
            foreach (MenuItem menuItem in mainMenu.Items)
            {
                if (menuItem.Index == mainMenu.HoverIndex && menuItem.Text[0] != '>')
                {
                    menuItem.Text = "> " + menuItem.Text;
                    menuItem.Color = new Color(246, 101, 255);
                }
                else if (menuItem.Index != mainMenu.HoverIndex && menuItem.Text[0] == '>')
                {
                    menuItem.Text = menuItem.Text.Substring(2);
                    menuItem.Color = new Color(255, 255, 255, 198);
                }
            }

            // Update the OK input prompt based on the currently active input type - this is safe to
            // perform synchronously since it will only query loaded images and will not cause file IO.
            okPromptImage.Renderable = GameServices.GetService<Input>().Prompts.GetImagesForAction(UserAction.Interact)[0];

            // Update the UI layout based on any changes to it above
            Desktop.UpdateLayout();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Nice looking purple color for the background
            Color bgColor = new(75, 43, 58);

            // Draw everything in a batch for speed
            GameServices.GetService<SpriteBatch>().Begin();

            // Draw a triangle beside where the menu would be drawn. Since Myra uses integer units,
            // we need to round down the menu width before using it to draw the triangle --
            // otherwise there'll be a noticeable gap in between.
            VertexPositionColor[] vertices = new VertexPositionColor[6];
            vertices[0] = new VertexPositionColor(new Vector3((int)MenuWidthCurrent, ScreenManager.GraphicsDevice.Viewport.Height, 0), bgColor);
            vertices[1] = new VertexPositionColor(new Vector3((int)MenuWidthCurrent, 0, 0), bgColor);
            vertices[2] = new VertexPositionColor(new Vector3((int)MenuWidthCurrent + MenuTriangleWidth, 0, 0), bgColor);

            // Set up a basic effect and an orthographic projection to draw the previous triangle
            // vertices onto the screen
            BasicEffect basicEffect = new BasicEffect(ScreenManager.GraphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height, 0, 0, 1);

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                ScreenManager.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 1);
            }
            GameServices.GetService<SpriteBatch>().End();

            // Render the UI
            Desktop.RenderVisual();
        }
    }
}
