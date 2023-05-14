using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
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
        private ColorPlateState state = ColorPlateState.ZeroSuccess;

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
            Get<Laser>("maze_laser_A").SetLaserDefaultScale(11.51f);
            Get<Laser>("maze_laser_B").SetLaserDefaultScale(13f);
            Get<Laser>("maze_laser_C").SetLaserDefaultScale(13f);
            Get<Laser>("maze_laser3").SetLaserDefaultScale(13.86f);
            Get<Laser>("moving_laser").SetLaserDefaultScale(8.52f);

            MoveLaser();
        }

        private async void MoveLaser()
        {
            Vector3 offsetFromBase = Get<Laser>("moving_laser").Position - Get<Wall>("moving_base").Position;

            TweenTimeline tweenTimeline = Tweening.NewTimeline();
            tweenTimeline
                .AddVector3(Get<Laser>("moving_laser").Position, p =>
                {
                    Get<Laser>("moving_laser").Position = p;
                    Get<Wall>("moving_base").Position = p - offsetFromBase;
                })
                .AddFrame(0, new Vector3(-151.500f, -14.400f, -249.534f))
                .AddFrame(4000, new Vector3(-151.500f, -14.400f, -249.534f) + new Vector3(0, 0, -80))
                .AddFrame(8000, new Vector3(-151.500f, -14.400f, -249.534f));
            tweenTimeline.Loop = true;
        }

        private enum ColorPlateState
        {
            ZeroSuccess,
            OneSuccess,
            TwoSuccess,
            ThreeSuccess,
            FourSuccess
        }

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            // Handle Pressure Plate logic
            var maze_pp_A = Get<PressurePlate>("maze_pp_A");
            var maze_pp_B = Get<PressurePlate>("maze_pp_B");
            var maze_pp_C = Get<PressurePlate>("maze_pp_C");

            Get<Laser>("maze_laser_A").Visible = !maze_pp_A.IsActivated();
            Get<Laser>("maze_laser_B").Visible = !maze_pp_B.IsActivated();
            Get<Laser>("maze_laser_C").Visible = !maze_pp_C.IsActivated();

            //if (Get<PressuerPlate>(""))

            //if (pressureplate_1.IsActivated() && pressureplate_2.IsActivated())
            //{
            //    if (!openedGoalDoor)
            //    {
            //        openedGoalDoor = true;
            //        Camera.SetFollowTarget(Get<Door>("door_goal"));
            //        await Services.GetService<ScriptUtils>().WaitSeconds(1);

            //        Get<Door>("door_goal").OpenDoor();

            //        await Services.GetService<ScriptUtils>().WaitSeconds(1);
            //        Camera.SetFollowTarget(Get<Player>("player1"));
            //    }
            //}
            //else
            //{
            //    openedGoalDoor = false;
            //    Get<Door>("door_goal").CloseDoor();
            //}
        }
    }
}
