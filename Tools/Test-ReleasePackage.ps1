param(
    [Parameter(Mandatory=$true)][string]$Archive,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-f0-9]{40}$')][string]$ExpectedCommit,
    [Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [switch]$RunPlayer
)
$ErrorActionPreference = 'Stop'
$archivePath = (Resolve-Path -LiteralPath $Archive).Path
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
$checksum = (Get-Content -LiteralPath ($archivePath + '.sha256') -Raw).Trim()
if ($checksum -notmatch ('(?i)^' + $hash + '  ' + [Regex]::Escape([IO.Path]::GetFileName($archivePath)) + '$')) { throw 'Release checksum mismatch.' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    [long]$total = 0
    foreach ($entry in $zip.Entries) {
        $name = $entry.FullName.Replace('\', '/')
        if ($name.StartsWith('/') -or $name -match '(^|/)\.\.(/|$)|:|[\x00-\x1f]' -or !$seen.Add($name)) { throw 'Unsafe or duplicate archive path.' }
        if ($name -match '(?i)(^|/)(\.env[^/]*|progress\.json|Player\.log|TestArtifacts|[^/]*DoNotShip[^/]*|[^/]*BackUpThisFolder[^/]*)(/|$)|\.(pdb|mdb|pem|pfx|p12|key|cs)$') { throw 'Private or development file in release archive.' }
        $total += $entry.Length
        if ($total -gt 2GB) { throw 'Release expands beyond the allowed size.' }
    }
    foreach ($required in @('RiskyDelivery.exe','UnityPlayer.dll','RiskyDelivery_Data/globalgamemanagers','RiskyDelivery_Data/Managed/Assembly-CSharp.dll','README.txt','build-info.json')) {
        if (!$seen.Contains($required)) { throw ('Missing release file: ' + $required) }
    }
    $reader = [IO.StreamReader]::new($zip.GetEntry('build-info.json').Open())
    try { $info = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
    if ($info.schema -ne 1 -or $info.sourceCommit -cne $ExpectedCommit -or $info.version -cne $Version -or $info.platform -cne 'Windows-x64' -or $info.dirty -ne $false -or $info.playerTestsPassed -ne $true) { throw 'Build provenance does not match the release tag or a clean tested source.' }
}
finally { $zip.Dispose() }
if ($RunPlayer) {
    $project = Split-Path -Parent $PSScriptRoot
    $runDirectory = Join-Path $project ('Builds/ReleaseCheck-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $runDirectory | Out-Null
    [IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $runDirectory)
    $logDirectory = Join-Path $project 'Logs'
    New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null
    $playerLog = Join-Path $logDirectory 'downloaded-player.log'
    $player = Start-Process -FilePath (Join-Path $runDirectory 'RiskyDelivery.exe') -WorkingDirectory $runDirectory -ArgumentList @('-batchmode','-nographics','-risky-smoke-test','-logFile',('"' + $playerLog + '"')) -WindowStyle Hidden -PassThru
    if (!$player.WaitForExit(240000)) { Stop-Process -Id $player.Id; throw 'Downloaded player timed out.' }
    $player.Refresh()
    if ($player.ExitCode -ne 0 -or !(Select-String -LiteralPath $playerLog -Pattern 'RISKY_DELIVERY_SMOKE_OK' -Quiet)) { throw 'Downloaded player integration tests failed. See Logs/downloaded-player.log.' }
}
Write-Output ('RELEASE_CHECK_OK: ' + $Version + ' at ' + $ExpectedCommit + ', SHA256 ' + $hash)
