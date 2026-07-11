#requires -version 5
<#
  NO Glass Panel — standalone installer.
  Finds Nuclear Option, installs BepInEx if missing, drops in the plugin, opens the port.

  One-liner:
    irm https://raw.githubusercontent.com/patrickryan01/no-glass-panel/main/install.ps1 | iex

  Or with an explicit path:
    .\install.ps1 -GameDir "X:\SteamLibrary\steamapps\common\Nuclear Option"
#>
[CmdletBinding()]
param(
  [string]$GameDir,
  [string]$Port = "8787",
  [switch]$SkipFirewall
)
$ErrorActionPreference = 'Stop'
$BEPINEX = 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip'
$DLLURL  = 'https://github.com/patrickryan01/no-glass-panel/releases/latest/download/GlassPanel.dll'

Write-Host "== NO Glass Panel installer ==" -ForegroundColor Cyan

function Find-Game {
  if ($GameDir) { if (Test-Path $GameDir) { return $GameDir } else { throw "GameDir not found: $GameDir" } }
  $steam = (Get-ItemProperty 'HKCU:\SOFTWARE\Valve\Steam' -Name SteamPath -ErrorAction SilentlyContinue).SteamPath
  $libs = New-Object System.Collections.Generic.List[string]
  if ($steam) {
    $libs.Add($steam)
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (Test-Path $vdf) {
      Select-String -Path $vdf -Pattern '"path"\s+"(.+?)"' | ForEach-Object {
        $libs.Add(($_.Matches.Groups[1].Value -replace '\\\\','\'))
      }
    }
  }
  foreach ($l in $libs) {
    $p = Join-Path $l 'steamapps\common\Nuclear Option'
    if (Test-Path $p) { return $p }
  }
  throw "Couldn't auto-find Nuclear Option. Re-run with -GameDir 'X:\...\Nuclear Option'."
}

$game = Find-Game
Write-Host "Game:  $game"

# BepInEx
if (-not (Test-Path (Join-Path $game 'BepInEx\core'))) {
  Write-Host "Installing BepInEx 5 (Mono x64)..."
  $zip = Join-Path $env:TEMP 'glasspanel_bepinex.zip'
  Invoke-WebRequest $BEPINEX -OutFile $zip
  Expand-Archive $zip $game -Force
  Write-Host "  BepInEx installed."
} else {
  Write-Host "BepInEx already present."
}

# Plugin
$plugins = Join-Path $game 'BepInEx\plugins'
New-Item -ItemType Directory -Force $plugins | Out-Null
Write-Host "Downloading GlassPanel.dll from the latest release..."
Invoke-WebRequest $DLLURL -OutFile (Join-Path $plugins 'GlassPanel.dll')
Write-Host "  Plugin -> $plugins\GlassPanel.dll" -ForegroundColor Green

# Firewall (best-effort; needs admin)
if (-not $SkipFirewall) {
  try {
    if (-not (Get-NetFirewallRule -DisplayName 'NO Glass Panel 8787' -ErrorAction SilentlyContinue)) {
      New-NetFirewallRule -DisplayName 'NO Glass Panel 8787' -Direction Inbound -Action Allow `
        -Protocol TCP -LocalPort $Port -Profile Any -ErrorAction Stop | Out-Null
      Write-Host "Firewall opened (TCP $Port, all profiles)."
    } else { Write-Host "Firewall rule already present." }
  } catch {
    Write-Warning "Couldn't open the firewall (run PowerShell as admin for this one line):"
    Write-Host "  New-NetFirewallRule -DisplayName 'NO Glass Panel 8787' -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port -Profile Any"
  }
}

$ip = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
  Where-Object { $_.IPAddress -notmatch '^(127\.|169\.254\.)' -and $_.PrefixOrigin -eq 'Dhcp' } |
  Select-Object -First 1 -ExpandProperty IPAddress)
if (-not $ip) { $ip = '<this-pc-ip>' }

Write-Host ""
Write-Host "Done. Launch Nuclear Option, then on your second screen open:" -ForegroundColor Cyan
Write-Host "  http://${ip}:${Port}" -ForegroundColor Green
