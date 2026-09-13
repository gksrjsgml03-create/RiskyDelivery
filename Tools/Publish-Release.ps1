param([switch]$CapturePreview)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
Set-Location $project
if (@(git diff HEAD --name-only).Count -gt 0 -or @(git ls-files --others --exclude-standard -- Assets Tools ProjectSettings Packages .github).Count -gt 0) { throw 'Commit source and automation changes before releasing.' }
$commit = (git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot read source commit.' }
$remoteCommit = (git ls-remote origin refs/heads/main).Split()[0]
if ($LASTEXITCODE -ne 0 -or $remoteCommit -ne $commit) { throw 'Release must be built from the pushed main commit.' }
$line = Select-String -Path 'ProjectSettings/ProjectSettings.asset' -Pattern '^\s*bundleVersion:\s*(\d+\.\d+\.\d+)\s*$'
if (!$line) { throw 'Invalid release version.' }
$version = $line.Matches[0].Groups[1].Value
$tag = 'v' + $version
$repo = 'gksrjsgml03-create/RiskyDelivery'
$existing = gh release list --repo $repo --limit 100 --json tagName | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect releases.' }
if ($existing.tagName -contains $tag) { throw 'Version already exists. Use a new version, or inspect and re-run the existing draft validation workflow.' }
& (Join-Path $PSScriptRoot 'Test-Secrets.ps1')
& (Join-Path $PSScriptRoot 'Package-Release.ps1') -CapturePreview:$CapturePreview
if (@(git diff HEAD --name-only).Count -gt 0) { throw 'Unity modified tracked files during the build. Commit and rebuild before releasing.' }
$archive = Join-Path $project ('Builds/Packages/RiskyDelivery-' + $version + '-Windows.zip')
& (Join-Path $PSScriptRoot 'Test-ReleasePackage.ps1') -Archive $archive -ExpectedCommit $commit -Version $version
$notes = Join-Path $project ('Docs/releases/' + $tag + '.md')
if (!(Test-Path -LiteralPath $notes)) { throw 'Write release notes before publication: ' + $notes }
git tag $tag $commit
if ($LASTEXITCODE -ne 0) { throw 'Cannot create a new release tag. Inspect existing tags.' }
git push origin ('refs/tags/' + $tag)
if ($LASTEXITCODE -ne 0) { throw 'Cannot push release tag.' }
gh release create $tag $archive ($archive + '.sha256') --repo $repo --verify-tag --draft --title ('Risky Delivery ' + $version) --notes-file $notes
if ($LASTEXITCODE -ne 0) { throw 'Draft upload failed. Inspect GitHub Releases before retrying.' }
gh workflow run release.yml --repo $repo --ref main -f ('tag=' + $tag)
if ($LASTEXITCODE -ne 0) { throw 'Draft is uploaded but validation dispatch failed. Run Release validation and publication for this tag.' }
Write-Output ('RELEASE_QUEUED: ' + $tag + '. GitHub publishes only after independent Windows validation succeeds.')
