namespace IUUT.Core.Services;

/// <summary>Configuration for <see cref="SteamProfileResolverService"/> (master doc §7.5.1 Settings).</summary>
public sealed class SteamProfileResolverOptions
{
    /// <summary>User-provided Steam Web API key. When null/empty, online resolution is skipped.</summary>
    public string? ApiKey { get; set; }

    /// <summary>How long a cached name is trusted before re-resolving. Default 7 days.</summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Master toggle for the online (Steam Web API) resolution step. Default on.</summary>
    public bool EnableOnlineResolution { get; set; } = true;
}
