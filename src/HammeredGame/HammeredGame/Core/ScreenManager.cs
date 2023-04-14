using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammeredGame.Core
{
    public class ScreenManager
    {
        /// <summary>
        /// List of all screens in the stack, in render order (0 is rendered first, n - 1 is rendered on top)
        /// </summary>
        private readonly List<Screen> screens = new();

        /// <summary>
        /// Whether we are past the LoadContent() step. Until this is true, AddScreen() will not
        /// load content, but will batch them up to load them all when LoadContent() is called.
        /// </summary>
        private bool passedLoadContent;

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
            passedLoadContent = true;
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
                // the rest of the stack, unless it passes through focus
                if (screen.State == ScreenState.Active)
                {
                    if (!screen.PassesFocusThrough)
                    {
                        hasFocus = false;
                    }
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
        /// Add a screen to the stack (rendered on top of everything else). If this function is
        /// called prior to LoadContent(), then the screen will be added to the stack but without
        /// its contents loaded. They will be loaded when LoadContent is called. If this function is
        /// called with a screen that is already preloaded, this function also will not call reload
        /// its contents. Otherwise, this function will call LoadContent() and may be expensive at runtime.
        /// </summary>
        /// <param name="screen"></param>
        public void AddScreen(Screen screen)
        {
            screen.GameServices = services;
            screen.ScreenManager = this;

            // Add the screen first before calling LoadContent on it, since it may add new screens,
            // thereby making an undesired order of addition.
            screens.Add(screen);

            // Load the content only if we have not loaded it before, and we are past LoadContent().
            // Otherwise, we'll load all content within LoadContent since we might still be in the
            // game Initialize() step.
            if (!screen.IsLoaded && passedLoadContent)
            {
                screen.LoadContent();
            }
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
            Func<Screen, string> screenProps = s =>
            {
                // Show only selected properties that are true
                List<string> propertiesToShow = new() { "HasFocus", "IsPartial", "PassesFocusThrough" };
                return "{" + string.Join(",", propertiesToShow.Where(p => (bool)s.GetType().GetProperty(p).GetValue(s, null))) + "}";
            };
            ImGui.TextWrapped($"Current screen stack: {string.Join(", ", screens.Select(s => s.GetType().Name + screenProps(s)))}");
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
