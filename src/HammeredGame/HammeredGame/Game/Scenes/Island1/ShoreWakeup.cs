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
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class ShoreWakeup : Scene
    {
        public ShoreWakeup(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/shore_bgm2");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island1/ShoreWakeup_voxel.xml", progress);
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

            // Disable player input until after the wake up animation has played
            Get<Player>("player1").InputEnabled = false;
            await WakeupCutScene();

            // Show a small dialogue
            ParentGameScreen.ShowUnskippableDialogue("...Where am I?");
            await Services.GetService<ScriptUtils>().WaitSeconds(3);
            ParentGameScreen.ShowUnskippableDialogue(null);
            
            await PhoneCutScene();

            // Enable player input now that the wake up animation has finished playing
            Get<Player>("player1").InputEnabled = true;

            // Show movement prompt 1 second after launch
            await Services.GetService<ScriptUtils>().WaitSeconds(1);
            CancellationTokenSource movementPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(
                new List<UserAction>() { UserAction.Movement, UserAction.CameraMovement },
                movementPromptTokenSource.Token);

            // On entering hammer vicinity show hammer prompt
            await Services.GetService<ScriptUtils>().WaitEvent(Get<TriggerObject>("hammer_trigger"), "OnTrigger");

            // Disable player movement
            //Get<Player>("player1").SetActiveCamera(null);
            Get<Player>("player1").InputEnabled = false;
            Get<Hammer>("hammer").InputEnabled = false;
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

            CancellationTokenSource hammerPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.SummonHammer }, hammerPromptTokenSource.Token);

            // On summon, show cut scene, hiding the prompts
            Get<Hammer>("hammer").InputEnabled = true;
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Hammer>("hammer"), "OnSummon");
            hammerPromptTokenSource.Cancel();

            // Temporarily take away player controls
            //Get<Player>("player1").SetActiveCamera(null);
            Get<Player>("player1").InputEnabled = false;
            await SummonCutScene();
            //Get<Player>("player1").SetActiveCamera(Camera);
            Get<Player>("player1").InputEnabled = true;
            Camera.SetFollowTarget(Get<Player>("player1"));

            //Get<Hammer>("hammer").InputEnabled = false;
            //await ParentGameScreen.ShowDialogueAndWait("Sweet! A hammer I can summon!?");
            //await ParentGameScreen.ShowDialogueAndWait("I've got no memory of why I'm here but hey,\nI have a... hammer!");
            await ParentGameScreen.ShowDialogueAndWait("Oh, a sticky note!");
            await ParentGameScreen.ShowDialogueAndWait("\"Hey friend,");
            await ParentGameScreen.ShowDialogueAndWait("Hope you’re not too hungover from the party last night!");
            await ParentGameScreen.ShowDialogueAndWait("So… I think I accidentally left my hammer behind,");
            await ParentGameScreen.ShowDialogueAndWait("would you mind bringing it back to my temple?");
            //await ParentGameScreen.ShowDialogueAndWait("Hope you’re not so hungover to forget our little bet from the part last night :) /n I’ll be waiting for you and my hammer at my temple, have fun!");
            //await ParentGameScreen.ShowDialogueAndWait("You might be wondering how you ended up stranded on/n a beach accompanied by my pet hammer, Mjölnir.");
            //await ParentGameScreen.ShowDialogueAndWait("As per last night’s honorable drinking duel, which I /n double handedly –one hand per glass- won, you have/n to make it all the way back to my summer temple.");
            //await ParentGameScreen.ShowDialogueAndWait("(Me and the boys are currently having a /n meeting about a “Rag a rock” or something.)");
            await ParentGameScreen.ShowDialogueAndWait("See you soon! XOXO Thor\"");
            await ParentGameScreen.ShowDialogueAndWait("\"P.S. Don’t worry, the hammer isn’t that heavy!");
            await ParentGameScreen.ShowDialogueAndWait("it weighs less than what you drank last night ;)\"");

            //Get<Hammer>("hammer").InputEnabled = true;

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

                    // todo: have some wrapper class for mediaplayer that allows fading etc
                    float oldVolume = MediaPlayer.Volume;
                    Tweening.Tween((f) => MediaPlayer.Volume = f, MediaPlayer.Volume, 0f, 500, Easing.Linear, LerpFunctions.Float);
                    await Services.GetService<ScriptUtils>().WaitMilliseconds(300);

                    Get<Player>("player1").InputEnabled = false;
                    Get<Player>("player1").ShowVictoryStars();
                    await Services.GetService<ScriptUtils>().WaitSeconds(1);
                    ParentGameScreen.InitializeLevel(typeof(TreeTutorial).FullName, true);

                    await Services.GetService<ScriptUtils>().WaitSeconds(2);
                    Tweening.Tween((f) => MediaPlayer.Volume = f, 0f, oldVolume, 3000, Easing.Linear, LerpFunctions.Float);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
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

        private async Task WakeupCutScene()
        {
            float originalDistance = Camera.FollowDistance;
            Camera.FollowDistance = 60f;
            // Trigger player´s wake up animation
            Get<Player>("player1").TriggerWakeUp();

            // Wait until player is up to start prompts
            await Services.GetService<ScriptUtils>().WaitMilliseconds((int)Get<Player>("player1").Animations.CurrentClip.Duration.TotalMilliseconds - 200);
            Camera.FollowDistance = originalDistance;
        }

        private async Task PhoneCutScene()
        {
            float originalDistance = Camera.FollowDistance;
            Camera.FollowDistance = 60f;
            // Trigger player´s phone animation
            Get<Player>("player1").TriggerPhone();
            
            ParentGameScreen.ShowUnskippableDialogue("Hey, it's me, Thor...");
            await Services.GetService<ScriptUtils>().WaitSeconds(3);
            ParentGameScreen.ShowUnskippableDialogue("I can't find my Hammer...I think I forgot it on the boat last night.");
            await Services.GetService<ScriptUtils>().WaitSeconds(3);
            ParentGameScreen.ShowUnskippableDialogue("Would you bring it back?");
            await Services.GetService<ScriptUtils>().WaitSeconds(3);
            ParentGameScreen.ShowUnskippableDialogue("Don't worry, it's really not THAT heavy...");
            await Services.GetService<ScriptUtils>().WaitSeconds(3);
            ParentGameScreen.ShowUnskippableDialogue(null);

            Get<Player>("player1").TriggerEndPhone();

            Camera.FollowDistance = originalDistance;
        }
    }
}
