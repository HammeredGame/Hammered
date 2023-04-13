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
            foreach (Screen screen in screens)
            {
                screen.LoadContent();
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

                if (screen.IsExiting)
                {
                    RemoveScreen(screen);
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

        public void AddScreen(Screen screen)
        {
            screen.GameServices = services;
            screen.ScreenManager = this;
            screen.LoadContent();
            screens.Add(screen);
        }

        public void RemoveScreen(Screen screen)
        {
            screen.UnloadContent();
            screens.Remove(screen);
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
