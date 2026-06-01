namespace IUUT.App.ViewModels;

/// <summary>
/// One engine/account flag for the flag editor: its numeric <see cref="Id"/>, the decoded friendly
/// <see cref="Label"/> (from the flag catalog, or <c>Flag N</c> if beyond the snapshot), and whether
/// it represents mission/story completion. Used both for the current-flags list and the add picker.
/// </summary>
public sealed class FlagRowViewModel
{
    /// <summary>Creates a flag row.</summary>
    public FlagRowViewModel(uint id, string label, bool isMission)
    {
        Id = id;
        Label = string.IsNullOrEmpty(label) ? $"Flag {id}" : label;
        IsMission = isMission;
    }

    /// <summary>The numeric flag id (0-based DataTable row index).</summary>
    public uint Id { get; }

    /// <summary>The decoded friendly label.</summary>
    public string Label { get; }

    /// <summary>Whether this flag is a mission/story-completion flag.</summary>
    public bool IsMission { get; }

    /// <summary>List display: id + label.</summary>
    public string Display => $"{Id} · {Label}";
}
