using BEPUutilities.Threading;
using HammeredGame.Core;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using System;
using Pleasing;
using System.IO;
using Microsoft.Xna.Framework.Media;

namespace HammeredGame
{
    public class HammeredGame : Microsoft.Xna.Framework.Game
    {
        // DISPLAY VARIABLES
        public const int SCREENWIDTH = 1280;

        public const int SCREENHEIGHT = 720;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private GraphicsDevice gpu;

        public static ParallelLooper ParallelLooper;

        // INPUT and other related stuff
        private Input input;

        // The render target, which may have a different resolution to the actual number of pixels
        // displayed on the screen
        private RenderTarget2D mainRenderTarget;

        private readonly GameServices gameServices = new();

        // Music variables
        //private List<SoundEffect> sfx = new List<SoundEffect>();
        private AudioManager audioManager;

        private ScreenManager manager;

        // ImGui renderer and list of UIs to render
        private ImGuiRenderer imGuiRenderer;

        private bool drawDebugUI;

        public HammeredGame()
        {
            // Get width and height of desktop and set the graphics device settings
            int desktop_width = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int desktop_height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = desktop_width,
                PreferredBackBufferHeight = desktop_height,
                IsFullScreen = false,
                PreferredDepthStencilFormat = DepthFormat.None,
                GraphicsProfile = GraphicsProfile.HiDef,
                HardwareModeSwitch = false
            };

            Window.Title = "HAMMERED";
            Window.IsBorderless = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Load user settings
            UserSettings settings = UserSettings.CreateFromFile("settings.txt");

            gpu = GraphicsDevice;
            PresentationParameters pp = gpu.PresentationParameters;
            spriteBatch = new SpriteBatch(gpu);

            // Set full screen (Windows-only) and border-less based on settings
            SetFullScreen(settings.FullScreen);
            SetBorderless(settings.Borderless);
            // Update render resolution and set up mainRenderTarget
            SetResolution(settings.Resolution.Width, settings.Resolution.Height);

            // Initialize Input class, todo: this isn't updated when resolution changes, although
            // it's currently not a serious issue since we don't use mouse position (Myra UI and
            // ImGui have their own handling code)
            input = new Input(pp, mainRenderTarget);

            //initialize audio manager and set initial volumes
            audioManager = new AudioManager(this);
            SetMediaVolume(settings.MediaVolume);
            SetSfxVolume(settings.SfxVolume);

            // Set up the parallelization pool for the physics engine based on the amount of cores
            // we have.
            if (ParallelLooper == null)
            {
                // Initialize parallel looper to tell the physics engine that it can use
                // multithreading, if possible
                ParallelLooper = new ParallelLooper();
                if (Environment.ProcessorCount > 1)
                {
                    for (int i = 0; i < Environment.ProcessorCount; i++)
                    {
                        ParallelLooper.AddThread();
                    }
                }
            }

            // Set up the UI library (Myra)'s access to the game context
            MyraEnvironment.Game = this;

            // Initialize ImGui's internal renderer and build its font atlas
            imGuiRenderer = new ImGuiRenderer(this);
            imGuiRenderer.RebuildFontAtlas();

            // Add useful game services that might want to be accessed globally
            gameServices.AddService<HammeredGame>(this);
            gameServices.AddService<GraphicsDevice>(gpu);
            gameServices.AddService<GraphicsDeviceManager>(graphics);
            gameServices.AddService<SpriteBatch>(spriteBatch);
            gameServices.AddService<Input>(input);
            gameServices.AddService<UserSettings>(settings);
            gameServices.AddService<ContentManager>(Content);
            gameServices.AddService<ScriptUtils>(new ScriptUtils());
            gameServices.AddService<AudioManager>(audioManager);

            manager = new ScreenManager(gameServices, gpu, mainRenderTarget);

#if DEBUG
            drawDebugUI = true;
#endif

            InitTitleScreen();

            base.Initialize();
        }

        /// <summary>
        /// Function to add the title screen to the stack, setting handlers for Continue and Start New.
        /// </summary>
        public void InitTitleScreen()
        {
            // We allow continuation if we a last saved scene exists and is a valid scene
            string lastSaveName = gameServices.GetService<UserSettings>().LastSaveScene;
            bool continuable = lastSaveName != null && Type.GetType(lastSaveName) != null;

            manager.AddScreen(new Game.Screens.TitleScreen()
            {
                Continuable = continuable,
                ContinueFunc = () =>
                {
                    manager.AddScreen(new Game.Screens.GameScreen(lastSaveName));
                },
                StartNewFunc = () =>
                {
                    manager.AddScreen(new Game.Screens.GameScreen(typeof(Game.Scenes.Island1.ShoreWakeup).FullName));
                },
                ToggleDebugUIFunc = () =>
                {
                    drawDebugUI = !drawDebugUI;
                }
            });
        }

        /// <summary>
        /// Called once when loading the game. Load all assets here since it is expensive to load
        /// them on demand when we need it in e.g. Update() or Draw().
        /// </summary>
        protected override void LoadContent()
        {
            // Load assets related to input prompts
            input.Prompts.LoadContent();

            // Load assets related to shown screens
            manager.LoadContent();
        }

