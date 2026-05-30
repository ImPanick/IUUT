<#
.SYNOPSIS
    Install IUUT's versioned git hooks by pointing core.hooksPath at .githooks/.

.DESCRIPTION
    Run once after cloning. Sets `git config core.hooksPath .githooks` so the
    repo-tracked hooks in .githooks/ are used by every git operation in this
    working tree. The hooks themselves are versioned with the project, so they
    travel with every clone.

    Currently installs:
      - commit-msg  → validates Agent / Consulted / Co-Authored-By trailers
                      per .agent/HANDOFF_PROTOCOL.md §2.

.EXAMPLE
    pwsh -File scripts/install-hooks.ps1

.NOTES
    Authority: AGENTS.md §5 (Enforcement), .agent/CONSTITUTION.md VIII
               (Consultation-mandatory).
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if (-not (Test-Path .git)) {
    Write-Host "[install-hooks] ERROR: not at the root of a git repo (no .git/ here)." -ForegroundColor Red
    Write-Host "                Run this from the repo root after 'git init' / 'git clone'."
    exit 1
}

if (-not (Test-Path .githooks)) {
    Write-Host "[install-hooks] ERROR: .githooks/ directory not found." -ForegroundColor Red
    Write-Host "                The repo's versioned hooks should live in .githooks/."
    exit 1
}

# Point git at the versioned hooks.
git config core.hooksPath .githooks
$currentPath = git config --get core.hooksPath
if ($currentPath -ne '.githooks') {
    Write-Host "[install-hooks] ERROR: failed to set core.hooksPath." -ForegroundColor Red
    exit 1
}

Write-Host "[install-hooks] core.hooksPath = .githooks" -ForegroundColor Green

# Ensure hooks are marked executable (no-op on Windows NTFS, but git tracks the
# bit and other platforms / WSL need it).
$hookFiles = Get-ChildItem .githooks -File -ErrorAction SilentlyContinue
if ($hookFiles) {
    foreach ($h in $hookFiles) {
        # On Windows, this is informational only; git's core.fileMode handles it.
        Write-Host "[install-hooks] Found hook: $($h.Name)"
    }
} else {
    Write-Host "[install-hooks] WARNING: .githooks/ is empty." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[install-hooks] Done. From here, every commit in this repo is" -ForegroundColor Green
Write-Host "                gated by .githooks/commit-msg." -ForegroundColor Green
Write-Host ""
Write-Host "                To verify, try an empty commit with a bad message:"
Write-Host "                  git commit --allow-empty -m 'test'   (should be rejected)"
Write-Host ""
Write-Host "                To temporarily bypass (only for governance-amendment"
Write-Host "                bootstrap or true emergencies): git commit --no-verify"
Write-Host "                Bypassing without justification is itself a governance"
Write-Host "                violation — see .agent/CONSTITUTION.md VIII.4."
exit 0
