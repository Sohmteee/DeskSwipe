# DeskSwipe Roadmap

This document tracks potential features, improvements, architectural changes, compatibility work, tooling, and polish for future DeskSwipe releases.

## 1. Broader Touchpad Compatibility

- Detect gesture scan codes automatically instead of assuming SC10F
- Support more ALPS models
- Support Synaptics touchpads
- Support ELAN touchpads
- Support Precision Touchpads where useful
- Add a gesture detection test screen
- Let users manually bind detected scan codes
- Add per-device profiles
- Show detected touchpad and driver information in Settings
- Warn when the current hardware is unsupported

## 2. Gesture Customization

- Three-finger left and right desktop switching
- Four-finger gesture support
- User-selectable finger count where hardware allows it
- Swipe sensitivity
- Minimum gesture threshold
- Gesture cooldown and debounce control
- Reverse direction toggle
- Per-direction actions
- Disable specific gestures
- Custom keyboard shortcut mappings
- Vertical gesture actions
- Tap gesture actions
- Gesture profiles for different apps
- Modifier-based alternate gestures

## 3. Desktop Switching Behavior

- Native Windows desktop switching
- Optional desktop wraparound
- Better wraparound implementation without taskbar or focus bugs
- Jump directly to a desktop number
- Skip selected desktops
- Remember previous desktop
- Back to previous desktop gesture
- Desktop cycling modes
- Different behavior at the first and last desktop
- Per-monitor virtual desktop behavior if Windows APIs permit it

## 4. Edge Bounce Improvements

- More bounce profiles
- Custom bounce strength slider
- Custom animation duration
- Custom bounce distance
- Springiness and damping controls
- Disable screenshot bounce in selected apps
- Reduced-motion mode
- Better multi-monitor handling
- High-refresh-rate tuning
- Different animation curves
- GPU-accelerated implementation
- Better HDR and display scaling support
- Optional subtle sound or haptic-like feedback where possible

## 5. Edge Feedback Messages

- More message styles
- Custom text
- Desktop name
- Desktop number
- Start and End
- Icons instead of text
- Position selection
- Opacity control
- Font size
- Duration slider
- Animation style
- Custom colors
- Follow Windows accent color
- Show only when desktop switching fails
- Per-monitor placement

## 6. Settings App

- Cleaner navigation sidebar
- Search settings
- Reset individual settings
- Reset all settings
- Import and export settings
- Windows-like preset
- macOS-like preset
- Minimal preset
- Fast preset
- Soft preset
- Live preview of bounce strength
- Live preview of feedback message style
- Gesture tester
- Hardware diagnostics page
- About page with version and build information
- Check for updates button
- Release notes inside the app
- Links to GitHub and issue reporting
- Better tooltips
- Accessibility improvements
- Keyboard navigation
- Localization

## 7. Tray Experience

- Pause DeskSwipe
- Resume DeskSwipe
- Quick toggle for edge bounce
- Quick toggle for messages
- Quick swipe direction toggle
- Current desktop display
- Open Settings
- Restart runtime
- Check for updates
- Start with Windows
- About
- Exit
- Better tray-state synchronization
- Different icon when paused
- Different icon when unsupported hardware is detected

## 8. Startup and Runtime Reliability

- Single-instance enforcement across all components
- Runtime watchdog
- Automatically restart the gesture runtime if it crashes
- Better communication between Settings and runtime
- Replace process-name checks with IPC
- Named-pipe communication between Settings and runtime
- Better shutdown handling
- Clean restart after settings changes
- Logging for runtime crashes
- Startup diagnostics
- Startup delay option
- Run only when compatible hardware is detected
- Better handling after sleep and hibernate
- Better handling when touchpad drivers restart

## 9. Installer

