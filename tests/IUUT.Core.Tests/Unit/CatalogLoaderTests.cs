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
        CatalogLoader.Load(new MemoryStream(Encoding.UTF8.GetBytes(SampleJson)));

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
    public void Label_FallsBackToRowName_WhenDisplayNameMissing()
    {
        var table = LoadSample();

        table.Label("Alpha").Should().Be("The Alpha");
        table.Label("Beta").Should().Be("Beta", "no display name falls back to the row name");
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
        var act = () => CatalogLoader.Load(new MemoryStream(Encoding.UTF8.GetBytes("null")));

        act.Should().Throw<JsonException>();
    }
}
