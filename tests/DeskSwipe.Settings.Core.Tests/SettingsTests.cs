using System;
using System.IO;
using System.Threading.Tasks;
using DeskSwipe.Settings;
using Xunit;

namespace DeskSwipe.Settings.Tests
{
    public class AppSettingsDefaultsTests
    {
        [Fact]
        public void NewSettingsHaveExpectedDefaults()
        {
            var settings = new AppSettings();

            Assert.Equal("10F", settings.GestureScanCode);
            Assert.Equal("natural", settings.SwipeDirection);
            Assert.Equal("bounce", settings.EdgeBehavior);
            Assert.Equal("balanced", settings.BounceStrength);
            Assert.True(settings.ShowEdgeMessage);
            Assert.Equal("startEnd", settings.MessageStyle);
            Assert.Equal("normal", settings.MessageDuration);
            Assert.True(settings.StartWithWindows);
            Assert.False(settings.OpenSettingsOnStartup);
            Assert.Equal("system", settings.Theme);
        }
    }

    public class GestureScanCodeTests
    {
        [Theory]
        [InlineData("10F", "10F")]
        [InlineData("10f", "10F")]
        [InlineData(" 1b ", "1B")]
        [InlineData("sc10f", "C10F")]
        [InlineData("0x1B", "01B")]
        [InlineData("!@#", "10F")]
        [InlineData("", "10F")]
        [InlineData(null, "10F")]
        [InlineData("1234567", "123456")]
        public void NormalizeProducesExpectedResults(
            string input,
            string expected)
        {
            Assert.Equal(
                expected,
                GestureScanCode.Normalize(input));
        }
    }

    public class SettingsStoreTests : IDisposable
    {
        private readonly string _directory;

        public SettingsStoreTests()
        {
            _directory =
                Path.Combine(
                    Path.GetTempPath(),
                    "DeskSwipe-tests-"
                        + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch
            {
            }
        }

        private static AppSettings SampleSettings() =>
            new()
            {
                GestureScanCode = "12E",
                SwipeDirection = "reversed",
                EdgeBehavior = "none",
                BounceStrength = "firm",
                ShowEdgeMessage = false,
                MessageStyle = "desktopName",
                MessageDuration = "long",
                StartWithWindows = false,
                OpenSettingsOnStartup = true,
                Theme = "dark"
            };

        [Fact]
        public async Task SaveThenLoadRoundTripsSettings()
        {
            var settings = SampleSettings();

            await SettingsStore.SaveAsync(
                settings,
                _directory);

            var loaded =
                await SettingsStore.LoadAsync(_directory);

            Assert.Equal(
                settings.GestureScanCode,
                loaded.GestureScanCode);

            Assert.Equal(
                settings.SwipeDirection,
                loaded.SwipeDirection);

            Assert.Equal(
                settings.EdgeBehavior,
                loaded.EdgeBehavior);

            Assert.Equal(
                settings.BounceStrength,
                loaded.BounceStrength);

            Assert.Equal(
                settings.ShowEdgeMessage,
                loaded.ShowEdgeMessage);

            Assert.Equal(
                settings.MessageStyle,
                loaded.MessageStyle);

            Assert.Equal(
                settings.MessageDuration,
                loaded.MessageDuration);

            Assert.Equal(
                settings.StartWithWindows,
                loaded.StartWithWindows);

            Assert.Equal(
                settings.OpenSettingsOnStartup,
                loaded.OpenSettingsOnStartup);

            Assert.Equal(
                settings.Theme,
                loaded.Theme);
        }

        [Fact]
        public async Task LoadMissingFileWritesDefaultsFile()
        {
            var loaded =
                await SettingsStore.LoadAsync(_directory);

            Assert.True(File.Exists(
                Path.Combine(_directory, "settings.json")));

            Assert.Equal(
                "10F",
                loaded.GestureScanCode);

            Assert.Equal(
                "natural",
                loaded.SwipeDirection);
        }

        [Fact]
        public async Task LoadMalformedJsonReturnsDefaults()
        {
            await File.WriteAllTextAsync(
                Path.Combine(_directory, "settings.json"),
                "{ not valid json ");

            var loaded =
                await SettingsStore.LoadAsync(_directory);

            Assert.Equal(
                "natural",
                loaded.SwipeDirection);

            Assert.Equal(
                "10F",
                loaded.GestureScanCode);
        }

        [Fact]
        public async Task SavedFileUsesCamelCaseKeys()
        {
            await SettingsStore.SaveAsync(
                new AppSettings(),
                _directory);

            var json =
                await File.ReadAllTextAsync(
                    Path.Combine(_directory, "settings.json"));

            Assert.Contains("\"gestureScanCode\"", json);
            Assert.Contains("\"swipeDirection\"", json);
            Assert.Contains("\"startWithWindows\"", json);
        }

        [Fact]
        public async Task PartialJsonFallsBackToDefaultsPerProperty()
        {
            await File.WriteAllTextAsync(
                Path.Combine(_directory, "settings.json"),
                "{\"swipeDirection\":\"reversed\"}");

            var loaded =
                await SettingsStore.LoadAsync(_directory);

            Assert.Equal(
                "reversed",
                loaded.SwipeDirection);

            Assert.Equal(
                "10F",
                loaded.GestureScanCode);

            Assert.Equal(
                "bounce",
                loaded.EdgeBehavior);
        }
    }
}
