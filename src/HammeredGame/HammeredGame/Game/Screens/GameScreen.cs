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
using System.Threading.Tasks;
using System.IO;

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
        private bool isPaused;

        private ControlPromptsScreen promptsScreen;
        private OnScreenDialogueScreen dialoguesScreen;
        private LoadingScreen loadingScreen;

        // Music variables
        private Song bgMusic;
        //private AudioListener listener = new AudioListener();
        //private AudioEmitter emitter = new AudioEmitter();

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
            //List<SoundEffect> sfx = GameServices.GetService<List<SoundEffect>>();
            bgMusic = Content.Load<Song>("Audio/BGM_V2_4x");

            //List<SoundEffect> sfx = GameServices.GetService<List<SoundEffect>>();
            //sfx.Add(Content.Load<SoundEffect>("Audio/step"));
            //sfx.Add(Content.Load<SoundEffect>("Audio/hammer_drop"));
            //sfx.Add(Content.Load<SoundEffect>("Audio/lohi_whoosh"));
            //sfx.Add(Content.Load<SoundEffect>("Audio/tree_fall"));
            //sfx.Add(Content.Load<SoundEffect>("Audio/ding"));
            //sfx.Add(Content.Load<SoundEffect>("Audio/door_open"));
            //sfx.Add(Content.Load<SoundEffect>("Audio/door_close"));

            loadingScreen = new LoadingScreen();
            ScreenManager.PreloadScreen(loadingScreen);

            InitializeLevel(currentSceneName);

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.1f;
            MediaPlayer.Play(bgMusic);

            // Preload the pause screen, so that adding the pause screen to the screen stack doesn't
            // call LoadContent every time (which lags because it has to loads fonts and create the
            // UI layout)
            pauseScreen = new PauseScreen
            {
                QuitMethod = () =>
                {
                    #region TEMPORARY SOLUTION TO CONTINUE/NEW GAME BEFORE SETTINGS AND PERSISTENT DATA IS IMPLEMENTED
                    try
                    {
                        File.WriteAllText("save.txt", currentSceneName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                    #endregion

                    // Specify the callback function when Quit To Title is called. We also need to
                    // specify the Restart Level callback, but this is done just before each time the
                    // screen is added to the manager, since we need the name of the currently active level.
                    ExitScreen(true);
                    // Ask the main game class to recreate the title screen, since it needs to
                    // assign handlers that we don't have access to
                    GameServices.GetService<HammeredGame>().InitTitleScreen();
                }
            };
            ScreenManager.PreloadScreen(pauseScreen);

            promptsScreen = new ControlPromptsScreen();
            ScreenManager.AddScreen(promptsScreen);

            dialoguesScreen = new OnScreenDialogueScreen();
            ScreenManager.AddScreen(dialoguesScreen);
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
            dialoguesScreen?.ExitScreen();
            loadingScreen?.ExitScreen();

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
            // Clear all prompts and dialogues shown on screen and remaining in queue
            promptsScreen?.ClearAllPrompts();
            dialoguesScreen?.ClearAllDialogues();

            currentSceneName = sceneToLoad;

            // Reset the progress so that there isn't a flash of 100 from the previous use
            loadingScreen.ResetProgress();
            ScreenManager.AddScreen(loadingScreen);
            Scene temporaryScene = (Scene)Activator.CreateInstance(Type.GetType(sceneToLoad), GameServices, this);

            temporaryScene
                // Pass a progress reporting function to retrieve the asynchronous progress
                .LoadContentAsync(progress: loadingScreen.ReportProgress)
                .ContinueWith(_ => {
                    // Make sure we run in the next update synchronously since we will be
                    // overwriting currentScene and also removing the loading screen (which we don't
                    // want to do in a Draw). This is necessary because .ContinueWith can run in an
                    // asynchronous thread of its own.
                    GameServices.GetService<ScriptUtils>().WaitNextUpdate();

                    currentScene = temporaryScene;
                    loadingScreen.ExitScreen(false);
                });
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
        /// Show a dialogue, see OnScreenDialogueScreen.ShowDialogue() for more info.
        /// </summary>
        /// <param name="dialogue"></param>
        public Task ShowDialogueAndWait(string dialogue)
        {
            return dialoguesScreen.ShowDialogueAndWait(dialogue);
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (currentScene == null || !currentScene.IsLoaded) return;

            Input input = GameServices.GetService<Input>();

            if (HasFocus && UserAction.Pause.Pressed(input))
            {
                isPaused = true;
                pauseScreen.RestartMethod = () =>
                {
                    isPaused = false;
                    InitializeLevel(currentSceneName);
                };
                pauseScreen.ContinueMethod = () => isPaused = false;
                ScreenManager.AddScreen(pauseScreen);
            }

            currentScene.Update(gameTime, HasFocus, isPaused);
        }

        /// <summary>
        /// Called on each game loop after Update(). Should not contain expensive computation but
        /// rather just rendering and drawing to the GPU. Shader effects are done here.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (currentScene == null) return;

            gameRenderer.DrawScene(gameTime, currentScene);
            gameRenderer.PostProcess();
            gameRenderer.CopyOutputTo(ScreenManager.MainRenderTarget);
        }

        /// <summary>
        /// Debug UI for the game screen.
        /// </summary>
        public override void UI()
        {
            if (currentScene == null) return;

            // Show a scene switcher dropdown, with the list of all scene class names in this assembly
            ImGui.Text("Current Loaded Scene: ");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##scene", currentScene.GetType().Name))
            {
                foreach (string fqn in Scene.GetAllSceneFQNs())
                {
                    if (ImGui.Selectable(fqn, fqn == currentScene.GetType().FullName))
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await GameServices.GetService<ScriptUtils>().WaitNextUpdate();
                            InitializeLevel(fqn);
                        });
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
