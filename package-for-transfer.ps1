# Packages only the real Unity project (source + settings) into a single zip that's safe to
# copy to any external device. Deliberately excludes everything .gitignore already excludes —
# Library/, Temp/, obj/, build/, Logs/, UserSettings/, .utmp/, *.apk, and the IL2CPP
# "BackUpThisFolder_ButDontShipItWithYourGame" output dirs — since those are regenerated
# automatically by Unity and are exactly what was hitting Windows' path-length limit on copy.
# Unity will rebuild Library/ on first open of the extracted project; that's normal and expected.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$include = @("Assets", "Packages", "ProjectSettings", ".gitignore", "readme.md")
$missing = $include | Where-Object { -not (Test-Path $_) }
if ($missing) {
    throw "Expected project item(s) not found: $($missing -join ', ')"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmm"
$zipName = "SIH-CoreHex_$timestamp.zip"
$zipPath = Join-Path (Split-Path -Parent $root) $zipName

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Compress-Archive -Path $include -DestinationPath $zipPath -CompressionLevel Optimal

$sizeMB = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "Created $zipPath ($sizeMB MB)"
Write-Host "This contains the full source project (Assets, Packages, ProjectSettings) with no generated/build folders."
Write-Host "On the other device: unzip, open the folder in Unity Hub, and Unity will regenerate Library/ automatically."
