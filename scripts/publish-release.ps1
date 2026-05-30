<#
.SYNOPSIS
    Publish a self-contained single-file IUUT.exe release.

.DESCRIPTION
    Authority: docs/IUUT-PROJECT-DOCUMENTATION.md §6.3, §19.

    SCAFFOLD STUB — not implemented yet (no .NET code to publish in pre-development).

    NOTE: The CANONICAL, verifiable release is produced by .github/workflows/release.yml
    on a vX.Y.Z tag (it is the only place the Sigstore build-provenance attestation can
    be generated). This script is a LOCAL dry-run that produces the same artifacts +
    checksums for inspection before tagging. See docs/IUUT-PROJECT-DOCUMENTATION.md §19
    and docs/CICD.md §5.

    Intended behavior (local dry-run):
      1. Verify clean working tree (`git status --porcelain`).
      2. Run `dotnet test` — fail if any test fails.
      3. Run `scripts/governance-lint.ps1` against the tree.
      4. dotnet publish IUUT.App with:
           -c Release -r win-x64 --self-contained true
           -p:PublishSingleFile=true
           -p:IncludeNativeLibrariesForSelfExtract=true
           -p:EnableCompressionInSingleFile=true
      5. Verify resulting IUUT.exe is ~15-25 MB (per master doc §6.1 size target)
         and that its manifest declares PerMonitorV2 DPI awareness + asInvoker.
      6. Produce IUUT-portable.zip (IUUT.exe + IUUT.portable marker + README).
      7. Generate SHA256SUMS.txt over both artifacts.
      8. Print the `gh attestation verify` command users will run (attestation itself
         is created by CI release.yml, not locally).
      9. Authenticode code-signing is a FUTURE step, gated on obtaining a cert.
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
