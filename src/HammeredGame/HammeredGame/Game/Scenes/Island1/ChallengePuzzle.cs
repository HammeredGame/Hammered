using HammeredGame.Core;
using HammeredGame.Game.GameObjects;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class ChallengePuzzle : Scene
    {
        public ChallengePuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/ChallengePuzzle.xml");
            OnSceneStart();
        }

        protected override void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));

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
