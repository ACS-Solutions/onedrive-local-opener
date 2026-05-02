# Manual install script (the MSI installer is the recommended route).
# Run from the repo root after building the host project.
# Requires no administrator rights — all writes go to HKCU.

param(
    [ValidateSet('user', 'machine')]
    [string]$Scope = 'user'
)

$ErrorActionPreference = 'Stop'

$publishDir = "$PSScriptRoot\publish"

Write-Host "Publishing host (self-contained, win-x64)..."
dotnet publish "$PSScriptRoot\Host.csproj" -c Release -r win-x64 --self-contained -o $publishDir

Write-Host "Registering (scope=$Scope)..."
& "$publishDir\Host.exe" register --scope $Scope

Write-Host ""
Write-Host "Done. Restart Chrome and/or Edge — the extension will install automatically once published."
Write-Host "For manual testing, load the extension unpacked from the 'extension' folder."
