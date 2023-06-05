using BEPUphysics.CollisionRuleManagement;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Scenes.Endgame;
using HammeredGame.Game.Scenes.Island3;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Pleasing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island2
{
    internal class ColorMinigamePuzzle : Scene
    {
        private ColorPlateState state = ColorPlateState.ZeroSuccess;
        private bool withinDoorInteractTrigger;

        private Vector3 movingLaserOffsetFromBase;

        public ColorMinigamePuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/waves_bgm4");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }

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

            CollisionGroup laserGroup = new CollisionGroup();
            CollisionGroup rockGroup = new CollisionGroup();
            CollisionGroup waterBoundsGroup = new CollisionGroup();

            // Set collision rule for laser rock interaction
            CollisionGroupPair laserRockpair = new CollisionGroupPair(laserGroup, rockGroup);
            CollisionRules.CollisionGroupRules.Add(laserRockpair, CollisionRule.NoSolver);

            // Set collision rule for rock water interaction
            CollisionGroupPair RockWaterpair = new CollisionGroupPair(rockGroup, waterBoundsGroup);
            CollisionRules.CollisionGroupRules.Add(RockWaterpair, CollisionRule.NoSolver);

            // Set collision rule for rock rock interaction
            CollisionGroupPair RockRockPair = new CollisionGroupPair(rockGroup, rockGroup);
            CollisionRules.CollisionGroupRules.Add(RockRockPair, CollisionRule.Normal);

            foreach (var gO in GameObjectsList)
            {
                // Update lasers in the scene
                var laser = gO as Laser;
                if (laser != null)
                {
                    laser.Entity.CollisionInformation.CollisionRules.Group = laserGroup;
                }

                // Update rocks in the scene
                var rock = gO as MoveBlock;
                if (rock != null)
                {
                    rock.Entity.CollisionInformation.CollisionRules.Group = rockGroup;
                }

                // Set water bounds objects to a group such that they do not block rocks
                var waterBounds = gO as WaterBoundsObject;
                if (waterBounds != null)
                {
                    waterBounds.Entity.CollisionInformation.CollisionRules.Group = waterBoundsGroup;
                }

                // Check for walls in the scene
                var wall = gO as Wall;
                if (wall != null)
                {
                    this.UpdateSceneGrid(wall, false, 0.9);
                }
            }

            this.UpdateSceneGrid(Get<Door>("pp_door"), false, 0.9);
            this.UpdateSceneGrid(Get<Door>("pp_door1"), false, 0.9);

            // Set the moving laser's original movement and the offset to the base
            Get<Laser>("moving_laser").Entity.LinearVelocity = new(0, 0, 30);
            movingLaserOffsetFromBase = Get<Laser>("moving_laser").Position - Get<Wall>("moving_base").Position;

            Get<PressurePlate>("maze_pp_A").OnTrigger += (_, _) =>
            {
                var maze_laser_A = Get<Laser>("maze_laser_A");
                maze_laser_A.SetLaserScale(0f);
                maze_laser_A.Deactivated = true;
            };

            Get<PressurePlate>("maze_pp_A").OnTriggerEnd += (_, _) =>
            {
                var maze_laser_A = Get<Laser>("maze_laser_A");
                maze_laser_A.ReturnToDefaultLength();
                maze_laser_A.Deactivated = false;
            };

            Get<PressurePlate>("maze_pp_B").OnTrigger += (_, _) =>
            {
                var maze_laser_B = Get<Laser>("maze_laser_B");
                maze_laser_B.SetLaserScale(0f);
                maze_laser_B.Deactivated = true;
            };

            Get<PressurePlate>("maze_pp_B").OnTriggerEnd += (_, _) =>
            {
                var maze_laser_B = Get<Laser>("maze_laser_B");
                maze_laser_B.ReturnToDefaultLength();
                maze_laser_B.Deactivated = false;
            };

            Get<PressurePlate>("maze_pp_C").OnTrigger += (_, _) =>
            {
                var maze_laser_C = Get<Laser>("maze_laser_C");
                maze_laser_C.SetLaserScale(0f);
                maze_laser_C.Deactivated = true;
            };

            Get<PressurePlate>("maze_pp_C").OnTriggerEnd += (_, _) =>
            {
                var maze_laser_C = Get<Laser>("maze_laser_C");
                maze_laser_C.ReturnToDefaultLength();
                maze_laser_C.Deactivated = false;
            };

            CancellationTokenSource doorInteractTokenSource = new();
            Get<TriggerObject>("hub_door_interact_trigger").OnTrigger += (_, _) =>
            {
                doorInteractTokenSource = new();
                ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Interact }, doorInteractTokenSource.Token);
                withinDoorInteractTrigger = true;
            };

            Get<TriggerObject>("hub_door_interact_trigger").OnTriggerEnd += (_, _) =>
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
                    // todo: have some wrapper class for mediaplayer that allows fading etc
                    float oldVolume = MediaPlayer.Volume;
                    Tweening.Tween((f) => MediaPlayer.Volume = f, MediaPlayer.Volume, 0f, 500, Easing.Linear, LerpFunctions.Float);
                    await Services.GetService<ScriptUtils>().WaitMilliseconds(300);

                    Get<Player>("player1").InputEnabled = false;
                    Get<Player>("player1").ShowVictoryStars();
                    await Services.GetService<ScriptUtils>().WaitSeconds(1);
                    ParentGameScreen.InitializeLevel(typeof(FinalIslandPuzzle).FullName, true);

                    await Services.GetService<ScriptUtils>().WaitSeconds(2);
                    Tweening.Tween((f) => MediaPlayer.Volume = f, 0f, oldVolume, 3000, Easing.Linear, LerpFunctions.Float);

                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("Don't forget to bring the hammer with you!");
                }
            };
        }

        public override void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            MovingLaserUpdate();
            //MazeUpdate();
            FourPlatesUpdate();
            DoorHintIfWithinVicinity();
        }

        /// <summary>
        /// If the moving laser has hit its ends, reverse its direction. Otherwise, update the laser
        /// base position to follow the laser.
        /// </summary>
        private void MovingLaserUpdate()
        {
            Laser laser = Get<Laser>("moving_laser");
            Wall laserBase = Get<Wall>("moving_base");

            if (laser.Position.Z >= -260f)
            {
                laser.Entity.LinearVelocity = new(0, 0, -30);
            }
            else if (laser.Position.Z <= -310f)
            {
                laser.Entity.LinearVelocity = new(0, 0, 30);
            }

            laserBase.Position = laser.Position - movingLaserOffsetFromBase;
        }

        //private void MazeUpdate()
        //{
        //    var maze_pp_A = Get<PressurePlate>("maze_pp_A");
        //    var maze_laser_A = Get<Laser>("maze_laser_A");
        //    var maze_pp_B = Get<PressurePlate>("maze_pp_B");
        //    var maze_laser_B = Get<Laser>("maze_laser_B");
        //    var maze_pp_C = Get<PressurePlate>("maze_pp_C");
        //    var maze_laser_C = Get<Laser>("maze_laser_C");

        //    // The three pressure plates in the laser maze bit toggle laser activation
        //    //if (maze_pp_A.IsActivated())
        //    //{
        //    //    maze_laser_A.SetLaserScale(0f);
        //    //}
        //    //else
        //    //{
        //    //    maze_laser_A.ReturnToDefaultLength();
        //    //}
        //    if (maze_pp_B.IsActivated())
        //    {
        //        maze_laser_B.SetLaserScale(0f);
        //    }
        //    else
        //    {
        //        maze_laser_B.ReturnToDefaultLength();
        //    }
        //    if (maze_pp_C.IsActivated())
        //    {
        //        maze_laser_C.SetLaserScale(0f);
        //    }
        //    else
        //    {
        //        maze_laser_C.ReturnToDefaultLength();
        //    }
        //}

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
            // As a reminder, the correct sequence of pressure plates is Purple -> Blue -> Yellow/Orange -> Green

            // WARNING: The current version of the code is not scalable and is very dependent on the order
            // upon which the if statements are called (which is fine for the sequential state machine that is here).
            // Best coding practices are NOT followed!
            if (state != ColorPlateState.Complete)
            {
                if (Get<PressurePlate>("pressureplate_P").IsActivated())
                {
                    if (state == ColorPlateState.ZeroSuccess)
                    {
                        ActivatePressurePlate(Get<PressurePlate>("pressureplate_P"));
                        state = ColorPlateState.OneSuccess;
                    }
                }
                if (Get<PressurePlate>("pressureplate_B").IsActivated())
                {
                    if (state == ColorPlateState.OneSuccess ||
                        state == ColorPlateState.TwoSuccess ||
                        state == ColorPlateState.ThreeSuccess)
                    {
                        ActivatePressurePlate(Get<PressurePlate>("pressureplate_B"));
                        state = ColorPlateState.TwoSuccess;
                    }
                    else
                    {
                        DeactivatePressurePlate(Get<PressurePlate>("pressureplate_P"));
                        state = ColorPlateState.ZeroSuccess;
                    }
                }
                if (Get<PressurePlate>("pressureplate_Y").IsActivated())
                {
                    if (state == ColorPlateState.TwoSuccess || state == ColorPlateState.ThreeSuccess)
                    {
                        ActivatePressurePlate(Get<PressurePlate>("pressureplate_Y"));
                        state = ColorPlateState.ThreeSuccess;
                    }
                    else
                    {
                        DeactivatePressurePlate(Get<PressurePlate>("pressureplate_P"));
                        DeactivatePressurePlate(Get<PressurePlate>("pressureplate_B"));
                        state = ColorPlateState.ZeroSuccess;
                    }
                }
                if (Get<PressurePlate>("pressureplate_G").IsActivated())
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
                    // In case of success, the "locking" mechanic is no longer needed. (if True).
                    // In case of failure, all pressure plates are reverted to normal (if False).
                    DeactivatePressurePlate(Get<PressurePlate>("pressureplate_P"));
                    DeactivatePressurePlate(Get<PressurePlate>("pressureplate_B"));
                    DeactivatePressurePlate(Get<PressurePlate>("pressureplate_Y"));
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

        private static void ActivatePressurePlate(PressurePlate pp)
        {
            pp.SetActivated(true);
            pp.lockActivationState = true;
        }

        private static void DeactivatePressurePlate(PressurePlate pp)
        {
            pp.lockActivationState = false;
            pp.SetActivated(false);
        }
    }
}
