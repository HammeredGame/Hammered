using BEPUphysics;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using Pleasing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class ShoreWakeup : Scene
    {
        public ShoreWakeup(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/ShoreWakeup_voxel.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            await Services.GetService<ScriptUtils>().WaitNextUpdate();

            Camera.SetFollowTarget(Get<Player>("player1"));

            // set active camera to determine which way is forward
            Get<Player>("player1").SetActiveCamera(Camera);

            // drop hammer here to set state as Dropped, so when we set the owner player later it
            // won't fly back
            Get<Hammer>("hammer").DropHammer();

            // Show a small dialogue
            await ParentGameScreen.ShowDialogueAndWait("...Where am I?");

            // Show movement prompt 1 second after launch
            await Services.GetService<ScriptUtils>().WaitSeconds(1);
            CancellationTokenSource movementPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Movement }, movementPromptTokenSource.Token);

            // On entering hammer vicinity show hammer prompt
            CancellationTokenSource hammerPromptTokenSource = new();

            await Services.GetService<ScriptUtils>().WaitEvent(Get<TriggerObject>("hammer_trigger"), "OnTrigger");

            // Disable player movement
            Get<Player>("player1").SetActiveCamera(null);
            await ParentGameScreen.ShowDialogueAndWait("...Huh, what's that over there?");
            Camera.SetFollowTarget(Get<Hammer>("hammer"));
            await ParentGameScreen.ShowDialogueAndWait("Looks like a hammer, and I feel it... calling to me.");
            await ParentGameScreen.ShowDialogueAndWait("...");
            await ParentGameScreen.ShowDialogueAndWait("Or me calling it?");

            // set hammer owner so we can now summon it
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));

            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // hide movement controls and show summoning controls
            movementPromptTokenSource.Cancel();
            ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.SummonHammer }, hammerPromptTokenSource.Token);

            // On summon, show cut scene, hiding the prompts
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Hammer>("hammer"), "OnSummon");
            hammerPromptTokenSource.Cancel();

            // Temporarily take away player controls
            Get<Player>("player1").SetActiveCamera(null);
            await SummonCutScene();
            Get<Player>("player1").SetActiveCamera(Camera);
            Camera.SetFollowTarget(Get<Player>("player1"));

            await ParentGameScreen.ShowDialogueAndWait("Sweet! A hammer I can summon!?");
            await ParentGameScreen.ShowDialogueAndWait("I've got no memory of why I'm here\nbut hey, I have a... hammer!");

            // Now show prompts for dropping too
            hammerPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(
                new List<UserAction>() { UserAction.DropHammer, UserAction.SummonHammer },
                hammerPromptTokenSource.Token);

            // Once the user has summoned the hammer at least once, make completion trigger
            // available to load next level. This disallows users exploring before interacting and
            // accidentally loading the next level.
            // Also, make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    hammerPromptTokenSource.Cancel();
                    ParentGameScreen.InitializeLevel(typeof(TreeTutorial).FullName);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let me bring it.");
                }
            };

            // Also hide the prompt if it's been retrieved two more times
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            hammerPromptTokenSource.Cancel();
        }

        /// <summary>
        /// A cut scene for summoning the hammer. Player controls should be disabled before calling
        /// this, and enabled after this. It's not included in this function to allow us to tweak
        /// when the control should be taken or given back.
        /// </summary>
        private async Task SummonCutScene()
        {
            // Keep track of current physics space time and camera distance
            float normalPhysicsTimeDuration = Space.TimeStepSettings.TimeStepDuration;
            float normalCameraDistance = Camera.FollowDistance;

            Camera.FollowDistance = 20f;

            // Interpolate the physics timestep so we go slow-mo and gradually reach normal speed again
            TweenTimeline physicsTimeInterpolation = Tweening.NewTimeline();
            physicsTimeInterpolation
                .AddFloat(Space.TimeStepSettings, nameof(Space.TimeStepSettings.TimeStepDuration))
                .AddFrame(0, 0f)
                .AddFrame(4000, normalPhysicsTimeDuration, Easing.Quadratic.In);
            physicsTimeInterpolation.Start();

            // Once it reaches the player, reset to normal physics time and camera distance
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            Space.TimeStepSettings.TimeStepDuration = normalPhysicsTimeDuration;
            Camera.FollowDistance = normalCameraDistance;
        }
    }
}
