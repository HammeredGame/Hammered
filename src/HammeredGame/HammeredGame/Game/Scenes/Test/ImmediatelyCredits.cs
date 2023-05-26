using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using Microsoft.Xna.Framework;
using Pleasing;
using System.Threading.Tasks;
using System;

namespace HammeredGame.Game.Scenes.Test
{
    internal class ImmediatelyCredits : Scene
    {
        public ImmediatelyCredits(GameServices services, GameScreen screen) : base(services, screen)
        { }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML("Content/SceneDescriptions/Test/ImmediatelyCredits.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            await Services.GetService<ScriptUtils>().WaitNextUpdate();

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(Grid);

            Get<PressurePlate>("temple_pressure_plate").OnTrigger += async (s, e) =>
            {
                // This event handler gets called in the physics thread, so in order to do UI stuff, we need to use the main thread.
                await Services.GetService<ScriptUtils>().WaitNextUpdate();
                ParentGameScreen.ScreenManager.AddScreen(new CreditRollScreen());
            };
        }
    }
}
