using IUUT.Core.Abstractions;

namespace IUUT.Core.Services;

/// <summary>
/// IUUT's local Steam-name cache (master doc §7.5.1). Persisted to
/// <c>%AppData%\IUUT\steam-profile-cache.json</c> by <see cref="SteamProfileCacheStore"/>;
/// kept in memory during a session.
/// </summary>
public sealed class SteamProfileCache
{
    /// <summary>Cache schema version.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Resolved entries keyed by SteamID64.</summary>
    public Dictionary<string, SteamProfileCacheEntry> Entries { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Returns a cached display if a fresh (within <paramref name="ttl"/>) entry exists.</summary>
    public SteamProfileDisplay? TryGetFresh(string steamId64, TimeSpan ttl, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Entries.TryGetValue(steamId64, out var entry) && clock.UtcNow - entry.ResolvedAt <= ttl)
        {
            return new SteamProfileDisplay(steamId64, entry.PersonaName, SteamNameSource.Cache, entry.ResolvedAt);
        }

        return null;
    }

    /// <summary>Stores a successfully-resolved display (no-op for fallback / null-name results).</summary>
    public void Store(SteamProfileDisplay display)
    {
        ArgumentNullException.ThrowIfNull(display);
        if (display.PersonaName is null || display.ResolvedAtUtc is null)
        {
            return;
        }

        Entries[display.SteamId64] = new SteamProfileCacheEntry
        {
            PersonaName = display.PersonaName,
            Source = display.Source,
            ResolvedAt = display.ResolvedAtUtc.Value,
        };
    }
}

/// <summary>One cached PersonaName resolution.</summary>
public sealed class SteamProfileCacheEntry
{
    /// <summary>The resolved PersonaName.</summary>
    public string PersonaName { get; set; } = "";

    /// <summary>Where it was resolved from.</summary>
    public SteamNameSource Source { get; set; }

    /// <summary>When it was resolved (UTC).</summary>
    public DateTimeOffset ResolvedAt { get; set; }
}
