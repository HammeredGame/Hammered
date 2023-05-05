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

namespace HammeredGame.Game.Scenes.Island1
{
    internal class PrototypePuzzle : Scene
    {
        private bool spawnedNewRock = false;
        private bool openedGoalDoor = false;
        private Vector3 newSpawnRockPosition = new Vector3(257.390f, 0.000f, -187.414f);
        private CollisionGroup laserRockGroup;

        public PrototypePuzzle(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/PrototypePuzzle_voxel.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // Set laser to desired length within level
            Laser laser1 = Get<Laser>("laser1");
            laser1.SetLaserDefaultScale(15.0f);

            MoveBlock rock1 = Get<MoveBlock>("rock1");
            MoveBlock rock2 = Get<MoveBlock>("rock2");

            laserRockGroup = new CollisionGroup();
            CollisionGroupPair pair = new CollisionGroupPair(laserRockGroup, laserRockGroup);
            CollisionRules.CollisionGroupRules.Add(pair, CollisionRule.NoSolver);

            laser1.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;
            rock1.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;
            rock2.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;

            Get<Key>("key").SetCorrespondingDoor(Get<Door>("door_key"));

            Get<Door>("door_goal").SetIsGoal(true);
            this.UpdateSceneGrid(Get<Door>("door_key"), false);
            this.UpdateSceneGrid(Get<Door>("door_pp"), false);

            //Get<PressurePlate>("pressureplate").SetTriggerObject(Get<Door>("door_pp"));

            this.UpdateSceneGrid(Get<Wall>("wall"), false);
            this.UpdateSceneGrid(Get<Wall>("wall1"), false);
            this.UpdateSceneGrid(Get<Wall>("wall2"), false);
            this.UpdateSceneGrid(Get<Wall>("wall3"), false);

            // No further initialization required for the <c>UniformGrid</c> instance.

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    await ParentGameScreen.ShowDialogueAndWait("Phewww, that was tough...!");
                    ParentGameScreen.InitializeLevel(typeof(TempleEndLevel).FullName);
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
                        var template_rock = Get<MoveBlock>("rock1");
                        // Generate a new name for the object
                        string name = GenerateUniqueNameWithPrefix(template_rock.GetType().Name.ToLower());

                        // Copy the entity
                        Entity entity = null;
                        if (template_rock.Entity is Box box)
                        {
                            entity = new Box(MathConverter.Convert(newSpawnRockPosition), box.Width, box.Height, box.Length, box.Mass);
                        }
                        else if (template_rock.Entity is Sphere sph)
                        {
                            entity = new Sphere(MathConverter.Convert(newSpawnRockPosition), sph.Radius, sph.Mass);
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
                        newObj.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;

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
                        Camera.FieldOfView = oldFov;
                        Camera.FollowDistance = oldFollowDistance;
                    }
                }

            }
            else
            {
                spawnedNewRock = false;
            }
        }
    }
}
