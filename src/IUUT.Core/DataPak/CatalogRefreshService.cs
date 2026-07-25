using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IUUT.Core.DataPak;

/// <summary>The outcome of a runtime catalog refresh: per-file JSON when it passed the sanity gates.</summary>
public sealed record CatalogRefreshResult(
    bool Ok,
    IReadOnlyDictionary<string, string> UpdatedCatalogs,
    IReadOnlyList<string> Report);

/// <summary>
/// Turns a <see cref="DataPakMiner"/> mine of the user's own game into refreshed catalog JSON — the
/// in-app codification of the weekly merge rules that were previously applied by hand (elevation
/// roadmap Tier 1, "runtime catalog self-refresh"):
/// <list type="bullet">
/// <item>talents.json is a SUPERSET: new live rows are added (display name from NSLOCTEXT, maxRank
/// from the Rewards count), vanished rows are kept and marked <c>live:false</c>, returned rows go
/// live again; curated display names are never overwritten.</item>
/// <item>items.json: rows tagged <c>Item.Meta</c>/<c>Item.Meta.*</c> are added; existing curated rows
/// are never removed; exact <c>maxDurability</c> is baked from the ItemStaticData→DurableData join.</item>
/// <item>flag catalogs are ORDER-SENSITIVE (index = id): only pure appends are accepted — any
/// pre-tail divergence fails the whole refresh (a shifted id would mis-toggle flags).</item>
/// <item>accolades/bestiary: set-additive. missions.json: regenerated from the <c>Prospect_*</c>
/// rows (fully derived data). metaresources.json is a curated whitelist and is NEVER touched.</item>
/// </list>
/// Failing any sanity gate fails the whole refresh — callers keep their current catalogs
/// (embedded fallback), so a partial Steam write or format change can never corrupt the app.
/// </summary>
public static class CatalogRefreshService
{
    private static readonly Regex _nsLocText = new(
        "NSLOCTEXT\\(\\s*\"(?:[^\"\\\\]|\\\\.)*\"\\s*,\\s*\"(?:[^\"\\\\]|\\\\.)*\"\\s*,\\s*\"((?:[^\"\\\\]|\\\\.)*)\"\\s*\\)",
        RegexOptions.Compiled);

    /// <summary>
    /// Runs the refresh. <paramref name="currentCatalogJson"/> resolves a catalog file name
    /// (e.g. <c>talents.json</c>) to its current JSON; <paramref name="versionStamp"/> becomes the
    /// new <c>catalogVersion</c> (callers stamp it from the pak identity, keeping this deterministic).
    /// </summary>
    public static CatalogRefreshResult Refresh(
        IReadOnlyList<MinedTable> mined,
        Func<string, string> currentCatalogJson,
        string versionStamp)
    {
        ArgumentNullException.ThrowIfNull(mined);
        ArgumentNullException.ThrowIfNull(currentCatalogJson);
        ArgumentException.ThrowIfNullOrEmpty(versionStamp);

        var report = new List<string>();
        var updated = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var tables = mined
                .GroupBy(t => t.RowStruct, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            updated["talents.json"] = RefreshTalents(Rows(tables, "Talent"), currentCatalogJson("talents.json"), versionStamp, report);
            updated["items.json"] = RefreshItems(Rows(tables, "ItemStaticData"), Rows(tables, "DurableData"), currentCatalogJson("items.json"), versionStamp, report);
            updated["accountflags.json"] = RefreshFlags(Rows(tables, "AccountFlag"), currentCatalogJson("accountflags.json"), versionStamp, "accountflags", minimum: 86, report);
            updated["characterflags.json"] = RefreshFlags(Rows(tables, "CharacterFlag"), currentCatalogJson("characterflags.json"), versionStamp, "characterflags", minimum: 40, report);
            updated["accolades.json"] = RefreshSetCatalog(Rows(tables, "AccoladeData"), currentCatalogJson("accolades.json"), versionStamp, "accolades", minimum: 400, report);
            updated["bestiary.json"] = RefreshSetCatalog(Rows(tables, "BestiaryData"), currentCatalogJson("bestiary.json"), versionStamp, "bestiary", minimum: 100, report);
            updated["missions.json"] = RefreshMissions(Rows(tables, "Talent"), currentCatalogJson("missions.json"), versionStamp, report);
            // metaresources.json: curated whitelist — deliberately never regenerated (DATA-PROVENANCE.md).

            return new CatalogRefreshResult(true, updated, report);
        }
        catch (CatalogRefreshException ex)
        {
            report.Add($"REFRESH REJECTED: {ex.Message} — keeping current catalogs.");
            return new CatalogRefreshResult(false, new Dictionary<string, string>(), report);
        }
    }

