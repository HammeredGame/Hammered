using HammeredGame.Core;
using HammeredGame.Game;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
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
        private SpriteFont _font;

        private Player _player;
        private Hammer _hammer;
        private Texture2D playerTex;
        private Floor _ground;
        private List<GameObject> gameObjects;

        private Key _key;

        static public List<EnvironmentObject> activeLevelObstacles;

        // SCENE TEST VARIABLES
        private int testObstaclesCombo = 0;

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
            _camera = new Camera(gpu, Vector3.Zero, Vector3.Up, inp);

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

            initializeLevel();
        }

        private void initializeLevel()
        {
            // Load Texture
            playerTex = Content.Load<Texture2D>("Temp");

            _font = Content.Load<SpriteFont>("temp_font");

            //(TEMPORARY - after xml parsing and incorporating better collision detection, all of this should change)
            // Load and initialize player character
            _player = new Player(Content.Load<Model>("character_test"), Vector3.Zero, 0.03f, inp, _camera, playerTex);

            // Load and initialize hammer object
            _hammer = new Hammer(Content.Load<Model>("temp_hammer2"), Vector3.Zero, 0.02f, _player, inp, _camera, null);

            // Load and initialize the terrain/ground
            _ground = new Floor(Content.Load<Model>("temp_floor_flat"), new Vector3(0, -10f, 0), 0.1f, _camera, playerTex);

            // Initialize list of gameobjects for drawing
            gameObjects = new List<GameObject> { _player, _hammer, _ground };

            // Load obstacles for testing
            switch (testObstaclesCombo)
            {
                case 0:
                    {
                        EnvironmentObject Obstacle1 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, _camera, null);
                        EnvironmentObject Obstacle2 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 1f, 10f), 0.02f, _camera, null);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, _camera, null);
                        activeLevelObstacles = new List<EnvironmentObject> { Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
                case 1:
                    {
                        EnvironmentObject Obstacle1 = new Door(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, _camera, null);
                        EnvironmentObject Obstacle2 = new PressurePlate(Content.Load<Model>("temp_pressureplate2"), new Vector3(-10f, 0f, 10f), 0.02f, _camera, null, Obstacle1);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, _camera, null);
                        activeLevelObstacles = new List<EnvironmentObject> { Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
                case 2:
                    {
                        EnvironmentObject Obstacle1 = new Door(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, _camera, null);
                        _key = new Key(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 1f, 10f), 0.01f, _camera, null, (Door)Obstacle1);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, _camera, null);
                        activeLevelObstacles = new List<EnvironmentObject> { Obstacle1, _key, Obstacle3 };
                        break;
                    }
                case 3:
                    {
                        EnvironmentObject Obstacle1 = new Tree(Content.Load<Model>("temp_tree2"), new Vector3(10f, 1f, -30f), 0.05f, _camera, null);
                        EnvironmentObject Obstacle2 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 0f, 10f), 0.02f, _camera, null);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, _camera, null);
                        activeLevelObstacles = new List<EnvironmentObject> { Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
                default:
                    {
                        EnvironmentObject Obstacle1 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, _camera, null);
                        EnvironmentObject Obstacle2 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 1f, 10f), 0.02f, _camera, null);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, _camera, null);
                        activeLevelObstacles = new List<EnvironmentObject> { Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
            }

            gameObjects.AddRange(activeLevelObstacles);

            // for now, add a temporary UI with the Player class debug info
            UIEntities.Add(_player);
            UIEntities.Add(this);
        }

        protected override void Update(GameTime gameTime)
        {
            // Update input
            inp.Update();
            // Check for exit input
            if (inp.back_down || inp.KeyDown(Keys.Escape)) Exit();

            if (inp.KeyDown(Keys.R))
            {
                UIEntities.Remove(_player);
                UIEntities.Remove(this);
                initializeLevel();
            }
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
            gpu.BlendState = BlendState.AlphaBlend; // Potentially needs to be modified depending on our textures
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
                gameObject.Draw(_camera.ViewMatrix, _camera.ProjMatrix);
            }

            // Draw MainTarget to BackBuffer
            gpu.SetRenderTarget(null);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            _spriteBatch.Draw(MainTarget, desktopRect, Color.White);
            if (_key != null &&_key.isKeyPickedUp())
            {
                _spriteBatch.DrawString(_font, "KEY PICKED UP!", new Vector2(100, 100), Color.Red);
            }
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

        public void UI()
        {
            ImGui.SetNextWindowBgAlpha(0.3f);
            ImGui.SetNextWindowPos(new System.Numerics.Vector2(50, 150));
            ImGui.Begin("Scene Debug", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing);

            var numericScene = testObstaclesCombo;
            ImGui.DragInt("Scene", ref numericScene, 0.1f, 0, 3);
            testObstaclesCombo = numericScene;

            ImGui.End();
        }
    }
}
