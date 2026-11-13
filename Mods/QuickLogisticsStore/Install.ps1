param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$GameDirectory,
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).ProviderPath
$gameExe = Join-Path $gameRoot 'DSPGAME.exe'
$bepDll = Join-Path $gameRoot 'BepInEx\core\BepInEx.dll'
if (-not (Test-Path -LiteralPath $gameExe -PathType Leaf)) {
    throw 'GameDirectory must be the game installation folder containing DSPGAME.exe.'
}
if (-not (Test-Path -LiteralPath $bepDll -PathType Leaf)) {
    throw 'Install BepInEx 5.4.17 or a newer 5.x version in this game folder first.'
}
$bepVersion = [Reflection.AssemblyName]::GetAssemblyName($bepDll).Version
if ($bepVersion.Major -ne 5 -or $bepVersion -lt [version]'5.4.17.0') {
    throw 'This mod requires BepInEx 5.4.17 or a newer 5.x version.'
}

$sourceDll = Join-Path $PSScriptRoot 'dist\QuickLogisticsStore.dll'
$hashFile = Join-Path $PSScriptRoot 'dist\QuickLogisticsStore.dll.sha256'
$expectedHash = [IO.File]::ReadAllText($hashFile).Trim()
if ($expectedHash -notmatch '^[0-9A-Fa-f]{64}$') { throw 'Invalid DLL checksum file.' }
$sourceHash = (Get-FileHash -LiteralPath $sourceDll -Algorithm SHA256).Hash
if ($sourceHash -ne $expectedHash) { throw 'DLL checksum mismatch. Pull the complete mod package again.' }

if ($ValidateOnly) {
    Write-Output 'Validation passed. No files were changed.'
    return
}
if (Get-Process -Name DSPGAME -ErrorAction SilentlyContinue) {
    throw 'Save and fully exit Dyson Sphere Program before installing this mod.'
}

$installRoot = Join-Path $gameRoot 'BepInEx\plugins\SanCun-QuickLogisticsStore'
$targetDll = Join-Path $installRoot 'QuickLogisticsStore.dll'
if (Test-Path -LiteralPath $targetDll) {
    if ((Get-FileHash -LiteralPath $targetDll -Algorithm SHA256).Hash -eq $sourceHash) {
        Write-Output 'Quick Logistics Store is already up to date.'
        return
    }
    $backupName = 'QuickLogisticsStore.dll.' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8) + '.bak'
    Copy-Item -LiteralPath $targetDll -Destination (Join-Path $installRoot $backupName)
}
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
Copy-Item -LiteralPath $sourceDll -Destination $targetDll -Force
if ((Get-FileHash -LiteralPath $targetDll -Algorithm SHA256).Hash -ne $sourceHash) {
    throw 'Installed DLL checksum mismatch.'
}
Write-Output ('Installed Quick Logistics Store: ' + $targetDll)
Write-Output 'Restart the game, open inventory, and use the logistics storage button or Left Ctrl + Left Shift + L.'
