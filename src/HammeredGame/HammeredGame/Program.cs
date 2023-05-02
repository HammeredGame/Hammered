namespace HammeredGame
{
    public static class Program
    {
        /// <summary>
        /// Attempt to request high-performance GPUs on devices with Nvidia graphic cards. This
        /// function will catch the exception if the load of the Nvidia DLL failed, so it's fine to
        /// attempt even on devices where we're not certain (such as devices with AMD GPUs).
        /// Reference: https://community.monogame.net/t/force-using-the-dedicated-gpu/15652
        /// Reference: https://community.monogame.net/t/directx-game-not-using-nvidia-gpu/11773
        /// Reference: https://stackoverflow.com/questions/17270429/forcing-hardware-accelerated-rendering
        /// </summary>
        private static void TryRequestHighPerformanceGPUs()
        {
            // Do nothing on non-Windows platforms
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return;
            }

            // Attempt to load the Nvidia DLLs, which causes this executable to register as
            // requiring high-performance GPUs. On non-Nvidia systems, these will just throw an
            // Exception so we catch it and do nothing.
            try
            {
                if (System.Environment.Is64BitProcess)
                    System.Runtime.InteropServices.NativeLibrary.Load("nvapi64.dll");
                else
                    System.Runtime.InteropServices.NativeLibrary.Load("nvapi.dll");
            }
            catch { }

            // On AMD machines, we need to export AmdPowerXpressRequestHighPerformance from the
            // final binary, but this is impossible to do from C# and requires patching the .exe, so
            // it not implemented until it's specifically requested from a user.
        }

        public static void Main()
        {
            // Try to indicate this program requires high performance GPUs. If this isn't done,
            // Windows laptops with integrated Nvidia GPUs (Nvidia Optimus) won't be able to fully
            // utilize the strong GPU it has.
            // TODO: make this an in-game option called "Hardware Acceleration"
            TryRequestHighPerformanceGPUs();

            using var game = new HammeredGame();
            game.Run();
        }
    }
}
