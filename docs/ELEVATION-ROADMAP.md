# IUUT Elevation Roadmap — to the *actual* Ultimate Icarus Utility Tool

> Produced 2026-07-24 (v1.6.0, catalog Week 242) from a five-track audit: codebase gaps,
> per-editor UX review, data-leverage analysis (257 game DataTables vs the 8 we mine),
> community-demand research, and architecture. Owner-prioritized; tiers below are ordered.
> Governance: everything here is CONSTITUTION-V-clean (offline; the *absence* of network is
> the moat — "your save never leaves your machine" is a claim the upload/paywall portals
> structurally cannot make).

## Vision

IUUT becomes the **offline, free, always-current rescue-and-repair suite for Icarus** — the
local answer to the upload-and-paywall portals. The community's dominant pain is disaster
recovery (items trapped in inaccessible prospects, dead mounts, unresumable saves), not
cheating — and IUUT already owns the ecosystem's technical moat: a tested, write-capable
lossless round-trip of the prospect world blob that no maintained local tool has finished.
Core is far ahead of the UI; elevation means (1) surface the finished Core stacks, (2) end
the weekly catalog treadmill by mining the user's own data.pak at runtime, (3) harden the
editing UX against silent data loss, then (4) spend the blob moat on the signature features
competitors paywall.

## Tier 0 — Quick wins (one sitting each)

