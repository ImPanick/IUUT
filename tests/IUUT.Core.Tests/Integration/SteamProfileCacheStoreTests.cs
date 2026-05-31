using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class SteamProfileCacheStoreTests : IDisposable
{
    private readonly TempDir _temp = new();

    [Fact]
    public void SaveThenLoad_RoundTripsEntries()
    {
        var cache = new SteamProfileCache();
        cache.Store(new SteamProfileDisplay("00000000000000001", "Joseph", SteamNameSource.LocalVdf, FixedClock.Default.UtcNow));
        var path = _temp.File("steam-profile-cache.json");

        SteamProfileCacheStore.Save(path, cache);
        var loaded = SteamProfileCacheStore.Load(path);

        loaded.Entries.Should().ContainKey("00000000000000001");
        loaded.Entries["00000000000000001"].PersonaName.Should().Be("Joseph");
        loaded.Entries["00000000000000001"].Source.Should().Be(SteamNameSource.LocalVdf, "the source enum round-trips as a string");
    }

    [Fact]
    public void Save_WritesUtf8WithoutBom()
    {
        var path = _temp.File("steam-profile-cache.json");

        SteamProfileCacheStore.Save(path, new SteamProfileCache());

        var bytes = File.ReadAllBytes(path);
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyCache()
    {
        var loaded = SteamProfileCacheStore.Load(_temp.File("does-not-exist.json"));

        loaded.Entries.Should().BeEmpty();
        loaded.Version.Should().Be(1);
    }

    public void Dispose() => _temp.Dispose();
}
