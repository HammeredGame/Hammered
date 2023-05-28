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

namespace HammeredGame.Game.Scenes.Island1
{
    internal class PrototypePuzzle : Scene
    {
        private bool spawnedNewRock = false;
        private bool openedGoalDoor = false;
        private bool openedKeyDoor = false;
        private bool withinDoorInteractTrigger = false;
        private CancellationTokenSource doorInteractTokenSource;// = new();

        private Vector3 newSpawnRockPosition = new Vector3(257.390f, 0.000f, -187.414f);
        private CollisionGroup rockGroup;

        public PrototypePuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/bgm2_4x_b");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island1/PrototypePuzzle_voxel.xml", progress);
        }

        protected override async void OnSceneStart() {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // Set laser to desired length within level
            Laser laser1 = Get<Laser>("laser1");
            laser1.SetLaserDefaultScale(11f);

            MoveBlock rock1 = Get<MoveBlock>("rock1");
            MoveBlock rock2 = Get<MoveBlock>("rock2");

            //noSolverGroup = new CollisionGroup();
            //CollisionGroupPair pair = new CollisionGroupPair(noSolverGroup, noSolverGroup);
            //CollisionRules.CollisionGroupRules.Add(pair, CollisionRule.NoSolver);

            //laser1.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
            //rock1.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
            //rock2.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;

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

            Get<Key>("key").SetCorrespondingDoor(Get<Door>("door_key"));

            Get<Door>("door_goal").SetIsGoal(true);
            this.UpdateSceneGrid(Get<Door>("door_key"), false, 0.9);
            this.UpdateSceneGrid(Get<Door>("door_pp"), false, 0.9);

            //Get<PressurePlate>("pressureplate").SetTriggerObject(Get<Door>("door_pp"));

            this.UpdateSceneGrid(Get<Wall>("wall"), false, 0.9);
            this.UpdateSceneGrid(Get<Wall>("wall1"), false, 0.9);
            this.UpdateSceneGrid(Get<Wall>("wall2"), false, 0.9);
            this.UpdateSceneGrid(Get<Wall>("wall3"), false, 0.9);

            // Insert any limitations on the paths the hammer may travel by calling functions from the <c>UniformGrid</c> instance.
            Vector3 floorDisableStart = new Vector3(this.Grid.originPoint.X, this.Grid.originPoint.Y, this.Grid.originPoint.Z);
            Vector3 floorDisableFinish = new Vector3(this.Grid.endPoint.X, this.Grid.originPoint.Y, this.Grid.endPoint.Z);
            this.Grid.MarkRangeAs(floorDisableStart, floorDisableFinish, false);


            doorInteractTokenSource = new();
            Get<TriggerObject>("door_interact_trigger").OnTrigger += (_, _) =>
            {
                if (!openedKeyDoor)
                {
                    doorInteractTokenSource = new();
                    ParentGameScreen.ShowPromptsFor(new List<UserAction>() { UserAction.Interact }, doorInteractTokenSource.Token);
                    withinDoorInteractTrigger = true;
                }
            };

            Get<TriggerObject>("door_interact_trigger").OnTriggerEnd += (_, _) =>
            {
                doorInteractTokenSource.Cancel();
                withinDoorInteractTrigger = false;
            };

            //await ParentGameScreen.ShowDialogueAndWait("Thor really went out of his way...");
            //await ParentGameScreen.ShowDialogueAndWait("to make it this much harder for me?");
            await ParentGameScreen.ShowDialogueAndWait("Oh boy this looks tricky...");
            await ParentGameScreen.ShowDialogueAndWait("Hopefully I'm not going to hit rock bottom on this!");

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    await ParentGameScreen.ShowDialogueAndWait("Phewww, that was tough...!");
                    ParentGameScreen.InitializeLevel(typeof(ColorMinigamePuzzle).FullName, true);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };
        }

        private int GetNumActiveRocks()
        {
            int num_active = 0;

            foreach (GameObject gO in GameObjectsList)
            {
                var rock = gO as MoveBlock;
                if (rock != null)
                {
                    if (rock.MblockState != MoveBlock.MBState.InWater) num_active++;
                }
            }

            return num_active;
        }

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            // Handle Pressure Plate logic
            var pressureplate_1 = Get<PressurePlate>("pressureplate1");
            var pressureplate_2 = Get<PressurePlate>("pressureplate2");

            if (pressureplate_1.IsActivated()) Get<Door>("door_pp").OpenDoor();
            else Get<Door>("door_pp").CloseDoor();

            if (pressureplate_1.IsActivated() && pressureplate_2.IsActivated())
            {
                if (!openedGoalDoor)
                {
                    openedGoalDoor = true;
                    Camera.SetFollowTarget(Get<Door>("door_goal"));
                    await Services.GetService<ScriptUtils>().WaitSeconds(1);

                    Get<Door>("door_goal").OpenDoor();

                    await Services.GetService<ScriptUtils>().WaitSeconds(1);
                    Camera.SetFollowTarget(Get<Player>("player1"));
                }
            }
            else
            {
                openedGoalDoor = false;
                Get<Door>("door_goal").CloseDoor();
            }

            if (pressureplate_2.IsActivated())
            {
                if (!spawnedNewRock)
                {
                    spawnedNewRock = true;

                    // Spawn New Rock, if the number of active rocks is less than 2
                    if (GetNumActiveRocks() < 2)
                    {
                        Get<Player>("player1").InputEnabled = false;
                        var template_rock = Get<MoveBlock>("rock1");
                        // Generate a new name for the object
                        string name = GenerateUniqueNameWithPrefix(template_rock.GetType().Name.ToLower());

                        // Copy the entity
                        Entity entity = null;
                        if (template_rock.Entity is Box box)
                        {
                            entity = new Box(newSpawnRockPosition.ToBepu(), box.Width, box.Height, box.Length, box.Mass);
                        }
                        else if (template_rock.Entity is Sphere sph)
                        {
                            entity = new Sphere(newSpawnRockPosition.ToBepu(), sph.Radius, sph.Mass);
                        }
                        // We want to call Create<T>() with T being the type of gameObject.
                        // However, we can't use variables for generic type parameters, so
                        // instead we will create a specific version of the method and invoke it
                        // manually. This causes some changes to how variadic "params dynamic[]"
                        // behaves, outlined below.
                        GameObject newObj = (GameObject)GetType().GetMethod(nameof(Create)).MakeGenericMethod(template_rock.GetType()).Invoke(this, new object[] {
                                    name,
                                    new object[] {
                                        Services,
                                        // We are passing references to the Model and Texture here,
                                        // assuming they won't change, and that loading them again from
                                        // the Content Manager would be a waste.
                                        template_rock.Model,
                                        template_rock.Texture,
                                        newSpawnRockPosition,
                                        template_rock.Rotation,
                                        template_rock.Scale,
                                        entity
                                    }
                                });
                        // Apply the same model offset as the original
                        newObj.EntityModelOffset = template_rock.EntityModelOffset;
                        newObj.Visible = false;
                        newObj.Entity.CollisionInformation.CollisionRules.Group = rockGroup;

                        if (openedGoalDoor)
                        {
                            // synchronise so it we start cut scene after the goal one is done,
                            // because the two branches containing awaits will be executed in
                            // succession without waiting apparently
                            await Services.GetService<ScriptUtils>().WaitSeconds(2);
                        }

                        // focus on the new rock, and make it appear on camera
                        Camera.SetFollowTarget(newObj);
                        float oldFov = Camera.FieldOfView;
                        float oldFollowDistance = Camera.FollowDistance;
                        Camera.FieldOfView = 1.3f;
                        Camera.FollowDistance = 50f;
                        await Services.GetService<ScriptUtils>().WaitSeconds(1);

                        newObj.Visible = true;
                        await ParentGameScreen.ShowDialogueAndWait("A new rock appeared!");

                        Camera.SetFollowTarget(Get<Player>("player1"));
                        Get<Player>("player1").InputEnabled = true;
                        Camera.FieldOfView = oldFov;
                        Camera.FollowDistance = oldFollowDistance;
                    }
                }

            }
            else
            {
                spawnedNewRock = false;
            }

            if (withinDoorInteractTrigger)
            {
                Input inp = this.Services.GetService<Input>();
                if (UserAction.Interact.Pressed(inp))
                {
                    if (Get<Key>("key").IsPickedUp())
                    {
                        Get<Door>("door_key").OpenDoor();
                        openedKeyDoor = true;
                        CheckpointManager.SaveCheckpoint("checkpoint_just_after_opening_door");
                        doorInteractTokenSource.Cancel();
                    }
                    else
                    {
                        await ParentGameScreen.ShowDialogueAndWait("Hmm... Maybe I need something to open this?");
                    }
                }
            }
        }
    }
}
