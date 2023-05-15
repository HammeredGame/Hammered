using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Constraints.SolverGroups;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Scenes.Endgame;
using HammeredGame.Game.Scenes.Island1;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;
using Pleasing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island2
{
    internal class ColorMinigamePuzzle : Scene
    {
        private ColorPlateState state = ColorPlateState.ZeroSuccess;
        private bool withinDoorInteractTrigger;

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
            Laser laser1 = Get<Laser>("purple_laser");
            laser1.SetLaserDefaultScale(4.0f);
            Laser laser2 = Get<Laser>("purple_laser1");
            laser2.SetLaserDefaultScale(4.0f);
            Laser laser3 = Get<Laser>("purple_laser2");
            laser3.SetLaserDefaultScale(6.35f);
            Get<Laser>("maze_laser_A").SetLaserDefaultScale(11.51f);
            Get<Laser>("maze_laser_B").SetLaserDefaultScale(13f);
            Get<Laser>("maze_laser_C").SetLaserDefaultScale(13f);
            Get<Laser>("maze_laser3").SetLaserDefaultScale(13.86f);
            Get<Laser>("moving_laser").SetLaserDefaultScale(8.52f);
            Get<Laser>("yellow_laser").SetLaserDefaultScale(13f);


            CollisionGroup noSolverGroup = new CollisionGroup();
            CollisionGroupPair pair = new CollisionGroupPair(noSolverGroup, noSolverGroup);
            CollisionRules.CollisionGroupRules.Add(pair, CollisionRule.NoSolver);

            foreach (var gO in GameObjectsList)
            {
                // Update lasers in the scene
                var laser = gO as Laser;
                if (laser != null)
                {
                    laser.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
                }

                // Update rocks in the scene
                var rock = gO as MoveBlock;
                if (rock != null)
                {
                    rock.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
                }

                // Set water bounds objects to a group such that they do not block rocks
                var waterBounds = gO as WaterBoundsObject;
                if (waterBounds != null)
                {
                    waterBounds.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
                }
            }

            MoveLaserLoop();

            CancellationTokenSource doorInteractTokenSource = new();
            Get<TriggerObject>("hub_door_interact_trigger").OnTrigger += async (_, _) =>
            {
                doorInteractTokenSource = new();
                ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Interact }, doorInteractTokenSource.Token);
                withinDoorInteractTrigger = true;
            };

            Get<TriggerObject>("hub_door_interact_trigger").OnTriggerEnd += async (_, _) =>
            {
                doorInteractTokenSource.Cancel();
                withinDoorInteractTrigger = false;
            };

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    ParentGameScreen.InitializeLevel(typeof(TempleEndLevel).FullName);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("temp text. bring hammer with you!");
                }
            };
        }

        private async void MoveLaserLoop()
        {
            // Continuously move the moving-laser
            Vector3 offsetFromBase = Get<Laser>("moving_laser").Position - Get<Wall>("moving_base").Position;

            Get<Laser>("moving_laser").Position = new Vector3(-151.500f, -14.400f, -255f);
            TweenTimeline tweenTimeline = Tweening.NewTimeline();
            tweenTimeline
                .AddFloat(Get<Laser>("moving_laser").Entity.LinearVelocity.Z, f =>
                {
                    Get<Laser>("moving_laser").Entity.LinearVelocity = new(0, 0, f);
                    Get<Wall>("moving_base").Position = Get<Laser>("moving_laser").Position - offsetFromBase;
                })
                .AddFrame(0, -20)
                .AddFrame(2999, -20)
                .AddFrame(3000, 20)
                .AddFrame(5999, 20);
            tweenTimeline.Loop = true;
        }

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            MazeUpdate();
            FourPlatesUpdate();
            DoorHintIfWithinVicinity();
        }

        private void MazeUpdate()
        {
            var maze_pp_A = Get<PressurePlate>("maze_pp_A");
            var maze_laser_A = Get<Laser>("maze_laser_A");
            var maze_pp_B = Get<PressurePlate>("maze_pp_B");
            var maze_laser_B = Get<Laser>("maze_laser_B");
            var maze_pp_C = Get<PressurePlate>("maze_pp_C");
            var maze_laser_C = Get<Laser>("maze_laser_C");

            // The three pressure plates in the laser maze bit toggle laser activation
            if (maze_pp_A.IsActivated())
            {
                maze_laser_A.SetLaserScale(0f);
            }
            else
            {
                maze_laser_A.ReturnToDefaultLength();
            }
            if (maze_pp_B.IsActivated())
            {
                maze_laser_B.SetLaserScale(0f);
            }
            else
            {
                maze_laser_B.ReturnToDefaultLength();
            }
            if (maze_pp_C.IsActivated())
            {
                maze_laser_C.SetLaserScale(0f);
            }
            else
            {
                maze_laser_C.ReturnToDefaultLength();
            }
        }

        private enum ColorPlateState
        {
            ZeroSuccess,
            OneSuccess,
            TwoSuccess,
            ThreeSuccess,
            Complete
        }

        private void FourPlatesUpdate()
        {
            // The four colored pressure plates are a state machine that you have to press in the
            // right order. Any bad press will revert the state back to zero. After a successful
            // completion, the pressure plates won't respond anymore and door will stay open.
            if (Get<PressurePlate>("pressureplate_P").IsActivated() && state != ColorPlateState.Complete)
            {
                if (state == ColorPlateState.ZeroSuccess || state == ColorPlateState.OneSuccess)
                {
                    state = ColorPlateState.OneSuccess;
                }
                else
                {
                    state = ColorPlateState.ZeroSuccess;
                }
            }
            else if (Get<PressurePlate>("pressureplate_G").IsActivated() && state != ColorPlateState.Complete)
            {
                if (state == ColorPlateState.OneSuccess || state == ColorPlateState.TwoSuccess)
                {
                    state = ColorPlateState.TwoSuccess;
                }
                else
                {
                    state = ColorPlateState.ZeroSuccess;
                }
            }
            else if (Get<PressurePlate>("pressureplate_B").IsActivated() && state != ColorPlateState.Complete)
            {
                if (state == ColorPlateState.TwoSuccess || state == ColorPlateState.ThreeSuccess)
                {
                    state = ColorPlateState.ThreeSuccess;
                }
                else
                {
                    state = ColorPlateState.ZeroSuccess;
                }
            }
            else if (Get<PressurePlate>("pressureplate_Y").IsActivated() && state != ColorPlateState.Complete)
            {
                if (state == ColorPlateState.ThreeSuccess)
                {
                    state = ColorPlateState.Complete;
                    Get<Door>("pp_door").OpenDoor();
                    Get<Door>("pp_door1").OpenDoor();
                }
                else
                {
                    state = ColorPlateState.ZeroSuccess;
                }
            }
        }

        private async void DoorHintIfWithinVicinity()
        {
            if (withinDoorInteractTrigger)
            {
                Input inp = this.Services.GetService<Input>();
                if (UserAction.Interact.Pressed(inp))
                {
                    if (Get<Key>("pp_key").IsPickedUp() &&
                        Get<Key>("maze_key").IsPickedUp() &&
                        Get<Key>("yellow_key").IsPickedUp())
                    {
                        Get<Door>("hub_door").OpenDoor();
                    }
                    else
                    {
                        await ParentGameScreen.ShowDialogueAndWait("Hmm... This door has three keyholes...");
                    }
                }
            }
        }
    }
}
