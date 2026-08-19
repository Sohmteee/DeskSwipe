# DeskSwipe

Smooth touchpad navigation for Windows virtual desktops.

DeskSwipe adds gesture-based virtual desktop switching with native Windows desktop transitions and rubber-band feedback when you reach the first or last virtual desktop.

## Features

- Three-finger virtual desktop switching
- Native Windows desktop transition animations
- Rubber-band edge feedback
- Start/end desktop toast feedback
- Light and dark theme-aware toast UI
- Lightweight resident helper
- Windows 11 support

## Tech Stack

- C#
- .NET 8
- Windows Forms
- AutoHotkey v2
- VirtualDesktopAccessor

## Project Structure

    DeskSwipe/
    ├── src/
    │   └── DeskSwipe/
    │       ├── Program.cs
    │       └── DeskSwipe.csproj
    ├── scripts/
    │   └── SwipeDesktop.ahk
    ├── lib/
    │   └── VirtualDesktopAccessor.dll
    ├── build.ps1
    ├── README.md
    └── .gitignore

## Requirements

- Windows 11
- .NET 8 SDK
- AutoHotkey v2

## Building

Run:

    .\build.ps1

The resulting build is placed in:

    dist/

## Current Gesture Support

The current gesture implementation was developed for an older Dell ALPS touchpad.

The ALPS driver emits an extended Tab scan code (SC10F) for its three-finger horizontal flick gesture, which DeskSwipe captures using AutoHotkey.

Other touchpad hardware may require different gesture handling.

## How It Works

For normal desktop navigation, DeskSwipe sends Windows' native virtual desktop keyboard shortcuts so Windows retains its normal animation and focus behavior.

When the user swipes beyond the first or last desktop, DeskSwipe does not change desktops. Instead, its C# helper displays a short rubber-band animation to indicate that the edge has been reached.

## Third-Party Dependency

DeskSwipe uses VirtualDesktopAccessor for querying Windows virtual desktop state.

VirtualDesktopAccessor:
https://github.com/Ciantic/VirtualDesktopAccessor

