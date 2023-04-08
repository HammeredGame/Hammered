using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class ShoreWakeup : Scene
    {
        public ShoreWakeup(GameServices services) : base(services)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/ShoreWakeup.xml");

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

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
