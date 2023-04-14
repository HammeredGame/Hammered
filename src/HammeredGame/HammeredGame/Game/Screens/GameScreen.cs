﻿using BEPUphysics.Entities;
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

namespace HammeredGame.Game.Screens
{

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

        /// <summary>
        /// Called once when loading the game. Load all assets here since it is expensive to load
        /// them on demand when we need it in e.g. Update() or Draw().
        /// </summary>
        public override void LoadContent()
        {
            base.LoadContent();

            // Load sound effects before initialising the first scene, since the scene setup
            // script might already use some of the sound effects.
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

            InitializeLevel("HammeredGame.Game.Scenes.Island1.ShoreWakeup");

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.1f;
            MediaPlayer.Play(bgMusic);

            SoundEffect.MasterVolume = 0.2f;

            // Preload the pause screen, so that adding the pause screen to the screen stack doesn't
            // call LoadContent every time (which lags because it has to loads fonts and create the
            // UI layout)
            pauseScreen = new PauseScreen();
            ScreenManager.PreloadScreen(pauseScreen);

            promptsScreen = new ControlPromptsScreen();
            ScreenManager.AddScreen(promptsScreen);
        }

        /// <summary>
        /// Relatively expensive function! Loads the XML file from disk, parses it and instantiates
        /// the level (including Camera and GameObjects like player, hammer, obstacles). Will reset
        /// all visible UI as well and show only the UIs relevant to the new objects.
        /// </summary>
        /// <param name="levelToLoad"></param>
        public void InitializeLevel(string levelToLoad)
        {
            currentScene = (Scene)Activator.CreateInstance(Type.GetType(levelToLoad), GameServices, this);
        }

        public void ShowPromptsFor(List<string> controls, CancellationToken stopToken)
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

            if (HasFocus && (input.ButtonPress(Buttons.Start) || input.KeyPress(Keys.Escape)))
            {
                pauseScreen.RestartLevelFunc = () => InitializeLevel(currentScene.GetType().FullName);
                ScreenManager.AddScreen(pauseScreen);
            }

            // Update each game object (TODO: pass HasFocus, or some way to stop responding to input
            // if screen not focused?)
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
            base.Draw(gameTime);

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
        }
    }
}