using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TwoIslandPuzzle : Scene
    {
        private bool withinDoorInteractTrigger = false;

        public TwoIslandPuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/TwoIslandPuzzle_voxel.xml");
            OnSceneStart();
        }

        protected override void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            Get<Door>("door_goal").SetIsGoal(true);
            this.UpdateSceneGrid(Get<Door>("door_pp"), false);

            Get<PressurePlate>("pressureplate").SetTriggerObject(Get<Door>("door_pp"));

            Get<Key>("key").SetCorrespondingDoor(Get<Door>("door_goal"));

            // No further initialization required for the <c>UniformGrid</c> instance.

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    await ParentGameScreen.ShowDialogueAndWait("(You found polaroid photos of yourself together with\na handsome god-like man)");
                    await ParentGameScreen.ShowDialogueAndWait("(The man is holding a hammer that looks just like\nthe one in your hand)");
                    await ParentGameScreen.ShowDialogueAndWait("Is that... Thor? And this... his hammer?\nI need to give it back to him!");
                    ParentGameScreen.InitializeLevel(typeof(LaserTutorial).FullName);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };

            CancellationTokenSource doorInteractTokenSource = new();
            Get<TriggerObject>("door_interact_trigger").OnTrigger += async (_, _) =>
            {
                doorInteractTokenSource = new();
                ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Interact }, doorInteractTokenSource.Token);
                withinDoorInteractTrigger = true;
            };

            Get<TriggerObject>("door_interact_trigger").OnTriggerEnd += async (_, _) =>
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
