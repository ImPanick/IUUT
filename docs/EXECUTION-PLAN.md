# IUUT EXECUTION PLAN — Path to v1.0 ("make it absolute")

> The single, ordered source of truth for finishing IUUT: every remaining item, phased, with an
> owner, its blockers, and a hard exit bar. When every phase's exit bar is met, IUUT is
> feature-complete, fully UI-surfaced, in-game-validated, and shipped as a signed/attested release.
>
> **Baseline:** `main` == `dev` == `0b457eb` (verified self-contained `IUUT.exe`, 286 Core tests green).
> Authority: `AGENTS.md`, `.agent/CONSTITUTION.md`, `.agent/DEFINITION_OF_DONE.md`.

## Legend

- **Owner** — 🤖 agent (me, conflict-free Core/docs) · 🎨 UI agent (App layer) · 🧑 you (human gate)
- **Gate** — what must be true/provided before the phase can start.
- Each item carries its task id (`#NN`) where one exists.

---

## Phase 0 — Lock the baseline 🤖🧑 *(now; conflict-free)*

**Goal:** a clean, protected, green-CI baseline so every later phase merges through gates.

- [ ] 🧑 **Branch protection on `main`** — require PR + `build.yml` + `governance-check.yml`, linear history,
      dismiss stale approvals. (I can apply via `gh api` on request.)
- [ ] 🤖 **Commit-prefix fix** — add `build` to the allowed conventional prefixes in the commit-msg hook
      (or standardize on `chore`); update `.agent/HANDOFF_PROTOCOL.md §2`.
- [ ] 🤖 **Governance-lint debt (14 items, test-only, non-shipping)** — placeholder SteamIDs
      `76561198…`/`76561190…` → `00000000000000000`; BOM-emitting `UTF8Encoding` → `new UTF8Encoding(false)`
      across ~6 test files. No production code touched.

**Exit bar:** `governance-lint.ps1` reports **0** violations; CI green on `main`; protection on.

---

## Phase 1 — Data completeness 🤖🧑 *(gate: your current `data.pak` / FModel JSON export)*

**Goal:** catalogs match the live weekly build; the missing missions/challenges are filled; item
dynamic-state is fully decoded.

- [ ] 🧑 Provide an FModel export (or confirm the IcarusData mirror is current) — `D_Talents`,
      `D_FactionMissions`, `D_Challenges`, `D_AccountFlags`, `D_CharacterFlags`, `D_ItemsStatic`, `D_Accolades`.
