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
    internal class FlatIsland_test : Scene
    {
        public FlatIsland_test(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/FlatIsland_test.xml");
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

            //Get<TriggerObject>("end_trigger").OnTrigger += (_, _) =>
            //{
            //    ParentGameScreen.InitializeLevel(typeof(LaserTutorial).FullName);
            //};

            // Get<Player>("player").OnMove += async _ => {
            //     System.Diagnostics.Debug.WriteLine("a");
            //     services.GetService<ScriptUtils>.WaitSeconds(5);
            //     System.Diagnostics.Debug.WriteLine("written after 5 seconds of player movement");
            // };

            //Create<Player>("player", services, content.Load<Model>("character-colored"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
            //Create<Hammer>("hammer", services, content.Load<Model>("temp_hammer2"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
        }

        public override void Update(GameTime gameTime, bool screenHasFocus)
        {
            base.Update(gameTime, screenHasFocus);

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
