param([string]$UnityData = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$output = Join-Path $project 'Builds\Validation'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compilerArgs = @('-nologo', '-target:library', '-nostdlib+', '-langversion:9.0', ('-out:' + (Join-Path $output 'RiskyDelivery.Validation.dll')))
$compilerArgs += Get-ChildItem (Join-Path $UnityData 'NetStandard\ref\2.1.0') -Filter *.dll | ForEach-Object { '-r:' + $_.FullName }
$compilerArgs += Get-ChildItem (Join-Path $UnityData 'Managed\UnityEngine') -Filter *.dll | ForEach-Object { '-r:' + $_.FullName }
$compilerArgs += Get-ChildItem (Join-Path $project 'Assets') -Recurse -Filter *.cs | ForEach-Object { $_.FullName }
& (Join-Path $UnityData 'NetCoreRuntime\dotnet.exe') (Join-Path $UnityData 'DotNetSdkRoslyn\csc.dll') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'C# compile check failed.' }
Write-Output 'C# compile check passed. This does not replace Unity import, build, or play testing.'