- [ ] 🤖 **Refresh every embedded catalog** to the current version + **lock flag indices**; update
      `accountflags.json` (label 86→93), `characterflags.json`, items, talents, accolades, bestiary. (#80)
- [ ] 🤖 **Fill the missing missions + challenges** — complete `missions.json` (faction + open-world +
      story) and add a challenges catalog; re-derive the prerequisite DAG.
- [ ] 🤖 **Decode the remaining `DynamicData` indices** — beyond 7 (stack) / 9 (durability): water, oxygen,
      ammo, fuel, etc. (Index→meaning map) for full in-prospect item-state editing.
- [ ] 🤖 Update `docs/DATA-PROVENANCE.md` (sources, version stamp, re-mine steps) + bump `GameCatalogsTests`
      asserts.

**Exit bar:** catalog counts match the live game; missions+challenges complete; `DATA-PROVENANCE.md`
re-stamped; all catalog tests assert the new counts and pass.

---

## Phase 2 — Core feature completion 🤖 *(conflict-free; can run alongside Phase 1)*

**Goal:** every edit category has a tested Core service; nothing is left half-wired below the UI.

- [ ] 🤖 **Character flags in Lazy Max** — extend `LazyMaxApplyService` to write `flags_<SteamID>.dat`
      (signature mission/recipe flags) alongside the profile bundle. (#82)
- [ ] 🤖 **Account Flags edit service (Core)** — set/clear `Profile.UnlockedFlags` by name with preview. (#81-core)
- [ ] 🤖 **Mission-completion apply service** — write the `Prospect_*` talents + signature account/character
      flags for a selected mission set, auto-including the transitive prerequisite closure
      (`MissionCatalog.AllPrerequisites`).
- [ ] 🤖 **Return-to-stash refinement** — classify player-owned vs world/loot inventories so "return" can
      target the player's trapped items (not every crate); expose stack/durability/repair in the service.
- [ ] 🤖 Tests for each (synthetic fixtures; real-data spot-checks in `%TEMP%` scratch only).

**Exit bar:** every Custom category + Lazy Max has a Core service with passing tests; `DEFINITION_OF_DONE`
met per service.

---

## Phase 3 — UI surfacing 🎨🤖 *(gate: UI agent's restyle lands on `dev`; then coordinate)*

**Goal:** every Core capability is reachable and usable in-app, in the glass-console design.

- [ ] 🤖 **DI wiring** — register `ProspectReturnService`, `ProspectReturnFileService`, mission/flag services.
- [ ] 🎨🤖 **Prospects editor: Return-to-Stash panel** — preview trapped items (friendly names + quantities)
      → multi-select → "Return to Orbital Stash" (confirm + backup), plus in-prospect set-stack / repair /
      remove / duplicate / retype.
- [ ] 🎨🤖 **Missions checklist** — checkbox list grouped by region, prereq auto-select, writes via the
      mission-completion service + Lazy Max "complete all missions" toggle. (#79)
- [ ] 🎨🤖 **Account Flags editor** — UnlockedFlags by name. (#81-ui)
- [ ] 🎨🤖 **Stash picker grouping/filter** — category facets over the 377 workshop items. (#83)
- [ ] 🤖 Manual UI smoke pass per screen (the `run`/`verify` skills).

**Exit bar:** every Custom category + Missions + Return-to-Stash is fully operable in `IUUT.exe`; no dead
buttons; design-consistent.

---

## Phase 4 — Validation 🧑🤖 *(gate: a built exe from Phase 3)*

**Goal:** prove the real game accepts every edit — the final authority beyond the programmatic hash gate.

- [ ] 🤖 Produce a validation build + a test matrix (one row per edit type: currencies, talents, accolades,
      bestiary, stash add/remove/durability, mounts, flags, missions, **prospect return-to-stash**, engine tuning).
- [ ] 🧑 **In-game validation (WP-15)** — load each edited save in Icarus; confirm it loads and behaves. (#84)
- [ ] 🤖 Fix anything the matrix surfaces; re-validate.

**Exit bar:** every matrix row signed off in-game; zero regressions; backups verified restorable.

---

## Phase 5 — Ship v1.0 🤖🧑

**Goal:** a signed, attested, documented public release.

- [ ] 🧑 **Code-signing cert** — obtain; 🤖 wire Authenticode signing into `release.yml` (kills SmartScreen).
- [ ] 🤖 **End-user docs** — `README` for the portable zip + release notes (grouped by `<type>` since last tag);
      finalize `docs/INSTALL.md`.
- [ ] 🧑 **Tag `v1.0.0`** (you tag; agents propose readiness — `HANDOFF_PROTOCOL §9`) → `release.yml` builds
      the signed `IUUT.exe` + `IUUT-portable.zip` + `SHA256SUMS.txt` + Sigstore attestation.
- [ ] 🤖 Verify `gh attestation verify` + checksums on the published artifacts.

**Exit bar:** a GitHub Release with a signed, attested `IUUT.exe` whose provenance verifies.

---

## Dependency gates (who unblocks what)

| Phase | Blocked by | Unblocks |
| --- | --- | --- |
| 0 | — | clean CI for all merges |
| 1 | 🧑 data.pak / FModel export | accurate Missions UI (P3), accurate Lazy Max (P2) |
| 2 | — (parallel with P1) | full UI surfacing (P3) |
| 3 | 🎨 restyle landing on `dev`; P1+P2 done | validation (P4) |
| 4 | 🧑 in-game testing; P3 exe | ship (P5) |
| 5 | 🧑 signing cert + tag | — |

**Parallelism:** P0 and P2 start now (conflict-free, mine). P1 starts the moment you drop the data export.
P3 waits for the UI restyle to land, then is co-owned. P4/P5 are human-gated.

## Definition of "absolute" (whole-project DoD)

1. Every Custom-editor category + Lazy Max + Game Tuner + Recovery + **prospect return-to-stash** works
   end-to-end in the shipped `IUUT.exe`.
2. Catalogs match the live game; missions + challenges complete; data provenance re-stampable per patch.
3. Every edit type validated in-game; every write is atomic + backed up + restorable.
4. `governance-lint` clean; CI green; `main` protected.
5. A signed, Sigstore-attested `v1.0.0` release whose provenance + checksums verify.
