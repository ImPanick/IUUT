using IUUT.Core.Abstractions;

namespace IUUT.Core.Services;

/// <summary>
/// Resolves SteamID64 → PersonaName for the profile UI (master doc §7.5.1, §9.2),
/// offline-first: <b>cache → local <c>loginusers.vdf</c> → Steam Web API → fallback</b>.
/// The online step is the only network call (CONSTITUTION V) and is skipped unless an
/// API key is configured and the offline sources missed. Never writes save files or
/// renames folders — read-only metadata.
/// </summary>
public sealed class SteamProfileResolverService
{
    private static readonly IReadOnlyDictionary<string, string> _emptyMap =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private readonly SteamProfileCache _cache;
    private readonly ILocalSteamNames _localNames;
    private readonly IClock _clock;
    private readonly SteamProfileResolverOptions _options;
    private readonly ISteamWebApiClient? _webApi;

    /// <summary>Creates the resolver. <paramref name="webApi"/> is optional (offline-only when null).</summary>
    public SteamProfileResolverService(
        SteamProfileCache cache,
        ILocalSteamNames localNames,
        IClock clock,
        SteamProfileResolverOptions? options = null,
        ISteamWebApiClient? webApi = null)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(localNames);
        ArgumentNullException.ThrowIfNull(clock);

        _cache = cache;
        _localNames = localNames;
        _clock = clock;
        _options = options ?? new SteamProfileResolverOptions();
        _webApi = webApi;
    }

    /// <summary>Resolves a single SteamID64.</summary>
    public async Task<SteamProfileDisplay> ResolveAsync(string steamId64, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(steamId64);
        var results = await ResolveAllAsync([steamId64], cancellationToken).ConfigureAwait(false);
        return results[0];
    }

    /// <summary>
    /// Resolves many SteamID64s, returning one display per input (input order, duplicates
    /// included). Offline sources are tried first; remaining ids are batched into a single
    /// online request when online resolution is configured.
    /// </summary>
    public async Task<IReadOnlyList<SteamProfileDisplay>> ResolveAllAsync(
        IEnumerable<string> steamId64s,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steamId64s);

        var input = steamId64s.Where(id => !string.IsNullOrEmpty(id)).ToList();
        var resolved = new Dictionary<string, SteamProfileDisplay>(StringComparer.Ordinal);
        var needOnline = new List<string>();

        foreach (var id in input.Distinct(StringComparer.Ordinal))
        {
            var cached = _cache.TryGetFresh(id, _options.CacheTtl, _clock);
            if (cached is not null)
            {
                resolved[id] = cached;
                continue;
            }

            var localName = _localNames.TryGetPersonaName(id);
            if (!string.IsNullOrEmpty(localName))
            {
                resolved[id] = Store(new SteamProfileDisplay(id, localName, SteamNameSource.LocalVdf, _clock.UtcNow));
                continue;
            }

            needOnline.Add(id);
        }

        if (needOnline.Count > 0 && CanResolveOnline())
        {
            var apiNames = await TryResolveOnlineAsync(needOnline, cancellationToken).ConfigureAwait(false);
            foreach (var id in needOnline)
            {
                resolved[id] = apiNames.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name)
                    ? Store(new SteamProfileDisplay(id, name, SteamNameSource.SteamApi, _clock.UtcNow))
                    : Fallback(id);
            }
        }
        else
        {
            foreach (var id in needOnline)
            {
                resolved[id] = Fallback(id);
            }
        }

        return input.Select(id => resolved[id]).ToList();
    }

    private bool CanResolveOnline() =>
        _webApi is not null
        && _options.EnableOnlineResolution
        && !string.IsNullOrEmpty(_options.ApiKey);

    private async Task<IReadOnlyDictionary<string, string>> TryResolveOnlineAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _webApi!.GetPersonaNamesAsync(ids, _options.ApiKey!, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Offline / API error → degrade to fallback (CONSTITUTION IX: warn, don't fail).
            return _emptyMap;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Request timeout (not a user cancellation) → degrade to fallback.
            return _emptyMap;
        }
    }

    private SteamProfileDisplay Store(SteamProfileDisplay display)
    {
        _cache.Store(display);
        return display;
    }

    private static SteamProfileDisplay Fallback(string steamId64) =>
        new(steamId64, null, SteamNameSource.Fallback, null);
}
