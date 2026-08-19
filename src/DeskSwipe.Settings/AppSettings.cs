using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeskSwipe.Settings
{
    public sealed class AppSettings
    {
        public string SwipeDirection { get; set; } = "natural";
        public string EdgeBehavior { get; set; } = "bounce";
        public string BounceStrength { get; set; } = "balanced";

        public bool ShowEdgeMessage { get; set; } = true;
        public string MessageStyle { get; set; } = "startEnd";
        public string MessageDuration { get; set; } = "normal";

        public bool StartWithWindows { get; set; } = true;
        public bool OpenSettingsOnStartup { get; set; } = false;
        public string Theme { get; set; } = "system";
    }

    public static class SettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        public static string DirectoryPath =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "DeskSwipe");

        public static string FilePath =>
            Path.Combine(
                DirectoryPath,
                "settings.json");

        public static async Task<AppSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    var defaults = new AppSettings();
                    await SaveAsync(defaults);
                    return defaults;
                }

                var json =
                    await File.ReadAllTextAsync(FilePath);

                return JsonSerializer.Deserialize<AppSettings>(
                           json,
                           JsonOptions)
                       ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static async Task SaveAsync(
            AppSettings settings)
        {
            Directory.CreateDirectory(
                DirectoryPath);

            var json =
                JsonSerializer.Serialize(
                    settings,
                    JsonOptions);

            await File.WriteAllTextAsync(
                FilePath,
                json);
        }
    }
}


