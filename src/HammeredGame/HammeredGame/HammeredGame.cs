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
            UIEntities.Clear();

            XMLLevelLoader levelLoader = new XMLLevelLoader($"level{levelToLoad.ToString()}.xml");

            _camera = levelLoader.GetCamera(gpu, inp);
            gameObjects = levelLoader.GetGameObjects(Content, inp, _camera);

            foreach (GameObject entity in gameObjects)
            {
                // Add all level objects with an associated UI to the list of UIs to draw in Draw()
                if (entity is IImGui imGuiAble)
                {
                    UIEntities.Add(imGuiAble);
                }

                // All objects that the player can collide with (for now, this is everything but
                // Ground) needs to be stored in activeLevelObstacles, which the Player class checks
                // for collision against.
                // TODO: this needs to change to a different implementation when collision detection changes.
                var envAble = entity as EnvironmentObject;
                if (envAble != null && entity is not Ground)
                {
                    activeLevelObstacles.Add(envAble);
                }
            }

            // The Game object itself (this class) also has an UI
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
