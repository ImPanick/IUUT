# Changelog

All notable changes to IUUT are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see `docs/CICD.md` §5 for the versioning policy).

Governance changes (amendments to `.agent/`) are additionally tracked in their
respective docs' revision-history tables and, once the first amendment lands,
in `docs/GOVERNANCE_CHANGELOG.md`.

## [Unreleased]

## [2.8.0] — 2026-07-28

### Added

- **Field Guide** (PROGRESSION) — the tracked data the game keeps and IUUT previously only
  preserved without showing: **144 statistics** with their in-game names and categories
  (distance travelled, time survived, creatures killed…), **fishing records** for all 71
  catchable fish — listed whether or not you've caught them, with the game's own lore — and
  the **completion checklists** (e.g. visit every biome). All editable, backed up and
  validated like every other editor.
- Two new self-refreshing catalogs mined from your `data.pak`: `fish.json` and
  `playertrackers.json`.

### Notes

- Investigated removing the minimap fog: it is **not stored in the save**. Every prospect's
  map record (`TileFlags`, `RadarV3Scans`) is empty even on heavily-explored worlds, and the
  game exposes no fog setting — so no save editor can reveal the map.

## [2.7.0] — 2026-07-25

### Added

- **Rename mounts deployed in a prospect** — the "DEPLOYED IN PROSPECTS" list in the Mounts
  editor is no longer read-only: each mount gets a Rename button that rewrites the name
  inside the prospect's world save. The name can grow or shrink freely; everything else in
  the world — stats, inventories, bases, quest state — stays byte-identical, and the file is
  backed up and re-validated as always.

## [2.6.0] — 2026-07-25

### Added

- **Prospect Quests panel** (WORLD) — quest reset arrives in the app: pick a prospect, see
  its mission and step-by-step completion read straight from the world save, and RESET
  MISSION to replay it. Same gated engine as `iuut quest-reset` — in-place and
  size-preserving, so items, mounts, and bases stay byte-identical; backed up and
  re-validated on every write.

## [2.5.0] — 2026-07-25

### Added

- **Quest reset** (`iuut quest-reset --prospect <name> [--apply]`) — reset a prospect's
  mission progress so it can be replayed. Preview by default; `--apply` writes with a
  backup first. Every write is in-place and size-preserving: items, mounts, bases, and
  every other recorder stay byte-identical (the gate tests count the exact bytes that
  change). The category leader's flagship paid feature — offline and free. CLI-first by
  design; the app panel follows.

## [2.4.0] — 2026-07-25

Tier 3 opens: the quest-state research track reaches its read-only milestone.

### Added

- **Quest-state decoding (read-only)** — IUUT now reads the faction mission and every
  quest step's completion state directly from a prospect's world blob. No writes: per the
  roadmap's research-track rule, quest *reset* only lands after round-trip fixtures pass.
- **`iuut prospect-report`** — a per-prospect report over the world saves: mission +
  step-by-step `[done]` state, and trapped-item totals (pointing at Return to Stash).
  `--profile` is optional and defaults to the save root's first profile.

## [2.3.0] — 2026-07-25

**Tier 2 complete** — the last two community hand-edits, made safe:

### Added

- **Loadout recovery** — INSURE ALL flips `bInsured` on every uninsured loadout (the fix
  for gear stuck with an offline host; only the one boolean changes), and RESTORE MISSING
  recreates stash items the loadouts reference but that vanished — exact GUID and item row,
  so the loadout is whole again. Both additive, both backed up and re-validated.
- **Mount rescue (roster restore/clone)** — the "Mount Reviver": clone any roster mount as
  a staged deep copy (its binary stats blob carried byte-for-byte), rename it, Apply.

## [2.2.0] — 2026-07-25

### Added

- **Backup Manager** (RESCUE) — browse every timestamped IUUT backup in the save folder
  (searchable, newest first), restore any of them (the current file is backed up first, so
  a restore is itself reversible), and prune old backups keeping the newest 3 per file.
  Only `.iuut-backup-` files are ever touched — save files are never deletion candidates.

## [2.1.0] — 2026-07-25

Tier 2 begins: two Core services that were fully built and tested but had no UI are now
reachable, in the sidebar homes Divine Elevation pre-placed for them.

