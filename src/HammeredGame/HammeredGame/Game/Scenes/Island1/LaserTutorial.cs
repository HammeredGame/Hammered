using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Constraints.SolverGroups;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.ImmovableObstacles;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Screens;
using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class LaserTutorial : Scene
    {
        public LaserTutorial(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/bgm2_4x_b");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }
        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island1/LaserTutorial_voxel.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);
            // Do not allow the hammer to traverse the floor.
            Microsoft.Xna.Framework.Vector3 floorStart, floorFinish;
            floorStart = new(this.Grid.originPoint.X, this.Grid.originPoint.Y, this.Grid.originPoint.Z);
            floorFinish = new(this.Grid.endPoint.X, this.Grid.originPoint.Y, this.Grid.endPoint.Z);
            this.Grid.MarkRangeAs(floorStart, floorFinish, false);

            // Set laser to desired length within level
            Laser laser1 = Get<Laser>("laser1");
            laser1.SetLaserDefaultScale(10.0f);
            Laser laser2 = Get<Laser>("laser2");
            laser2.SetLaserDefaultScale(12.0f);

            MoveBlock rock1 = Get<MoveBlock>("rock1");
            //MoveBlock rock2 = Get<MoveBlock>("rock2");

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

            await ParentGameScreen.ShowDialogueAndWait("Hmm, I wonder what's going on here…I’ve/n been caught between a rock and a hard place.");

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    await ParentGameScreen.ShowDialogueAndWait("That was surprisingly easy...!");
                    await ParentGameScreen.ShowDialogueAndWait("I hope the next one is also like that.");
                    ParentGameScreen.InitializeLevel(typeof(PrototypePuzzle).FullName, true);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };
        }
    }
}
