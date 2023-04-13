using ImMonoGame.Thing;
using Microsoft.Xna.Framework;

namespace HammeredGame.Core
{
    public enum ScreenState
    {
        Hidden,
        Active
    }

    public abstract class Screen : IImGui
    {
        public bool IsPartial { get; protected set; }

        public ScreenState State { get; protected set; } = ScreenState.Active;

        private bool otherScreenHasFocus;

        public bool HasFocus {
            get { return State == ScreenState.Active && !otherScreenHasFocus; }
        }

        public bool IsExiting { get; protected set; } = false;

        public GameServices GameServices { get; set; }

        public ScreenManager ScreenManager { get; set; }

        public virtual void LoadContent() { }
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

        public void ExitScreen()
        {
            IsExiting = true;
        }

        public virtual void UI() { }

    }
}
