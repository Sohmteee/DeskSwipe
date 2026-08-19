#Requires AutoHotkey v2.0
#SingleInstance Force

dll := A_ScriptDir "\VirtualDesktopAccessor.dll"
helper := A_ScriptDir "\DeskSwipe.exe"

global toastGui := 0

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

    try {
        if !toastGui
            return

        hwnd := toastGui.Hwnd

        Loop 6 {
            alpha :=
                220 -
                Round(
                    220 *
                    (A_Index / 6)
                )

            if alpha < 0
                alpha := 0

            WinSetTransparent(
                alpha,
                "ahk_id " hwnd
            )

            Sleep 12
        }

        toastGui.Destroy()
        toastGui := 0
    }
}

ShowCurrentDesktopToast() {
    global toastGui
    global GetCurrentDesktopNumberProc
    global GetDesktopCountProc

    HideDesktopToast()

    current := DllCall(
        GetCurrentDesktopNumberProc,
        "Int"
    )

    count := DllCall(
        GetDesktopCountProc,
        "Int"
    )

    if current = 0 {
        toastText := "Start of desktops"
        icon := "?"
    } else if current = count - 1 {
        toastText := "End of desktops"
        icon := "?"
    } else {
        toastText := "Desktop " (current + 1)
        icon := "•"
    }

    ; Windows theme:
    ; AppsUseLightTheme = 0 -> dark
    ; AppsUseLightTheme = 1 -> light
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
        muted := "666666"
        border := "D7D7D7"
        transparency := 238
    } else {
        bg := "242424"
        fg := "F4F4F4"
        muted := "B7B7B7"
        border := "464646"
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

    ; Allow styling the GUI while it is still hidden
    DetectHiddenWindows true

    ; Rounded pill
    WinSetRegion(
        "0-0 W244 H48 R24-24",
        "ahk_id " hwnd
    )

    ; Subtle transparency
    WinSetTransparent(
        transparency,
        "ahk_id " hwnd
    )

    ; Position bottom center
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

    ; Fade in
    Loop 6 {
        alpha :=
            Round(
                transparency *
                (A_Index / 6)
            )

        WinSetTransparent(
            alpha,
            "ahk_id " hwnd
        )

        Sleep 14
    }

    SetTimer(
        HideDesktopToast,
        -1150
    )
}

SendBounce(direction) {
    hwnd := DllCall(
        "FindWindow",
        "Ptr", 0,
        "Str", "DeskSwipeIPC",
        "Ptr"
    )

    if !hwnd
        return

    ; 3 = left edge bounce
    ; 4 = right edge bounce
    command := direction = "left" ? 3 : 4

    DllCall(
        "PostMessage",
        "Ptr", hwnd,
        "UInt", 0x802A,
        "Ptr", command,
        "Ptr", 0
    )

    ; Bounce lasts roughly 325 ms.
    ; Show the desktop label as it settles.
    SetTimer(
        ShowCurrentDesktopToast,
        -300
    )
}

SwitchDesktop(direction) {
    global GetCurrentDesktopNumberProc
    global GetDesktopCountProc

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
            Send "#^{Right}"
        } else {
            SendBounce("left")
        }

    } else {

        if current > 0 {
            Send "#^{Left}"
        } else {
            SendBounce("right")
        }
    }
}

<!+SC10F::{
    SwitchDesktop("left")
}

<!SC10F::{
    SwitchDesktop("right")
}










