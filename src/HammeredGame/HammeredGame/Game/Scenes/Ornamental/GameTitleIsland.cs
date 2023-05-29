using BEPUphysics;
using HammeredGame.Core;
using HammeredGame.Game.GameObjects;
using HammeredGame.Game.GameObjects.EmptyGameObjects;
using HammeredGame.Game.Screens;
using Pleasing;
using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using HammeredGame.Game.GameObjects.EnvironmentObjects.ObstacleObjs.UnbreakableObstacles.MovableObstacles;


namespace HammeredGame.Game.Scenes.Ornamental
{
    internal class GameTitleIsland : Scene
    {
        public GameTitleIsland(GameServices services, GameScreen screen) : base(services, screen)
        {
            Song bgMusic;
            bgMusic = services.GetService<ContentManager>().Load<Song>("Audio/balanced/bgm2_amb");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);
        }

        protected override async Task LoadSceneContent(IProgress<int> progress)
        {
            await base.LoadSceneContent(progress);
            await CreateFromXML($"Content/SceneDescriptions/Ornamental/GameTitleScene.xml", progress);
        }

        protected override async void OnSceneStart()
        {
            await Services.GetService<ScriptUtils>().WaitNextUpdate();

            Camera.SetFollowTarget(Get<Player>("player1"));

            // set active camera to determine which way is forward
            Get<Player>("player1").SetActiveCamera(Camera);
            // set hammer owner so we can now summon it
            Get<Hammer>("hammer").SetOwnerPlayer(Get<Player>("player1"));
            // Allow <c>Hammer</c> instance to have access to the grid of the scene for the path planning.
            // THIS IS REQUIRED FOR ALL SCENES!
            Get<Hammer>("hammer").SetSceneUniformGrid(this.Grid);

            // Set all trunks as already toppled, so that they do not interact with the hammer when it is called.
            foreach (GameObject treeObject in this.GameObjects.Values)
            {
                if (treeObject.GetType() == typeof(Tree))
                {
                    if ((string)treeObject.Texture.Name == "Meshes/Trees/trunk_texture")
                    {
                        (treeObject as Tree).SetTreeFallen(true);
                    }
                }
            }

            // Enable player input now that the wake up animation has finished playing
            Get<Player>("player1").InputEnabled = true;
            Get<Hammer>("hammer").InputEnabled = true;

            // The cutscene starts the moment the player calls back the hammer for the first time.
            await Services.GetService<ScriptUtils>().WaitEvent(Get<Hammer>("hammer"), "OnSummon");
            await ZoomoutCutScene();

        }

        private async Task ZoomoutCutScene()
        {
            // Disable player and hammer input once the hammer is called. Let the cutscene unfold.
            Get<Player>("player1").InputEnabled = false;
            Get<Hammer>("hammer").InputEnabled = false;

            // Follow and zoom on the hammer toppling the standing trees...
            Camera.SetFollowTarget(Get<Hammer>("hammer"));
            Camera.FollowDistance = 100f;
            // Setting consistent following angles for both when the hammer is called
            // and when focusing on the game title.
            Camera.FollowAngleHorizontal = -94.263f;
            Camera.FollowAngleVertical = 1.491f;

            await Services.GetService<ScriptUtils>().WaitEvent(Get<Player>("player1"), "OnHammerRetrieved");
            // ...until the hammer is retrieved from the player (i.e. the hammer has travelled to the player).
            // Revert camera to normal: normal viewing distance and following the player.
            Camera.SetFollowTarget(Get<TriggerObject>("final_camera_focus"));
            //Camera.FollowAngleHorizontal = -94.263f;
            //Camera.FollowAngleVertical = 1.491f;

            float finalCameraDistance = 743f;
            // Interpolate the camera distance
            TweenTimeline finalCameraDistanceInterpolation = Tweening.NewTimeline();
            finalCameraDistanceInterpolation
                .AddFloat(Camera, nameof(Camera.FollowDistance))
                .AddFrame(0, Camera.FollowDistance)
                .AddFrame(4000, finalCameraDistance, Easing.Linear);
            finalCameraDistanceInterpolation.Start();

            // Interpolate the focus. If it feels like it makes the player too dizzy, experiment with:
            // a) selecting a larger initial follow distance
            // b) remove this segment entirely to see if the zooming out result alone is satisfactory.
            // This process is very closely related to the orientation of the hammer -> character
            TweenTimeline finalCameraFocusInterpolation = Tweening.NewTimeline();
            finalCameraFocusInterpolation
                .AddVector3(Camera, nameof(Camera.Target))
                .AddFrame(0, Get<Player>("player1").Position)
                .AddFrame(5000, Get<TriggerObject>("final_camera_focus").Position, Easing.Quadratic.In);
            finalCameraFocusInterpolation.Start();

        }
    }
}
