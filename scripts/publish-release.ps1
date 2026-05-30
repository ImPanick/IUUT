<#
.SYNOPSIS
    Publish a self-contained single-file IUUT.exe release.

.DESCRIPTION
    Authority: docs/IUUT-PROJECT-DOCUMENTATION.md §6.3, §19.

    SCAFFOLD STUB — not implemented yet (no .NET code to publish in pre-development).

    Intended behavior:
      1. Verify clean working tree (`git status --porcelain`).
      2. Verify the current commit is a SemVer tag (`vX.Y.Z`).
      3. Run `dotnet test` — fail if any test fails.
      4. Run `scripts/governance-lint.ps1` against the tagged tree.
      5. dotnet publish IUUT.App with:
           -c Release
           -r win-x64
           --self-contained true
           -p:PublishSingleFile=true
           -p:IncludeNativeLibrariesForSelfExtract=true
      6. Verify resulting IUUT.exe is 15-25 MB (per master doc §6.1 size target).
      7. Verify the binary's manifest declares PerMonitorV2 DPI awareness.
      8. Optionally code-sign if a signing cert is available.
      9. Produce a portable zip with IUUT.exe + LICENSE + README.md.
     10. Emit a release-notes draft (commit log since previous tag, grouped by `<type>`).

.PARAMETER Configuration
    Build config. Defaults to Release.

.PARAMETER Runtime
    Target RID. Defaults to win-x64.

.PARAMETER OutputRoot
    Where to put the published artifacts. Defaults to ./artifacts.

.EXAMPLE
    pwsh -File scripts/publish-release.ps1
#>

[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $Runtime = 'win-x64',
    [string] $OutputRoot = './artifacts'
)

Write-Host "================================================================" -ForegroundColor Yellow
Write-Host "  IUUT — publish-release.ps1 is a SCAFFOLD STUB." -ForegroundColor Yellow
Write-Host "================================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Not implemented. See master doc §6.3, §19, and Phase 6 of §16."
Write-Host "  Implementation lands when v0.1 MVP approaches release readiness."
Write-Host ""
Write-Host "  Requested configuration: $Configuration"
Write-Host "  Requested runtime:       $Runtime"
Write-Host "  Requested output root:   $OutputRoot"
Write-Host ""
exit 64  # EX_USAGE — "feature not implemented"
