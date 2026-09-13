param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.18f1\Editor\Unity.exe',
    [switch]$CapturePreview
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'Build-And-Test.ps1') -UnityEditor $UnityEditor -Release -CapturePreview:$CapturePreview
$verification = Get-Content (Join-Path $project 'Builds\Validation\verification-release.json') -Raw | ConvertFrom-Json
if (!$verification.buildPassed -or !$verification.playerTestsPassed -or ($CapturePreview -and !$verification.previewPassed)) {
    throw 'Packaging requires a fresh successful release build and tests.'
}

$versionLine = Select-String -Path (Join-Path $project 'ProjectSettings\ProjectSettings.asset') -Pattern '^\s*bundleVersion:\s*(\S+)'
$releaseVersion = $versionLine.Matches[0].Groups[1].Value
if ($releaseVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Release version must use major.minor.patch.' }
$source = [IO.Path]::GetFullPath((Join-Path $project 'Builds\Release'))
$packages = [IO.Path]::GetFullPath((Join-Path $project 'Builds\Packages'))
$stage = [IO.Path]::GetFullPath((Join-Path $packages ('Stage-' + [Guid]::NewGuid().ToString('N'))))
New-Item -ItemType Directory -Force -Path $stage | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
    $relative = $file.FullName.Substring($source.Length).TrimStart('\')
    if ($relative -match '(^|[\\/])(TestArtifacts|[^\\/]*DoNotShip[^\\/]*|[^\\/]*BackUpThisFolder[^\\/]*)([\\/]|$)' -or $file.Extension -in @('.pdb', '.mdb')) { continue }
    $destination = Join-Path $stage $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destination
}
Copy-Item -LiteralPath (Join-Path $project 'Docs\PLAYER_GUIDE.md') -Destination (Join-Path $stage 'README.txt')
$sourceCommit = (& git -C $project rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify the release source commit.' }
$dirty = @(& git -C $project diff HEAD --name-only).Count -gt 0
@{ schema = 1; version = $releaseVersion; platform = 'Windows-x64'; sourceCommit = $sourceCommit; dirty = $dirty; builtAt = [DateTime]::UtcNow.ToString('o'); playerTestsPassed = $true } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'build-info.json') -Encoding UTF8
$archive = Join-Path $packages ('RiskyDelivery-' + $releaseVersion + '-Windows.zip')
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archiveStream = [IO.File]::Open($archive, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$writer = [IO.Compression.ZipArchive]::new($archiveStream, [IO.Compression.ZipArchiveMode]::Create, $false)
try {
    foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
        $entryName = $file.FullName.Substring($stage.Length).TrimStart('\').Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($writer, $file.FullName, $entryName, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally { $writer.Dispose(); $archiveStream.Dispose() }

$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    $names = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($required in @('RiskyDelivery.exe', 'UnityPlayer.dll', 'RiskyDelivery_Data/globalgamemanagers', 'RiskyDelivery_Data/Managed/Assembly-CSharp.dll', 'README.txt')) {
        if ($names -notcontains $required) { throw ('Missing release file: ' + $required) }
    }
    if ($names -match 'TestArtifacts|DoNotShip|BackUpThisFolder|\.pdb$|\.mdb$') { throw 'Development artifacts leaked into release package.' }
    $entry = $zip.GetEntry('RiskyDelivery_Data/Managed/Assembly-CSharp.dll')
    $stream = $entry.Open()
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $packedHash = [BitConverter]::ToString($hasher.ComputeHash($stream)).Replace('-', '') }
    finally { $stream.Dispose(); $hasher.Dispose() }
    $builtHash = (Get-FileHash -LiteralPath (Join-Path $source 'RiskyDelivery_Data\Managed\Assembly-CSharp.dll') -Algorithm SHA256).Hash
    if ($packedHash -ne $builtHash) { throw 'Packaged scripts differ from the tested release build.' }
}
finally { $zip.Dispose() }
$archiveHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
Set-Content -LiteralPath ($archive + '.sha256') -Value ($archiveHash + '  ' + [IO.Path]::GetFileName($archive)) -Encoding ASCII

# Only this generated, verified staging directory may be recursively removed.
if (!$stage.StartsWith($packages + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($stage) -notmatch '^Stage-[a-f0-9]{32}$') {
    throw 'Refusing to clean an unexpected staging path.'
}
Remove-Item -LiteralPath $stage -Recurse -Force
Write-Output ('RELEASE_PACKAGE_OK: ' + $archive)
Write-Output ('SHA256: ' + $archiveHash)
