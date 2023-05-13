using System;
using System.IO;
using System.Text.Json;

namespace HammeredGame.Core
{
    /// <summary>
    /// Resolution represents possible values for the game resolution. The options screen would use
    /// these values for showing which values are selectable.
    /// </summary>
    /// <param name="Width"></param>
    /// <param name="Height"></param>
    public record Resolution(int Width, int Height)
    {
        public static readonly Resolution Res1280720 = new(1280, 720);
        public static readonly Resolution Res1360768 = new(1360, 768);
        public static readonly Resolution Res1366768 = new(1366, 768);
        public static readonly Resolution Res1600900 = new(1600, 900);
        public static readonly Resolution Res19201080 = new(1920, 1080);
        public static readonly Resolution Res20481152 = new(2048, 1152);
        public static readonly Resolution Res25601440 = new(2560, 1440);
        public static readonly Resolution Res25761450 = new(2576, 1450);

        public static readonly Resolution[] AcceptedList = new Resolution[]
        {
            Res1280720, Res1360768, Res1366768, Res1600900, Res19201080, Res20481152, Res25601440, Res25761450
        };

        public override string ToString()
        {
            return Width + "x" + Height;
        }
    }

    /// <summary>
    /// UserSettings represents settings chosen by the user for the game. Each property has a
    /// default value, but users can overwrite them in the options screen, which will call Save on it.
    /// </summary>
    public record UserSettings
    {
        /// <summary>
        /// The file path of this settings instance.
        /// </summary>
        private string SaveFilePath { get; set; }
        public float MediaVolume { get; set; } = 0.4f;
        public float SfxVolume { get; set; } = 0.8f;
        public Resolution Resolution { get; set; } = Resolution.Res19201080;
        public bool FullScreen { get; set; } = true;
        public bool Borderless { get; set; } = true;

        // TODO: LastSaveScene is not a setting value but a game context or save state. Perhaps it
        // should be a different data structure, but it's just this single string so for now it's
        // placed within settings.
        public string LastSaveScene { get; set; } = null;

        /// <summary>
        /// Save user settings to the file it was loaded from, overwriting any existing contents.
        /// </summary>
        public void Save()
        {
            string output = JsonSerializer.Serialize<UserSettings>(this, new JsonSerializerOptions()
            {
                 WriteIndented = true,
            });

            File.WriteAllText(this.SaveFilePath, output);
        }

        /// <summary>
        /// Read user settings from a file. If any exceptions are raised, this function will
        /// suppress them and return the default settings.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static UserSettings CreateFromFile(string fileName)
        {
            UserSettings settings;
            try
            {
                string contents = File.ReadAllText(fileName);
                settings = JsonSerializer.Deserialize<UserSettings>(contents);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings file '{fileName}' couldn't be loaded, using default settings: {ex}");
                settings = new UserSettings();
            }
            settings.SaveFilePath = fileName;
            return settings;
        }
    }
}
