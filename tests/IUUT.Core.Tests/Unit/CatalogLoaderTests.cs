using System.Text;
using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Catalog;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class CatalogLoaderTests
{
    private const string SampleJson = """
        {
            "catalogVersion": "2026-02-mendel",
            "dataTable": "D_Example",
            "source": "test",
            "rows": [
                { "rowName": "Alpha", "displayName": "The Alpha" },
                { "rowName": "Beta", "displayName": null, "maxRank": 4 }
            ]
        }
        """;

    private static CatalogTable LoadSample() =>
        CatalogLoader.Load(new MemoryStream(new UTF8Encoding(false).GetBytes(SampleJson)));

    [Fact]
    public void Load_ReadsVersionTableAndRows()
    {
        var table = LoadSample();

        table.DataTable.Should().Be("D_Example");
        table.CatalogVersion.Should().Be("2026-02-mendel");
        table.Count.Should().Be(2);
    }

    [Fact]
    public void TryGet_KnownRow_ReturnsRow()
    {
        var table = LoadSample();

        table.TryGet("Alpha", out var row).Should().BeTrue();
        row!.DisplayName.Should().Be("The Alpha");
    }

    [Fact]
    public void Label_FallsBackToHumanizedRowName_WhenDisplayNameMissing()
    {
        var table = LoadSample();

        table.Label("Alpha").Should().Be("The Alpha", "a curated display name wins");
        table.Label("Beta").Should().Be("Beta", "no display name falls back to the humanized row name");
    }

    [Theory]
    [InlineData("Larkwell_Armor_Alpha_Chest", "Larkwell Armor Alpha Chest")]
    [InlineData("Envirosuit_Tier2", "Envirosuit Tier 2")]
    [InlineData("Meta_Crossbow_Inaris_D", "Meta Crossbow Inaris D")]
    [InlineData("Basic_Quiver", "Basic Quiver")]
    [InlineData("Plain", "Plain")]
    [InlineData("", "")]
    public void Humanize_SplitsUnderscoresAndBoundaries(string rowName, string expected)
    {
        CatalogName.Humanize(rowName).Should().Be(expected);
    }

    [Fact]
    public void Contains_UnknownRow_IsFalse_AndTolerant()
    {
        var table = LoadSample();

        table.Contains("Nonexistent").Should().BeFalse();
        table.TryGet("Nonexistent", out _).Should().BeFalse();
        table.Label("Nonexistent").Should().Be("Nonexistent", "unknown rows round-trip as themselves (CONSTITUTION VI)");
    }

    [Fact]
    public void Row_PreservesTableSpecificFields_ViaExtra()
    {
        var table = LoadSample();
        table.TryGet("Beta", out var beta).Should().BeTrue();

        beta!.Extra.Should().ContainKey("maxRank");
        beta.Extra!["maxRank"].GetInt32().Should().Be(4);
    }

    [Fact]
    public void Load_Empty_Throws()
    {
        var act = () => CatalogLoader.Load(new MemoryStream(new UTF8Encoding(false).GetBytes("null")));

        act.Should().Throw<JsonException>();
    }

    private const string LiveSampleJson = """
        {
            "catalogVersion": "v",
            "dataTable": "D_Example",
            "source": "test",
            "rows": [
                { "rowName": "LiveDefault", "displayName": "Live by default" },
                { "rowName": "Staged", "displayName": "Staged content", "live": false }
            ]
        }
        """;

    private static CatalogTable LoadLiveSample() =>
        CatalogLoader.Load(new MemoryStream(new UTF8Encoding(false).GetBytes(LiveSampleJson)));

    [Fact]
    public void Live_DefaultsTrue_AndParsesFalse()
    {
        var table = LoadLiveSample();

        table.TryGet("LiveDefault", out var live).Should().BeTrue();
        live!.Live.Should().BeTrue("rows without a 'live' field are live by default");
        table.TryGet("Staged", out var staged).Should().BeTrue();
        staged!.Live.Should().BeFalse("\"live\": false marks staged / not-live content");
    }

    [Fact]
    public void LiveRowNames_And_IsLive_ExcludeNotLiveRows()
    {
        var table = LoadLiveSample();

        table.LiveRowNames.Should().BeEquivalentTo("LiveDefault");
        table.IsLive("LiveDefault").Should().BeTrue();
        table.IsLive("Staged").Should().BeFalse();
        table.IsLive("Unknown").Should().BeTrue("absence from the catalog never marks something not-live (CONSTITUTION VI)");
    }
}
