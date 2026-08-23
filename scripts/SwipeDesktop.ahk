#Requires AutoHotkey v2.0
#SingleInstance Force

dll := A_ScriptDir "\VirtualDesktopAccessor.dll"
helper := A_ScriptDir "\DeskSwipe.exe"

global toastGui := 0

A_IconTip := "DeskSwipe"

iconPath := A_ScriptDir "\DeskSwipe.ico"

if FileExist(iconPath)
    TraySetIcon(iconPath)

SetupDeskSwipeTray()
HandleStartupLaunch()

hVDA := DllCall(
    "LoadLibrary",
    "Str", dll,
    "Ptr"
)

GetCurrentDesktopNumberProc := DllCall(
    "GetProcAddress",
    "Ptr", hVDA,
    "AStr", "GetCurrentDesktopNumber",
    "Ptr"
)

GetDesktopCountProc := DllCall(
    "GetProcAddress",
    "Ptr", hVDA,
    "AStr", "GetDesktopCount",
    "Ptr"
)

GetDesktopNameProc := DllCall(
    "GetProcAddress",
    "Ptr", hVDA,
    "AStr", "GetDesktopName",
    "Ptr"
)

if !ProcessExist("DeskSwipe.exe") {
    Run '"' helper '" --resident --dll "' dll '" --duration 220 --capture-delay 12',
        A_ScriptDir,
        "Hide"

    Sleep 350
}

GetSettingsPath() {
    return A_AppData "\DeskSwipe\settings.json"
}

GetJsonString(json, key, defaultValue) {
    pattern := '"' key '"\s*:\s*"([^"]*)"'

    if RegExMatch(json, pattern, &match)
        return match[1]

    return defaultValue
}

GetJsonBool(json, key, defaultValue) {
    pattern := 'i)"' key '"\s*:\s*(true|false)'

    if RegExMatch(json, pattern, &match)
        return StrLower(match[1]) = "true"

    return defaultValue
}

LoadDeskSwipeSettings() {
    settings := Map(
        "gestureScanCode", "10F",
        "swipeDirection", "natural",
        "edgeBehavior", "bounce",
        "showEdgeMessage", true,
        "messageStyle", "startEnd",
        "messageDuration", "normal",
        "openSettingsOnStartup", false
    )

    path := GetSettingsPath()

    if !FileExist(path)
        return settings

    try {
        json := FileRead(path, "UTF-8")

        settings["gestureScanCode"] :=
            GetJsonString(
                json,
                "gestureScanCode",
                "10F"
            )

        settings["swipeDirection"] :=
            GetJsonString(
                json,
                "swipeDirection",
                "natural"
            )

        settings["edgeBehavior"] :=
            GetJsonString(
                json,
                "edgeBehavior",
                "bounce"
            )

        settings["showEdgeMessage"] :=
            GetJsonBool(
                json,
                "showEdgeMessage",
                true
            )

        settings["messageStyle"] :=
            GetJsonString(
                json,
                "messageStyle",
                "startEnd"
            )

        settings["messageDuration"] :=
            GetJsonString(
                json,
                "messageDuration",
                "normal"
            )

        settings["openSettingsOnStartup"] :=
            GetJsonBool(
                json,
                "openSettingsOnStartup",
                false
            )
    }

    return settings
}

GetDesktopNameSafe(desktopNumber) {
    global GetDesktopNameProc

    if !GetDesktopNameProc
        return "Desktop " (desktopNumber + 1)

    buf := Buffer(1024, 0)

    result := DllCall(
        GetDesktopNameProc,
        "Int", desktopNumber,
        "Ptr", buf.Ptr,
        "UPtr", buf.Size,
        "Int"
    )

    if result < 0
        return "Desktop " (desktopNumber + 1)

    name := StrGet(buf, "UTF-8")

    if name = ""
        return "Desktop " (desktopNumber + 1)

    return name
}

