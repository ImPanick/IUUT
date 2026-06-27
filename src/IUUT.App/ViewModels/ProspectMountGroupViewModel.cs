using IUUT.Core.Prospects.World;

namespace IUUT.App.ViewModels;

/// <summary>
/// One prospect's deployed mounts in the Mounts editor — read-only (issue #19). These live in the
/// prospect's world blob, separate from the editable <c>Mounts.json</c> roster.
/// </summary>
public sealed class ProspectMountGroupViewModel
{
    /// <summary>Wraps a Core mount group for display.</summary>
    public ProspectMountGroupViewModel(ProspectMountGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        ProspectName = group.ProspectName;
        Mounts = group.Mounts
            .Select(m => string.IsNullOrWhiteSpace(m.MountType) ? m.Label : $"{m.Label}   ·   {m.MountType}")
            .ToList();
    }

    /// <summary>The prospect (world-save file) name.</summary>
    public string ProspectName { get; }

    /// <summary>Each deployed mount, as "name · type".</summary>
    public IReadOnlyList<string> Mounts { get; }

    /// <summary>Group header: prospect name + mount count.</summary>
    public string Header => $"{ProspectName}   ({Mounts.Count})";
}
