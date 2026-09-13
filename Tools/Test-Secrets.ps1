param([switch]$SkipHistory)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$toolRoot = Join-Path $project 'Builds/SecurityTools/8.30.1'
New-Item -ItemType Directory -Force -Path $toolRoot | Out-Null
$archive = Join-Path $toolRoot 'gitleaks.zip'
$expected = 'd29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e'
if (!(Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/gitleaks/gitleaks/releases/download/v8.30.1/gitleaks_8.30.1_windows_x64.zip' -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) { throw 'Gitleaks download checksum mismatch.' }
Expand-Archive -LiteralPath $archive -DestinationPath $toolRoot -Force
$scanner = Join-Path $toolRoot 'gitleaks.exe'
$reports = Join-Path $project 'Logs/Security'
New-Item -ItemType Directory -Force -Path $reports | Out-Null
if (!$SkipHistory) {
    & $scanner git $project --log-opts=--all --redact --no-banner --report-format json --report-path (Join-Path $reports 'history.json')
    if ($LASTEXITCODE -ne 0) { throw 'Git history secret scan failed. Inspect the redacted local report.' }
}
# Scan publishable working files, including unstaged changes; do not traverse Unity caches.
$stage = Join-Path $project ('Builds/SecurityScan-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$files = & git -C $project -c core.quotepath=false ls-files --cached --others --exclude-standard
if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate repository files.' }
foreach ($relative in ($files | Sort-Object -Unique)) {
    $source = Join-Path $project $relative
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) { continue }
    if ((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Secret scan refuses symbolic links.' }
    $destination = Join-Path $stage $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
}
& $scanner dir $stage --redact --no-banner --report-format json --report-path (Join-Path $reports 'working-tree.json')
if ($LASTEXITCODE -ne 0) { throw 'Working tree secret scan failed. Inspect the redacted local report.' }
$resolvedStage = [IO.Path]::GetFullPath($stage)
$buildRoot = [IO.Path]::GetFullPath((Join-Path $project 'Builds')) + [IO.Path]::DirectorySeparatorChar
if (!$resolvedStage.StartsWith($buildRoot, [StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolvedStage) -notmatch '^SecurityScan-[a-f0-9]{32}$') { throw 'Invalid scan cleanup path.' }
Remove-Item -LiteralPath $resolvedStage -Recurse -Force
Write-Output 'SECRET_SCAN_OK: Gitleaks 8.30.1, redacted reports, Git history and publishable working files checked.'
