namespace IUUT.Core.Services;

/// <summary>
/// The online PersonaName source (Steam Web API <c>GetPlayerSummaries</c>). This is the
/// <b>only</b> outbound network call permitted in IUUT (CONSTITUTION V); it is optional
/// and gated behind a user-provided API key. Implementations must not be invoked unless
/// the resolver has exhausted the offline sources.
/// </summary>
public interface ISteamWebApiClient
{
    /// <summary>
    /// Resolves PersonaNames for the given SteamID64s. Returns a map containing only the
    /// ids that resolved (omit the rest). Implementations should batch (≤100 per request).
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetPersonaNamesAsync(
        IReadOnlyCollection<string> steamId64s,
        string apiKey,
        CancellationToken cancellationToken = default);
}
