# Changelog

All notable changes to IUUT are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see `docs/CICD.md` §5 for the versioning policy).

Governance changes (amendments to `.agent/`) are additionally tracked in their
respective docs' revision-history tables and, once the first amendment lands,
in `docs/GOVERNANCE_CHANGELOG.md`.

## [Unreleased]

### Added

- **True per-talent max ranks** mined from `Talent.Rewards` — the talent editor, Lazy Max,
  and the rank sliders are exact per row (Genetics rows max at 2; three creature talents at 5).
- **Account Flags checklist editor** (#81) — `Profile.json` `UnlockedFlags` by friendly name.
- **Stash add-picker filter** (#83 first slice) — substring search over the 380+ item picker.

### Fixed

- **One shared save root**: the folder browsed on Home now reaches Custom, Recovery, and the
  Game Tuner (previously each hardcoded the default — non-default Steam libraries broke).
- **Apply feedback**: the "Applied — a backup was taken" status survives the post-apply reload
  in all seven editors; Stash/Engine-Flags selection survives staged operations.
- `extract-datapak.ps1`: RowStruct collisions no longer silently overwrite; every run emits
  `_inventory.csv` for week-over-week new-table detection.

## [1.6.0] — 2026-07-24
Week 240–242 catch-up catalog refresh (Workshop Flashlight → the stash picker; Settlement/GH talents).

## [1.5.0] — 2026-07-03
Week 239 refresh — MXC Oxite Dissolver + Salt pack join the stash picker (first `items.json` change).

## [1.4.1] — 2026-07
Per-prospect deployed mount listings (closes #19) — mounts read from prospect world blobs.

## [1.4.0] — 2026-06-26
Show-unreleased catalog toggle (`live` flag superset model), Game Tuner expansion from the game's
datamined `SettingsSchema.json`, readable Loadouts viewer, Week 238 refresh.

## [1.3.0] — 2026-06-18
Week 237 catalog refresh (Settlement Radius/Fortified Walls talents).

## [1.2.0] — 2026-06-12
Week 236 catalog refresh (+11 Settlement Hub talents; non-destructive superset model groundwork).

## [1.1.0] — 2026-06-10
Prospects editor ProspectID fix (closes #8), Week 235 catalog refresh, tag-derived exe version.

## [1.0.0] — 2026-06

### Added

- **Governance contract** for multi-agent development: `AGENTS.md` (universal entry),
  `CLAUDE.md` / `.cursorrules` / `.cursor/rules/agents.mdc` / `.antigravity/rules.md`
  (agent redirectors), and the `.agent/` folder (CONSTITUTION, SCOPE_GUARDRAILS,
  AGENT_WORKFLOW, HANDOFF_PROTOCOL, DEFINITION_OF_DONE, CODE_STYLE, SECURITY_PROTOCOL,
  TESTING_CONTRACT, AMENDMENT_PROCESS, AGENT_REGISTRY).
- **Enforcement plumbing:** `commit-msg` hook, `governance-lint.ps1`, `install-hooks.ps1`,
  PR template, and the Governance Check CI workflow.
- **Solution scaffold** per master doc §17: `IUUT.Core`, `IUUT.Catalog`, `IUUT.App` (WPF),
  `IUUT.Cli`, and `IUUT.Core.Tests`, with `Directory.Build.props`, `.editorconfig`,
  `global.json`, and the solution file.
- **DevOps groundwork:** `docs/DEVELOPMENT.md` and `docs/CICD.md` runbooks, Build & Test
  CI workflow, Dependabot config, `CONTRIBUTING.md`, `SECURITY.md`, this changelog,
  `CODEOWNERS`, and issue templates.
- **Operator-execution guarantees:** `docs/INSTALL.md` operator guide; `release.yml`
  (single-file `IUUT.exe` + portable zip + `SHA256SUMS.txt` + Sigstore build-provenance
  attestation on a `vX.Y.Z` tag); master doc §6.4 (two acquisition paths, no-installer /
  no-admin / no-registry footprint, `%AppData%\IUUT\` default + `IUUT.portable` opt-in,
  clean removal) and §19 release pipeline + user verification.

### Fixed

- Steam name-cache path inconsistency in master doc §7.5.1 (now `%AppData%\IUUT\`).

### Notes

- v1.0.0 shipped the complete initial app on top of this scaffold: the three workflows
  (Broken-Save Recovery, Lazy Max, Custom editors), the embedded game catalogs, the prospect
  world-blob read/write stack, and the Game Tuner — 300+ tests, warnings-as-errors.
- The product specification lives in `docs/IUUT-PROJECT-DOCUMENTATION.md`; the save-format
  field guide in `Icarus-Analysis.md`.

---

<!--
  Release entries take this shape once tagging begins (see docs/CICD.md §5):

  ## [0.1.0] - YYYY-MM-DD
  ### Added
  ### Changed
  ### Fixed
  ### Security
-->
