<#
.SYNOPSIS
    Refresh IUUT catalog JSON from upstream sources (Eureka Endeavors).

.DESCRIPTION
    Authority: docs/IUUT-PROJECT-DOCUMENTATION.md §15.2; gameplan §8.

    SCAFFOLD STUB — not implemented yet.

    Intended behavior:
      1. Fetch the current D_Talents / D_ItemsStatic / D_Accolades / D_BestiaryData /
         D_MetaResources tables from icarus.eurekaendeavors.com.
      2. Normalize into the IUUT.Catalog schema:
         {
           "catalog-version": "2026-02-mendel",
           "source-url": "...",
           "fetched-at": "<ISO 8601>",
           "rows": [ { "RowName": "...", "DisplayName": "...", "MaxRank": 4, ... } ]
         }
      3. Write to catalogs/*.json (overwriting).
      4. Print a summary diff against the previous version.
      5. Exit 0 on success, non-zero on any fetch or schema-validation failure.

    Once implemented, the operator runs this manually and commits the changed
    catalogs as a `catalog-update`-labeled PR with the source build cited.

    Forward compatibility (CONSTITUTION VI): IUUT.Core round-trips unknown
    RowNames even when this catalog is stale. Refreshing is a quality-of-life
    improvement, not a safety requirement.

.PARAMETER OutputRoot
    Where to write catalog files. Defaults to ./catalogs.

.PARAMETER Catalogs
    Subset of catalogs to fetch. Defaults to all known tables.

.EXAMPLE
    pwsh -File scripts/fetch-catalogs.ps1

.EXAMPLE
    pwsh -File scripts/fetch-catalogs.ps1 -Catalogs talents,items
#>

[CmdletBinding()]
param(
    [string] $OutputRoot = "./catalogs",
    [string[]] $Catalogs = @('talents', 'items', 'accolades', 'bestiary', 'meta-resources')
)

Write-Host "================================================================" -ForegroundColor Yellow
Write-Host "  IUUT — fetch-catalogs.ps1 is a SCAFFOLD STUB." -ForegroundColor Yellow
Write-Host "================================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Not implemented. See gameplan §8 and master doc §15.2 for the"
Write-Host "  intended behavior. Implementation lands as part of Phase 0."
Write-Host ""
Write-Host "  Requested catalogs: $($Catalogs -join ', ')"
Write-Host "  Requested output:   $OutputRoot"
Write-Host ""
exit 64  # EX_USAGE — "feature not implemented"
