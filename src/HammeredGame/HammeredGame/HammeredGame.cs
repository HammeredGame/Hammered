using BEPUphysics.Settings;
using BEPUphysics;
using HammeredGame.Core;
using HammeredGame.Game;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;
using BEPUphysics.Entities;
using BEPUutilities.Threading;
using System;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework.Content;

namespace HammeredGame
{
    public class HammeredGame : Microsoft.Xna.Framework.Game, IImGui
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

        private SpriteFont tempFont;

        private readonly GameServices gameServices = new();

        private Scene currentScene;

        // Music variables
        private Song bgMusic;
        private List<SoundEffect> sfx = new List<SoundEffect>();
        private AudioListener listener = new AudioListener();
        private AudioEmitter emitter = new AudioEmitter();
        private AudioManager audioManager;
        

        // ImGui renderer and list of UIs to render
        private ImGuiRenderer imGuiRenderer;

        // Bounding Volume debugging variables
        private bool drawBounds = false;
        private List<EntityDebugDrawer> debugEntities = new();

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

            // Set title for game window
            Window.Title = "HAMMERED";
            
            //initialize audio manager
            audioManager = new AudioManager(this); 

            // Initialize ImGui's internal renderer and build its font atlas
            imGuiRenderer = new ImGuiRenderer(this);
            imGuiRenderer.RebuildFontAtlas();

            // Add useful game services that might want to be accessed globally
            gameServices.AddService<HammeredGame>(this);
            gameServices.AddService<GraphicsDevice>(gpu);
            gameServices.AddService<Input>(input);
            gameServices.AddService<ContentManager>(Content);
            gameServices.AddService<ScriptUtils>(new ScriptUtils());
            gameServices.AddService<List<SoundEffect>>(sfx);
            gameServices.AddService<AudioManager>(audioManager);
            

            base.Initialize();
        }

        /// <summary>
        /// Called once when loading the game. Load all assets here since it is expensive to load
        /// them on demand when we need it in e.g. Update() or Draw().
        /// </summary>
        protected override void LoadContent()
        {
            tempFont = Content.Load<SpriteFont>("temp_font");

            InitializeLevel("HammeredGame.Game.Scenes.Island1.ShoreWakeup");

            
            bgMusic = Content.Load<Song>("Audio/BGM_V2_4x");
            sfx.Add(Content.Load<SoundEffect>("Audio/step"));
            sfx.Add(Content.Load<SoundEffect>("Audio/hammer_drop"));
            sfx.Add(Content.Load<SoundEffect>("Audio/lohi_whoosh"));
            sfx.Add(Content.Load<SoundEffect>("Audio/tree_fall"));
            sfx.Add(Content.Load<SoundEffect>("Audio/ding"));
            sfx.Add(Content.Load<SoundEffect>("Audio/door_open"));
            sfx.Add(Content.Load<SoundEffect>("Audio/door_close"));
            

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.1f; 
            MediaPlayer.Play(bgMusic);

            SoundEffect.MasterVolume = 0.2f; 
        }

        /// <summary>
        /// Relatively expensive function! Loads the XML file from disk, parses it and instantiates
        /// the level (including Camera and GameObjects like player, hammer, obstacles). Will reset
        /// all visible UI as well and show only the UIs relevant to the new objects.
        /// </summary>
        /// <param name="levelToLoad"></param>
        public void InitializeLevel(string levelToLoad)
        {
            currentScene = (Scene)Activator.CreateInstance(Type.GetType(levelToLoad), gameServices);
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            gameServices.GetService<Input>().Update();
            gameServices.GetService<ScriptUtils>().Update(gameTime);

            // Check for exit input
            if (input.BACK_DOWN || input.KeyDown(Keys.Escape)) Exit();

            if (input.ButtonPress(Buttons.Y) || input.KeyPress(Keys.R))
            {
                // Reload the current scene class
                InitializeLevel(currentScene.GetType().FullName);
            }
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            // Update each game object
            foreach (GameObject gameObject in currentScene.GameObjectsList)
            {
                gameObject.Update(gameTime);
            }

            // Update camera
            currentScene.Camera.UpdateCamera();

            //Steps the simulation forward one time step.
            currentScene.Space.Update();

            // Set up the list of debug entities for debugging visualization
            SetupDebugBounds();

            base.Update(gameTime);
        }

        /// <summary>
        /// Adapted from AlienScribble Make 3D Games with Monogame playlist: https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
        /// <para/>
        /// To set state variables within graphics device back to default (in case they are changed
        /// at any point) to ensure we are correctly drawing in 3D space
        /// </summary>
        private void Set3DStates()
        {
            gpu.BlendState = BlendState.AlphaBlend; // Potentially needs to be modified depending on our textures
            gpu.DepthStencilState = DepthStencilState.Default; // Ensure we are using depth buffer (Z-buffer) for 3D
            if (gpu.RasterizerState.CullMode == CullMode.None)
            {
                // Cull back facing polygons
                RasterizerState rs = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };
                gpu.RasterizerState = rs;
            }
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
            Set3DStates();

            // Render all the scene objects (given that they are not destroyed)
            foreach (GameObject gameObject in currentScene.GameObjectsList)
            {
                gameObject.Draw(currentScene.Camera.ViewMatrix, currentScene.Camera.ProjMatrix);
            }

            if (drawBounds)
            {
                RasterizerState currentRS = gpu.RasterizerState;
                gpu.RasterizerState = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.WireFrame };
                foreach (EntityDebugDrawer entity in debugEntities)
                {
                    entity.Draw(gameTime, currentScene.Camera.ViewMatrix, currentScene.Camera.ProjMatrix);
                }
                gpu.RasterizerState = currentRS;
            }

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

            base.Draw(gameTime);

