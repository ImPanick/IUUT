# Changelog

All notable changes to IUUT are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
(see `docs/CICD.md` §5 for the versioning policy).

Governance changes (amendments to `.agent/`) are additionally tracked in their
respective docs' revision-history tables and, once the first amendment lands,
in `docs/GOVERNANCE_CHANGELOG.md`.

## [Unreleased]

## [2.16.0] — 2026-08-07

### Added

- **Find your body, and bring it to you.** `iuut rescue-grave` lists the bodies and grave markers in
  a prospect — where each one is and how much it is holding — and moves one back within reach.

  Icarus leaves two kinds: `Player_Gravestone_DBNO`, the downed body a teammate can still revive,
  and `Player_Gravestone_MIA`, the marker left when a body is simply gone. MIA is the one nobody can
  recover in-game, and it is what a zone reset or a boss that strands you leaves behind.

  **It moves the grave, not the loot.** The contents are never converted, re-typed, or handed to a
  different container, so no item can end up somewhere that will not accept it — you walk up and
  loot it the way the game intends. Underneath it is the same in-place, size-preserving actor move
  that base relocation already uses.

  ```
  1 grave(s) in 'PGH-5':
    [0] missing-in-action marker — 41 item slot(s) — at (500, -250, 100) m

  Bringing grave [0] to player …2784
    Its 41 item slot(s) ride along untouched
  ```

- **Mount inventories are decoded.** A mount keeps three: the saddle slot (17), cargo (19), and
  heavy cargo (20). Both cargo holds start at **zero** slots (`InventoryInfo`: `Mount_Cargo` = 0,
  `Mount_Heavy_Cargo` = 0), so every cargo slot a mount has was granted — by its saddle or by
  creature talents like `Creature_Base_InventorySlots_Horse` and `Creature_Buffalo_ExtraCarry`,
  which the mount's recorder stores by row name and rank alongside its `Experience`.

### Notes

Gravestone detection is **not yet verified against a real grave** — no save on hand contains one,
because nobody in them is dead. The row names and the 70-slot capacity come from the game's own
tables and the recorder is the ordinary deployable shape, but the first real grave should be checked
before this is trusted blindly. `rescue-grave` says plainly when it finds nothing.

A related risk is now tracked rather than assumed away: `return-to-stash` adds recovered rows to the
orbital stash without checking them against the catalog, and the stash is `Item.Meta` by design
while a prospect holds Fur, Cooked_Meat, and Iron_Wall. For mixed loot, prefer `rescue-grave` — it
converts nothing.

## [2.15.0] — 2026-08-07

### Added

- **See what a character is carrying, laid out the way the game shows it.**
  `iuut rescue-character --prospect <name> --inventory` reads all six of a character's inventories
  and prints them as the game's own panels — Toolbelt, Inventory, the character doll with named
  body slots, the Oxygen/Food/Water column, **Auxiliary** modules, and the light slot.

  ```
    Character  (inventory 5) — 9 item(s)
        [ 0] Head       Meta_Carbon_Head_Alpha
        [ 5] Envirosuit Envirosuit_Larkwell_Alpha
        [ 8] Backpack   Basic_Quiver

    Auxiliary  (inventory 11) — 3 item(s)
        [ 0] Meta_Module_Temperature
  ```

This is the groundwork for editing a live inventory in a panel that mirrors the game screen, and
the decode is the hard half. The six inventory ids were identified from real saves by what each one
contains rather than assumed: **2** toolbelt (it holds the axe, the bow, and the bare-fist entry),
**3** the main grid, **4** the consumable column, **5** the equipment doll in a fixed order
(`Head, Chest, Arms, Legs, Feet, Envirosuit, Skin, Cap, Backpack`), **11** the suit's Auxiliary
module bay, **12** the lantern.

**Slots are stored sparsely** — only occupied ones, each carrying its own `Location` — which a real
character proves by holding exactly two consumables, at Location 0 and Location 2. So the save
records what is carried and *never how much fits*.

