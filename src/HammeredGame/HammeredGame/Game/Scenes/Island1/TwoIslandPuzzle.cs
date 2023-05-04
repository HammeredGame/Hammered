using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TwoIslandPuzzle : Scene
    {
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

            this.UpdateSceneGrid(Get<Wall>("wall_1"), false);

            // No further initialization required for the <c>UniformGrid</c> instance.

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    ParentGameScreen.InitializeLevel(typeof(LaserTutorial).FullName);
                }
            };

            // Get<Player>("player").OnMove += async _ => {
            //     System.Diagnostics.Debug.WriteLine("a");
            //     services.GetService<ScriptUtils>.WaitSeconds(5);
            //     System.Diagnostics.Debug.WriteLine("written after 5 seconds of player movement");
            // };

            //Create<Player>("player", services, content.Load<Model>("character-colored"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
            //Create<Hammer>("hammer", services, content.Load<Model>("temp_hammer2"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
        }

        public override void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            // Handle Pressure Plate logic
            var pressureplate_1 = Get<PressurePlate>("pressureplate");
            if (pressureplate_1 != null)
            {
                if (pressureplate_1.IsActivated()) Get<Door>("door_pp").OpenDoor();
                else Get<Door>("door_pp").CloseDoor();
            }
        }
    }
}