#if DEBUG
            // == Draw debug UI on top of all rendered base.
            // Code adapted from ImMonoGame example code.
            // Begin by calling BeforeLayout

            imGuiRenderer.BeforeLayout(gameTime);

            // Draw the main developer UI
            UI();

            // Call AfterLayout to finish.
            imGuiRenderer.AfterLayout();
#endif
        }

        // Prepare the entities for debugging visualization
        private void SetupDebugBounds()
        {
            debugEntities.Clear();
            var CubeModel = Content.Load<Model>("cube");
            //Go through the list of entities in the space and create a graphical representation for them.
            foreach (Entity e in currentScene.Space.Entities)
            {
                Box box = e as Box;
                if (box != null) //This won't create any graphics for an entity that isn't a box since the model being used is a box.
                {
                    BEPUutilities.Matrix scaling = BEPUutilities.Matrix.CreateScale(box.Width, box.Height, box.Length); //Since the cube model is 1x1x1, it needs to be scaled to match the size of each individual box.
                    EntityDebugDrawer model = new EntityDebugDrawer(e, CubeModel, scaling, this);
                    //Add the drawable game component for this entity to the game.
                    debugEntities.Add(model);
                }
            }
        }

        public void UI()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 500), ImGuiCond.FirstUseEver);
            ImGui.Begin("Hammered");

            // Show whether the gamepad is detected
            if (input.GamePadState.IsConnected)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.0f, 1.0f, 1.0f), "Gamepad Connected");
            }
            float fr = ImGui.GetIO().Framerate;
            ImGui.Text($"{1000.0f / fr:F2} ms/frame ({fr:F1} FPS)");

            // Show a scene switcher dropdown, with the list of all scene class names in this assembly
            ImGui.Text("Current Loaded Scene: ");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##scene", currentScene.GetType().Name))
            {
                foreach (string fqn in Scene.GetAllSceneFQNs())
                {
                    if (ImGui.Selectable(fqn, fqn == currentScene.GetType().FullName))
                    {
                        InitializeLevel(fqn);
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.Text("Press R on keyboard or Y on controller to reload level");
            ImGui.Separator();

            ImGui.Checkbox("DrawBounds", ref drawBounds);

            // Show the scene's UI within the same window
            currentScene.UI();
            ImGui.End();
        }
    }
}