HideDesktopToast() {
    global toastGui

    if !toastGui
        return

    try {
        hwnd := toastGui.Hwnd
    } catch {
        toastGui := 0
        return
    }

    try {
        Loop 6 {
            if !WinExist("ahk_id " hwnd)
                break

            alpha :=
                220 -
                Round(
                    220 *
                    (A_Index / 6)
                )

            if alpha < 0
                alpha := 0

            try WinSetTransparent(
                alpha,
                "ahk_id " hwnd
            )

            Sleep 12
        }
    }

    try {
        if WinExist("ahk_id " hwnd)
            toastGui.Destroy()
    }

    toastGui := 0
}

ShowCurrentDesktopToast() {
    global toastGui
    global GetCurrentDesktopNumberProc
    global GetDesktopCountProc

    settings := LoadDeskSwipeSettings()

    if !settings["showEdgeMessage"]
        return

    HideDesktopToast()

    current := DllCall(
        GetCurrentDesktopNumberProc,
        "Int"
    )

    count := DllCall(
        GetDesktopCountProc,
        "Int"
    )

    style := settings["messageStyle"]

    if style = "desktopName" {

        toastText :=
            GetDesktopNameSafe(current)

    } else if style = "desktopNumber" {

        toastText :=
            "Desktop " (current + 1)

    } else {

        if current = 0
            toastText := "Start of desktops"
        else if current = count - 1
            toastText := "End of desktops"
        else
            toastText := "Desktop " (current + 1)
    }

    try {
        isLight := RegRead(
            "HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme",
            1
        )
    } catch {
        isLight := 1
    }

    if isLight {
        bg := "F5F5F5"
        fg := "181818"
        transparency := 238
    } else {
        bg := "242424"
        fg := "F4F4F4"
        transparency := 232
    }

    toastGui := Gui(
        "+AlwaysOnTop -Caption +ToolWindow +E0x20"
    )

    toastGui.BackColor := bg
    toastGui.MarginX := 14
    toastGui.MarginY := 10

    toastGui.SetFont(
        "s11 c" fg,
        "Segoe UI Variable Text"
    )

    label := toastGui.AddText(
        "x14 y10 w216 h28 Center +0x200",
        toastText
    )

    label.SetFont(
        "s11 c" fg,
        "Segoe UI Variable Text"
    )

    toastGui.Show(
        "NoActivate w244 h48 Hide"
    )

    hwnd := toastGui.Hwnd

    DetectHiddenWindows true

    WinSetRegion(
        "0-0 W244 H48 R24-24",
        "ahk_id " hwnd
    )

    WinSetTransparent(
        transparency,
        "ahk_id " hwnd
    )

    x := (A_ScreenWidth - 244) // 2
    y := A_ScreenHeight - 48 - 72

    toastGui.Move(
        x,
        y,
        244,
        48
    )

    toastGui.Show(
        "NoActivate"
    )

    DetectHiddenWindows false

    Loop 6 {
        if !WinExist("ahk_id " hwnd)
            break

        alpha :=
            Round(
                transparency *
                (A_Index / 6)
            )

        try WinSetTransparent(
            alpha,
            "ahk_id " hwnd
        )

        Sleep 14
    }

    durationSetting :=
        settings["messageDuration"]

    duration :=
        durationSetting = "short"
            ? 650
            : durationSetting = "long"
                ? 1700
                : 1100

    SetTimer(
        HideDesktopToast,
        -duration
    )
}

SendBounce(direction) {
    settings := LoadDeskSwipeSettings()

    if settings["edgeBehavior"] != "bounce"
        return

    hwnd := DllCall(
        "FindWindow",
        "Ptr", 0,
        "Str", "DeskSwipeIPC",
        "Ptr"
    )

    if !hwnd
        return

    ; 3 = bounce left
    ; 4 = bounce right
    command :=
        direction = "left"
            ? 3
            : 4

    DllCall(
        "PostMessage",
        "Ptr", hwnd,
        "UInt", 0x802A,
        "Ptr", command,
        "Ptr", 0
    )

    if settings["showEdgeMessage"] {
        SetTimer(
            ShowCurrentDesktopToast,
            -300
        )
    }
}

