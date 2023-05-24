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

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);
        }
    }
}
