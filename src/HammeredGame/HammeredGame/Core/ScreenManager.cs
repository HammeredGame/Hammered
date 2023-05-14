using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HammeredGame.Core
{
    /// <summary>
    /// A ScreenManager manages the active "screen" stack in the game. There is
    /// only only ScreenManager ever active throughout the entire game, and it
    /// controls what is visible. For example, a stack containing a PauseScreen
    /// and a GameScreen will render as the pause screen on top of the gameplay
    /// screen.
    /// </summary>
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
        /// This main render target is drawn to the GPU without any processing (since it may contain
        /// things like UI elements). Any screen-wide shaders that should be applied to the whole
        /// game scene should be done within GameScreen.
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
            for (int i = 0; i < screens.Count; i++)
            {
                screens[i].LoadContent();
            }

            // Set the internal flag that indicates that any further handling of
            // this.AddScreen() will need to call LoadContent() on it, since the main game LoadContent()
            // has already passed.
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

                // If the screen is drawn in some amount (even mid-transition), then mark it as
                // having stolen focus from the rest of the stack, unless it passes through focus
                if (screen.State != ScreenState.Hidden)
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
        /// in the screen. Resulting screens can be added via AddScreen() without much delay. This
        /// should be called at the main game LoadContent() at the earliest, since it will call
        /// the screen LoadContent() function which may rely on GPU or GameServices being initialised.
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
        /// its contents loaded. They will be loaded when this.LoadContent() is called. This is
        /// because we may not have access to a various things when this function is called,
        /// like an initialised GPU or certain GameServices (like ContentManager). We guarantee
        /// that they are present by calling screen.LoadContent() when the main game LoadContent()
        /// is called.
        /// <para/>
        /// If this function is called with a screen that is already preloaded (for example, to
        /// quickly remove/add screens during the game loop), this function will not call
        /// screen.LoadContent() again to reload its contents.
        /// <para/>
        /// Finally, if the screen is NOT preloaded, and we are past the main game LoadContent()
        /// stage, this function will call screen.LoadContent() and may be expensive at runtime.
        /// </summary>
        /// <param name="screen"></param>
        public void AddScreen(Screen screen)
        {
            // Do nothing if the screen already exists. This is fine for the current set up but
            // may need to change if we legitimately need to have two identical instances of a
            // screen added to the stack. When that happens, care needs to be taken to prevent
            // quick double-clicks from adding two screens from menus.
            if (screens.Contains(screen)) return;

            // Set the variables that we are passing to the screen
            screen.GameServices = services;
            screen.ScreenManager = this;

            // Add the screen first before calling its LoadContent() function. This is because
            // screen.LoadContent() might itself try to add new screens using AddScreen(), and
            // since that will complete first (before computation continues here), the order of
            // added screens will be switched undesirably.
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
        /// Remove a screen from the stack, optionally calling UnloadContent() on it. Usually you'd
        /// want to unload the contents too, unless the same screen is planned to be re-added again.
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="alsoUnloadContent"></param>
        public void RemoveScreen(Screen screen, bool alsoUnloadContent = true)
        {
            screens.Remove(screen);

            if (alsoUnloadContent) screen.UnloadContent();
        }

        /// <summary>
        /// Updates the resolution for each of the screens on the stack by calling SetResolution on them.
        /// </summary>
        /// <param name="width">The new render resolution</param>
        /// <param name="height">The new render resolution</param>
        public void SetResolution(int width, int height)
        {
            foreach (Screen screen in screens)
            {
                screen.SetResolution(width, height);
            }
        }

        /// <summary>
        /// Debug information about the UI.
        /// </summary>
        public void UI()
        {
            // screenProps is a function that given a Screen, shows all of the hardcoded properties
            // that are True on that screen.
            Func<Screen, string> screenProps = s =>
            {
                // Show only debug-worthy important ones
                List<string> propertiesToShow = new() { "HasFocus", "IsPartial", "PassesFocusThrough" };
                return "{" + string.Join(",", propertiesToShow.Where(p => (bool)s.GetType().GetProperty(p).GetValue(s, null))) + "}";
            };
            // Call screenProps on all current screens and show them on the UI
            ImGui.TextWrapped($"Current screen stack: {string.Join(", ", screens.Select(s => s.GetType().Name + screenProps(s)))}");

            // Show any screen-specific UI for any active screen, from the top
            for (int i = screens.Count - 1; i >= 0; i--)
            {
                if (screens[i].State != ScreenState.Hidden)
                {
                    screens[i].UI();
                }
            }
        }
    }
}
