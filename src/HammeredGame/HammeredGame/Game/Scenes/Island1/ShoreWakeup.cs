﻿using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using System.Collections.Generic;
using System.Threading;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class ShoreWakeup : Scene
    {
        public ShoreWakeup(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/ShoreWakeup.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);

            await Services.GetService<ScriptUtils>().WaitSeconds(1);
            CancellationTokenSource movementPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(new List<string>() { "Move" }, movementPromptTokenSource.Token);

            CancellationTokenSource summonPromptTokenSource = new();
            Get<TriggerObject>("hammer_trigger").OnTrigger += (_, _) =>
            {
                Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
                movementPromptTokenSource.Cancel();

                ParentGameScreen.ShowPromptsFor(new List<string>() { "Summon Hammer" }, summonPromptTokenSource.Token);
            };


            // completion trigger to load next level
            Get<TriggerObject>("end_trigger").OnTrigger += (_, _) =>
            {
                summonPromptTokenSource.Cancel();
                ParentGameScreen.InitializeLevel(typeof(TreeTutorial).FullName);
            };

            // Get<Player>("player").OnMove += async _ => {
            //     System.Diagnostics.Debug.WriteLine("a");
            //     services.GetService<ScriptUtils>.WaitSeconds(5);
            //     System.Diagnostics.Debug.WriteLine("written after 5 seconds of player movement");
            // };

            //Create<Player>("player", services, content.Load<Model>("character-colored"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
            //Create<Hammer>("hammer", services, content.Load<Model>("temp_hammer2"), null, Vector3.Zero, Quaternion.Identity, 0.3f);
        }
    }
}
