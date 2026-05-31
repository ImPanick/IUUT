using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class SteamProfileResolverServiceTests
{
    private const string Id = "00000000000000001";

    private static SteamProfileResolverService Make(
        SteamProfileCache cache,
        ILocalSteamNames local,
        ISteamWebApiClient? web = null,
        SteamProfileResolverOptions? options = null) =>
        new(cache, local, FixedClock.Default, options, web);

    [Fact]
    public async Task ResolveAsync_LocalVdfHit_ReturnsLocalAndCaches()
    {
        var cache = new SteamProfileCache();
        var sut = Make(cache, new FakeLocalSteamNames { [Id] = "Joseph" });

        var result = await sut.ResolveAsync(Id);

        result.PersonaName.Should().Be("Joseph");
        result.Source.Should().Be(SteamNameSource.LocalVdf);
        cache.TryGetFresh(Id, TimeSpan.FromDays(7), FixedClock.Default).Should().NotBeNull("the result must be cached");
    }

    [Fact]
    public async Task ResolveAsync_FreshCacheHit_DoesNotConsultLocalOrApi()
    {
        var cache = new SteamProfileCache();
        cache.Store(new SteamProfileDisplay(Id, "Cached", SteamNameSource.LocalVdf, FixedClock.Default.UtcNow));
        var web = new FakeSteamWebApiClient();
        var sut = Make(cache, new FakeLocalSteamNames(), web, new SteamProfileResolverOptions { ApiKey = "k" });

        var result = await sut.ResolveAsync(Id);

        result.Source.Should().Be(SteamNameSource.Cache);
        result.PersonaName.Should().Be("Cached");
        web.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ResolveAsync_StaleCache_ReResolvesFromLocal()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var cache = new SteamProfileCache();
        cache.Store(new SteamProfileDisplay(Id, "Old", SteamNameSource.SteamApi, now.AddDays(-8)));
        var sut = new SteamProfileResolverService(cache, new FakeLocalSteamNames { [Id] = "Fresh" }, new FixedClock(now));

        var result = await sut.ResolveAsync(Id);

        result.Source.Should().Be(SteamNameSource.LocalVdf);
        result.PersonaName.Should().Be("Fresh");
    }

    [Fact]
    public async Task ResolveAsync_NoLocalNoApi_ReturnsFallback()
    {
        var sut = Make(new SteamProfileCache(), new FakeLocalSteamNames());

        var result = await sut.ResolveAsync(Id);

        result.Source.Should().Be(SteamNameSource.Fallback);
        result.PersonaName.Should().BeNull();
        result.DisplayLabel.Should().Be(Id);
    }

    [Fact]
    public async Task ResolveAsync_OnlineResolves_WhenLocalMisses()
    {
        var web = new FakeSteamWebApiClient { [Id] = "OnlineName" };
        var sut = Make(new SteamProfileCache(), new FakeLocalSteamNames(), web, new SteamProfileResolverOptions { ApiKey = "k" });

        var result = await sut.ResolveAsync(Id);

        result.Source.Should().Be(SteamNameSource.SteamApi);
        result.PersonaName.Should().Be("OnlineName");
        web.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ResolveAsync_OnlineSkipped_WhenNoApiKey()
    {
        var web = new FakeSteamWebApiClient { [Id] = "OnlineName" };
        var sut = Make(new SteamProfileCache(), new FakeLocalSteamNames(), web, new SteamProfileResolverOptions { ApiKey = null });

        var result = await sut.ResolveAsync(Id);

        result.Source.Should().Be(SteamNameSource.Fallback);
        web.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ResolveAsync_ApiThrows_FallsBackGracefully()
    {
        var web = new FakeSteamWebApiClient(throwHttp: true);
        var sut = Make(new SteamProfileCache(), new FakeLocalSteamNames(), web, new SteamProfileResolverOptions { ApiKey = "k" });

        var result = await sut.ResolveAsync(Id);

        result.Source.Should().Be(SteamNameSource.Fallback, "a network failure must not throw (CONSTITUTION IX)");
    }

    [Fact]
    public async Task ResolveAllAsync_PreservesInputOrder_AndBatchesOnlineMisses()
    {
        const string a = "00000000000000001";
        const string b = "00000000000000002";
        const string c = "00000000000000003";
        var local = new FakeLocalSteamNames { [b] = "LocalB" };
        var web = new FakeSteamWebApiClient { [a] = "OnlineA", [c] = "OnlineC" };
        var sut = Make(new SteamProfileCache(), local, web, new SteamProfileResolverOptions { ApiKey = "k" });

        var results = await sut.ResolveAllAsync([c, a, b]);

        results.Select(r => r.SteamId64).Should().Equal(c, a, b);
        results[0].PersonaName.Should().Be("OnlineC");
        results[2].Source.Should().Be(SteamNameSource.LocalVdf);
        web.CallCount.Should().Be(1, "online misses are resolved in a single batched request");
        web.LastRequestedIds.Should().BeEquivalentTo([a, c]);
    }
}
