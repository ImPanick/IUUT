using System.Text;
using System.Text.Json;
using FluentAssertions;
using IUUT.Core.DataPak;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the runtime catalog self-refresh merge rules against synthetic mined tables — the
/// in-app codification of the weekly hand-merge (superset talents, Item.Meta additions, exact
/// maxDurability join, order-sensitive flags, sanity-gated failure).
/// </summary>
public class CatalogRefreshServiceTests
{
    // ---- synthetic mined tables (volumes chosen to clear the sanity gates) ----

    private static readonly string[] _durableRows = ["{\"Name\": \"Durable_Sword\", \"Max_Durability\": 750}"];
    private static readonly string[] _injectedFlag = ["Injected_First"];

    private static MinedTable Table(string rowStruct, IEnumerable<string> rowJson) =>
        new(rowStruct, $"{{\"RowStruct\": \"/Script/Icarus.{rowStruct}\", \"Rows\": [{string.Join(",", rowJson)}]}}", rowJson.Count());

    private static string TalentRow(string name, string? display = null, int rewards = 0)
    {
        var json = "{\"Name\": \"" + name + "\"";
        if (display is not null)
        {
            json += ", \"DisplayName\": \"NSLOCTEXT(\\\"D_Talents\\\", \\\"" + name + "-DisplayName\\\", \\\"" + display + "\\\")\"";
        }

        if (name.StartsWith("Prospect_", StringComparison.Ordinal))
        {
            json += ", \"TalentTree\": {\"RowName\": \"Prospect_Olympus\"}, \"RequiredTalents\": [{\"RowName\": \"Prospect_Root\"}]";
        }

        if (rewards > 0)
        {
            json += ", \"Rewards\": [" + string.Join(",", Enumerable.Repeat("{}", rewards)) + "]";
        }

        return json + "}";
    }

    private static string ItemRow(string name, bool meta, string? durable = null)
    {
        var tag = meta ? "{\"TagName\": \"Item.Meta\"}" : "{\"TagName\": \"Item.Resource\"}";
        var json = "{\"Name\": \"" + name + "\", \"Generated_Tags\": {\"GameplayTags\": [" + tag + "]}";
        if (durable is not null)
        {
            json += ", \"Durable\": {\"RowName\": \"" + durable + "\"}";
        }

        return json + "}";
    }

    private static string NameRow(string name) => $"{{\"Name\": \"{name}\"}}";

    private static string ProspectRow(string name, string? dropName = null)
    {
        var json = "{\"Name\": \"" + name + "\"";
        if (dropName is not null)
        {
            json += ", \"DropName\": \"NSLOCTEXT(\\\"D_ProspectList\\\", \\\"" + name + "-DropName\\\", \\\"" + dropName + "\\\")\"";
        }

        return json + "}";
    }

    private static List<MinedTable> DefaultMine(
        IEnumerable<string>? talentRows = null,
        IEnumerable<string>? accountFlagNames = null)
    {
        var talents = new List<string>();
        for (var i = 0; i < 1000; i++)
        {
            talents.Add(TalentRow($"Bulk_Talent_{i}"));
        }

        for (var i = 0; i < 141; i++)
        {
            talents.Add(TalentRow($"Prospect_Mission_{i}"));
        }

        talents.AddRange(talentRows ?? []);

        var items = new List<string>();
        for (var i = 0; i < 300; i++)
        {
            items.Add(ItemRow($"Meta_Bulk_{i}", meta: true));
        }

        items.Add(ItemRow("Meta_Sword", meta: true, durable: "Durable_Sword"));
        items.Add(ItemRow("Raw_Stone", meta: false));

        var accountFlags = (accountFlagNames ?? Enumerable.Range(0, 90).Select(i => $"AFlag_{i}")).Select(NameRow);
        var characterFlags = Enumerable.Range(0, 45).Select(i => NameRow($"CFlag_{i}"));
        var accolades = Enumerable.Range(0, 401).Select(i => NameRow($"Accolade_{i}"));
        var bestiary = Enumerable.Range(0, 101).Select(i => NameRow($"Creature_{i}"));
        var prospects = Enumerable.Range(0, 150).Select(i => ProspectRow($"Map_Prospect_{i}"))
            .Append(ProspectRow("Outpost_Forest", dropName: "ARCWOOD: Outpost"));

        return
        [
            Table("Talent", talents),
            Table("ItemStaticData", items),
            Table("DurableData", _durableRows),
            Table("AccountFlag", accountFlags),
            Table("CharacterFlag", characterFlags),
            Table("AccoladeData", accolades),
            Table("BestiaryData", bestiary),
            Table("IcarusProspect", prospects),
        ];
    }

