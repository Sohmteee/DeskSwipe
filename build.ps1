$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$icon = Join-Path $root "assets\DeskSwipe.ico"
$project = Join-Path $root "src\DeskSwipe\DeskSwipe.csproj"
$script = Join-Path $root "scripts\SwipeDesktop.ahk"
$vda = Join-Path $root "lib\VirtualDesktopAccessor.dll"

$publish = Join-Path $root "release\DeskSwipe"
$settingsProject = Join-Path $root "src\DeskSwipe.Settings\DeskSwipe.Settings.csproj"

$settingsBuild = Join-Path `
    $root `
    "src\DeskSwipe.Settings\bin\Release\net8.0-windows10.0.19041.0\win-x64"

$settingsRelease = Join-Path $publish "Settings"
$gestureExe = Join-Path $publish "DeskSwipeGestures.exe"

$ahkCompiler = "C:\Program Files\AutoHotkey\Compiler\Ahk2Exe.exe"
$ahkBase = "C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe"
$tempGestureExe = Join-Path ([IO.Path]::GetTempPath()) ("DeskSwipeGestures-" + [guid]::NewGuid().ToString("N") + ".exe")

if (-not (Test-Path $ahkCompiler)) {
    throw "Ahk2Exe.exe was not found."
}

if (-not (Test-Path $ahkBase)) {
    throw "AutoHotkey v2 runtime was not found."
}

if (Test-Path $publish) {
    Remove-Item $publish -Recurse -Force
}

Remove-Item $tempGestureExe -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Force -Path $publish |
    Out-Null

Write-Host ""
Write-Host "Publishing DeskSwipe..."
Write-Host ""

dotnet publish `
    $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publish

Write-Host ""
Write-Host ""
Write-Host "Building DeskSwipe.Settings..."

dotnet build `
    $settingsProject `
    -c Release `
    -r win-x64

if ($LASTEXITCODE -ne 0) {
    throw "DeskSwipe.Settings build failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $settingsBuild)) {
    throw "DeskSwipe.Settings output was not found: $settingsBuild"
}

Remove-Item `
    $settingsRelease `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

Copy-Item `
    $settingsBuild `
    $settingsRelease `
    -Recurse `
    -Force

Write-Host ""Write-Host "Compiling DeskSwipeGestures..."
Write-Host ""

& $ahkCompiler `
    /in $script `
    /out $tempGestureExe `
    /base $ahkBase `
    /icon $icon `
    /compress 0

$created = $false

for ($i = 0; $i -lt 50; $i++) {
    if (Test-Path $tempGestureExe) {
        $created = $true
        break
    }

    Start-Sleep -Milliseconds 200
}

if (-not $created) {
    throw "Ahk2Exe failed to create DeskSwipeGestures.exe."
}

Copy-Item `
    $tempGestureExe `
    $gestureExe `
    -Force

Copy-Item `
    $vda `
    (Join-Path $publish "VirtualDesktopAccessor.dll") `
    -Force

Remove-Item `
    $tempGestureExe `
    -Force `
    -ErrorAction SilentlyContinue

Write-Host ""
Copy-Item `
    $icon `
    (Join-Path $publish "DeskSwipe.ico") `
    -Force
Write-Host "DeskSwipe portable build complete:"
Write-Host $publish
Write-Host ""

Get-ChildItem $publish |
    Select-Object Name, Length







