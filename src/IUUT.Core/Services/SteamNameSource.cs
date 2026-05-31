using System.Text.Json.Serialization;

namespace IUUT.Core.Services;

/// <summary>Where a resolved Steam PersonaName came from (master doc §7.5.1).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SteamNameSource>))]
public enum SteamNameSource
{
    /// <summary>From the local IUUT name cache.</summary>
    Cache,

    /// <summary>From the local Steam <c>loginusers.vdf</c> (offline).</summary>
    LocalVdf,

    /// <summary>From the Steam Web API (online).</summary>
    SteamApi,

    /// <summary>Unresolved — the raw SteamID64 is shown.</summary>
    Fallback,
}