Real capacity is base plus what your gear grants, and both live in the game's data rather than the
save. `InventoryInfo.StartingSlots` gives the base (a backpack starts at 24), and an equipped suit
grants more through its armour stats: `Envirosuit_Larkwell_Alpha` resolves to
`Undersuit_Larkwell_Alpha`, whose `ArmourStats` carry `BaseBackpackSlots_+ = 6` and
`BaseUpgradeSlots_+ = 4` — exactly the +6 inventory and +4 module slots the game displays.

Talents add more still. A real character holds items as far out as Location 35 despite 24 + 6 = 30
from base and suit, which is why the grid is drawn as `max(occupied + 1, base + granted)`: an
unmodelled bonus can never hide someone's items.

## [2.14.0] — 2026-08-07

### Added

- **Get a stranded character back.** When a zone resets behind you, or a boss glitches and pins your
  body somewhere you cannot reach, the game offers no way to your gear. `iuut rescue-character`
  lists everyone recorded in a prospect and moves one somewhere reachable, with `--snap` to drop
  them on the ground and `--revive` if they died down there. Their carried inventory travels with
  them — the gear is on the body.

  ```
  2 character(s) in 'PGH-5':
    [0] player …2784 · character slot 3 · alive · 124 hp · at (-1,608, -687, -64) m
         carrying 59 item slot(s); 1 respawn(s) used
  ```

  This is host-side by nature: a character's position and carried items live in the host's prospect
  world save, not in any player's own profile, so one person with the file can free the whole group.
  Everyone must be out of the prospect first, or the running session will overwrite it.

- **`iuut return-to-stash`.** Pulling trapped items back to the orbital stash has worked in the app
  since v2.1.0 but had no CLI verb at all. Now it has one, preview-first like everything else.

### Changed

- **Rescue features you can actually find.** IUUT could already recover items trapped in a prospect,
  and its own author could not find that when he needed it — which makes it, in practice, a feature
  that does not exist. Fixed at the two places someone actually looks:

  - `iuut check` — the first thing anyone runs when something has gone wrong — now reports what is
    sitting inside each prospect (items, characters, and whether any are dead), then names the way
    out: `return-to-stash`, `rescue-character`, and where to find them in the app.
  - The app's RESCUE entry is renamed from **"Return to Stash"** to **"Stuck in a prospect?"**, and
    describes the situations it solves rather than the mechanism it uses. Someone who just lost a
    body to a zone reset searches for "stuck", not for the name of the fix.

Both rescue writes are in-place and size-preserving, the same low-risk class as the quest reset and
the base relocation: `Location` is a natively serialised `Vector`, and `bIsAlive`'s value byte lives
in the property tag. Gated on a real 17 MB two-player save copied to scratch — the target landed
where asked with all 59 carried slots, and the other player's position, health, and 52 slots were
untouched, alongside 885 item slots, 13 quest steps, 460 structures, and the blob's exact length.

## [2.13.0] — 2026-08-07

### Added

- **IUUT now knows roughly how high the ground is.** `iuut homestead-move` reports the estimated
  ground height where your build would land, how confident it is, and whether the build would end
  up floating or buried. `--snap` picks the z offset that sits it on the ground instead of making
  you work it out.

  ```
  Ground height (estimated from 1,088 world features, not surveyed):
    at the destination: -172 m — Low confidence
      nothing within 60 m to measure against — the closest is 65 m away
    The build would land about 67 m BELOW the ground — buried.
    Add --snap to use a z offset of 67 m instead and sit it on the ground.
  ```

This closes the caveat v2.12.0 shipped with, and it is the piece the planned click-and-deploy map
needs: a map click gives X and Y, and this gives Z.

**Where the number comes from.** Icarus does ship real landscape heightmaps, but they are
Oodle-compressed inside the game's pakchunks and the game links Oodle statically — there is no
decompressor IUUT could reach without bundling proprietary code, so that route is closed (recorded
so nobody re-opens it). The save answers the question anyway: every actor that is not player-built
— resource deposits, voxels, cave mouths — sits on the terrain, and v2.11.0 decodes all of their
positions. That is a scattered height field, free and offline.

