# DATA PROVENANCE — where IUUT's game data comes from, and how to re-map on updates

> **Why this file exists.** IUUT ships *game* data (item/talent/accolade/flag/mission tables) as
> embedded catalogs so the app stays offline (CONSTITUTION V). That data is re-derived from the
> game's own DataTables — it must be **re-mined per game version**. Icarus ships **weekly patches**;
> in practice these *add* rows (new mounts, items, missions), they don't usually renumber existing
> ones — but flag ids are **0-based row indices**, so a re-order *can* shift them. This doc says
> exactly where each catalog lives, what its keys mean, and how to refresh it.

## The catalogs (all in `src/IUUT.Catalog/Embedded/`, embedded by the `Embedded\**\*.json` glob)

| File | Game table | Key → value | Loader | Notes |
| --- | --- | --- | --- | --- |
| `talents.json` | `D_Talents` | `rowName` → `displayName` | `CatalogLoader` | Character talents + the 456 `Workshop_*`/`Prospect_*` unlock rows. Display names mostly null → humanized. |
| `items.json` | `D_ItemsStatic` | `rowName` → `displayName` | `CatalogLoader` | **Complete itemable set (2,993 rows, dev/test excluded)** — the Stash "Add item" picker, so every blueprint-crafted item is addable. 90 curated display names; the rest humanize. |
| `accolades.json` | `D_Accolades` | `rowName` → `displayName` | `CatalogLoader` | 446 rows (complete). |
| `bestiary.json` | `D_BestiaryData` | `rowName` → `displayName` | `CatalogLoader` | 78 creature groups. |
| `metaresources.json` | `D_MetaResources` | `rowName` → `displayName` | `CatalogLoader` | 7 currencies. |
| `accountflags.json` | `D_AccountFlags` | **array index = flag id** → name | `FlagCatalogLoader` | Backs `Profile.json` `UnlockedFlags`. **86-row Week-149 snapshot** — a maxed save already has ids 86/93 beyond it (re-mine to label). |
| `characterflags.json` | `D_CharacterFlags` | **array index = flag id** → name | `FlagCatalogLoader` | Backs `flags_<SteamID>.dat`. 40 rows. id 27 = `Mission_Olympus_Unlock` for the current shipped version (matches the reference save). |
| `missions.json` | `D_Talents` (`Prospect_*` rows) | `rowName` → `{tree, requires[], defaultUnlocked}` | `MissionCatalogLoader` | The **mission prerequisite DAG**. 145 missions, 140 with prereqs, 5 region trees. |

## How "mission completion" is stored (the important one)

There is **no single "missions completed" file**. Completion is split across three mechanisms:

1. **`Prospect_*` talent in `Profile.json` Talents (the universal one).** A *mission* = its `Prospect_*`
   reward talent; **complete = the player owns that talent**. This is what `missions.json` models.
   The **prerequisite graph** is `D_Talents.RequiredTalents` on each `Prospect_*` row (a verified DAG;
   AND-gates exist, e.g. `Riverlands ← Glacier AND Canyon`). → `MissionCatalog.AllPrerequisites()`
   returns the transitive closure, so checking a mission must also complete its ancestors.
2. **Account flags (`Profile.json` `UnlockedFlags`) — story/boss milestones.** e.g. id 8
   `GrantedTalent_Olympus_Nightfall`, 9 `GrantedTalent_Styx_Ironclad`, 6 `GrantedTalent_Nullsector_Story`,
   0–2 `Mission_Communicator_T2/3/4_Upgrade`, 3–4 `*_Level_Boost_Claimed`. Lazy Max already sets these
   (`LazyMaxService.MaxAccountMissionFlags`); `FlagCatalog.IsMissionFlag` classifies them.
3. **Character flags (`flags_<SteamID>.dat`) — signature item/recipe unlocks.** The `Mission_*` rows in
   `D_CharacterFlags` (e.g. `Mission_Olympus_Unlock` the map gate, `Mission_Sandworm_Items`). The Engine
   Flags editor's "Complete missions" sets these.

⚠️ **The which-mission-sets-which-flag binding for (2)/(3) is NOT in any DataTable** — the game sets
those flags via quest scripts. So the ~12 character + ~6 account "signature" flags are matched by
**name heuristic** (`FlagCatalog.IsMissionFlag`: `Mission_*`, `*Story*`, Nightfall/Ironclad, `_Unlock`,
`Level_Boost`). If you ever want a precise mission→flag map, it must be hand-curated.

## Humanized / friendly names — where they live

- **Curated display names** are baked into the catalog JSON (`displayName`) for items (90) and
  accolades (446), sourced from **icarusdatabase.com** (page slugs / icon filenames derive from the
  internal RowNames → asset-confirmed) + `icarus.fandom.com`.
- **Everything else** (talents, workshop/prospect, flags, missions, bestiary) falls back to
  **`CatalogName.Humanize(rowName)`** — a pure offline transform (underscores→spaces, camelCase / digit
  splits). So labels are always readable even without a curated name.
- **The game's own UI display strings** live in **StringTables** (`StringTables/ST_*.csv` in the data
  pak — e.g. `ST_Prospect.csv`, `ST_Quests.csv`). These are mostly UI chrome, not 1:1 mission titles;
  the authoritative mission display name is `D_FactionMissions.Name`. Pull from there for a future
  pass if curated mission titles are wanted.

## Sources (in order of authority)

1. **The game's data pak** — `…\steamapps\common\Icarus\Icarus\Content\Paks\` (cooked, AES). The owner
   also has a **`Data/data.pak`** (~2.3 MB, Icarus's *custom* data-pak format — NOT a UE pak/zip, not
   crackable without the Icarus mod tooling). This is ground truth for the current version.
2. **Data-mine mirror** — `github.com/GODOFMINECRAFT4/IcarusData` (branch **`master`**), e.g.
   `…/master/Talents/D_Talents.json`, `…/master/Flags/D_AccountFlags.json`. What the current catalogs
   were generated from. May lag the live game by a version.
3. **`icarus.eurekaendeavors.com/catalog/`** — browsable data catalog (cross-check).

## How to re-mine on a game update

1. Extract the current DataTables to JSON (FModel on the Paks folder with the Icarus AES key, **or**
   pull the table from the IcarusData mirror's `master`).
2. Regenerate the affected `Embedded/*.json`:
   - Flags: array of `Rows[].Name` in order → `names[]` (index = id).
   - Missions: `Prospect_*` rows → `{rowName, tree=TalentTree.RowName, requires=RequiredTalents[].RowName, defaultUnlocked=bDefaultUnlocked}`.
   - Items/Talents/Accolades/Bestiary: `Rows[].Name` → row with curated `displayName` where known.
3. Re-run `dotnet test` — `GameCatalogsTests` asserts the key counts + spot-checks (e.g. flag id 27,
   `Mission_Olympus_Unlock`; mission `Forest_Scan ← Forest_Exploration`). Update those asserts if the
   game genuinely renumbered.

## Related assets the owner provided (for future features)

- **`Data.zip` → `Json/Prebuilt/*.json`** (~260 MB): the **prospect world-base layouts** (ELY story
  bases, Great Hunt arenas, NPC camps, bunkers). These are the world structures a future
  **"return trapped items from a prospect to the orbital stash"** feature would have to parse — that's
  a world-blob reverse-engineering project (the items live in the zlib `ProspectBlob`), the hardest
  item on the roadmap.
- A **~45 GB set of 32 `.pak` files** is the full cooked content (textures/meshes/etc.) — not needed
  for save editing; the small `data.pak` + the mirror cover the DataTables.