- Optional desktop shortcut checkbox
- Optional Start Menu shortcut
- Optional Start with Windows checkbox
- Clean upgrades from previous versions
- Preserve settings during updates
- Remove settings checkbox during uninstall
- Proper app version metadata
- Publisher metadata
- Installer architecture detection
- Better uninstall cleanup
- Repair option
- Silent installation support
- Portable ZIP release alongside installer
- Reduce installer size
- Code signing

## 10. Automatic Updates

- GitHub Releases update checker
- Notify when a new version exists
- Download and update from Settings
- Stable and beta update channels
- Optional automatic background updates
- Show release notes before updating
- Verify SHA-256 or signatures before installing

## 11. Logging and Diagnostics

- Optional debug logging
- Gesture event log
- Detected scan code viewer
- VirtualDesktopAccessor status
- Current desktop index
- Desktop count
- Driver and device information
- Runtime process state
- Startup status
- Last error
- Export diagnostics button
- Copy diagnostics to clipboard
- Log rotation
- Disable diagnostic logging by default

## 12. Virtual Desktop Features

- Show desktop names
- Rename desktops from DeskSwipe
- Create desktop
- Delete desktop
- Desktop overview
- Jump to desktop
- Desktop-specific rules
- Desktop-specific wallpaper integration where Windows permits it
- Remember app and desktop associations
- Exclude selected desktops from gesture navigation
- Pin apps and windows where supported

## 13. Per-App Behavior

- Disable DeskSwipe in games
- Disable DeskSwipe in fullscreen apps
- Disable DeskSwipe in Remote Desktop
- Disable DeskSwipe in selected applications
- Different sensitivity per app
- Different actions per app
- Allow gestures to pass through in selected apps
- Profiles for browsers, IDEs, games, and other applications

## 14. Gaming and Fullscreen Support

- Automatically suspend gestures in exclusive fullscreen
- Game mode
- Ignore accidental gestures while selected keys or buttons are held
- Lower runtime overhead
- Detect anti-cheat-sensitive environments and avoid unnecessary hooks
- Quick tray pause

## 15. Multi-Monitor Support

- Correct bounce capture for each monitor
- Per-monitor DPI handling
- Mixed display scaling support
- Correct animation on portrait monitors
- Better coordinate handling
- Select which monitor receives visual feedback
- Test virtual desktop behavior thoroughly across multiple monitors

## 16. Performance

- Reduce memory usage of the .NET helper
- Keep the animation helper dormant until needed
- Reduce screenshot allocation
- Cache reusable resources
- Faster first bounce
- Avoid reading the settings file on every gesture
- Keep settings in memory and reload when the file changes
- Use IPC for settings updates
- Reduce the number of processes eventually

## 17. Architecture Cleanup

- Potentially merge DeskSwipe.exe and DeskSwipeGestures.exe
- Potentially replace the AutoHotkey runtime with native C# or C++ input handling
- Central configuration service
- Shared settings model
- Named-pipe IPC
- Structured logging
- Dependency injection where useful
- Separate gesture backend abstraction
- Separate virtual desktop backend abstraction
- Hardware adapters for ALPS, Synaptics, ELAN, and other devices

## 18. Native Implementation

- Gradually replace AutoHotkey where practical
- Investigate C# input handling
- Investigate C++ input handling
- Investigate Raw Input
- Investigate low-level Windows keyboard and input APIs
- Investigate driver-specific event detection where feasible
- Simplify packaging and signing
- Improve debugging and crash diagnostics

## 19. Accessibility

- Reduced motion mode
- High contrast support
- Screen reader labels
- Full keyboard navigation
- Better large-text and UI scaling support
- Color-independent status indicators
- Allow messages without bounce animations

## 20. Localization

- English
- Igbo
- French
- Spanish
- German
- Additional community translations
- Locale-aware Settings UI
- Localized installer

## 21. README and Repository

- Settings screenshot near the top of the README
- Animated GIF or video demonstrating desktop switching
- DeskSwipe logo or repository banner
- Download Latest Release section
- Installation section
- Compatibility matrix
- Troubleshooting section
- FAQ
- Development setup instructions
- Architecture diagram
- CONTRIBUTING.md
- Issue templates
- Feature request template
- Bug report template
- CHANGELOG.md
- Choose and add a license
- SECURITY.md
- Roadmap
- Screenshots directory

