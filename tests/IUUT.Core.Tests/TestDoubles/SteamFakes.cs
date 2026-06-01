using System.Net;
using System.Text;
using IUUT.Core.Services;

namespace IUUT.Core.Tests.TestDoubles;

/// <summary>In-memory <see cref="ILocalSteamNames"/> for resolver tests.</summary>
internal sealed class FakeLocalSteamNames : ILocalSteamNames
{
    private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);

    public string? this[string steamId64]
    {
        set => _names[steamId64] = value!;
    }

    public string? TryGetPersonaName(string steamId64) =>
        _names.TryGetValue(steamId64, out var name) ? name : null;
}

/// <summary>In-memory <see cref="ISteamWebApiClient"/> that records calls (optionally throws).</summary>
internal sealed class FakeSteamWebApiClient : ISteamWebApiClient
{
    private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
    private readonly bool _throwHttp;

    public FakeSteamWebApiClient(bool throwHttp = false) => _throwHttp = throwHttp;

    public int CallCount { get; private set; }

    public IReadOnlyList<string> LastRequestedIds { get; private set; } = [];

    public string? this[string steamId64]
    {
        set => _names[steamId64] = value!;
    }

    public Task<IReadOnlyDictionary<string, string>> GetPersonaNamesAsync(
        IReadOnlyCollection<string> steamId64s, string apiKey, CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastRequestedIds = steamId64s.ToList();
        if (_throwHttp)
        {
            throw new HttpRequestException("simulated network failure");
        }

        IReadOnlyDictionary<string, string> map = steamId64s
            .Where(_names.ContainsKey)
            .ToDictionary(id => id, id => _names[id], StringComparer.Ordinal);
        return Task.FromResult(map);
    }
}

/// <summary>Returns a canned HTTP response and captures the request URI (for SteamWebApiClient tests).</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseJson;

    public StubHttpMessageHandler(string responseJson) => _responseJson = responseJson;

    public Uri? LastRequestUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_responseJson, new UTF8Encoding(false), "application/json"),
        });
    }
}
