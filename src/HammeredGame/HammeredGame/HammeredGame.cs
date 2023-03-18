using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
//using System.Numerics;

namespace HammeredGame
{
    public class HammeredGame : Game
    {
        // DISPLAY
        const int SCREENWIDTH = 1280;
        const int SCREENHEIGHT = 720;
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _device;
        private GraphicsDevice gpu;
        private Effect _effect;
        static public int screenW, screenH;
        Camera _camera;

        // INPUT and other related stuff
        Input inp;

        // RENDER TARGETS & TEXTURES
        RenderTarget2D MainTarget;
        //Texture2D test_tex;

        // RECTANGLES WOOOO
        Rectangle desktopRect;
        Rectangle screenRect;

        // 3D Objects and other related stuff
        private Player _player;
        private Hammer _hammer;
        private Texture2D playerTex;
        private WorldObject _ground;
        private List<GameObject> gameObjects;


        public HammeredGame()
        {
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
            // TODO: Add your initialization logic here
            gpu = GraphicsDevice;
            PresentationParameters pp = gpu.PresentationParameters;
            _spriteBatch = new SpriteBatch(gpu);
            MainTarget = new RenderTarget2D(gpu, SCREENWIDTH, SCREENHEIGHT, false, pp.BackBufferFormat, DepthFormat.Depth24);
            screenW = MainTarget.Width;
            screenH = MainTarget.Height;
            desktopRect = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            screenRect = new Rectangle(0, 0, screenW, screenH);

            // Input
            inp = new Input(pp, MainTarget);

            //_player = new Player(Content.Load<Model>("placeholder_character2"), Vector3.Zero, 1.0f, inp, _camera);
            _camera = new Camera(gpu, Vector3.Down, inp);
            
            Window.Title = "HAMMERED";

            base.Initialize();
        }

        protected override void LoadContent()
        {
            playerTex = Content.Load<Texture2D>("Temp");
            _player = new Player(Content.Load<Model>("character_test"), Vector3.Zero, 1.5f, inp, _camera, playerTex);
            _hammer = new Hammer(Content.Load<Model>("temp_hammer"), Vector3.Zero, 1.5f, _player, inp, _camera, null);
            _ground = new WorldObject(Content.Load<Model>("temp_floor"), new Vector3(0, 20f, 0), 25f, inp, _camera, playerTex);


            gameObjects = new List<GameObject> { _player, _hammer, _ground };
        }

        protected override void Update(GameTime gameTime)
        {
            inp.Update();
            if (inp.back_down || inp.KeyDown(Keys.Escape)) Exit();
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.Update(gameTime);
            }
            _camera.UpdateCamera();

            base.Update(gameTime);
        }

        void Set3DStates()
        {
            gpu.BlendState = BlendState.NonPremultiplied;
            gpu.DepthStencilState = DepthStencilState.Default;
            if (gpu.RasterizerState.CullMode == CullMode.None)
            {
                RasterizerState rs = new RasterizerState { CullMode = CullMode.CullCounterClockwiseFace };
                gpu.RasterizerState = rs;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            gpu.SetRenderTarget(MainTarget);
            gpu.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.AliceBlue, 1.0f, 0);
            Set3DStates();
            //gpu.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            // Render Scene objects
            foreach (GameObject gameObject in gameObjects)
            {
                gameObject.Draw(_camera.view, _camera.proj);
            }

            // Draw MainTarget to BackBuffer
            gpu.SetRenderTarget(null);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone);
            _spriteBatch.Draw(MainTarget, desktopRect, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}