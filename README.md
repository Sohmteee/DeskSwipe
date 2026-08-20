# DeskSwipe

DeskSwipe brings configurable three-finger virtual desktop switching to compatible Windows touchpads.

It was developed primarily for older Dell/ALPS touchpads that do not expose native Windows Precision Touchpad desktop gestures.

## Features

- Three-finger horizontal virtual desktop switching
- Native Windows desktop transitions
- macOS-style edge bounce feedback
- Configurable swipe direction
- Configurable edge behavior
- Soft, Balanced, and Firm bounce strengths
- Optional edge feedback messages
- Desktop name, desktop number, or Start/End message styles
- Configurable message duration
- Windows 11-style WinUI 3 Settings app
- System, Light, and Dark themes
- Start with Windows
- Optional Settings window at sign-in
- System tray menu
- Custom DeskSwipe icon

- Windows installer
- Desktop shortcut

## Settings

### Gestures

- Swipe direction: Natural or Reversed
- Edge behavior: Bounce or Do nothing
- Bounce strength: Soft, Balanced, or Firm

### Feedback

- Edge message on/off
- Message style: Start / End, Desktop name, or Desktop number
- Message duration: Short, Normal, or Long

### System

- Start with Windows
- Open Settings at sign-in
- Theme: System, Light, or Dark

Open Settings at sign-in is disabled by default.

## Launch behavior

Opening DeskSwipe manually opens the Settings window and ensures the background gesture runtime is running.

When Windows starts DeskSwipe automatically, the startup shortcut launches `DeskSwipeGestures.exe --startup`.

The gesture runtime starts silently by default.

If Open Settings at sign-in is enabled, the Settings window also opens.

## Tray menu

DeskSwipe runs in the notification area and provides:

- Settings
- Start with Windows
- About
- Quit

Double-clicking the tray icon opens Settings.

## How it works

On the target Dell/ALPS touchpad, three-finger horizontal flicks are exposed as the extended scan code `SC10F`.

DeskSwipe captures those gestures using AutoHotkey v2.

Normal desktop changes use the native Windows shortcuts `Win + Ctrl + Left` and `Win + Ctrl + Right`.

At the first or last desktop, DeskSwipe can display a screenshot-based rubber-band bounce animation instead of attempting to wrap around.

## Components

### DeskSwipe.exe

C# / .NET helper responsible for the custom edge animation.

### DeskSwipeGestures.exe

Compiled AutoHotkey v2 runtime responsible for gesture detection, desktop switching, the tray menu, edge messages, and startup behavior.

### DeskSwipe.Settings.exe

WinUI 3 Settings application.

Settings are stored at `%APPDATA%\DeskSwipe\settings.json`.

## Technology

- C#
- .NET 8
- WinForms
- WinUI 3
- AutoHotkey v2
- VirtualDesktopAccessor
- Inno Setup

## Build requirements

- Windows 10 or Windows 11 x64
- .NET 8 SDK
- AutoHotkey v2
- Ahk2Exe
- Inno Setup 6

## Build

From the repository root, run `.\build.ps1`.

The portable build is created under `release\DeskSwipe\`.

The release directory contains:

- `DeskSwipe.exe`
- `DeskSwipeGestures.exe`
- `DeskSwipe.ico`
- `VirtualDesktopAccessor.dll`
- `Settings\DeskSwipe.Settings.exe`

The WinUI Settings application uses its normal Release build output rather than `dotnet publish`.

## Installer

Build DeskSwipe first with `.\build.ps1`.

Then compile the installer with Inno Setup using:

`& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" ".\installer\DeskSwipe.iss"`

The installer is generated at `release\DeskSwipe-Setup-0.2.1.exe`.

## Hardware compatibility

DeskSwipe was developed for a Dell ALPS touchpad with hardware ID `ACPI\DLL0532`.

The current gesture implementation depends on the ALPS driver emitting `SC10F` for three-finger horizontal flicks.

Other touchpads may require different gesture detection.

## Version

Current release: **v0.2.1**

## Credits

DeskSwipe uses VirtualDesktopAccessor for Windows virtual desktop information.

## License

A license has not yet been selected.
