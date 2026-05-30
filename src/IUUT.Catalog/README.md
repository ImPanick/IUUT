# IUUT.Catalog

Embedded D_* game data tables.

**Authority:** `docs/IUUT-PROJECT-DOCUMENTATION.md` §15; `.agent/CODE_STYLE.md` §1.

## What lives here

Catalog JSON derived from [Eureka Endeavors](https://icarus.eurekaendeavors.com/catalog/) and embedded as assembly resources for `IUUT.Core` to read at runtime.

| Catalog | Source table | Used by |
| --- | --- | --- |
| `talents.json` | `D_Talents` | character talents, workshop unlocks, mount talents |
| `items.json` | `D_ItemsStatic` | stash items, durability max, stack max |
| `accolades.json` | `D_Accolades` | accolade picker |
| `bestiary.json` | `D_BestiaryData` | creature groups |
| `meta-resources.json` | `D_MetaResources` | currency labels |

## Version stamp

Each catalog file has a `catalog-version: <YYYY-MM-build>` header. The current target is `2026-02-mendel` (DataVersion 4).

## Forward compatibility

Per CONSTITUTION VI, `IUUT.Core` round-trips unknown `RowName` / `MetaRow` values even when not present here. This catalog is for **display** and **lookup**, not gatekeeping.

## Updating

Run `pwsh -File scripts/fetch-catalogs.ps1` to refresh from Eureka Endeavors. Diff the output, commit only what changed, label the PR `catalog-update`.

This is a scaffold; the `Embedded/` directory currently contains no files. First catalog populated per master doc §16 Phase 0.
