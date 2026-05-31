using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly TempDir _temp = new();

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var path = _temp.File("settings.json");
        var settings = new AppSettings
        {
            SaveRootOverride = @"D:\Games\Icarus\Saved",
            EnableSteamNameResolution = false,
            SteamCacheTtlDays = 14,
        };

        AppSettingsStore.Save(path, settings);
        var loaded = AppSettingsStore.Load(path);

        loaded.SaveRootOverride.Should().Be(@"D:\Games\Icarus\Saved");
        loaded.EnableSteamNameResolution.Should().BeFalse();
        loaded.SteamCacheTtlDays.Should().Be(14);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = AppSettingsStore.Load(_temp.File("nope.json"));

        settings.SaveRootOverride.Should().BeNull();
        settings.EnableSteamNameResolution.Should().BeTrue();
        settings.SteamCacheTtlDays.Should().Be(7);
    }

    [Fact]
    public void Save_WritesUtf8WithoutBom()
    {
        var path = _temp.File("settings.json");

        AppSettingsStore.Save(path, new AppSettings());

        File.ReadAllBytes(path).Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [Fact]
    public void Load_PreservesUnknownMembers()
    {
        var path = _temp.File("settings.json");
        File.WriteAllText(path, """{ "SaveRootOverride": "X", "FutureSetting": { "k": 1 } }""");

        var loaded = AppSettingsStore.Load(path);
        loaded.AdditionalData.Should().ContainKey("FutureSetting");

        // Round-trip preserves it (CONSTITUTION VI).
        AppSettingsStore.Save(path, loaded);
        File.ReadAllText(path).Should().Contain("FutureSetting");
    }

    public void Dispose() => _temp.Dispose();
}
