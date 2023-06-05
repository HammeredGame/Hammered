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
using System.Runtime.ExceptionServices;

namespace HammeredGame.Game.Screens
{
    /// <summary>
    /// The game screen shows the main gameplay. It has one active scene at a time, and may add a
    /// PauseScreen to the screen stack upon pausing.
    /// </summary>
    public class GameScreen : Screen
    {
        // Current active game scene.
        public Scene CurrentScene;

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

        private readonly string scenePendingLoad;

        private GameRenderer gameRenderer;

        public GameScreen(string startScene)
        {
            // Don't load the scene yet, since it's expensive. Do it in LoadContent()
            scenePendingLoad = startScene;
        }

        /// <summary>
        /// Called when the game resolution changes. We re-create the game renderer.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public override void SetResolution(int width, int height)
        {
            base.SetResolution(width, height);

            // Re-create the game renderer since many of its internal render targets need to use the
            // correct resolution
            ContentManager Content = GameServices.GetService<ContentManager>();
            gameRenderer = new GameRenderer(GameServices.GetService<GraphicsDevice>(), Content);
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
            //bgMusic = Content.Load<Song>("Audio/BGM_V2_4x");

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

            InitializeLevel(scenePendingLoad, false);

            //MediaPlayer.IsRepeating = true;
            //MediaPlayer.Play(bgMusic);

            // Preload the pause screen, so that adding the pause screen to the screen stack doesn't
            // call LoadContent every time (which lags because it has to loads fonts and create the
            // UI layout)
            pauseScreen = new PauseScreen(this);
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
        /// <param name="resetAllCheckpoints">
        /// Whether to reset any existing checkpoints for the scene to load
        /// </param>
        public void InitializeLevel(string sceneToLoad, bool resetAllCheckpoints)
        {
            // Clear all prompts and dialogues shown on screen and remaining in queue
            promptsScreen?.ClearAllPrompts();
            dialoguesScreen?.ClearAllDialogues();

            // Reset the progress so that there isn't a flash of 100 from the previous use
            loadingScreen.ResetProgress();
            ScreenManager.AddScreen(loadingScreen);

            // Reset checkpoints for the scene we're leaving from
            CurrentScene?.CheckpointManager.ResetAllCheckpoints();

            Scene temporaryScene = (Scene)Activator.CreateInstance(Type.GetType(sceneToLoad), GameServices, this);

            temporaryScene
                // Pass a progress reporting function to retrieve the asynchronous progress
                .LoadContentAsync(progress: loadingScreen.ReportProgress)
                .ContinueWith(async t =>
                {
                    // Make sure we run in the next update synchronously since we will be
                    // overwriting currentScene and also removing the loading screen (which we don't
                    // want to do in a Draw). This is necessary because .ContinueWith can run in an
                    // asynchronous thread of its own.
                    await GameServices.GetService<ScriptUtils>().WaitNextUpdate();

                    CurrentScene = temporaryScene;

                    // Delete any existing checkpoints for the scene we're loading. We'd like this
                    // happen if we're loading the next scene as part of a playthrough, but if we're
                    // continuing a previous checkpoint, we definitely wouldn't want to reset it.
                    if (resetAllCheckpoints)
                    {
                        CurrentScene.CheckpointManager.ResetAllCheckpoints();
                    }

                    // Apply any checkpoint for the scene we're loading anew
                    CurrentScene.CheckpointManager.ApplyLastCheckpoint();

                    // If the LoadContentAsync failed with an exception, it will get silently
                    // swallowed unless we check for it here. We can't re-throw it, since
                    // ContinueWith is still in a different thread and its exceptions will still not
                    // propagate to the main thread. So we set a field on the scene that we'll read
                    // out of later.
                    if (t.IsFaulted)
                    {
                        CurrentScene.LoadError = t.Exception.InnerException;
                    }

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
        /// Show a dialogue that must be cleared with <see cref="ClearAllDialogues"/>. It
        /// force-displays over any previously ongoing dialogues. If the argument string is
        /// null, the current dialogue will be cleared.
        /// </summary>
        /// <param name="dialogue"></param>
        public void ShowUnskippableDialogue(string dialogue)
        {
            dialoguesScreen.ShowUnskippableDialogue(dialogue);
        }

        /// <summary>
        /// Called on every game update loop. The interval at this function is called is not
        /// constant, so use the gameTime argument to make sure speeds appear natural.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (CurrentScene == null) return;

            // Rethrow the asynchronous load exception if it exists
            if (CurrentScene.LoadError != null)
            {
                ExceptionDispatchInfo.Capture(CurrentScene.LoadError).Throw();
            }

            if (!CurrentScene.IsLoaded) return;

            Input input = GameServices.GetService<Input>();

            if (HasFocus && UserAction.Pause.Pressed(input))
            {
                isPaused = true;
                pauseScreen.OnExit = () => isPaused = false;
                ScreenManager.AddScreen(pauseScreen);
            }

            CurrentScene.Update(gameTime, HasFocus, isPaused);
        }

        /// <summary>
        /// Called on each game loop after Update(). Should not contain expensive computation but
        /// rather just rendering and drawing to the GPU. Shader effects are done here.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (CurrentScene == null || !CurrentScene.IsLoaded) return;

            gameRenderer.DrawScene(gameTime, CurrentScene);
            gameRenderer.PostProcess();
            gameRenderer.CopyOutputTo(ScreenManager.MainRenderTarget);
        }

        /// <summary>
        /// Debug UI for the game screen.
        /// </summary>
        public override void UI()
        {
            if (CurrentScene == null) return;

            // Show a scene switcher dropdown, with the list of all scene class names in this assembly
            ImGui.Text("Current Loaded Scene: ");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##scene", CurrentScene.GetType().Name))
            {
                foreach (string fqn in Scene.GetAllSceneFQNs())
                {
                    if (ImGui.Selectable(fqn, fqn == CurrentScene.GetType().FullName))
                    {
                        Task.Factory.StartNew(async () =>
                        {
                            await GameServices.GetService<ScriptUtils>().WaitNextUpdate();
                            InitializeLevel(fqn, true);
                        });
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.Separator();

            // Show the scene's UI within the same window
            CurrentScene.UI();

            ImGui.Begin("Graphics");
            gameRenderer.UI();
            ImGui.End();
        }
    }
}
