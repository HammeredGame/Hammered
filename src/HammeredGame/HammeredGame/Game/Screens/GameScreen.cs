using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using HammeredGame.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using System.Threading;
using HammeredGame.Graphics;
using ImMonoGame.Thing;

namespace HammeredGame.Game.Screens
{
    public struct Light
    {
        public Vector3 Position;
        public Vector3 Direction;
        public Vector3 Normal;

        public Light(Vector3 lp, Vector3 ld, Vector3 up) : this()
        {
            this.Position = lp;
            this.Direction = ld;
            this.Normal = up;
        }
    }

    /// <summary>
    /// The game screen shows the main gameplay. It has one active scene at a time, and may add a
    /// PauseScreen to the screen stack upon pausing.
    /// </summary>
    public class GameScreen : Screen
    {
        // Current active game scene.
        private Scene currentScene;

        // Pause screen is always loaded, see LoadContent().
        private PauseScreen pauseScreen;

        private ControlPromptsScreen promptsScreen;

        // Music variables
        private Song bgMusic;
        private AudioListener listener = new AudioListener();
        private AudioEmitter emitter = new AudioEmitter();

        // Bounding Volume debugging variables
        private bool drawBounds = false;

        private List<EntityDebugDrawer> debugEntities = new();

        // Uniform Grid debugging variables
        private bool drawGrid = false;
        private List<GridDebugDrawer> debugGridCells = new();

        private string currentSceneName;

        private GameRenderer gameRenderer;

        public GameScreen(string startScene)
        {
            // Don't load the scene yet, since it's expensive. Do it in LoadContent()
            currentSceneName = startScene;
        }

        /// <summary>
        /// Called once when loading the game. Load all assets here since it is expensive to load
        /// them on demand when we need it in e.g. Update() or Draw().
        /// </summary>
        public override void LoadContent()
        {
            base.LoadContent();

            ContentManager Content = GameServices.GetService<ContentManager>();

            gameRenderer = new GameRenderer(GameServices.GetService<GraphicsDevice>(), Content);

            // Load sound effects before initialising the first scene, since the scene setup
            // script might already use some of the sound effects.
            List<SoundEffect> sfx = GameServices.GetService<List<SoundEffect>>();
            bgMusic = Content.Load<Song>("Audio/BGM_V2_4x");
            sfx.Add(Content.Load<SoundEffect>("Audio/step"));
            sfx.Add(Content.Load<SoundEffect>("Audio/hammer_drop"));
            sfx.Add(Content.Load<SoundEffect>("Audio/lohi_whoosh"));
            sfx.Add(Content.Load<SoundEffect>("Audio/tree_fall"));
            sfx.Add(Content.Load<SoundEffect>("Audio/ding"));
            sfx.Add(Content.Load<SoundEffect>("Audio/door_open"));
            sfx.Add(Content.Load<SoundEffect>("Audio/door_close"));

            InitializeLevel(currentSceneName);

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.1f;
            MediaPlayer.Play(bgMusic);

            SoundEffect.MasterVolume = 0.2f;

            // Preload the pause screen, so that adding the pause screen to the screen stack doesn't
            // call LoadContent every time (which lags because it has to loads fonts and create the
            // UI layout)
            pauseScreen = new PauseScreen() {
                // Specify the callback function when Quit To Title is called. We also need to
                // specify the Restart Level callback, but this is done just before each time the
                // screen is added to the manager, since we need the name of the currently active level.
                QuitToTitleFunc = () => {
                    ExitScreen(true);
                    // Ask the main game class to recreate the title screen, since it needs to
                    // assign handlers that we don't have access to
                    GameServices.GetService<HammeredGame>().InitTitleScreen();
                }
            };
            ScreenManager.PreloadScreen(pauseScreen);

            promptsScreen = new ControlPromptsScreen();
            ScreenManager.AddScreen(promptsScreen);
        }

        /// <summary>
        /// Called when the scene is exiting. Should be used to dispose any assets that were loaded
        /// manually or take a lot of memory.
        /// </summary>
        public override void UnloadContent()
        {
            base.UnloadContent();

            // Make sure we have exited screens that we created.
            pauseScreen?.ExitScreen();
            promptsScreen?.ExitScreen();

            MediaPlayer.Stop();
        }