**What it is worth.** Measured against 1,010 real placements whose true height is known, across
seven prospects: overall median error 2.0 m, p90 8.5 m — but the tail reaches 191 m, which is why
confidence exists rather than a bare number. Every catastrophic miss falls in the low-confidence
buckets. Restricted to high confidence, 66% of placements are covered at a median of 1.6 m, p90
4.4 m, worst 16.9 m. Two calibration results shaped it, both contradicting the obvious guess:
filtering to "clean" resource-deposit samples is four times *worse* than using every world actor
(density beats purity), and cave actors barely bias anything, so they stay in.

It is an estimate and says so on every line that reports it. It is never presented as a survey.

## [2.12.0] — 2026-08-07

### Added

- **Move your base.** `iuut homestead-move --prospect <name>` lists what you've built as separate
  builds — pieces within 60 m of each other count as one — and `--build <n> --by <x,y,z>` relocates
  one of them by that many metres. Preview by default; `--apply` writes after taking a backup.
  Useful when a base ends up somewhere you regret: too far from water, blocking a cave mouth, or
  parked on the drop pod.

  Structures keep their shape, contents, and anchoring, and nothing else in the save changes.
  It moves geometry only — IUUT can't know the ground height where the build lands, so a big jump
  can leave it floating or buried. Short hops on flat ground are the safe case, and the preview
  says so before you commit.

Moving a base *within* its own prospect turns out to need none of the three hard parts of a
cross-prospect move: actor ids stay as they are (so nothing collides and no foundation link
dangles), tame whitelists are untouched, and player-built structures carry no terrain binding at
all — `TileName` is `None` and the level/record/instance indices are `-1` on every structure of
every real prospect checked. That makes a relocation three overwritten floats per piece, the same
low-risk in-place class as the quest reset. The blob's byte length is unchanged.

Gated on a real save copied to scratch: a 23-piece base moved 250 m with its item slots (201),
quest steps (11), other builds, and foundation links all intact, and the blob exactly the same
size. Cross-prospect pack-up still waits on lossless round-trip fixtures.

## [2.11.0] — 2026-08-07

### Added

- **Where your base is.** `iuut prospect-report` now shows each base's centre, elevation, and
  how far it's spread — decoded straight from the world save. Real saves range from a tight
  21-piece camp to 460 structures scattered across 7 km.

Under the hood this decodes each structure's world placement (position, rotation, scale), which
was previously unreadable. It's the prerequisite for two features: relocating a base between
prospects, and the planned offline map viewer.

## [2.10.0] — 2026-08-07

### Added

- **See what you've built.** `iuut prospect-report` now lists the structures in each prospect
  alongside its mission state and trapped items — benches, beds, windows, lights, crates, and
  the rest, picked out of the world save and counted by type. On a fully built homestead that's
  114 structures among 1,820 world actors.

This is the read-only first step of **homestead pack-up** (moving a base between prospects).
Alongside the inventory it measures what such a move would have to reconcile — the actor-id
space, the foundation anchors and tame whitelists that link pieces together, and the terrain
tiles a base is bound to. Per the project's own rule for this feature, nothing is written until
lossless round-trip fixtures pass.

## [2.9.0] — 2026-08-07

Catalog refresh for the latest game patch (build 24590233).

### Added

- **New talents**: Carbon Fishing Rod, T2/T3/T4 Weapon Rack, and a T4 Decoration Reroute
  (2,221 → 2,226 rows). The superseded `Decorations_Reroute` is kept and marked not-live
  rather than deleted, so a save that still holds it is never misread.

### Fixed

- **Exact stash-repair durability now ships in the box.** The `ItemStaticData → DurableData`
  join has worked at runtime since v1.8.0, but the *bundled* item catalog was never
  regenerated with it — so a fresh install, before its first self-refresh, fell back to
  guessing max durability from whatever the save happened to contain. The bundled catalog
  now carries all 136 exact values.

### Changed

- `LegendaryWeapon_SlugLauncher` no longer declares a durability record in the game data
  (exact-durability coverage 137 → 136).

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