## 22. GitHub Release Workflow

- GitHub Actions build workflow
- Automatic installer compilation when tags are pushed
- Automatically attach installers to releases
- Generate SHA-256 checksum files
- Generate release notes
- Build portable ZIP packages
- Verify build reproducibility
- Run compilation checks on pull requests
- Derive versions automatically from Git tags

## 23. Testing

- Unit tests for settings parsing
- Unit tests for startup behavior
- Tests for direction mappings
- Tests for edge conditions
- Tests for desktop count and index logic
- Integration tests
- Installer tests
- Upgrade tests
- Windows 10 testing
- Windows 11 testing
- Different DPI and scaling tests
- Different touchpad driver versions
- Multi-monitor tests

## 24. Security

- Code signing certificate
- Signed installer
- Signed binaries
- Verify update downloads
- Avoid unnecessary elevated privileges
- Harden settings parsing
- Prevent arbitrary executable invocation from settings
- Document the input hooks DeskSwipe uses
- Consider Windows SmartScreen and antivirus reputation for releases

## 25. Privacy

- Clearly state that DeskSwipe operates locally
- No telemetry by default
- Make crash reporting opt-in if it is ever introduced
- Add a privacy policy if network functionality is introduced

## 26. Better Onboarding

- Welcome screen
- Detect touchpad
- Ask the user to perform a swipe
- Verify DeskSwipe can detect the gesture
- Choose swipe direction
- Preview bounce behavior
- Choose startup preference
- Finish setup wizard

## 27. Compatibility Wizard

- Ask the user to swipe three fingers left
- Capture incoming scan codes and input events
- Ask the user to swipe three fingers right
- Automatically assign detected signals
- Allow manual correction
- Save a hardware profile
- Remove the hard dependency on SC10F where possible

## 28. User-Defined Actions

- Switch virtual desktop
- Open Task View
- Alt+Tab
- Volume controls
- Brightness controls
- Media controls
- Browser back and forward
- Show desktop
- Launch applications
- Send custom keyboard shortcuts
- Run a command or script

## 29. Profiles

- Default profile
- Work profile
- Gaming profile
- Laptop or battery profile
- Application-specific profiles
- Automatically switch profile based on the active application

## 30. Public Release Polish

- Gesture calibration and detection wizard
- Support beyond the current ALPS hardware
- Automatic update checking
- Pause DeskSwipe tray option
- Diagnostics and log export
- Settings screenshots and animated demo in README
- Portable ZIP release
- Automated GitHub Actions releases
- Code signing
- License, contributing guide, security policy, and issue templates

## Suggested Priority

The most important long-term improvement is making gesture input configurable instead of depending on a specific ALPS SC10F signal.

- Priority 1: Gesture detection and calibration wizard
- Priority 2: Broader ALPS, Synaptics, ELAN, and Precision Touchpad compatibility
- Priority 3: Runtime reliability, diagnostics, and crash recovery
- Priority 4: Automatic updates
- Priority 5: Pause and quick controls in the tray
- Priority 6: Improved README, screenshots, compatibility information, and troubleshooting
- Priority 7: Automated builds and releases
- Priority 8: Portable ZIP distribution
- Priority 9: Code signing
- Priority 10: Native gesture backend exploration

## Current Status

DeskSwipe v0.2.1 provides configurable three-finger virtual desktop switching, native Windows desktop transitions, edge bounce feedback, a WinUI 3 Settings application, startup controls, tray integration, desktop and Start Menu shortcuts, and a Windows installer.

The current gesture implementation was developed around a Dell ALPS touchpad that emits SC10F for three-finger horizontal flicks. Expanding beyond that hardware behavior is the primary path toward making DeskSwipe a general-purpose Windows gesture utility.

