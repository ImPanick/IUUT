using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// <c>Accolades.json</c> — the achievement/accolade log (master doc §8.4). Three
/// top-level keys observed on the live save: <see cref="CompletedAccolades"/> (the
/// edit target) plus <c>PlayerTrackers</c> and <c>PlayerTaskListTrackers</c> — large
/// flat counter objects that IUUT does not edit. The latter two are **preserved
/// verbatim** through <see cref="AdditionalData"/> (CONSTITUTION VI) rather than
/// modelled, since their members are open-ended progress counters.
/// </summary>
public sealed class AccoladesModel
{
    /// <summary>The completed-accolade log; Lazy Max appends missing catalog rows here.</summary>
    public List<AccoladeEntry> CompletedAccolades { get; set; } = [];

    /// <summary>
    /// Unknown / unmodelled top-level members — including <c>PlayerTrackers</c> and
    /// <c>PlayerTaskListTrackers</c> — preserved verbatim on round-trip (CONSTITUTION VI).
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
