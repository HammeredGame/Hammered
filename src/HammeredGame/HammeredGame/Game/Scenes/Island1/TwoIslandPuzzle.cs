using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Pleasing;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TwoIslandPuzzle : Scene
    {
        private bool openedKeyDoor = false;
        private bool withinDoorInteractTrigger = false;
        private CancellationTokenSource doorInteractTokenSource;// = new();

        public TwoIslandPuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/trees_bgm2");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }
        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island1/TwoIslandPuzzle_voxel.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            Get<Door>("door_goal").SetIsGoal(true);
            this.UpdateSceneGrid(Get<Door>("door_pp"), false);

            Get<PressurePlate>("pressureplate").SetTriggerObject(Get<Door>("door_pp"));

            Get<Key>("key").SetCorrespondingDoor(Get<Door>("door_goal"));

            CollisionGroup waterboundsGroup = new CollisionGroup();
            foreach (var gO in GameObjectsList)
            {
                // Set water bounds objects to a group (usually used to ensure that it doesn't
                // block any rocks in the scene, but here there are no rocks in the scene.
                // If there are no rocks, the water bounds still needs a collision group attached,
                // so attaching an empty collision group here)
                var waterBounds = gO as WaterBoundsObject;
                if (waterBounds != null)
                {
                    waterBounds.Entity.CollisionInformation.CollisionRules.Group = waterboundsGroup;
                }
            }

            await ParentGameScreen.ShowDialogueAndWait("I must be hallucinating...");
            await ParentGameScreen.ShowDialogueAndWait("I think I see something shining not far from here!");

            // No further initialization required for the <c>UniformGrid</c> instance.

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    // todo: have some wrapper class for mediaplayer that allows fading etc
                    float oldVolume = MediaPlayer.Volume;
                    Tweening.Tween((f) => MediaPlayer.Volume = f, MediaPlayer.Volume, 0f, 500, Easing.Linear, LerpFunctions.Float);
                    await Services.GetService<ScriptUtils>().WaitMilliseconds(300);

                    Get<Player>("player1").InputEnabled = false;
                    Get<Player>("player1").ShowVictoryStars();
                    await Services.GetService<ScriptUtils>().WaitSeconds(1);
                    ParentGameScreen.InitializeLevel(typeof(LaserTutorial).FullName, true);

                    await Services.GetService<ScriptUtils>().WaitSeconds(2);
                    Tweening.Tween((f) => MediaPlayer.Volume = f, 0f, oldVolume, 3000, Easing.Linear, LerpFunctions.Float);

                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };

            doorInteractTokenSource = new();
            Get<TriggerObject>("door_interact_trigger").OnTrigger += (_, _) =>
            {
                if (!openedKeyDoor)
                {
                    doorInteractTokenSource = new();
                    ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Interact }, doorInteractTokenSource.Token);
                    withinDoorInteractTrigger = true;
                }
            };

            Get<TriggerObject>("door_interact_trigger").OnTriggerEnd += (_, _) =>
            {
                doorInteractTokenSource.Cancel();
                withinDoorInteractTrigger = false;
            };
        }

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            // Handle Pressure Plate logic
            var pressureplate_1 = Get<PressurePlate>("pressureplate");
            if (pressureplate_1 != null)
            {
                if (pressureplate_1.IsActivated()) Get<Door>("door_pp").OpenDoor();
                else Get<Door>("door_pp").CloseDoor();
            }

            if (withinDoorInteractTrigger)
            {
                Input inp = this.Services.GetService<Input>();
                if (UserAction.Interact.Pressed(inp))
                {
                    if (Get<Key>("key").IsPickedUp())
                    {
                        Get<Door>("door_goal").OpenDoor();
                        openedKeyDoor = true;
                        doorInteractTokenSource.Cancel();
                    }
                    else
                    {
                        await ParentGameScreen.ShowDialogueAndWait("Hmm... Maybe I need something to open this?");
                    }
                }
            }
        }
    }
}
