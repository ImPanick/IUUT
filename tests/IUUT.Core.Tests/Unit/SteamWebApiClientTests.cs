using FluentAssertions;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class SteamWebApiClientTests
{
    [Fact]
    public async Task GetPersonaNames_ParsesResponseAndBuildsRequestUrl()
    {
        const string json = """
            { "response": { "players": [
                { "steamid": "00000000000000001", "personaname": "Joseph" },
                { "steamid": "00000000000000002", "personaname": "Kiara" }
            ] } }
            """;
        var stub = new StubHttpMessageHandler(json);
        using var http = new HttpClient(stub);
        var client = new SteamWebApiClient(http);

        var map = await client.GetPersonaNamesAsync(["00000000000000001", "00000000000000002"], "APIKEY");

        map.Should().HaveCount(2);
        map["00000000000000001"].Should().Be("Joseph");
        map["00000000000000002"].Should().Be("Kiara");
        stub.LastRequestUri!.ToString().Should()
            .Contain("GetPlayerSummaries")
            .And.Contain("key=APIKEY")
            .And.Contain("steamids=00000000000000001,00000000000000002");
    }

    [Fact]
    public async Task GetPersonaNames_EmptyIds_MakesNoRequest()
    {
        var stub = new StubHttpMessageHandler("{}");
        using var http = new HttpClient(stub);
        var client = new SteamWebApiClient(http);

        var map = await client.GetPersonaNamesAsync([], "APIKEY");

        map.Should().BeEmpty();
        stub.LastRequestUri.Should().BeNull("no ids means no network request");
    }
}
