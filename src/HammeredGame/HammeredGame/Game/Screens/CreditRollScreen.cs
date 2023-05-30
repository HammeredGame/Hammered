using FontStashSharp;
using HammeredGame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Pleasing;
using Microsoft.Xna.Framework.Content;
using System;
using Microsoft.Xna.Framework.Media;
using System.Reflection;
using System.Diagnostics;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// The credits rolls screen shows credits for the game, then auto-exits once it finishes.
    /// </summary>
    internal class CreditRollScreen : Screen
    {
        private Desktop desktop;
        private Texture2D whiteRectangle;

        private FontSystem barlowMediumFont;
        private FontSystem barlowBoldFont;

        private ScrollViewer scrollingCredits;
        private VerticalStackPanel creditsPanel;

        private float scrollPosition = 0f;

        private int fivePercentageHeight;

        private Color backgroundColor = Color.Transparent;
        private TweenTimeline transitionTimeline;
        private readonly bool showTheEnd;

        float oldVolume;

        public Action FinishedFunc;

        public CreditRollScreen(bool showTheEnd = true)
        {
            IsPartial = false;
            PassesFocusThrough = false;
            this.showTheEnd = showTheEnd;
        }

        public override void LoadContent()
        {

            Song bgMusic;
            bgMusic = GameServices.GetService<ContentManager>().Load<Song>("Audio/balanced/bgm5_4x");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
            base.LoadContent();

            whiteRectangle = new Texture2D(ScreenManager.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            // Calculate the height of 5% of the screen, which is used for spacing and text size, so
            // it is responsive and looks the same on all resolutions.
            fivePercentageHeight = ScreenManager.GraphicsDevice.Viewport.Height / 20;

            // Load fonts, both Medium and Bold
            byte[] barlowMediumTtfData = System.IO.File.ReadAllBytes("Content/Fonts/Barlow-Medium.ttf");
            barlowMediumFont = new FontSystem();
            barlowMediumFont.AddFont(barlowMediumTtfData);

            byte[] barlowBoldTtfData = System.IO.File.ReadAllBytes("Content/Fonts/Barlow-Bold.ttf");
            barlowBoldFont = new FontSystem();
            barlowBoldFont.AddFont(barlowBoldTtfData);

            // The scrolling credits widget, which is the size of the screen
            scrollingCredits = new ScrollViewer()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ShowVerticalScrollBar = false,
            };

            // The underlying panel that scrolls within the scroll viewer, and which contains all
            // the credits
            creditsPanel = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            // Key people
            creditsPanel.AddChild(Credit("Producer", "Yuto Takano"));
            creditsPanel.AddChild(Credit("Co-Producer", "Audrey Leong"));
            creditsPanel.AddChild(Credit("Level Designer", "Andrew Dobis"));
            creditsPanel.AddChild(Credit("Artist", "Marie Jaillot"));
            creditsPanel.AddChild(Credit("Tech Lead", "Siddharth Menon"));
            creditsPanel.AddChild(Credit("Composer", "Audrey Leong"));
            creditsPanel.AddChild(Credit("Sound Engineer", "Audrey Leong"));
            creditsPanel.AddChild(Credit("Written by", "Konstantinos Stavratis"));
            creditsPanel.AddChild(Credit("Programmers", "Siddharth Menon", "Audrey Leong", "Marie Jaillot", "Andrew Dobis", "Konstantinos Stavratis", "Yuto Takano"));
            creditsPanel.AddChild(Credit("3D Modeling", "Andrew Dobis", "Marie Jaillot"));

            creditsPanel.AddChild(Heading("Trailer Cast"));
            creditsPanel.AddChild(Credit("Thor", "Morten Borup Pertersen"));

            // External Testers
            creditsPanel.AddChild(Heading("Testers"));
            creditsPanel.AddChild(Credit("QA Tester",
                "Steven Wang",
                "Samuel Simko",
                "Roxana Stiuca",
                "Boyko Borisov",
                "Oana Rosca",
                "Saikiran Akkapaka"));

            // Software special thanks (licenses will be in a separate screen if required)
            creditsPanel.AddChild(Heading("Software"));

            creditsPanel.AddChild(SmallText(@"
ZapSplat
(zapsplat.com)
Additional Samples by Avery Berman
(empoweredmusicproducer.com)
Kenney UI Audio pack
(https://kenney.nl/assets/ui-audio)
Universal UI/Menu Soundpack
(https://ellr.itch.io/universal-ui-soundpack)
"));

            creditsPanel.AddChild(LineSpacing());
            creditsPanel.AddChild(Image("Credits/monogame-logo", 0.7f));
            creditsPanel.AddChild(LineSpacing());
            creditsPanel.AddChild(SmallText("Made with MonoGame."));
            creditsPanel.AddChild(SmallText("MonoGame® is a registered trademark of the MonoGame project."));

            //creditsPanel.AddChild(SmallText("See the separate license screen for open-source software licenses used in this game."));

            // Special thanks to GTC
            creditsPanel.AddChild(Heading("Thanks To"));
            creditsPanel.AddChild(Image("Credits/gtc-logo", 0.2f));
            creditsPanel.AddChild(LineSpacing());
            creditsPanel.AddChild(SmallText("The Game Programming Lab course at ETH Zurich for providing us\nwith the opportunity to make this game."));

            creditsPanel.AddChild(LineSpacing());
            creditsPanel.AddChild(LineSpacing());
            creditsPanel.AddChild(SmallText("Hammered v" + (FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion ?? "Unknown version") + " is presented to you by the Hammered Team"));

            scrollingCredits.Content = creditsPanel;

            // Add it to the desktop temporarily to calculate the bounds, needed to find the scroll position
            desktop = new()
            {
                Root = scrollingCredits
            };

            // Update the desktop once to set the scroll position to just below the screen
            desktop.UpdateLayout();
            scrollPosition = -scrollingCredits.Bounds.Height;
            scrollingCredits.ScrollPosition = new Point(0, (int)scrollPosition);

            // Set the desktop to a The End text at first, which we switch out to the credits once
            // the fade in transition is over.
            desktop.Root = new Label()
            {
                TextColor = Color.White,
                Text = "The End",
                Font = barlowBoldFont.GetFont(MathHelper.Max(fivePercentageHeight, 32f) * 2),
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            // Re-calculate desktop layouts to center the above label properly
            desktop.UpdateLayout();
            // Start the desktop transparent and fade it in through transitions
            desktop.Opacity = 0f;
        }

        /// <summary>
        /// Create a widget centered in the horizontal middle that has a role name on the left and a
        /// list of names on the right.
        /// </summary>
        /// <param name="role"></param>
        /// <param name="names"></param>
        public Widget Credit(string role, params string[] names)
        {
            Label roleLabel = new()
            {
                TextColor = Color.Gray,
                Text = role,
                // minimum 32 font size
                Font = barlowMediumFont.GetFont(MathHelper.Max(fivePercentageHeight, 32f)),
                HorizontalAlignment = HorizontalAlignment.Right,
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Right
            };
            Widget spacing = new()
            {
                Width = 50
            };
            Label nameLabel = new()
            {
                TextColor = Color.White,
                Text = string.Join("\n", names),
                // minimum 32 font size
                Font = barlowMediumFont.GetFont(MathHelper.Max(fivePercentageHeight, 32f)),
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Left
            };
            HorizontalStackPanel panel = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Make the left and right take up the available amounts, so that the middle spacing
            // always gets centered in the middle regardless of the length of the role and names.
            panel.Proportions.Add(new Proportion { Type = ProportionType.Part });
            panel.Proportions.Add(new Proportion());
            panel.Proportions.Add(new Proportion { Type = ProportionType.Part });
            panel.Widgets.Add(roleLabel);
            panel.Widgets.Add(spacing);
            panel.Widgets.Add(nameLabel);
            return panel;
        }

        /// <summary>
        /// Create an all-caps heading with some padding above and below.
        /// </summary>
        /// <param name="heading"></param>
        public Label Heading(string heading)
        {
            Label label = FreeText(heading.ToUpper());
            label.Font = barlowBoldFont.GetFont(MathHelper.Max(fivePercentageHeight, 32f));
            label.Padding = new Thickness(0, fivePercentageHeight * 2, 0, fivePercentageHeight);
            return label;
        }

        /// <summary>
        /// Create a larger all-caps heading with some padding above. A <see
        /// cref="Heading(string)"/> should immediately follow this.
        /// </summary>
        /// <param name="heading"></param>
        public Label LargeHeading(string heading)
        {
            Label label = FreeText(heading.ToUpper());
            label.Font = barlowBoldFont.GetFont(MathHelper.Max(fivePercentageHeight, 32f) * 1.5f);
            label.Padding = new Thickness(0, fivePercentageHeight * 2, 0, 0);
            return label;
        }

        /// <summary>
        /// Some center-aligned free text. Wrapping must be done manually.
        /// </summary>
        /// <param name="text"></param>
        public Label FreeText(string text)
        {
            return new Label
            {
                TextColor = Color.White,
                Text = text,
                Font = barlowMediumFont.GetFont(MathHelper.Max(fivePercentageHeight, 32f)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Center
            };
        }

        /// <summary>
        /// Some center-aligned free text at half size. Wrapping must be done manually.
        /// </summary>
        /// <param name="text"></param>
        public Label SmallText(string text)
        {
            Label label = FreeText(text);
            label.Font = barlowBoldFont.GetFont(MathHelper.Max(fivePercentageHeight / 2, 16f));
            return label;
        }

        /// <summary>
        /// An empty line 5% of the height of the screen.
        /// </summary>
        public Widget LineSpacing()
        {
            return new Widget()
            {
                Height = fivePercentageHeight
            };
        }

        /// <summary>
        /// An image, loaded from the content pipeline and scaled appropriately.
        /// </summary>
        /// <param name="path">The name of the image, passed to the ContentManager</param>
        /// <param name="scale">
        /// A scale, where a value of 1.0 will make 50px of the image display as 5% of the screen height
        /// </param>
        public Image Image(string path, float scale = 1f)
        {
            Texture2D image = GameServices.GetService<ContentManager>().Load<Texture2D>(path);

            return new Image
            {
                Renderable = new TextureRegion(image),
                // Scale it so its shows at the same size regardless of screen height, with 50px of
                // the image mapping to the fivePercentageHeight.
                Width = (int)(image.Width * fivePercentageHeight / 50 * scale),
                Height = (int)(image.Height * fivePercentageHeight / 50 * scale),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            whiteRectangle.Dispose();
        }

        /// <summary>
        /// Fade in the background and the "The End" text, then fade it out.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="firstFrame"></param>
        /// <returns></returns>
        public override bool UpdateTransitionIn(GameTime gameTime, bool firstFrame)
        {
            if (firstFrame)
            {
                transitionTimeline = Tweening.NewTimeline();
                transitionTimeline
                    .AddColor(this, nameof(backgroundColor))
                    .AddFrame(1000, Color.Black, Easing.Linear);
                if (showTheEnd)
                {
                    transitionTimeline
                        .AddFloat(desktop, nameof(desktop.Opacity))
                        .AddFrame(0, 0)
                        .AddFrame(1000, 0)
                        .AddFrame(1500, 1, Easing.Linear)
                        .AddFrame(3500, 1, Easing.Linear)
                        .AddFrame(4000, 0, Easing.Linear);
                }
            }

            // This function is called only until the first frame where the timeline is stopped, so
            // on that frame, we'll switch to the scrolling credits and reset the opacity.
            if (transitionTimeline.State == TweenState.Stopped)
            {
                desktop.Opacity = 1;
                desktop.Root = scrollingCredits;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Fade out.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="firstFrame"></param>
        /// <returns></returns>
        public override bool UpdateTransitionOut(GameTime gameTime, bool firstFrame)
        {
            if (firstFrame)
            {
                transitionTimeline = Tweening.NewTimeline();
                transitionTimeline
                    .AddColor(this, nameof(backgroundColor))
                    .AddFrame(1500, Color.White, Easing.Linear);
                oldVolume = MediaPlayer.Volume;
                transitionTimeline
                    .AddProperty(oldVolume, (f) => MediaPlayer.Volume = f, LerpFunctions.Float)
                    .AddFrame(0, MediaPlayer.Volume)
                    .AddFrame(1000, 0f, Easing.Linear);
            }
            // This function is called only until the first frame where the timeline is stopped, so
            // on that frame, we'll call any callbacks and return true to indicate we're done.
            if (transitionTimeline.State == TweenState.Stopped)
            {
                FinishedFunc?.Invoke();
                MediaPlayer.Stop();
                MediaPlayer.Volume = oldVolume;
                return true;
            }
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Don't auto-scroll if something else is taking input
            if (!HasFocus) return;

            // Scroll the credits by the same amount regardless of screen size (since we chose text
            // size based on screen size)
            float scrollAmount = ScreenManager.GraphicsDevice.Viewport.Height / 500f;

            if (UserAction.Movement.GetValue(GameServices.GetService<Input>()).Y > 0.5f)
            {
                scrollAmount *= -1;
            }
            if (UserAction.Movement.GetValue(GameServices.GetService<Input>()).Y < -0.5f ||
                UserAction.Confirm.Held(GameServices.GetService<Input>()))
            {
                scrollAmount *= 5;
            }
            scrollPosition += scrollAmount;
            scrollingCredits.ScrollPosition = new Point(0, (int)scrollPosition);

            // If we've scrolled past the end, exit the screen
            if (scrollPosition > creditsPanel.Bounds.Height)
            {
                ExitScreen();
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GameServices.GetService<SpriteBatch>().Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
            GameServices.GetService<SpriteBatch>().Draw(
                whiteRectangle,
                new Rectangle(0, 0, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height),
                null,
                backgroundColor);
            GameServices.GetService<SpriteBatch>().End();

            desktop.RenderVisual();
        }
    }
}
