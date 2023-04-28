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
        public bool IsPartial { get; protected set; } = false;

        /// <summary>
        /// Whether this screen steals focus or passes it through, i.e. screens below this also get
        /// focus or not.
        /// </summary>
        public bool PassesFocusThrough { get; protected set; } = false;

        /// <summary>
        /// The draw state of the screen. This is updated in Update() automatically. It is Hidden
        /// when the screen is behind some other screen that has IsPartial set to false, i.e. a
        /// full-screen screen. It is set to Active if the screen is drawn in some amount.
        /// </summary>
        public ScreenState State { get; protected set; } = ScreenState.Active;

        /// <summary>
        /// Whether this screen's contents are already loaded. Set to true by LoadContent(). Screens
        /// with this set to true can be added to the ScreenManager cheaply.
        /// </summary>
        public bool IsLoaded { get; protected set; } = false;

        /// <summary>
        /// A screen has "focus" when it is drawn and nothing else has taken its focus. Input
        /// handling should try to rely on this boolean.
        /// </summary>
        public bool HasFocus { get; protected set; } = false;

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
        public virtual void UnloadContent() {
            IsLoaded = false;
        }

        /// <summary>
        /// Called on every game update as long as the screen is in the stack. This function is not
        /// virtual. This is to prevent implementations of Screen Update() methods to access the
        /// hasFocus and <paramref name="isCoveredByNonPartialScreen"/> arguments.
        /// <para />
        /// Use Update() for implementing an Update function.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="hasFocus">Whether the screen has focus</param>
        /// <param name="isCoveredByNonPartialScreen">
        /// Whether a non-partial screen exists above this
        /// </param>
        public void UpdateWithPrelude(GameTime gameTime, bool hasFocus, bool isCoveredByNonPartialScreen) {
            this.HasFocus = hasFocus;

            if (isCoveredByNonPartialScreen)
            {
                State = ScreenState.Hidden;
            } else
            {
                State = ScreenState.Active;
            }

            Update(gameTime);
        }

        /// <summary>
        /// Called on every game update as long as the screen is in the stack. Use HasFocus for
        /// checking if it has focus, and Status for checking if it is drawn at all.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime) { }

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
