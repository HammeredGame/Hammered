using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;
using HammeredGame.Game.Screens;
using Pleasing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TreeTutorial : Scene
    {
        public TreeTutorial(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/TreeTutorial_voxel.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            ScriptUtils utils = Services.GetService<ScriptUtils>();

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            Get<Tree>("tree").SetTreeFallen(true);

            await ParentGameScreen.ShowDialogueAndWait("Woah! Magical teleportation!?");
            await ParentGameScreen.ShowDialogueAndWait("This hammer must be mythical or something!");
            await utils.WaitSeconds(1);
            await ParentGameScreen.ShowDialogueAndWait("...Hold on, mythical?");
            await ParentGameScreen.ShowDialogueAndWait("I vaguely recall that I met some people\nwho called themselves \"Gods\" yesterday...");

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
                    await ParentGameScreen.ShowDialogueAndWait("(Huh, you found some traffic cones)");
                    await ParentGameScreen.ShowDialogueAndWait("(Did you bring them here yesterday?)");
                    ParentGameScreen.InitializeLevel(typeof(TwoIslandPuzzle).FullName);
                }
                else
                {
                    await ParentGameScreen.ShowDialogueAndWait("The hammer might be needed later, let's bring it.");
                }
            };
        }
    }
}