SwitchDesktop(direction) {
    global GetCurrentDesktopNumberProc
    global GetDesktopCountProc

    settings := LoadDeskSwipeSettings()

    ; Reverse the perceived swipe direction if requested.
    if settings["swipeDirection"] = "reversed" {
        direction :=
            direction = "left"
                ? "right"
                : "left"
    }

    current := DllCall(
        GetCurrentDesktopNumberProc,
        "Int"
    )

    count := DllCall(
        GetDesktopCountProc,
        "Int"
    )

    if direction = "left" {

        if current < count - 1 {

            ; Native Windows desktop transition
            Send "#^{Right}"

        } else {

            ; Final desktop
            SendBounce("left")
        }

    } else {

        if current > 0 {

            ; Native Windows desktop transition
            Send "#^{Left}"

        } else {

            ; First desktop
            SendBounce("right")
        }
    }
}

; ============================================================
; Gesture hotkey registration
; ============================================================

NormalizeGestureScanCode(value) {
    cleaned :=
        StrUpper(
            RegExReplace(
                Trim(value),
                "[^0-9a-fA-F]"
            )
        )

    if cleaned = ""
        cleaned := "10F"

    return "SC" SubStr(cleaned, 1, 6)
}

ApplyGestureHotkeys(scanCode) {
    global LeftGestureFunc
    global RightGestureFunc

    LeftGestureFunc :=
        (*) => SwitchDesktop("left")

    RightGestureFunc :=
        (*) => SwitchDesktop("right")

    Hotkey(
        "<!+" scanCode,
        LeftGestureFunc,
        "On"
    )

    Hotkey(
        "<!" scanCode,
        RightGestureFunc,
        "On"
    )
}

DisableGestureHotkeys(scanCode) {
    global LeftGestureFunc
    global RightGestureFunc

    if LeftGestureFunc != "" {
        try Hotkey(
            "<!+" scanCode,
            LeftGestureFunc,
            "Off"
        )
    }

    if RightGestureFunc != "" {
        try Hotkey(
            "<!" scanCode,
            RightGestureFunc,
            "Off"
        )
    }
}

SyncGestureHotkeys() {
    global RegisteredGestureKey
    global LeftGestureFunc
    global RightGestureFunc

    settings := LoadDeskSwipeSettings()

    scanCode :=
        NormalizeGestureScanCode(
            settings["gestureScanCode"]
        )

    if scanCode = RegisteredGestureKey
        return

    try {
        DisableGestureHotkeys(RegisteredGestureKey)

        ApplyGestureHotkeys(scanCode)

        RegisteredGestureKey := scanCode
    } catch {
        ; Fall back to the default ALPS signal
        ; when the configured code is unusable.
        try {
            DisableGestureHotkeys(RegisteredGestureKey)

            ApplyGestureHotkeys("SC10F")

            RegisteredGestureKey := "SC10F"
        }
    }
}

RegisteredGestureKey := ""
LeftGestureFunc := ""
RightGestureFunc := ""

SyncGestureHotkeys()

SetTimer(SyncGestureHotkeys, 1500)



; ============================================================
; DeskSwipe tray menu
; ============================================================

GetStartupShortcutPath() {
    return A_Startup "\DeskSwipe.lnk"
}

IsStartWithWindowsEnabled() {
    return FileExist(GetStartupShortcutPath()) != ""
}

SetStartWithWindows(enabled) {
    shortcutPath := GetStartupShortcutPath()

    if !enabled {
        try {
            if FileExist(shortcutPath)
                FileDelete(shortcutPath)
        }

        UpdateStartWithWindowsJson(false)
        return
    }

    try {
        shell := ComObject("WScript.Shell")

        shortcut :=
            shell.CreateShortcut(
                shortcutPath
            )

        shortcut.TargetPath :=
            A_ScriptFullPath

        shortcut.WorkingDirectory :=
            A_ScriptDir

        shortcut.Arguments :=
            "--startup"

        shortcut.Description :=
            "DeskSwipe"

        shortcut.IconLocation :=
            A_ScriptDir "\DeskSwipe.ico"

        shortcut.Save()

        UpdateStartWithWindowsJson(true)
    }
}

