$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$project = Join-Path $root "src\DeskSwipe\DeskSwipe.csproj"
$dist = Join-Path $root "dist"

if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}

dotnet publish `
    $project `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o $dist

Copy-Item `
    (Join-Path $root "scripts\SwipeDesktop.ahk") `
    $dist `
    -Force

Copy-Item `
    (Join-Path $root "lib\VirtualDesktopAccessor.dll") `
    $dist `
    -Force

Write-Host ""
Write-Host "DeskSwipe built successfully:"
Write-Host $dist
