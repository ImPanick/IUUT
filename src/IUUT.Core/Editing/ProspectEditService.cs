using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode prospect edits (master doc §8.8, §8.9): "unstick" a phantom prospect association to
/// free a stuck character, and a read-only world-blob integrity check. The world blob itself is
/// never mutated here (that is the <c>ProspectBlobCodec</c>'s job, WP-28). Pure in-memory.
/// </summary>
public sealed class ProspectEditService
{
    /// <summary>
    /// Removes the association with <paramref name="prospectId"/> from a character's
    /// <c>AssociatedProspects</c> index (the "unstick" action); returns whether it was present.
    /// </summary>
    public bool Unstick(AssociatedProspectsModel associations, string prospectId)
    {
        ArgumentNullException.ThrowIfNull(associations);
        ArgumentException.ThrowIfNullOrEmpty(prospectId);

        var match = associations.Prospects
            .FirstOrDefault(p => string.Equals(p.ProspectId, prospectId, StringComparison.Ordinal));
        return match is not null && associations.Prospects.Remove(match);
    }

    /// <summary>Whether the world blob's SHA-1 still matches its recorded hash (safe to edit the header).</summary>
    public bool IsWorldBlobIntact(ProspectFileModel prospect)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        return ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob);
    }
}
