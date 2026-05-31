using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Services;

/// <summary>
/// User-facing IUUT preferences, persisted to <c>&lt;StateRoot&gt;\settings.json</c>.
/// Holds only non-secret settings — the Steam API key is stored separately,
/// DPAPI-encrypted, in <see cref="AppPaths.ApiKeyFile"/> (SECURITY_PROTOCOL §5), never here.
/// Unknown members round-trip verbatim for forward compatibility (CONSTITUTION VI).
/// </summary>
public sealed class AppSettings
{
    /// <summary>Manual save-root override (master doc §7.1). Null = use the auto-detected root.</summary>
    public string? SaveRootOverride { get; set; }

    /// <summary>Master toggle for Steam name resolution (master doc §7.5.1 Settings).</summary>
    public bool EnableSteamNameResolution { get; set; } = true;

    /// <summary>How long cached Steam names are trusted, in days (default 7).</summary>
    public int SteamCacheTtlDays { get; set; } = 7;

    /// <summary>Re-resolve stale Steam names on launch when online (master doc §7.5.1).</summary>
    public bool RefreshSteamNamesOnLaunch { get; set; } = true;

    /// <summary>Unknown/forward-compat members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
