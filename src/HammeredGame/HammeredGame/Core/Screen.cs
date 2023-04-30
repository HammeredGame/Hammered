using ImMonoGame.Thing;
using Microsoft.Xna.Framework;

namespace HammeredGame.Core
{
    public enum ScreenState
    {
        Hidden, // Automatically becomes this when screen should not be drawn
        Active, // Automatically becomes this when screen should be drawn
        TransitionIn, // Transition state from Hidden -> Active
        TransitionOut // Transition state from Active -> Hidden/Exit
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
        public ScreenState State { get; protected set; } = ScreenState.Hidden;

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
        /// Flag that is set when the screen is bound to be removed from the stack. This is
        /// necessary because we need to distinguish between the two cases for when a screen is in
        /// the middle of an outgoing transition: it could just be transitioning to Hidden because
        /// another screen is obstructing it, or it could be exiting itself from the stack.
        /// <para/>
        /// This flag gets set to true in ExitScreen() and checked in Update()
        /// </summary>
        private bool isNowExiting = false;

        /// <summary>
        /// Because ExitScreen() doesn't immediately remove our screen from the stack (but rather
        /// marks it for exit using isNowExiting and transitions off), we need a way to retain our
        /// preference for calling Unload() or not.
        /// </summary>
        private bool unloadContentAfterExit = true;

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
        /// hasFocus and <paramref name="isCoveredByNonPartialScreen"/> arguments (for
        /// encapsulation), and to prevent implementations from messing with transition functionality.
        /// <para/>
        /// Use Update() for implementing an Update function.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="hasFocus">Whether the screen has focus</param>
        /// <param name="isCoveredByNonPartialScreen">
        /// Whether a non-partial screen exists above this
        /// </param>
        public void UpdateWithPrelude(GameTime gameTime, bool hasFocus, bool isCoveredByNonPartialScreen) {
            this.HasFocus = hasFocus;

            // The screen is marked to exit by ExitScreen(), we should transition off and remove
            // ourselves from the screen manager when done.
            if (isNowExiting)
            {
                // Whether this is the first frame that we're in this branch
                bool firstTransitionFrame = State != ScreenState.TransitionOut && State != ScreenState.Hidden;
                if (UpdateTransitionOut(gameTime, firstTransitionFrame))
                {
                    // Transition is complete, remove the screen and unload as necessary
                    ScreenManager.RemoveScreen(this, unloadContentAfterExit);

                    // Reset the flags and set the state to Hidden, so the screen can enter again
                    // fresh if necessary
                    isNowExiting = false;
                    State = ScreenState.Hidden;

                    // Return early so we don't call any Update()s after we have exited and maybe
                    // even unloaded contents
                    return;
                }

                // As long as we are transitioning, keep the state as TransitionOut
                State = ScreenState.TransitionOut;

                // todo: perhaps we should early return here and not call further Update() on this
                //       screen (otherwise it may cause unwanted side-effects like another input
                //       being registered)
            }
            // The screen is not marked to exit and be removed from the stack, but it is fully
            // covered and we're going to be hiding it. We'll still play the outgoing transition,
            // but there are no early returns in this branch since even hidden screens should still
            // be Update()-ed.
            else if (isCoveredByNonPartialScreen)
            {
                // Whether this is the first frame that we're in this branch
                bool firstTransitionFrame = State != ScreenState.TransitionOut && State != ScreenState.Hidden;
                if (UpdateTransitionOut(gameTime, firstTransitionFrame))
                {
                    // The transition completed
                    State = ScreenState.Hidden;
                }
                else
                {
                    // We're still mid-transition
                    State = ScreenState.TransitionOut;
                }
            }
            // The screen is active and in view.
            else
            {
                // Whether this is the first frame that we're in this branch
                bool firstTransitionFrame = State != ScreenState.TransitionIn && State != ScreenState.Active;
                if (UpdateTransitionIn(gameTime, firstTransitionFrame))
                {
                    // The transition completed
                    State = ScreenState.Active;
                }
                else
                {
                    // We're still mid-transition
                    State = ScreenState.TransitionIn;
                }
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
        /// Called on game update during the screen transitioning from Hidden to Active state. Since
        /// scenes by default start being Hidden, this is also called on the first addition of the
        /// screen even if it wasn't explicitly hidden before.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="firstFrame">
        /// True if this is the first frame for this transition, you can use this to set up
        /// animations or timers.
        /// </param>
        /// <returns>
        /// True if the transition is completed and the screen can now turn Active, otherwise false.
        /// Once true is returned, this function will no longer be called since it is assumed that
        /// the transition is finished.
        /// </returns>
        public virtual bool UpdateTransitionIn(GameTime gameTime, bool firstFrame) { return true;  }

        /// <summary>
        /// Called on game update during the screen transitioning from Active to Hidden state, or
        /// from Active to exit state (which implicitly is a change to Hidden + removal).
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="firstCall">
        /// True if this is the first frame of this transition, you can use this to set up
        /// animations or timers.
        /// </param>
        /// <returns>
        /// True if the transition is completed and the screen can now turn Hidden or removed from
        /// the screen manager (based on whether this transition was called because another screen
        /// displayed over it, or because ExitScreen was called on it). Once true is returned, this
        /// function will no longer be called since it is assumed that the transition is finished.
        /// </returns>
        public virtual bool UpdateTransitionOut(GameTime gameTime, bool firstCall) { return true; }

        /// <summary>
        /// Called to draw.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Draw(GameTime gameTime) { }

        /// <summary>
        /// Mark this screen as ready to exit, which will cause the exit transition to start on the
        /// next call to Update(). Because there may be transitions, the screen's Update() method
        /// may still be called after this.
        /// </summary>
        /// <param name="alsoUnloadContent">Whether to call UnloadContent() too</param>
        public void ExitScreen(bool alsoUnloadContent = true)
        {
            // Don't immediately call ScreenManager.RemoveScreen(this), but rather, offload it to
            // after the transition completes. We'll store the alsoUnloadContent variable until then.
            isNowExiting = true;
            unloadContentAfterExit = alsoUnloadContent;
        }

        public virtual void UI() { }

    }
}
