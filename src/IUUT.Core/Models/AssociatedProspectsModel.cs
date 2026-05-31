namespace IUUT.Core.Models;

/// <summary>
/// <c>AssociatedProspects_Slot_N.json</c> — a character's claimed-prospect index (master doc §8.8).
/// Same nested-stringified-array container as <c>Characters.json</c>; the outer key is the file
/// name, captured in <see cref="ContainerKey"/> so it round-trips exactly.
/// </summary>
public sealed class AssociatedProspectsModel
{
    /// <summary>The single outer container key (the file name, e.g. <c>AssociatedProspects_Slot_1.json</c>).</summary>
    public required string ContainerKey { get; init; }

    /// <summary>The prospect associations for this character slot.</summary>
    public List<AssociatedProspect> Prospects { get; init; } = [];
}
