using ImMonoGame.Thing;
using Microsoft.Xna.Framework;

namespace HammeredGame.Core
{
    public enum ScreenState
    {
        Hidden, // Automatically becomes this when screen should not be drawn
        Active // Automatically becomes this when screen should be drawn
    }

    /// <summary>
    /// A screen is a.... a screen. The game window always contains at least one screen, and draws
    /// all of them in the stack from bottom up.
    /// </summary>
    public abstract class Screen : IImGui
    {
        /// <summary>
        /// Whether this screen completely blocks out screens under it. If false, all screens below
        /// it will receive { isCoveredByNonPartialScreen = true } in their Update() calls.
        /// </summary>
        public bool IsPartial { get; protected set; }

        /// <summary>
        /// The draw state of the screen. This is updated in Update() automatically.
        /// </summary>
        public ScreenState State { get; protected set; } = ScreenState.Active;

        /// <summary>
        /// Whether this screen's contents are already loaded. Set to true by LoadContent(). Screens
        /// with this set to true can be added to the ScreenManager cheaply.
        /// </summary>
        public bool IsLoaded { get; protected set; }

        /// <summary>
        /// Whether another screen exists above this screen layer.
        /// </summary>
        private bool isBelowAnotherScreen;

        /// <summary>
        /// A screen has "focus" when it is drawn and another screen doesn't exist on top. Input
        /// handling can try to rely on this boolean.
        /// </summary>
        public bool HasFocus {
            get { return State == ScreenState.Active && !isBelowAnotherScreen; }
        }

        /// <summary>
        /// The game services (set by ScreenManager) that the screen has access to.
        /// </summary>
        public GameServices GameServices { get; set; }

        /// <summary>
        /// The parent screen manager.
        /// </summary>
        public ScreenManager ScreenManager { get; set; }

        /// <summary>
        /// Load any content required for the screen. If this is expensive, it's recommended to
        /// preload this screen through ScreenManager.PreloadScreen().
        /// </summary>
        public virtual void LoadContent() {
            IsLoaded = true;
        }

        /// <summary>
        /// Unload any content specific to this screen.
        /// </summary>
        public virtual void UnloadContent() { }

        /// <summary>
        /// Called on every game update as long as the screen is in the stack. The base
        /// implementation of this function changes the draw State and whether it has focus.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="isBelowAnotherScreen">Whether a screen exists above this</param>
        /// <param name="isCoveredByNonPartialScreen">Whether a non-partial screen exists above this</param>
        public virtual void Update(GameTime gameTime, bool isBelowAnotherScreen, bool isCoveredByNonPartialScreen) {
            this.isBelowAnotherScreen = isBelowAnotherScreen;

            if (isCoveredByNonPartialScreen)
            {
                State = ScreenState.Hidden;
            } else
            {
                State = ScreenState.Active;
            }
        }

        /// <summary>
        /// Called to draw.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Draw(GameTime gameTime) { }

        /// <summary>
        /// Exit this screen.
        /// </summary>
        /// <param name="alsoUnloadContent">Whether to call UnloadContent() too</param>
        public void ExitScreen(bool alsoUnloadContent = true)
        {
            ScreenManager.RemoveScreen(this, alsoUnloadContent);
        }

        public virtual void UI() { }

    }
}
