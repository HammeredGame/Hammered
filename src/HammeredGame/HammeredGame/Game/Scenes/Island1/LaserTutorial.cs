using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class LaserTutorial : Scene
    {
        public LaserTutorial(GameServices services) : base(services)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/LaserTutorial.xml");
            OnSceneStart();
        }

        protected override void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));

            // Set laser to desired length within level
            Laser laser1 = Get<Laser>("laser1");
            laser1.SetLaserDefaultScale(5.0f);
            Laser laser2 = Get<Laser>("laser2");
            laser2.SetLaserDefaultScale(5.0f);

            MoveBlock rock1 = Get<MoveBlock>("rock1");
            //MoveBlock rock2 = Get<MoveBlock>("rock2");

            var laserRockGroup = new CollisionGroup();
            CollisionGroupPair pair = new CollisionGroupPair(laserRockGroup, laserRockGroup);
            CollisionRules.CollisionGroupRules.Add(pair, CollisionRule.NoSolver);

            laser1.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;
            laser2.Entity.CollisionInformation.CollisionRules.Group= laserRockGroup;
            rock1.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;
            //rock2.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;

            Get<TriggerObject>("end_trigger").OnTrigger += (_, _) =>
            {
                Services.GetService<HammeredGame>().InitializeLevel(typeof(ChallengePuzzle).FullName);
            };

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
