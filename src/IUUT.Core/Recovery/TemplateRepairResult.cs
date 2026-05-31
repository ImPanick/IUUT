namespace IUUT.Core.Recovery;

/// <summary>The outcome of a template repair (master doc §11.3 F-023).</summary>
public sealed record TemplateRepairResult
{
    /// <summary>Whether this file type has a safe template (only the four modeled files do).</summary>
    public required bool CanRepair { get; init; }

    /// <summary>The rebuilt content to write, or <c>null</c> when <see cref="CanRepair"/> is false.</summary>
    public string? NewContent { get; init; }

    /// <summary>True if real data was recovered (the file actually parsed); false for a bare skeleton.</summary>
    public bool Salvaged { get; init; }

    /// <summary>True when a skeleton was rebuilt (data lost) — drives the partial-recovery flag.</summary>
    public bool IsPartial { get; init; }

    /// <summary>Human-readable note for the recovery report.</summary>
    public required string Notes { get; init; }

    /// <summary>The file parsed cleanly; reserialized without loss.</summary>
    public static TemplateRepairResult Salvage(string content, string notes) =>
        new() { CanRepair = true, NewContent = content, Salvaged = true, IsPartial = false, Notes = notes };

    /// <summary>A bare valid skeleton was rebuilt; original data was lost.</summary>
    public static TemplateRepairResult Skeleton(string content, string notes) =>
        new() { CanRepair = true, NewContent = content, Salvaged = false, IsPartial = true, Notes = notes };

    /// <summary>This file has no safe template and cannot be rebuilt automatically.</summary>
    public static TemplateRepairResult Unsupported(string fileName) =>
        new()
        {
            CanRepair = false,
            NewContent = null,
            Salvaged = false,
            IsPartial = false,
            Notes = $"{fileName} has no safe template; recover it from a backup or rebuild it in-game.",
        };
}
