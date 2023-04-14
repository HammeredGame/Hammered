using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HammeredGame.Core
{
    public class ScreenManager
    {
        private readonly List<Screen> screens = new();
        private readonly GameServices services;
        public GraphicsDevice GraphicsDevice;
        public RenderTarget2D MainRenderTarget;

        public ScreenManager(GameServices services, GraphicsDevice gpu, RenderTarget2D mainRenderTarget)
        {
            this.services = services;
            this.GraphicsDevice = gpu;
            this.MainRenderTarget = mainRenderTarget;
        }

        public void LoadContent()
        {
            // Call LoadContent for screens that were already added to the list through AddScreen
            for (int i = 0; i < screens.Count; i++) {
                screens[i].LoadContent();
            }
        }

        public void UnloadContent()
        {
            foreach (Screen screen in screens)
            {
                screen.UnloadContent();
            }
        }

        public void Update(GameTime gameTime)
        {
            List<Screen> screensWorkingCopy = new();
            screensWorkingCopy.AddRange(screens);

            bool otherScreenHasFocus = false;
            bool coveredByOtherScreen = false;
            for (int i = screensWorkingCopy.Count - 1; i >= 0; i--)
            {
                Screen screen = screensWorkingCopy[i];
                screen.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

                if (screen.State == ScreenState.Active)
                {
                    otherScreenHasFocus = true;
                    if (!screen.IsPartial)
                    {
                        coveredByOtherScreen = true;
                    }
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            foreach (Screen screen in screens)
            {
                if (screen.State != ScreenState.Hidden)
                {
                    screen.Draw(gameTime);
                }
            }
        }

        public void PreloadScreen(Screen screen)
        {
            screen.GameServices = services;
            screen.ScreenManager = this;
            screen.LoadContent();
        }

        public void AddScreen(Screen screen)
        {
            screen.GameServices = services;
            screen.ScreenManager = this;

            if (!screen.IsLoaded)
            {
                screen.LoadContent();
            }

            screens.Add(screen);
        }

        public void RemoveScreen(Screen screen, bool alsoUnloadContent = true)
        {
            screens.Remove(screen);

            if (alsoUnloadContent) screen.UnloadContent();
        }

        public void UI()
        {
            ImGui.TextWrapped($"Current screen stack: {string.Join(", ", screens)}");
            foreach (Screen screen in screens)
            {
                if (screen.State != ScreenState.Hidden)
                {
                    screen.UI();
                }
            }
        }
    }
}
