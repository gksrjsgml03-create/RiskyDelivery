param(
    [string]$UnityData = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Data',
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$ValidationName = 'NightPlayer'
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$original = Join-Path $project 'Builds\Windows'
$validation = Join-Path (Join-Path $project 'Builds\Validation') $ValidationName
if (!(Test-Path (Join-Path $original 'RiskyDelivery.exe'))) { throw 'An existing Windows Mono player is required.' }
# This isolated copy checks runtime script changes; it does not validate Unity import,
# scene/asset changes, stripping, or replace a fresh Unity build.
New-Item -ItemType Directory -Force -Path $validation | Out-Null
Get-ChildItem -LiteralPath $original | Copy-Item -Destination $validation -Recurse -Force
$managed = Join-Path $validation 'RiskyDelivery_Data\Managed'
$compilerArgs = @('-nologo', '-target:library', '-nostdlib+', '-langversion:9.0', '-debug:portable', ('-out:' + (Join-Path $managed 'Assembly-CSharp.dll')))
$compilerArgs += Get-ChildItem -LiteralPath $managed -Filter *.dll | Where-Object { $_.Name -ne 'Assembly-CSharp.dll' } | ForEach-Object { '-r:' + $_.FullName }
$compilerArgs += Get-ChildItem (Join-Path $project 'Assets\Scripts') -Filter *.cs | ForEach-Object { $_.FullName }
& (Join-Path $UnityData 'NetCoreRuntime\dotnet.exe') (Join-Path $UnityData 'DotNetSdkRoslyn\csc.dll') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Player script compile failed.' }
$log = Join-Path $project ('Logs\smoke-' + $ValidationName + '.log')
$playerProcess = Start-Process -FilePath (Join-Path $validation 'RiskyDelivery.exe') -ArgumentList @('-batchmode', '-nographics', '-risky-smoke-test', '-logFile', ('"' + $log + '"')) -WindowStyle Hidden -PassThru
if (!$playerProcess.WaitForExit(180000)) {
    Stop-Process -Id $playerProcess.Id
    throw 'Player smoke test timed out after 180 seconds.'
}
$playerProcess.Refresh()
if ($playerProcess.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern 'RISKY_DELIVERY_SMOKE_OK' -Quiet)) {
    Get-Content -LiteralPath $log -Tail 30
    throw 'Player smoke test failed.'
}
Select-String -LiteralPath $log -Pattern 'RISKY_CHECK_OK|RISKY_DELIVERY_SMOKE_OK'
