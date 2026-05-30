using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>One completed-accolade record in <see cref="AccoladesModel.CompletedAccolades"/> (field guide §3, master §8.4).</summary>
public sealed class AccoladeEntry
{
    /// <summary>The accolade's <c>D_Accolades</c> reference.</summary>
    public DataTableRef Accolade { get; set; } = new();

    /// <summary>Completion timestamp, format <c>YYYY.MM.DD-HH.MM.SS</c>.</summary>
    public string TimeCompleted { get; set; } = "";

    /// <summary>Prospect GUID where it was earned, or empty string.</summary>
    public string ProspectID { get; set; } = "";

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
