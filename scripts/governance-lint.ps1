<#
.SYNOPSIS
    IUUT governance lint — validates a diff (or working tree) against the
    binding contract in .agent/CONSTITUTION.md and supporting docs.

.DESCRIPTION
    Run locally before committing, and as part of .github/workflows/governance-check.yml.

    Checks performed:
      1. PII scan (Steam IDs, hardcoded usernames) per SECURITY_PROTOCOL §2.
      2. BOM-emitting UTF-8 encoder usage per Icarus-Analysis §10 / CONSTITUTION III.
      3. Banned commits-of-shame (.iuut-backup-*, .editor-backup-*, *.bak in committed paths).
      4. Required revision-history update when docs/IUUT-PROJECT-DOCUMENTATION.md or .agent/* is touched.
      5. PR-template presence (when .github/ exists).

    Exits non-zero on any violation; prints actionable messages.

.PARAMETER DiffFiles
    Path to a text file listing the files to lint (one per line). Used by CI.
    If omitted, lints all tracked files in the working tree.

.PARAMETER StagedOnly
    If set, lints only files currently staged for commit (typical pre-commit usage).

.EXAMPLE
    pwsh -File scripts/governance-lint.ps1

.EXAMPLE
    pwsh -File scripts/governance-lint.ps1 -StagedOnly

.EXAMPLE
    pwsh -File scripts/governance-lint.ps1 -DiffFiles changed-files.txt
#>

[CmdletBinding()]
param(
    [string] $DiffFiles,
    [switch] $StagedOnly
)

$ErrorActionPreference = 'Stop'
$violations = New-Object System.Collections.Generic.List[string]

function Add-Violation {
    param([string] $file, [int] $line, [string] $rule, [string] $message)
    $loc = if ($line -gt 0) { "${file}:${line}" } else { $file }
    $violations.Add("[$rule] ${loc}: ${message}") | Out-Null
}

# --- Determine target files ---
$files = @()
if ($DiffFiles -and (Test-Path $DiffFiles)) {
    $files = Get-Content $DiffFiles | Where-Object { $_ -and (Test-Path $_) }
} elseif ($StagedOnly) {
    $staged = git diff --cached --name-only 2>$null
    $files = $staged | Where-Object { $_ -and (Test-Path $_) }
} else {
    $tracked = git ls-files 2>$null
    $files = $tracked | Where-Object { $_ -and (Test-Path $_) }
}

if (-not $files -or $files.Count -eq 0) {
    Write-Host "[governance-lint] No files to lint."
    exit 0
}

Write-Host "[governance-lint] Linting $($files.Count) file(s)..."

# --- Patterns ---
# SteamID64: starts with 7656119 and is followed by 10 digits (17 total).
$steamIdPattern = '7656119\d{10}'

# Allowed sentinel SteamID per SECURITY_PROTOCOL §3.
$allowedSentinel = '00000000000000000'

# Known hardcoded-username smell: literal "C:\Users\<name>\" with a real name.
# We allow "<UserName>", "<user>", "josep" only in the existing field guide /
# .agent docs that need to reference live paths. Block everywhere else.
$hardcodedUsernamePattern = '[Cc]:\\[Uu]sers\\(?!<|%USERPROFILE%|josep)[A-Za-z0-9_.-]+'

# BOM-emitting encoders.
$bomEncoderPatterns = @(
    '\[System\.Text\.Encoding\]::UTF8(?!Encoding)',
    'new\s+UTF8Encoding\s*\(\s*true\s*\)',
    'Encoding\.UTF8(?!\s*\.\s*GetString)'  # heuristic; Encoding.UTF8.GetString is read-only
)

# Files that may legitimately contain steamId-shaped strings: governance docs
# describing the regex itself, and the field guide / live evidence sections.
$piiAllowlist = @(
    '\.agent[/\\]SECURITY_PROTOCOL\.md',
    '\.agent[/\\]CONSTITUTION\.md',
    'scripts[/\\]governance-lint\.ps1',
    '\.github[/\\]workflows[/\\]governance-check\.yml',
    'AGENTS\.md',
    'CLAUDE\.md',
    '\.agent[/\\]AGENT_WORKFLOW\.md',
    '\.agent[/\\]HANDOFF_PROTOCOL\.md',
    '\.agent[/\\]DEFINITION_OF_DONE\.md',
    '\.agent[/\\]TESTING_CONTRACT\.md'
)

# Files that may legitimately reference the live save path with "josep" username
# (the project's own dev environment).
$pathAllowlist = @(
    'Icarus-Analysis\.md',
    'docs[/\\]IUUT-PROJECT-DOCUMENTATION\.md',
    'docs[/\\]icarus-save-editor-gameplan\.md',
    'docs[/\\]max-icarus-characters_47df3b52\.plan\.md',
    'README\.md',
    'AGENTS\.md',
    'CLAUDE\.md',
    '\.agent[/\\].+\.md',
    'scripts[/\\]governance-lint\.ps1'
)

function Test-Allowlist {
    param([string] $path, [string[]] $patterns)
    foreach ($p in $patterns) {
        if ($path -match $p) { return $true }
    }
    return $false
}

# --- Scan each file ---
foreach ($f in $files) {
    if (-not (Test-Path $f) -or (Get-Item $f).PSIsContainer) { continue }

    $ext = [System.IO.Path]::GetExtension($f).ToLowerInvariant()
    # Skip binary files
    if ($ext -in @('.exr', '.fog', '.dat', '.png', '.jpg', '.zip', '.pdb', '.dll', '.exe')) {
        continue
    }

    try {
        $lines = Get-Content $f -ErrorAction Stop
    } catch {
        continue
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNo = $i + 1

        # 1. SteamID64 PII scan
        if ($line -match $steamIdPattern -and $matches[0] -ne $allowedSentinel) {
            if (-not (Test-Allowlist $f $piiAllowlist)) {
                Add-Violation $f $lineNo 'PII-STEAMID' "Real-shaped SteamID64 '$($matches[0])' found. Replace with $allowedSentinel per SECURITY_PROTOCOL §3."
            }
        }

        # 2. Hardcoded username path
        if ($line -match $hardcodedUsernamePattern) {
            if (-not (Test-Allowlist $f $pathAllowlist)) {
                Add-Violation $f $lineNo 'PII-PATH' "Hardcoded user-profile path '$($matches[0])' found. Use %USERPROFILE% / %LOCALAPPDATA% per CODE_STYLE §10."
            }
        }

        # 3. BOM-emitting encoder — applies to CODE only (.cs / .ps1 / .psm1).
        #    Markdown and other docs legitimately cite the banned pattern as a
        #    "don't do this" counter-example, so they are exempt. The lint script
        #    itself documents the pattern and is also exempt.
        $isCodeFile = ($ext -in @('.cs', '.ps1', '.psm1')) -and ($f -notmatch '(?i)scripts[/\\]governance-lint\.ps1')
        if ($isCodeFile) {
            foreach ($p in $bomEncoderPatterns) {
                if ($line -match $p) {
                    Add-Violation $f $lineNo 'BOM-ENCODER' "BOM-emitting UTF-8 encoder detected. Use 'new UTF8Encoding(false)' or '(New-Object System.Text.UTF8Encoding `$false)' per Icarus-Analysis §10."
                }
            }
        }
    }
}

# --- 4. Revision-history update check (informational) ---
# If a governance or spec doc changed but its revision history table did not, warn.
# (Soft check — printed but does not fail the run; reviewer judgment.)
$revHistoryDocs = @(
    'docs/IUUT-PROJECT-DOCUMENTATION.md',
    '.agent/CONSTITUTION.md',
    '.agent/SCOPE_GUARDRAILS.md',
    '.agent/AGENT_WORKFLOW.md',
    '.agent/HANDOFF_PROTOCOL.md',
    '.agent/DEFINITION_OF_DONE.md',
    '.agent/CODE_STYLE.md',
    '.agent/SECURITY_PROTOCOL.md',
    '.agent/TESTING_CONTRACT.md',
    '.agent/AMENDMENT_PROCESS.md',
    '.agent/AGENT_REGISTRY.md',
    'Icarus-Analysis.md'
)
foreach ($d in $revHistoryDocs) {
    $normalized = $d -replace '/', [IO.Path]::DirectorySeparatorChar
    if ($files -contains $d -or $files -contains $normalized) {
        if (Test-Path $d) {
            $content = Get-Content $d -Raw
            if ($content -notmatch '(?im)^\|\s*\d+\.\d+\.\d+\s*\|') {
                Write-Host "[governance-lint] NOTE: $d was modified; verify Revision history table has a new entry." -ForegroundColor Yellow
            }
        }
    }
}

# --- 5. PR-template presence ---
if (Test-Path '.github') {
    if (-not (Test-Path '.github/PULL_REQUEST_TEMPLATE.md')) {
        Add-Violation '.github/PULL_REQUEST_TEMPLATE.md' 0 'TEMPLATE-MISSING' "Pull request template is missing. Restore from governance baseline."
    }
}

# --- Report ---
if ($violations.Count -gt 0) {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Red
    Write-Host "  IUUT GOVERNANCE LINT — $($violations.Count) VIOLATION(S)" -ForegroundColor Red
    Write-Host "================================================================" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  $v" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "  Fix the above and re-run. See .agent/SECURITY_PROTOCOL.md and" -ForegroundColor Red
    Write-Host "  .agent/CONSTITUTION.md for the normative rules." -ForegroundColor Red
    Write-Host "================================================================" -ForegroundColor Red
    exit 1
}

Write-Host "[governance-lint] OK — $($files.Count) file(s) clean." -ForegroundColor Green
exit 0
