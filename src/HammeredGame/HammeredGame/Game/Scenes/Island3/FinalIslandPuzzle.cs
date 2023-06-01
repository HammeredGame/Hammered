using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.Entities;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.CollectibleInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Screens;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using HammeredGame.Game.Scenes.Endgame;
using HammeredGame.Game.Scenes.Island2;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace HammeredGame.Game.Scenes.Island3
{
    internal class FinalIslandPuzzle : Scene
    {
        private bool spawnedNewRock = false;
        private bool openedGoalDoor = false;
        private bool openedKeyDoor = false;
        private bool withinDoorInteractTrigger = false;
        private CancellationTokenSource doorInteractTokenSource;// = new();

        private Vector3 leftCornerLaser1OffsetFromBase;
        private Vector3 leftCornerLaser2OffsetFromBase;

        private Vector3 newSpawnRockPosition = new Vector3(257.390f, 0.000f, -187.414f);
        private CollisionGroup rockGroup;

        public FinalIslandPuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/BGM_V2_4x");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island3/FinalIslandPuzzle.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // Set laser to desired length within level
            Get<Laser>("start_laser_1").SetLaserDefaultScale(20.0f);
            Get<Laser>("start_laser_2").SetLaserDefaultScale(20.0f);
            Get<Laser>("start_laser_3").SetLaserDefaultScale(20.0f);
            Get<Laser>("start_laser_4").SetLaserDefaultScale(20.0f);

            Get<Laser>("left_corner_laser_1").SetLaserDefaultScale(20.0f);
            Get<Laser>("left_corner_laser_2").SetLaserDefaultScale(20.0f);

            Get<Laser>("botright_laser_1").SetLaserDefaultScale(20.0f);
            Get<Laser>("botright_laser_2").SetLaserDefaultScale(21.0f);

            Get<Laser>("topright_laser_1").SetLaserDefaultScale(25.0f);
            Get<Laser>("topright_laser_2").SetLaserDefaultScale(25.0f);
            Get<Laser>("topright_laser_3").SetLaserDefaultScale(20.0f);
            Get<Laser>("topright_laser_4").SetLaserDefaultScale(20.0f);

            Get<Laser>("mid_laser_1").SetLaserDefaultScale(20.0f);
            Get<Laser>("mid_laser_2").SetLaserDefaultScale(20.0f);

            CollisionGroup laserGroup = new CollisionGroup();
            rockGroup = new CollisionGroup();
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
            }


            // Insert any limitations on the paths the hammer may travel by calling functions from the <c>UniformGrid</c> instance.
            Vector3 floorDisableStart = new Vector3(this.Grid.originPoint.X, this.Grid.originPoint.Y, this.Grid.originPoint.Z);
            Vector3 floorDisableFinish = new Vector3(this.Grid.endPoint.X, this.Grid.originPoint.Y, this.Grid.endPoint.Z);
            this.Grid.MarkRangeAs(floorDisableStart, floorDisableFinish, false);

            // Set the moving laser's original movement and the offset to the base
            Get<Laser>("left_corner_laser_1").Entity.LinearVelocity = new(0, 0, 30);
            leftCornerLaser1OffsetFromBase = Get<Laser>("left_corner_laser_1").Position - Get<Wall>("left_corner_laser_1_base").Position;

            Get<Laser>("left_corner_laser_2").Entity.LinearVelocity = new(-30, 0, 0);
            leftCornerLaser2OffsetFromBase = Get<Laser>("left_corner_laser_2").Position - Get<Wall>("left_corner_laser_2_base").Position;

            Get<Laser>("mid_laser_1").Entity.LinearVelocity = new(0, 0, 30);
            leftCornerLaser1OffsetFromBase = Get<Laser>("mid_laser_1").Position - Get<Wall>("mid_laser_base1").Position;

            Get<Laser>("mid_laser_2").Entity.LinearVelocity = new(-30, 0, 0);
            leftCornerLaser2OffsetFromBase = Get<Laser>("mid_laser_2").Position - Get<Wall>("mid_laser_base2").Position;


            Get<PressurePlate>("start_pressureplate1").OnTrigger += (_, _) =>
            {
                var start_laser_1 = Get<Laser>("start_laser_1");
                start_laser_1.SetLaserScale(0f);
                start_laser_1.Deactivated = true;
            };

            Get<PressurePlate>("start_pressureplate1").OnTriggerEnd += (_, _) =>
            {
                var start_laser_1 = Get<Laser>("start_laser_1");
                start_laser_1.ReturnToDefaultLength();
                start_laser_1.Deactivated = false;
            };

            Get<PressurePlate>("start_pressureplate2").OnTrigger += (_, _) =>
            {
                var start_laser_2 = Get<Laser>("start_laser_2");
                start_laser_2.SetLaserScale(0f);
                start_laser_2.Deactivated = true;
            };

            Get<PressurePlate>("start_pressureplate2").OnTriggerEnd += (_, _) =>
            {
                var start_laser_2 = Get<Laser>("start_laser_2");
                start_laser_2.ReturnToDefaultLength();
                start_laser_2.Deactivated = false;
            };

            //doorInteractTokenSource = new();
            //Get<TriggerObject>("door_interact_trigger").OnTrigger += async (_, _) =>
            //{
            //    if (!openedKeyDoor)
            //    {
            //        doorInteractTokenSource = new();
            //        ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Interact }, doorInteractTokenSource.Token);
            //        withinDoorInteractTrigger = true;
            //    }
            //};

            //Get<TriggerObject>("door_interact_trigger").OnTriggerEnd += async (_, _) =>
            //{
            //    doorInteractTokenSource.Cancel();
            //    withinDoorInteractTrigger = false;
            //};

            //await ParentGameScreen.ShowDialogueAndWait("Thor really went out of his way...");
            //await ParentGameScreen.ShowDialogueAndWait("to make it this much harder for me?");
            //await ParentGameScreen.ShowDialogueAndWait("Oh boy this looks tricky...");
            //await ParentGameScreen.ShowDialogueAndWait("Hopefully I'm not going to hit rock bottom on this!");

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            //Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            //Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            //{
            //    if (Get<Hammer>("hammer").IsWithCharacter())
            //    {
            //        await ParentGameScreen.ShowDialogueAndWait("Phewww, that was tough...!");
            //        ParentGameScreen.InitializeLevel(typeof(ColorMinigamePuzzle).FullName);
            //    }
            //    else
            //    {
            //        await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
            //    }
            //};
        }

        /// <summary>
        /// If the moving laser has hit its ends, reverse its direction. Otherwise, update the laser
        /// base position to follow the laser.
        /// </summary>
        private void MovingLaserUpdate(string laserName, string laserBaseName, int axis, float minLimit, float maxLimit, Vector3 movingLaserOffsetFromBase)
        {
            Laser laser = Get<Laser>(laserName);
            Wall laserBase = Get<Wall>(laserBaseName);

            float comparePos = 0.0f;
            BEPUutilities.Vector3 laserVelocity;
            if (axis == 0)
            {
                comparePos = laser.Position.X;
                laserVelocity = new(30, 0, 0);
            }
            else if (axis == 1)
            {
                comparePos = laser.Position.Y;
                laserVelocity = new(0, 30, 0);
            }
            else
            {
                comparePos = laser.Position.Z;
                laserVelocity = new(0, 0, 30);
            }

            if (comparePos >= maxLimit)
            {
                laser.Entity.LinearVelocity = -1 * laserVelocity;
            }
            else if (comparePos <= minLimit)
            {
                laser.Entity.LinearVelocity = laserVelocity;
            }

            laserBase.Position = laser.Position - movingLaserOffsetFromBase;
        }

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            MovingLaserUpdate("left_corner_laser_1", "left_corner_laser_1_base", 2, -350f, -260f, leftCornerLaser1OffsetFromBase);
            MovingLaserUpdate("left_corner_laser_2", "left_corner_laser_2_base", 0, 53f, 205f, leftCornerLaser2OffsetFromBase);

            MovingLaserUpdate("mid_laser_1", "mid_laser_base1", 2, -190f, -134f, leftCornerLaser1OffsetFromBase);
            MovingLaserUpdate("mid_laser_2", "mid_laser_base2", 0, 350f, 410f, leftCornerLaser2OffsetFromBase);

            // Handle Pressure Plate logic

            // Start section
            var start_pressureplate_1 = Get<PressurePlate>("start_pressureplate1");
            var start_pressureplate_2 = Get<PressurePlate>("start_pressureplate2");
            var start_pressureplate_3 = Get<PressurePlate>("start_pressureplate3");

            if (start_pressureplate_1.IsActivated() && start_pressureplate_2.IsActivated())
            {
                var start_laser_4 = Get<Laser>("start_laser_4");
                start_laser_4.SetLaserScale(0f);
                start_laser_4.Deactivated = true;
            }
            else
            {
                var start_laser_4 = Get<Laser>("start_laser_4");
                start_laser_4.ReturnToDefaultLength();
                start_laser_4.Deactivated = false;
            }

            if (start_pressureplate_1.IsActivated() && start_pressureplate_2.IsActivated() && start_pressureplate_3.IsActivated())
            {
                var start_laser_3 = Get<Laser>("start_laser_3");
                start_laser_3.SetLaserScale(0f);
                start_laser_3.Deactivated = true;
            }
            else
            {
                var start_laser_3 = Get<Laser>("start_laser_3");
                start_laser_3.ReturnToDefaultLength();
                start_laser_3.Deactivated = false;
            }

            // Mid Left Section
            var midleft_pressureplate_1 = Get<PressurePlate>("five_pp_pressureplate_1");
            var midleft_pressureplate_2 = Get<PressurePlate>("five_pp_pressureplate_2");
            var midleft_pressureplate_3 = Get<PressurePlate>("five_pp_pressureplate_3");
            var midleft_pressureplate_4 = Get<PressurePlate>("five_pp_pressureplate_4");
            var midleft_pressureplate_5 = Get<PressurePlate>("five_pp_pressureplate_5");

            if (midleft_pressureplate_1.IsActivated() && midleft_pressureplate_2.IsActivated() && 
                midleft_pressureplate_3.IsActivated() && midleft_pressureplate_4.IsActivated() &&
                midleft_pressureplate_5.IsActivated())
            {
                Get<Door>("five_pp_key_door_1").OpenDoor();
                Get<Door>("five_pp_key_door_2").OpenDoor();
                Get<Door>("five_pp_key_door_3").OpenDoor();
                Get<Door>("five_pp_key_door_4").OpenDoor();
            }
            else
            {
                Get<Door>("five_pp_key_door_1").CloseDoor();
                Get<Door>("five_pp_key_door_2").CloseDoor();
                Get<Door>("five_pp_key_door_3").CloseDoor();
                Get<Door>("five_pp_key_door_4").CloseDoor();
            }

            // Top Mid Section
            var topmid_pressureplate_1 = Get<PressurePlate>("pressureplate_fourcorner_1");
            var topmid_pressureplate_2 = Get<PressurePlate>("pressureplate_fourcorner_2");
            var topmid_pressureplate_3 = Get<PressurePlate>("pressureplate_fourcorner_3");
            var topmid_pressureplate_4 = Get<PressurePlate>("pressureplate_fourcorner_4");

            if (topmid_pressureplate_1.IsActivated() && topmid_pressureplate_2.IsActivated() &&
                topmid_pressureplate_3.IsActivated() && topmid_pressureplate_4.IsActivated())
            {
                Get<Door>("four_corner_door_1").OpenDoor();
                Get<Door>("four_corner_door_2").OpenDoor();
                Get<Door>("four_corner_door_3").OpenDoor();
                Get<Door>("four_corner_door_4").OpenDoor();
            }
            else
            {
                Get<Door>("four_corner_door_1").CloseDoor();
                Get<Door>("four_corner_door_2").CloseDoor();
                Get<Door>("four_corner_door_3").CloseDoor();
                Get<Door>("four_corner_door_4").CloseDoor();
            }
        }
    }
}
