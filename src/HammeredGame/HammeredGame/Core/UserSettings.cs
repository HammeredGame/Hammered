using System;
using System.IO;
using System.Text.Json;

namespace HammeredGame.Core
{
    public record Resolution(int Width, int Height)
    {
        public static readonly Resolution Res1280720 = new(1280, 720);
        public static readonly Resolution Res1360768 = new(1360, 768);
        public static readonly Resolution Res1366768 = new(1366, 768);
        public static readonly Resolution Res1600900 = new(1600, 900);
        public static readonly Resolution Res19201080 = new(1920, 1080);

        public override string ToString()
        {
            return Width + "x" + Height;
        }
    }

    public record UserSettings
    {
        public float MasterVolume { get; set; } = 0.2f;
        public float SFXVolume { get; set; } = 0.3f;
        public Resolution Resolution { get; set; } = Resolution.Res19201080;
        public bool FullScreen { get; set; } = false;
        public bool Borderless { get; set; } = false;
        public string LastSaveScene { get; set; } = null;

        public static void SaveToFile(UserSettings settings, string fileName)
        {
            string output = JsonSerializer.Serialize<UserSettings>(settings, new JsonSerializerOptions()
            {
                 WriteIndented = true,
            });

            File.WriteAllText(fileName, output);
        }

        public static UserSettings LoadFromFile(string fileName)
        {
            try
            {
                string contents = File.ReadAllText(fileName);
                return JsonSerializer.Deserialize<UserSettings>(contents);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings file '{fileName}' couldn't be loaded, using default settings: {ex}");
                return new UserSettings();
            }
        }

    }
}
