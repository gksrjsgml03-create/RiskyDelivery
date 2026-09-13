$ErrorActionPreference = 'Stop'
foreach ($file in Get-ChildItem $PSScriptRoot -Filter '*.ps1') {
    $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$null, [ref]$errors)
    if ($errors.Count) { throw ('PowerShell syntax errors: ' + $file.Name) }
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Join-Path (Split-Path -Parent $PSScriptRoot) ('Builds/DeploymentTests-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$commit = 'a' * 40
$validator = Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1'
function New-TestArchive([string]$name, [string]$extra = '') {
    $path = Join-Path $root ($name + '.zip')
    $writer = [IO.Compression.ZipFile]::Open($path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $files = @('RiskyDelivery.exe','UnityPlayer.dll','RiskyDelivery_Data/globalgamemanagers','RiskyDelivery_Data/Managed/Assembly-CSharp.dll','README.txt','build-info.json')
        if ($extra) { $files += $extra }
        foreach ($file in $files) {
            $stream = [IO.StreamWriter]::new($writer.CreateEntry($file).Open())
            try {
                if ($file -eq 'build-info.json') { $stream.Write((@{schema=1;version='1.2.3';sourceCommit=$commit;platform='Windows-x64';dirty=$false;playerTestsPassed=$true} | ConvertTo-Json)) }
                else { $stream.Write('Synthetic archive validation fixture, not an executable.') }
            }
            finally { $stream.Dispose() }
        }
    }
    finally { $writer.Dispose() }
    Set-Content -LiteralPath ($path + '.sha256') -Encoding ASCII -Value ((Get-FileHash $path).Hash + '  ' + [IO.Path]::GetFileName($path))
    return $path
}
function Must-Reject([string]$path, [string]$expectedCommit, [string]$reason) {
    $rejected = $false
    try { & $validator -Archive $path -ExpectedCommit $expectedCommit -Version '1.2.3' | Out-Null }
    catch { $rejected = $true }
    if (!$rejected) { throw ('Validator accepted: ' + $reason) }
}
$valid = New-TestArchive 'valid'
& $validator -Archive $valid -ExpectedCommit $commit -Version '1.2.3'
Must-Reject $valid ('b' * 40) 'wrong source commit'
$traversal = New-TestArchive 'traversal' '../escape.txt'
Must-Reject $traversal $commit 'path traversal'
$private = New-TestArchive 'private' '.env'
Must-Reject $private $commit 'private file'
$duplicate = New-TestArchive 'duplicate' 'RiskyDelivery.exe'
Must-Reject $duplicate $commit 'duplicate executable'
Set-Content -LiteralPath ($valid + '.sha256') -Value ('0' * 64 + '  valid.zip')
Must-Reject $valid $commit 'checksum mismatch'
Write-Output 'DEPLOYMENT_TESTS_OK: syntax, valid package, wrong commit, traversal, private file, duplicate path and hash mismatch.'