UpdateStartWithWindowsJson(enabled) {
    path := GetSettingsPath()

    try {
        if FileExist(path) {
            json := FileRead(path, "UTF-8")
        } else {
            DirCreate(A_AppData "\DeskSwipe")

            json :=
                '{'
                . '"gestureScanCode":"10F",'
                . '"swipeDirection":"natural",'
                . '"edgeBehavior":"bounce",'
                . '"bounceStrength":"balanced",'
                . '"showEdgeMessage":true,'
                . '"messageStyle":"startEnd",'
                . '"messageDuration":"normal",'
                . '"startWithWindows":'
                . (enabled ? "true" : "false")
                . ','
                . '"theme":"system"'
                . '}'

            FileAppend(
                json,
                path,
                "UTF-8"
            )

            return
        }

        replacement :=
            '"startWithWindows": '
            . (enabled ? "true" : "false")

        if RegExMatch(
            json,
            'i)"startWithWindows"\s*:\s*(true|false)'
        ) {
            json := RegExReplace(
                json,
                'i)"startWithWindows"\s*:\s*(true|false)',
                replacement
            )
        } else {
            json := RegExReplace(
                json,
                '\}\s*$',
                ','
                . replacement
                . '}'
            )
        }

        file := FileOpen(
            path,
            "w",
            "UTF-8"
        )

        file.Write(json)
        file.Close()
    }
}

OpenDeskSwipeSettings(*) {
    settingsExe :=
        A_ScriptDir
        . "\Settings\DeskSwipe.Settings.exe"

    if !FileExist(settingsExe) {
        MsgBox(
            "DeskSwipe Settings could not be found.`n`n"
            . settingsExe,
            "DeskSwipe",
            "Icon!"
        )

        return
    }

    Run(
        '"' settingsExe '"',
        A_ScriptDir "\Settings"
    )
}

ToggleStartWithWindows(*) {
    enabled :=
        IsStartWithWindowsEnabled()

    SetStartWithWindows(
        !enabled
    )

    SyncTrayMenu()
}

ShowDeskSwipeAbout(*) {
    MsgBox(
        "DeskSwipe`n"
        . "Version 0.2.0`n`n"
        . "Three-finger desktop switching for Windows.",
        "About DeskSwipe",
        "Iconi"
    )
}

QuitDeskSwipe(*) {
    try ProcessClose("DeskSwipe.exe")

    ExitApp()
}

SyncTrayMenu(*) {
    try {
        if IsStartWithWindowsEnabled()
            A_TrayMenu.Check("Start with Windows")
        else
            A_TrayMenu.Uncheck("Start with Windows")
    }
}


HandleStartupLaunch() {
    launchedAtStartup := false

    for arg in A_Args {
        if arg = "--startup" {
            launchedAtStartup := true
            break
        }
    }

    if !launchedAtStartup
        return

    settings := LoadDeskSwipeSettings()

    if settings["openSettingsOnStartup"] {
        SetTimer(
            OpenDeskSwipeSettings,
            -600
        )
    }
}


SetupDeskSwipeTray() {
    A_TrayMenu.Delete()

    A_TrayMenu.Add(
        "Settings",
        OpenDeskSwipeSettings
    )

    A_TrayMenu.Add()

    A_TrayMenu.Add(
        "Start with Windows",
        ToggleStartWithWindows
    )

    A_TrayMenu.Add()

    A_TrayMenu.Add(
        "About",
        ShowDeskSwipeAbout
    )

    A_TrayMenu.Add(
        "Quit",
        QuitDeskSwipe
    )

    A_TrayMenu.Default :=
        "Settings"

    A_TrayMenu.ClickCount :=
        2

    SyncTrayMenu()

    SetTimer(
        SyncTrayMenu,
        1000
    )
}
