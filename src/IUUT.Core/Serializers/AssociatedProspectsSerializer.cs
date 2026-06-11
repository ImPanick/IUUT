using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes an <see cref="AssociatedProspectsModel"/> back into its nested-stringified container (on-disk write is SafeSaveWriter's job).</summary>
public static class AssociatedProspectsSerializer
{
    /// <summary>Serializes using the model's captured <see cref="AssociatedProspectsModel.ContainerKey"/>.</summary>
    public static string Serialize(AssociatedProspectsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        // Re-wrap each association in its "AssociatedProspect" object to match the on-disk format.
        return NestedStringifiedJson.Serialize(
            model.ContainerKey,
            model.Prospects.Select(p => new AssociatedProspectEntry { AssociatedProspect = p }));
    }
}
