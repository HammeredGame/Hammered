using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class ShoreWakeup : Scene
    {
        public ShoreWakeup(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/ShoreWakeup.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));

            // set active camera to determine which way is forward
            Get<Player>("player1").SetActiveCamera(Camera);

            // drop hammer here to set state as Dropped, so when we set the owner player later it
            // won't fly back
            Get<Hammer>("hammer").DropHammer();

            // Show movement prompt 1 second after launch
            await Services.GetService<ScriptUtils>().WaitSeconds(1);
            CancellationTokenSource movementPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Movement }, movementPromptTokenSource.Token);

            // On entering hammer vicinity show hammer prompt
            CancellationTokenSource hammerPromptTokenSource = new();

            await Services.GetService<ScriptUtils>().WaitEvent(Get<TriggerObject>("hammer_trigger"), "OnTrigger");

            // set hammer owner so we can now summon it
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));

            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // hide movement controls
            movementPromptTokenSource.Cancel();

            // show summon controls
            ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.SummonHammer }, hammerPromptTokenSource.Token);

            // On summon, show cut scene
            // Keep track of current physics space time and camera distance
            float normalPhysicsTimeDuration = Space.TimeStepSettings.TimeStepDuration;
            float normalCameraDistance = Camera.FollowDistance;

            // Start following the hammer very closely, in very slow motion
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Hammer>("hammer"), "OnSummon");
            Space.TimeStepSettings.TimeStepDuration = normalPhysicsTimeDuration * 0.01f;
            Camera.FollowDistance = 20f;
            Get<Player>("player1").SetActiveCamera(null);
            Camera.SetFollowTarget(Get<Hammer>("hammer"));

            // After three seconds, speed up the slow motion a little bit
            await Services.GetService<ScriptUtils>().WaitSeconds(3);
            Space.TimeStepSettings.TimeStepDuration = normalPhysicsTimeDuration * 0.1f;

            // Once it reaches the player, reset to normal physics time and camera distance
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            Space.TimeStepSettings.TimeStepDuration = normalPhysicsTimeDuration;
            Camera.FollowDistance = normalCameraDistance;
            Get<Player>("player1").SetActiveCamera(Camera);
            Camera.SetFollowTarget(Get<Player>("player1"));

            // Now show prompts for dropping too
            ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.DropHammer }, hammerPromptTokenSource.Token);

            // Once the user has summoned the hammer at least once, make completion trigger
            // available to load next level. This disallows users exploring before interacting and
            // accidentally loading the next level.
            Get<TriggerObject>("end_trigger").OnTrigger += (_, _) =>
            {
                hammerPromptTokenSource.Cancel();
                ParentGameScreen.InitializeLevel(typeof(TreeTutorial).FullName);
            };

            // Also hide the prompt if it's been retrieved two more times
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            hammerPromptTokenSource.Cancel();
        }
    }
}
