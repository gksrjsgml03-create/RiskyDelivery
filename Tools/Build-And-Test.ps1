param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe',
    [switch]$AllowExistingPlayer,
    [switch]$Release,
    [switch]$CapturePreview
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$unityData = Join-Path (Split-Path -Parent $UnityEditor) 'Data'
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Builds\Validation') | Out-Null
$summaryFile = if ($Release) { 'Builds\Validation\verification-release.json' } else { 'Builds\Validation\verification.json' }
$summaryPath = Join-Path $project $summaryFile
$buildKind = if ($Release) { 'release' } else { 'development' }
$playerDirectory = if ($Release) { 'Builds\Release' } else { 'Builds\Windows' }
$playerExecutable = Join-Path (Join-Path $project $playerDirectory) 'RiskyDelivery.exe'
$summary = [ordered]@{ verifiedAt = [DateTime]::UtcNow.ToString('o'); compilePassed = $false; buildPassed = $false; playerTestsPassed = $false; previewPassed = $null; mode = ('fresh-unity-' + $buildKind + '-build'); limitation = $null }
$summary | ConvertTo-Json | Set-Content -LiteralPath $summaryPath -Encoding UTF8

function Wait-ForChild($child, [int]$timeoutSeconds) {
    $deadline = [DateTime]::UtcNow.AddSeconds($timeoutSeconds)
    while (!$child.WaitForExit(1000)) {
        if ([DateTime]::UtcNow -gt $deadline) {
            Stop-Process -Id $child.Id
            throw ('Process timed out: ' + $child.ProcessName)
        }
    }
    $child.Refresh()
}

try {
    & (Join-Path $PSScriptRoot 'Check-Compile.ps1') -UnityData $unityData
    $summary.compilePassed = $true
    $buildLogName = if ($Release) { 'Logs\build-release.log' } else { 'Logs\build-latest.log' }
    $buildLog = Join-Path $project $buildLogName
    Set-Content -LiteralPath $buildLog -Value ''
    $buildMethod = if ($Release) { 'RiskyDelivery.Editor.ProjectTools.BuildReleaseWindows' } else { 'RiskyDelivery.Editor.ProjectTools.BuildWindows' }
    $builder = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $project + '"'), '-executeMethod', $buildMethod, '-logFile', ('"' + $buildLog + '"')) -WindowStyle Hidden -PassThru
    Wait-ForChild $builder 600
    $buildText = Get-Content -LiteralPath $buildLog -Raw
    if ($builder.ExitCode -ne 0 -or $buildText -notmatch 'RISKY_DELIVERY_BUILD_OK') {
        if ($AllowExistingPlayer -and $buildText -match 'No valid Unity Editor license found') {
            $summary.mode = 'existing-player-script-validation'
            $summary.limitation = 'Unity license is inactive; fresh build and asset import are not validated.'
            Write-Warning $summary.limitation
            & (Join-Path $PSScriptRoot 'Test-PlayerScripts.ps1') -UnityData $unityData
            $summary.playerTestsPassed = $true
        }
        else {
            Get-Content -LiteralPath $buildLog -Tail 30
            throw ('Unity build failed. See ' + $buildLogName)
        }
    }
    else {
        $summary.buildPassed = $true
        $smokeLogName = if ($Release) { 'Logs\smoke-release.log' } else { 'Logs\smoke-windows.log' }
        $smokeLog = Join-Path $project $smokeLogName
        Set-Content -LiteralPath $smokeLog -Value ''
        $player = Start-Process -FilePath $playerExecutable -ArgumentList @('-batchmode', '-nographics', '-risky-smoke-test', '-logFile', ('"' + $smokeLog + '"')) -WindowStyle Hidden -PassThru
        Wait-ForChild $player 180
        if ($player.ExitCode -ne 0 -or !(Select-String -LiteralPath $smokeLog -Pattern 'RISKY_DELIVERY_SMOKE_OK' -Quiet)) {
            Get-Content -LiteralPath $smokeLog -Tail 30
            throw 'Windows player integration tests failed.'
        }
        $summary.playerTestsPassed = $true
        if ($CapturePreview) {
            $previewFolder = if ($Release) { 'Builds\Validation\ReleasePreview' } else { 'Builds\Validation\LatestPreview' }
            $previewLogName = if ($Release) { 'Logs\preview-release.log' } else { 'Logs\preview-latest.log' }
            $previewLog = Join-Path $project $previewLogName
            Set-Content -LiteralPath $previewLog -Value ''
            # ScreenCapture needs a visible graphics window; this opt-in check is for an interactive desktop.
            $preview = Start-Process -FilePath $playerExecutable -ArgumentList @('-risky-preview', ('"' + (Join-Path $project $previewFolder) + '"'), '-logFile', ('"' + $previewLog + '"')) -WindowStyle Normal -PassThru
            Wait-ForChild $preview 90
            if ($preview.ExitCode -ne 0 -or !(Select-String -LiteralPath $previewLog -Pattern 'RISKY_DELIVERY_PREVIEW_OK' -Quiet)) {
                throw ('Graphics preview failed. See ' + $previewLogName)
            }
            $summary.previewPassed = $true
        }
    }
}
catch {
    $summary.limitation = $_.Exception.Message
    throw
}
finally {
    $summary | ConvertTo-Json | Set-Content -LiteralPath $summaryPath -Encoding UTF8
}

if (!$summary.buildPassed) {
    Write-Output ('SCRIPT_TESTS_PASSED_BUILD_BLOCKED: See ' + $summaryFile + '. This is not a successful fresh build.')
    exit 2
}
Write-Output 'BUILD_AND_TEST_OK: fresh Unity Windows build and player integration tests passed.'
