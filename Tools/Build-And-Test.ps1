param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe',
    [switch]$AllowExistingPlayer
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$unityData = Join-Path (Split-Path -Parent $UnityEditor) 'Data'
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Logs') | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $project 'Builds\Validation') | Out-Null
$summaryPath = Join-Path $project 'Builds\Validation\verification.json'
$summary = [ordered]@{ verifiedAt = [DateTime]::UtcNow.ToString('o'); compilePassed = $false; buildPassed = $false; playerTestsPassed = $false; mode = 'fresh-unity-build'; limitation = $null }

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
    $buildLog = Join-Path $project 'Logs\build-latest.log'
    Set-Content -LiteralPath $buildLog -Value ''
    $builder = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'RiskyDelivery.Editor.ProjectTools.BuildWindows', '-logFile', ('"' + $buildLog + '"')) -WindowStyle Hidden -PassThru
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
            throw 'Unity build failed. See Logs/build-latest.log.'
        }
    }
    else {
        $summary.buildPassed = $true
        $smokeLog = Join-Path $project 'Logs\smoke-windows.log'
        Set-Content -LiteralPath $smokeLog -Value ''
        $player = Start-Process -FilePath (Join-Path $project 'Builds\Windows\RiskyDelivery.exe') -ArgumentList @('-batchmode', '-nographics', '-risky-smoke-test', '-logFile', ('"' + $smokeLog + '"')) -WindowStyle Hidden -PassThru
        Wait-ForChild $player 180
        if ($player.ExitCode -ne 0 -or !(Select-String -LiteralPath $smokeLog -Pattern 'RISKY_DELIVERY_SMOKE_OK' -Quiet)) {
            Get-Content -LiteralPath $smokeLog -Tail 30
            throw 'Windows player integration tests failed.'
        }
        $summary.playerTestsPassed = $true
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
    Write-Output 'SCRIPT_TESTS_PASSED_BUILD_BLOCKED: See Builds/Validation/verification.json. This is not a successful fresh build.'
    exit 2
}
Write-Output 'BUILD_AND_TEST_OK: fresh Unity Windows build and player integration tests passed.'
