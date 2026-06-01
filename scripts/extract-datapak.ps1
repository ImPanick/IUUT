<#
.SYNOPSIS
    Crack the live Icarus data.pak and dump its gameplay DataTables to JSON (per-patch re-mine).

.DESCRIPTION
    Authority: docs/DATA-PROVENANCE.md §Sources.

    Icarus's gameplay DataTables live in `…\Icarus\Content\Data\data.pak` (~2.4 MB). Despite the
    ".pak" name it is NOT a UE pak and needs NO AES / Oodle / FModel — it is simply a run of zlib
    streams (`0x78 0x9C`), each ~64 KB of UTF-8 JSON, plus a tail directory index. This script inflates
    every block, splits the concatenated text into top-level JSON DataTable objects, and writes each out
    named by its (unique) RowStruct. Those JSON files are the source for regenerating the embedded
    catalogs in `src/IUUT.Catalog/Embedded/`.

    RowStruct -> catalog mapping (current):
      AccountFlag   -> accountflags.json (ordered names = flag ids)
      CharacterFlag -> characterflags.json
      Talent        -> talents.json  (+ Prospect_* rows -> missions.json: TalentTree, RequiredTalents)
      ItemStaticData-> items.json     (filter rows whose Generated_Tags/Manual_Tags include `Item.Meta*`)
      AccoladeData  -> accolades.json (DisplayName is NSLOCTEXT("…","…","<friendly>"))
      BestiaryData  -> bestiary.json

.PARAMETER DataPak
    Path to data.pak. Defaults to the Steam install.

.PARAMETER OutDir
    Where to write the per-table JSON. Defaults to ./artifacts/datapak.

.EXAMPLE
    pwsh -File scripts/extract-datapak.ps1
#>

[CmdletBinding()]
param(
    [string] $DataPak = "$env:ProgramFiles(x86)\Steam\steamapps\common\Icarus\Icarus\Content\Data\data.pak",
    [string] $OutDir = './artifacts/datapak'
)

$ErrorActionPreference = 'Stop'
# %ProgramFiles(x86)% doesn't expand in PowerShell strings; fall back to the known path.
if (-not (Test-Path $DataPak)) { $DataPak = "C:\Program Files (x86)\Steam\steamapps\common\Icarus\Icarus\Content\Data\data.pak" }
if (-not (Test-Path $DataPak)) { throw "data.pak not found: $DataPak" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$b = [System.IO.File]::ReadAllBytes($DataPak)
Write-Host "data.pak: $($b.Length.ToString('N0')) bytes" -ForegroundColor Cyan

# 1) Inflate every zlib block (0x78 0x9C/DA/01) in file order, concatenate.
$full = New-Object System.IO.MemoryStream
$blocks = 0
for ($i = 0; $i -lt $b.Length - 2; $i++) {
    if ($b[$i] -ne 0x78) { continue }
    if ($b[$i + 1] -ne 0x9C -and $b[$i + 1] -ne 0xDA -and $b[$i + 1] -ne 0x01) { continue }
    try {
        # offset-based stream view over the original buffer (no per-block copy)
        $ms = New-Object System.IO.MemoryStream -ArgumentList $b, ($i + 2), ($b.Length - $i - 2), $false
        $ds = New-Object System.IO.Compression.DeflateStream($ms, [System.IO.Compression.CompressionMode]::Decompress)
        $before = $full.Length
        $ds.CopyTo($full); $ds.Dispose(); $ms.Dispose()
        if ($full.Length -gt $before) { $blocks++; $i += 4 }
    } catch { }
}
$text = [System.Text.Encoding]::UTF8.GetString($full.ToArray())
Write-Host "inflated $blocks blocks -> $($text.Length.ToString('N0')) chars" -ForegroundColor Green

# 2) Split into top-level JSON objects (brace match, string/escape aware).
$objs = New-Object System.Collections.Generic.List[string]
$depth = 0; $start = -1; $inStr = $false; $esc = $false
for ($i = 0; $i -lt $text.Length; $i++) {
    $c = $text[$i]
    if ($inStr) { if ($esc) { $esc = $false } elseif ($c -eq '\') { $esc = $true } elseif ($c -eq '"') { $inStr = $false }; continue }
    if ($c -eq '"') { $inStr = $true }
    elseif ($c -eq '{') { if ($depth -eq 0) { $start = $i }; $depth++ }
    elseif ($c -eq '}') { $depth--; if ($depth -eq 0 -and $start -ge 0) { $objs.Add($text.Substring($start, $i - $start + 1)); $start = -1 } }
}
Write-Host "top-level DataTable objects: $($objs.Count)"

# 3) Write each out by its RowStruct (unique per table).
$wanted = 'AccountFlag','CharacterFlag','Talent','ItemStaticData','AccoladeData','BestiaryData','MetaCurrency','FactionMission'
foreach ($o in $objs) {
    $m = [regex]::Match($o, '"RowStruct"\s*:\s*"/Script/Icarus\.([^"]+)"')
    if (-not $m.Success) { continue }
    $rs = $m.Groups[1].Value
    if ($wanted -notcontains $rs) { continue }
    $rows = ([regex]::Matches($o, '"Name"\s*:')).Count
    $path = Join-Path $OutDir "$rs.json"
    [System.IO.File]::WriteAllText($path, $o)
    Write-Host ("  {0,-16} rows~{1,-5} -> {2}" -f $rs, $rows, $path)
}
Write-Host "`nDone. Regenerate src/IUUT.Catalog/Embedded/*.json from these per docs/DATA-PROVENANCE.md." -ForegroundColor Green
