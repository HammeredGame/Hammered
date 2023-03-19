namespace ImMonoGame.Thing
{
    /// <summary>
    /// Interface for anything that supports an ImGui UI with the UI() method.
    /// </summary>
    public interface IImGui
    {
        /// <summary>
        /// Implement this method to render ImGui UIs. You can optionally wrap the
        /// declaration in ImGui.Begin() and ImGui.End(), otherwise it will use the
        /// default window title "Debug" and default styles.
        /// <para />
        /// Call this method in the MonoGame Draw() cycle.
        /// </summary>
        void UI();
    }
}
