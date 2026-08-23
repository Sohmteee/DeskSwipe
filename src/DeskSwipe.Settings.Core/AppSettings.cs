using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeskSwipe.Settings
{
    public sealed class AppSettings
    {
        public string GestureScanCode { get; set; } = "10F";

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

    public static class GestureScanCode
    {
        public static string Normalize(string value)
        {
            var cleaned =
                System.Text.RegularExpressions.Regex.Replace(
                    value?.Trim() ?? string.Empty,
                    "[^0-9a-fA-F]",
                    string.Empty);

            if (cleaned.Length == 0)
                cleaned = "10F";

            if (cleaned.Length > 6)
                cleaned = cleaned[..6];

            return cleaned.ToUpperInvariant();
        }
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

        public static async Task<AppSettings> LoadAsync(
            string? directory = null)
        {
            var path =
                SettingsFilePath(directory);

            try
            {
                if (!File.Exists(path))
                {
                    var defaults = new AppSettings();
                    await SaveAsync(defaults, directory);
                    return defaults;
                }

                var json =
                    await File.ReadAllTextAsync(path);

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
            AppSettings settings,
            string? directory = null)
        {
            var path =
                SettingsFilePath(directory);

            Directory.CreateDirectory(
                Path.GetDirectoryName(path)!);

            var json =
                JsonSerializer.Serialize(
                    settings,
                    JsonOptions);

            await File.WriteAllTextAsync(
                path,
                json);
        }

        private static string SettingsFilePath(
            string? directory)
        {
            if (string.IsNullOrEmpty(directory))
                return FilePath;

            return Path.Combine(
                directory,
                "settings.json");
        }
    }
}