        /// <summary>
        /// Relatively expensive function! Loads the XML file from disk, parses it and instantiates
        /// the level (including Camera and GameObjects like player, hammer, obstacles). Will reset
        /// all visible UI as well and show only the UIs relevant to the new objects.
        /// </summary>
        /// <param name="sceneToLoad"></param>
        public void InitializeLevel(string sceneToLoad)
        {
            // Clear all prompts shown on screen
            promptsScreen?.ClearAllPrompts();

            currentSceneName = sceneToLoad;
            currentScene = (Scene)Activator.CreateInstance(Type.GetType(sceneToLoad), GameServices, this);

            //// Set up the list of debug grid cells for debugging visualization
            //// WARNING: Execute the following line of code if you wish to initialize the grid once.
            //// Suggested for when AVAILABLE grid cells are shown in debug mode.
            //SetupDebugGrid();
        }

        /// <summary>
        /// Show input prompts for controls, see ControlPromptsScreen.ShowPromptsFor() for more info.
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="stopToken"></param>
        public void ShowPromptsFor(List<UserAction> controls, CancellationToken stopToken)
        {
            promptsScreen.ShowPromptsFor(controls, stopToken);
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Input input = GameServices.GetService<Input>();

            if (HasFocus && UserAction.Pause.Pressed(input))
            {
                pauseScreen.RestartLevelFunc = () => InitializeLevel(currentSceneName);
                ScreenManager.AddScreen(pauseScreen);
            }

            // Update each game object
            foreach (GameObject gameObject in currentScene.GameObjectsList)
            {
                gameObject.Update(gameTime, HasFocus);
            }

            // Update camera
            currentScene.Camera.UpdateCamera(HasFocus);

            //Steps the simulation forward one time step.
            // TODO: perhaps this shouldn't update if it's paused (i.e. check for HasFocus)?
            currentScene.Space.Update();

            // Set up the list of debug entities for debugging visualization
            SetupDebugBounds();
            // Set up the list of debug grid cells for debugging visualization
            // WARNING: Execute the following line of code if you wish to update the grid at each frame.
            // Suggested for when NON available grid cells are shown.
            SetupDebugGrid();
        }

        /// <summary>
        /// Called on each game loop after Update(). Should not contain expensive computation but
        /// rather just rendering and drawing to the GPU. Shader effects are done here.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            gameRenderer.DrawScene(currentScene);
            gameRenderer.PostProcess();
            gameRenderer.CopyOutputTo(ScreenManager.MainRenderTarget);
        }

        // Prepare the entities for debugging visualization
        private void SetupDebugBounds()
        {
            debugEntities.Clear();
            var CubeModel = GameServices.GetService<ContentManager>().Load<Model>("cube");
            //Go through the list of entities in the space and create a graphical representation for them.
            foreach (Entity e in currentScene.Space.Entities)
            {
                Box box = e as Box;
                if (box != null) //This won't create any graphics for an entity that isn't a box since the model being used is a box.
                {
                    BEPUutilities.Matrix scaling = BEPUutilities.Matrix.CreateScale(box.Width, box.Height, box.Length); //Since the cube model is 1x1x1, it needs to be scaled to match the size of each individual box.
                    EntityDebugDrawer model = new EntityDebugDrawer(e, CubeModel, scaling);
                    //Add the drawable game component for this entity to the game.
                    debugEntities.Add(model);
                }
            }
        }

        // Prepare the grid cells for debugging visualization
        private void SetupDebugGrid()
        {
            debugGridCells.Clear();
            var CubeModel = GameServices.GetService<ContentManager>().Load<Model>("cube");
            //Go through the list of entities in the space and create a graphical representation for them.
            float sideLength = this.currentScene.Grid.sideLength;
            Matrix scaling = Matrix.CreateScale(sideLength);

            int[] gridDimensions = this.currentScene.Grid.GetDimensions();
            for (int i = 0; i < gridDimensions[0]; ++i)
            {
                for (int j = 0; j < gridDimensions[1]; ++j)
                {
                    for (int k = 0; k < gridDimensions[2]; ++k)
                    {
                        if (!this.currentScene.Grid.mask[i, j, k])
                        {
                            Vector3 gridcell = this.currentScene.Grid.grid[i, j, k];
                            GridDebugDrawer gdd = new GridDebugDrawer(CubeModel, gridcell, scaling);
                            debugGridCells.Add(gdd);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Debug UI for the game screen.
        /// </summary>
        public override void UI()
        {
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
            ImGui.Separator();

            // Show the scene's UI within the same window
            currentScene.UI();

            ImGui.Begin("Graphics");
            gameRenderer.UI();
            ImGui.End();
        }
    }
}