| Item | Impact / Effort | Notes |
| --- | --- | --- |
| **True per-talent max ranks** | transformative / small | `Talent.Rewards.Count` in the mined data = the real max. The blind 0–4 clamp over-ranks ~86% of 2,211 talents (65.6% max at rank 1). Bake `maxRank` into talents.json at mine time; clamp per-row; Lazy Max becomes exact. |
| **BUG: unify save root** | high / small | The path browsed on Home never reaches Custom/Recovery/Game Tuner — non-default Steam-library users get "No save profiles found". Shared `SaveRootState` singleton in DI. |
| **BUG: clobbered apply status + lost selection** | high / small | Every editor's post-apply reload overwrites "Applied — a backup was taken"; Stash/Flags also drop selection after each staged op. Fix all 7 ApplyAsync paths. |
| Account Flags editor UI (#81) | medium / small | `AccountFlagEditService` is tested but never registered in DI and has no sidebar category. |
| Stash picker filter (first slice of #83) | medium / small | 381-item add picker has no search box. FilterText + ICollectionView. |
| Truth pass | low / small | "Advanced / Raw: Coming." on a shipped editor; "visual grid coming" on the shipped grid; stale CHANGELOG; hardcoded-green Home dots; empty Presets folder. |
| extract-datapak hardening | low / small | RowStruct collision (duplicate ProcessorRecipe silently overwrites) + emit `_inventory.csv` each run for free new-table detection. |
| winget manifest | medium / small | Free SmartScreen/mark-of-the-web mitigation, zero app changes. |

## Tier 1 — Core elevation (foundation that raises every screen)

**Status: shipped in v1.8.0** — self-refresh (miner + merge rules + startup wiring + CLI),
search/virtualization (`FilteredView<T>`), dirty guard (`IDirtyEditor`), prospects catalog.
Remaining: `ItemableData` tooltips + mounts catalog (folded into Tier 2 polish); signing
stays owner-gated (procurement + CICD §8 amendment).

| Item | Impact / Effort | Notes |
| --- | --- | --- |
| **Runtime catalog self-refresh** | transformative / medium | Port `extract-datapak.ps1` to a C# `DataPakMiner` (BCL DeflateStream + JsonDocument, ~1–2s) + `DataPakLocator` (Steam path, libraryfolders.vdf, override). Catalogs regenerate from the *user's own* game; embedded JSON becomes fallback with sanity gates (talents≥1000, flags≥86, `Mission_Olympus_Unlock` present). Ends the weekly release treadmill. **Never auto-regenerate metaresources.json** (curated whitelist). |
| **Search/filter + virtualization everywhere** | transformative / medium | Talents (2,221 rows/character), accolades (447), blueprint checklist, flags — all scroll-only, non-virtualized. FilterText + ICollectionView + virtualizing ListBox. |
| Dirty tracking + editor-switch guard | high / medium | Switching category/profile silently discards staged edits; Raw editor loses hand-typed JSON. `IDirtyEditor` + confirm-before-swap. Ship early — this is a data-loss trust issue. |
| Catalog enrichment | high / medium | `DurableData` (exact max durability — repair is currently a guess), `ItemableData` (weight/stack/description → stash tooltips), prospects catalog (friendly names for Prospects/Loadouts UIs), mounts catalog. |
| CLI buildout | high / medium | `iuut check / backup-all / restore / lazy-max / catalog-refresh / prospect-report`; every service already tested; hand-rolled verb dispatch (dependency gate per SCOPE_GUARDRAILS §2.6). |
| Authenticode signing | high / small | Azure Trusted Signing (~$10/mo) + one release.yml step. SmartScreen reputation is per-file-hash: weekly unsigned exes = permanent scare screens. Needs owner procurement + a CICD §8 amendment (CI secret). |

## Tier 1.5 — Divine Elevation (owner-named UI/UX overhaul + platform prep)

**Status: shipped in v2.0.0** — token set (+ staged role), all five primitives
(FilteredListBox, state trio, confirm-with-diff, staged/busy chips, long-op surface),
intent-grouped sidebar with T2 homes pre-placed, Characters card/detail, Home catalog
provenance. Remaining polish rides along with Tier 2 screens as they land (full Recovery
three-step re-layout, Prospects/Mounts card layouts, deeper keyboard pass).

A dedicated visual/interaction overhaul between the foundation and the signature features —
and explicitly **prep work for Tiers 2–3**: the overhaul must produce the primitives those
features will live in, not just restyle what exists.

| Item | Notes |
| --- | --- |
| **Design system pass** | Consolidate the glass theme into a token set (spacing/type/color/elevation roles) every view consumes; kill per-view one-off styles. |
| **Reusable primitives** | The components later tiers need, built once: `FilteredListBox` (search + virtualization — Tier 1's filters migrate into it), a card/detail layout, an empty/error/loading state trio, a confirm-with-diff dialog shell (Return-to-Stash and mission reset will need it), a progress/long-operation surface (blob operations, backup snapshots). |
| **Navigation + IA rework** | The Custom sidebar is a flat 11-item list; regroup by intent (Rescue / Progression / World / Advanced) so Tier-2 panels (Return-to-Stash, Backup manager, Missions) land in obvious homes. |
| **Dirty/busy/undo affordances** | Visual language for staged-vs-applied state (pairs with Tier 1's dirty tracking). |
| **Accessibility + polish** | Keyboard flow, focus visuals, tooltips, consistent iconography. |

**Standing rule for Tier 1 (prep-aware building):** anything Tier 1 adds that Tier 1.5 will
restyle must be built as a *reusable* piece, not copy-paste per view — e.g. the
search/filter work ships as one control/behavior, the miner reports progress through the
long-operation surface's interface, new views use the token styles only.

## Tier 2 — Signature features (surface the finished-but-invisible Core)

| Item | Impact / Effort | Notes |
| --- | --- | --- |
| **Return-to-Stash + in-prospect editing panel** | transformative / medium | The headline rescue feature is **fully built and tested in Core** (`ProspectWorldEditor`, `SlotOwner`, `ProspectReturnService`, `ProspectReturnFileService` — stash-saved-first ordering) but is **not registered in DI and has zero UI**. ~3 lines of DI + the EXECUTION-PLAN Phase 3 panel. |
| Missions checklist UI + Lazy Max hook (#79) | high / medium | `MissionCompletionService` (prereq closure, idempotent, tested) + missions.json (145 rows, tree grouping) done; no VM/DI/category. Lazy Max never grants `Prospect_*` talents today. |
| Backup snapshot / rollback manager | high / medium | Icarus has no Steam Cloud; community advice is hand-copying `Saved\`. Timestamped snapshots, one-click restore, autosave browser, "rebuild character from backup" flow. |
| Mount rescue suite | high / medium | Proven demand ("Mount Reviver Needed!"). Slices: roster restore/clone → in-prospect rename (UeNode locator + StrProperty write, both proven) → level/XP (needs recorder field decode). |
| Loadout recovery | high / small | The community hand-edit for gear stuck with an offline host is flipping `bInsured` — players fear corrupting it by hand. Guided flip + dangling-GUID cleanup; needs `SaveLoadoutsAsync` (serializer exists). |
| Prospect diagnosis + repair | high / medium | "failed to resume prospect – 020" threads have no tool. Header editor (promised in the sidebar, never built), `IsWorldBlobIntact` surfacing (never called from App), host reassignment. |

## Tier 3 — Long bets (spend the blob moat; research tracks with read-only milestones first)

| Item | Impact / Effort | Notes |
| --- | --- | --- |
| Mission/quest reset inside prospect saves | high / large | Decode `IcarusQuestManagerRecorderComponent`; read-only CLI report first, then gated writes. The category leader's flagship paid feature, offline and free. |
| **Homestead pack-up / base relocation** | transformative / large | Extract deployable/container actor subtrees from one prospect blob and inject into another. Paywalled at ~$4/28-days elsewhere. Full actor-graph read/write; do not announce until round-trip fixtures pass. |
| Offline map + deployable viewer | medium / large | Plot containers/deployables from the local blob over terrain maps; click a crate → see contents → jump to Return-to-Stash. The privacy-preserving alternative to upload-based cartographers. |

## Standing risks

- **Blob writes are the highest-stakes ops in the app.** Every new write path keeps the
  `Serialize(forceReconstruct:true)` equality gate, `ProspectBlobVerifier` round-trip,
  stash-saved-first ordering, and automatic backups — no exceptions for "small" edits.
- Catalog self-refresh must fail safe to embedded catalogs (sanity gates; tolerate Steam
  mid-update partial writes).
- Ship the dirty-tracking guard early — silent edit loss undercuts the safety pitch.
- Signing requires a CI secret → CICD §8 amendment + owner identity procurement (lead time).
- Dedicated-server support is Article-II-gated: acquire + scrub real server fixtures first.
- Doc truth debt: every tier item updates its spec sections in the same PR (Article I).
