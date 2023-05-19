using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using HammeredGame.Game.GameObjects.EnvironmentObjects.InteractableObjs.ImmovableInteractables;
using Microsoft.Xna.Framework;
using Pleasing;
using System.Threading.Tasks;
using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media; 

namespace HammeredGame.Game.Scenes.Endgame
{
    internal class TempleEndLevel : Scene
    {
        private bool ended = false;

        public TempleEndLevel(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/bgm2_4x_b");
            MediaPlayer.IsRepeating = true; 
            MediaPlayer.Play(bgMusic);
        }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Endgame/TempleEndLevel_voxel.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            await Services.GetService<ScriptUtils>().WaitNextUpdate();

            Camera.SetFollowTarget(Get<Player>("player1"));
            Get<Player>("player1").SetActiveCamera(Camera);
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            Get<Hammer>("hammer").SetSceneUniformGrid(Grid);

            // MANUALLY setting areas of the level/scenethe to be unavailable for path routing.
            // Such areas include the floor and the walls of the temple.
            // The player may decide to deviate from the main story (i.e. drop the hammer to the temple)
            // and instead intends to "play around" the scene.
            SetInaccessible();

            await ParentGameScreen.ShowDialogueAndWait("I really need a drink after all this...");

            await ParentGameScreen.ShowDialogueAndWait("A temple with a huge statue of a hammer...\n Gee, I wonder which god this is for.");
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
                //await ParentGameScreen.ShowDialogueAndWait("(The hammer was sucked into the clouds)");
                //await ParentGameScreen.ShowDialogueAndWait("(You hear Thor sounding happy! He'll send you home!");
                await ParentGameScreen.ShowDialogueAndWait("Dear friend, ");
                await ParentGameScreen.ShowDialogueAndWait(
                    "Thank you /n for bringing me the hammer back :)");
                await ParentGameScreen.ShowDialogueAndWait("Same time next week?");
                await Services.GetService<ScriptUtils>().WaitSeconds(2);
                await ParentGameScreen.ShowDialogueAndWait("The End! (for now)");
                Get<Hammer>("hammer").InputEnabled = false;
            }
        }

        // Consider making this function (with perhaps a more fitting signature name) an abstract function
        // which may optionally be implemented in each scene class.
        // The default implementation should be <c>return;</c> (a.k.a. "do nothing").
        void SetInaccessible()
        {
            Vector3 wallStart, wallFinish;

            //// Floor
            //wallStart = new(this.Grid.originPoint.X, this.Grid.originPoint.Y, this.Grid.originPoint.Z),
            //wallFinish = new(this.Grid.endPoint.X, this.Grid.originPoint.Y, this.Grid.endPoint.Z);
            //this.Grid.MarkRangeAs(wallStart, wallFinish, false);

            // Not worth doing the hammer. The particular level can lead to unwanted behaviour.
            // If the hammer is dropped at the very edge of the map, and the character distances themselves
            // sufficiently away from the shore, then the hammer cannot return to the character.
            // Functionality is preferred over completeness.

            const float lowestBrickHeight = 3.0f;

            // Wall 1 (right of hammer statue):
            wallStart = new(269.0f, Grid.originPoint.Y, -264.0f);
            wallFinish = new(273.0f, 40.0f, -139.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);

            // Wall 2 (behind the hammer statue) PART 1:
            wallStart = new(210.0f, Grid.originPoint.Y, -264.0f);
            wallFinish = new(273.0f, 40.0f, -264.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);
            // Wall 2 (behind the hammer statue) PART 2:
            wallStart = new(172.0f, Grid.originPoint.Y, -260.0f);
            wallFinish = new(210.0f, lowestBrickHeight, -260.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);
            // Wall 2 (behind the hammer statue) PART 3:
            wallStart = new(150.0f, Grid.originPoint.Y, -260.0f);
            wallFinish = new(172.0f, 40.0f, -260.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);

            // Wall 3 (left of the hammer statue) PART 1:
            wallStart = new(142.0f, Grid.originPoint.Y, -260.0f);
            wallFinish = new(142.0f, 40.0f, -178.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);
            // Wall 3 (left of the hammer statue) PART 2:
            wallStart = new(142.0f, Grid.originPoint.Y, -178.0f);
            wallFinish = new(142.0f, lowestBrickHeight + Grid.sideLength, -140.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);

            // Wall 4 (front of the hammer statue, main entrance) PART 1
            wallStart = new(142.0f, Grid.originPoint.Y, -140.0f);
            wallFinish = new(182.0f, lowestBrickHeight + Grid.sideLength, -140.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);
            // Wall 4 (front of the hammer statue, main entrance) PART 2
            wallStart = new(230.0f, Grid.originPoint.Y, -140.0f);
            wallFinish = new(270.0f, lowestBrickHeight + Grid.sideLength, -140.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);

            // Gate (front of the hammer statue, main entrance) LEFT COLUMN
            wallStart = new(190.0f, Grid.originPoint.Y, -140.0f);
            wallFinish = new(190.0f, Grid.endPoint.Y, -140.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);
            // Gate (front of the hammer statue, main entrance) ARC
            wallStart = new(190.0f, Grid.endPoint.Y - Grid.sideLength, -140.0f);
            wallFinish = new(220.0f, Grid.endPoint.Y - Grid.sideLength, -140.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);
            // Gate (front of the hammer statue, main entrance) RIGHT COLUMN
            wallStart = new(220.0f, Grid.originPoint.Y, -140.0f);
            wallFinish = new(220.0f, Grid.endPoint.Y, -140.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);

            // Hammer statue
            wallStart = new(200.0f, Grid.originPoint.Y, -230.0f);
            wallFinish = new(210.0f, Grid.endPoint.Y, -212.0f);
            Grid.MarkRangeAs(wallStart, wallFinish, false);

        }
    }
}
