# FEATURE ROADMAP — beyond the save editor

> Candidate future features that grow IUUT from a save editor/repair tool toward the
> **offline "Ultimate" utility** its name implies: a single, offline-first companion that
> bundles the things players currently get from a half-dozen separate community sites.
> **Documentation only — vision + constraints + phasing. No code, and nothing here is
> committed-to until the open decisions in §4 are resolved by the owner.**

| | |
| --- | --- |
| **Status** | Roadmap / future scope (post-v1.0). Tracked here + master §20. |
| **Authority** | `.agent/CONSTITUTION.md` (esp. **V** network, **VII** privacy), `.agent/SECURITY_PROTOCOL.md` (deps/data licensing), master §6.4 (footprint), §2.3 (what IUUT is not) |
| **Hard gate** | Every item below is **blocked on §4** (network + licensing + footprint + positioning). Do not start any of these until those are decided. |

---

## 1. The vision — an offline superset

Today there's a constellation of community Icarus tools, each online and separate: interactive
maps (e.g. **icarusintel.com** — caves/missions/POIs), talent/workshop planners, loadout
calculators, recipe/tech-tree references, prospect planners. IUUT could be the **offline,
single-app** home for the most useful of these, sitting alongside the save editor — so a player
on a plane, a LAN, or a flaky connection still has everything.

The model that fits IUUT's constraints: ship/curate **versioned, offline "data packs"** and
render them locally. IUUT itself stays offline-first (CONSTITUTION V) — it does **not** phone a
content server at runtime. See §4 for why that framing is mandatory, not optional.

---

## 2. Candidate features

| Feature | What it is | Data source | Notes / risk |
| --- | --- | --- | --- |
| **Intel Maps (offline)** ⭐ | Interactive per-map view (Olympus/Styx/Prometheus/…) with toggleable layers — caves, missions, bosses, resources, POIs — and tap-for-notes (deep-dive §3). | Map imagery + curated POIs (see §3, §4 — the licensing crux). | Flagship; biggest data + legal lift. |
| **Talent / Workshop planner** | Plan character talent trees + account workshop unlocks offline; export to a Custom-mode edit. | Our **embedded `D_Talents` catalog** (already shipped, WP-11). | Low risk — first-party data we already have; natural extension of the Custom editor. |
| **Loadout / drop calculator** | Pre-plan an envirosuit + meta-item loadout, weight/insurance budget; export to `Loadouts.json`. | `D_ItemsStatic` (catalog, partial — enrich first). | Builds on the parked Stash UI. |
| **Recipe / tech-tree reference** | Searchable crafting recipes + tech unlock tree. | Game `D_*` data tables (first-party extraction). | First-party; license-clean. |
| **Prospect planner** | Browse prospect types, difficulty, rewards; plan a run. | `D_*` prospect tables. | First-party. |

**Observation:** the features whose data is **first-party** (talent/workshop/recipe/prospect —
all from the game's own `D_*` tables, which we already extract for catalogs) are *low-risk* and
align with what IUUT already does. **Intel Maps is the outlier** — its value (cave/POI locations)
is *community-collected*, not in a clean game data table, which is exactly where the licensing and
sourcing problems live (§4).

---

## 3. Deep-dive — Intel Maps (offline)

**Screen.** An "Intel Maps" entry on the Glass Console: a map selector (per playable map) + a
legend/layer-toggle panel (caves, missions, bosses, resources, POIs), a pannable/zoomable map
canvas, marker tap → detail/notes, and **local-only** user annotations stored on-device (in the
one IUUT data folder — CONSTITUTION VII: never leaves the machine).

**Rendering.** A standard offline raster-map stack (tiles or a single large image per map) with a
marker overlay; markers positioned by the game's coordinate→pixel projection (reproduced once per
map). No external tile server — all imagery is in the local data pack.

**Data-pack model (the constitution-safe shape).**
- A **versioned offline pack**: `{ packVersion, gameVersion, maps: [{ id, image(s)/tiles, projection, layers: [{ type, points:[{id, x, y, label, meta}] }] }] }`.
- The pack is **bundled with the app and/or sideloaded by the user** (they download a pack file
  and point IUUT at it). IUUT reads it from disk. **IUUT performs no content fetch itself.**
