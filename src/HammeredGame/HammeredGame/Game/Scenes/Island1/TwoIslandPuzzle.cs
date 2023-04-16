using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TwoIslandPuzzle : Scene
    {
        public TwoIslandPuzzle(GameServices services) : base(services)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/TwoIslandPuzzle.xml");
            OnSceneStart();
        }

        protected override void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));

            Get<Door>("door_goal").SetIsGoal(true);

            Get<PressurePlate>("pressureplate").SetTriggerObject(Get<Door>("door_pp"));

            Get<Key>("key").SetCorrespondingDoor(Get<Door>("door_goal"));

            // No further initialization required for the <c>UniformGrid</c> instance.

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
