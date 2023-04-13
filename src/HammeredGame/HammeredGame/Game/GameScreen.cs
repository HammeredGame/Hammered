using HammeredGame.Core;
using HammeredGame.Game;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;
using BEPUphysics.Entities;
using System;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

namespace HammeredGame
{
    public class GameScreen : Screen
    {
        // RENDER TARGET
        private Scene currentScene;

        // Music variables
        private Song bgMusic;
        private AudioListener listener = new AudioListener();
        private AudioEmitter emitter = new AudioEmitter();

        // Bounding Volume debugging variables
        private bool drawBounds = false;
        private List<EntityDebugDrawer> debugEntities = new();

        /// <summary>
        /// Called once when loading the game. Load all assets here since it is expensive to load
        /// them on demand when we need it in e.g. Update() or Draw().
        /// </summary>
        public override void LoadContent()
        {
            InitializeLevel("HammeredGame.Game.Scenes.Island1.ShoreWakeup");

            ContentManager Content = GameServices.GetService<ContentManager>();
            List<SoundEffect> sfx = GameServices.GetService<List<SoundEffect>>();
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
        private void InitializeLevel(string levelToLoad)
        {
            currentScene = (Scene)Activator.CreateInstance(Type.GetType(levelToLoad), GameServices);
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            Input input = GameServices.GetService<Input>();

            if (!otherScreenHasFocus && (input.ButtonPress(Buttons.Start) || input.KeyPress(Keys.Escape)))
            {
                ScreenManager.AddScreen(new PauseScreen(() => InitializeLevel(currentScene.GetType().FullName)));
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
        }

        /// <summary>
        /// Adapted from AlienScribble Make 3D Games with Monogame playlist: https://www.youtube.com/playlist?list=PLG6XrMFqMJUBOPVTJrGJnIDDHHF1HTETc
        /// <para/>
        /// To set state variables within graphics device back to default (in case they are changed
        /// at any point) to ensure we are correctly drawing in 3D space
        /// </summary>
        private void Set3DStates()
        {
            GraphicsDevice gpu = ScreenManager.GraphicsDevice;
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
        public override void Draw(GameTime gameTime)
        {
            Set3DStates();

            GraphicsDevice gpu = GameServices.GetService<GraphicsDevice>();
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

        public override void UI()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 500), ImGuiCond.FirstUseEver);
            ImGui.Begin("Hammered");

            // Show whether the gamepad is detected
            if (GameServices.GetService<Input>().GamePadState.IsConnected)
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
