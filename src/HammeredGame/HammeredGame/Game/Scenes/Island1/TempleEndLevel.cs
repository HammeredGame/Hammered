using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TempleEndLevel : Scene
    {
        public TempleEndLevel(GameServices services, GameScreen screen) : base(services, screen)
        {
            CreateFromXML($"Content/SceneDescriptions/Island1/TempleEndLevel_voxel.xml");
            OnSceneStart();
        }

        protected override async void OnSceneStart()
        {
            await Services.GetService<ScriptUtils>().WaitNextUpdate();

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            await ParentGameScreen.ShowDialogueAndWait("A temple with a huge statue of a hammer...\nGee, I wonder which god this is for.");
            await ParentGameScreen.ShowDialogueAndWait("Let's hurry up and drop off the hammer!");
        }
    }
}
