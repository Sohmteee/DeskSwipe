$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$project = Join-Path $root "src\DeskSwipe\DeskSwipe.csproj"
$script = Join-Path $root "scripts\SwipeDesktop.ahk"
$vda = Join-Path $root "lib\VirtualDesktopAccessor.dll"

$publish = Join-Path $root "release\DeskSwipe"
$gestureExe = Join-Path $publish "DeskSwipeGestures.exe"

$ahkCompiler = "C:\Program Files\AutoHotkey\Compiler\Ahk2Exe.exe"
$ahkBase = "C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe"
$tempGestureExe = Join-Path $env:TEMP "DeskSwipeGestures.exe"

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
Write-Host "Compiling DeskSwipeGestures..."
Write-Host ""

& $ahkCompiler `
    /in $script `
    /out $tempGestureExe `
    /base $ahkBase `
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
Write-Host "DeskSwipe portable build complete:"
Write-Host $publish
Write-Host ""

Get-ChildItem $publish |
    Select-Object Name, Length
