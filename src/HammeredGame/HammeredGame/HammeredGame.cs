using HammeredGame.Core;
using HammeredGame.Game;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.FloorObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
//using System.Numerics;

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
        private Camera camera;

        // INPUT and other related stuff
        private Input input;

        // RENDER TARGET
        private RenderTarget2D mainRenderTarget;

        // RECTANGLES (need to modify to allow modifiable resolutions, etc.)
        private Rectangle desktopRect;
        private Rectangle screenRect;

        private SpriteFont tempFont;

        private List<GameObject> gameObjects;

        private Key key;

        static public List<EnvironmentObject> ActiveLevelObstacles = new();

        // SCENE TEST VARIABLES
        private int testObstaclesCombo = 3;

        // ImGui renderer and list of UIs to render
        private ImGuiRenderer imGuiRenderer;
        private readonly List<IImGui> uiEntities = new();

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
            mainRenderTarget = new RenderTarget2D(gpu, SCREENWIDTH, SCREENHEIGHT, false, pp.BackBufferFormat, DepthFormat.Depth24);
            ScreenW = mainRenderTarget.Width;
            ScreenH = mainRenderTarget.Height;
            desktopRect = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            screenRect = new Rectangle(0, 0, ScreenW, ScreenH);

            // Initialize Input class
            input = new Input(pp, mainRenderTarget);

            // Initialize Camera class
            camera = new Camera(gpu, Vector3.Zero, Vector3.Up, input);

            // Set title for game window
            Window.Title = "HAMMERED";

            // Initialize ImGui's internal renderer and build its font atlas
            imGuiRenderer = new ImGuiRenderer(this);
            imGuiRenderer.RebuildFontAtlas();

            base.Initialize();
        }

        /// <summary>
        /// Called once when loading the game. Load all assets here since it is expensive to load
        /// them on demand when we need it in e.g. Update() or Draw().
        /// </summary>
        protected override void LoadContent()
        {
            tempFont = Content.Load<SpriteFont>("temp_font");

            InitializeLevel(0);
        }

        /// <summary>
        /// Relatively expensive function! Loads the XML file from disk, parses it and instantiates
        /// the level (including Camera and GameObjects like player, hammer, obstacles). Will reset
        /// all visible UI as well and show only the UIs relevant to the new objects.
        /// </summary>
        /// <param name="levelToLoad"></param>
        private void InitializeLevel(int levelToLoad)
        {
            // Clear the UI list to get a clean state with no duplicates
            uiEntities.Clear();

            XMLLevelLoader levelLoader = new XMLLevelLoader($"level{levelToLoad.ToString()}.xml");

            camera = levelLoader.GetCamera(gpu, input);
            gameObjects = levelLoader.GetGameObjects(Content, input, camera);

            ActiveLevelObstacles.Clear();
            foreach (GameObject entity in gameObjects)
            {
                // Add all level objects with an associated UI to the list of UIs to draw in Draw()
                if (entity is IImGui imGuiAble)
                {
                    uiEntities.Add(imGuiAble);
                }

                // All objects that the player can collide with (for now, this is everything but
                // Ground) needs to be stored in activeLevelObstacles, which the Player class checks
                // for collision against.
                // TODO: this needs to change to a different implementation when collision detection changes.
                var envAble = entity as EnvironmentObject;
                if (envAble != null && entity is not Ground)
                {
                    ActiveLevelObstacles.Add(envAble);
                }
            }

            // The Game object itself (this class) also has an UI
            uiEntities.Add(this);
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            // Update input
            input.Update();
            // Check for exit input
            if (input.BACK_DOWN || input.KeyDown(Keys.Escape)) Exit();

            if (input.ButtonPress(Buttons.Y) || input.KeyDown(Keys.R))
            {
                InitializeLevel(testObstaclesCombo);
            }
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            // Update each game object
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.Update(gameTime);
            }

            // Update camera
            camera.UpdateCamera();

            base.Update(gameTime);
        }

        /// <summary>
        /// Adapted from AlienScribble Make 3D Games with Monogame playlist: https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
        /// <para/>
        /// To set state variables within graphics device back to default (in case they are changed
        /// at any point) to ensure we are correctly drawing in 3D space
        /// </summary>
        void Set3DStates()
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
            // Set the Render Target for drawing
            gpu.SetRenderTarget(mainRenderTarget);
            gpu.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.AliceBlue, 1.0f, 0);
            Set3DStates();

            // Render all the scene objects (given that they are not destroyed)
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.Draw(camera.ViewMatrix, camera.ProjMatrix);
            }

            // Draw MainTarget to BackBuffer
            gpu.SetRenderTarget(null);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(mainRenderTarget, desktopRect, Color.White);
            if (key != null &&key.IsPickedUp())
            {
                spriteBatch.DrawString(tempFont, "KEY PICKED UP!", new Vector2(100, 100), Color.Red);
            }
            spriteBatch.End();

            base.Draw(gameTime);

#if DEBUG
            // == Draw debug UI on top of all rendered base.
            // Code adapted from ImMonoGame example code.
            // Begin by calling BeforeLayout

            imGuiRenderer.BeforeLayout(gameTime);

            // Draw each of our entities
            foreach (var UIEntity in uiEntities)
            {
                if (UIEntity != null)
                    UIEntity.UI();
            }

            // Call AfterLayout to finish.
            imGuiRenderer.AfterLayout();
#endif
        }

        public void UI()
        {
            ImGui.SetNextWindowBgAlpha(0.3f);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(50, 150));
            ImGui.Begin("Hammered", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing);

            ImGui.DragInt("Scene", ref testObstaclesCombo, 0.1f, 0, 3);
            ImGui.Text($"Camera Coordinates: {camera.Position.ToString()}");
            ImGui.Text($"Camera Focus: {camera.Target.ToString()}");
            ImGui.Text($"Loaded objects: {gameObjects.Count().ToString()}");

            ImGui.End();
        }
    }
}
