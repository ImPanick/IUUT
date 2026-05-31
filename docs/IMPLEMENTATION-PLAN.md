# IMPLEMENTATION PLAN — from scaffold to v1.0

> The actionable build plan. It turns the master doc §16 roadmap into
> dependency-ordered, PR-sized **work packages (WPs)**, says exactly where to
> start, and defines what "final product" means. Every WP is executed through the
> governance ritual (`.agent/AGENT_WORKFLOW.md`) on a branch off `dev`.

| | |
| --- | --- |
| **Audience** | Implementing agents + humans |
| **Authority** | `docs/IUUT-PROJECT-DOCUMENTATION.md` §16, §6.2, §9; `.agent/*` |
| **Status** | Active — implementation starts here |

---

## 0. Resume point (live build status)

> Updated **2026-05-31** (context-compress). Cold-start handoff: a new agent or a compacted
> session resumes from here. Per-WP detail lives in `git log` on `dev`; this block is the
> current state, the rules, and the next task. Overwrite it at the end of each work chunk.

**Branch / remote:** everything commits to `dev` and pushes to `origin/dev` (owner-authorized,
no branch protection). The commit-msg hook **requires** the three trailers: `Agent:`,
`Consulted:` (must include `.agent/CONSTITUTION.md`), `Co-Authored-By:`.

**STATE — all save-editing Core done + tested (230 tests); UI pass underway.**
- **Core (Phases 0–5, `src/IUUT.Core/`, each subsystem tested):** discovery · Steam-name resolve ·
  game detect · `ValidationEngine` · app-state; the four-file model + parsers/serializers; catalogs;
  **Lazy Max** + `LazyMaxApplyService` (Preview→Apply, atomic via `ISafeSaveWriter`); **Broken Save
  Recovery** (`Recovery/`: `BackupChainWalker`/`RecoveryPlanner`/`RecoveryService` + `RecoveryAdvisor`;
  master Appendix E); **Custom edit services** (`Editing/`: `CustomApplyService`, `AccountEditService`,
  `CharacterEditService`, `AccoladeBestiaryEditService`, `StashEditService`, `MountEditService`,
  `FlagsEditService`, `ProspectEditService`, `LoadoutCrossReference`); the stash/loadout/prospect/
  mount models + `ProspectBlobCodec` (zlib+Adler-32) + `FlagsFileCodec` (82-byte binary); **Game
  Tuning** (`GameTuning/`: `EngineIni` codec + `GameTuningCatalog` w/ stable-max caps + service).
