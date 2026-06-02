# Icarus Ultimate Utility Tool (IUUT)

A free, **offline**, open-source Windows tool that **repairs broken [Icarus](https://www.surviveicarus.com/) (RocketWerkz) save files** and lets players **safely edit their own local progression** — with automatic backups, atomic writes, and **zero telemetry**.

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/920c16bf5898406495629923788e394f)](https://app.codacy.com/gh/ImPanick/IUUT/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Build & Test](https://github.com/ImPanick/IUUT/actions/workflows/build.yml/badge.svg)](https://github.com/ImPanick/IUUT/actions/workflows/build.yml)
[![Governance](https://github.com/ImPanick/IUUT/actions/workflows/governance-check.yml/badge.svg)](https://github.com/ImPanick/IUUT/actions/workflows/governance-check.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform: Windows x64](https://img.shields.io/badge/platform-Windows%20x64-0078D6)
[![Code style: dotnet format](https://img.shields.io/badge/code%20style-dotnet%20format-success)](.editorconfig)
<!-- After connecting the repo at https://app.codacy.com, paste the generated badge here:
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/<PROJECT_ID>)](https://app.codacy.com/gh/ImPanick/IUUT/dashboard) -->

> **Status — feature-complete, pre-release.** All three workflows and every save-editing
> category are implemented and tested; the solution builds clean (0 warnings / 0 errors,
> warnings-as-errors), **300+ tests pass**, and `dotnet format` + the governance linter
> verify clean. v1.0.0 ships once it's code-signed and tagged.

---

## Mission

Editing an Icarus save means hand-surgery on fragile JSON (and a binary blob or two) that the
game will silently reject — or that a mid-write crash, a Steam Cloud conflict, or a bad patch
can corrupt outright. **IUUT exists so no Icarus player has to do that by hand, and so a broken
save is recoverable instead of lost.** It is a community tool, unaffiliated with RocketWerkz, that
reads and writes the exact same files the game already does — on your own PC, fully offline, never
without a backup.

**Non-negotiables** (enforced by an in-repo contract + CI, not just promised):

- **Safety first.** Every write is `backup → atomic temp → validate → rename`; a timestamped
  backup is made before any change, and a write that wouldn't round-trip is refused, never applied.
- **Offline & private.** No telemetry, analytics, crash reporting, cloud upload, accounts, or
  auto-update. The *only* network call is an optional Steam name lookup with your own API key.
- **No PII, ever.** Real SteamID64s / character names / persona names never enter the repo (CI-enforced).
- **No install.** One self-contained `IUUT.exe` — no setup wizard, no admin, no registry, no .NET required.

## What IUUT does

Three workflows from one Windows desktop app:

### 🛠 Broken-Save Recovery
Full health scan (parses every JSON + checksums every prospect blob) → backup-chain restore
(finds and ranks every `*.backup*` by parse-OK + recency) → template repair (rebuilds a valid
skeleton and salvages what it can) → a plain-English advisor for the *non*-corruption causes
(Steam Cloud conflict, Controlled Folder Access, OneDrive conflicted copies, schema mismatch).

### ⚡ Lazy Max
One click, non-breaking: unlock all character talents (the game clamps each to its true max on
load), max currencies, unlock all workshop/prospect blueprints, complete the accolade + bestiary
logs, and set the account/character mission-unlock flags — across the four core save files, with a
confirmation dialog and an automatic backup. It deliberately leaves your stash, loadouts, prospects,
mounts, and config untouched.

### 🎛 Custom editor
Pick exactly what to change, **Preview → Apply** with a diff:

| Category | What you can edit |
| --- | --- |
| Account & Currencies | every MetaResource amount |
| Workshop Blueprints | searchable unlock checklist |
| Account Flags | `UnlockedFlags` by name |
| Characters & Talents | rename, XP/debt, revive, per-talent ranks, bulk-max trees |
| Accolades & Bestiary | grant/complete accolades, set creature scan points |
| **Orbital Stash** (signature) | visual item grid, durability bars, **repair**, add/remove, stack editing, loadout-aware warnings |
| Loadouts | per-prospect viewer (stash cross-reference) |
| Mounts | name, level, type |
| Engine Flags | `flags_<SteamID>.dat` checklist by name |
| Prospects | unstick a stuck character; state/difficulty/insurance |
| **Prospect worlds** (new) | edit items *inside* a prospect and **return trapped items to your orbital stash** — IUUT reverse-engineers the game's UE binary world blob to do this losslessly |
| Game Tuner | toggle-owned `Engine.ini` tweaks (FPS, fog, …) |
| Advanced / Raw | read-only JSON viewer + validated import/export |

Catalog-driven: every internal RowName is shown as its human name, re-mined directly from the
game's current `data.pak` (see [docs/DATA-PROVENANCE.md](docs/DATA-PROVENANCE.md)).

## Get IUUT

Two ways to the **same** single-file `IUUT.exe` — full guide in **[docs/INSTALL.md](docs/INSTALL.md)**:

- **Download (recommended):** grab `IUUT.exe` + `SHA256SUMS.txt` from
  [Releases](https://github.com/ImPanick/IUUT/releases), verify (below), then double-click.
- **Build it yourself:** clone and run `scripts/publish-release.ps1` (see *Build & run*).

**No install, no admin, no registry.** IUUT is one `.exe`; its only footprint is a single
`%AppData%\IUUT\` folder (or a portable `IUUT-Data\` beside the exe — drop an empty `IUUT.portable`
file next to `IUUT.exe`). Removal = delete the exe + that folder.

### Verify your download

The published `IUUT.exe` is self-contained (no .NET install needed) and is built reproducibly by
CI. Verify before running:

```powershell
# 1) checksum — compare against the SHA256SUMS.txt shipped with the release
Get-FileHash -Algorithm SHA256 .\IUUT.exe

# 2) build provenance — confirm GitHub Actions built this exact binary (Sigstore attestation)
gh attestation verify .\IUUT.exe --repo ImPanick/IUUT
```

If you built it yourself, `scripts/publish-release.ps1` writes the matching hashes to
`artifacts/SHA256SUMS.txt`.

## Quick facts

| | |
| --- | --- |
| **Platform** | Windows 10/11 x64 only |
| **Stack** | .NET 8, WPF (WPF-UI), self-contained single-file `IUUT.exe` |
| **Save root** | `%LOCALAPPDATA%\Icarus\Saved\` |
| **Target** | `PlayerData\<SteamID>\` (shown in UI as your Steam **display name**) |
| **Online** | Optional Steam name lookup only; **all editing works fully offline** |
| **Telemetry** | **None.** No analytics, no cloud, no accounts. |

## Build & run

Requires the **.NET 8 SDK** (a newer SDK works via `global.json` roll-forward).

```powershell
git clone https://github.com/ImPanick/IUUT.git
cd IUUT
pwsh -File scripts/install-hooks.ps1               # REQUIRED governance hook

dotnet build IcarusUltimateUtilityTool.sln -c Release   # 0 warnings / 0 errors
dotnet test  IcarusUltimateUtilityTool.sln -c Release   # 300+ tests
dotnet run   --project src/IUUT.App                     # launch the app

# produce the shippable single-file IUUT.exe + SHA256SUMS.txt
pwsh -File scripts/publish-release.ps1
```

Full prerequisites and workflow: **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)**.

## Code quality

Quality is gated, not assumed:

- **Warnings-as-errors** across the solution, with Roslyn analyzers enabled (`Directory.Build.props`).
- **`dotnet format --verify-no-changes`** style gate in CI.
- **Governance linter** (`scripts/governance-lint.ps1`) — blocks committed PII (SteamID/persona),
  BOM-emitting encoders, and contract violations on every PR.
- **300+ xUnit tests** (round-trip parse/serialize, edit services, recovery, codecs, validation).

External grading is via **[Codacy](https://www.codacy.com/)**. To activate it: sign in to
[app.codacy.com](https://app.codacy.com) with GitHub, add `ImPanick/IUUT`, and paste the generated
**Grade** badge into the badge row above. The repo ships a [`.codacy.yaml`](.codacy.yaml) that excludes
embedded game-data catalogs, fixtures, docs, and mockups so the grade reflects real source code.

---

## Repository map

```
IUUT/
├── AGENTS.md                     ← start here if you're contributing (human or agent)
├── .agent/                       ← the binding governance contract (CONSTITUTION etc.)
├── .github/  .githooks/          ← CI workflows + commit-msg governance hook
├── docs/
│   ├── IUUT-PROJECT-DOCUMENTATION.md   ← master spec (what to build)
│   ├── EXECUTION-PLAN.md               ← phased path to v1.0
│   ├── DATA-PROVENANCE.md              ← where the catalog data comes from + how to re-mine
│   ├── DEVELOPMENT.md / CICD.md        ← dev runbook / pipelines
│   └── INSTALL.md                      ← operator guide (get, verify, run, remove)
├── Icarus-Analysis.md            ← save-format field guide (technical source of truth)
├── src/
│   ├── IUUT.Core/                ← domain logic (zero UI deps): editing, recovery, prospect blobs
│   ├── IUUT.Catalog/             ← embedded D_* catalog data (re-mined from data.pak)
│   ├── IUUT.App/                 ← WPF shell
│   └── IUUT.Cli/                 ← headless CLI (scaffold)
├── tests/IUUT.Core.Tests/        ← xUnit + FluentAssertions
└── scripts/                      ← publish, extract-datapak, governance-lint, install-hooks
```

## Contributing — a governed multi-agent repo

IUUT is built by **multiple AI coding agents** (Claude Code, OpenAI Codex, Cursor, Google
Antigravity) **and humans**, all bound by an enforceable contract. Before touching anything:

1. **[AGENTS.md](AGENTS.md)** — the universal contract.
2. **[.agent/CONSTITUTION.md](.agent/CONSTITUTION.md)** — immutable principles.
3. **[CONTRIBUTING.md](CONTRIBUTING.md)** — the contribution loop.

Every commit cites the docs it consulted and declares its agent identity; the `commit-msg` hook and
CI enforce this. See **[docs/CICD.md](docs/CICD.md)** for the pipeline and **[SECURITY.md](SECURITY.md)**
for disclosure.

## Disclaimer

IUUT is **not affiliated with RocketWerkz** or the publishers of Icarus. It modifies local files
only. **Back up your save folder before making changes** (IUUT does this automatically, but a manual
copy never hurts). Multiplayer hosts should coordinate with their group before editing shared
prospect files.

## License

[MIT](LICENSE).
