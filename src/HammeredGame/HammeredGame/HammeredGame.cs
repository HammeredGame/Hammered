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
using System.Collections.Generic;

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
        public int ScreenW, ScreenH;

        public static ParallelLooper ParallelLooper;

        // INPUT and other related stuff
        private Input input;

        // RENDER TARGET
        private RenderTarget2D mainRenderTarget;

        // RECTANGLES (need to modify to allow modifiable resolutions, etc.)
        private Rectangle desktopRect;

        private Rectangle screenRect;

        private readonly GameServices gameServices = new();

        // Music variables
        private List<SoundEffect> sfx = new List<SoundEffect>();

        private AudioManager audioManager;

        private ScreenManager manager;

        // ImGui renderer and list of UIs to render
        private ImGuiRenderer imGuiRenderer;


        public HammeredGame()
        {
            // Get width and height of desktop and set the graphics device settings
            int desktop_width = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 10;
            int desktop_height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 10;
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = desktop_width,
                PreferredBackBufferHeight = desktop_height,
                IsFullScreen = false,
                PreferredDepthStencilFormat = DepthFormat.None,
                GraphicsProfile = GraphicsProfile.HiDef
            };

            Window.Title = "HAMMERED";
            Window.IsBorderless = true;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            gpu = GraphicsDevice;
            PresentationParameters pp = gpu.PresentationParameters;
            spriteBatch = new SpriteBatch(gpu);

            // Set Render Target to SCREENWIDTH x SCREENHEIGHT
            mainRenderTarget = new RenderTarget2D(gpu, pp.BackBufferWidth, pp.BackBufferHeight, false, pp.BackBufferFormat, DepthFormat.Depth24);
            ScreenW = mainRenderTarget.Width;
            ScreenH = mainRenderTarget.Height;
            desktopRect = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            screenRect = new Rectangle(0, 0, ScreenW, ScreenH);

            // Initialize Input class
            input = new Input(pp, mainRenderTarget);

            // Set up the parallelization pool for the physics engine based on the amount of cores
            // we have.
            if (ParallelLooper == null)
            {
                // Initialize paraller looper to tell the physics engine that it can use
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

            MyraEnvironment.Game = this;

            //initialize audio manager
            audioManager = new AudioManager(this);

            // Initialize ImGui's internal renderer and build its font atlas
            imGuiRenderer = new ImGuiRenderer(this);
            imGuiRenderer.RebuildFontAtlas();

            // Add useful game services that might want to be accessed globally
            gameServices.AddService<HammeredGame>(this);
            gameServices.AddService<GraphicsDevice>(gpu);
            gameServices.AddService<SpriteBatch>(spriteBatch);
            gameServices.AddService<Input>(input);
            gameServices.AddService<ContentManager>(Content);
            gameServices.AddService<ScriptUtils>(new ScriptUtils());
            gameServices.AddService<List<SoundEffect>>(sfx);
            gameServices.AddService<AudioManager>(audioManager);

            manager = new ScreenManager(gameServices, gpu, mainRenderTarget);
            InitTitleScreen();

            base.Initialize();
        }

        /// <summary>
        /// Function to add the title screen to the stack, setting handlers for Continue and Start New.
        /// </summary>
        public void InitTitleScreen()
        {
            manager.AddScreen(new Game.Screens.TitleScreen()
            {
                ContinueFunc = () =>
                {
                    // load scene name from file
                    manager.AddScreen(new Game.Screens.GameScreen(typeof(Game.Scenes.Island1.TreeTutorial).FullName));
                },
                StartNewFunc = () =>
                {
                    manager.AddScreen(new Game.Screens.GameScreen(typeof(Game.Scenes.Island1.ShoreWakeup).FullName));
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
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            // Update global things
            gameServices.GetService<Input>().Update();
            gameServices.GetService<ScriptUtils>().Update(gameTime);

            // Call update on the various active screens to do their thing
            manager.Update(gameTime);

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
            spriteBatch.Draw(mainRenderTarget, desktopRect, Color.White);

            // FOR THE PURPOSES OF THE DEMO, we indicate whether the puzzle is solved here
            //if (player.ReachedGoal)
            //{
            //    spriteBatch.DrawString(tempFont, "PUZZLE SOLVED!! \nPress R on keyboard or Y on controller to reload level", new Microsoft.Xna.Framework.Vector2(100, 100), Color.Red);
            //}

            // Commit all the data to the back buffer
            spriteBatch.End();

#if DEBUG
            // == Draw debug UI on top of all rendered base.
            // Begin by calling BeforeLayout, which handles input
            imGuiRenderer.BeforeLayout(gameTime, this.IsActive);

            // Draw the main developer UI
            UI();

            // Call AfterLayout to finish.
            imGuiRenderer.AfterLayout();
#endif

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
