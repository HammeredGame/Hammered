using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;
using Pleasing;
using System;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island2
{
    internal class ColorMinigamePuzzle : Scene
    {
        public ColorMinigamePuzzle(GameServices services, GameScreen screen) : base(services, screen)
        { }
        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island2/ColorMinigamePuzzle.xml", progress);
        }

        protected override void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // Set laser to desired length within level
            Laser laser1 = Get<Laser>("red_laser");
            laser1.SetLaserDefaultScale(4.0f);
            Laser laser2 = Get<Laser>("red_laser1");
            laser2.SetLaserDefaultScale(4.0f);
            Laser laser3 = Get<Laser>("red_laser2");
            laser3.SetLaserDefaultScale(6.35f);
            Get<Laser>("maze_laser").SetLaserDefaultScale(13f);
            Get<Laser>("maze_laser1").SetLaserDefaultScale(13f);
            Get<Laser>("maze_laser2").SetLaserDefaultScale(11.51f);
            Get<Laser>("maze_laser3").SetLaserDefaultScale(13.86f);
            Get<Laser>("moving_laser").SetLaserDefaultScale(8.52f);

            MoveLaser();
        }

        private async void MoveLaser() {
            Vector3 offsetFromBase = Get<Laser>("moving_laser").Position - Get<Wall>("moving_base").Position;

            TweenTimeline tweenTimeline = Tweening.NewTimeline();
            tweenTimeline
                .AddVector3(Get<Laser>("moving_laser").Position, p =>
                {
                    Get<Laser>("moving_laser").Position = p;
                    Get<Wall>("moving_base").Position = p - offsetFromBase;
                })
                .AddFrame(0, Get<Laser>("moving_laser").Position)
                .AddFrame(4000, Get<Laser>("moving_laser").Position + new Vector3(0, 0, -80))
                .AddFrame(8000, Get<Laser>("moving_laser").Position);
            tweenTimeline.Loop = true;
        }
    }
}
