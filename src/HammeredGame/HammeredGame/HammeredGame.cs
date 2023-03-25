using HammeredGame.Core;
using HammeredGame.Game;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.GroundObjects;
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
        private FloorObject _ground;
        private FloorObject _water;
        private List<GameObject> gameObjects;

        private Key _key;

        static public List<EnvironmentObject> activeLevelObstacles = new List<EnvironmentObject>();

        // SCENE TEST VARIABLES
        private int testObstaclesCombo = 3;

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
            _font = Content.Load<SpriteFont>("temp_font");

            var levCon = new XMLLevelLoader("level1.xml");

            _camera = levCon.GetCamera(gpu, inp);

            gameObjects = levCon.GetGameObjects(Content, inp, _camera);

            foreach (GameObject entity in gameObjects)
            {
                var imGuiAble = entity as IImGui;
                if (imGuiAble != null)
                {
                    UIEntities.Add(imGuiAble);
                }

                var envAble = entity as EnvironmentObject;
                if (envAble != null)
                {
                    if (entity is not Ground)
                    {
                        activeLevelObstacles.Add(envAble);
                    }
                }
            }
            UIEntities.Add(this);

            //initializeLevel();
        }

        private void initializeLevel()
        {
            // Load Texture
            playerTex = Content.Load<Texture2D>("Temp");

            _font = Content.Load<SpriteFont>("temp_font");

            //(TEMPORARY - after xml parsing and incorporating better collision detection, all of this should change)
            // Load and initialize player character
            _player = new Player(Content.Load<Model>("character_3"), Vector3.Zero, 0.03f, playerTex, inp, _camera);

            // Load and initialize hammer object
            _hammer = new Hammer(Content.Load<Model>("temp_hammer2"), Vector3.Zero, 0.02f, null, inp, _player);

            // Load and initialize the terrain/ground
            _ground = new Ground(Content.Load<Model>("temp_floor_with_biggerhole"), new Vector3(0, 0f, 0), 0.02f,  null);

            _water = new Water(Content.Load<Model>("test_water_bigger"), new Vector3(5.0f, 0.0f, -80.0f), 0.03f, null);

            // Initialize list of gameobjects for drawing
            gameObjects = new List<GameObject> { _player, _hammer, _ground, _water };

            // Load obstacles for testing
            switch (testObstaclesCombo)
            {
                case 0:
                    {
                        // 2 breakable objects and 1 unbreakable object
                        EnvironmentObject Obstacle1 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, null);
                        EnvironmentObject Obstacle2 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 1f, 10f), 0.02f, null);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, null);
                        activeLevelObstacles = new List<EnvironmentObject> { _water, Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
                case 1:
                    {
                        // 1 Door, 1 Pressure plate, 1 unbreakable object
                        EnvironmentObject Obstacle1 = new Door(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, null);
                        EnvironmentObject Obstacle2 = new PressurePlate(Content.Load<Model>("temp_pressureplate2"), new Vector3(-10f, 0f, 10f), 0.02f, null, Obstacle1);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, null);
                        activeLevelObstacles = new List<EnvironmentObject> { _water, Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
                case 2:
                    {
                        // 1 Door, 1 Key, 1 Unbreakable object
                        EnvironmentObject Obstacle1 = new Door(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, null);
                        _key = new Key(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 1f, 10f), 0.01f, null, (Door)Obstacle1);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, null);
                        activeLevelObstacles = new List<EnvironmentObject> { _water, Obstacle1, _key, Obstacle3 };
                        break;
                    }
                case 3:
                    {
                        // 1 Tree, 1 breakable, 1 unbreakable
                        EnvironmentObject Obstacle1 = new Tree(Content.Load<Model>("temp_tree2"), new Vector3(-32f, 1.0f, -20f), 0.04f, null);
                        EnvironmentObject Obstacle2 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 0f, 10f), 0.02f, null);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, null);
                        activeLevelObstacles = new List<EnvironmentObject> { Obstacle1, Obstacle2, Obstacle3, _water };
                        break;
                    }
                default:
                    {
                        // Default = Scene 0
                        EnvironmentObject Obstacle1 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(10f, 1f, -30f), 0.02f, null);
                        EnvironmentObject Obstacle2 = new BreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(-10f, 1f, 10f), 0.02f, null);
                        EnvironmentObject Obstacle3 = new UnbreakableObstacle(Content.Load<Model>("test_obstacle"), new Vector3(20f, 1f, -10f), 0.02f, null);
                        activeLevelObstacles = new List<EnvironmentObject> { _water, Obstacle1, Obstacle2, Obstacle3 };
                        break;
                    }
            }

            gameObjects.AddRange(activeLevelObstacles);

            // for now, add a temporary UI with the Player class debug info
            UIEntities.Add(_player);
            UIEntities.Add(_water);
            UIEntities.Add(this);
        }

        protected override void Update(GameTime gameTime)
        {
            // Update input
            inp.Update();
            // Check for exit input
            if (inp.back_down || inp.KeyDown(Keys.Escape)) Exit();

            if (inp.ButtonPress(Buttons.Y) || inp.KeyDown(Keys.R))
            {
                UIEntities.Remove(_player);
                UIEntities.Remove(_water);
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
            ImGui.Begin("Hammered", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing);

            ImGui.DragInt("Scene", ref testObstaclesCombo, 0.1f, 0, 3);
            ImGui.Text($"Camera Coordinates: {_camera.Position.ToString()}");
            ImGui.Text($"Camera Focus: {_camera.Target.ToString()}");
            ImGui.Text($"Loaded objects: {gameObjects.Count().ToString()}");
            ImGui.End();
        }
    }
}
