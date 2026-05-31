namespace IUUT.App.ViewModels;

/// <summary>One entry in the Custom editor's category sidebar (master doc §10.3).</summary>
public sealed record CustomCategory
{
    /// <summary>Sidebar icon glyph.</summary>
    public required string Glyph { get; init; }

    /// <summary>Sidebar / header label.</summary>
    public required string Label { get; init; }

    /// <summary>What this category edits.</summary>
    public required string Description { get; init; }

    /// <summary>Build status (which Core service backs it; "future"; etc.).</summary>
    public required string Status { get; init; }
}
