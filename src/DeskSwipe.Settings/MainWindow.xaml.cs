using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace DeskSwipe.Settings
{
    public sealed partial class MainWindow : Window
    {
        private AppSettings _settings = new();
        private bool _loading = true;

        public MainWindow()
        {
            InitializeComponent();

            EnsureGestureRuntime();

            Activated += MainWindow_Activated;
        }

        private void EnsureGestureRuntime()
        {
            try
            {
                if (Process.GetProcessesByName("DeskSwipeGestures").Length > 0)
                    return;

                var settingsExe = Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(settingsExe))
                    return;

                var settingsDirectory = Path.GetDirectoryName(settingsExe);

                if (string.IsNullOrWhiteSpace(settingsDirectory))
                    return;

                var appDirectory = Directory.GetParent(settingsDirectory)?.FullName;

                if (string.IsNullOrWhiteSpace(appDirectory))
                    return;

                var gestureExe = Path.Combine(appDirectory, "DeskSwipeGestures.exe");

                if (!File.Exists(gestureExe))
                    return;

                Process.Start(new ProcessStartInfo
                {
                    FileName = gestureExe,
                    WorkingDirectory = appDirectory,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "DeskSwipe-runtime-error.txt"),
                    ex.ToString()
                );
            }
        }

        private async void MainWindow_Activated(
            object sender,
            WindowActivatedEventArgs args)
        {
            if (!_loading)
                return;

            await LoadSettingsAsync();

            HookSettingEvents();

            _loading = false;
        }

        private async Task LoadSettingsAsync()
        {
            _settings =
                await SettingsStore.LoadAsync();

            SwipeDirectionComboBox.SelectedIndex =
                _settings.SwipeDirection == "reversed"
                    ? 1
                    : 0;

            EdgeBehaviorComboBox.SelectedIndex =
                _settings.EdgeBehavior == "none"
                    ? 1
                    : 0;

            BounceStrengthComboBox.SelectedIndex =
                _settings.BounceStrength switch
                {
                    "soft" => 0,
                    "firm" => 2,
                    _ => 1
                };

            EdgeMessageToggle.IsOn =
                _settings.ShowEdgeMessage;

            MessageStyleComboBox.SelectedIndex =
                _settings.MessageStyle switch
                {
                    "desktopName" => 1,
                    "desktopNumber" => 2,
                    _ => 0
                };

            MessageDurationComboBox.SelectedIndex =
                _settings.MessageDuration switch
                {
                    "short" => 0,
                    "long" => 2,
                    _ => 1
                };

            StartWithWindowsToggle.IsOn =
                _settings.StartWithWindows;

            OpenSettingsOnStartupToggle.IsOn =
                _settings.OpenSettingsOnStartup;

            OpenSettingsOnStartupToggle.IsEnabled =
                _settings.StartWithWindows;

            ThemeComboBox.SelectedIndex =
                _settings.Theme switch
                {
                    "light" => 1,
                    "dark" => 2,
                    _ => 0
                };

            ApplyTheme();
        }

        private void HookSettingEvents()
        {
            SwipeDirectionComboBox.SelectionChanged +=
                SettingChanged;

            EdgeBehaviorComboBox.SelectionChanged +=
                SettingChanged;

            BounceStrengthComboBox.SelectionChanged +=
                SettingChanged;

            MessageStyleComboBox.SelectionChanged +=
                SettingChanged;

            MessageDurationComboBox.SelectionChanged +=
                SettingChanged;

            ThemeComboBox.SelectionChanged +=
                ThemeChanged;

            EdgeMessageToggle.Toggled +=
                SettingToggled;

            StartWithWindowsToggle.Toggled +=
                SettingToggled;

            OpenSettingsOnStartupToggle.Toggled +=
                SettingToggled;
        }

        private async void SettingChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            await SaveCurrentSettingsAsync();
        }

        private async void SettingToggled(
    object sender,
    RoutedEventArgs e)
{
    if (_loading)
        return;

    ReadControlsIntoSettings();

    if (ReferenceEquals(sender, StartWithWindowsToggle))
    {
        ApplyStartupSetting(
            _settings.StartWithWindows);
    }

    await SettingsStore.SaveAsync(
        _settings);
}

        private async void ThemeChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_loading)
                return;

            ReadControlsIntoSettings();

            ApplyTheme();

            await SettingsStore.SaveAsync(
                _settings);
        }

        private async Task SaveCurrentSettingsAsync()
        {
            if (_loading)
                return;

            ReadControlsIntoSettings();

            await SettingsStore.SaveAsync(
                _settings);
        }

        private void ReadControlsIntoSettings()
        {
            _settings.SwipeDirection =
                SwipeDirectionComboBox.SelectedIndex == 1
                    ? "reversed"
                    : "natural";

            _settings.EdgeBehavior =
                EdgeBehaviorComboBox.SelectedIndex == 1
                    ? "none"
                    : "bounce";

            _settings.BounceStrength =
                BounceStrengthComboBox.SelectedIndex switch
                {
                    0 => "soft",
                    2 => "firm",
                    _ => "balanced"
                };

            _settings.ShowEdgeMessage =
                EdgeMessageToggle.IsOn;

            _settings.MessageStyle =
                MessageStyleComboBox.SelectedIndex switch
                {
                    1 => "desktopName",
                    2 => "desktopNumber",
                    _ => "startEnd"
                };

            _settings.MessageDuration =
                MessageDurationComboBox.SelectedIndex switch
                {
                    0 => "short",
                    2 => "long",
                    _ => "normal"
                };

            _settings.StartWithWindows =
                StartWithWindowsToggle.IsOn;

            _settings.OpenSettingsOnStartup =
                OpenSettingsOnStartupToggle.IsOn;

            _settings.Theme =
                ThemeComboBox.SelectedIndex switch
                {
                    1 => "light",
                    2 => "dark",
                    _ => "system"
                };
        }

        private static string StartupShortcutPath
        {
            get
            {
                var startup =
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.Startup);

                return Path.Combine(
                    startup,
                    "DeskSwipe.lnk");
            }
        }

        private static void ApplyStartupSetting(bool enabled)
        {
            try
            {
                if (!enabled)
                {
                    if (File.Exists(StartupShortcutPath))
                    {
                        File.Delete(StartupShortcutPath);
                    }

                    return;
                }

                var settingsDirectory =
                    AppContext.BaseDirectory;

                var appDirectory =
                    Directory.GetParent(
                        settingsDirectory.TrimEnd(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar))
                    ?.FullName
                    ?? settingsDirectory;

                var gestureExe =
                    Path.Combine(
                        appDirectory,
                        "DeskSwipeGestures.exe");

                if (!File.Exists(gestureExe))
                {
                    return;
                }

                var shellType =
                    Type.GetTypeFromProgID(
                        "WScript.Shell");

                if (shellType is null)
                    return;

                dynamic? shell =
                    Activator.CreateInstance(shellType);

                if (shell is null)
                    return;

                dynamic shortcut =
                    shell.CreateShortcut(
                        StartupShortcutPath);

                shortcut.TargetPath =
                    gestureExe;

                shortcut.WorkingDirectory =
                    appDirectory;

                shortcut.Arguments =
                    "--startup";

                shortcut.Description =
                    "DeskSwipe";

                shortcut.IconLocation =
                    Path.Combine(
                        appDirectory,
                        "DeskSwipe.ico");

                shortcut.Save();
            }
            catch
            {
                // Startup shortcut failure should not
                // crash the settings application.
            }
        }
        private void SetWindowIcon()
        {
            try
            {
                var hwnd =
                    WindowNative.GetWindowHandle(this);

                var windowId =
                    Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);

                var appWindow =
                    AppWindow.GetFromWindowId(windowId);

                var iconPath =
                    Path.GetFullPath(
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "..",
                            "DeskSwipe.ico"));

                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
            catch
            {
            }
        }
        private void ApplyTheme()
        {
            RootGrid.RequestedTheme =
                _settings.Theme switch
                {
                    "light" => ElementTheme.Light,
                    "dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
        }
    }
}






















