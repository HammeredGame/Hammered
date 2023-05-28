using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Constraints.SolverGroups;
using BEPUutilities;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Screens;
using Pleasing;
using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;


namespace HammeredGame.Game.Scenes.Island1
{
    internal class TreeTutorial : Scene
    {
        public TreeTutorial(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/trees_bgm2");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }
        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Island1/TreeTutorial_voxel.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            ScriptUtils utils = Services.GetService<ScriptUtils>();

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            Get<Tree>("tree").SetTreeFallen(true);

            CollisionGroup waterboundsGroup = new CollisionGroup();
            foreach (var gO in GameObjectsList)
            {
                // Set water bounds objects to a group (usually used to ensure that it doesn't
                // block any rocks in the scene, but here there are no rocks in the scene.
                // If there are no rocks, the water bounds still needs a collision group attached,
                // so attaching an empty collision group here)
                var waterBounds = gO as WaterBoundsObject;
                if (waterBounds != null)
                {
                    waterBounds.Entity.CollisionInformation.CollisionRules.Group = waterboundsGroup;
                }
            }

            //// Set a layer of grid cells which the hammer will not be able to traverse.
            //// This is done to ensure that the hammer will not attempt to find a path consisting of points from that layer onwards.
            //// Whether the "onwards" refers to "upwards" or "downwards" is dependent on the initial position of the hammer.
            //// More often than not, it will be the case that the base floor of the level will have such a "deactivated" layer.
            //// This scene, at least, does so.
            //Microsoft.Xna.Framework.Vector3 floorStart = new (this.Grid.originPoint.X, -30.0f, this.Grid.originPoint.Z),
            //    floorFinish = new(this.Grid.endPoint.X, -30.0f, this.Grid.endPoint.Z);
            //this.Grid.MarkRangeAs(floorStart, floorFinish, false);

            // The above lines of code were commented because there are no obstacles to avoid IN THIS PARTICULAR LEVEL.
            // Hence, the hammer will always follow a straight path route, without ever "escaping" to A*.
            // Keep the above as an example for other scenes/levels.

            //await ParentGameScreen.ShowDialogueAndWait("Woah! Magical teleportation!?");
            //await ParentGameScreen.ShowDialogueAndWait("This hammer must be mythical or something!");
            //await utils.WaitSeconds(1);
            //await ParentGameScreen.ShowDialogueAndWait("...Hold on, mythical?");
            //await ParentGameScreen.ShowDialogueAndWait("I vaguely recall that I met some people\nwho called themselves \"Gods\" yesterday...");

            await ParentGameScreen.ShowDialogueAndWait("Thor, you bloody liar!");
            await ParentGameScreen.ShowDialogueAndWait("This hammer is so heavy it could topple a tree!");

            // Zoom in on the player while crossing the first bridge to emphasise the log
            float originalCameraFollowDistance = Camera.FollowDistance;

            Get<TriggerObject>("log_bridge_trigger").OnTrigger += (_, _) =>
            {
                Tweening.Tween(Camera, nameof(Camera.FollowDistance), 150f, 500, Easing.Quadratic.InOut, LerpFunctions.Float);
            };
            Get<TriggerObject>("log_bridge_trigger").OnTriggerEnd += (_, _) =>
            {
                Tweening.Tween(Camera, nameof(Camera.FollowDistance), originalCameraFollowDistance, 500, Easing.Quadratic.InOut, LerpFunctions.Float);
            };

            // Make sure the hammer is being carried by the player. If the player does not have the
            // hammer, they will be blocked and not allowed to continue to the next level.
            Get<TriggerObject>("end_trigger").Entity.CollisionInformation.CollisionRules.Personal = BEPUphysics.CollisionRuleManagement.CollisionRule.Normal;
            Get<TriggerObject>("end_trigger").OnTrigger += async (_, _) =>
            {
                if (Get<Hammer>("hammer").IsWithCharacter())
                {
                    //await ParentGameScreen.ShowDialogueAndWait("(Huh, you found some traffic cones)");
                    //await ParentGameScreen.ShowDialogueAndWait("(Did you bring them here yesterday?)");
                    ParentGameScreen.InitializeLevel(typeof(TwoIslandPuzzle).FullName, true);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };
        }
    }
}
