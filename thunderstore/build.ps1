# Builds the Thunderstore package zip from the current release DLL.
#   manifest.json + icon.png + README.md + plugins/GlassPanel.dll
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSCommandPath -Parent
$repo = Split-Path $root -Parent
$ver  = (Get-Content "$root\manifest.json" -Raw | ConvertFrom-Json).version_number
$dll  = "$repo\mod\GlassPanel\bin\Release\net472\GlassPanel.dll"
if (-not (Test-Path $dll)) { throw "GlassPanel.dll not found — run 'dotnet build -c Release' first." }

$stage = "$root\.stage"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory "$stage\plugins" -Force | Out-Null
Copy-Item "$root\manifest.json" $stage
Copy-Item "$root\icon.png"      $stage
Copy-Item "$root\README.md"     $stage
Copy-Item $dll "$stage\plugins\"

$zip = "$root\GlassPanel-$ver.zip"
Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$stage\*" -DestinationPath $zip -Force
Remove-Item $stage -Recurse -Force
Write-Output "packaged $zip"
