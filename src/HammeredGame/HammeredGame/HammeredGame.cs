using ImGuiNET;
using ImMonoGame.Thing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
//using System.Numerics;

namespace HammeredGame
{
    public class HammeredGame : Game
    {
        // DISPLAY VARIABLES
        const int SCREENWIDTH = 1280;
        const int SCREENHEIGHT = 720;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice gpu;
        static public int screenW, screenH;
        Camera _camera;

        // INPUT and other related stuff
        Input inp;

        // RENDER TARGET
        RenderTarget2D MainTarget;

        // RECTANGLES (need to modify to allow modifiable resolutions, etc.)
        Rectangle desktopRect;
        Rectangle screenRect;

        // 3D Objects and other related stuff (Class refactoring required)
        private Player _player;
        private Hammer _hammer;
        private Texture2D playerTex;
        private WorldObject _ground;
        private List<GameObject> gameObjects;
        private List<GameObject> levelObstacles;

        // ImGui renderer and list of UIs to render
        private ImGuiRenderer _imGuiRenderer;
        private List<IImGui> UIEntities = new List<IImGui>();

        public HammeredGame()
        {
            // Get width and height of desktop and set the graphics device settings
            int desktop_width = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 10;
            int desktop_height = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 10;
            _graphics = new GraphicsDeviceManager(this)
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
            _spriteBatch = new SpriteBatch(gpu);

            // Set Render Target to SCREENWIDTH x SCREENHEIGHT 
            MainTarget = new RenderTarget2D(gpu, SCREENWIDTH, SCREENHEIGHT, false, pp.BackBufferFormat, DepthFormat.Depth24);
            screenW = MainTarget.Width;
            screenH = MainTarget.Height;
            desktopRect = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            screenRect = new Rectangle(0, 0, screenW, screenH);

            // Initialize Input class
            inp = new Input(pp, MainTarget);

            // Initialize Camera class
            _camera = new Camera(gpu, Vector3.Up, inp);

            // Set title for game window
            Window.Title = "HAMMERED";

            // Initialize ImGui's internal renderer and build the font atlas
            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // The structure of the content of this function might change 
            // depending on how we structure our classes/hierarchy (how we want to load things into the scene)
            // Most likely: will be replaced with XML parsing here

            // Load Texture
            playerTex = Content.Load<Texture2D>("Temp");

            // Load obstacles for testing (TEMPORARY - after xml parsing and incorporating better collision detection, this should change)
            Obstacle Obstacle1 = new Obstacle(Content.Load<Model>("test_obstacle"), new Vector3(10f, 0f, 10f), 1.5f, inp, _camera, null);
            Obstacle Obstacle2 = new Obstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 0f, -10f), 1.5f, inp, _camera, null);
            Obstacle Obstacle3 = new Obstacle(Content.Load<Model>("test_obstacle"), new Vector3(5f, 0f, -5f), 1.5f, inp, _camera, null);
            levelObstacles = new List<GameObject> { Obstacle1, Obstacle2, Obstacle3 };

            // Load and initialize player character
            _player = new Player(Content.Load<Model>("character_test"), Vector3.Zero, 1.5f, inp, _camera, playerTex, levelObstacles);

            // Load and initialize hammer object
            _hammer = new Hammer(Content.Load<Model>("temp_hammer"), Vector3.Zero, 1.5f, _player, inp, _camera, null, levelObstacles);

            // Load and initialize the terrain/ground
            _ground = new WorldObject(Content.Load<Model>("temp_floor"), new Vector3(0, -10f, 0), 25f, inp, _camera, playerTex);

            // Initialize list of gameobjects for drawing
            gameObjects = new List<GameObject> { _player, _hammer, _ground, Obstacle1, Obstacle2, Obstacle3 };

            // for now, add a temporary UI with the Player class debug info
            UIEntities.Add(_player);
        }

        protected override void Update(GameTime gameTime)
        {
            // Update input
            inp.Update();
            // Check for exit input
            if (inp.back_down || inp.KeyDown(Keys.Escape)) Exit();
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            // Update each game object
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.Update(gameTime);
            }

            // Update camera
            _camera.UpdateCamera();

            base.Update(gameTime);
        }

        // Adapted from AlienScribble Make 3D Games with Monogame playlist: https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
        // To set state variables within graphics device back to default (in case they are changed at any point)
        // to ensure we are correctly drawing in 3D space
        void Set3DStates()
        {
            gpu.BlendState = BlendState.NonPremultiplied; // Potentially needs to be modified depending on our textures
            gpu.DepthStencilState = DepthStencilState.Default; // Ensure we are using depth buffer (Z-buffer) for 3D
            if (gpu.RasterizerState.CullMode == CullMode.None)
            {
                // Cull back facing polygons
                RasterizerState rs = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };
                gpu.RasterizerState = rs;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // Set the Render Target for drawing
            gpu.SetRenderTarget(MainTarget);
            gpu.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.AliceBlue, 1.0f, 0);
            Set3DStates();

            // Render all the scene objects (given that they are not destroyed)
            foreach (GameObject gameObject in gameObjects)
            {
                if (!gameObject.destroyed)
                {
                    gameObject.Draw(_camera.view, _camera.proj);
                }
            }

            // Draw MainTarget to BackBuffer
            gpu.SetRenderTarget(null);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            _spriteBatch.Draw(MainTarget, desktopRect, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);

#if DEBUG
            // == Draw debug UI on top of all rendered base.
            // Code adapted from ImMonoGame example code.
            // Begin by calling BeforeLayout
            _imGuiRenderer.BeforeLayout(gameTime);

            // Draw each of our entities
            foreach (var UIEntity in UIEntities)
            {
                if (UIEntity != null)
                    UIEntity.UI();
            }

            // Call AfterLayout to finish.
            _imGuiRenderer.AfterLayout();
#endif
        }
    }
}