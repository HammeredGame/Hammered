using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Screens;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TreeTutorial : Scene
    {
        public TreeTutorial(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/TreeTutorial_voxel.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            ScriptUtils utils = Services.GetService<ScriptUtils>();

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            Get<Tree>("tree").SetTreeFallen(true);

            await ParentGameScreen.ShowDialogueAndWait("Woah! Magical teleportation!?");
            await ParentGameScreen.ShowDialogueAndWait("This hammer must be mythical or something...!");

            // Show a hint for camera controls upon entering its trigger area, but set up the event
            // handler without blocking further script execution. This is so that the end trigger
            // can be activated without doing the camera actions.
            CancellationTokenSource cameraHintTokenSource = new();
            EventHandler cameraHintTriggerOnce = null;
            Get<TriggerObject>("camera_hint_trigger").OnTrigger += cameraHintTriggerOnce = async (_, _) =>
            {
                // remove the handler so we don't keep prompting repeatedly
                Get<TriggerObject>("camera_hint_trigger").OnTrigger -= cameraHintTriggerOnce;

                // Show prompts
                ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.RotateCameraLeft, UserAction.RotateCameraRight }, cameraHintTokenSource.Token);

                // Player must perform one of each action to get rid of the hint
                await Task.WhenAll(
                    utils.WaitEvent(Camera, "OnRotateLeft"),
                    utils.WaitEvent(Camera, "OnRotateRight")
                );
                cameraHintTokenSource.Cancel();
            };

            // End trigger will be active regardless of camera actions
            Get<TriggerObject>("end_trigger").OnTrigger += (_, _) =>
            {
                cameraHintTokenSource.Cancel();
                ParentGameScreen.InitializeLevel(typeof(TwoIslandPuzzle).FullName);
            };

            // Get<Player>("player").OnMove += async _ => {
            //     System.Diagnostics.Debug.WriteLine("a");
            //     services.GetService<ScriptUtils>.WaitSeconds(5);
            //     System.Diagnostics.Debug.WriteLine("written after 5 seconds of player movement");
            // };

            //Create<Player>("player", services, content.Load<Model>("character-colored"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
            //Create<Hammer>("hammer", services, content.Load<Model>("temp_hammer2"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
        }
    }
}
