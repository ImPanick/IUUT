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
| **WP-7** | `GameProcessDetector` (`Icarus-Win64-Shipping.exe`) | Master §14; warn-only. |
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
