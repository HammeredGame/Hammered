using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using Microsoft.Xna.Framework;
using Pleasing;

namespace HammeredGame.Game.Scenes.Island1
{
    internal class TempleEndLevel : Scene
    {
        private bool ended = false;
        
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

        public override async void Update(GameTime gameTime, bool screenHasFocus, bool isPaused)
        {
            base.Update(gameTime, screenHasFocus, isPaused);

            if (Get<PressurePlate>("temple_pressure_plate").IsActivated() && !ended)
            {
                ended = true;
                Hammer hammer = Get<Hammer>("hammer");
                hammer.DropHammer();
                hammer.Entity.Gravity = new BEPUutilities.Vector3(0, 50f, 0);
                await ParentGameScreen.ShowDialogueAndWait("(The hammer was sucked into the clouds)");
                await ParentGameScreen.ShowDialogueAndWait("(You hear Thor sounding happy! He'll send you home!");
                await Services.GetService<ScriptUtils>().WaitSeconds(2);
                await ParentGameScreen.ShowDialogueAndWait("The End! (for now)");
                Get<Hammer>("hammer").InputEnabled = false;
            }
        }
    }
}
