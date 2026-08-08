# Icarus Ultimate Utility Tool (IUUT)

A free, **offline**, open-source Windows tool that **repairs broken [Icarus](https://www.surviveicarus.com/) (RocketWerkz) save files**, **rescues what the game has trapped**, and lets players **safely edit their own local progression** — with automatic backups, atomic writes, and **zero telemetry**.

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/920c16bf5898406495629923788e394f)](https://app.codacy.com/gh/ImPanick/IUUT/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Build & Test](https://github.com/ImPanick/IUUT/actions/workflows/build.yml/badge.svg)](https://github.com/ImPanick/IUUT/actions/workflows/build.yml)
[![Governance](https://github.com/ImPanick/IUUT/actions/workflows/governance-check.yml/badge.svg)](https://github.com/ImPanick/IUUT/actions/workflows/governance-check.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform: Windows x64](https://img.shields.io/badge/platform-Windows%20x64-0078D6)
[![Code style: dotnet format](https://img.shields.io/badge/code%20style-dotnet%20format-success)](.editorconfig)

> **Status — shipping.** Latest release **v2.7.0**. The solution builds clean (0 warnings /
> 0 errors, warnings-as-errors), **363 tests pass**, and `dotnet format` + the governance
> linter verify clean. Releases are built and attested by CI; binaries are **not yet
> Authenticode-signed**, so Windows SmartScreen will warn on first run — verify the checksum
> and build attestation instead ([below](#verify-your-download)).

---

## Mission

Editing an Icarus save means hand-surgery on fragile JSON (and a compressed binary world blob)
that the game will silently reject — or that a mid-write crash, a Steam Cloud conflict, or a bad
patch can corrupt outright. **IUUT exists so no Icarus player has to do that by hand, and so a
broken save is recoverable instead of lost.** It is a community tool, unaffiliated with
RocketWerkz, that reads and writes the exact same files the game already does — on your own PC,
fully offline, never without a backup.

**Non-negotiables** (enforced by an in-repo contract + CI, not just promised):

- **Safety first.** Every write is `backup → atomic temp → validate → rename`; a timestamped
  backup is made before any change, and a write that wouldn't round-trip is refused, never applied.
- **Offline & private.** No telemetry, analytics, crash reporting, cloud upload, accounts, or
  auto-update. The *only* network call is an optional Steam name lookup with your own API key.
  Your save never leaves your machine — a claim the upload-and-paywall portals structurally
  cannot make.
- **No PII, ever.** Real SteamID64s / character names / persona names never enter the repo (CI-enforced).
- **No install.** One self-contained `IUUT.exe` — no setup wizard, no admin, no registry, no .NET required.

## What IUUT does

### 🛠 Rescue — the reason IUUT exists

| | |
| --- | --- |
| **Broken-save recovery** | Full health scan (parses every JSON, checksums every prospect blob) → backup-chain restore (ranks every candidate by parse-OK + recency) → template repair (rebuilds a valid skeleton and salvages what it can) → a plain-English advisor for the *non*-corruption causes (Steam Cloud conflict, Controlled Folder Access, OneDrive conflicted copies, schema mismatch). |
| **Return to Stash** | Items stranded in a prospect you can't re-enter — because the host is gone, or the world won't resume — pulled back into your orbital stash. The stash is written *first*, so an interrupted rescue can only duplicate (recoverable), never lose. |
| **Backup Manager** | Browse every timestamped IUUT backup, restore any of them (the current file is backed up first — restores are themselves reversible), prune the rest. |
| **Loadout recovery** | The two community hand-edits, made safe: flip `bInsured` to free gear held by an offline host, and recreate stash items a loadout references but that vanished — exact GUID and item row, so the loadout is whole again. |
| **Mount rescue** | Clone a roster mount (its binary stats blob carried byte-for-byte), and rename mounts deployed *inside* a prospect. |
| **Base relocation** | Built somewhere you regret? IUUT groups your structures into separate builds and moves one of them, contents and anchoring intact, without rebuilding a thing. It also works out roughly how high the ground is where the build would land — inferred from the world's own features, graded by how sure it is, and never dressed up as a survey — so you can drop it level instead of floating or buried. |

### ⚡ Lazy Max

One click, non-breaking: unlock all character talents (each clamped to its **true** max, mined
from the game's own reward tables), max currencies, unlock all workshop/prospect blueprints,
complete the accolade + bestiary logs, and set the mission-unlock flags — with a confirmation
dialog and an automatic backup. It deliberately leaves your stash, loadouts, prospects, mounts,
and config untouched.

### 🎛 Custom editor

Pick exactly what to change, **Preview → Apply**, always behind a confirm dialog that names the
files it will touch. The sidebar is grouped by intent:

| Group | Editors |
| --- | --- |
| **Progression** | Account & Currencies · Characters & Talents (rename, XP/debt, revive, per-talent ranks) · Accolades & Bestiary · Account Flags · Engine Flags · **Missions** (complete a mission *and* its full prerequisite chain) |
| **World** | **Orbital Stash** (visual grid, durability bars, repair, add/remove, stack editing, loadout-aware warnings) · Loadouts · Prospects (unstick a stuck character) · Mounts · **Prospect Quests** (reset a mission so it can be replayed) |
| **Rescue** | Return to Stash · Backup Manager (broken-save Recovery is its own top-level screen) |
| **Advanced** | Game Tuner (`Engine.ini` tweaks — FPS, fog, quality) · Raw JSON viewer + validated import/export |

Search boxes and virtualized lists throughout (the talent list alone is 2,200+ rows), a staged-edit
indicator, and an unsaved-changes guard so a stray click never discards your work.

### 🔄 Always current — no update treadmill

IUUT **mines your own installed `data.pak` at runtime**. When Icarus patches, the tool re-reads
the game's 257 data tables and merges them into its catalogs on next launch — 2,221 talents, 447
accolades, 197 prospects, and every item and flag, named the way the game names them. The merge is
sanity-gated: if anything looks wrong, the refresh is rejected wholesale and IUUT keeps its shipped
snapshot. Nothing is downloaded; the pak never leaves your disk.

### 🧭 The prospect world blob

IUUT reverse-engineers the game's compressed Unreal world save losslessly — the technical moat no
other maintained local tool has finished. Today that powers Return to Stash, deployed-mount
listing and renaming, and **quest reset** (the category leader's flagship *paid* feature, here
offline and free). Every blob write is gated by round-trip fixtures before it ships.

## Command line

The same engine, headless — for scripting, CI, and power users:

```
iuut check              # health scan every profile (exit 2 = issues found)
iuut backup-all         # timestamped backups of every save file
iuut lazy-max           # preview by default; --apply to write
iuut catalog-refresh    # re-mine catalogs from your data.pak
iuut prospect-report    # per-prospect mission + quest-step state, trapped-item totals
iuut quest-reset        # reset a prospect's mission (preview; --apply to write)
iuut homestead-move     # list your builds; relocate one (preview; --apply to write)
                        #   --snap sits the build on the estimated ground height
```

## Get IUUT

Two ways to the **same** single-file `IUUT.exe` — full guide in **[docs/INSTALL.md](docs/INSTALL.md)**:

- **Download (recommended):** grab `IUUT.exe` + `SHA256SUMS.txt` from
  [Releases](https://github.com/ImPanick/IUUT/releases), verify (below), then double-click.
- **Build it yourself:** clone and run `scripts/publish-release.ps1` (see *Build & run*).

**No install, no admin, no registry.** IUUT is one `.exe`; its only footprint is a single
`%AppData%\IUUT\` folder (or a portable `IUUT-Data\` beside the exe — drop an empty `IUUT.portable`
file next to `IUUT.exe`). Removal = delete the exe + that folder.

### Verify your download

The published `IUUT.exe` is self-contained (no .NET install needed) and is built by CI. Because it
is not yet code-signed, verifying is how you establish trust:

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
| **Game data** | Self-refreshed from your local `Icarus\Content\Data\data.pak` |
| **Online** | Optional Steam name lookup only; **all editing works fully offline** |
| **Telemetry** | **None.** No analytics, no cloud, no accounts. |

## Where IUUT is going

The current arc is tracked in **[docs/ELEVATION-ROADMAP.md](docs/ELEVATION-ROADMAP.md)**. Tiers 0–2
(foundation, self-refresh, UI overhaul, and surfacing every finished Core capability) are complete;
Tier 3 spends the blob moat on features competitors paywall:

| Next | What it is |
| --- | --- |
| **Homestead pack-up** | Moving a base *within* its prospect ships today. The remaining half is carrying it to a **different** prospect — extracting the actor subtree and re-homing its ids in the destination world. Gated: lossless round-trip fixtures must pass before it is even announced. |
| **Offline map + deployable viewer** | Plot your containers and bases from the local blob over terrain maps; click a crate → see contents → jump to Return to Stash. The privacy-preserving alternative to upload-based cartographers. |
| **Click-and-deploy relocation** | Base relocation, on the map: pick a build, click where you want it, done. Both halves now exist — the move (v2.12.0) and the ground height (v2.13.0) — so what's left is letting you pick the spot by eye instead of typing coordinates. |
| **Deeper prospect diagnosis** | Header repair and host reassignment for the "failed to resume prospect" threads that today have no tool at all. |
| **Code signing** | Authenticode via Azure Trusted Signing, to retire the SmartScreen warning. |

## Build & run

Requires the **.NET 8 SDK** (a newer SDK works via `global.json` roll-forward).

```powershell
git clone https://github.com/ImPanick/IUUT.git
cd IUUT
pwsh -File scripts/install-hooks.ps1               # REQUIRED governance hook

dotnet build IcarusUltimateUtilityTool.sln -c Release   # 0 warnings / 0 errors
dotnet test  IcarusUltimateUtilityTool.sln -c Release   # 363 tests
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
- **385 xUnit tests** — round-trip parse/serialize, edit services, recovery, blob codecs,
  catalog-refresh merge rules, and **surgical write gates** for every blob edit (the quest-reset
  test counts the exact bytes that change and re-verifies that neighbouring records are untouched).
- **Adversarial review** before releases: multi-agent passes that must confirm a finding against
  the real code before it counts.

External grading is via **[Codacy](https://www.codacy.com/)**. The repo ships a
[`.codacy.yaml`](.codacy.yaml) that excludes embedded game-data catalogs, fixtures, docs, and
mockups so the grade reflects real source code.

---

## Repository map

```
IUUT/
├── AGENTS.md                     ← start here if you're contributing (human or agent)
├── .agent/                       ← the binding governance contract (CONSTITUTION etc.)
├── .github/  .githooks/          ← CI workflows + commit-msg governance hook
├── docs/
│   ├── IUUT-PROJECT-DOCUMENTATION.md   ← master spec (what to build)
│   ├── ELEVATION-ROADMAP.md            ← the current arc: tiers, status, what's next
│   ├── DATA-PROVENANCE.md              ← where the catalog data comes from + how it refreshes
│   ├── DEVELOPMENT.md / CICD.md        ← dev runbook / pipelines + versioning policy
│   └── INSTALL.md                      ← operator guide (get, verify, run, remove)
├── Icarus-Analysis.md            ← save-format field guide (technical source of truth)
├── src/
│   ├── IUUT.Core/                ← domain logic (zero UI deps)
│   │   ├── DataPak/              ←   runtime miner + sanity-gated catalog refresh
│   │   ├── Prospects/World/      ←   UE world-blob reader/writer (items, mounts, quests)
│   │   ├── Editing/  Recovery/   ←   edit + rescue services
│   │   └── Io/                   ←   safe writer, backups, backup inventory
│   ├── IUUT.Catalog/             ← embedded catalog snapshots (fallback for the self-refresh)
│   ├── IUUT.App/                 ← WPF shell — Controls/ Dialogs/ Theme/ ViewModels/ Views/
│   └── IUUT.Cli/                 ← headless CLI (check, backup-all, lazy-max, …)
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
CI enforce this. See **[docs/CICD.md](docs/CICD.md)** for the pipeline and versioning policy
(`X` = major/overhaul, `Y` = content, `Z` = fixes), and **[SECURITY.md](SECURITY.md)** for disclosure.

## Disclaimer

IUUT is **not affiliated with RocketWerkz** or the publishers of Icarus. It modifies local files
only. **Back up your save folder before making changes** (IUUT does this automatically, but a manual
copy never hurts). Multiplayer hosts should coordinate with their group before editing shared
prospect files.

## License

[MIT](LICENSE).
