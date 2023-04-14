using HammeredGame.Core;
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

            // set active camera to determine which way is forward
            Get<Player>("player1").SetActiveCamera(Camera);

            // drop hammer here to set state as Dropped, so when we set the owner player later it
            // won't fly back
            Get<Hammer>("hammer").DropHammer();

            // Show movement prompt 1 second after launch
            await Services.GetService<ScriptUtils>().WaitSeconds(1);
            CancellationTokenSource movementPromptTokenSource = new();
            ParentGameScreen.ShowPromptsFor(new List<string>() { "Move" }, movementPromptTokenSource.Token);

            // On entering hammer vicinity show hammer prompt
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
        }
    }
}