- Pack version is shown in-app (mirroring icarusintel's "data vX.Y.Z, game version, week N") so the
  user knows the patch level.

**Updating.** "When online, get a newer pack" is the natural ask — but in-app fetching is a
**network call IUUT may not make** (CONSTITUTION V). Options, owner's call (§4): (a) user manually
downloads packs from a webpage and sideloads — fully compliant, ships today; (b) amend
CONSTITUTION V to permit a single, IUUT-owned, content-only pack endpoint (no telemetry, no PII) —
a real governance decision, not a default.

---

## 4. Hard constraints & open decisions (resolve before any build)

These are the reason this is a roadmap, not a work package. Each needs an explicit owner decision.

1. **Network (CONSTITUTION V).** IUUT's binding contract: the Steam Web API is the **only**
   permitted outbound call. A "sync map data" feature is, by default, **forbidden**. → Default
   design: **bundled + user-sideloaded data packs, zero in-app fetch.** In-app pack syncing
   requires a **CONSTITUTION amendment** (`.agent/AMENDMENT_PROCESS.md`) — flag, don't assume.

2. **Licensing / IP (the icarusintel question).** icarusintel.com is **crowd-sourced, contributor-
   curated, "not affiliated with RocketWerkz,"** with no obvious open license. **Scraping their
   endpoints and redistributing their cave/POI dataset is their contributors' work, not ours to
   take.** SECURITY_PROTOCOL §8 (license review) extends to *data assets*, not just code. The
   only acceptable sourcing paths:
   - **First-party extraction** — derive map imagery + POIs from the game's own files / data
     tables where feasible (license-clean; but cave locations specifically are hard — they live in
     level/world data, which is why the community hand-collected them).
   - **Permissioned reuse** — ask the maintainers (their Discord), get explicit permission, ship
     with **prominent attribution** and offer improvements back. The Perplexity transcript that
     seeded this idea *itself* flagged this as the required step.
   - **Embed/link, don't copy** — link out to / embed their site for online users.
   - **Do NOT** mirror-and-redistribute without one of the above. (This also protects IUUT's own
     "clean, no-shady-data" positioning.)

3. **Footprint (master §6.4).** IUUT is a ~15–25 MB single-file, no-install, one-folder exe. Map
   imagery (raster tiles across many maps) can be **large** and does **not** belong inside the
   single-file binary. → Data packs are **separate, optional, sideloaded assets** in the IUUT data
   folder — never inflating the core download. Keep the base app the same lean exe.

4. **Positioning / scope (master §2.3).** Today IUUT *is* "a save editor + repair + utility for
   *your own* save." Maps/planners push it toward "general Icarus companion." That's a legitimate
   growth direction given the name — but it's a **deliberate scope expansion** the owner should
   ratify (and decide whether it's v2 territory, a separate sibling app, or a plugin), not a thing
   that accretes by default.

---

## 5. Phasing plan (only after §4 is resolved; post-v1.0)

Core-first, offline-first, license-clean. Mirrors the GAME-TUNING phasing style.

| WP | Deliverable | Notes |
| --- | --- | --- |
| **FR-1** | **Data-pack schema + loader** (read a versioned offline pack from disk; show pack/game version). No network. | §3; CONSTITUTION V |
| **FR-2** | **First-party catalog tools** (talent/workshop planner, recipe/prospect reference) from existing `D_*` data — the *low-risk* wins, do these first. | §2; reuses WP-11 catalogs |
| **FR-3** | **Map sourcing decision + pipeline** — implement whichever §4.2 path the owner approved (first-party extraction *or* permissioned import w/ attribution). License + provenance recorded. | §4.2 — **gated** |
| **FR-4** | **Offline map renderer** (raster + marker overlay + projection) — no external tiles. | §3 |
| **FR-5** | **Intel Maps screen** (selector, layer toggles, marker detail, local-only annotations) on the Glass Console. | §3; UI pass |
| **FR-6** | *(amendment-gated, optional)* in-app pack updater — **only** if CONSTITUTION V is amended for a content-only endpoint. | §4.1 |

FR-1/FR-2/FR-4 are testable Core; FR-3 is the legal/sourcing gate; FR-5 is UI (with the parked
UI pass); FR-6 is governance-gated and may never happen.

---

## 6. Recommendation (for the owner's decision)

- **Do the first-party tools first** (FR-2): talent/workshop/recipe planners reuse data we already
  ship, carry no licensing risk, and strengthen the editor — high value, low risk.
- **Treat Intel Maps as a separate, gated track**: decide the **sourcing path (§4.2)** and the
  **network posture (§4.1)** *before* committing engineering. The clean default is *permissioned,
  attributed, sideloaded, offline* — or first-party extraction if it proves feasible.
- **Keep the base app lean**: maps ship as optional data packs, never inside the single-file exe.
