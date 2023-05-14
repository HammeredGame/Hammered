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

namespace HammeredGame.Game.Scenes.Island1
{
    internal class LaserTutorial : Scene
    {
        public LaserTutorial(GameServices services, GameScreen screen) : base(services, screen)
        { }
        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island1/LaserTutorial_voxel.xml", progress);
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
            Laser laser1 = Get<Laser>("laser1");
            laser1.SetLaserDefaultScale(10.0f);
            Laser laser2 = Get<Laser>("laser2");
            laser2.SetLaserDefaultScale(12.0f);

            MoveBlock rock1 = Get<MoveBlock>("rock1");
            //MoveBlock rock2 = Get<MoveBlock>("rock2");

            var noSolverGroup = new CollisionGroup();
            CollisionGroupPair pair = new CollisionGroupPair(noSolverGroup, noSolverGroup);
            CollisionRules.CollisionGroupRules.Add(pair, CollisionRule.NoSolver);

            laser1.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
            laser2.Entity.CollisionInformation.CollisionRules.Group= noSolverGroup;
            rock1.Entity.CollisionInformation.CollisionRules.Group = noSolverGroup;
            //rock2.Entity.CollisionInformation.CollisionRules.Group = laserRockGroup;

            foreach (var gO in GameObjectsList)
            {
                // Check for rocks in the scene
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

                // Check for walls in the scene
                var wall = gO as Wall;
                if (wall != null)
                {
                    this.UpdateSceneGrid(wall, false, 0.9);
                }
            }

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    await ParentGameScreen.ShowDialogueAndWait("That was surprisingly easy...! I hope the next one\nis also like that.");
                    ParentGameScreen.InitializeLevel(typeof(PrototypePuzzle).FullName);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };
        }
    }
}
