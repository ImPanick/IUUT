# catalogs/

Generated catalog JSON, embedded at build time into `IUUT.Catalog`.

**Authority:** `docs/IUUT-PROJECT-DOCUMENTATION.md` §15; `Icarus-Analysis.md` §11.

## What this folder is for

Output of `scripts/fetch-catalogs.ps1`. Sources are mirrored from [Eureka Endeavors](https://icarus.eurekaendeavors.com/catalog/) and normalized into the schema `IUUT.Catalog` expects.

Files land here, then `IUUT.Catalog.csproj` picks them up via `EmbeddedResource Include="..\..\catalogs\**\*.json" Link="Embedded\%(RecursiveDir)%(Filename)%(Extension)" />` (wiring lands when the first catalog is generated).

## Expected files (initial cut, per gameplan §8)

| Catalog | Source `D_*` table | Used by IUUT.Core for |
| --- | --- | --- |
| `talents.json` | `D_Talents` | Character talents, workshop unlocks, mount talents |
| `items.json` | `D_ItemsStatic` | Stash items, durability max, stack max |
| `accolades.json` | `D_Accolades` | Accolade picker, Lazy Max append-all |
| `bestiary.json` | `D_BestiaryData` | Bestiary scan groups |
| `meta-resources.json` | `D_MetaResources` | Currency display labels |

Each file has a top-level `catalog-version` field (`<YYYY-MM-build>`, currently `2026-02-mendel`).

## Update procedure

1. `pwsh -File scripts/fetch-catalogs.ps1`
2. Diff the output.
3. Commit only what changed, label the PR `catalog-update`, include the build the catalog came from.
4. Run integration tests against catalog-dependent code.

## Forward compatibility

Per CONSTITUTION VI, `IUUT.Core` round-trips unknown `RowName` / `MetaRow` values verbatim, even when not in the embedded catalog. This folder is for **display labels** and **caps**, not gatekeeping.

This is a scaffold; no catalog files have been generated yet.