    private sealed class CatalogRefreshException(string message) : Exception(message);

    private static List<JsonElement> Rows(Dictionary<string, MinedTable> tables, string rowStruct)
    {
        if (!tables.TryGetValue(rowStruct, out var table))
        {
            throw new CatalogRefreshException($"mined data has no {rowStruct} table");
        }

        using var doc = JsonDocument.Parse(table.Json);
        if (!doc.RootElement.TryGetProperty("Rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
        {
            throw new CatalogRefreshException($"{rowStruct} table has no Rows array");
        }

        return rows.EnumerateArray().Select(r => r.Clone()).ToList();
    }

    private static string? Name(JsonElement row) =>
        row.TryGetProperty("Name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;

    private static string? DisplayName(JsonElement row)
    {
        if (!row.TryGetProperty("DisplayName", out var d) || d.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var match = _nsLocText.Match(d.GetString() ?? "");
        return match.Success && match.Groups[1].Value.Length > 0 ? match.Groups[1].Value : null;
    }

    // ---- talents (superset: live flag + maxRank) -----------------------------

    private static string RefreshTalents(List<JsonElement> live, string currentJson, string stamp, List<string> report)
    {
        var liveByName = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var row in live)
        {
            if (Name(row) is { } n)
            {
                liveByName.TryAdd(n, row);
            }
        }

        using var current = JsonDocument.Parse(currentJson);
        var rows = new List<(string RowName, string? Display, bool Live, int? MaxRank)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var added = 0;
        var dropped = 0;
        var returned = 0;

        foreach (var row in current.RootElement.GetProperty("rows").EnumerateArray())
        {
            var rowName = row.GetProperty("rowName").GetString()!;
            seen.Add(rowName);
            var curated = row.TryGetProperty("displayName", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
            var wasLive = !row.TryGetProperty("live", out var l) || l.ValueKind != JsonValueKind.False;

            if (liveByName.TryGetValue(rowName, out var liveRow))
            {
                if (!wasLive)
                {
                    returned++;
                }

                rows.Add((rowName, curated ?? DisplayName(liveRow), true, MaxRank(liveRow)));
            }
            else
            {
                if (wasLive)
                {
                    dropped++;
                }

                var priorMax = row.TryGetProperty("maxRank", out var m) && m.ValueKind == JsonValueKind.Number ? m.GetInt32() : (int?)null;
                rows.Add((rowName, curated, false, priorMax));
            }
        }

        foreach (var (name, row) in liveByName)
        {
            if (seen.Add(name))
            {
                added++;
                rows.Add((name, DisplayName(row), true, MaxRank(row)));
            }
        }

        var liveCount = rows.Count(r => r.Live);
        if (liveCount < 1000)
        {
            throw new CatalogRefreshException($"talents sanity: only {liveCount} live rows (expected ≥ 1000)");
        }

        if (liveCount != liveByName.Count)
        {
            throw new CatalogRefreshException($"talents merge invariant broke: live {liveCount} != mined {liveByName.Count}");
        }

        report.Add($"talents: {rows.Count} rows ({liveCount} live) — +{added} new, {dropped} newly not-live, {returned} returned");
        return WriteCatalog(stamp, current.RootElement, header =>
        {
            header.Append("  \"dataTable\": \"D_Talents\",\n");
        }, "rows", rows.Select(r =>
        {
            var sb = new StringBuilder();
            sb.Append("    {\n      \"rowName\": ").Append(JsonSerializer.Serialize(r.RowName));
            sb.Append(",\n      \"displayName\": ").Append(r.Display is null ? "null" : JsonSerializer.Serialize(r.Display));
            if (!r.Live)
            {
                sb.Append(",\n      \"live\": false");
            }

            if (r.MaxRank is { } max)
            {
                sb.Append(",\n      \"maxRank\": ").Append(max);
            }

            sb.Append("\n    }");
            return sb.ToString();
        }));

        static int? MaxRank(JsonElement row) =>
            row.TryGetProperty("Rewards", out var rewards) && rewards.ValueKind == JsonValueKind.Array && rewards.GetArrayLength() >= 1
                ? rewards.GetArrayLength()
                : null;
    }

    // ---- items (curated superset + Item.Meta additions + exact maxDurability) ----

    private static string RefreshItems(List<JsonElement> liveItems, List<JsonElement> durables, string currentJson, string stamp, List<string> report)
    {
        var metaNames = new List<string>();
        var durableRefByItem = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in liveItems)
        {
            if (Name(row) is not { } name)
            {
                continue;
            }

            if (IsMeta(row))
            {
                metaNames.Add(name);
            }

            if (row.TryGetProperty("Durable", out var durable) && durable.ValueKind == JsonValueKind.Object &&
                durable.TryGetProperty("RowName", out var dr) && dr.ValueKind == JsonValueKind.String &&
                dr.GetString() is { Length: > 0 } durableRow && durableRow != "None")
            {
                durableRefByItem[name] = durableRow;
            }
        }

        if (metaNames.Count < 300)
        {
            throw new CatalogRefreshException($"items sanity: only {metaNames.Count} Item.Meta rows (expected ≥ 300)");
        }

        // DurableData: durable RowName -> exact max durability.
        var maxByDurable = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in durables)
        {
            if (Name(row) is { } n &&
                row.TryGetProperty("Max_Durability", out var max) && max.ValueKind == JsonValueKind.Number)
            {
                maxByDurable[n] = max.GetInt32();
            }
        }

        int? MaxDurabilityOf(string itemRow) =>
            durableRefByItem.TryGetValue(itemRow, out var durableRow) && maxByDurable.TryGetValue(durableRow, out var max) && max > 0
                ? max
                : null;

        using var current = JsonDocument.Parse(currentJson);
        var rows = new List<(string RowName, string? Display, int? MaxDurability)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in current.RootElement.GetProperty("rows").EnumerateArray())
        {
            var rowName = row.GetProperty("rowName").GetString()!;
            seen.Add(rowName);
            var curated = row.TryGetProperty("displayName", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
            rows.Add((rowName, curated, MaxDurabilityOf(rowName)));
        }

        var added = 0;
        foreach (var name in metaNames)
        {
            if (seen.Add(name))
            {
                added++;
                rows.Add((name, null, MaxDurabilityOf(name)));
            }
        }

        var withDurability = rows.Count(r => r.MaxDurability is not null);
        report.Add($"items: {rows.Count} rows — +{added} new Item.Meta, {withDurability} with exact maxDurability");
        return WriteCatalog(stamp, current.RootElement, null, "rows", rows.Select(r =>
        {
            var sb = new StringBuilder();
            sb.Append("    {\n      \"rowName\": ").Append(JsonSerializer.Serialize(r.RowName));
            sb.Append(",\n      \"displayName\": ").Append(r.Display is null ? "null" : JsonSerializer.Serialize(r.Display));
            if (r.MaxDurability is { } max)
            {
                sb.Append(",\n      \"maxDurability\": ").Append(max);
            }

            sb.Append("\n    }");
            return sb.ToString();
        }));

        static bool IsMeta(JsonElement row)
        {
            foreach (var tagsProperty in new[] { "Generated_Tags", "Manual_Tags" })
            {
                if (row.TryGetProperty(tagsProperty, out var tags) && tags.ValueKind == JsonValueKind.Object &&
                    tags.TryGetProperty("GameplayTags", out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    foreach (var tag in list.EnumerateArray())
                    {
                        if (tag.TryGetProperty("TagName", out var tn) && tn.ValueKind == JsonValueKind.String &&
                            tn.GetString() is { } t && (t == "Item.Meta" || t.StartsWith("Item.Meta.", StringComparison.Ordinal)))
                        {
                            return true;
                        }
                    }

                    return false; // Generated_Tags present and no meta tag — authoritative.
                }
            }

            return false;
        }
    }

    // ---- flags (order-sensitive: pure append only) ---------------------------

    private static string RefreshFlags(List<JsonElement> live, string currentJson, string stamp, string label, int minimum, List<string> report)
    {
        var liveNames = live.Select(Name).Where(n => n is not null).Select(n => n!).ToList();
        using var current = JsonDocument.Parse(currentJson);
        var currentNames = current.RootElement.GetProperty("names").EnumerateArray().Select(e => e.GetString()!).ToList();

        if (liveNames.Count < minimum)
        {
            throw new CatalogRefreshException($"{label} sanity: only {liveNames.Count} rows (expected ≥ {minimum})");
        }

        for (var i = 0; i < currentNames.Count; i++)
        {
            if (i >= liveNames.Count || !string.Equals(liveNames[i], currentNames[i], StringComparison.Ordinal))
            {
                // A shifted flag id would silently mis-toggle unlocks — reject the whole refresh.
                throw new CatalogRefreshException($"{label} ORDER DIVERGENCE at index {i} (id shift) — manual review required");
            }
        }

        report.Add($"{label}: {liveNames.Count} names (+{liveNames.Count - currentNames.Count} appended)");
        return WriteCatalog(stamp, current.RootElement, header =>
        {
            var rowStruct = current.RootElement.TryGetProperty("rowStruct", out var rs) ? rs.GetString() : null;
            if (rowStruct is not null)
            {
                header.Append("  \"rowStruct\": ").Append(JsonSerializer.Serialize(rowStruct)).Append(",\n");
            }
        }, "names", liveNames.Select(n => "    " + JsonSerializer.Serialize(n)));
    }

    // ---- accolades / bestiary (set-additive) ---------------------------------

    private static string RefreshSetCatalog(List<JsonElement> live, string currentJson, string stamp, string label, int minimum, List<string> report)
    {
        var liveByName = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var row in live)
        {
            if (Name(row) is { } n)
            {
                liveByName.TryAdd(n, row);
            }
        }

        if (liveByName.Count < minimum)
        {
            throw new CatalogRefreshException($"{label} sanity: only {liveByName.Count} rows (expected ≥ {minimum})");
        }

        using var current = JsonDocument.Parse(currentJson);
        var rows = new List<(string RowName, string? Display)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in current.RootElement.GetProperty("rows").EnumerateArray())
        {
            var rowName = row.GetProperty("rowName").GetString()!;
            seen.Add(rowName);
            var curated = row.TryGetProperty("displayName", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
            rows.Add((rowName, curated ?? (liveByName.TryGetValue(rowName, out var lr) ? DisplayName(lr) : null)));
        }

        var added = 0;
        foreach (var (name, row) in liveByName)
        {
            if (seen.Add(name))
            {
                added++;
                rows.Add((name, DisplayName(row)));
            }
        }

        report.Add($"{label}: {rows.Count} rows (+{added} new)");
        var dataTable = current.RootElement.TryGetProperty("dataTable", out var dt) ? dt.GetString() : null;
        return WriteCatalog(stamp, current.RootElement, header =>
        {
            if (dataTable is not null)
            {
                header.Append("  \"dataTable\": ").Append(JsonSerializer.Serialize(dataTable)).Append(",\n");
            }
        }, "rows", rows.Select(r =>
            "    {\n      \"rowName\": " + JsonSerializer.Serialize(r.RowName) +
            ",\n      \"displayName\": " + (r.Display is null ? "null" : JsonSerializer.Serialize(r.Display)) + "\n    }"));
    }

    // ---- missions (fully derived from Prospect_* talent rows) ----------------

    private static string RefreshMissions(List<JsonElement> liveTalents, string currentJson, string stamp, List<string> report)
    {
        var missions = new List<(string RowName, string Tree, List<string> Requires, bool DefaultUnlocked)>();
        foreach (var row in liveTalents)
        {
            if (Name(row) is not { } name || !name.StartsWith("Prospect_", StringComparison.Ordinal))
            {
                continue;
            }

            var tree = row.TryGetProperty("TalentTree", out var tt) && tt.ValueKind == JsonValueKind.Object &&
                       tt.TryGetProperty("RowName", out var tr) && tr.ValueKind == JsonValueKind.String
                ? tr.GetString() ?? ""
                : "";
            var requires = new List<string>();
            if (row.TryGetProperty("RequiredTalents", out var req) && req.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in req.EnumerateArray())
                {
                    if (r.TryGetProperty("RowName", out var rn) && rn.ValueKind == JsonValueKind.String &&
                        rn.GetString() is { Length: > 0 } required && required != "None")
                    {
                        requires.Add(required);
                    }
                }
            }

            var defaultUnlocked = row.TryGetProperty("bDefaultUnlocked", out var du) && du.ValueKind == JsonValueKind.True;
            missions.Add((name, tree, requires, defaultUnlocked));
        }

        if (missions.Count < 140)
        {
            throw new CatalogRefreshException($"missions sanity: only {missions.Count} Prospect_* rows (expected ≥ 140)");
        }

        report.Add($"missions: {missions.Count} rows (regenerated from Prospect_* talents)");
        using var current = JsonDocument.Parse(currentJson);
        return WriteCatalog(stamp, current.RootElement, null, "missions", missions.Select(m =>
        {
            var sb = new StringBuilder();
            sb.Append("    {\n      \"rowName\": ").Append(JsonSerializer.Serialize(m.RowName));
            sb.Append(",\n      \"tree\": ").Append(JsonSerializer.Serialize(m.Tree));
            sb.Append(",\n      \"requires\": ");
            if (m.Requires.Count == 0)
            {
                sb.Append("[]");
            }
            else
            {
                sb.Append("[\n").Append(string.Join(",\n", m.Requires.Select(r => "        " + JsonSerializer.Serialize(r)))).Append("\n      ]");
            }

            sb.Append(",\n      \"defaultUnlocked\": ").Append(m.DefaultUnlocked ? "true" : "false");
            sb.Append("\n    }");
            return sb.ToString();
        }));
    }

    // ---- shared writer (matches the embedded 2-space style) ------------------

    private static string WriteCatalog(
        string stamp,
        JsonElement current,
        Action<StringBuilder>? extraHeader,
        string arrayName,
        IEnumerable<string> entries)
    {
        var source = current.TryGetProperty("source", out var s) && s.ValueKind == JsonValueKind.String
            ? s.GetString()
            : "Runtime self-refresh from the local game data.pak (DATA-PROVENANCE.md).";

        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"catalogVersion\": ").Append(JsonSerializer.Serialize(stamp)).Append(",\n");
        extraHeader?.Invoke(sb);
        sb.Append("  \"source\": ").Append(JsonSerializer.Serialize(source)).Append(",\n");
        sb.Append("  \"").Append(arrayName).Append("\": [\n");
        sb.Append(string.Join(",\n", entries));
        sb.Append("\n  ]\n}");
        return sb.ToString();
    }
}