- **UI (`src/IUUT.App/`, Glass Console on WPF-UI `FluentWindow`):** navigation **shell**
  (`ShellViewModel` + `INavigationService`, in-window page swap; pages resolved by key from DI;
  rendered by implicit DataTemplate VM→View); **Home** (4 tiles, all navigate); **Recovery** (wired);
  **Custom editor** (`CustomViewModel`/`CustomView` — profile selector + category sidebar →
  `CurrentEditor` panel swapped by implicit DataTemplate, via `CustomApplyService.LoadAsync`/
  `PreviewBundleAsync`): **Account & Currencies WIRED** (per-currency edit + "Max all" + "Unlock all
  blueprints", `AccountEditService`); **Characters & Talents WIRED** (character picker → name/XP/debt/
  dead/abandoned + per-talent rank slider + "Max XP"/"Max talents", `CharacterEditService`);
  **Accolades & Bestiary WIRED** (catalog accolade checklist + grant/revoke all; creature-group scan
  points + "Max all", `AccoladeBestiaryEditService`); other categories show a placeholder editor
  pending their UI); **Game Tuner** (own tile, fully wired:
  toggles + slider/number-box clamped to stable-max → Engine.ini). DI uses **`ValidateOnBuild`**
  (whole graph validated at startup). Visual QA is **owner-run** (smoke-launch confirms render; the
  harness can't screenshot a WPF GUI).

**NEXT: wire the remaining Custom categories** to their already-built Core services, following the
Account / Characters / Accolades vertical (load → edit → confirm → `PreviewBundleAsync` →
`ApplyAsync`). Remaining: Orbital Stash (`StashEditService` — needs the deferred `items.json`
enrichment for a friendly picker), Loadouts, Prospects, Mounts, Engine Flags, Advanced/Raw. The
lighter ones (Mounts, Engine Flags) are good single passes. Canonical plan below.

**Parked (owner, 2026-05-31):**
- **WP-15 (v0.1 MVP manual test) — PARKED.** Folded into one big bulk in-game test later;
  not run now. Don't block Phase 2 on it.
- **Polish Home — PARKED to backlog.** The Glass Console Home is a good **template** but
  that's all it is for now: replace emoji with line-based sci-fi icons, embed the OFL display
  font for the wordmark, add real per-tile `BlurEffect`, subtle gradient drift. Revisit after
  the Core features land.
- **Troubleshooting / FAQ modal — PARKED to backlog (owner, 2026-05-31).** An in-app
  FAQ-style help modal that guides the user through fixes, sourced from **master Appendix E**
  (corruption causes → remedies) + live `RecoveryAdvisor` advisories. Goes in with the UI pass,
  next to the (also-parked) WPF Recovery screen — both render the recovery Core we already built.
- **Feature roadmap — `docs/FEATURE-ROADMAP.md` (owner, 2026-05-31).** Vision of IUUT as an
  offline community-tools superset (Intel Maps + talent/workshop/recipe/loadout/prospect planners,
  master §20.5). **Gated** on four owner decisions (network/CONSTITUTION V, data licensing,
  footprint, positioning) — no WP until resolved. Recommendation: build the low-risk *first-party*
  planners (catalog data we already ship) first; Intel Maps is a separate, legally-gated track.

**Custom editor wiring — canonical plan (current work).** Give each Custom category real content.
- **Pattern (mirror the shell page-swap):** `CustomViewModel` exposes `CurrentEditor` (an editor VM)
  that swaps when `SelectedCategory` / `SelectedSlot` changes; `CustomView`'s content region binds
  `CurrentEditor` and renders it by implicit DataTemplate (one per editor VM → its View). Unwired
  categories use a shared placeholder editor VM. `CustomViewModel` injects the edit services +
  `CustomApplyService` and builds the matching editor VM, passing the selected save folder.
- **Load/apply (extend `CustomApplyService`, `IUUT.Core/Editing/`):** add
  `LoadAsync(folder) → SaveEditBundle?` (parse the 4 files; null on missing/unparseable) and
  `PreviewBundleAsync(folder, bundle) → SaveEditPlan` (re-read original from disk as the canonical
  "before", diff vs the edited bundle's serialization, run `ValidationEngine`). Existing
  `ApplyAsync(plan)` then writes only changed files via `ISafeSaveWriter`. Keep
  `PreviewAsync(folder, edit)` for delegate-style edits. Add tests for both.
- **Editor VM shape:** load bundle → bind/edit the relevant model in place → confirm (View
  code-behind `MessageBox`, VM stays WPF-free) → `PreviewBundleAsync` → `ApplyAsync`. Category →
  service map: Account→`AccountEditService`; Characters→`CharacterEditService`; Accolades/Bestiary→
  `AccoladeBestiaryEditService`; Stash→`StashEditService` (+ item picker needs the deferred
  `items.json` enrichment); Loadouts→`LoadoutCrossReference`; Prospects→`ProspectEditService`;
  Mounts→`MountEditService`; Engine Flags→`FlagsEditService`/`FlagsFileCodec`; Advanced/Raw→raw JSON.
- **Order:** **Account & Currencies first** (proves the editor→preview→apply vertical), then the
  others one category or a batch at a time. Game Tuning is its OWN Home tile now — not a Custom category.

**Polish backlog (owner-noted, screenshots 2026-05-31 — note, don't act mid-feature):** the Recovery
header **title+subtitle overlap** (two `TextBlock`s in one grid cell → split into rows); a general
spacing/contrast/typography pass; Home polish (line icons, embedded display font, per-tile blur).

**Parked decisions (owner notes, 2026-05-31):**
- **UI direction chosen — "Glass Console" (WP-U1 done).** The owner pulled UI forward
  (reversing the Phase-6 deferral). Design system: `docs/UI-DESIGN-CONCEPT.md` (frosted
  glass, dark orbit background, amber + ion-cyan accents, frameless custom chrome); live
  HTML mockup: `docs/ui-concept/iuut-glass-console-mockup.html`. **Build route = WPF-UI**
  (lepoco, **MIT**, v4.3.0) for `FluentWindow` custom chrome + themed dark controls; our
  `Theme/GlassTheme.xaml` layers the tokens/glass on top. All logic stays in Core/VMs, so
  screens are *born styled* as their feature WPs land. Visual QA is **owner-run** (WPF GUI —
  the harness can't screenshot it; a build-time smoke launch confirms it renders).
- **"Engine Mods / Buff FPS" is a tracked future feature** (post-v1.0, master §20.1):
  on/off toggle cards that merge/remove owned `[/script/...]` cvar fragments in
  `Engine.ini`, with duplicate-cvar de-dup, seeded from a user-run live-client cvar dump
  (`scripts/dump-cvars.ps1`), backed by its own INI backup/atomic/rollback flow. No WP
  allocated yet; do not start until the v1 save-editing scope is complete.

**The ritual (every WP, before commit):** `dotnet build -c Release` (0 warn — warnings
are errors) → `dotnet test` → `dotnet format --verify-no-changes` (exit 0) →
`pwsh scripts/governance-lint.ps1 -StagedOnly` → commit with the three trailers
(`Agent:` / `Consulted:` / `Co-Authored-By:`, enforced by the commit-msg hook) →
`git push origin dev`.

**Hard-won gotchas (each cost a red build this session):**
- `dotnet build` does **not** enforce IDE1006 naming; `dotnet format` does. Private
  `const` → PascalCase; private `static readonly` → `_camelCase` (CODE_STYLE §2).
- XML-doc `<see cref>` to an overloaded method → CS0419 (→ error). Use `Method()`.
- `.csproj` XML comments cannot contain `--` (MSB4025).
- Model classes mirror game JSON keys verbatim (underscores fine — CA1707 exempted for
  `src/IUUT.Core/Models/*.cs` in `.editorconfig`). Services are CA1822-exempt. SHA-1
  interop hashing needs a `CA5350` pragma + justification (game format, not security).
- Embedded-resource file names must avoid `-` (use `metaresources.json`).
- New `IUUT.Core` subsystem folders need a CA1822 `.editorconfig` exemption (Services/Recovery/
  Editing/GameTuning are exempted — stateless methods on DI singletons).
- **WPF (`IUUT.App`):** `System.IO` is NOT in this project's implicit usings (System.Linq IS);
  `Window.GetWindow` is nullable → use owner-less `MessageBox.Show`/`OpenFolderDialog.ShowDialog()`;
  a View hosted by a DataTemplate has a parameterless ctor and reaches its VM via `DataContext`
  (don't inject it); a stub VM property must touch instance state (use an auto-property, not an
  expression-bodied constant — else CA1822); use the canonical pack URI for merged
  `ResourceDictionary` Source. **Build lock:** a running `IUUT.exe` (e.g. an owner smoke-test
  instance) locks the output exe → `Get-Process IUUT | Stop-Process -Force` before rebuilding. DI
  uses `ValidateOnBuild` so a broken graph fails at startup (the smoke-launch catches it).

**Data / environment:** SDK is .NET 9.x building `net8.0` (`global.json`
`rollForward: latestMajor`). Catalogs seeded from a real save (`items.json` partial — only
owned items; enrich via `scripts/fetch-catalogs.ps1` before the Phase 4 stash UI).
**Never commit real save data / Steam IDs / persona names / personal paths** (CONSTITUTION
VII). The reference save was analyzed only under `%TEMP%\iuut-scratch` (outside the repo);
fixtures in the repo are anonymized.

**Test doubles available:** `TempDir`, `FixedClock`, `Fixtures`, `ProspectBlobFactory`,
plus fakes (`FakeSafeSaveWriter`, `FakeLocalSteamNames`, `FakeRunningProcesses`) — reuse them.

---

## 1. The shape of the work

```
IUUT.App (WPF)  ─┐
IUUT.Cli        ─┼──▶  IUUT.Core  ──▶  IUUT.Catalog
                 │      (all logic)     (embedded D_* data)
                 └──▶  (zero UI deps in Core)
```

Everything rests on **two non-negotiable primitives**, built first:

1. **Safe save I/O** — `backup → write → re-parse → restore-on-failure` (CONSTITUTION III).
   No mutator ships until this exists and is tested.
2. **Round-trippable models** — `[JsonExtensionData]` preserves unknown fields
   (CONSTITUTION VI). No model ships without an unknown-field round-trip test.

Build order principle (from field guide §12): **cheapest, highest-leverage, fully
unit-testable logic first; UI last.** We earn a runnable, game-verified MVP before
breadth.

---

## 2. WHERE WE START

> **WP-0 then WP-1.** Pure `IUUT.Core` logic, no UI. This exercises the entire
> governance + test machine (fixtures, round-trip tests, backup/restore, DoD) on
> the simplest file, and unblocks everything else.

- **WP-0 — Fixtures.** Anonymize a real `Profile.json` (+ `Characters.json`) into
  `fixtures/profiles/` and `fixtures/characters/` per `.agent/SECURITY_PROTOCOL.md` §3.
  Nothing can be tested without these. (This is the first thing the next agent does.)
- **WP-1 — Core safety spine.** `IClock`, `IGuidProvider`, an `IFileSystem` seam;
  `SafeSaveWriter` (the backup→write→reparse→restore protocol); `BackupManager`
  (`<File>.iuut-backup-<ts>`); shared UTF-8-**no-BOM** `JsonSerializerOptions`
  (tabs, CRLF, relaxed escaping).
- **WP-2 — `Profile.json` end to end.** Model (+extension data) → parser → serializer
  → tests (round-trip, unknown-field, malformed, encoding). The template every later
  model copies.

After WP-2, the first `dotnet test` proves the whole pipeline on real (anonymized) data.

---

## 3. Work packages (dependency-ordered)

Legend: **[CP]** = on the critical path to the v0.1 MVP. Deps in parentheses.

### Phase 0 — Foundation
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-0** [CP] | Anonymized fixtures (Profile, Characters, Accolades, Bestiary) | Gate for all parser tests. SECURITY_PROTOCOL §3. |
| **WP-1** [CP] | Core safety spine: `IClock`/`IGuidProvider`/`IFileSystem`, `SafeSaveWriter`, `BackupManager`, JSON options | CONSTITUTION III, VI; CODE_STYLE §5,§9,§10. |
| **WP-2** [CP] | `Profile.json` model + parser + serializer + tests | Field guide §3. The reference implementation. |
| **WP-3** [CP] | `Characters.json` (nested-stringified) + `NestedStringifiedConverter<T>` + snapshot test | Field guide §4; TESTING_CONTRACT snapshot. |
| **WP-4** | `Accolades.json` + `BestiaryData.json` models/parsers | Field guide; simple flat JSON. |
| **WP-5** [CP] | `SaveDiscoveryService` (enumerate `PlayerData\`, metadata) + manual path override | Master §7.1; auto-link then Browse fallback. |
| **WP-6** | `SteamProfileResolverService` (cache → `loginusers.vdf` → Web API → fallback) | Master §7.5.1. The **only** network code; offline-first. |
| **WP-7** | `GameProcessDetector` — pattern match (name starts `Icarus` + contains `Shipping`; the exe carries version+expansion, e.g. `Icarus-3.0.12.152317-Shipping-DangerousHorizons`) | Master §14; warn-only. |
| **WP-8** | `HealthScanService` (parse all + prospect SHA-1) | Master §11.3. |
| **WP-9** | `ValidationEngine` (hard fails §13.1 / soft warns §13.2) | Gates every write. |
| **WP-10** | App-state/footprint plumbing: `%AppData%\IUUT\` + `IUUT.portable` + `DOTNET_BUNDLE_EXTRACT_BASE_DIR`; settings store | Master §6.4. |

### Phase 1 — Home + Lazy Max → **v0.1 MVP**
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-11** | Catalog loaders + first embedded catalogs (`talents/items/accolades/bestiary/meta-resources`) | Master §15; `fetch-catalogs.ps1`. |
| **WP-12** [CP] | `LazyMaxService` (Characters talents-union+Genetics, XP≥80M, debt=0, revive; Profile max+workshop; Accolades append; Bestiary max) | Master §12.2; port `icarus_max.ps1`. Relies on the documented clamp behavior. |
| **WP-13** [CP] | WPF Home shell: 3 cards, profile dropdown (PersonaName), health + game-state banners | Master §10.2; CODE_STYLE §7 (MVVM). |
| **WP-14** [CP] | Preview-diff → Apply pipeline (backup → validate → write → re-parse → report) | Master §13.3. |
| **WP-15** [CP] | **Manual acceptance**: Main-Menu test on a real save (clamp matches field guide §10) → tag `v0.1.0` | MANUAL_CHECKLIST §3,§4,§9. |

### Phase 2 — Broken Save Recovery → **v0.2**
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-16** | Backup-chain walker (glob `<File>.*backup*`, rank parse+mtime, prospect second-newest, `.iuut-backup` fallback for no-rotation files) | Master §12.1. |
| **WP-17** | Template repair + salvage merge + partial-recovery flag | Master §11.3. |
| **WP-18** | Recovery report UI + full-folder backup zip | Master §10.1. |

### Phase 3 — Custom core → **v0.3**
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-19** | Custom shell + category nav + per-category Preview→Apply | Master §10.3. |
| **WP-20** | Account & Currencies (MetaResources, UnlockedFlags, workshop checklist) | Master §11.6. |
| **WP-21** | Characters & Talents (XP/debt/toggles, talent tree from catalog, bulk max) | Master §11.5. |
| **WP-22** | Accolades & Bestiary UI | Master §11.7. |

### Phase 4 — Orbital Stash → **v0.4**
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-23** | `MetaInventory.json` model/parser + GUID generation | Field guide §5. |
| **WP-24** | Stash grid UI (durability, repair/replace/add/remove, loadout-GUID warnings) | Master §10.4. |
| **WP-25** | `Loadouts.json` model + cross-reference | Field guide §6. |

### Phase 5 — Prospects & Mounts
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-26** | `AssociatedProspects_Slot_N` model + unstick | Field guide §7. |
| **WP-27** | Prospect header editor (`ProspectInfo`) — no blob mutation | Master §8.9. |
| **WP-28** | `ProspectBlobCodec`: decode/verify (`ZLibStream`, SHA-1) + re-encode (`78 9C` + raw deflate + big-endian Adler-32) + round-trip tests | Field guide §8.1; TESTING_CONTRACT §6. |
| **WP-29** | `Mounts.json` JSON-field editor | Field guide §9.2. |
| **WP-30** | `flags_*.dat` binary editor (82-byte layout) | Field guide §9.1. |

### Phase 6 — Polish & release → **v1.0**
| WP | Deliverable | Notes |
| --- | --- | --- |
| **WP-31** | Settings UI (paths, DPAPI API key, cache TTL, Steam Cloud note) | Master §7.5.1; SECURITY_PROTOCOL §5. |
| **WP-32** | Advanced/Raw viewer + export/import | Master §11.10. |
| **WP-33** | Release hardening: `release.yml` end-to-end, SHA256SUMS + attestation, portable zip, INSTALL verification, full MANUAL_CHECKLIST | Master §19; docs/CICD.md §5. |
| **WP-34** | **Tag `v1.0.0`** + GitHub Release | Public launch. |

### Phase 7 — Game Tuning (Engine Mods) → **post-v1.0**
Engine.ini console-variable toggles ("Buff FPS", fog/volumetric/quality/net tuning). **Full
spec: `docs/GAME-TUNING.md`** (NG3 / master §20.1). Documentation only so far — no code. Core
(GT-1..GT-4) needs no game; GT-5 is UI; GT-6 is operator-run validation against a live client.
| WP | Deliverable | Notes |
| --- | --- | --- |
| **GT-1** | `EngineIni` reader/writer (sections/keys, preserve unknown lines+comments, atomic backup-and-write) | GAME-TUNING §6; CONSTITUTION III |
| **GT-2** | Embedded tuning catalog (vetted cards → owned `[ConsoleVariables]`/`[/Script/...]` fragments) + `scripts/dump-cvars.ps1` | GAME-TUNING §4,§5,§7 |
| **GT-3** | Toggle engine (merge/remove owned fragments, shared-section append, duplicate-cvar de-dup, reconstruct state) | GAME-TUNING §3 |
| **GT-4** | INI validation/lint + "Reset Game Tuning" restore | GAME-TUNING §6 |
| **GT-5** | Game Tuning UI tab (toggle cards) — with the parked UI pass | GAME-TUNING §8; UI-DESIGN-CONCEPT |
| **GT-6** | Manual validation on a live build (cvar dump → mark Verified) | GAME-TUNING §7 |

---

## 4. Critical path (the spine to a runnable product)

```
WP-0 ─▶ WP-1 ─▶ WP-2 ─▶ WP-3 ─▶ WP-5 ─▶ WP-12 ─▶ WP-13 ─▶ WP-14 ─▶ WP-15
fixtures  safe   Profile  Chars   discovery LazyMax  Home     Apply    MVP test
          I/O                                                          = v0.1.0
```

Everything else (WP-4, 6–11, 16–34) branches off this spine and can largely proceed
in parallel once the primitive it needs exists.

---

## 5. Multi-agent parallelization

Lock by spec section, not by file (`.agent/HANDOFF_PROTOCOL.md` §4). Safe concurrency windows:

- **After WP-1 lands:** WP-2, WP-4, WP-7 are independent (different files, same primitives).
- **After WP-2 lands:** WP-3 (its converter is reused by WP-26), WP-6 (resolver, fully independent), WP-8/WP-9 can proceed.
- **UI WPs (13, 19, 24…)** depend on their Core services but are independent of each other once the Custom shell (WP-19) exists.
- **WP-28 (blob codec)** is self-contained and can be built anytime after WP-1 — good candidate for an agent working in isolation.

Avoid: two agents on the same file; cross-cutting `IUUT.Core` API changes while feature WPs are open (tag `cross-cutting`, land first).

---

## 6. How each WP is executed (the loop)

1. `git switch dev && git pull`
2. `git switch -c agent/<agent>/wp-NN-<slug>`
3. Plan (auto-accept if in-scope & < 200 lines, else surface) — `.agent/AGENT_WORKFLOW.md` §3.
4. Implement in small trailer-carrying commits.
5. Tests to the bar in `.agent/DEFINITION_OF_DONE.md` (§4 parsers, §5 mutators, §6 blob, §7 UI).
6. PR **into `dev`**, template filled; Governance Check + Build & Test green; review; merge.
7. Periodically `dev → main`; tag releases on `main` (triggers `release.yml`).

---

## 7. Definition of "final product" (v1.0 exit criteria)

- [ ] All three home presets work end to end: **Broken Save Recovery**, **Lazy Max**, **Custom**.
- [ ] Every Custom category editable per master §10.3 (account, characters, talents, accolades, bestiary, stash, loadouts, prospects index + worlds header, mounts, flags, advanced).
- [ ] Orbital Stash visual grid with repair/replace/add/remove + loadout-GUID safety.
- [ ] Prospect header edits + blob verify; mounts JSON edits; `flags_*.dat` edits.
- [ ] Save safety holds everywhere: backup → write → re-parse → restore; round-trip tests green; unknown fields preserved.
- [ ] Steam name resolution (offline-first) + game-state banner + health scan.
- [ ] Signed, attested single-file release (`IUUT.exe` + `SHA256SUMS.txt` + provenance), portable mode, INSTALL verification steps pass.
- [ ] Full `tests/MANUAL_CHECKLIST.md` pass, including **game-load acceptance on a real save**.
- [ ] No telemetry / network beyond the optional Steam name lookup (CONSTITUTION V).

When every box is checked and `v1.0.0` is tagged and released, IUUT is done for v1.

---

## 8. Immediate next action

**Start WP-0 + WP-1 on `dev`.** Branch `agent/claude/wp-0-fixtures` (or `wp-1-core-safety`),
anonymize the first fixtures, build the safety spine, then WP-2 (`Profile.json`). Say the
word and that branch opens.

---

*Maintained per `.agent/AMENDMENT_PROCESS.md` §4. Last updated: 2026-05-25.*
