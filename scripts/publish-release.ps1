<#
.SYNOPSIS
    Publish a self-contained single-file IUUT.exe release (local, verifiable).

.DESCRIPTION
    Authority: docs/IUUT-PROJECT-DOCUMENTATION.md §6.3, §19.

    Produces a self-contained, single-file IUUT.exe (no .NET install required on the
    target machine), a portable zip, and a SHA256SUMS.txt over the artifacts so the
    build is verifiable.

    The CANONICAL, attested release is still produced by .github/workflows/release.yml on a
    vX.Y.Z tag (the only place the Sigstore build-provenance attestation is generated). This
    script is the LOCAL build that yields the same exe + checksums for inspection / shipping
    before a tag exists.

    Steps:
      1. Warn if the working tree is dirty (this is then a dev build, not a tagged release).
      2. Run `dotnet test` (unless -SkipTests) — abort on failure.
      3. Run scripts/governance-lint.ps1 if present (non-fatal: warn only on a local build).
      4. dotnet publish IUUT.App: -c Release -r <rid> --self-contained true
           -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
           -p:EnableCompressionInSingleFile=true
      5. Verify IUUT.exe exists; report its size (self-contained WPF is ~70-160 MB compressed).
      6. Assemble a portable bundle (IUUT.exe + IUUT.portable marker + README) and zip it.
      7. Emit SHA256SUMS.txt over IUUT.exe and the zip, and print the hashes.

.PARAMETER Configuration
    Build config. Defaults to Release.

.PARAMETER Runtime
    Target RID. Defaults to win-x64.

.PARAMETER OutputRoot
    Where to put the published artifacts. Defaults to ./artifacts.

.PARAMETER SkipTests
    Skip the dotnet test gate (not recommended for a release build).

.EXAMPLE
    pwsh -File scripts/publish-release.ps1
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $Runtime = 'win-x64',
    [string] $OutputRoot = './artifacts',
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

function Write-Step([string] $msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

try {
    # 1. Working tree status (informational on a local build).
    $dirty = git status --porcelain
    if ($dirty) {
        Write-Warning "Working tree is not clean — this is a DEV build of the current source, not a tagged release."
    }
    $commit = (git rev-parse --short HEAD 2>$null)
    Write-Host "Source commit: $commit" -ForegroundColor DarkGray

    # 2. Test gate.
    if (-not $SkipTests) {
        Write-Step "Running tests"
        dotnet test (Join-Path $repoRoot 'tests/IUUT.Core.Tests/IUUT.Core.Tests.csproj') -c $Configuration --nologo -v q
        if ($LASTEXITCODE -ne 0) { throw "Tests failed — aborting release build." }
    }
    else {
        Write-Warning "Skipping tests (-SkipTests)."
    }

    # 3. Governance lint (non-fatal locally).
    $lint = Join-Path $PSScriptRoot 'governance-lint.ps1'
    if (Test-Path $lint) {
        Write-Step "Governance lint"
        try { & $lint } catch { Write-Warning "governance-lint reported issues (non-fatal for a local build): $_" }
    }

    # 4. Publish single-file self-contained.
    $publishDir = Join-Path $OutputRoot "publish-$Runtime"
    if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

    Write-Step "Publishing IUUT.exe ($Configuration / $Runtime / self-contained single-file)"
    dotnet publish (Join-Path $repoRoot 'src/IUUT.App/IUUT.App.csproj') `
        -c $Configuration -r $Runtime --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $publishDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

    $exe = Join-Path $publishDir 'IUUT.exe'
    if (-not (Test-Path $exe)) { throw "Expected IUUT.exe was not produced in $publishDir." }
    $sizeMB = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host "Produced IUUT.exe — $sizeMB MB" -ForegroundColor Green
    if ($sizeMB -lt 30) { Write-Warning "IUUT.exe is unusually small ($sizeMB MB) for a self-contained WPF build." }

    # 5. Portable bundle: exe + portable marker + README, zipped.
    $bundleDir = Join-Path $OutputRoot "IUUT-portable-$Runtime"
    if (Test-Path $bundleDir) { Remove-Item -Recurse -Force $bundleDir }
    New-Item -ItemType Directory -Force -Path $bundleDir | Out-Null
    Copy-Item $exe (Join-Path $bundleDir 'IUUT.exe')
    # The presence of IUUT.portable makes IUUT keep its state next to the exe (AppPaths portable mode).
    Set-Content -Path (Join-Path $bundleDir 'IUUT.portable') -Value '' -NoNewline
    @"
IUUT — Icarus Ultimate Utility Tool ($commit)

Portable build. Just run IUUT.exe — no .NET install required (self-contained).
The IUUT.portable marker keeps backups/settings next to the exe instead of %APPDATA%.

ALWAYS let IUUT back up your saves (it does this automatically before any write).
Close Icarus before editing a save.
"@ | Set-Content -Path (Join-Path $bundleDir 'README.txt') -Encoding UTF8

    $zip = Join-Path $OutputRoot "IUUT-portable-$Runtime.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path (Join-Path $bundleDir '*') -DestinationPath $zip

    # 6. Checksums.
    Write-Step "Checksums (SHA256)"
    $artifacts = @($exe, $zip)
    $sums = foreach ($a in $artifacts) {
        $h = (Get-FileHash -Algorithm SHA256 -Path $a).Hash.ToLower()
        "{0}  {1}" -f $h, (Split-Path -Leaf $a)
    }
    $sumsFile = Join-Path $OutputRoot 'SHA256SUMS.txt'
    $sums | Set-Content -Path $sumsFile -Encoding ASCII
    $sums | ForEach-Object { Write-Host "  $_" }

    Write-Host ""
    Write-Host "Done." -ForegroundColor Green
    Write-Host "  exe : $exe ($sizeMB MB)"
    Write-Host "  zip : $zip"
    Write-Host "  sums: $sumsFile"
    Write-Host "Verify with: Get-FileHash -Algorithm SHA256 '$exe'"
}
finally {
    Pop-Location
}
