using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.Screens;

namespace HammeredGame.Game.Scenes.Test
{
    internal class LaserTest : Scene
    {
        public LaserTest(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Test/LaserTest.xml");
            OnSceneStart();
        }

        protected override void OnSceneStart()
        {
            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Laser laser1 = Get<Laser>("laser1");
            laser1.SetLaserDefaultScale(6.0f);
        }
    }
}
