using Wpf.Ui.Controls;

namespace IUUT.App.ViewModels;

/// <summary>One entry in the Custom editor's category sidebar (master doc §10.3).</summary>
public sealed record CustomCategory
{
    /// <summary>Stable key used to select the editor (independent of the display label).</summary>
    public required string Key { get; init; }

    /// <summary>Sidebar line icon (Fluent System Icons).</summary>
    public required SymbolRegular Glyph { get; init; }

    /// <summary>Sidebar / header label.</summary>
    public required string Label { get; init; }

    /// <summary>What this category edits.</summary>
    public required string Description { get; init; }

    /// <summary>Build status (which Core service backs it; "future"; etc.).</summary>
    public required string Status { get; init; }

    /// <summary>Sidebar intent group (DE-3 IA): Rescue / Progression / World / Advanced.</summary>
    public string Group { get; init; } = "";

    /// <summary>False for pre-placed Tier-2 homes (shown greyed with a tier tag, not selectable).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Tier tag shown on disabled entries (e.g. "T2"); empty for live entries.</summary>
    public string Tier { get; init; } = "";
}