        /// <summary>
        /// Changes the game resolution. Specifically, it changes the GPU's back buffer size, the
        /// intermediate render target size (the one before copying to the GPU), and any other
        /// screen-specific things by issuing <see cref="ScreenManager.SetResolution(int, int)"/>.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetResolution(int width, int height)
        {
            // Update the GPU back buffer size, this changes the window size as well unless we're
            // full-screen. If we are full screen, then the input arguments should equal to the
            // display resolution (obtained from gpu.DisplayMode), and the result otherwise is
            // inconsistent and buggy across platforms.
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.ApplyChanges();

            // Set up the render target, which we will render to, which gets copied to the GPU back buffer.
            mainRenderTarget = new RenderTarget2D(gpu, width, height, false, gpu.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);

            if (manager != null)
            {
                manager.MainRenderTarget = mainRenderTarget;
                manager.SetResolution(width, height);
            }
        }

        /// <summary>
        /// Changes the game's full-screen status. This uses a soft full screen (set by <see
        /// cref="GraphicsDeviceManager.HardwareModeSwitch"/> being false), which means we aren't
        /// taking over the GPU exclusively (which is buggy on Mac for example and not recommended
        /// on Windows). As a consequence though, you generally also have to set the back buffer
        /// size to be equal to the display resolution (with <see cref="SetResolution(int, int)"/>
        /// whenever you call this function with the true argument.
        /// </summary>
        /// <param name="fullScreen"></param>
        public void SetFullScreen(bool fullScreen)
        {
            graphics.IsFullScreen = fullScreen;
            graphics.ApplyChanges();
        }

        /// <summary>
        /// Toggles the game's OS border/chrome.
        /// </summary>
        /// <param name="borderless"></param>
        public void SetBorderless(bool borderless)
        {
            Window.IsBorderless = borderless;
            graphics.ApplyChanges();
        }

        /// <summary>
        /// Set the music volume, taking a linear input from [0,1] and scaling exponentially and
        /// shifted to the [0,1] range to fit the natural human hearing curve. Since the exponential
        /// function doesn't reach 0 at x = 0, we switch to a linear function near that point.
        /// <para/>
        /// Explanation here: https://www.dr-lex.be/info-stuff/volumecontrols.html
        /// </summary>
        /// <param name="mediaVolume"></param>
        public void SetMediaVolume(float mediaVolume)
        {
            // The value 0.296 is the intersection between the exp(5x - 5) graph and the 0.1x graph,
            // and is the point where we switch from exponential to linear falloff as x nears 0
            MediaPlayer.Volume = mediaVolume > 0.296f ? MathF.Exp(5f * mediaVolume - 5f) : mediaVolume * 0.1f;
        }

        /// <summary>
        /// Set the sound effect volume, taking a linear input from [0,1] and scaling exponentially
        /// and shifted to the [0,1] range to fit the natural human hearing curve. Since the
        /// exponential function doesn't reach 0 at x = 0, we switch to a linear function near that point.
        /// <para/>
        /// Explanation here: https://www.dr-lex.be/info-stuff/volumecontrols.html
        /// </summary>
        /// <param name="sfxVolume"></param>
        public void SetSfxVolume(float sfxVolume)
        {
            SoundEffect.MasterVolume = sfxVolume > 0.296f ? MathF.Exp(5f * sfxVolume - 5f) : sfxVolume * 0.1f;
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            // Update global things

            // Input should only be updated if the window is focused. Keyboard events are usually
            // blocked by the OS and don't fall through from other windows, but click events do.
            // Without this check, clicking on another window on top of the game can register clicks
            // in the game, which is annoying.
            if (this.IsActive)
            {
                gameServices.GetService<Input>().Update();
            }

            gameServices.GetService<ScriptUtils>().Update(gameTime);

            // Update any animations that are active (doing this before the ScreenManager update so
            // that new values are used for it)
            Tweening.Update(gameTime);
            AsyncContentManagerExtension.Update();

            // Call update on the various active screens to do their thing
            manager.Update(gameTime);
            audioManager.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Called on each game loop after Update(). Should not contain expensive computation but
        /// rather just rendering and drawing to the GPU.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            // Make all draw calls to the GPU write not to the main back buffer (which gets swapped
            // out with the front buffer and shown to the user), but instead write to a temporary
            // render target, which allows us to inspect the content if we want, to apply filters or
            // capture screenshots of the game. For now we don't make use of this, but it can be useful.
            gpu.SetRenderTarget(mainRenderTarget);

            // Clear the target
            gpu.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.AliceBlue, 1.0f, 0);
            //Set3DStates();

            manager.Draw(gameTime);

            // Change the GPU target to null, which means all further draw calls will now write to
            // the back buffer. We need to copy over what we have in the temporary render target.
            gpu.SetRenderTarget(null);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(mainRenderTarget,  new Rectangle(0, 0, gpu.PresentationParameters.BackBufferWidth, gpu.PresentationParameters.BackBufferHeight), Color.White);

            // Commit all the data to the back buffer
            spriteBatch.End();

            if (drawDebugUI)
            {
                // == Draw debug UI on top of all rendered base.
                // Begin by calling BeforeLayout, which handles input
                imGuiRenderer.BeforeLayout(gameTime, this.IsActive);

                // Draw the main developer UI
                UI();

                // Call AfterLayout to finish.
                imGuiRenderer.AfterLayout();
            }

            base.Draw(gameTime);
        }

        public void UI()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 500), ImGuiCond.FirstUseEver);
            ImGui.Begin("Hammered");

            // Show whether the gamepad is detected
            if (input.GamePadState.IsConnected)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.0f, 1.0f, 1.0f), "Gamepad Connected - " + GamePad.GetCapabilities(0).DisplayName);
            }
            float fr = ImGui.GetIO().Framerate;
            ImGui.Text($"{1000.0f / fr:F2} ms/frame ({fr:F1} FPS)");
            manager.UI();
        }
    }
}
