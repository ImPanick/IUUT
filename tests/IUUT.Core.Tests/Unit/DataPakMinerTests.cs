using System.IO.Compression;
using System.Text;
using FluentAssertions;
using IUUT.Core.DataPak;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the runtime catalog self-refresh miner against synthetic paks (zlib blocks built with
/// <see cref="ZLibStream"/> — the same <c>78 9C</c> format the game uses). No real game data.
/// </summary>
public class DataPakMinerTests
{
    private const string TableA = """
        {"RowStruct": "/Script/Icarus.Talent", "Defaults": {}, "Rows": [
            {"Name": "Alpha_Talent", "DisplayName": "with { brace in string"},
            {"Name": "Beta_Talent"}
        ]}
        """;

    private const string TableB = """
        {"RowStruct": "/Script/Icarus.AccountFlag", "Rows": [{"Name": "Flag_One"}]}
        """;

    private static byte[] Zlib(string text)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            zlib.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static byte[] SyntheticPak(params string[] tables)
    {
        using var pak = new MemoryStream();
        var garbage = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };
        pak.Write(garbage);
        foreach (var table in tables)
        {
            var block = Zlib(table);
            pak.Write(block);
            pak.Write(garbage);
        }

        return pak.ToArray();
    }

    [Fact]
    public void Mine_FindsEveryTable_ByRowStruct_WithRowCounts()
    {
        var tables = DataPakMiner.Mine(SyntheticPak(TableA, TableB));

        tables.Should().HaveCount(2);
        var talent = tables.Should().ContainSingle(t => t.RowStruct == "Talent").Subject;
        talent.ApproxRows.Should().Be(2);
        talent.Json.Should().Contain("Alpha_Talent").And.Contain("with { brace in string",
            "braces inside JSON strings must not break the top-level splitter");
        tables.Should().ContainSingle(t => t.RowStruct == "AccountFlag").Which.ApproxRows.Should().Be(1);
    }

    [Fact]
    public void Mine_GarbageBytes_ReturnsEmpty_AndDoesNotThrow()
    {
        var garbage = Enumerable.Range(0, 4096).Select(i => (byte)(i * 37 % 256)).ToArray();

        DataPakMiner.Mine(garbage).Should().BeEmpty();
    }

    [Fact]
    public void Mine_ReportsProgress()
    {
        var messages = new List<string>();
        var progress = new Progress<string>(messages.Add);

        DataPakMiner.Mine(SyntheticPak(TableA), progress);

        // Progress<T> posts via the sync context; give the thread-pool posts a moment.
        SpinWait.SpinUntil(() => messages.Count >= 3, TimeSpan.FromSeconds(2));
        messages.Should().Contain(m => m.Contains("Mined 1 DataTables"));
    }

    [Fact]
    public void Locator_ProbesOverrideFirst_ThenSteamRoot_ThenVdfLibraries()
    {
        var temp = Directory.CreateTempSubdirectory("iuut-locator-test");
        try
        {
            var steamApps = Directory.CreateDirectory(Path.Combine(temp.FullName, "steamapps"));
            File.WriteAllText(Path.Combine(steamApps.FullName, "libraryfolders.vdf"), """
                "libraryfolders"
                {
                    "0" { "path" "D:\\FakeLibrary" }
                }
                """);

            var candidates = DataPakLocator.Candidates(@"X:\override\data.pak", temp.FullName).ToList();

            candidates.Should().HaveCount(3);
            candidates[0].Should().Be(@"X:\override\data.pak", "the explicit override probes first");
            candidates[1].Should().Be(Path.Combine(temp.FullName, DataPakLocator.RelativePakPath), "the Steam root's own install probes second");
            candidates[2].Should().Be(Path.Combine(@"D:\FakeLibrary", DataPakLocator.RelativePakPath), "libraryfolders.vdf libraries probe last");
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [Fact]
    public void Locator_MissingVdf_YieldsJustOverrideAndRoot()
    {
        var temp = Directory.CreateTempSubdirectory("iuut-locator-novdf");
        try
        {
            var candidates = DataPakLocator.Candidates(overridePath: null, steamRoot: temp.FullName).ToList();

            candidates.Should().ContainSingle().Which.Should().Be(Path.Combine(temp.FullName, DataPakLocator.RelativePakPath));
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }
}