    // ---- synthetic "current" catalogs matching the bulk rows -----------------

    private static string CurrentCatalog(string fileName) => fileName switch
    {
        "talents.json" => Catalog("rows", "\"dataTable\": \"D_Talents\",",
            Enumerable.Range(0, 1000).Select(i => $"{{\"rowName\": \"Bulk_Talent_{i}\", \"displayName\": null}}")
                .Append("{\"rowName\": \"Curated_Kept\", \"displayName\": \"My Curated Name\", \"maxRank\": 3}")
                .Append("{\"rowName\": \"Was_NotLive\", \"displayName\": null, \"live\": false}")),
        "items.json" => Catalog("rows", null,
            Enumerable.Range(0, 300).Select(i => $"{{\"rowName\": \"Meta_Bulk_{i}\", \"displayName\": null}}")
                .Append("{\"rowName\": \"Curated_Extra\", \"displayName\": \"Kept Forever\"}")),
        "accountflags.json" => Catalog("names", "\"rowStruct\": \"/Script/Icarus.AccountFlag\",",
            Enumerable.Range(0, 88).Select(i => $"\"AFlag_{i}\"")),
        "characterflags.json" => Catalog("names", "\"rowStruct\": \"/Script/Icarus.CharacterFlag\",",
            Enumerable.Range(0, 45).Select(i => $"\"CFlag_{i}\"")),
        "accolades.json" => Catalog("rows", "\"dataTable\": \"D_Accolades\",",
            Enumerable.Range(0, 401).Select(i => $"{{\"rowName\": \"Accolade_{i}\", \"displayName\": null}}")),
        "bestiary.json" => Catalog("rows", "\"dataTable\": \"D_BestiaryData\",",
            Enumerable.Range(0, 101).Select(i => $"{{\"rowName\": \"Creature_{i}\", \"displayName\": null}}")),
        "missions.json" => Catalog("missions", null,
            ["{\"rowName\": \"Prospect_Old\", \"tree\": \"T\", \"requires\": [], \"defaultUnlocked\": false}"]),
        "prospects.json" => Catalog("rows", "\"dataTable\": \"D_ProspectList\",",
            ["{\"rowName\": \"Old_Prospect\", \"displayName\": null}"]),
        _ => throw new InvalidOperationException(fileName),
    };

    private static string Catalog(string arrayName, string? extraHeader, IEnumerable<string> entries) =>
        $"{{\"catalogVersion\": \"old\", {extraHeader ?? ""} \"source\": \"test\", \"{arrayName}\": [{string.Join(",", entries)}]}}";

    private static CatalogRefreshResult Run(List<MinedTable>? mine = null) =>
        CatalogRefreshService.Refresh(mine ?? DefaultMine(), CurrentCatalog, "stamp-1");

    // ---- behaviors -----------------------------------------------------------

    [Fact]
    public void Refresh_AppliesTheSupersetTalentRules()
    {
        var result = Run(DefaultMine(talentRows:
        [
            TalentRow("Brand_New", display: "Shiny New Talent", rewards: 2),
            TalentRow("Was_NotLive"), // returns to the live game
            TalentRow("Curated_Kept", display: "Game Name Ignored", rewards: 4),
        ]));

        result.Ok.Should().BeTrue(string.Join(" | ", result.Report));
        using var talents = JsonDocument.Parse(result.UpdatedCatalogs["talents.json"]);
        var rows = talents.RootElement.GetProperty("rows").EnumerateArray()
            .ToDictionary(r => r.GetProperty("rowName").GetString()!, r => r);

        rows["Brand_New"].GetProperty("displayName").GetString().Should().Be("Shiny New Talent");
        rows["Brand_New"].GetProperty("maxRank").GetInt32().Should().Be(2);
        rows["Was_NotLive"].TryGetProperty("live", out _).Should().BeFalse("a returned row goes live again (no live flag)");
        rows["Curated_Kept"].GetProperty("displayName").GetString().Should().Be("My Curated Name", "curated names are never overwritten");
        rows["Curated_Kept"].GetProperty("maxRank").GetInt32().Should().Be(4, "maxRank refreshes from the mine");
        rows["Prospect_Mission_0"].TryGetProperty("live", out _).Should().BeFalse("mined prospect rows are live");
        talents.RootElement.GetProperty("catalogVersion").GetString().Should().Be("stamp-1");
    }

