using IUUT.Core.Prospects.World;

namespace IUUT.App.ViewModels;

/// <summary>
/// One deployed mount row: its prospect, its document-order index (the rename key for
/// <c>CustomFileService.RenameProspectMountAsync</c>), and its display label.
/// </summary>
public sealed record DeployedMountViewModel(string ProspectName, int Index, string Name, string Label);

/// <summary>
/// One prospect's deployed mounts in the Mounts editor. Listing closed issue #19; Tier 3 adds
/// in-prospect RENAME (the length-fixup blob write) — the rest of the blob stays untouched.
/// </summary>
public sealed class ProspectMountGroupViewModel
{
    /// <summary>Wraps a Core mount group for display.</summary>
    public ProspectMountGroupViewModel(ProspectMountGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        ProspectName = group.ProspectName;
        Mounts = group.Mounts
            .Select((m, i) => new DeployedMountViewModel(
                group.ProspectName,
                i,
                m.Name,
                string.IsNullOrWhiteSpace(m.MountType) ? m.Label : $"{m.Label}   ·   {m.MountType}"))
            .ToList();
    }

    /// <summary>The prospect (world-save file) name.</summary>
    public string ProspectName { get; }

    /// <summary>Each deployed mount.</summary>
    public IReadOnlyList<DeployedMountViewModel> Mounts { get; }

    /// <summary>Group header: prospect name + mount count.</summary>
    public string Header => $"{ProspectName}   ({Mounts.Count})";
}
