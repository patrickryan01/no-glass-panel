# Build the Glass Panel mod and drop it straight into the game's BepInEx/plugins.
#   .\build.ps1                                  # uses the default game path below
#   .\build.ps1 -GameDir "X:\SteamLibrary\steamapps\common\Nuclear Option"
param([string]$GameDir = "D:\SteamLibrary\steamapps\common\Nuclear Option")
$ErrorActionPreference = "Stop"

dotnet build "$PSScriptRoot\GlassPanel\GlassPanel.csproj" -c Release -p:NuclearOptionDir="$GameDir"

$dll  = "$PSScriptRoot\GlassPanel\bin\Release\net472\GlassPanel.dll"
$dest = "$GameDir\BepInEx\plugins"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item $dll $dest -Force
Write-Host "Installed -> $dest\GlassPanel.dll"
