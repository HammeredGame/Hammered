using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace HammeredGame.Core
{
    public class ScreenManager
    {
        /// <summary>
        /// List of all screens in the stack, in render order (0 is rendered first, n - 1 is rendered on top)
        /// </summary>
        private readonly List<Screen> screens = new();

        /// <summary>
        /// Game services, graphics device, and the main render target that all Screens can access.
        /// </summary>
        private readonly GameServices services;
        public GraphicsDevice GraphicsDevice;
        public RenderTarget2D MainRenderTarget;

        public ScreenManager(GameServices services, GraphicsDevice gpu, RenderTarget2D mainRenderTarget)
        {
            this.services = services;
            this.GraphicsDevice = gpu;
            this.MainRenderTarget = mainRenderTarget;
        }

        /// <summary>
        /// Should be called once during main game LoadContent(). Calls LoadContent for any screens
        /// that were already added before this through AddScreen.
        /// </summary>
        public void LoadContent()
        {
            // Screens may load additional screens, so use a for loop instead of a foreach loop.
            // We assume screens won't exit during content load...
            for (int i = 0; i < screens.Count; i++) {
                screens[i].LoadContent();
            }
        }

        /// <summary>
        /// Unloads content from all screens.
        /// </summary>
        public void UnloadContent()
        {
            foreach (Screen screen in screens)
            {
                screen.UnloadContent();
            }
        }

        /// <summary>
        /// Calls Update() on all screens on the stack, passing them parameters on whether or not it
        /// has focus (is in the foreground), or whether it is completely hidden from view by other screens.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Create a working copy of the screens list since Update() may add or remove screens,
            // but we only want to loop over the screens that are present at this point in time.
            List<Screen> screensWorkingCopy = new();
            screensWorkingCopy.AddRange(screens);

            bool hasFocus = true;
            bool isCoveredByNonPartialScreen = false;

            // Traverse backwards, so from the top of the stack. This way we can tell the ones on
            // the bottom that they are covered and don't have focus.
            for (int i = screensWorkingCopy.Count - 1; i >= 0; i--)
            {
                Screen screen = screensWorkingCopy[i];
                screen.UpdateWithPrelude(gameTime, hasFocus, isCoveredByNonPartialScreen);

                // If the screen is drawn in some amount, then mark it as having stolen focus from
                // the rest of the stack
                if (screen.State == ScreenState.Active)
                {
                    hasFocus = false;
                    if (!screen.IsPartial)
                    {
                        isCoveredByNonPartialScreen = true;
                    }
                }
            }
        }

        /// <summary>
        /// Calls Draw() on all screens on the stack that aren't totally hidden in the background.
        /// </summary>
        /// <param name="gameTime"></param>
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

        /// <summary>
        /// Calls LoadContent() on the screen, for preloading content. This sets the IsLoaded flag
        /// in the screen. Resulting screens can be added via AddScreen() without much delay.
        /// </summary>
        /// <param name="screen"></param>
        public void PreloadScreen(Screen screen)
        {
            screen.GameServices = services;
            screen.ScreenManager = this;
            screen.LoadContent();
        }

        /// <summary>
        /// Add a screen to the stack (rendered on top of everything else). If the screen is already
        /// preloaded, this function is very cheap. Otherwise, it calls LoadContent() and could be expensive.
        /// </summary>
        /// <param name="screen"></param>
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

        /// <summary>
        /// Remove a screen from the stack, optionally calling UnloadContent() on it.
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="alsoUnloadContent"></param>
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
