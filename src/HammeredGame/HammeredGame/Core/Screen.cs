using ImMonoGame.Thing;
using Microsoft.Xna.Framework;

namespace HammeredGame.Core
{
    public enum ScreenState
    {
        Hidden, // Automatically becomes this when screen should not be drawn
        Active // Automatically becomes this when screen should be drawn
    }

    public abstract class Screen : IImGui
    {
        public bool IsPartial { get; protected set; }

        public ScreenState State { get; protected set; } = ScreenState.Active;

        public bool IsLoaded { get; protected set; }

        private bool otherScreenHasFocus;

        public bool HasFocus {
            get { return State == ScreenState.Active && !otherScreenHasFocus; }
        }

        public GameServices GameServices { get; set; }

        public ScreenManager ScreenManager { get; set; }

        public virtual void LoadContent() {
            IsLoaded = true;
        }

        public virtual void UnloadContent() { }
        public virtual void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen) {
            this.otherScreenHasFocus = otherScreenHasFocus;

            if (coveredByOtherScreen)
            {
                State = ScreenState.Hidden;
            } else
            {
                State = ScreenState.Active;
            }
        }
        public virtual void Draw(GameTime gameTime) { }

        public void ExitScreen(bool alsoUnloadContent = true)
        {
            ScreenManager.RemoveScreen(this, alsoUnloadContent);
        }

        public virtual void UI() { }

    }
}
