using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.Screens;
using System;
using System.Threading.Tasks;

namespace HammeredGame.Game.Scenes.Test
{
    internal class JustHammer : Scene
    {
        public JustHammer(GameServices services, GameScreen screen) : base(services, screen)
        { }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Test/JustHammer.xml", progress);
        }

        protected override void OnSceneStart()
        {
            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);
            Camera.SetFollowTarget(Get<Hammer>("hammer"));
        }
    }
}
