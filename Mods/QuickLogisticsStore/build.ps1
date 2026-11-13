param(
    [Parameter(Mandatory = $true)]
    [string]$GameDirectory,
    [switch]$Install
)

$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$managed = Join-Path $GameDirectory 'DSPGAME_Data\Managed'
$core = Join-Path $GameDirectory 'BepInEx\core'
$output = Join-Path $PSScriptRoot 'dist'
$references = @(
    (Join-Path $core 'BepInEx.dll'),
    (Join-Path $core '0Harmony.dll'),
    (Join-Path $managed 'Assembly-CSharp.dll'),
    (Join-Path $managed 'netstandard.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.InputLegacyModule.dll'),
    (Join-Path $managed 'UnityEngine.UI.dll'),
    (Join-Path $managed 'UnityEngine.UIModule.dll'),
    (Join-Path $managed 'UnityEngine.TextRenderingModule.dll')
)
if (-not (Test-Path -LiteralPath $compiler)) { throw '未找到 .NET Framework C# 编译器。' }
foreach ($reference in $references) {
    if (-not (Test-Path -LiteralPath $reference)) { throw "缺少本机游戏程序集：$reference" }
}
New-Item -ItemType Directory -Path $output -Force | Out-Null
$dllPath = Join-Path $output 'QuickLogisticsStore.dll'
$arguments = @('/nologo', '/target:library', '/langversion:5', '/optimize+', '/utf8output', "/out:$dllPath")
$arguments += @($references | ForEach-Object { "/reference:$_" })
$arguments += @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw '模组编译失败。' }
$dllHash = (Get-FileHash -LiteralPath $dllPath -Algorithm SHA256).Hash
[IO.File]::WriteAllText((Join-Path $output 'QuickLogisticsStore.dll.sha256'), $dllHash + "`n", [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md'),(Join-Path $PSScriptRoot 'manifest.json') -Destination $output -Force
$version = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json).version_number
$archive = Join-Path $output "SanCun-QuickLogisticsStore-$version.zip"
Compress-Archive -LiteralPath $dllPath,(Join-Path $output 'README.md'),(Join-Path $output 'manifest.json') -DestinationPath $archive -Force
Write-Output "已编译：$dllPath"
Write-Output "安装包：$archive"
if ($Install) {
    if (Get-Process -Name DSPGAME -ErrorAction SilentlyContinue) { throw '游戏正在运行；退出游戏后再执行 -Install。' }
    $installRoot = Join-Path $GameDirectory 'BepInEx\plugins\SanCun-QuickLogisticsStore'
    New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
    Copy-Item -LiteralPath $dllPath -Destination (Join-Path $installRoot 'QuickLogisticsStore.dll') -Force
    Write-Output "已安装：$installRoot"
}
