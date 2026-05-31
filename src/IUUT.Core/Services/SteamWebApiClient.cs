using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Services;

/// <summary>
/// <see cref="ISteamWebApiClient"/> over the Steam Web API
/// <c>ISteamUser/GetPlayerSummaries/v2</c> (master doc §7.5.1). This is the <b>only</b>
/// outbound network call in IUUT (CONSTITUTION V). The <see cref="HttpClient"/> is
/// injected so it can be pooled by the host and faked in tests.
/// </summary>
public sealed class SteamWebApiClient : ISteamWebApiClient
{
    private const int MaxIdsPerRequest = 100;
    private const string Endpoint = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/";

    private readonly HttpClient _http;

    /// <summary>Creates a client over the given <see cref="HttpClient"/>.</summary>
    public SteamWebApiClient(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetPersonaNamesAsync(
        IReadOnlyCollection<string> steamId64s,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steamId64s);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (steamId64s.Count == 0)
        {
            return result;
        }

        foreach (var batch in steamId64s.Chunk(MaxIdsPerRequest))
        {
            var uri = new Uri(
                $"{Endpoint}?key={Uri.EscapeDataString(apiKey)}&steamids={string.Join(',', batch)}");

            using var response = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer
                .DeserializeAsync<ApiEnvelope>(stream, JsonSerializerOptions.Default, cancellationToken)
                .ConfigureAwait(false);

            if (payload?.Response?.Players is { } players)
            {
                foreach (var player in players)
                {
                    if (!string.IsNullOrEmpty(player.SteamId) && !string.IsNullOrEmpty(player.PersonaName))
                    {
                        result[player.SteamId] = player.PersonaName;
                    }
                }
            }
        }

        return result;
    }

    private sealed record ApiEnvelope(
        [property: JsonPropertyName("response")] ApiResponse? Response);

    private sealed record ApiResponse(
        [property: JsonPropertyName("players")] List<ApiPlayer>? Players);

    private sealed record ApiPlayer(
        [property: JsonPropertyName("steamid")] string? SteamId,
        [property: JsonPropertyName("personaname")] string? PersonaName);
}