    [Fact]
    public void Refresh_MarksVanishedTalents_NotLive_InsteadOfDeleting()
    {
        // Curated_Kept and Was_NotLive are NOT in the default mine.
        var result = Run();

        result.Ok.Should().BeTrue(string.Join(" | ", result.Report));
        using var talents = JsonDocument.Parse(result.UpdatedCatalogs["talents.json"]);
        var kept = talents.RootElement.GetProperty("rows").EnumerateArray()
            .Single(r => r.GetProperty("rowName").GetString() == "Curated_Kept");
        kept.GetProperty("live").GetBoolean().Should().BeFalse("vanished rows are kept as not-live");
        kept.GetProperty("displayName").GetString().Should().Be("My Curated Name");
        kept.GetProperty("maxRank").GetInt32().Should().Be(3, "the prior mined max survives while not-live");
    }

    [Fact]
    public void Refresh_AddsMetaItems_KeepsCuratedExtras_AndBakesExactDurability()
    {
        var result = Run();

        result.Ok.Should().BeTrue(string.Join(" | ", result.Report));
        using var items = JsonDocument.Parse(result.UpdatedCatalogs["items.json"]);
        var rows = items.RootElement.GetProperty("rows").EnumerateArray()
            .ToDictionary(r => r.GetProperty("rowName").GetString()!, r => r);

        rows.Should().ContainKey("Meta_Sword", "new Item.Meta rows join the stash picker");
        rows["Meta_Sword"].GetProperty("maxDurability").GetInt32().Should().Be(750, "exact from the DurableData join");
        rows.Should().ContainKey("Curated_Extra", "curated non-Meta rows are never removed");
        rows.Should().NotContainKey("Raw_Stone", "non-Meta live items stay out");
    }

    [Fact]
    public void Refresh_AcceptsFlagAppends_ButRejectsAnyOrderDivergence()
    {
        // Default mine appends AFlag_88 + AFlag_89 to the 88-name current list — accepted.
        var ok = Run();
        ok.Ok.Should().BeTrue(string.Join(" | ", ok.Report));
        using (var flags = JsonDocument.Parse(ok.UpdatedCatalogs["accountflags.json"]))
        {
            flags.RootElement.GetProperty("names").GetArrayLength().Should().Be(90);
        }

        // An insertion at index 0 shifts every id — the WHOLE refresh must be rejected.
        var shifted = _injectedFlag.Concat(Enumerable.Range(0, 89).Select(i => $"AFlag_{i}"));
        var rejected = Run(DefaultMine(accountFlagNames: shifted));

        rejected.Ok.Should().BeFalse();
        rejected.UpdatedCatalogs.Should().BeEmpty("a rejected refresh must not offer partial output");
        rejected.Report.Should().Contain(r => r.Contains("ORDER DIVERGENCE"));
    }

    [Fact]
    public void Refresh_RegeneratesMissions_FromProspectRows()
    {
        var result = Run();

        result.Ok.Should().BeTrue(string.Join(" | ", result.Report));
        using var missions = JsonDocument.Parse(result.UpdatedCatalogs["missions.json"]);
        var rows = missions.RootElement.GetProperty("missions").EnumerateArray().ToList();
        rows.Should().HaveCount(141, "missions are fully derived from the mined Prospect_* rows");
        rows[0].GetProperty("requires").EnumerateArray().Select(e => e.GetString())
            .Should().Contain("Prospect_Root");
    }

    [Fact]
    public void Refresh_RegeneratesProspects_WithDropNames()
    {
        var result = Run();

        result.Ok.Should().BeTrue(string.Join(" | ", result.Report));
        using var prospects = JsonDocument.Parse(result.UpdatedCatalogs["prospects.json"]);
        var rows = prospects.RootElement.GetProperty("rows").EnumerateArray()
            .ToDictionary(r => r.GetProperty("rowName").GetString()!, r => r);

        rows.Should().HaveCount(151, "prospects are fully regenerated from the mined IcarusProspect table");
        rows.Should().NotContainKey("Old_Prospect", "regeneration replaces the prior list");
        rows["Outpost_Forest"].GetProperty("displayName").GetString().Should().Be("ARCWOOD: Outpost", "the drop name is the display name");
        rows["Map_Prospect_0"].GetProperty("displayName").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Refresh_NeverTouchesMetaResources()
    {
        Run().UpdatedCatalogs.Should().NotContainKey("metaresources.json", "the curated currency whitelist is never regenerated");
    }

    [Fact]
    public void Refresh_MissingTable_RejectsEverything()
    {
        var mine = DefaultMine().Where(t => t.RowStruct != "BestiaryData").ToList();

        var result = CatalogRefreshService.Refresh(mine, CurrentCatalog, "stamp-1");

        result.Ok.Should().BeFalse();
        result.Report.Should().Contain(r => r.Contains("BestiaryData"));
    }
}