### Added

- **Missions checklist** (PROGRESSION) — every catalog mission with its completion state;
  staging a mission completes it *and* its full prerequisite chain on apply. Additive and
  idempotent — completed missions are never revoked. Searchable, stage-all, dirty-guarded.
- **Return to Stash** (RESCUE) — pick a prospect world save, preview the items trapped in
  it, and return them all to the orbital stash. The stash is written first, so a
  mid-operation failure can only duplicate items (recoverable from backup), never lose
  them. Both files are backed up and re-validated.

## [2.0.0] — 2026-07-25

**Divine Elevation** — the owner-named UI/UX overhaul (roadmap Tier 1.5). The graphite
glass console stays; it becomes a *system*: one token set, five reusable primitives, and
navigation regrouped by intent — explicitly built as the foundation Tier-2 features
(Return to Stash, Missions, Backup Manager) will land in.

### Changed

- **Intent-grouped sidebar** — the flat 11-item Custom list becomes PROGRESSION / WORLD /
  RESCUE / ADVANCED, with Tier-2 homes pre-placed as tier-tagged (disabled) entries.
- **Characters & Talents card/detail** — character cards (name, slot, dead-revive marker)
  replace the combo box; the 2.2k-row talent list lives in the shared search control.
- **One confirm shell** — every destructive apply (8 editors, unstick, raw save, Lazy Max,
  Recovery repair, the discard guard) uses the same themed confirm-with-diff dialog
  instead of the OS message box.
- **Staged-state language** — a dedicated amber "staged" color: the Custom header lights a
  STAGED chip while any editor holds unapplied edits.
- **Design tokens** — type scale, spacing scale, state roles consolidated in the theme;
  the last per-view style one-offs removed.

### Added

- `FilteredListBox`, `StateDisplay` (empty/loading/error trio), `ProgressPanel`
  (long-operation surface, first used by Recovery), `ConfirmDialog` — the reusable
  primitive set later tiers build on.
- Home now shows catalog provenance: "refreshed from your game" vs "shipped snapshot",
  with the exact stamp in the tooltip.

## [1.8.0] — 2026-07-25

### Added

- **Runtime catalog self-refresh** — IUUT now mines the installed game's `data.pak` itself.
  On startup (and via `iuut catalog-refresh`), a changed pak is inflated, its 257 DataTables
  split, and every catalog re-merged under the codified weekly rules: superset talents
  (vanished rows kept as not-live, curated names never overwritten), `Item.Meta` additions
  with exact `maxDurability`, order-sensitive flag catalogs (pure append or full rejection),
  regenerated missions/prospects. Any sanity-gate failure rejects the whole refresh and the
  app stays on its shipped snapshots. Fully offline — the pak never leaves the machine.
- **Headless CLI** (`iuut.exe`) — `check` (health scan, CI-friendly exit codes), `backup-all`,
  `lazy-max` (preview by default, `--apply` to write), `catalog-refresh`.
- **Search everywhere** — live search boxes over the 2.2k-row talent list (now virtualized),
  accolades, bestiary, engine flags, account flags, and the Game Tuner settings.
- **Unsaved-changes guard** — switching the Custom category or save profile with staged,
  unapplied edits now asks before discarding (all seven stage-then-apply editors).
- **Prospects catalog** — a new `D_ProspectList` catalog (197 prospects); the Prospects
  editor shows in-game drop names ("ARCWOOD: Outpost") next to the raw association ids.

## [1.7.0] — 2026-07-24

### Added

- **True per-talent max ranks** mined from `Talent.Rewards` — the talent editor, Lazy Max,
  and the rank sliders are exact per row (Genetics rows mine at 1–3; three creature talents at 5).
  Loaded save ranks are never coerced (a stale catalog must not downgrade earned ranks), and
  Lazy Max is raise-only for ranks, matching its currency/XP contract.
- **Account Flags checklist editor** (#81) — `Profile.json` `UnlockedFlags` by friendly name.
- **Stash add-picker filter** (#83 first slice) — substring search over the 380+ item picker.
- **DataPakMiner + DataPakLocator** (Core) — the C# runtime data.pak miner that powers the
  upcoming catalog self-refresh (257 tables in ~0.3s; Steam library discovery via VDF).

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
